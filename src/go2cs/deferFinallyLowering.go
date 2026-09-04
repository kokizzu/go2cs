// deferFinallyLowering.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"strconv"
	"strings"
)

// Defer→finally lowering (capability 4 of DESIGN-zh-box-three-capabilities.md).
//
// A deferred call on a RECEIVER FIELD — `defer c.mu.Unlock()` — costs a ж-box per call: the
// registration must hold the receiver, and holding `c.mu` means boxing it. The call is already
// going to run on every exit path, and a C# `finally` already runs on every exit path, so where
// the two coincide the registration is pure overhead and the call can be emitted DIRECTLY into
// the frame's finally. Nothing is stored, so nothing is boxed.
//
// "Where the two coincide" is the whole difficulty, because Go's defer is not a finally:
//
//   - Go evaluates the deferred call's RECEIVER AND ARGUMENTS at REGISTRATION, not at unwind. A
//     finally evaluates them at unwind. Every gate below exists to make that difference
//     unobservable, and each one was measured over the corpus rather than assumed.
//   - A defer that was never REACHED never runs. A finally runs whenever its try was entered. The
//     per-site flag closes that: the finally calls only what the body actually reached.
//   - Go's defers are LIFO. The finally emits them in REVERSE source order, which is the same
//     thing for the top-level-only population this admits.
//
// The gates (population measured at 220 sites in Go 1.23.12's std; 166 qualify, 75.5%):
//
//	shape         the call is `<recv>.<field>.<Method>()` — capability 4's boxing shape
//	top-level     the defer is a direct child of the body (so it is unconditional and unlooped,
//	              and its source order IS its registration order)
//	no arguments  arguments are evaluated at registration; storing them is the box again
//	no recover    the frame's catch/recover protocol is untouched by this cut
//	no named result   a named-result exit reads results AFTER the finally; not in this increment
//	no Goexit/Exit    a call that never returns must not have its finally semantics reasoned about
//	receiver stable   neither the receiver nor any prefix of the path is reassigned or addressed
//	                  — `defer c.mu.Unlock(); c = other` unlocks the ORIGINAL c's mutex in Go and
//	                  would unlock the NEW one here, silently
//	prefix dereferenced first   some UNCONDITIONAL statement ahead of the defer already dereferences
//	                  the same prefix, so a nil receiver panics there in BOTH forms and the moved
//	                  evaluation can never be the first panic. Measured at THREE sites with no
//	                  earlier dereference at all — a gate predicted empty and measured non-zero —
//	                  and at FOUR more whose only earlier dereference was CONDITIONAL, which
//	                  witnesses nothing; both are refused.
//	all-or-nothing    every defer in the function qualifies, or none is lowered. A function that
//	                  mixes registered and lowered defers would have to interleave two LIFO orders.
//
// A defer inside a func literal is not a direct child of the outer body, so any such function
// fails all-or-nothing and is left entirely alone. That is deliberate and conservative.

// loweredDefer is one admitted site: the flag that records the body reached it, and the call
// rendered as an ordinary expression (filled in when visitDeferStmt reaches the statement).
type loweredDefer struct {
	flagName  string
	call      string
	aliasName string
}

