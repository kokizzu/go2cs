// chanDirectionCargo.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/types"
)

// A Go channel's DIRECTION is part of its type, and it is the one part the managed emission cannot
// carry: `chan T`, `chan<- T` and `<-chan T` all render as golib's `channel<T>`, distinguished only
// by the `/*<-*/` marker comment the type renderer places for the reader. So the direction is
// carried on the VALUE instead, as descriptor cargo, exactly the way a fixed-size array's length is
// (see emitGoArrayDimsAttribute and arrayZeroValueArgs): the reflection bridge reads it back off
// the live channel and stamps it on the abi.Type, which is what makes reflect.Type.ChanDir() and
// String() answer `chan<- string` rather than the bidirectional type.
//
// The emission sites that stamp it are the places a directional channel VALUE is born in
// converted code — the same finite set the array dims occupy, position for position:
//
//   - `make(chan<- T[, n])` — the MADE channel (convCallExpr's make arm);
//   - the ZERO value of a directional channel type: a `var` declaration (LOCAL and GLOBAL — both
//     rungs of visitValueSpec's inline ladder since 2026-09-01; before that the doc claimed this
//     site and the implementation lacked it, which is exactly how reflect's TestChanOf read
//     `TypeOf(left)` as bidirectional), a named result (zeroValueInitializer), a struct FIELD's
//     initializer (visitStructType), and an array ELEMENT (visitArrayType);
//   - `new(chan<- T)`, whose pointee is that same zero value (convCallExpr's new arm);
//   - a CONVERSION OF NIL to a directional type — `(chan<- string)(nil)` — which mints the same
//     zero value through a cast (convCallExpr's nil-conversion arm, added 2026-09-01: see below).
//
// What is deliberately NOT stamped, and why — AMENDED 2026-09-01, because the original rule's
// premise died on measurement:
//
//   - a NARROWING conversion of a LIVE channel (`var s chan<- int = ch`) keeps the source value's
//     direction. Go makes a new value of a new TYPE there, but the narrowing has no construction
//     to hook — it is a plain struct copy — and stamping it would mean an explicit call at every
//     assignment, argument and return of a directional channel in the corpus (89 such positions).
//     The ORIGINAL r39d exclusion covered the nil-cast and the zero-value var too, justified as
//     "a datum no measured consumer reads" — and then reflect's own suite measured FOUR consumers
//     (TestAll #12, TestTypes #20-22, TestChanOf, TestChanOfDir), so the coordinator ruled the
//     narrowing CARRIED (2026-09-01) and the construction-shaped positions above joined the stamp
//     set. What remains excluded is only the live-copy narrowing, which no measured consumer
//     reads YET; if one appears, this paragraph is the precedent for how the rule falls.
//   - a DEFINED channel type (`type closeWaiter chan struct{}`) is not stamped, for the same reason
//     a defined ARRAY type carries no dims: its managed form is a go2cs-gen wrapper struct rather
//     than `channel<T>`, so there is no field to carry the cargo and no reader to consume it. An
//     ALIAS for a channel type IS its target, so it is stamped (types.Unalias resolves it).
//   - a type PARAMETER instantiated at a channel type: `make<T>(…)` routes through ISupportMake,
//     which has no direction-taking form.

// chanDirCargoName renders the golib GoChanDir member for a directional, undefined Go channel type,
// or "" when the type carries no direction this emission stamps.
func chanDirCargoName(t types.Type) string {
	if t == nil {
		return ""
	}

	resolved := types.Unalias(t)

	if _, isNamed := resolved.(*types.Named); isNamed {
		return ""
	}

	chanType, isChan := resolved.Underlying().(*types.Chan)

	if !isChan {
		return ""
	}

	switch chanType.Dir() {
	case types.SendOnly:
		return "GoChanDir.Send"
	case types.RecvOnly:
		return "GoChanDir.Recv"
	}

	return ""
}

// chanDirNilValue renders the NIL channel of a directional type — the zero VALUE of `chan<- T` /
// `<-chan T`, which is still a value whose Go type has a direction — as golib's SendOnly/RecvOnly
// factory on the emitted channel type. Returns "" for any type with no direction to carry, which
// leaves every existing zero-value emission byte-identical.
func (v *Visitor) chanDirNilValue(t types.Type) string {
	var member string

	switch chanDirCargoName(t) {
	case "GoChanDir.Send":
		member = ".SendOnly"
	case "GoChanDir.Recv":
		member = ".RecvOnly"
	default:
		return ""
	}

	return v.getCSharpTypeName(t) + member
}

// chanDirNarrowedValue wraps an already-rendered channel expression in golib's
// `.WithDirection(...)` re-stamp when the value NARROWS from a bidirectional Go channel type into a
// directional one — `var s <-chan T = ch`, the argument of a `func(<-chan T)`, a directional result.
// Returns "" when the flow is not a narrowing, which leaves every other emission byte-identical.
//
// This is the LIVE-COPY position the exclusion at the top of this file deferred, and it is the one
// the reflection bridge could not answer without: the construction-shaped positions stamp the
// direction at BIRTH (make/zero/new/nil-cast, through channel<T>'s directional constructors), but a
// narrowing has no construction to hook — Go makes a value of a new TYPE out of a value that
// already exists. The re-stamp shares the SAME core, so Go's channel identity (==, Equals and the
// hash all read the core) and every operation survive; only the carried direction differs.
// reflect's TestMakeFuncInvalidReturnAssignments is the measured consumer: it returns a `<-chan int`
// narrowed from `make(chan int)` into a `chan int` result, and Go REFUSES that widening — unstamped,
// the marshalling identity arm sees two bidirectional `channel<int>` values and admits it.
//
// dst is the STATIC target type (the declared var type, the parameter type, the result type); src is
// the expression's own type. A DEFINED channel type on either side (`type closeWaiter chan struct{}`)
// is a go2cs-gen wrapper with no direction cargo, exactly as chanDirCargoName already refuses.
func (v *Visitor) chanDirNarrowedValue(dst, src types.Type, rendered string) string {
	if dst == nil || src == nil || rendered == "" {
		return ""
	}

	dstDir := chanDirCargoName(dst)

	if dstDir == "" {
		return ""
	}

	// The SOURCE must be a bidirectional, undefined channel: a source that already carries the
	// destination's direction is not a narrowing (it was stamped at birth or narrowed upstream),
	// and a named/defined source has no direction cargo to re-stamp.
	srcResolved := types.Unalias(src)

	if _, isNamed := srcResolved.(*types.Named); isNamed {
		return ""
	}

	srcChan, isChan := srcResolved.Underlying().(*types.Chan)

	if !isChan || srcChan.Dir() != types.SendRecv {
		return ""
	}

	return fmt.Sprintf("%s.WithDirection(%s)", rendered, dstDir)
}
