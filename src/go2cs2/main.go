package main

import (
	"bytes"
	"embed"
	_ "embed"
	"flag"
	"fmt"
	"go/ast"
	"go/importer"
	"go/parser"
	"go/printer"
	"go/token"
	"go/types"
	"io"
	"log"
	"os"
	"path"
	"path/filepath"
	"strings"
	"sync"
	"time"
	"unicode"
	"unicode/utf8"

	. "go2cs/hashset"
	. "go2cs/stack"
)

type Options struct {
	indentSpaces        int
	preferVarDecl       bool
	useChannelOperators bool
	includeComments     bool
	parseCgoTargets     bool
	showParseTree       bool
}

type FileEntry struct {
	file     *ast.File
	filePath string
}

// CapturedVarInfo tracks information about captured variables
type CapturedVarInfo struct {
	origIdent *ast.Ident // Original identifier
	copyIdent *ast.Ident // Temporary copy identifier
	varType   types.Type // Type of the variable
	used      bool       // Whether the capture has been used
}

// LambdaCapture handles analysis and tracking of captured variables
type LambdaCapture struct {
	capturedVars    map[*ast.Ident]*CapturedVarInfo  // Map of original idents to their capture info
	stmtCaptures    map[ast.Node]map[*ast.Ident]bool // Track which vars are captured by which stmt
	pendingCaptures map[string]*CapturedVarInfo      // Variables that need declarations before lambda

	currentLambdaVars map[string]string // Original var name to capture name tracking within current lambda

	// Analysis phase tracking
	analysisInLambda  bool     // Currently analyzing a lambda
	currentLambda     ast.Node // Current lambda being analyzed
	detectingCaptures bool

	// Conversion phase tracking
	conversionInLambda bool     // Currently converting a lambda
	currentConversion  ast.Node // Current node being converted
}

type Visitor struct {
	fset               *token.FileSet
	pkg                *types.Package
	info               *types.Info
	file               *token.File
	targetFile         *strings.Builder
	standAloneComments map[token.Pos]string
	sortedCommentPos   []token.Pos
	processedComments  HashSet[token.Pos]
	newline            string
	indentLevel        int
	usesUnsafeCode     bool
	options            Options
	globalIdentNames   map[*ast.Ident]string // Global identifiers to adjusted names map
	globalScope        map[string]*types.Var // Global variable scope

	// ImportSpec variables
	currentImportPath string
	packageImports    *strings.Builder
	importQueue       HashSet[string]
	requiredUsings    HashSet[string]

	// FuncDecl variables
	inFunction       bool
	currentFunction  *types.Func
	paramNames       HashSet[string]
	hasDefer         bool
	hasRecover       bool
	capturedVarCount map[string]int
	tempVarCount     map[string]int

	// BlockStmt variables
	blocks                 Stack[*strings.Builder]
	firstStatementIsReturn bool
	lastStatementWasReturn bool
	identEscapesHeap       map[*ast.Ident]bool
	identNames             map[*ast.Ident]string   // Local identifiers to adjusted names map
	isReassigned           map[*ast.Ident]bool     // Local identifiers to reassignment status map
	scopeStack             []map[string]*types.Var // Stack of local variable scopes
	lambdaCapture          *LambdaCapture          // Lambda capture tracking
}

const RootNamespace = "go"
const PackageSuffix = "_package"
const OutputTypeMarker = ">>MARKER:OUTPUT_TYPE<<"

// Extended unicode characters are being used to help avoid conflicts with Go identifiers
// for intermediate and temporary variables. Some character variants will be better suited
// to different fonts or display environments. Defaults have been chosen based on best
// appearance with the Visual Studio default code font "Cascadia Mono":

const AddressPrefix = "\u13D1"               // Variants: Ꮡ ꝸ
const ShadowVarMarker = "\u0394"             // Variants: Δ Ʌ ꞥ
const CapturedVarMarker = "\u0297"           // Variants: ʗ ɔ ᴄ
const TempVarMarker = "\u1D1B"               // Variants: ᴛ Ŧ ᵀ
const TrueMarker = "\u1427"                  // Variants: ᐧ true
const OverloadDiscriminator = "\uA7F7"       // Variants: ꟷ false
const ElipsisOperator = "\uA4F8\uA4F8\uA4F8" // Variants: ꓸꓸꓸ ᐧᐧᐧ
const ChannelLeftOp = "\u1438\uA7F7"         // Example: `ch.ᐸꟷ(val)` for `ch <- val`
const ChannelRightOp = "\uA7F7\u1433"        // Example: `ch.ꟷᐳ(out var val)` for `val := <-ch`

