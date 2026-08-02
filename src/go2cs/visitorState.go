// visitorState.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the Visitor and the per-file state it carries.
//
// A Visitor is created per Go source file and walks that file's AST, appending C# to an output
// builder. Nearly every conv*/visit* file in the converter is a method on this type, so its field
// set is effectively the converter's working memory: what is being emitted, what has been
// discovered about the current function, and what the analysis passes recorded earlier.
//
// Only PER-FILE state lives here. State shared across a whole package — the registries the
// concurrently-visited files publish to each other — lives in packageGlobalState.go, whose
// lifecycle is owned by packageStateOperations.go.

package main

import (
	"go/ast"
	"go/token"
	"go/types"
	"strings"
)

type FileEntry struct {
	file             *ast.File
	filePath         string
	identEscapesHeap map[types.Object]bool

	// sstringEligible flags a `s := string(x)` / `var s = string(x)` string LOCAL that the escape
	// pass has proven may be emitted as a stack-only `sstring` (a zero-copy view over x's bytes)
	// instead of the heap `@string` — non-escaping, not returned, used only through safe reads, and
	// with no write to the conversion's source for the lifetime of the view. Keyed by the local's
	// types.Object. Computed per file (no cross-file sharing) in performEscapeAnalysis.
	sstringEligible map[types.Object]bool

	// ssliceEligible flags a variadic parameter whose uses are proven not to let its slice header
	// escape the function frame. Its params Span<T> prologue may therefore bind through the
	// stack-only sslice<T> instead of copying into the heap slice<T>. Keyed by the parameter's
	// types.Object and computed per file in performEscapeAnalysis.
	ssliceEligible map[types.Object]bool

	// sstringConvExprs flags the specific `string(x)` conversion CallExprs that must emit `(sstring)x`
	// (the zero-copy view) rather than `(@string)x` (the heap copy): the RHS of an eligible local
	// (above) and unnamed `string(x)` temporaries consumed within a comparison against a literal
	// (`string(buf) == "…"`), which never outlive the expression so are safe unconditionally. Keyed
	// by the *ast.CallExpr node.
	sstringConvExprs map[*ast.CallExpr]bool

	// manualConversion marks a file whose DESTINATION `.cs` is a hand-owned manual conversion
	// ([module: go.GoManualConversion]). The file is still fully analyzed and visited with the
	// rest of its package — its package-wide state contributions (anonymous-struct lifts,
	// package-var registrations, escape/addressed-global analysis, imports) must match an
	// unseeded conversion exactly, or sibling files emit corrupted — but its EMISSION is
	// redirected to the non-compiled `<name>.cs.auto` review sibling instead of overwriting
	// the hand-owned `<name>.cs`.
	manualConversion bool
	// emissionExcluded marks a `-tests` variant file the Phase-4D file-exclusion ruling drops from
	// emission entirely (an Example/Benchmark-only `_test.go`, selectCompileExcludedTestFiles). It
	// stays in the variant's analysis entry list — pkg.Syntax feeds the shared passes — but no C#
	// is ever written for it, so, like a manualConversion file, it must never CLAIM a hoisted
	// string-literal field: a claim would assign the declaration to a file that renders nothing,
	// leaving every other use of that literal referencing a name that does not exist (strings'
	// `"abc"` in ExampleClone did exactly this once 3-char slugs became hoistable — CS0103 across
	// reader_test/replace_test).
	emissionExcluded bool
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

	// currentLambdaVarObjs records, per captured NAME, the types.Object of the OUTER variable that was
	// captured. currentLambdaVars maps by name, so a same-named variable DECLARED inside the lambda (an
	// `s := f(s)` self-shadow, where the inner `s` shadows the captured outer `s`) would otherwise be
	// renamed to the capture name too — conflating the two (`var sʗ3 = …(~sʗ3)…`, CS0841). The capture
	// name is applied only when an ident resolves to this exact captured object; a distinct inner binding
	// falls through to its own name.
	currentLambdaVarObjs map[string]types.Object

	// boxRefVars holds heap-boxed local variables whose address is taken inside a lambda. Such a
	// variable must NOT be snapshot-captured (the value copy loses the box, so writes through the
	// captured `&m` are lost — and the copy declaration is invalid in expression position, e.g. a
	// func literal passed as a call argument). Instead the lambda references the box directly: `&m`
	// emits `Ꮡm` (a capturable reference) and value uses emit `Ꮡm.Value` — the ref-local alias
	// `ref var m = ref Ꮡm.Value` itself can't be captured (CS8175). Keyed by the var's types.Object.
	boxRefVars map[types.Object]bool

	// Analysis phase tracking
	analysisInLambda  bool     // Currently analyzing a lambda
	currentLambda     ast.Node // Current lambda being analyzed
	detectingCaptures bool

	// Conversion phase tracking
	conversionInLambda bool     // Currently converting a lambda
	currentConversion  ast.Node // Current node being converted

	// conversionStack saves the conversion-phase fields above (plus the per-lambda var maps) on each
	// enterLambdaConversion so a NESTED lambda restores the ENCLOSING lambda's state on exit rather
	// than clobbering it. Without it, a nested func literal's exit reset conversionInLambda to false
	// (and nil'd the var maps), so every receiver/box reference in the enclosing lambda's body AFTER
	// the nested lambda rendered as the un-boxed ref-local — uncapturable inside a closure (CS8175;
	// database/sql (*Stmt).QueryContext read `s.cg` after the inner releaseConn closure).
	conversionStack []lambdaConversionState
}

