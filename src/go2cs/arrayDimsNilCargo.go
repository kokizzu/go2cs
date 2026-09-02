// arrayDimsNilCargo.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/types"
	"strconv"
	"strings"
)

// A Go array's LENGTH is part of its type, and it is the one part the managed emission cannot
// carry: `[0]byte` and `[3]byte` both render as golib's `array<byte>`. Everywhere else the
// reflection bridge recovers it from a live source — a VALUE reveals its own length, a struct
// FIELD from the declaring type's zero instance, a func PARAMETER from the `[GoArrayDims]` stamp
// (see emitGoArrayDimsAttribute and arrayZeroValueArgs). A NIL POINTER TO AN ARRAY has none of
// those: there is no pointee to measure — `GoReflect.PointeeArrayDims` says exactly that in its
// own words — and there is no attribute slot at an expression position. So the length is carried
// on the VALUE, as descriptor cargo, which is the array analog of chanDirectionCargo.go's
// treatment of a directional channel's direction and the third member of that same finite set.
//
// WHAT IT COSTS TO GET WRONG, measured rather than argued (2026-09-02). Without the cargo,
// `(*[0]byte)(nil)` and `(*[3]byte)(nil)` emit the IDENTICAL C# expression, so
// `reflect.TypeOf((*[10000]Xscalar)(nil))` cannot answer 10000 and reflect's own
// `verifyGCBits` row at all_test.go:7274 has no length to allocate from. The Go 1.23.12 standard
// library contains THIRTEEN nil constructions of pointer-to-array type; all thirteen are in
// `_test.go` files and NONE is in production code, which is why a `-stdlib` census of this defect
// reports zero and why the number that sized this cut came from `-tests`.
//
// WHAT IS DELIBERATELY NOT STAMPED, and it is the same exclusion chanDirectionCargo.go draws for
// a DEFINED channel type: a pointer to a NAMED array type — `type mediumScalarEven [8192]byte`,
// `(*mediumScalarEven)(nil)` — carries nothing here, because the named array gets its own C#
// struct whose `[GoType("[8192]byte")]` already holds the dimension. Four of the thirteen sites
// are that shape (reflect's `*MyBytesArray0` pair and both `runtime/arena_test.go` sites) and they
// are CORRECT today; stamping them would move four goldens for no behavior. The walk below
// therefore descends through UNNAMED array elements only and stops at the first named one, whose
// own declaration carries the rest.

// nilArrayPtrDims returns the Go array dimensions a nil pointer-to-array conversion must carry,
// outermost first, or nil when the target is not a pointer to an UNNAMED array.
func nilArrayPtrDims(t types.Type) []int64 {
	if t == nil {
		return nil
	}

	// The pointer type itself must be UNDEFINED, not merely have a pointer underlying: a DEFINED
	// pointer type (`type MyBytesArrayPtr0 *[0]byte`) emits as a go2cs-gen wrapper CLASS, which has
	// no `NilBoxOfDims` to call — the cargo has nowhere to ride there, and the wrapper's own
	// `[GoType("ж<array<byte>>")]` metadata is where its length would have to live instead. That is
	// a generator change with its own gate ladder (route #7), deliberately not bundled here. Same
	// exclusion chanDirectionCargo.go draws for a defined channel type, and for the same reason.
	ptr, isPtr := types.Unalias(t).(*types.Pointer)

	if !isPtr {
		return nil
	}

	var dims []int64

	// types.Unalias, not Underlying: an ALIAS for an array type IS that array type and carries no
	// declaration of its own, exactly as an alias for a channel type is stamped while a DEFINED
	// channel type is not. A *types.Named stops the walk — its own emission holds the dimension.
	for elem := types.Unalias(ptr.Elem()); ; {
		arr, isArr := elem.(*types.Array)

		if !isArr {
			break
		}

		dims = append(dims, arr.Len())
		elem = types.Unalias(arr.Elem())
	}

	return dims
}

// nilArrayPtrValue renders the NIL pointer to an unnamed array carrying its dimensions — what
// `(*[N]E)(nil)` is — or "" when the target carries no dimensions this emission stamps.
//
// The dimensions are rendered as C# `long` literals, matching GoArrayDimsAttribute's deliberate
// 64-bit widening rather than GoMapKeyDimsAttribute's un-widened form. A Go array length is Go's
// `int`, and the standard library uses the full range: runtime/vdso_linux.go declares
// `*[1<<50 - 1]byte`, Go's pointer-to-unbounded-array idiom, which reaches the bridge as a FIELD
// today and reaches THIS site the first time anyone writes it as a conversion.
func (v *Visitor) nilArrayPtrValue(t types.Type, targetCS string) string {
	dims := nilArrayPtrDims(t)

	if len(dims) == 0 || targetCS == "" {
		return ""
	}

	rendered := make([]string, len(dims))

	for i, dim := range dims {
		rendered[i] = strconv.FormatInt(dim, 10) + "L"
	}

	return targetCS + ".NilBoxOfDims(" + strings.Join(rendered, ", ") + ")"
}