// TODO: Consider adding removing items that are also reserved by Go to reduce search space
var keywords = NewHashSet[string]([]string{
	// The following are all valid C# keywords, if encountered in Go code they should be escaped
	"abstract", "as", "base", "bool", "catch", "char", "checked", "class", "const", "decimal",
	"delegate", "do", "double", "enum", "event", "explicit", "extern", "finally", "fixed", "foreach",
	"implicit", "in", "interface", "internal", "is", "lock", "namespace", "new", "null", "object",
	"operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
	"sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "this", "throw",
	"try", "typeof", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
	"__argslist", "__makeref", "__reftype", "__refvalue",
	// The following C# type names are reserved by go2cs as they may be used during code conversion
	"GoType", "GoUntyped", "GoTag", "go\u01C3", "OK", "ERR", "VAL", "GetGoTypeName",
	"CastCopy", "ConvertToType", "WhenAny", TrueMarker,
})

// These C# keywords overlap with Go keywords, so they do not need detection
// "break", "byte", "case", "const", "continue", "default", "else", "false", "float", "for",
// "goto", "if", "int", "long", "return", "select", "switch", "true", "var", "uint", "ulong"

//go:embed csproj-template.xml
var csprojTemplate []byte

//go:embed go2cs.ico
var iconFileBytes []byte

//go:embed go2cs.png
var pngFileBytes []byte

//go:embed profiles/*
var publishProfiles embed.FS