// planDeferFinallyLowering decides, before the body is visited, which of this function's defer
// statements are emitted into the frame's finally instead of registered. It populates
// v.loweredDefers in SOURCE order and v.loweredDeferIndex for visitDeferStmt to look itself up in;
// both are empty when the function does not qualify, which is the overwhelming majority.
func (v *Visitor) planDeferFinallyLowering(funcDecl *ast.FuncDecl) {
	v.loweredDefers = nil
	v.loweredDeferIndex = nil

	if funcDecl == nil || funcDecl.Body == nil || !v.hasDefer {
		return
	}

	// The frame's recover protocol and the named-result exit are both out of scope for this
	// increment: the first shares the catch arm this cut does not touch, the second reads its
	// results after the finally has run.
	if v.hasRecover || v.namedReturnDeferMode {
		return
	}

	recvName := funcDeclReceiverName(funcDecl)

	if recvName == "" {
		return
	}

	// Go evaluates the receiver at registration. If the body can rebind it — or take its address,
	// after which this pass cannot prove it is not rebound — the lowered call would run against a
	// different receiver than Go's would.
	if receiverPathUnstable(funcDecl.Body, recvName) {
		return
	}

	// A call that does not return normally leaves the finally's relationship to Go's defer order
	// something to reason about rather than something to state. Measured at zero sites in the
	// qualifying population; refused regardless, because refusing costs nothing.
	if bodyReachesNonReturning(funcDecl.Body) {
		return
	}

	// Collect the TOP-LEVEL defers in source order, and require that they are ALL of the function's
	// defers. A defer nested in a block, a loop, a conditional or a func literal fails this.
	var topLevel []*ast.DeferStmt

	for _, stmt := range funcDecl.Body.List {
		if deferStmt, ok := stmt.(*ast.DeferStmt); ok {
			topLevel = append(topLevel, deferStmt)
		}
	}

	if len(topLevel) == 0 {
		return
	}

	total := 0

	ast.Inspect(funcDecl.Body, func(node ast.Node) bool {
		if _, ok := node.(*ast.DeferStmt); ok {
			total++
		}

		return true
	})

	if total != len(topLevel) {
		return
	}

	// Every site must carry the shape and pass its own gates — all-or-nothing.
	for _, deferStmt := range topLevel {
		if !isReceiverFieldMethodCall(deferStmt.Call, recvName) {
			return
		}

		if len(deferStmt.Call.Args) != 0 || deferStmt.Call.Ellipsis.IsValid() {
			return
		}

		if !prefixDereferencedBefore(deferStmt, funcDecl.Body) {
			return
		}
	}

	v.loweredDefers = make([]*loweredDefer, 0, len(topLevel))
	v.loweredDeferIndex = make(map[*ast.DeferStmt]int, len(topLevel))

	for i, deferStmt := range topLevel {
		v.loweredDeferIndex[deferStmt] = i
		v.loweredDefers = append(v.loweredDefers, &loweredDefer{
			flagName: GoFrameVar + "d" + strconv.Itoa(i+1),
		})
	}
}

// funcDeclReceiverName is the method receiver's name, or "" for a function, a blank receiver or an
// unnamed one — none of which can carry capability 4's shape.
func funcDeclReceiverName(funcDecl *ast.FuncDecl) string {
	if funcDecl.Recv == nil || len(funcDecl.Recv.List) == 0 || len(funcDecl.Recv.List[0].Names) == 0 {
		return ""
	}

	name := funcDecl.Recv.List[0].Names[0].Name

	if name == "_" {
		return ""
	}

	return name
}

// isReceiverFieldMethodCall reports capability 4's shape: `<recv>.<field>.<Method>()`, a method
// called on a FIELD of the receiver. That is the emission whose registration boxes the field.
func isReceiverFieldMethodCall(call *ast.CallExpr, recvName string) bool {
	outer, ok := call.Fun.(*ast.SelectorExpr)

	if !ok {
		return false
	}

	inner, ok := outer.X.(*ast.SelectorExpr)

	if !ok {
		return false
	}

	base, ok := inner.X.(*ast.Ident)

	return ok && base.Name == recvName
}

// receiverPathUnstable reports whether the body assigns to the receiver name or takes its address.
// Go binds the deferred call's receiver at REGISTRATION; a finally binds it at unwind. If the name
// can be rebound between the two, the lowered call runs against the wrong object — silently, with
// no compile error and no panic, which is the worst failure this cut could have.
func receiverPathUnstable(body *ast.BlockStmt, recvName string) bool {
	unstable := false

	ast.Inspect(body, func(node ast.Node) bool {
		switch n := node.(type) {
		case *ast.AssignStmt:
			for _, lhs := range n.Lhs {
				if ident, ok := lhs.(*ast.Ident); ok && ident.Name == recvName {
					unstable = true
				}
			}
		case *ast.IncDecStmt:
			if ident, ok := n.X.(*ast.Ident); ok && ident.Name == recvName {
				unstable = true
			}
		case *ast.UnaryExpr:
			if n.Op == token.AND {
				if ident, ok := n.X.(*ast.Ident); ok && ident.Name == recvName {
					unstable = true
				}
			}
		}

		return !unstable
	})

	return unstable
}

