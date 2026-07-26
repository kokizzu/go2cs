// hoistedLiteralOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"strconv"
	"strings"
)

// Go string literals live in RODATA: `return "true"`, `HasPrefix(line, "//go:build")` and
// `counts["build"]++` allocate NOTHING in a Go binary. The converted C# pays a fresh backing
// byte[] at EVERY evaluation, because each literal -> @string materialization copies the u8 span.
// This pass HOISTS each package-unique literal that materializes a VALUE to one
// `private static readonly` field declared immediately above the function that first consumes it,
// so the literal costs at most one allocation per program run — Go's own cost model.
//
// It is a whole-package PRE-pass, not an emission-time decision, for three reasons the design
// (docs/Phase4/DESIGN-string-literal-allocation.md §4) pins:
//
//  1. A literal is emitted PRE-BOXED (`static readonly object`) only when EVERY use in the
//     package is an `any` target — a property no forward-only emission walk can know.
//  2. collectMovedInitVars must know, BEFORE any file is emitted, which functions read a hoisted
//     field: a package-level var initializer that transitively reads one would otherwise run as a
//     C# field initializer in UNSPECIFIED cross-part order and could observe `default(@string)`
//     ("") instead of the literal (§4.4's init-order rule).
//  3. Placement is first-USE, which is only defined against the whole package's file order.
//
// Emission is then a pure substitution: convExpr's BasicLit arm renders the field name for any
// node this pass registered, and visitFuncDecl emits the declarations the function owns.

// packageHoistedLits maps every string-literal NODE this pass resolved to the package-scoped
// static field that stands in for it. convExpr's BasicLit arm consults it; a node absent here
// renders exactly as it always has.
var packageHoistedLits map[*ast.BasicLit]*hoistedLiteral

// packageHoistedDecls maps each function declaration to the hoisted fields it OWNS — the ones
// whose first package-wide use is inside it. visitFuncDecl writes them into the function-prefix
// builder, which lands them immediately above the function's doc comment.
var packageHoistedDecls map[*ast.FuncDecl][]*hoistedLiteral

// packageHoistLitReaders holds every package function whose body READS a hoisted field. It is the
// extra dependency edge collectMovedInitVars needs: a package-level var initializer that reaches
// one of these transitively must be relocated into the ordered static constructor, which C# runs
// after ALL static field initializers (§4.4).
var packageHoistLitReaders map[*types.Func]bool

// packageHoistNames maps a literal's Go SOURCE TOKEN to the field the pass claimed for it — the
// production-side map a `-tests` conversion is seeded with (see collectHoistedLiterals).
var packageHoistNames map[string]hoistSeed

// hoistedLiteral is one package-unique string literal hoisted to a static field.
type hoistedLiteral struct {
	token     string        // Go literal SOURCE token text — the dedupe key AND the emitted spelling
	node      *ast.BasicLit // the first-use node, re-rendered for the field initializer
	name      string        // emitted C# field name (slug + HoistedLiteralMarker [+ ordinal])
	owner     *ast.FuncDecl // function whose prefix declares the field (nil = seeded, declared elsewhere)
	anyUses   int           // uses whose target slot is `any`
	valueUses int           // uses whose target slot is a string / named-string type
	preBoxed  bool          // anyUses > 0 && valueUses == 0 -> `static readonly object`
	seeded    bool          // declared by the production conversion (`-tests` pass) — reference only
}

// hoistSeed is one production-side declaration a `-tests` conversion may reference.
type hoistSeed struct {
	name     string
	preBoxed bool
}

// hoistUseKind is the target-slot shape of one use site: a plain value slot (a Go `string` or a
// named type over one) or an `any` slot, which boxes. A literal whose every use is `any` is emitted
// pre-boxed, so its call sites allocate nothing at all.
type hoistUseKind int

const (
	hoistUseValue hoistUseKind = iota
	hoistUseAny
)

// maxHoistSlugLength is the §4.3 budget: identifier-safe words, camelCase-joined, truncated at a
// WORD boundary. Long enough that `goBuildIgnoreDirective` survives, short enough that a sentence
// literal does not become an unreadable identifier.
const maxHoistSlugLength = 24