func main() {
	commandLine := flag.NewFlagSet(os.Args[0], flag.ContinueOnError)
	commandLine.SetOutput(io.Discard)

	// Define command line flags for options
	indentSpaces := commandLine.Int("indent", 4, "Number of spaces for indentation")
	preferVarDecl := commandLine.Bool("var", true, "Prefer \"var\" declarations")
	useChannelOperators := commandLine.Bool("uco", true, fmt.Sprintf("Use channel operators: %s / %s", ChannelLeftOp, ChannelRightOp))
	includeComments := commandLine.Bool("comments", false, "Include comments in output")
	parseCgoTargets := commandLine.Bool("cgo", false, "Parse cgo targets")
	showParseTree := commandLine.Bool("tree", false, "Show parse tree")
	csprojFile := commandLine.String("csproj", "", "Path to custom .csproj template file")

	err := commandLine.Parse(os.Args[1:])
	inputFilePath := strings.TrimSpace(commandLine.Arg(0))

	if err != nil || inputFilePath == "" {
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %s\n", err)
		}

		fmt.Fprintln(os.Stderr, `
File usage: go2cs [options] <input.go> [output.cs]
 Dir usage: go2cs [options] <input_dir> [output_dir]
 
 Options:`)

		commandLine.SetOutput(nil)
		commandLine.PrintDefaults()

		fmt.Fprintln(os.Stderr, `
Examples:
  go2cs -indent 2 -var=false example.go conv/example.cs
  go2cs example.go
  go2cs -cgo=true input_dir output_dir
  go2cs package_dir
 `)
		os.Exit(1)
	}

	options := Options{
		indentSpaces:        *indentSpaces,
		preferVarDecl:       *preferVarDecl,
		useChannelOperators: *useChannelOperators,
		includeComments:     *includeComments,
		parseCgoTargets:     *parseCgoTargets,
		showParseTree:       *showParseTree,
	}

	// Load custom .csproj template if specified
	if *csprojFile != "" {
		var err error
		csprojTemplate, err = os.ReadFile(*csprojFile)

		if err != nil {
			log.Fatalf("Failed to read custom .csproj template file \"%s\": %s\n", *csprojFile, err)
		}
	}

	fset := token.NewFileSet()
	files := []FileEntry{}

	// Check if the input is a file or a directory
	fileInfo, err := os.Stat(inputFilePath)

	if err != nil {
		log.Fatalf("Failed to access input file path \"%s\": %s\n", inputFilePath, err)
	}

	var parseMode parser.Mode

	if options.includeComments {
		parseMode = parser.ParseComments | parser.SkipObjectResolution
	} else {
		parseMode = parser.SkipObjectResolution
	}

	outputFilePath := ""

	// If the user has provided a second argument, we will use it as the output directory or file
	if commandLine.NArg() > 1 {
		outputFilePath = strings.TrimSpace(commandLine.Arg(1))
	} else {
		outputFilePath = inputFilePath
	}

	var projectFileName string

	if fileInfo.IsDir() {
		// If the input is a directory, write project files (if needed)
		if projectFileName, err = writeProjectFiles(filepath.Base(inputFilePath), outputFilePath); err != nil {
			log.Fatalf("Failed to write project files for directory \"%s\": %s\n", outputFilePath, err)
		} else {
			// Parse all .go files in the directory
			err := filepath.Walk(inputFilePath, func(path string, info os.FileInfo, err error) error {
				if err != nil {
					return err
				}

				if !info.IsDir() && strings.HasSuffix(info.Name(), ".go") {
					file, err := parser.ParseFile(fset, path, nil, parseMode)

					if err != nil {
						return fmt.Errorf("failed to parse input source file \"%s\": %s", path, err)
					}

					files = append(files, FileEntry{file, path})
				}

				return nil
			})

			if err != nil {
				log.Fatalf("Failed to parse files in directory \"%s\": %s\n", inputFilePath, err)
			}
		}
	} else {
		// If the input is a single file, parse it
		if !strings.HasSuffix(inputFilePath, ".go") {
			log.Fatalln("Invalid file extension for input source file: please provide a .go file as first argument")
		}

		file, err := parser.ParseFile(fset, inputFilePath, nil, parseMode)

		if err != nil {
			log.Fatalf("Failed to parse input source file \"%s\": %s\n", inputFilePath, err)
		}

		files = append(files, FileEntry{file, inputFilePath})
	}

	conf := types.Config{Importer: importer.Default()}

	info := &types.Info{
		Types: make(map[ast.Expr]types.TypeAndValue),
		Defs:  make(map[*ast.Ident]types.Object),
		Uses:  make(map[*ast.Ident]types.Object),
	}

	extractFiles := func(files []FileEntry) []*ast.File {
		result := make([]*ast.File, len(files))

		for i, fileEntry := range files {
			result[i] = fileEntry.file
		}

		return result
	}

	pkg, err := conf.Check(".", fset, extractFiles(files), info)

	if err != nil {
		log.Fatalf("Failed to parse types from input source files: %s\n", err)
	}

	// Once we have the package details, we can determine the assembly output type
	outputType := getAssemblyOutputType(pkg)

	// Update project file with correct output type
	if len(projectFileName) > 0 {
		projectContents, err := os.ReadFile(projectFileName)

		if err != nil {
			log.Fatalf("Failed to read project file %q: %s", projectFileName, err)
		}

		// Replace the output type marker with the actual output type
		newContents := []byte(strings.ReplaceAll(string(projectContents), OutputTypeMarker, outputType))

		// Rewrite project file atomically
		err = os.WriteFile(projectFileName, newContents, 0644)

		if err != nil {
			log.Fatalf("Failed to write project file %q: %s", projectFileName, err)
		}

		// For executable projects, write OS-specific publish profiles
		if outputType == "Exe" {
			err = writePublishProfiles(outputFilePath)

			if err != nil {
				log.Fatalf("Failed to write publish profiles for project \"%s\": %s\n", outputFilePath, err)
			}
		}

		// For library projects, write package files, like icon
		if outputType == "Library" {
			err = writePackageFiles(outputFilePath)

			if err != nil {
				log.Fatalf("Failed to write package files for project \"%s\": %s\n", outputFilePath, err)
			}
		}
	}

	globalIdentNames := make(map[*ast.Ident]string)
	globalScope := map[string]*types.Var{}

	// Pre-process all global variables in package
	for _, fileEntry := range files {
		performGlobalVariableAnalysis(fileEntry.file.Decls, info, globalIdentNames, globalScope)

		if options.showParseTree {
			ast.Fprint(os.Stdout, fset, fileEntry.file, nil)
		}
	}

	var concurrentTasks sync.WaitGroup

	for _, fileEntry := range files {
		concurrentTasks.Add(1)

		go func(fileEntry FileEntry) {
			defer concurrentTasks.Done()

			visitor := &Visitor{
				fset:               fset,
				pkg:                pkg,
				info:               info,
				targetFile:         &strings.Builder{},
				packageImports:     &strings.Builder{},
				requiredUsings:     HashSet[string]{},
				importQueue:        HashSet[string]{},
				standAloneComments: map[token.Pos]string{},
				sortedCommentPos:   []token.Pos{},
				processedComments:  HashSet[token.Pos]{},
				newline:            "\r\n",
				options:            options,
				globalIdentNames:   globalIdentNames,
				globalScope:        globalScope,
				blocks:             Stack[*strings.Builder]{},
				identEscapesHeap:   map[*ast.Ident]bool{},
			}

			visitor.visitFile(fileEntry.file)

			var outputFileName string

			if fileInfo.IsDir() {
				outputFileName = filepath.Join(outputFilePath, strings.TrimSuffix(filepath.Base(fileEntry.filePath), ".go")+".cs")
			} else {
				outputFileName = strings.TrimSuffix(outputFilePath, ".go") + ".cs"
			}

			if err := visitor.writeOutputFile(outputFileName); err != nil {
				log.Printf("%s\n", err)
			}
		}(fileEntry)
	}

	concurrentTasks.Wait()
}