// lambdaConversionState snapshots the conversion-phase LambdaCapture fields that
// enter/exitLambdaConversion mutate, so nested lambdas nest correctly (LIFO save/restore).
type lambdaConversionState struct {
	conversionInLambda   bool
	currentConversion    ast.Node
	currentLambdaVars    map[string]string
	currentLambdaVarObjs map[string]types.Object
}

type Visitor struct {
	fset               *token.FileSet
	pkg                *types.Package
	info               *types.Info
	file               *token.File
	outputBuilder      *strings.Builder
	standAloneComments map[token.Pos]string
	sortedCommentPos   []token.Pos
	processedComments  HashSet[token.Pos]
	newline            string
	indentLevel        int
	options            Options
	globalIdentNames   map[*ast.Ident]string // Global identifiers to adjusted names map
	globalScope        map[string]*types.Var // Global variable scope
	liftedTypeNames    HashSet[string]
	liftedTypeMap      map[types.Type]string
	subStructTypes     map[types.Type][]types.Type

	// Lifted ANONYMOUS struct types deduplicated by structural signature within a function:
	// structurally identical anonymous structs are ONE Go type, so repeated occurrences must
	// lift to a single C# type or reflect.Type identity splits per occurrence (see
	// visitStructType). Keyed `<funcName>\x00<signature>` → lifted name.
	liftedAnonStructNames map[string]string

	// hoistedDecls, when non-nil, collects func-literal capture declarations that would otherwise
	// be emitted inline (a `var mʗ1 = m;` statement) at the func literal's position — invalid C#
	// when the literal sits in an expression slot (a call argument, an assignment RHS, a composite-
	// literal element). The enclosing statement emitter (visitAssignStmt, …) sets this to a buffer,
	// converts its expressions, then writes the collected decls before the statement. convFuncLit
	// consults it (after context.deferredDecls, which go/defer/return thread explicitly). Save and
	// restore around nested statements so an inner statement's decls don't leak to the outer buffer.
	hoistedDecls *strings.Builder

	// globalDeclHoist, when non-nil, is the PACKAGE-LEVEL var-initializer spill sink: a
	// multi-value inner call spread at a package-level initializer (`var debug = template.Must(
	// template.New(…).Parse(…))`) has no statement sink, so convExprList emits a hidden static
	// tuple FIELD here and visitValueSpec flushes it before the var's own field (C# static field
	// initializers run in textual order). Only the tuple-spread arm writes to it.
	globalDeclHoist *strings.Builder

	// ImportSpec variables
	currentImportPath     string
	packageImports        *strings.Builder
	importQueue           HashSet[string]
	requiredUsings        HashSet[string]
	typeAliasDeclarations *strings.Builder
	// blankImportInits collects this FILE's blank-import module-initializer hooks — the
	// `[GoInit] … builtin.initPackage(typeof(<pkg>_package));` methods that force a
	// side-effects-only import's `init` to run (see visitImportSpec's blank-alias branch).
	// Spliced into the top of the file's class body at BlankImportInitMarker.
	blankImportInits *strings.Builder
	// A cross-package type reference emits a short-alias form (`pkg.Type`, `@unsafe.Pointer`) that
	// resolves only through a file-local alias `using <alias> = <namespace>;`. That alias is emitted
	// when the file imports the package under its canonical (unaliased) name; a file can reference the
	// type WITHOUT such an import — via type INFERENCE (a same-package function returns a foreign type,
	// so the caller need not import it — e.g. `fd := funcdata(...)`, funcdata returns unsafe.Pointer),
	// a BLANK import (`_ "pkg"`, whose C# alias is `_`), or an alias that differs from the canonical
	// name — and then the reference fails to resolve (CS0246). referencedForeignPackages collects the
	// import paths whose types getAliasQualifiedTypeName emits; canonicalAliasImported records the paths whose
	// canonical alias a file import already emitted. visitFile supplies the alias for the difference.
	referencedForeignPackages HashSet[string]
	canonicalAliasImported    HashSet[string]
	// importAliasesEmitted holds the C# alias NAMES a file's real imports already bound (`asn1`,
	// `encoding_asn1`, `time`). visitFile's synthesized canonical-alias `using` is skipped when its
	// alias collides with one of these — a same-named subpackage plus an aliased parent import both
	// resolving to alias `asn1` (cryptobyte's `encoding/asn1` + `.../cryptobyte/asn1`, CS1537).
	importAliasesEmitted HashSet[string]

	// importAliasTargets maps each C# alias name a file's imports bound to the TARGET that using
	// resolves to (`@unsafe` → `unsafe_package`, `ast` → `go.ast_package`). C# resolves a using-alias
	// REFERENT with the compilation unit's own using directives NOT in effect, so a referent may not
	// name another file-local alias — variadicElementType substitutes the recorded target to keep the
	// ellipsis alias legal (`using ꓸꓸꓸPointer = Span<unsafe_package.Pointer>;`, never
	// `Span<@unsafe.Pointer>` which fails CS0246). Recorded at the emit sites rather than re-derived,
	// so the `-tests` package-under-test rebinding (visitImportSpec's isPackageUnderTest) is honored
	// automatically instead of silently diverging.
	importAliasTargets map[string]string

	// importPathAliases maps a Go import PATH to the C# alias THIS FILE bound for it, for the
	// EXPLICITLY-ALIASED imports only. getAliasQualifiedTypeName consults it so a foreign type renders via the
	// file's ACTUAL alias, not the canonical package name: cryptobyte's asn1.go imports
	// `encoding/asn1` under the NON-canonical alias `encoding_asn1` (the vendored
	// `.../cryptobyte/asn1` subpackage claims the canonical `asn1`), so a `*asn1.BitString` type
	// reference must render `encoding_asn1.BitString`, not `asn1.BitString` (which resolves to the
	// subpackage — CS0426). Unaliased / blank / dot / Δ-collision-renamed imports are absent and fall
	// back to importQualifier(pkg.Name()) (the prior behavior), so this only changes explicit-alias
	// renders — no churn elsewhere. types.Type carries no source alias, so this map supplies it.
	importPathAliases map[string]string

	// FuncDecl variables
	inFunction           bool
	currentFuncDecl      *ast.FuncDecl
	currentFuncSignature *types.Signature
	// currentReturnSignature is the signature whose RESULTS a `return` currently emits against — the
	// enclosing function's, or a nested function literal's own (set with save/restore in convFuncLit).
	// Distinct from currentFuncSignature (which stays the enclosing func for receiver/param detection).
	currentReturnSignature *types.Signature
	currentFuncName        string
	currentFuncPrefix      *strings.Builder
	paramNames             HashSet[string]
	paramObjects           map[types.Object]bool
	// erasedTypeParams holds the current FUNCTION declaration's pointer-core (erased) type
	// parameters, identity-keyed to their pointer types (see collectErasedTypeParams) — the
	// single source every renderer/classifier consults so the erasure flips coherently, and
	// declined shapes (generic named types, receiver type params) never half-erase. Reset per
	// function declaration; func literals inside the declaration inherit it.
	erasedTypeParams map[*types.TypeParam]*types.Pointer
	// identAddressTakenCache memoizes per-object `&ident` scans of the current function
	// (see identAddressTaken); lazily initialized, keyed by the *types.Object so entries
	// from prior functions are simply never consulted again.
	identAddressTakenCache map[types.Object]bool
	// captureAnalysisDecl is the declaration whose body performVariableAnalysis is currently
	// walking — the real FuncDecl, or visitValueSpec's SYNTHETIC wrapper for a package-level
	// func-literal initializer. The shared-capture routing's write scan (varShareFacts) reads
	// it: it must match the tree processPotentialCapture is analyzing, which currentFuncDecl
	// does not during synthetic analysis (it still points at the previously visited function).
	captureAnalysisDecl *ast.FuncDecl
	// captureShareFactsCache memoizes varShareFacts per captured variable (reset per
	// performVariableAnalysis; variable objects are function-unique so entries never collide).
	captureShareFactsCache map[types.Object]captureShareFacts
	// nilSafePtrParamNames holds the raw names of pointer PARAMETERS that are compared with `==`/
	// `!=` (against nil or another pointer) anywhere in the current function body — i.e. params
	// walked to a nil terminator (`for p != nil { …; p = p.next }`). For these, the deref-alias and
	// any pointer-reassignment re-alias use the nil-safe `Ꮡp.DerefOrNil()` accessor instead of
	// `Ꮡp.Value`, so re-aliasing to a nil box yields a ref to default(T) (never read while p is nil)
	// rather than throwing a nil-pointer dereference. Populated per function in visitFuncDecl;
	// other (non-nil-compared) pointer params keep the plain `.Value` form (zero golden churn).
	nilSafePtrParamNames HashSet[string]
	// nilSafeEntryOnlyParamName is the raw name of a pointer parameter / direct-ж receiver that the
	// first body statement RE-POINTS before anything reads through it (Go's nil-receiver
	// NORMALIZATION idiom, `l = l.get()`). Only the ENTRY alias goes nil-safe for it — the alias the
	// assignment immediately replaces, so nothing can read the throwaway slot. The re-alias AFTER
	// the assignment deliberately keeps `.Value`, because by then the pointer is whatever the
	// normalizer returned and a genuine deref of a still-nil one must panic exactly as Go's does.
	// That is what separates this from nilSafePtrParamNames, which covers both aliases and accepts
	// the "reads default(T) instead of panicking" trade in exchange. A parameter can be in both sets
	// (a body that re-points AND nil-compares), and the two compose: entry nil-safe either way, the
	// re-alias nil-safe only when the comparison arm asked for it.
	nilSafeEntryOnlyParamName string
	// funcLitHeapBoxParamNames holds the RENDERED names of the function literal parameters that
	// need an entry-time heap box (see funcLitHeapBoxParamIdents) — set transiently by
	// convFuncLit around exactly the signature-generation calls (convFuncType for a plain
	// literal, iifeParamNames for an IIFE) so the parameter emits under its incoming `ʗp` name,
	// and nil otherwise. A literal's signature is generated from SYNTHESIZED vars (see
	// getSignature) that can never match the identEscapesHeap entries paramNeedsHeapBox keys
	// on, so the box decision travels by name here.
	funcLitHeapBoxParamNames HashSet[string]
	varNames                 map[*types.Var]string
	hasDefer                 bool
	hasRecover               bool
	// pendingTypeAccess carries an explicit C# access modifier ("public ") for the type
	// declaration currently being emitted — set by visitTypeSpec for an unexported type that
	// must be publicized (used as an exported struct field; see packagePublicizedTypes), and
	// consumed (read and cleared) by the type-kind emitter (visitArrayType/visitStructType/…).
	pendingTypeAccess string
	// manualConversion mirrors the file entry's flag: this file's destination `.cs` is a hand-owned
	// manual conversion, so the visit still feeds package-wide state but the emitted text lands in
	// the non-compiled `.cs.auto` review sibling. Consulted by recordTypeAccessibility, which must
	// not declare the accessibility of types the hand-written file owns.
	manualConversion bool
	// namedReturnDeferMode is set when the current function has named return values AND uses
	// defer/recover. Such a function is emitted as a block body that declares the named returns
	// *outside* the `func((defer, recover) => …)` wrapper (so deferred code, including recover,
	// mutates them by closure) and returns them *after* the wrapper runs — matching Go, where a
	// `return` assigns the result params, runs the defers, then returns the (possibly-mutated)
	// result params. namedReturnNames holds those result identifiers in order.
	namedReturnDeferMode bool
	namedReturnNames     []string
	// blankResultNames interns the generated slot name for each BLANK (`_`) result of a
	// namedReturnDefer signature — Go allows mixing blank and named results
	// (`func parse(…) (_ *Regexp, err error)`), and the blank slot still needs a C# local so
	// returns can write it and the post-defer return can read it back. Keyed by the result's
	// *types.Var so every render site of the same slot agrees on one name.
	blankResultNames map[*types.Var]string
	useUnsafeFunc    bool
	capturedVarCount map[string]int
	tempVarCount     map[string]int

	// BlockStmt variables
	blocks                 Stack[*strings.Builder]
	firstStatementIsReturn bool
	// tupleTempIndex numbers the multi-value-call expansion temp markers monotonically per
	// file (see convExprList's tuple-arg expansion).
	tupleTempIndex int
	// inForPost is set while emitting a for-loop's POST statement. A deref-aliased pointer
	// param/box repointed in the post (`for ; scope != nil; scope = scope.Outer`) expands to a
	// box-repoint PLUS a value re-alias (`Ꮡscope = scope.Outer; scope = ref Ꮡscope…`); the
	// second statement cannot share the single for-post slot, so the re-alias is stashed in
	// forPostReAlias and visitForStmt injects it at the TOP of the loop body instead.
	inForPost      bool
	forPostReAlias string
	// forPerIterVars holds the `for i := …` clause variables currently emitted with Go 1.22+
	// per-iteration semantics (see forClausePerIterVars): the clause drives a renamed carrier
	// and the body re-declares the variable fresh each pass. convertToHeapTypeDecl consults it
	// so the carrier stays a plain value — a boxed variable's fresh box is emitted inside the
	// body per iteration, never hoisted at the declaration site.
	forPerIterVars map[types.Object]bool
	// loopCopyBackStack parallels the enclosing loop nesting during body emission. Each entry
	// holds the per-iteration copy-back statements (`iᴛ1 = i;`) an unlabeled `continue` must
	// emit before transferring to the post clause (nil for range loops and for loops whose
	// per-iteration variables are never written in the body).
	loopCopyBackStack       [][]string
	lastStatementWasReturn  bool
	lastReturnIndentLevel   int
	identEscapesHeap        map[types.Object]bool
	tightenedConsts         map[*types.Const]*types.Basic // Function-local untyped consts declared at their single concrete use type (see performUntypedConstAnalysis)
	sstringEligible         map[types.Object]bool         // String locals emittable as stack-only sstring (see FileEntry.sstringEligible)
	ssliceEligible          map[types.Object]bool         // Variadic params emittable as stack-only sslice (see FileEntry.ssliceEligible)
	sstringConvExprs        map[*ast.CallExpr]bool        // `string(x)` conversions that emit `(sstring)x` (see FileEntry.sstringConvExprs)
	emitStringConvAsSString bool                          // Transient: while emitting an eligible decl's RHS, a string([]byte) conversion emits `(sstring)` not `(@string)`
	sstringHoistedConvExprs map[*ast.CallExpr]string      // Per-func: eligible `string(x)` uses lifted to a shared sstring temp — each emits the temp NAME (see planSStringHoists)
	sstringHoistsByStmt     map[ast.Stmt][]sstringHoist   // Per-func: hoisted sstring temp decls to inject before a top-level body statement (its anchor)
	suppressSStringHoist    bool                          // Transient: while rendering a hoisted temp's OWN initializer, ignore sstringHoistedConvExprs so the real `((sstring)x)` view is emitted
	identNames              map[*ast.Ident]string         // Local identifiers to adjusted names map
	isReassigned            map[*ast.Ident]bool           // Local identifiers to reassignment status map
	// untypedConstContexts maps an UNTYPED constant subexpression to the resolved type of its
	// enclosing typed constant expression — the context go/types drops when it leaves constant
	// operands untyped (see markUntypedConstContexts). convBasicLit consults it for the F/D
	// float-literal suffix and the postfix `.i()` complex64/complex128 overload choice.
	untypedConstContexts map[ast.Expr]types.Type
	funcLevelDecls       map[string]*types.Var // Function-level local declarations of the current function (for global-shadow qualification)
	// funcScopeVarNames holds the Go name of every variable declared ANYWHERE in the current
	// function — receiver, parameters, results and locals at every nesting depth, including inside
	// func literals. A bare type name spelled by the EMITTER (the `Type.Ꮡfield` box accessor) binds
	// to such a variable rather than to the type wherever one exists, so boxAccessorType qualifies
	// against this set. Repopulated per function by performVariableAnalysis.
	funcScopeVarNames HashSet[string]
	scopeStack        []map[string]*types.Var // Stack of local variable scopes
	lambdaCapture     *LambdaCapture          // Lambda capture tracking
}