// minHoistSlugLength is the degenerate-slug floor (§4.2): degenerate = empty slug OR a slug of
// three characters or fewer. A literal whose slug carries no information — a bare verb (`"%v"` ->
// `v`), punctuation only (`"; "` -> nothing), a one- or two-letter word — is NOT hoisted at all: it
// stays inline in its Tier-B rendering, where it is still readable. `"true"` slugs to a healthy
// four-character `true` and DOES hoist (the design's headline example). The `strˢN` fallback the
// design mentions therefore exists only as a collision ordinal among HEALTHY slugs, never as a
// naming dump.
const minHoistSlugLength = 4

// collectHoistedLiterals runs the whole-package hoist analysis (see the file comment). It must run
// BEFORE collectMovedInitVars, whose graph consults packageHoistLitReaders.
//
// seed is nil for a production conversion. On the `-tests` path it carries the PRODUCTION
// literal→field map: a seeded literal may only be REFERENCED by the test files (the production
// `.cs` on disk already declares it, and an internal `_test.go` emits into the same package class
// and can sort BEFORE its production owner — the seed, not name luck, is what prevents CS0102).
// Only `_test.go` files may claim a NEW field in that mode, since the production files are not
// re-emitted.
func collectHoistedLiterals(files []FileEntry, pkg *types.Package, info *types.Info, seed map[string]hoistSeed) {
	packageHoistedLits = make(map[*ast.BasicLit]*hoistedLiteral)
	packageHoistedDecls = make(map[*ast.FuncDecl][]*hoistedLiteral)
	packageHoistLitReaders = make(map[*types.Func]bool)
	packageHoistNames = make(map[string]hoistSeed)

	pkgPath := ""

	if pkg != nil {
		pkgPath = pkg.Path()
	}

	c := &hoistCollector{
		info:    info,
		pkgPath: pkgPath,
		seed:    seed,
		byToken: make(map[string]*hoistedLiteral),
		uses:    make(map[*ast.BasicLit]*hoistedLiteral),
		useFunc: make(map[*ast.BasicLit]*ast.FuncDecl),
	}

	// Sequential, sorted-filename order — the same order emission uses, so first-use placement and
	// collision ordinals are reproducible run to run.
	for _, fileEntry := range files {
		// A [module: GoManualConversion] file's emission is redirected to a non-compiled
		// `.cs.auto`, so it must never CLAIM a field (nothing would declare it) — and since its
		// own literals then have no declaration to reference, they simply stay inline. Skipping
		// the file entirely gives both properties at once (§4.4).
		if fileEntry.manualConversion {
			continue
		}

		// In seeded (`-tests`) mode only the `_test.go` files are EMITTED; a production file's
		// literal is either already seeded (declared in the production `.cs` on disk) or must stay
		// exactly as that `.cs` renders it — a claim here would declare a field into a file that is
		// never rewritten.
		c.emitted = seed == nil || strings.HasSuffix(strings.ToLower(fileEntry.filePath), "_test.go")

		for _, decl := range fileEntry.file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok || funcDecl.Body == nil {
				continue
			}

			// `func init()` runs exactly once by construction — hoisting buys nothing and would
			// move real work into the type initializer. A deterministic AST filter, not a
			// hotness heuristic (§4.2).
			if funcDecl.Recv == nil && funcDecl.Name.Name == "init" {
				continue
			}

			// A declaration owned by a manual conversion emits only a placeholder comment (see
			// visitFuncDecl's isManualFuncDecl early return) — it renders no FunctionPrefixMarker,
			// so it can neither declare a field nor reference one from a body that is discarded.
			if isManualFuncDeclInPackage(c.pkgPath, funcDecl) {
				continue
			}

			c.collectFunc(funcDecl)
		}
	}

	c.assignNames(pkg)

	// Publish only the literals that survived naming (a degenerate slug drops the literal and
	// every one of its uses back to inline rendering).
	for lit, hl := range c.uses {
		if hl.name == "" {
			continue
		}

		packageHoistedLits[lit] = hl
	}

	for _, hl := range c.ordered {
		if hl.name == "" {
			continue
		}

		packageHoistNames[hl.token] = hoistSeed{name: hl.name, preBoxed: hl.preBoxed}

		// owner == nil means the field is SEEDED — declared by the production conversion's own
		// output, which this pass does not re-emit.
		if hl.owner != nil {
			packageHoistedDecls[hl.owner] = append(packageHoistedDecls[hl.owner], hl)
		}
	}

	// Which package functions READ a hoisted field — the input to collectMovedInitVars' rule.
	// Computed from the SURVIVING literals only: a function whose every literal was dropped as
	// degenerate reads no field and must not drag its callers' initializers into the static ctor.
	for lit := range packageHoistedLits {
		funcDecl := c.useFunc[lit]

		if funcDecl == nil {
			continue
		}

		if obj, ok := info.Defs[funcDecl.Name].(*types.Func); ok && obj != nil {
			packageHoistLitReaders[obj] = true
		}
	}
}