func writeProjectFiles(projectName string, projectPath string) (string, error) {
	// Make sure project path ends with a directory separator
	projectPath = strings.TrimRight(projectPath, string(filepath.Separator)) + string(filepath.Separator)

	iconFileName := projectPath + "go2cs.ico"

	// Check if icon file needs to be written
	if needToWriteFile(iconFileName, iconFileBytes) {
		iconFile, err := os.Create(iconFileName)

		if err != nil {
			return "", fmt.Errorf("failed to create icon file \"%s\": %s", iconFileName, err)
		}

		defer iconFile.Close()

		_, err = iconFile.Write(iconFileBytes)

		if err != nil {
			return "", fmt.Errorf("failed to write to icon file \"%s\": %s", iconFileName, err)
		}
	}

	// TODO: Need to know which projects to reference based on package imports

	// Generate project file contents
	projectFileContents := fmt.Sprintf(string(csprojTemplate),
		OutputTypeMarker,
		projectName,
		time.Now().Year())

	projectFileName := projectPath + projectName + ".csproj"

	// Check if project file needs to be written
	if needToWriteFile(projectFileName, []byte(projectFileContents)) {
		projectFile, err := os.Create(projectFileName)

		if err != nil {
			return "", fmt.Errorf("failed to create project file \"%s\": %s", projectFileName, err)
		}

		_, err = projectFile.WriteString(projectFileContents)

		if err != nil {
			return "", fmt.Errorf("failed to write to project file \"%s\": %s", projectFileName, err)
		}

		defer projectFile.Close()
	}

	return projectFileName, nil
}

func writePackageFiles(projectPath string) error {
	// Make sure project path ends with a directory separator
	projectPath = strings.TrimRight(projectPath, string(filepath.Separator)) + string(filepath.Separator)

	pngFileName := projectPath + "go2cs.png"

	// Check if icon file needs to be written
	if needToWriteFile(pngFileName, pngFileBytes) {
		iconFile, err := os.Create(pngFileName)

		if err != nil {
			return fmt.Errorf("failed to create package icon file \"%s\": %s", pngFileName, err)
		}

		defer iconFile.Close()

		_, err = iconFile.Write(pngFileBytes)

		if err != nil {
			return fmt.Errorf("failed to write to package icon file \"%s\": %s", pngFileName, err)
		}
	}

	return nil
}

func writePublishProfiles(projectPath string) error {
	// Make sure "Properties/PublishProfiles" directory exists
	publishProfilesDir := filepath.Join(projectPath, "Properties", "PublishProfiles")

	if err := os.MkdirAll(publishProfilesDir, 0755); err != nil {
		return fmt.Errorf("failed to create directory \"%s\": %s", publishProfilesDir, err)
	}

	// Get list of publish profiles
	profiles, err := publishProfiles.ReadDir("profiles")

	if err != nil {
		return fmt.Errorf("failed to read publish profiles: %s", err)
	}

	// Write each publish profile file
	for _, profile := range profiles {
		profileBytes, err := publishProfiles.ReadFile(path.Join("profiles", profile.Name()))

		if err != nil {
			return fmt.Errorf("failed to read publish profile \"%s\": %s", profile.Name(), err)
		}

		profileFileName := filepath.Join(publishProfilesDir, profile.Name())

		// Check if profile file already exists - user may change default parameters, so we don't overwrite
		if _, err := os.Stat(profileFileName); err == nil {
			continue
		}

		profileFile, err := os.Create(profileFileName)

		if err != nil {
			return fmt.Errorf("failed to create publish profile \"%s\": %s", profileFileName, err)
		}

		defer profileFile.Close()

		_, err = profileFile.Write(profileBytes)

		if err != nil {
			return fmt.Errorf("failed to write to publish profile \"%s\": %s", profileFileName, err)
		}
	}

	return nil
}

