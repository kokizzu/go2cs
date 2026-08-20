// chanDirectionCargo.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
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
// Three emission sites stamp it, and they are the three places a directional channel VALUE is born
// in converted code — the same finite set the array dims occupy, position for position:
//
//   - `make(chan<- T[, n])` — the MADE channel (convCallExpr's make arm);
//   - the ZERO value of a directional channel type: a `var` declaration, a named result, and a
//     struct FIELD's initializer (zeroValueInitializer / visitStructType), which is the position
//     `new(chan<- string)` and reflectlite's `struct{ x chan<- string }` read;
//   - `new(chan<- T)`, whose pointee is that same zero value (convCallExpr's new arm).
//
// What is deliberately NOT stamped, and why:
//
//   - a NARROWING conversion (`var s chan<- int = ch`) keeps the source value's direction. Go makes
//     a new value of a new TYPE there, but the narrowing has no construction to hook — it is a
//     plain struct copy — and stamping it would mean an explicit call at every assignment, argument
//     and return of a directional channel in the corpus (89 such positions) for a datum no measured
//     consumer reads. The r39d rule: a dims-less descriptor is a state the bridge already handles.
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