// bodyReachesNonReturning reports a direct `runtime.Goexit()` or `os.Exit()` call in the body.
// Measured at zero sites in the qualifying population; refused regardless.
func bodyReachesNonReturning(body *ast.BlockStmt) bool {
	found := false

	ast.Inspect(body, func(node ast.Node) bool {
		call, ok := node.(*ast.CallExpr)

		if !ok {
			return !found
		}

		if sel, ok := call.Fun.(*ast.SelectorExpr); ok {
			if pkg, ok := sel.X.(*ast.Ident); ok {
				if (pkg.Name == "runtime" && sel.Sel.Name == "Goexit") ||
					(pkg.Name == "os" && sel.Sel.Name == "Exit") {
					found = true
				}
			}
		}

		return !found
	})

	return found
}

// prefixDereferencedBefore reports whether a statement BEFORE the defer already dereferences the
// deferred call's receiver prefix (`c.mu` for `defer c.mu.Unlock()`).
//
// Go evaluates that prefix AT the defer statement, so a nil `c` panics there and registers nothing.
// A lowered finally evaluates it at EXIT — so the body would run to completion first and the panic
// would surface late, with whatever the body did already committed. No flag repairs that: evaluating
// the prefix at registration means STORING it, which is the box this capability exists to remove.
//
// But when a statement ahead of the defer dereferences the identical prefix — the
// `c.mu.Lock(); defer c.mu.Unlock()` idiom that dominates the population — that statement panics
// first in BOTH forms, so the defer's own evaluation can never be the first panic and the
// divergence is unreachable.
//
// The match must be UNCONDITIONAL. A dereference inside an `if`, a `switch`, a `select`, a loop or
// a func literal proves nothing: that branch may not have run, so the defer's own evaluation can
// still be the first dereference and the divergence is back. Measured: FOUR of the qualifying sites
// leaned on exactly such a conditional match, so this is a populated hole rather than a theoretical
// one — the first form of this predicate accepted them.
//
// Measured: THREE sites in Go 1.23.12's std have no earlier dereference at all — a gate predicted
// empty, measured non-zero, and those three would have been mis-lowered without it.
func prefixDereferencedBefore(deferStmt *ast.DeferStmt, body *ast.BlockStmt) bool {
	outer, ok := deferStmt.Call.Fun.(*ast.SelectorExpr)

	if !ok {
		return false
	}

	prefix, ok := outer.X.(*ast.SelectorExpr)

	if !ok {
		return false
	}

	base, ok := prefix.X.(*ast.Ident)

	if !ok {
		return false
	}

	want := base.Name + "." + prefix.Sel.Name
	found := false

	for _, stmt := range body.List {
		if stmt.Pos() >= deferStmt.Pos() {
			break
		}

		// Only statements that unconditionally execute can witness the dereference.
		switch stmt.(type) {
		case *ast.IfStmt, *ast.SwitchStmt, *ast.TypeSwitchStmt, *ast.SelectStmt,
			*ast.ForStmt, *ast.RangeStmt, *ast.LabeledStmt:
			continue
		}

		ast.Inspect(stmt, func(node ast.Node) bool {
			// A func literal's body may never run either.
			if _, isLit := node.(*ast.FuncLit); isLit {
				return false
			}

			if sel, ok := node.(*ast.SelectorExpr); ok {
				if b, ok := sel.X.(*ast.Ident); ok && b.Name+"."+sel.Sel.Name == want {
					found = true
				}
			}

			return !found
		})

		if found {
			return true
		}
	}

	return false
}