type hoistCollector struct {
	info    *types.Info
	pkgPath string
	seed    map[string]hoistSeed
	byToken map[string]*hoistedLiteral
	ordered []*hoistedLiteral
	uses    map[*ast.BasicLit]*hoistedLiteral
	useFunc map[*ast.BasicLit]*ast.FuncDecl

	// per-file walk state: whether this file's literals may CLAIM a field declaration.
	emitted bool

	// per-function walk state
	funcDecl *ast.FuncDecl
}

func (c *hoistCollector) collectFunc(funcDecl *ast.FuncDecl) {
	c.funcDecl = funcDecl

	var stack []ast.Node

	ast.Inspect(funcDecl.Body, func(n ast.Node) bool {
		if n == nil {
			stack = stack[:len(stack)-1]
			return true
		}

		if lit, ok := n.(*ast.BasicLit); ok && lit.Kind == token.STRING {
			c.consider(lit, stack)
		}

		stack = append(stack, n)
		return true
	})
}

// consider registers lit when its context materializes a VALUE that a shared static field can
// stand in for. Every §4.2 exclusion is applied here.
func (c *hoistCollector) consider(lit *ast.BasicLit, stack []ast.Node) {
	value, err := strconv.Unquote(lit.Value)

	if err != nil {
		return // unparsable token — leave it exactly as it renders today
	}

	// The empty literal already costs 0 B (ToArray() of an empty span returns Array.Empty), and a
	// raw-byte-escape literal takes the byte-array-backed @string path, not the u8 span (§4.2).
	if value == "" || (!strings.HasPrefix(lit.Value, "`") && stringLiteralNeedsByteArray(lit.Value)) {
		return
	}

	kind, ok := c.hoistTarget(lit, stack)

	if !ok {
		return
	}

	hl := c.byToken[lit.Value]

	if hl == nil {
		if seeded, isSeeded := c.seed[lit.Value]; isSeeded {
			// Already declared by the production conversion — REFERENCE only (owner nil), with
			// the production field's TYPE, which this pass cannot change.
			hl = &hoistedLiteral{token: lit.Value, node: lit, name: seeded.name, preBoxed: seeded.preBoxed, seeded: true}
		} else if !c.emitted {
			return // a non-emitted file cannot declare the field its own use would need
		} else {
			hl = &hoistedLiteral{token: lit.Value, node: lit, owner: c.funcDecl}
		}

		c.byToken[lit.Value] = hl
		c.ordered = append(c.ordered, hl)
	}

	// A SEEDED field the production pass emitted PRE-BOXED is an `object`; a value slot needs an
	// @string, so that use keeps its inline rendering. (Cannot arise for a literal this pass owns:
	// preBoxed is derived from its complete use set.)
	if hl.seeded && hl.preBoxed && kind == hoistUseValue {
		return
	}

	if kind == hoistUseAny {
		hl.anyUses++
	} else {
		hl.valueUses++
	}

	c.uses[lit] = hl
	c.useFunc[lit] = c.funcDecl
}

