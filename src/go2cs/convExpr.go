// convExpr.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/types"
	"strings"
)

type KeyValueSource int

const (
	StructSource KeyValueSource = iota
	MapSource
	ArraySource
)

// ExprContext is the common shape of every per-expression conversion context (BasicLitContext,
// LambdaContext, CallExprContext, …). Callers hand convExpr a heterogeneous []ExprContext and the
// generic getExprContext picks out the one entry of the type a given expression kind cares about,
// so an expression deep in a tree can see the intent of the slot it is being emitted into.
//
// getDefault exists so getExprContext can produce a fully-initialized context when the caller
// supplied none of that type. It is declared on the interface — rather than looked up in a table —
// because Go can then call it on a ZERO value of the type parameter, which is the only handle
// getExprContext has before any real context exists.
type ExprContext interface {
	getDefault() StmtContext
}

type CallExprContext struct {
	u8StringArgOK     map[int]bool
	useGoStringArg    map[int]bool
	argTypeIsPtr      map[int]bool
	interfaceTypes    map[int]types.Type
	// emptyInterfaceArgs marks arguments whose PARAMETER is a real empty interface (`any`):
	// a func-LITERAL argument there is natural-typed by C# (no delegate target), so its Go
	// result type must be stated explicitly or inference picks the arms' literal type
	// (`func(x int) int { return 0 }` inferred Func<nint, int> — Go int32! — collapsing
	// distinct Go func types under reflection; testing/quick's TestFailure #3). See convFuncLit.
	emptyInterfaceArgs map[int]bool
	// genericResultInferredFuncArgs marks func-LITERAL arguments whose DECLARED parameter type
	// is a signature with a type parameter in its RESULT list (`OnceValue[T any](f func() T)`).
	// C# must infer that type argument FROM THE LAMBDA'S RETURN TYPE, so the arms' natural C#
	// type — not the declared Go result — decides it; a body that returns nothing inferable
	// (`panic`, `return nil`) fails outright (CS0411), and one whose natural type merely differs
	// (`return 42` → `Func<int>` where Go says `Func<nint>`) infers the WRONG delegate (CS0029).
	// convFuncLit states the declared result type explicitly for these. A type parameter that
	// appears only in the PARAMETER list (slices.SortFunc's `cmp func(a, b E) int`) is inferred
	// from the lambda's own typed parameters and is deliberately not marked (no churn).
	genericResultInferredFuncArgs map[int]bool
	hasSpreadOperator bool
	keyValueSource    KeyValueSource
	keyValueIdent     *ast.Ident
	// keyValueArrayBacked marks a keyed composite that is backed by a C# array/SparseArray (an
	// indexed slice/array literal, `[]T{i: v}`), not a real map. Its indexer takes a Go `int`
	// (nint), so a key whose Go type is a defined integer type must be cast to int (a `num:nint`
	// key like runtime's `lockRank` cannot implicitly narrow to the indexer type otherwise).
	keyValueArrayBacked bool
	// keyValueCompositeType carries the keyed composite's resolved type through to
	// KeyValueContext.compositeType (see that field's note; runtime/metrics CS1739).
	keyValueCompositeType types.Type
	forceMultiLine        bool
	sourceIsRuneArray     bool
	sourceIsTypeParams    bool
	callArgs              []string
	replacementArgs       []string
	castArgToType         map[int]string
	// wrapArgWithNew wraps the indexed argument in a constructor call (`new slice<E>(arg)`) — the
	// S-where-[]E-expected materialization (see convExprList).
	wrapArgWithNew map[int]string
	// wrapArgWithLambda re-wraps a FUNC-typed argument (a method group / func value) as a lambda
	// `(p0, p1) => value(p0, p1)` so the enclosing delegate's return/param positions can apply the
	// user-defined implicit conversion a C# method-group conversion won't (a constraint-proxy
	// `func() Point` field assigned `nistec.NewP224Point`, whose ж<P224Point> return needs the
	// proxy — CS0407). The map value is the comma-joined lambda parameter list ("" for niladic).
	wrapArgWithLambda map[int]string
	// cloneArrayArg appends the strongly-typed `.Clone()` to the indexed element — a POSITIONAL
	// composite-literal element that reads an ARRAY value out of existing storage (Go copies the
	// array into the composite's slot; the emitted struct copy would alias its backing — see
	// exprReadsValueNeedingClone). Applied before any interface conversion so an `any`/
	// interface slot boxes the clone. Keyed elements clone in convKeyValueExpr instead.
	cloneArrayArg map[int]bool
	// deferredDecls hoists a func-literal argument's capture declarations out of the call's
	// argument list (where a `var mʗ1 = m;` statement is invalid C#) up to the enclosing
	// statement. Threaded from the statement emitter (visitExprStmt/visitAssignStmt) through
	// convCallExpr → convExprList → the func-literal arg's convFuncLit. Nil when not hoisting.
	deferredDecls *strings.Builder
}

