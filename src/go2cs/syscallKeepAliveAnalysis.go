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

// syscallFunnelFuncNames names the funnel functions whose uintptr arguments must be treated as
// live pointers. The set is Go's own `//go:uintptrkeepalive` set — a uintptr argument derived from
// a pointer is only guaranteed to name live storage for calls into exactly this set (see
// unsafe.Pointer's own doc comment, rule (4), which the converted unsafe.cs carries verbatim);
// everywhere else — including a plain call recorded on a variable first — Go itself gives no such
// guarantee, so this converter fix reproduces Go's own contract rather than inventing a broader
// one. Censused against GOROOT (go1.23.12, `//go:uintptrkeepalive` outside cmd/) on 2026-09-02:
// exactly four declarations, all in package syscall — RawSyscall, RawSyscall6 (syscall_linux.go:50,
// 58) and Syscall, Syscall6 (:69, 91).
//
// Two members are here for a go2cs-specific reason Go's directive set does not have to state, and
// both rest on the same fact: the CLR heap MOVES, where Go's does not. Go needs uintptrkeepalive
// only against STACK COPYING (which is why its funnels are also //go:nosplit and why an assembly
// funnel needs no directive at all); a managed address handed across a native boundary here is
// pinned for the LIFETIME OF THE BOX that produced it (golib `ж<T>.EnsureStableAddress`, whose
// GCHandle is an instance field released with the box), so an unheld box is a relocatable buffer
// the kernel may still be writing through. Hence:
//
//   - the Windows funnels Syscall9/12/15/18/N, which carry no Go directive: go2cs's syscalln
//     (src/core/syscall/windows/dll_windows.cs) dispatches through a raw delegate*-unmanaged calli
//     with no marshaling layer of its own, so the managed side is responsible for honoring the
//     contract explicitly — the .NET P/Invoke marshaler would normally do it for a POINTER-typed
//     parameter, but every argument here arrives as a bare numeric uintptr, indistinguishable from
//     any other integer once computed;
//   - internal/runtime/syscall.Syscall6 (see syscallFunnelPackagePaths), which carries no Go
//     directive because it is written in assembly — but in this corpus it is the BOTTOM of the
//     Linux boundary itself ([LibraryImport("libc","syscall")], reached by every syscall-package
//     funnel), and its own package calls it with the pointer-derived shape (EpollCtl,
//     internal/runtime/syscall/syscall_linux.go:36).
var syscallFunnelFuncNames = map[string]bool{
	"RawSyscall": true, "RawSyscall6": true,
	"Syscall": true, "Syscall6": true, "Syscall9": true,
	"Syscall12": true, "Syscall15": true, "Syscall18": true,
	"SyscallN": true,
}

// syscallFunnelPackagePaths names the packages whose members syscallFunnelFuncNames applies to.
// Matching on the PATH rather than on the name alone is what keeps a same-named function in an
// unrelated package from false-matching; both paths declare only funnels of this shape among these
// names (internal/runtime/syscall declares Syscall6 and nothing else the map names), so one shared
// name set covers both without a per-package table.
var syscallFunnelPackagePaths = map[string]bool{
	"syscall":                  true,
	"internal/runtime/syscall": true,
}

// syscallFunnelCall reports whether callExpr calls one of the funnel functions — resolved via
// go/types, matching flag.Set's own `fn.Pkg().Path()+"."+fn.Name()` idiom (testConversion.go)
// rather than a name-only guess, so a same-named function in an unrelated package can never
// false-match.
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

	return ok && fn.Pkg() != nil && syscallFunnelPackagePaths[fn.Pkg().Path()] && syscallFunnelFuncNames[fn.Name()]
}