// hoistTarget classifies lit's immediate context and reports the target-slot kind, or ok=false
// when the site is one §4.2 excludes. The classification is by IMMEDIATE PARENT, which is what
// makes the composite-literal exclusion exact: a literal that is a call ARGUMENT inside a
// composite (`[]error{fmt.Errorf("x")}`) still qualifies, while a composite ELEMENT does not.
// Everything not listed here — comparison and concat operands, switch-case values, composite
// elements and keys, struct tags, index expressions over a non-map — is excluded by falling
// through.
func (c *hoistCollector) hoistTarget(lit *ast.BasicLit, stack []ast.Node) (hoistUseKind, bool) {
	if len(stack) == 0 {
		return 0, false
	}

	parent := stack[len(stack)-1]

	switch p := parent.(type) {
	case *ast.ReturnStmt:
		idx := exprIndexOf(p.Results, lit)
		sig := c.enclosingSignature(stack)

		if idx < 0 || sig == nil || sig.Results() == nil || len(p.Results) != sig.Results().Len() {
			return 0, false
		}

		return c.classifyTarget(sig.Results().At(idx).Type())

	case *ast.AssignStmt:
		// `s += "…"` is a CONCAT in assignment clothing and is excluded with the other concat
		// operands (§4.2): golib's `operator +(@string, ReadOnlySpan<byte>)` already consumes the
		// span without materializing it.
		if p.Tok != token.ASSIGN && p.Tok != token.DEFINE {
			return 0, false
		}

		idx := exprIndexOf(p.Rhs, lit)

		if idx < 0 || len(p.Lhs) != len(p.Rhs) {
			return 0, false
		}

		return c.classifyTarget(c.info.TypeOf(p.Lhs[idx]))

	case *ast.ValueSpec:
		// A CONST spec's value is emitted as the constant's own `static readonly` field — there is
		// nothing to hoist it to, and doing so would just rename the declaration.
		for i := len(stack) - 1; i >= 0; i-- {
			if genDecl, ok := stack[i].(*ast.GenDecl); ok {
				if genDecl.Tok == token.CONST {
					return 0, false
				}

				break
			}
		}

		idx := exprIndexOf(p.Values, lit)

		if idx < 0 || len(p.Names) != len(p.Values) {
			return 0, false
		}

		if p.Type != nil {
			return c.classifyTarget(c.info.TypeOf(p.Type))
		}

		if obj := c.info.Defs[p.Names[idx]]; obj != nil {
			return c.classifyTarget(obj.Type())
		}

		return 0, false

	case *ast.SendStmt:
		if p.Value != lit {
			return 0, false
		}

		chanType, ok := c.typeUnder(c.info.TypeOf(p.Chan)).(*types.Chan)

		if !ok {
			return 0, false
		}

		return c.classifyTarget(chanType.Elem())

	case *ast.IndexExpr:
		// A standalone map-index key (`counts["build"]++`, `m["k"] = v`, `x := m["k"]`) rebuilds
		// the key @string on every evaluation.
		if p.Index != lit {
			return 0, false
		}

		mapType, ok := c.typeUnder(c.info.TypeOf(p.X)).(*types.Map)

		if !ok {
			return 0, false
		}

		return c.classifyTarget(mapType.Key())

	case *ast.CallExpr:
		return c.callArgTarget(p, lit)
	}

	return 0, false
}