func DefaultCallExprContext() *CallExprContext {
	return &CallExprContext{
		u8StringArgOK:       make(map[int]bool),
		useGoStringArg:      make(map[int]bool),
		argTypeIsPtr:        make(map[int]bool),
		interfaceTypes:      make(map[int]types.Type),
		hasSpreadOperator:   false,
		keyValueSource:      StructSource,
		keyValueIdent:       nil,
		keyValueArrayBacked: false,
		forceMultiLine:      false,
		sourceIsRuneArray:   false,
		sourceIsTypeParams:  false,
		callArgs:            nil,
		replacementArgs:     nil,
		castArgToType:       nil,
		deferredDecls:       nil,
	}
}

// getDefault is what makes *CallExprContext satisfy ExprContext, and that conformance is
// load-bearing even though nothing calls this method: convExprList builds
// `[]ExprContext{basicLitContext, identContext, keyValueContext, lambdaContext, callContext}`, and
// the call context could not go into that slice without it. Dead-code analyzers cannot see
// interface satisfaction as a use, so they flag this — keep it.
//
// Unlike its ten siblings it is never reached THROUGH the dispatcher: no code instantiates
// getExprContext[CallExprContext], because callers that want the call context already hold it as a
// typed *CallExprContext parameter. (It could not work that way either — getExprContext calls
// getDefault on a zero value, and the zero *CallExprContext is nil, which this value receiver
// would dereference.) The body still returns a real default so the method is honest rather than a
// panic waiting to happen.
func (c CallExprContext) getDefault() StmtContext {
	return DefaultCallExprContext()
}

type BasicLitContext struct {
	u8StringOK        bool
	sourceIsRuneArray bool
	castToGoString    bool

	// spanTargetUnsupported marks a slot that cannot accept a bare `ReadOnlySpan<byte>` value —
	// an `any`/interface (object) position, a ValueTuple element, an attribute argument, panic's
	// object parameter. It is the signal convBinaryExpr uses to suppress the `u8` form inside a
	// string CONCATENATION, whose RESULT lands in that slot: two u8 literal operands fold to a
	// single span (C# folds utf8 literal constants), which then has no boxing conversion to
	// object — `print("\n" + "\t")` in runtime's newstack diagnostics is CS1503. It was formerly
	// derived from `!u8StringOK`, which coupled the two: flipping a site's literal rendering to
	// the combined `(@string)"…"u8` form (which those slots DO accept, since the cast makes it an
	// @string) would silently re-enable u8 inside its concats and break the build. The two are now
	// independent — the literal flags say how a STANDALONE literal renders, this says whether the
	// slot tolerates a span — and every site that renders a literal for a span-hostile slot sets it.
	spanTargetUnsupported bool
}

func DefaultBasicLitContext() BasicLitContext {
	return BasicLitContext{
		u8StringOK:            true,
		sourceIsRuneArray:     false,
		castToGoString:        false,
		spanTargetUnsupported: false,
	}
}

func (c BasicLitContext) getDefault() StmtContext {
	return DefaultBasicLitContext()
}

type ArrayTypeContext struct {
	compositeInitializer bool
	indexedInitializer   bool
	maxLength            int
}

func DefaultArrayTypeContext() ArrayTypeContext {
	return ArrayTypeContext{
		compositeInitializer: false,
		indexedInitializer:   false,
		maxLength:            0,
	}
}

func (c ArrayTypeContext) getDefault() StmtContext {
	return DefaultArrayTypeContext()
}

