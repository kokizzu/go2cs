// cpu_x86_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// getGOAMD64level answers the amd64 MICROARCHITECTURE LEVEL THE BINARY WAS BUILT FOR — not the
// level the host CPU happens to support. Go's cpu_x86.s is a compile-time constant selected by the
// GOAMD64_vN define the toolchain sets from `go env GOAMD64`, whose fall-through is $1:
//
//     TEXT ·getGOAMD64level(SB),NOSPLIT,$0-4
//     #ifdef GOAMD64_v4  $4  #else #ifdef GOAMD64_v3  $3  #else #ifdef GOAMD64_v2  $2  #else  $1
//
// go2cs emits portable C#: there is no GOAMD64 define, no microarchitecture-gated emission, and no
// instruction-set floor above the amd64 baseline. The faithful answer is therefore the same
// constant Go's own assembly produces for a build with no GOAMD64_vN define — 1 — and it is a
// measured property of the emission rather than a placeholder. Probing the CPU here would answer a
// DIFFERENT question and be wrong in Go's terms: a v3 machine running a v1 build still reports 1,
// which is precisely why doinit keeps the sse3/avx/avx512 GODEBUG knobs switchable at level 1.
//
// The converter drops the auto form of this declaration (manualConversionFuncs["internal/cpu"] in
// go2cs/manualTypeOperations.go), leaving a placeholder comment at the site.
//
// Demonstrated consumers: doinit's option table, whose `level < 2/3/4` gates decide which cpu.*
// options remain switchable; and internal/cpu's own TestDisableSSE3, whose first line is
// `if GetGOAMD64level() > 1 { t.Skip(…) }` — the unimplemented PartialStubGenerator stub turned
// that guard into an infrastructure-error where Go reads 1 and walks on to a matching skip.

using go;

[module: go.GoManualConversion]

namespace go.@internal;

partial class cpu_package
{
    internal static int32 getGOAMD64level()
    {
        return 1;
    }
}