func (c *hoistCollector) callArgTarget(call *ast.CallExpr, lit *ast.BasicLit) (hoistUseKind, bool) {
	idx := exprIndexOf(call.Args, lit)

	// A SPREAD call (`append(b, "x"...)`) renders its arguments through the ellipsis machinery,
	// which wraps a literal in its own `((@string)…)ꓸꓸꓸ` form — leave the whole call alone.
	if idx < 0 || call.Ellipsis.IsValid() {
		return 0, false
	}

	// A UNIVERSE BUILTIN is not an ordinary call: go/types records a call-site-specific signature
	// for it (so `panic("…")` reads as an `any` parameter), but the converter emits each builtin
	// through its own special path — `panic` deliberately keeps the bare interned literal (zero
	// cost until a panic actually fires), `print`/`println` take a golib overload set, and
	// `[]byte("…")`/`copy` must materialize fresh storage. None of them hoist.
	if callIsUniverseBuiltin(c.info, call.Fun) {
		return 0, false
	}

	// A CONVERSION, not a call. `[]byte("x")` / `[]rune("x")` must materialize FRESH mutable
	// storage and already emit the single mandatory allocation; only a named type whose underlying
	// is `string` re-materializes the same shared value through the wrapper's ctor (§4.2).
	if tv, ok := c.info.Types[call.Fun]; ok && tv.IsType() {
		named, isNamed := types.Unalias(c.info.TypeOf(call)).(*types.Named)

		if !isNamed {
			return 0, false
		}

		if basic, isBasic := named.Underlying().(*types.Basic); !isBasic || basic.Kind() != types.String {
			return 0, false
		}

		return hoistUseValue, true
	}

	sig, ok := c.typeUnder(c.info.TypeOf(call.Fun)).(*types.Signature)

	if !ok {
		return 0, false
	}

	params := sig.Params()

	if params == nil {
		return 0, false
	}

	// FORMAT-POSITION literals stay inline (§4.2): a format string slugs badly (`"%v"` -> `vˢ`,
	// `"invalid %s at %d"` -> `invalidSAtD`), and a formatting call's cost is dominated by the
	// formatting itself. Recognised structurally — a variadic function whose name ends in `f`
	// with a `string` parameter immediately before the variadic — which covers fmt/log/testing's
	// whole *f family and any user wrapper shaped like one.
	if sig.Variadic() && params.Len() >= 2 && idx == params.Len()-2 && callFuncNameEndsWithF(call.Fun) {
		if basic, isBasic := params.At(idx).Type().Underlying().(*types.Basic); isBasic && basic.Kind() == types.String {
			return 0, false
		}
	}

	var target types.Type

	if sig.Variadic() && idx >= params.Len()-1 {
		slice, isSlice := params.At(params.Len() - 1).Type().(*types.Slice)

		if !isSlice {
			return 0, false
		}

		target = slice.Elem()
	} else if idx < params.Len() {
		target = params.At(idx).Type()
	} else {
		return 0, false
	}

	return c.classifyTarget(target)
}

// classifyTarget maps a target slot's type to the hoist kind. Only two slots qualify: a Go
// `string` (or a named type over one — the wrapper converts implicitly from @string) and the
// EMPTY interface. A NON-empty interface is excluded on purpose: its value needs an adapter, not
// an @string; a type PARAMETER is excluded because its instantiation, not the slot, decides.
func (c *hoistCollector) classifyTarget(t types.Type) (hoistUseKind, bool) {
	if t == nil {
		return 0, false
	}

	if _, isTypeParam := types.Unalias(t).(*types.TypeParam); isTypeParam {
		return 0, false
	}

	if isEmptyInterfaceTarget(t) {
		return hoistUseAny, true
	}

	if isIface, _ := isInterface(t); isIface {
		return 0, false
	}

	if basic, ok := t.Underlying().(*types.Basic); ok && basic.Kind() == types.String {
		return hoistUseValue, true
	}

	return 0, false
}

func (c *hoistCollector) typeUnder(t types.Type) types.Type {
	if t == nil {
		return nil
	}

	return t.Underlying()
}

// enclosingSignature resolves the signature a `return` belongs to — the innermost enclosing func
// LITERAL if there is one, else the function declaration being walked.
func (c *hoistCollector) enclosingSignature(stack []ast.Node) *types.Signature {
	for i := len(stack) - 1; i >= 0; i-- {
		if funcLit, ok := stack[i].(*ast.FuncLit); ok {
			sig, _ := c.info.TypeOf(funcLit).(*types.Signature)
			return sig
		}
	}

	if obj, ok := c.info.Defs[c.funcDecl.Name].(*types.Func); ok && obj != nil {
		sig, _ := obj.Type().(*types.Signature)
		return sig
	}

	return nil
}

