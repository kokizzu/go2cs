// goFrameOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/types"
)

// A Go function that defers or recovers is emitted with its body INLINE inside try/catch/finally,
// beside a GoFrame local that holds this call's defer list:
//
//	internal static void Main() {
//	    GoFrame ᒐ = default;
//	    try {
//	        fmt.Println(openFileˢ);
//	        deferǃ(fmt.Println, closeFileˢ, ref ᒐ);
//	    }
//	    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
//	    finally { ᒐ.Run(); }
//	}
//
// It replaces the `func<T>((defer, recover) => …)` execution context, which modelled a catch, a
// finally and a defer list as an OBJECT owning the body. Owning the body forced the body to be a
// delegate; a delegate forced a display class for everything the body touched; and a display class
// forced the GoFunc<TRef1…TRef16> ladder for everything a delegate cannot capture. None of it was
// ever needed — try/catch/finally are STATEMENTS, and recover() reads a static thread-local rather
// than a handle on the frame — so only the defer LIST is genuinely per-call, and a ref struct holds
// it for free.
//
// Two consequences beyond the allocation, both of which the emitted text shows directly: the body
// captures NOTHING (a whole class of divergence where the lambda closed over variables the Go
// original never closed over cannot arise), and the deferred calls sit in the function that wrote
// them rather than one lambda level in.
//
// Full design, including the staged migration this predicate walks: docs/Phase4/DESIGN-closure-emission.md §4.

// goFrameLiteralStage is the §4.8 stage-3c dial. Function LITERALS keep the execution context until
// the declaration form has landed and been gated, so the corpus is never a mixture of forms in
// flight — the one shape that would genuinely break is a frame-form scope nested inside a
// lambda-form one, and staging declarations first makes that unreachable. Deleted with its use when
// the stage lands.
var goFrameLiteralStage = false

// goFrameName is the emitted GoFrame local; the rest of the frame vocabulary composes from it, so
// the whole shape moves with the one symbol.
func goFrameName() string {
	return GoFrameVar
}

// goFrameExceptionName is the emitted catch clause's exception variable.
func goFrameExceptionName() string {
	return GoFrameVar + "ex"
}

// goFramePanicName is the emitted catch filter's adopted-panic out variable.
func goFramePanicName() string {
	return GoFrameVar + "p"
}

// goFrameExitLabel is the label a named-result function's early `return` jumps to (§4.4): the
// results are declared before the try and returned after the finally, so an exit from inside the
// try has to leave through a goto, which runs the finally exactly as a return would.
func goFrameExitLabel() string {
	return GoFrameVar + "done"
}

// goFrameEligible reports whether this function's defer/recover scope is emitted as a GoFrame
// rather than as the `func((defer, recover) => …)` execution context. It is the migration's dial:
// each stage of DESIGN-closure-emission.md §4.8 widens it by removing one clause, and every clause
// removed is a lowering rule that has been written and gated. hasDefer/hasRecover/
// namedReturnDeferMode must already be set for THIS function.
func (v *Visitor) goFrameEligible(funcDecl *ast.FuncDecl, signature *types.Signature) bool {
	if funcDecl == nil || funcDecl.Body == nil || signature == nil {
		return false
	}

	if !(v.hasDefer || v.hasRecover) {
		return false
	}

	// --- MIGRATION GATES (§4.8). Each is removed by the stage that lands its lowering rule. ---

	// Stage 3a: recover(). The body's `recover()` currently binds the wrapper's `Recover`
	// parameter; under the frame it resolves to the static builtin.recover(), which reads the same
	// thread-local slot the emitted catch parks the panic in.
	if v.hasRecover {
		return false
	}

	// Stage 3b: named results (§4.4). Go runs the deferred calls AFTER the result params are
	// assigned and BEFORE the caller sees them, which a C# `finally` cannot do to a value a
	// `return` has already evaluated — so the results are declared before the try and every exit
	// leaves through a goto to a label after the finally.
	if v.namedReturnDeferMode {
		return false
	}

	// --- END MIGRATION GATES ---

	// A DEFERRED function literal that contains a `defer` of its OWN. Go scopes that inner defer
	// to the literal, but the literal is a deferred-call target, so convFuncLit deliberately gives
	// it no defer scope (its recover() belongs to the enclosing function, which is the case that
	// rule exists for) — and the inner registration therefore lands in the ENCLOSING function's
	// scope, silently running at the wrong time. Under the frame form it cannot even do that: the
	// frame is a ref struct, which a lambda cannot capture, so the same shape is a compile error
	// instead of a wrong answer. Neither is acceptable, so the shape keeps the lambda form until
	// the literal can carry its own frame (§4.8 stage 3c). Measured across the whole converted
	// corpus (4,951 emitted files) the shape occurs ZERO times, so this costs nothing today and
	// exists to protect a converted end-user program during the migration window.
	if deferredLiteralHasOwnDefer(funcDecl.Body) {
		return false
	}

	return true
}