// reRootOnEntryBox re-roots a lowered call on the receiver's BOX when the receiver is reached
// through an entry deref alias.
//
// The alias (`ref var c = ref Ꮡc.DerefOrNull();`) is declared inside the frame's TRY, because a nil
// box must fault where the catch filter can turn it into a Go panic. The finally is outside that
// scope, so a call emitted there cannot name `c` — it would not compile. The box parameter itself is
// a method parameter and is in scope everywhere, so the call is re-rooted on the expression the
// alias was declared FROM, captured verbatim at the alias's own emission site.
//
// Nothing is allocated by this: the box already exists as the receiver parameter. And nothing can
// fault that would not have faulted anyway — the flag guarding the call is set only after the body
// reached the defer, which the prefix gate proves is after a dereference of the same path.
// It runs at the frame's TAIL rather than at the defer statement, because the alias map is filled
// while the function preamble is composed — which happens AFTER the body has been rendered (the
// preamble's own emission consults the rendered body text). So the defer records the alias name it
// rendered against and the substitution happens here, where both are known.
func (v *Visitor) reRootOnEntryBox(lowered *loweredDefer) string {
	if lowered.call == "" || lowered.aliasName == "" || len(v.entryAliasBoxPaths) == 0 {
		return lowered.call
	}

	boxPath, aliased := v.entryAliasBoxPaths[lowered.aliasName]

	if !aliased || !strings.HasPrefix(lowered.call, lowered.aliasName+".") {
		return lowered.call
	}

	return boxPath + strings.TrimPrefix(lowered.call, lowered.aliasName)
}

// loweredCallAliasName is the name the lowered call's receiver base rendered as — the key the
// finally's re-rooting looks up in entryAliasBoxPaths.
func (v *Visitor) loweredCallAliasName(call *ast.CallExpr) string {
	outer, ok := call.Fun.(*ast.SelectorExpr)

	if !ok {
		return ""
	}

	inner, ok := outer.X.(*ast.SelectorExpr)

	if !ok {
		return ""
	}

	base, ok := inner.X.(*ast.Ident)

	if !ok {
		return ""
	}

	return v.convIdent(base, DefaultIdentContext())
}

// goFrameLoweredFlagDecls renders the reached-flags, declared with the frame itself — BEFORE the
// try, because the finally reads them and a local declared inside a try is not in scope there.
//
// Every site carries a flag, including one whose defer is the body's FIRST statement. That is a
// deliberate departure from the shape first proposed for this cut: the function preamble the frame
// emits (parameter deref aliases, entry-time boxes) sits INSIDE the try, so a panic there reaches
// the finally, and an unflagged call would run on a path Go never registered it on. The store is a
// stack local; correctness is worth more than eliding it.
func (v *Visitor) goFrameLoweredFlagDecls(indentLevel int) string {
	if len(v.loweredDefers) == 0 {
		return ""
	}

	result := strings.Builder{}

	for _, lowered := range v.loweredDefers {
		result.WriteString(v.newline)
		result.WriteString(v.indent(indentLevel))
		result.WriteString(fmt.Sprintf("bool %s = false;", lowered.flagName))
	}

	return result.String()
}

// goFrameLoweredFinallyCalls renders the lowered calls for the frame's finally, in REVERSE source
// order — Go's LIFO — each guarded by the flag that records the body reached its defer statement.
// They run BEFORE the frame's own Run(), which re-throws an unrecovered panic and would otherwise
// leave them unexecuted.
func (v *Visitor) goFrameLoweredFinallyCalls() string {
	if len(v.loweredDefers) == 0 {
		return ""
	}

	result := strings.Builder{}

	for i := len(v.loweredDefers) - 1; i >= 0; i-- {
		lowered := v.loweredDefers[i]
		call := v.reRootOnEntryBox(lowered)

		if call == "" {
			continue
		}

		result.WriteString(fmt.Sprintf("if (%s) %s; ", lowered.flagName, call))
	}

	return result.String()
}