func needToWriteFile(fileName string, fileBytes []byte) bool {
	existingFileBytes, err := os.ReadFile(fileName)

	if err != nil {
		return true
	}

	return !bytes.Equal(existingFileBytes, fileBytes)
}

func (v *Visitor) writeOutputFile(outputFileName string) error {
	outputFile, err := os.Create(outputFileName)

	if err != nil {
		return fmt.Errorf("failed to create output source file \"%s\": %s", outputFileName, err)
	}

	defer outputFile.Close()

	_, err = outputFile.WriteString(v.targetFile.String())

	if err != nil {
		return fmt.Errorf("failed to write to output source file \"%s\": %s", outputFileName, err)
	}

	return nil
}

func getAssemblyOutputType(pkg *types.Package) string {
	if hasMainFunction(pkg) {
		return "Exe"
	}

	return "Library"
}

func hasMainFunction(pkg *types.Package) bool {
	// First check if this is a main package
	if pkg.Name() != "main" {
		return false
	}

	// Look through all objects in the package scope
	scope := pkg.Scope()
	mainObj := scope.Lookup("main")

	if mainObj == nil {
		return false
	}

	// Check if it's a function
	mainFunc, ok := mainObj.(*types.Func)

	if !ok {
		return false
	}

	// Get the function's type
	funcType, ok := mainFunc.Type().(*types.Signature)

	if !ok {
		return false
	}

	// main function should have no parameters and no return values
	return funcType.Params().Len() == 0 && funcType.Results().Len() == 0
}

func (v *Visitor) addRequiredUsing(usingName string) {
	v.requiredUsings.Add(usingName)
}

func (v *Visitor) getPrintedNode(node ast.Node) string {
	result := &strings.Builder{}
	printer.Fprint(result, v.fset, node)
	return result.String()
}

func (v *Visitor) getStringLiteral(str string) (result string, isRawStr bool) {
	// Convert Go raw string literal to C# raw string literal
	if strings.HasPrefix(str, "`") {
		// Remove backticks from the start and end of the string
		str = strings.Trim(str, "`")

		// See if raw string literal is required (contains newline)
		if strings.Contains(str, "\n") {
			// C# raw string literals are enclosed in triple (or more) quotes
			prefix := `"""`
			suffix := `"""`

			// Keep adding quotes until the source string does not contain the
			// prefix to create a unique C# raw string literal token
			for while := strings.Contains(str, prefix); while; {
				prefix += `"`
				suffix += `"`
				while = strings.Contains(str, prefix)
			}

			// Ensure multiline C# raw string literals starts with newline
			if !strings.HasPrefix(str, "\n") {
				prefix += v.newline
			}

			// Ensure multiline C# raw string literals ends with newline
			if !strings.HasSuffix(str[:len(str)-1], "\n") {
				// Get index of last newline
				lastNewline := strings.LastIndex(str, "\n")

				// Check if any characters beyond the last newline are not whitespace
				if strings.TrimSpace(str[lastNewline:]) != "" {
					suffix = v.newline + suffix
				}
			}

			return prefix + str + suffix, true
		}

		// Use C# verbatim string literal for more simple raw strings
		return fmt.Sprintf("@\"%s\"", strings.ReplaceAll(str, "\"", "\"\"")), true
	}

	return str, false
}

func (v *Visitor) isNonCallValue(expr ast.Expr) bool {
	_, isCallExpr := expr.(*ast.CallExpr)

	return v.info.Types[expr].IsValue() && !isCallExpr
}

func getSanitizedIdentifier(identifier string) string {
	if strings.HasPrefix(identifier, "@") {
		return identifier // Already sanitized
	}

	if keywords.Contains(identifier) ||
		strings.HasPrefix(identifier, AddressPrefix) ||
		strings.HasSuffix(identifier, PackageSuffix) {
		return "@" + identifier
	}

	return identifier
}

func getSanitizedFunctionName(funcName string) string {
	funcName = getSanitizedIdentifier(funcName)

	// Handle special exceptions
	if funcName == "Main" {
		// C# "Main" method name is reserved, so we need to
		// shadow it if Go code has a function named "Main"
		return ShadowVarMarker + "Main"
	}

	return funcName
}

func getAccess(name string) string {
	// If name starts with a lowercase letter, scope is "private"
	ch, _ := utf8.DecodeRuneInString(name)

	if unicode.IsLower(ch) {
		return "private"
	}

	// Otherwise, scope is "public"
	return "public"
}

func isDiscardedVar(varName string) bool {
	return len(varName) == 0 || varName == "_"
}