type LambdaContext struct {
	isAssignment  bool
	isCallExpr    bool
	renderParams  bool
	isPointerCast bool
	// untypedInterfaceTarget marks a func literal converted into an `any` parameter slot: it is
	// natural-typed by C# (no delegate target type), so convFuncLit states its Go result type
	// explicitly (see CallExprContext.emptyInterfaceArgs).
	untypedInterfaceTarget bool
	// genericResultInferenceTarget marks a func literal whose result type a generic callee's
	// type argument is inferred FROM (see CallExprContext.genericResultInferredFuncArgs).
	genericResultInferenceTarget bool
	deferredDecls *strings.Builder
	callArgs      []string
	// isIIFE marks an immediately-invoked, no-argument function literal — emitted as a
	// `func((defer, recover) => body)` execution-context call so it runs with its OWN
	// defer/recover scope (a bare C# lambda cannot be invoked directly, and an inner defer
	// must not bind to the enclosing function's wrapper). See convCallExpr / convFuncLit.
	isIIFE bool
	// deferOrGoCall marks a call that is the target of a defer/go statement. Such a
	// `defer func(){…}()` / `go func(){…}()` literal must NOT be treated as an IIFE — the
	// deferred/goroutine body is inlined by visitDeferStmt/visitGoStmt.
	deferOrGoCall bool
	// deferCall marks specifically a defer-statement target. A deferred closure's recover()
	// recovers the *enclosing* function (which is itself wrapped, since it contains the defer
	// statement), so the closure must NOT get its own func() execution context. (A goroutine or
	// assigned closure is independent and does get one when it uses defer/recover.)
	deferCall bool
	// suppressGenericTypeArgs tells convSelectorExpr NOT to append a generic function's inferred
	// type arguments (the method-group-value path). Set when the selector is the BASE (X) of an
	// explicit-instantiation IndexExpr (`pkg.Func[T]`): convIndexExpr renders the `<T>` itself, so
	// convSelectorExpr appending them too produced `pkg.Func<T><T>` (CS1525/CS0119/CS8124 across
	// encoding/gob/xml/asn1/json, text/template, debug/*, unique).
	suppressGenericTypeArgs bool
}

func DefaultLambdaContext() LambdaContext {
	return LambdaContext{
		isAssignment:  false,
		isCallExpr:    false,
		renderParams:  false,
		isPointerCast: false,
		deferredDecls: nil,
		callArgs:      nil,
		isIIFE:        false,
	}
}

func (c LambdaContext) getDefault() StmtContext {
	return DefaultLambdaContext()
}

type UnaryExprContext struct {
	isTupleResult bool

	// deferredDecls threads the enclosing statement's hoist target through `&composite`
	// operands so a func-literal FIELD value's capture decls hoist (elf file.go's
	// `&readSeekerFromReader{reset: func() {…}}` in return position, CS1003 ×6).
	deferredDecls *strings.Builder
}

func DefaultUnaryExprContext() UnaryExprContext {
	return UnaryExprContext{
		isTupleResult: false,
	}
}

func (c UnaryExprContext) getDefault() StmtContext {
	return DefaultUnaryExprContext()
}

type IndexExprContext struct {
	// isTupleResult marks a map index used in comma-ok form (`v, ok := m[k]`), so it is
	// emitted via golib's two-value indexer `m[key, ꟷ]` (returning `(value, present)`).
	isTupleResult bool
	// isAssignmentTarget marks an index expression that IS the assignment LHS
	// (`req.Header[k] = vv`): its BASE converts in assignment context, so a pointer
	// auto-deref takes the writable `.Value` path. The read form `(~req).Header[k] = vv`
	// indexer-sets a map-wrapper FIELD of an rvalue struct copy (CS0131, net/http client's
	// redirect loop). Distinct from LambdaContext.isAssignment, which also rides along RHS
	// conversions of an assignment statement — index READS there must keep the deref form.
	isAssignmentTarget bool
}

func DefaultIndexExprContext() IndexExprContext {
	return IndexExprContext{
		isTupleResult:      false,
		isAssignmentTarget: false,
	}
}

func (c IndexExprContext) getDefault() StmtContext {
	return DefaultIndexExprContext()
}

