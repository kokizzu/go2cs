// syscallKeepAliveAnalysis.go - Gbtc
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

// syscallFunnelFuncNames names the syscall-package functions Go's own compiler marks
// uintptrkeepalive: a uintptr argument derived from a pointer is only guaranteed to name live
// storage for calls into exactly this set (see unsafe.Pointer's own doc comment, rule (4), which
// the converted unsafe.cs carries verbatim). Everywhere else — including a plain call recorded on
// a variable first — Go itself gives no such guarantee, so this converter fix reproduces exactly
// Go's own contract rather than inventing a broader one.
//
// go2cs's syscalln (src/core/syscall/windows/dll_windows.cs) dispatches through a raw
// delegate*-unmanaged calli with no marshaling layer of its own, which is what makes the managed
// side responsible for honoring this contract explicitly — the .NET P/Invoke marshaler would
// normally do it for a POINTER-typed parameter, but every argument here arrives as a bare
// numeric uintptr, indistinguishable from any other integer once computed.
var syscallFunnelFuncNames = map[string]bool{
	"Syscall": true, "Syscall6": true, "Syscall9": true,
	"Syscall12": true, "Syscall15": true, "Syscall18": true,
	"SyscallN": true,
}

// syscallFunnelCall reports whether callExpr calls one of the syscall-package funnel functions —
// resolved via go/types, matching flag.Set's own `fn.Pkg().Path()+"."+fn.Name()` idiom
// (testConversion.go) rather than a name-only guess, so a same-named function in an unrelated
// package can never false-match.
func syscallFunnelCall(info *types.Info, callExpr *ast.CallExpr) bool {
	var ident *ast.Ident

	switch fun := callExpr.Fun.(type) {
	case *ast.Ident:
		ident = fun
	case *ast.SelectorExpr:
		ident = fun.Sel
	default:
		return false
	}

	fn, ok := info.Uses[ident].(*types.Func)

	return ok && fn.Pkg() != nil && fn.Pkg().Path() == "syscall" && syscallFunnelFuncNames[fn.Name()]
}

// pointerDerivedArgSource returns the innermost operand X of the `uintptr(unsafe.Pointer(X))`
// shape — Go's own mksyscall-generated idiom for passing a pointer into the syscall funnel, and
// the ONLY shape a real Go program can use to hand a *T to a uintptrkeepalive function while
// staying inside the language's own rules (unsafe.Pointer's doc comment, rule (4), requires the
// conversion to appear "in" the call's argument list — never through an intermediate variable).
// Returns nil for anything else: a plain integer argument (`uintptr(access)`), an
// already-computed uintptr passed through a variable (already outside Go's own guarantee, so
// outside this fix's job to protect), or any other shape.
//
// X — not the intermediate unsafe.Pointer(X) conversion — is what must be captured: the
// converter's existing peephole (markDeadUnsafePointerBox, convCallExpr.go) already elides the
// unsafe.Pointer wrapper for exactly this pattern and converts X directly to the box that ends up
// cast to uintptr, so X's own converted form is the actual value whose lifetime needs extending.
func pointerDerivedArgSource(info *types.Info, arg ast.Expr) ast.Expr {
	outer, ok := arg.(*ast.CallExpr)

	if !ok || len(outer.Args) != 1 || !exprNamesType(info, outer.Fun, "", "uintptr") {
		return nil
	}

	inner, ok := outer.Args[0].(*ast.CallExpr)

	if !ok || len(inner.Args) != 1 || !exprNamesType(info, inner.Fun, "unsafe", "Pointer") {
		return nil
	}

	return inner.Args[0]
}

// exprNamesType reports whether expr — a bare identifier or a qualified selector — resolves to
// the type pkgPath.typeName. pkgPath == "" matches a universe-scope type (uintptr has no
// package); anything else matches a package-scoped type (unsafe.Pointer).
func exprNamesType(info *types.Info, expr ast.Expr, pkgPath, typeName string) bool {
	var ident *ast.Ident

	switch e := expr.(type) {
	case *ast.Ident:
		ident = e
	case *ast.SelectorExpr:
		ident = e.Sel
	default:
		return false
	}

	tn, ok := info.Uses[ident].(*types.TypeName)

	if !ok || tn.Name() != typeName {
		return false
	}

	if pkgPath == "" {
		return tn.Pkg() == nil
	}

	return tn.Pkg() != nil && tn.Pkg().Path() == pkgPath
}

// syscallFunnelUintptrCastPrefix is the exact leading text the GENERAL conversion path renders for
// a `uintptr(unsafe.Pointer(X))` argument: convCallExpr's basic-numeric-conversion tail casts a
// call/address-of operand with no extra parens of its own (targetIsBasic, needsParentheses and
// castOperandNeedsParens both false for the call/&-shaped operands this pattern always has), and
// the inner unsafe.Pointer(X) conversion's own peephole (markDeadUnsafePointerBox) elides its
// wrapper so the box stands alone — confirmed against real corpus output (`(uintptr)Ꮡsystemname`).
// convSyscallFunnelCall strips exactly this prefix to recover the box expression; see its own
// comment for why the whole argument is converted rather than the inner operand in isolation.
const syscallFunnelUintptrCastPrefix = "(uintptr)"