func isLogicalOperator(op token.Token) bool {
	switch op {
	case token.LAND, token.LOR:
		return true
	default:
		return false
	}
}

func isComparisonOperator(op token.Token) bool {
	switch op {
	case token.EQL, token.NEQ, token.LSS, token.LEQ, token.GTR, token.GEQ:
		return true
	default:
		return false
	}
}

func (v *Visitor) isInterface(ident *ast.Ident) (result bool, empty bool) {
	obj := v.info.ObjectOf(ident)

	if obj == nil {
		return false, false
	}

	return isInterface(obj.Type())
}

func isInterface(t types.Type) (result bool, empty bool) {
	exprType := t.Underlying()

	if interfaceType, ok := exprType.(*types.Interface); ok {
		// Empty interface has zero methods
		return true, interfaceType.NumMethods() == 0
	}

	return false, false
}

func (v *Visitor) isPointer(ident *ast.Ident) bool {
	obj := v.info.ObjectOf(ident)

	if obj == nil {
		return false
	}

	return isPointer(obj.Type())
}

func isPointer(t types.Type) bool {
	exprType := t.Underlying()

	_, isPointer := exprType.(*types.Pointer)

	return isPointer
}

func paramsAreInterfaces(paramTypes *types.Tuple, andNotEmptyInterface bool) []bool {
	if paramTypes == nil {
		return nil
	}

	paramIsInterface := make([]bool, paramTypes.Len())

	for i := 0; i < paramTypes.Len(); i++ {
		param := paramTypes.At(i)
		paramType := param.Type()
		isInterface, isEmpty := isInterface(paramType)

		if andNotEmptyInterface {
			paramIsInterface[i] = isInterface && !isEmpty
		} else {
			paramIsInterface[i] = isInterface
		}
	}

	return paramIsInterface
}

func paramsArePointers(paramTypes *types.Tuple) []bool {
	if paramTypes == nil {
		return nil
	}

	paramIsPointer := make([]bool, paramTypes.Len())

	for i := 0; i < paramTypes.Len(); i++ {
		param := paramTypes.At(i)
		paramIsPointer[i] = isPointer(param.Type())
	}

	return paramIsPointer
}

func (v *Visitor) convertToInterfaceType(interfaceExpr ast.Expr, targetExpr string) string {
	return convertToInterfaceType(v.getType(interfaceExpr, false), targetExpr)
}

func convertToInterfaceType(interfaceType types.Type, targetExpr string) string {
	result := &strings.Builder{}

	// Convert to interface type using Go converted interface ".As" method,
	// this handles duck typed Go interface implementations
	result.WriteString(convertToCSTypeName(getTypeName(interfaceType)))
	result.WriteString(".As(")
	result.WriteString(targetExpr)
	result.WriteRune(')')

	return result.String()
}

func getIdentifier(node ast.Node) *ast.Ident {
	var ident *ast.Ident

	if indexExpr, ok := node.(*ast.IndexExpr); ok {
		if identExpr, ok := indexExpr.X.(*ast.Ident); ok {
			ident = identExpr
		}
	} else if starExpr, ok := node.(*ast.StarExpr); ok {
		ident = getIdentifier(starExpr.X)
	} else if identExpr, ok := node.(*ast.Ident); ok {
		ident = identExpr
	} else if chanExpr, ok := node.(*ast.ChanType); ok {
		ident = getIdentifier(chanExpr.Value)
	}

	return ident
}

func (v *Visitor) getType(expr ast.Expr, underlying bool) types.Type {
	exprType := v.info.TypeOf(expr)

	if exprType == nil {
		return nil
	}

	if underlying {
		return exprType.Underlying()
	}

	return exprType
}

func (v *Visitor) getTypeName(expr ast.Expr, underlying bool) string {
	return getTypeName(v.getType(expr, underlying))
}

func getTypeName(t types.Type) string {
	if named, ok := t.(*types.Named); ok {
		return named.Obj().Name()
	}

	return strings.ReplaceAll(t.String(), "..", "")
}

func getCSTypeName(t types.Type) string {
	return convertToCSTypeName(getTypeName(t))
}

func convertToCSTypeName(typeName string) string {
	fullTypeName := convertToCSFullTypeName(typeName)

	// If full type name starts with root namespace, remove it
	if strings.HasPrefix(fullTypeName, RootNamespace+".") {
		return fullTypeName[len(RootNamespace)+1:]
	}

	return fullTypeName
}