// litGoFrameEligible is goFrameEligible for a function LITERAL. A literal's frame lives in the
// lambda (or local function) the literal emits as, which is an ordinary scope for a ref-struct
// LOCAL — only capturing one is forbidden, and an inline body captures nothing. The migration gates
// mirror the declaration's, so a literal never runs ahead of the stage that landed its rule.
func (v *Visitor) litGoFrameEligible(litSig *types.Signature, hasDefer, hasRecover, namedDefer bool, funcLit *ast.FuncLit) bool {
	// MIGRATION GATE (§4.8 stage 3c) — see goFrameLiteralStage.
	if !goFrameLiteralStage {
		return false
	}

	if litSig == nil || funcLit == nil || funcLit.Body == nil {
		return false
	}

	if !(hasDefer || hasRecover) {
		return false
	}

	// See goFrameEligible: held back until a deferred literal can carry its own frame.
	if deferredLiteralHasOwnDefer(funcLit.Body) {
		return false
	}

	_ = namedDefer

	return true
}

// deferredLiteralHasOwnDefer reports whether the body contains `defer func(){ … defer … }()` — a
// deferred function literal that itself defers. See goFrameEligible for why that shape is held
// back.
func deferredLiteralHasOwnDefer(body *ast.BlockStmt) bool {
	found := false

	ast.Inspect(body, func(n ast.Node) bool {
		if found {
			return false
		}

		deferStmt, ok := n.(*ast.DeferStmt)

		if !ok {
			return true
		}

		funcLit, ok := deferStmt.Call.Fun.(*ast.FuncLit)

		if !ok {
			return true
		}

		ast.Inspect(funcLit.Body, func(inner ast.Node) bool {
			if found {
				return false
			}

			// A literal nested INSIDE this one owns its own defers, so stop at it.
			if _, isLit := inner.(*ast.FuncLit); isLit {
				return false
			}

			if _, isDefer := inner.(*ast.DeferStmt); isDefer {
				found = true
				return false
			}

			return true
		})

		return true
	})

	return found
}

// goFrameHead renders the text that replaces the execution-context marker — everything between the
// signature's closing paren and the body block, which the body then opens with ` {`.
// namedResultDecls is the named-result declaration block (§4.4), each line already newline-led, or
// empty for the unnamed-result form: those results are declared BEFORE the try because deferred
// code mutates them and the exit after the finally reads them back.
func (v *Visitor) goFrameHead(indentLevel int, namedResultDecls string) string {
	return fmt.Sprintf(" {%s%s%sGoFrame %s = default;%s%stry",
		namedResultDecls,
		v.newline, v.indent(indentLevel+1), goFrameName(),
		v.newline, v.indent(indentLevel+1))
}

// goFrameTail renders the catch, the finally and the closing brace of a frame-form function body.
// resultsZeroValue is the `return` the catch arm ends with for a value-returning function whose
// results are UNNAMED: a recovered panic returns Go's zero results, and an UNrecovered one never
// reaches the return because Run() re-throws from the finally. It is empty for a void function and
// for the named-result form, whose exit runs through the label instead.
func (v *Visitor) goFrameTail(indentLevel int, catchReturn string) string {
	if catchReturn != "" {
		catchReturn = " " + catchReturn
	}

	return fmt.Sprintf("%s%scatch (Exception %s) when (GoFrame.IsPanic(%s, out PanicException? %s)) { GoFrame.Capture(%s);%s }%s%sfinally { %s.Run(); }",
		v.newline, v.indent(indentLevel+1),
		goFrameExceptionName(), goFrameExceptionName(), goFramePanicName(), goFramePanicName(), catchReturn,
		v.newline, v.indent(indentLevel+1), goFrameName())
}
