package main

import (
	"fmt"
	"go/ast"
	"go/importer"
	"go/parser"
	"go/token"
	"go/types"
	"log"
	"os"
	"strings"
	"unicode"

	. "go2cs/hashset"
	. "go2cs/stack"
)

type Options struct {
	indentSpaces    int
	parseCgoTargets bool
	showParseTree   bool
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

	// ImportSpec variables
	currentImportPath string
	packageImports    *strings.Builder
	importQueue       HashSet[string]
	requiredUsings    HashSet[string]

	// FuncDecl variables
	inFunction      bool
	currentFunction *types.Func
	hasDefer        bool
	hasPanic        bool
	hasRecover      bool

	// BlockStmt variables
	blocks                    Stack[*strings.Builder]
	blockInnerPrefixInjection Stack[string]
	blockInnerSuffixInjection Stack[string]
	blockOuterPrefixInjection Stack[string]
	blockOuterSuffixInjection Stack[string]
	firstStatementIsReturn    bool
}

const RootNamespace = "go"
const ClassSuffix = "_package"
const AddressPrefix = "_addr_"

var keywords = NewHashSet[string]([]string{
	// The following are all valid C# keywords, if encountered in Go code they should be escaped
	"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
	"continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
	"false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
	"internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
	"params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
	"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
	"uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
	"__argslist", "__makeref", "__reftype", "__refvalue",
	// The following C# type names are reserved by go2cs as they may be used during code conversion
	"GoType", "GoUntyped", "GoTag",
	// The following symbols are reserved by go2cs as they are publically defined in "golib"
	"WithOK", "WithErr", "WithVal", "InitKeyedValues", "GetGoTypeName", "CastCopy", "ConvertToType",
})

/*
   Current expected C# global project aliases:

   <Using Include="go.builtin" Static="True" />
   <Using Include="System.Byte" Alias="uint8" />
   <Using Include="System.UInt16" Alias="uint16" />
   <Using Include="System.UInt32" Alias="uint32" />
   <Using Include="System.UInt64" Alias="uint64" />
   <Using Include="System.SByte" Alias="int8" />
   <Using Include="System.Int16" Alias="int16" />
   <Using Include="System.Int32" Alias="int32" />
   <Using Include="System.Int64" Alias="int64" />
   <Using Include="System.Single" Alias="float32" />
   <Using Include="System.Double" Alias="float64" />
   <Using Include="System.Numerics.Complex" Alias="complex128" />
   <Using Include="System.Int32" Alias="rune" />
   <Using Include="System.UIntPtr" Alias="uintptr" />
   <Using Include="System.Numerics.BigInteger" Alias="GoUntyped" />
   <Using Include="System.ComponentModel.DescriptionAttribute" Alias="GoTag" />

*/

func main() {
	if len(os.Args) < 2 {
		log.Fatalln("Usage: go run main.go <input.go> [output.cs]")
	}

	inputFileName := strings.TrimSpace(os.Args[1])

	// Check if the file has a ".go" extension
	if len(inputFileName) < 3 || inputFileName[len(inputFileName)-3:] != ".go" {
		log.Fatalln("Invalid file extension for input source file: please provide a .go file as first argument")
	}

	// TODO: Load options from command line arguments
	options := Options{
		indentSpaces:    4,
		parseCgoTargets: false,
		showParseTree:   true,
	}

	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, inputFileName, nil, parser.ParseComments|parser.SkipObjectResolution)

	if err != nil {
		log.Fatalf("Failed to parse input source file \"%s\": %s\n", inputFileName, err)
	}

	if options.showParseTree {
		ast.Fprint(os.Stdout, fset, file, nil)
	}

	conf := types.Config{Importer: importer.Default()}

	info := &types.Info{
		Types: make(map[ast.Expr]types.TypeAndValue),
		Defs:  make(map[*ast.Ident]types.Object),
		Uses:  make(map[*ast.Ident]types.Object),
	}

	pkg, err := conf.Check(".", fset, []*ast.File{file}, info)
	if err != nil {
		log.Fatalf("Failed to parse types from input source file \"%s\": %s\n", inputFileName, err)
	}

	var outputFileName string

	if len(os.Args) > 2 {
		// If the user has provided a second argument, we will use it as the output file
		outputFileName = strings.TrimSpace(os.Args[2])
	} else {
		// Otherwise, output file will replace ".go" with ".cs"
		outputFileName = inputFileName[:len(inputFileName)-3] + ".cs"
	}

	outputFile, err := os.Create(outputFileName)

	if err != nil {
		log.Fatalf("Failed to create output source file \"%s\": %s\n", outputFileName, err)
	}

	defer outputFile.Close()

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

		// BlockStmt variable initializations
		blocks:                    Stack[*strings.Builder]{},
		blockInnerPrefixInjection: Stack[string]{},
		blockInnerSuffixInjection: Stack[string]{},
		blockOuterPrefixInjection: Stack[string]{},
		blockOuterSuffixInjection: Stack[string]{},
	}

	visitor.enterFile(file)
	visitor.exitFile(file)

	outputFile.WriteString(visitor.targetFile.String())
}

func (v *Visitor) indent(indentLevel int) string {
	return strings.Repeat(" ", v.options.indentSpaces*indentLevel)
}