// assignNames turns each registered literal into a field name, dropping the ones whose slug is
// degenerate (§4.2) and giving same-slug literals a deterministic first-occurrence ordinal.
func (c *hoistCollector) assignNames(pkg *types.Package) {
	claimed := map[string]bool{}

	// Package-declared C# names are off limits — performNameCollisionAnalysis walks GO
	// declarations only and never sees a synthetic name, so the check has to run here (§4.3).
	if pkg != nil && pkg.Scope() != nil {
		for _, name := range pkg.Scope().Names() {
			claimed[getSanitizedIdentifier(name)] = true
		}
	}

	// Seeded names are already declared by the production conversion — reserve them before any new
	// test-side literal can pick the same identifier.
	for _, hl := range c.ordered {
		if hl.seeded {
			claimed[hl.name] = true
		}
	}

	for _, hl := range c.ordered {
		if hl.seeded {
			continue // the production declaration decides both the name AND the field type
		}

		value, err := strconv.Unquote(hl.token)

		if err != nil {
			continue
		}

		slug := literalSlug(value)

		if len(slug) < minHoistSlugLength {
			continue // degenerate — the literal and all its uses stay inline
		}

		name := slug + HoistedLiteralMarker

		for ordinal := 2; claimed[name]; ordinal++ {
			name = fmt.Sprintf("%s%s%d", slug, HoistedLiteralMarker, ordinal)
		}

		claimed[name] = true
		hl.name = name
		hl.preBoxed = hl.anyUses > 0 && hl.valueUses == 0
	}
}

// literalSlug renders a string literal's VALUE as an identifier-safe camelCase slug: word runs of
// ASCII letters/digits, joined, truncated at a WORD boundary within the §4.3 budget. Returns ""
// when the value has no usable word content (the literal then stays inline — §4.2's degenerate
// rule).
//
// ASCII, not `unicode.IsLetter`: a C# identifier is lexed over UTF-16 CODE UNITS, so a letter
// outside the BMP is a surrogate pair and is never a valid identifier character even though Go
// classifies it Lu — go/types' universe type set is spelled `"𝓤"` (U+1D4E4), and a rune-wide slug
// emitted `𝓤ˢ`, a CS1056/CS1519 cascade in typeset.cs and typeterm.cs. Restricting the alphabet to
// ASCII closes that class outright (along with combining marks, format characters, and RTL
// content) and is what keeps a content-derived name readable in the first place; a literal with no
// ASCII word content simply is not hoisted.
func literalSlug(value string) string {
	var words []string
	var current strings.Builder

	for _, r := range value {
		if isASCIILetter(r) || (r >= '0' && r <= '9') {
			current.WriteRune(r)
			continue
		}

		if current.Len() > 0 {
			words = append(words, current.String())
			current.Reset()
		}
	}

	if current.Len() > 0 {
		words = append(words, current.String())
	}

	// An identifier cannot start with a digit: drop leading all-digit words rather than mangle.
	for len(words) > 0 && !startsWithLetter(words[0]) {
		words = words[1:]
	}

	if len(words) == 0 {
		return ""
	}

	var slug strings.Builder

	for i, word := range words {
		next := camelCaseSlugWord(word, i == 0)

		// Truncate at a WORD boundary — never mid-word, which is where unreadable names come from.
		if slug.Len() > 0 && slug.Len()+len(next) > maxHoistSlugLength {
			break
		}

		slug.WriteString(next)

		if slug.Len() >= maxHoistSlugLength {
			break
		}
	}

	return slug.String()
}

// camelCaseSlugWord folds one slug word into its camelCase position. An ALL-CAPS word is folded
// whole (`"TESTING KEY"` -> `testingKey`, `"CONTENT-TYPE"` -> `contentType`, `"GET"` -> `get`) —
// touching only its first character would leave `tESTINGKEY`, which is not camelCase in any useful
// sense and was 9% of the corpus' hoisted names on the first cut (ALL-CAPS constants, HTTP verbs
// and header names, DNS record types). A word that already MIXES case keeps its interior verbatim,
// so `"ParseBool"` stays `parseBool`.
func camelCaseSlugWord(word string, first bool) string {
	allUpper := true
	hasLetter := false

	for i := 0; i < len(word); i++ {
		if isASCIILetter(rune(word[i])) {
			hasLetter = true

			if word[i] >= 'a' && word[i] <= 'z' {
				allUpper = false
			}
		}
	}

	if hasLetter && allUpper {
		word = strings.ToLower(word)
	}

	// ASCII-only alphabet (see literalSlug), so byte indexing is rune-safe here.
	if first {
		return strings.ToLower(word[:1]) + word[1:]
	}

	return strings.ToUpper(word[:1]) + word[1:]
}

func startsWithLetter(s string) bool {
	for _, r := range s {
		return isASCIILetter(r)
	}

	return false
}