// pointerDerivedArgSource returns the unsafe.Pointer-typed OPERAND of a `uintptr(...)` conversion
// standing in a funnel call's argument list — the shape Go's own rule (4) covers, and the shape
// whose lifetime this file extends.
//
// The predicate is the GO COMPILER'S, transcribed rather than paraphrased. cmd/compile decides
// uintptrkeepalive in escape.rewriteArgument (cmd/compile/internal/escape/call.go): an argument is
// rewritten into a kept-alive temp when it is an OCONVNOP whose operand type `IsUnsafePtr()` and
// whose own type `IsUintptr()` — a test on the operand's TYPE, with no requirement that the
// operand be a literal `unsafe.Pointer(X)` conversion. So BOTH of Go's own idioms are inside the
// guarantee:
//
//   - the inline form, `uintptr(unsafe.Pointer(&x))`, which mksyscall emits for a scalar argument;
//   - the TWO-STEP form, `_p0 = unsafe.Pointer(&p[0])` … `uintptr(_p0)`, which mksyscall emits for
//     every `[]byte` argument — 16 calls in GOROOT go1.23.12's syscall/zsyscall_linux_amd64.go and
//     13 in zsyscall_darwin_amd64.go, `read`, `write`, `pread`, `pwrite`, `recvfrom` and `sendto`
//     among them.
//
// This function matched only the first form until 2026-09-04, on a reading of rule (4) ("the
// conversion must appear in the argument list") that took "through an intermediate variable" to
// exclude the two-step form. What may not travel through a variable is the UINTPTR; the
// unsafe.Pointer may, and Go's own generated wrappers do exactly that. The consequence was
// measured rather than argued: every converted read and write handed the kernel a managed buffer
// address with nothing holding the box that pins it, and sixteen concurrent TLS connections over
// the converted stack died SIGSEGV in five seconds, 3/3.
//
// Returns nil for anything else — a plain integer argument (`uintptr(access)`), or an
// already-computed uintptr passed through a variable, which is outside Go's guarantee too (the
// compiler's test is on the operand being an unsafe POINTER) and so outside this fix's job.
//
// The returned node marks the argument as pointer-derived; convSyscallFunnelCall converts the
// WHOLE argument through the general path and recovers the value by stripping the rendered
// `(uintptr)` cast, so the operand is never converted in isolation here.
func pointerDerivedArgSource(info *types.Info, arg ast.Expr) ast.Expr {
	outer, ok := arg.(*ast.CallExpr)

	if !ok || len(outer.Args) != 1 || !exprNamesType(info, outer.Fun, "", "uintptr") {
		return nil
	}

	operand := outer.Args[0]
	operandType := info.TypeOf(operand)

	if operandType == nil {
		return nil
	}

	basic, ok := operandType.Underlying().(*types.Basic)

	if !ok || basic.Kind() != types.UnsafePointer {
		return nil
	}

	return operand
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

// convSyscallFunnelCall emits a call to one of the funnel functions (syscallFunnelCall) with each
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

// rejectDeferredSyscallKeepAlive panics if a DEFERRED or SPAWNED funnel call carries a
// pointer-derived argument — the one funnel shape convCallExpr's general path cannot carry, and
// the reason that fall-through is a rejection rather than a silent widening.
//
// The uintptrkeepalive contract this file implements is STATEMENT-scoped: the ᴋ temps
// convSyscallFunnelCall hoists are kept alive by drainSyscallKeepAlive, which visitStmt runs
// immediately after the statement it just converted. Under `defer`/`go` the STATEMENT is the
// defer/go statement while the CALL runs at unwind (or on another goroutine), so the emitted
// `GC.KeepAlive(ᴋN)` would fire before the syscall it protects — reintroducing, silently, exactly
// the lifetime bug the machinery exists to prevent. A defer-scoped keepalive is a different
// contract (the box must be captured BY the thunk and released after it returns), which is a
// design, not a patch.
//
// Go 1.23.12 contains no such call. Censused 2026-09-02 over the whole GOROOT: exactly two
// deferred funnel calls exist — runtime/memmove_linux_amd64_test.go:44 (`defer
// syscall.Syscall(syscall.SYS_MUNMAP, base+off, 65536, 0)`) and race/race_windows_test.go:33
// (`defer syscall.Syscall(VirtualFree.Addr(), 3, mem, 1<<20, MEM_RELEASE)`) — and neither has a
// pointer-derived argument; both pass integers only. There are no spawned ones. So the shape is
// rejected LOUDLY rather than emitted with a keepalive that keeps nothing alive: the same trade
// convSyscallFunnelCall's own cast-prefix panic already makes one door over, and the panic text
// names the real remedy rather than the symptom.
func (v *Visitor) rejectDeferredSyscallKeepAlive(callExpr *ast.CallExpr) {
	for i, arg := range callExpr.Args {
		if pointerDerivedArgSource(v.info, arg) == nil {
			continue
		}

		panic(fmt.Sprintf(
			"@rejectDeferredSyscallKeepAlive - deferred/spawned syscall funnel call has pointer-derived argument %d (%s): the uintptrkeepalive contract is statement-scoped, so its GC.KeepAlive would run at the defer/go statement while the syscall itself runs at unwind. Go 1.23.12 contains no call of this shape; carrying one needs a defer-scoped keepalive design (the box captured by the thunk, released after it returns), not an emission",
			i, v.getPrintedNode(arg),
		))
	}
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