type IdentContext struct {
	// isField marks a FIELD selection (vs a method/function name) — fields skip the
	// function-name Main→ΔMain special (see convIdent's isMethod arm).
	isField bool

	isPointer bool
	isType    bool
	isMethod  bool
	ident     *ast.Ident
	// fieldCollidesWithType marks a struct-field selector whose name equals its enclosing
	// struct's type name. C# forbids a member sharing the enclosing type's name (CS0542), so
	// the field is emitted with the disambiguation marker (matching its renamed declaration).
	fieldCollidesWithType bool
	// fieldTypeIsRenamed marks that the field's enclosing type is itself Δ-renamed for a
	// type-vs-method collision in ITS OWN package (a FOREIGN such type, invisible to the current
	// package's nameCollisions). The field access then DOUBLES the marker to match the
	// declaration's ΔΔ form (see typeCollidingFieldName / fieldTypeIsRenamed).
	fieldTypeIsRenamed bool
}

func DefaultIdentContext() IdentContext {
	return IdentContext{
		isPointer:             false,
		isType:                false,
		isMethod:              false,
		ident:                 nil,
		fieldCollidesWithType: false,
		fieldTypeIsRenamed:    false,
	}
}

func (c IdentContext) getDefault() StmtContext {
	return DefaultIdentContext()
}

type KeyValueContext struct {
	// deferredDecls threads the enclosing statement's hoist target into a KEYED composite
	// VALUE's conversion — a func-literal field value with captures otherwise dumps its
	// snapshot decls INLINE in the argument list (elf file.go's readSeekerFromReader{reset:
	// func() {…zrd…}}, CS1003 syntax cascade ×6).
	deferredDecls *strings.Builder

	source      KeyValueSource
	ident       *ast.Ident
	arrayBacked bool

	// compositeType carries the keyed composite literal's resolved type so a struct-field key
	// can detect the field-named-like-its-own-type collision (the declaration applies
	// typeCollidingFieldName; the keyed ctor argument must match — runtime/metrics CS1739).
	compositeType types.Type
}

func DefaultKeyValueContext() KeyValueContext {
	return KeyValueContext{
		source:      StructSource,
		ident:       nil,
		arrayBacked: false,
	}
}

func (c KeyValueContext) getDefault() StmtContext {
	return DefaultKeyValueContext()
}

// Handles pattern match expressions, e.g.: "x is 1 or > 3"
type PatternMatchExprContext struct {
	usePattenMatch bool
	declareIsExpr  bool
}

func DefaultPatternMatchExprContext() PatternMatchExprContext {
	return PatternMatchExprContext{
		usePattenMatch: false,
		declareIsExpr:  false,
	}
}

func (c PatternMatchExprContext) getDefault() StmtContext {
	return DefaultPatternMatchExprContext()
}

type StarExprContext struct {
	inParenExpr bool
	inLhsAssign bool
}

func DefaultStarExprContext() StarExprContext {
	return StarExprContext{
		inParenExpr: false,
		inLhsAssign: false,
	}
}

func (c StarExprContext) getDefault() StmtContext {
	return DefaultStarExprContext()
}

func getExprContext[TContext ExprContext](contexts []ExprContext) TContext {
	var zeroValue TContext

	if len(contexts) == 0 {
		return zeroValue.getDefault().(TContext)
	}

	for _, context := range contexts {
		if context != nil {
			if targetContext, ok := context.(TContext); ok {
				return targetContext
			}
		}
	}

	return zeroValue.getDefault().(TContext)
}