func isASCIILetter(r rune) bool {
	return (r >= 'a' && r <= 'z') || (r >= 'A' && r <= 'Z')
}

// callFuncNameEndsWithF reports whether a call's callee is named `…f` (Printf, Errorf, Fatalf,
// Logf, a user wrapper) — the structural half of the format-position test.
func callFuncNameEndsWithF(fun ast.Expr) bool {
	var name string

	switch f := fun.(type) {
	case *ast.Ident:
		name = f.Name
	case *ast.SelectorExpr:
		name = f.Sel.Name
	case *ast.IndexExpr:
		return callFuncNameEndsWithF(f.X)
	case *ast.IndexListExpr:
		return callFuncNameEndsWithF(f.X)
	case *ast.ParenExpr:
		return callFuncNameEndsWithF(f.X)
	default:
		return false
	}

	return strings.HasSuffix(name, "f")
}

// callIsUniverseBuiltin reports whether a call's callee resolves to a universe (or unsafe)
// builtin — `panic`, `print`, `copy`, `unsafe.Slice`, … go/types records a call-specific
// SIGNATURE for these, so a signature check alone cannot tell them apart from a real function.
func callIsUniverseBuiltin(info *types.Info, fun ast.Expr) bool {
	for {
		if paren, ok := fun.(*ast.ParenExpr); ok {
			fun = paren.X
			continue
		}

		break
	}

	var ident *ast.Ident

	switch f := fun.(type) {
	case *ast.Ident:
		ident = f
	case *ast.SelectorExpr:
		ident = f.Sel
	default:
		return false
	}

	_, isBuiltin := info.Uses[ident].(*types.Builtin)

	return isBuiltin
}

func exprIndexOf(exprs []ast.Expr, target ast.Expr) int {
	for i, expr := range exprs {
		if expr == target {
			return i
		}
	}

	return -1
}

// hoistedLiteralName returns the field name standing in for basicLit, or "" when the node was not
// hoisted. convExpr's BasicLit arm consults this: emission for Tier C is a pure substitution.
func hoistedLiteralName(basicLit *ast.BasicLit) string {
	if packageHoistedLits == nil {
		return ""
	}

	if hl, ok := packageHoistedLits[basicLit]; ok {
		return hl.name
	}

	return ""
}

// funcReadsHoistedLiteral reports whether fn's body reads a hoisted field (see
// packageHoistLitReaders) — collectMovedInitVars' extra dependency edge.
func funcReadsHoistedLiteral(fn *types.Func) bool {
	if packageHoistLitReaders == nil || fn == nil {
		return false
	}

	return packageHoistLitReaders[fn]
}

// writeHoistedLiteralDecls emits the `static readonly` field declarations funcDecl OWNS into the
// function-prefix builder, which visitFuncDecl back-patches immediately above the function's doc
// comment — the same declaration-injection path a lifted anonymous struct/interface rides. Fields
// are `private` to the package class, so the C# beforefieldinit question never becomes cross-type.
func (v *Visitor) writeHoistedLiteralDecls(funcDecl *ast.FuncDecl) {
	decls := packageHoistedDecls[funcDecl]

	if len(decls) == 0 {
		return
	}

	if v.currentFuncPrefix.Len() > 0 {
		v.currentFuncPrefix.WriteString(v.newline)
	}

	v.currentFuncPrefix.WriteString("// Hoisted @string literals (single allocation; Go keeps these in RODATA)")
	v.currentFuncPrefix.WriteString(v.newline)

	for _, hl := range decls {
		litContext := DefaultBasicLitContext()
		litContext.u8StringOK = true
		// A PRE-BOXED field is an `object`, so its initializer needs the same `(@string)` cast an
		// inline `any`-slot literal takes — the span has no conversion to object on its own.
		litContext.castToGoString = hl.preBoxed
		litContext.spanTargetUnsupported = hl.preBoxed

		fieldType := "@string"

		if hl.preBoxed {
			fieldType = "object"
		}

		v.currentFuncPrefix.WriteString(fmt.Sprintf("private static readonly %s %s = %s;", fieldType, hl.name, v.convBasicLit(hl.node, litContext)))
		v.currentFuncPrefix.WriteString(v.newline)
	}
}