func (v *Visitor) writeString(builder *strings.Builder, format string, a ...interface{}) {
	if v.indentLevel > 0 {
		builder.WriteString(v.indent(v.indentLevel))
	}

	builder.WriteString(fmt.Sprintf(format, a...))
}

func (v *Visitor) writeStringLn(builder *strings.Builder, format string, a ...interface{}) {
	v.writeString(builder, format, a...)
	builder.WriteString(v.newline)
}

func (v *Visitor) writeDocString(builder *strings.Builder, doc *ast.CommentGroup, targetPos token.Pos) {
	// Handle standalone comments that may precede the target position
	if targetPos != token.NoPos {
		if v.file == nil {
			v.file = v.fset.File(targetPos)
		}

		handledPos := []token.Pos{}

		for _, pos := range v.sortedCommentPos {
			if pos > targetPos {
				break
			}

			comment, found := v.standAloneComments[pos]

			if !found {
				continue
			}

			builder.WriteString(comment)
			builder.WriteString(v.newline)

			delete(v.standAloneComments, pos)
			handledPos = append(handledPos, pos)
		}

		if len(handledPos) > 0 {
			lastCommentPos := handledPos[len(handledPos)-1]

			// Add line breaks if there is a gap between the last comment and the target position
			if lastCommentPos > token.NoPos && doc != nil {
				docPos := doc.Pos()
				targetLine := v.file.Line(lastCommentPos)
				nodeLine := v.file.Line(docPos)

				if int(nodeLine-targetLine)-1 > 0 {
					builder.WriteString(v.newline)
				}
			}

			removePos := func(slice []token.Pos, pos token.Pos) []token.Pos {
				for i, v := range slice {
					if v == pos {
						return append(slice[:i], slice[i+1:]...)
					}
				}
				return slice
			}

			// Remove handled positions from sorted list
			for _, pos := range handledPos {
				v.sortedCommentPos = removePos(v.sortedCommentPos, pos)
			}
		}
	}

	if doc == nil {
		return
	}

	// Handle doc comments
	v.writeString(builder, "") // Write indent
	v.writeCommentString(builder, doc, token.NoPos)
	builder.WriteString(v.newline)
}

func (v *Visitor) writeCommentString(builder *strings.Builder, comment *ast.CommentGroup, targetPos token.Pos) {
	if comment == nil {
		return
	}

	if !v.processedComments.Add(comment.Pos()) {
		return
	}

	for index, comment := range comment.List {
		if index > 0 {
			builder.WriteString(v.newline)
		}

		if targetPos > token.NoPos {
			padding := int(comment.Slash - targetPos)

			if padding < 1 {
				padding = 1
			}

			builder.WriteString(strings.Repeat(" ", padding))
		}

		builder.WriteString(comment.Text)
	}
}

func (v *Visitor) replaceMarkerString(builder *strings.Builder, marker string, replacement string) {
	builderString := strings.ReplaceAll(builder.String(), marker, replacement)
	builder.Reset()
	builder.WriteString(builderString)
}

func (v *Visitor) writeOutput(format string, a ...interface{}) {
	v.writeString(v.targetFile, format, a...)
}

func (v *Visitor) writeOutputLn(format string, a ...interface{}) {
	v.writeStringLn(v.targetFile, format, a...)
}

func (v *Visitor) writeDoc(doc *ast.CommentGroup, targetPos token.Pos) {
	v.writeDocString(v.targetFile, doc, targetPos)
}

func (v *Visitor) writeComment(comment *ast.CommentGroup, targetPos token.Pos) {
	v.writeCommentString(v.targetFile, comment, targetPos)
}

func (v *Visitor) replaceMarker(marker string, replacement string) {
	v.replaceMarkerString(v.targetFile, marker, replacement)
}

func (v *Visitor) addRequiredUsing(usingName string) {
	v.requiredUsings.Add(usingName)
}

func convertToCSTypeName(typeName string) string {
	fullTypeName := convertToCSFullTypeName(typeName)

	// If full type name starts with root namespace, remove it
	if strings.HasPrefix(fullTypeName, RootNamespace+".") {
		return fullTypeName[3:]
	}

	return fullTypeName
}

func convertToCSFullTypeName(typeName string) string {
	if strings.HasPrefix(typeName, "untyped ") {
		typeName = typeName[8:]
	}

	switch typeName {
	case "int":
		return "nint"
	case "float":
		return "float64"
	case "complex64":
		return "go.complex64"
	case "string":
		return "go.@string"
	default:
		return typeName
	}
}

func getTypeName(t types.Type) string {
	if named, ok := t.(*types.Named); ok {
		return named.Obj().Name()
	}

	return t.String()
}

func getCSTypeName(t types.Type) string {
	return convertToCSTypeName(getTypeName(t))
}

func getAccess(name string) string {
	// If name starts with a lowercase letter, scope is "private"
	if unicode.IsLower([]rune(name)[0]) {
		return "private"
	}

	// Otherwise, scope is "public"
	return "public"
}

func getSanitizedIdentifier(identifier string) string {
	if keywords.Contains(identifier) {
		return "@" + identifier
	}

	return identifier
}

func getStringLiteral(decl *ast.BasicLit) string {
	if decl.Kind != token.STRING {
		return ""
	}

	name := decl.Value

	if []rune(name)[0] != '`' {
		return name
	}

	// Trim the back-ticks from the string literal
	return "@\"" + strings.Trim(decl.Value, "`") + "\""
}