func convertToCSFullTypeName(typeName string) string {
	typeName = strings.TrimPrefix(typeName, "untyped ")

	if strings.HasPrefix(typeName, "[]") {
		return fmt.Sprintf("%s.slice<%s>", RootNamespace, convertToCSTypeName(typeName[2:]))
	}

	// Handle array types
	if strings.HasPrefix(typeName, "[") {
		return fmt.Sprintf("%s.array<%s>", RootNamespace, convertToCSTypeName(typeName[strings.Index(typeName, "]")+1:]))
	}

	if strings.HasPrefix(typeName, "map[") {
		keyValue := strings.Split(typeName[4:], "]")
		return fmt.Sprintf("%s.map<%s, %s>", RootNamespace, convertToCSTypeName(keyValue[0]), convertToCSTypeName(keyValue[1]))
	}

	if strings.HasPrefix(typeName, "chan ") {
		return fmt.Sprintf("%s.channel<%s>", RootNamespace, convertToCSTypeName(typeName[5:]))
	}

	if strings.HasPrefix(typeName, "chan<- ") {
		return fmt.Sprintf("%s.channel/*<-*/<%s>", RootNamespace, convertToCSTypeName(typeName[7:]))
	}

	if strings.HasPrefix(typeName, "<-chan ") {
		return fmt.Sprintf("%s./*<-*/channel<%s>", RootNamespace, convertToCSTypeName(typeName[7:]))
	}

	if typeName == "func()" {
		return "Action"
	}

	if strings.HasPrefix(typeName, "func(") {
		// Find the matching closing parenthesis for the parameter list
		depth := 0
		closingParenIndex := -1

		for i := 5; i < len(typeName); i++ {
			if typeName[i] == '(' {
				depth++
			} else if typeName[i] == ')' {
				depth--
				if depth == -1 {
					closingParenIndex = i
					break
				}
			}
		}

		if closingParenIndex == -1 {
			return "Action" // Malformed input (unexpected)
		}

		// Extract parameter types, handling nested functions
		paramString := typeName[5:closingParenIndex]
		paramTypes := extractTypes(paramString)

		// Convert parameter types to C#
		csTypeNames := make([]string, len(paramTypes))

		for i, pType := range paramTypes {
			csTypeNames[i] = convertToCSTypeName(pType)
		}

		// Check for return type after the closing parenthesis
		remainingType := strings.TrimSpace(typeName[closingParenIndex+1:])

		if len(remainingType) > 0 {
			// Has explicit return type
			csReturnType := convertToCSTypeName(remainingType)

			if len(csTypeNames) > 0 {
				return fmt.Sprintf("Func<%s, %s>", strings.Join(csTypeNames, ", "), csReturnType)
			}

			return fmt.Sprintf("Func<%s>", csReturnType)
		}

		// No return type, use Action
		if len(csTypeNames) > 0 {
			return fmt.Sprintf("Action<%s>", strings.Join(csTypeNames, ", "))
		}

		return "Action"
	}

	// Handle pointer types
	if strings.HasPrefix(typeName, "*") {
		return fmt.Sprintf("%s.ptr<%s>", RootNamespace, convertToCSTypeName(typeName[1:]))
	}

	switch typeName {
	case "int":
		return "nint"
	case "uint":
		return "nuint"
	case "bool":
		return "bool"
	case "byte":
		return "byte"
	case "float":
		return "float64"
	case "complex64":
		return RootNamespace + ".complex64"
	case "string":
		return RootNamespace + ".@string"
	case "interface{}":
		return "object"
	default:
		return getSanitizedIdentifier(typeName)
	}
}

func extractTypes(signature string) []string {
	// Remove any whitespace at the ends
	signature = strings.TrimSpace(signature)

	// Handle empty signature
	if signature == "" {
		return []string{}
	}

	// Split the signature into individual parameter declarations
	params := strings.Split(signature, ",")
	types := make([]string, 0, len(params))

	for _, param := range params {
		// Trim whitespace
		param = strings.TrimSpace(param)

		// Find the first space or end of string
		var typeStart int

		for i, char := range param {
			if unicode.IsSpace(char) {
				typeStart = i
				break
			}
		}

		// If no space found, the entire param is a type (e.g., "string")
		if typeStart == 0 {
			types = append(types, param)
		} else {
			// Extract everything after the space
			paramType := convertToCSTypeName(strings.TrimSpace(param[typeStart:]))
			types = append(types, paramType)
		}
	}

	return types
}