// convSyscallFunnelCall emits a call to one of the syscall-package funnel functions with each
// pointer-derived argument (pointerDerivedArgSource) routed through a statement-scoped temp
// holding the box itself, cast to uintptr at the call site — reproducing Go's own uintptrkeepalive
// contract (the temp is what visitStmt's drainSyscallKeepAlive keeps alive after the statement,
// via v.pendingSyscallKeepAlive) rather than converting straight to a bare uintptr with nothing
// left referencing the box that produced it. Every other argument converts exactly as the general
// call path would.
//
// The box expression is recovered by converting the WHOLE `uintptr(unsafe.Pointer(X))` argument
// through the GENERAL path (v.convExpr(arg, nil) — the same call the non-pointer-derived branch
// below already makes for every other argument) and stripping the leading cast
// (syscallFunnelUintptrCastPrefix) its own peephole guarantees, rather than converting X in
// isolation. An earlier version of this function took the second, narrower path and broke on a
// boxed VALUE parameter whose prologue rebinds it to a dereferenced ref-local before the funnel
// call is ever reached (internal/syscall/windows/registry's RegCreateKeyExW: `ref var sa = ref
// Ꮡsa.DerefOrNull();`) — converting the bare Go-source identifier `sa` in isolation picked up the
// REBOUND VALUE the ref-local now names, not the box (CS0030: `SecurityAttributes` → `uintptr`).
// Routing through the whole argument reuses whatever context-dependent rebinding the general path
// already applies to X, so that class of mismatch cannot recur silently: a genuinely unexpected
// shape now panics (below) instead of quietly capturing the wrong value — the loud failure this
// design specifically trades the narrow bug for.
func (v *Visitor) convSyscallFunnelCall(callExpr *ast.CallExpr) string {
	calleeIdentContext := DefaultIdentContext()
	calleeIdentContext.suppressGenericTypeArgs = true
	funcExpr := v.convExpr(callExpr.Fun, []ExprContext{calleeIdentContext})

	args := make([]string, len(callExpr.Args))

	for i, arg := range callExpr.Args {
		source := pointerDerivedArgSource(v.info, arg)

		if source == nil {
			args[i] = v.convExpr(arg, nil)
			continue
		}

		fullExpr := v.convExpr(arg, nil)

		if !strings.HasPrefix(fullExpr, syscallFunnelUintptrCastPrefix) {
			panic(fmt.Sprintf(
				"@convSyscallFunnelCall - pointer-derived argument %d (%s) did not render with the expected %q cast prefix (got %q) — markDeadUnsafePointerBox's peephole contract has changed underneath this emission",
				i, v.getPrintedNode(arg), syscallFunnelUintptrCastPrefix, fullExpr,
			))
		}

		boxExpr := fullExpr[len(syscallFunnelUintptrCastPrefix):]
		tempName := fmt.Sprintf("ᴋ%d", v.syscallKeepAliveCounter)
		v.syscallKeepAliveCounter++

		if v.hoistedDecls != nil {
			v.hoistedDecls.WriteString(fmt.Sprintf("%s%svar %s = %s;", v.newline, v.indent(v.indentLevel), tempName, boxExpr))
		}

		v.pendingSyscallKeepAlive = append(v.pendingSyscallKeepAlive, tempName)
		args[i] = fmt.Sprintf("(uintptr)%s", tempName)
	}

	// The hoisted-decls convention is a LEADING newline+indent per entry (see visitExprStmt's own
	// comment on its hoistBuf), so the statement that follows — assembled by the caller from this
	// function's return value — needs its OWN leading newline+indent too, or it lands glued onto
	// this call's last temp declaration on one line. Every other hoistedDecls producer in the
	// corpus is a func-literal capture, always followed by more expression text on the SAME line
	// (an argument list, a lambda body) where that would be correct; a funnel call is always the
	// whole RHS of a standalone statement, the one shape where the following text is a new
	// statement in its own right.
	if v.hoistedDecls != nil && len(v.pendingSyscallKeepAlive) > 0 {
		v.hoistedDecls.WriteString(v.newline + v.indent(v.indentLevel))
	}

	return fmt.Sprintf("%s(%s)", funcExpr, strings.Join(args, ", "))
}

// drainSyscallKeepAlive emits `GC.KeepAlive(temp);` for every temp convSyscallFunnelCall recorded
// while converting the statement visitStmt just finished, and clears the pending list — called
// unconditionally after both visitAssignStmt and visitExprStmt so a syscall-funnel call reached
// through either statement shape is covered, and so a statement that recorded none leaves nothing
// behind for the next one to misattribute.
func (v *Visitor) drainSyscallKeepAlive() {
	if len(v.pendingSyscallKeepAlive) == 0 {
		return
	}

	for _, tempName := range v.pendingSyscallKeepAlive {
		v.outputBuilder.WriteString(fmt.Sprintf("%s%sSystem.GC.KeepAlive(%s);", v.newline, v.indent(v.indentLevel), tempName))
	}

	v.pendingSyscallKeepAlive = nil
}