func (v *Visitor) convExpr(expr ast.Expr, contexts []ExprContext) string {
	switch exprType := expr.(type) {
	case *ast.ArrayType:
		context := getExprContext[ArrayTypeContext](contexts)
		return v.convArrayType(exprType, context)
	case *ast.BasicLit:
		// Tier C: a string literal the whole-package hoist pre-pass resolved to a package-scoped
		// `static readonly` field renders as that field's NAME — a pure substitution, since the
		// pre-pass already proved the slot accepts an @string (or, pre-boxed, an object). Every
		// other literal renders exactly as it always has. See hoistedLiteralOperations.go.
		if name := hoistedLiteralName(exprType); name != "" {
			return name
		}

		context := getExprContext[BasicLitContext](contexts)
		return v.convBasicLit(exprType, context)
	case *ast.BinaryExpr:
		context := getExprContext[PatternMatchExprContext](contexts)
		litContext := getExprContext[BasicLitContext](contexts)
		return v.convBinaryExpr(exprType, context, litContext)
	case *ast.CallExpr:
		context := getExprContext[LambdaContext](contexts)
		return v.convCallExpr(exprType, context)
	case *ast.ChanType:
		return v.convChanType(exprType)
	case *ast.CompositeLit:
		context := getExprContext[KeyValueContext](contexts)

		// Adopt the ambient LambdaContext's hoist target when the KeyValueContext carries
		// none — a `&composite` in RETURN position arrives without a KeyValueContext, and a
		// func-literal FIELD value's capture decls otherwise dump inline (elf file.go's
		// `&readSeekerFromReader{reset: func() {…}}`, CS1003 cascade ×6).
		if context.deferredDecls == nil {
			if lambdaContext := getExprContext[LambdaContext](contexts); lambdaContext.deferredDecls != nil {
				context.deferredDecls = lambdaContext.deferredDecls
			}
		}

		return v.convCompositeLit(exprType, context)
	case *ast.FuncLit:
		context := getExprContext[LambdaContext](contexts)
		return v.convFuncLit(exprType, context)
	case *ast.FuncType:
		// A func TYPE in expression position — the target of a conversion like
		// `(func())(nil)` (reflect FuncOf's prototype) — renders as its C# delegate type name.
		return convertToCSTypeName(v.getExprTypeName(exprType, false))
	case *ast.Ident:
		context := getExprContext[IdentContext](contexts)
		rendered := v.convIdent(exprType, context)

		// A BigInteger-backed untyped const reference carries no implicit conversion to a
		// built-in numeric type, so a concrete numeric context must cast it — everywhere, not
		// just in the comparison arm that used to be its only casting consumer.
		if cast := v.bigIntegerConstMaterialization(exprType, rendered); cast != "" {
			return cast
		}

		return rendered
	case *ast.IndexExpr:
		context := getExprContext[IndexExprContext](contexts)
		return v.convIndexExpr(exprType, context)
	case *ast.IndexListExpr:
		return v.convIndexListExpr(exprType)
	case *ast.KeyValueExpr:
		context := getExprContext[KeyValueContext](contexts)
		return v.convKeyValueExpr(exprType, context)
	case *ast.MapType:
		return v.convMapType(exprType)
	case *ast.ParenExpr:
		context := getExprContext[LambdaContext](contexts)
		litContext := getExprContext[BasicLitContext](contexts)
		return v.convParenExpr(exprType, context, litContext)
	case *ast.SelectorExpr:
		context := getExprContext[LambdaContext](contexts)
		rendered := v.convSelectorExpr(exprType, context)

		// A package-qualified BigInteger-backed const (`math.MaxUint64 * 2`-class) takes the
		// same concrete-context cast as the bare-ident form above.
		if cast := v.bigIntegerConstMaterialization(exprType, rendered); cast != "" {
			return cast
		}

		return rendered
	case *ast.SliceExpr:
		return v.convSliceExpr(exprType)
	case *ast.StarExpr:
		context := getExprContext[StarExprContext](contexts)
		return v.convStarExpr(exprType, context)
	case *ast.TypeAssertExpr:
		return v.convTypeAssertExpr(exprType)
	case *ast.StructType:
		context := getExprContext[IdentContext](contexts)
		return v.convStructType(exprType, context)
	case *ast.InterfaceType:
		context := getExprContext[IdentContext](contexts)
		return v.convInterfaceType(exprType, context)
	case *ast.UnaryExpr:
		context := getExprContext[UnaryExprContext](contexts)

		// Adopt the ambient hoist target (see UnaryExprContext.deferredDecls).
		if context.deferredDecls == nil {
			if lambdaContext := getExprContext[LambdaContext](contexts); lambdaContext.deferredDecls != nil {
				context.deferredDecls = lambdaContext.deferredDecls
			}
		}

		return v.convUnaryExpr(exprType, context)
	case *ast.BadExpr:
		v.showWarning("@convExpr - BadExpr encountered: %#v", exprType)
		return ""
	default:
		panic(fmt.Sprintf("@convExpr - Unexpected Expr type: %#v", v.getPrintedNode(exprType)))
	}
}