func (v *Visitor) convertToHeapTypeDecl(ident *ast.Ident, createNew bool) string {
	escapesHeap := v.identEscapesHeap[ident]
	identType := v.info.TypeOf(ident)

	if !escapesHeap || isInherentlyHeapAllocatedType(identType) {
		return ""
	}

	goTypeName := getTypeName(identType)
	csIDName := v.getIdentName(ident)

	// Handle array types
	if strings.HasPrefix(goTypeName, "[") {
		arrayLen := strings.Split(goTypeName[1:], "]")[0]

		// Get array element type
		arrayType := convertToCSTypeName(goTypeName[strings.Index(goTypeName, "]")+1:])

		if v.options.preferVarDecl {
			if createNew {
				return fmt.Sprintf("ref var %s = ref heap(new array<%s>(%s), out var %s%s);", csIDName, arrayType, arrayLen, AddressPrefix, csIDName)
			}

			return fmt.Sprintf("ref var %s = ref heap<array<%s>>(out var %s%s);", csIDName, arrayType, AddressPrefix, csIDName)
		}

		if createNew {
			return fmt.Sprintf("ref array<%s> %s = ref heap(new array<%s>(%s), out ptr<array<%s>> %s%s);", arrayType, csIDName, arrayType, arrayLen, arrayType, AddressPrefix, csIDName)
		}

		return fmt.Sprintf("ref array<%s> %s = ref heap<array<%s>>(out %s%s);", arrayType, csIDName, arrayType, AddressPrefix, csIDName)
	}

	csTypeName := convertToCSTypeName(goTypeName)

	if v.options.preferVarDecl {
		if createNew {
			return fmt.Sprintf("ref var %s = ref heap(new %s(), out var %s%s);", csIDName, csTypeName, AddressPrefix, csIDName)
		}

		return fmt.Sprintf("ref var %s = ref heap<%s>(out var %s%s);", csIDName, csTypeName, AddressPrefix, csIDName)
	}

	if createNew {
		return fmt.Sprintf("ref %s %s = ref heap(out ptr<%s> %s%s);", csTypeName, csIDName, csTypeName, AddressPrefix, csIDName)
	}

	return fmt.Sprintf("ref %s %s = ref heap<%s>(out %s%s);", csTypeName, csIDName, csTypeName, AddressPrefix, csIDName)
}

func isInherentlyHeapAllocatedType(typ types.Type) bool {
	switch typ.Underlying().(type) {
	case *types.Map, *types.Slice, *types.Chan, *types.Interface, *types.Signature, *types.Pointer:
		// Maps, slices, channels, interfaces, functions and pointers are reference types
		return true
	default:
		return false
	}
}

func getParameterType(sig *types.Signature, i int) (types.Type, bool) {
	var paramType types.Type
	params := sig.Params()

	// Check variadic parameter type
	if sig.Variadic() && i >= params.Len()-1 {
		paramType = params.At(params.Len() - 1).Type()

		if sliceType, ok := paramType.(*types.Slice); ok {
			paramType = sliceType.Elem()
		}
	} else if i < params.Len() {
		paramType = params.At(i).Type()
	} else {
		return nil, false
	}

	return paramType, true
}

func (v *Visitor) getVarIdent(varType *types.Var) *ast.Ident {
	for ident, obj := range v.info.Defs {
		if obj == varType {
			return ident
		}
	}

	return nil
}

func (v *Visitor) getExprType(expr ast.Expr) types.Type {
	return v.info.TypeOf(expr)
}

// Get the adjusted identifier name, considering captures and shadowing
func (v *Visitor) getIdentName(ident *ast.Ident) string {
	// Check if we're in a lambda conversion
	if v.lambdaCapture != nil && v.lambdaCapture.conversionInLambda {
		// First check if we already have a mapping for this variable in this lambda
		if captureName, ok := v.lambdaCapture.currentLambdaVars[ident.Name]; ok {
			return captureName
		}

		// Then check if it needs to be captured
		if captureInfo, ok := v.lambdaCapture.capturedVars[ident]; ok {
			captureInfo.used = true

			// Store the mapping for this lambda
			v.lambdaCapture.currentLambdaVars[ident.Name] = captureInfo.copyIdent.Name

			return captureInfo.copyIdent.Name
		}
	}

	// Fall back to existing shadowing logic
	if v.identNames != nil {
		if name, ok := v.identNames[ident]; ok {
			return name
		}
	}

	if v.globalIdentNames != nil {
		if name, ok := v.globalIdentNames[ident]; ok {
			return name
		}
	}

	return ident.Name
}

// Determine if the identifier represents a reassignment
func (v *Visitor) isReassignment(ident *ast.Ident) bool {
	return v.isReassigned[ident]
}
