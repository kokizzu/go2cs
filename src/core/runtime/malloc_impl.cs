// malloc_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// go2cs HAND-OWNED companion to malloc.go.
//
// TEST SEAM. GolibTests is not in runtime's InternalsVisibleTo grant, so the Go-prefixed PUBLIC
// helper below exposes runtime's zerobase for the zero-base identity guard (pinner_impl.cs's
// pattern: one public helper per operation).
[module: go.GoManualConversion]

namespace go;

using @unsafe = unsafe_package;

public static partial class runtime_package
{
    /// <summary>
    /// <c>unsafe.Pointer(&amp;zerobase)</c> — the address Go's mallocgc answers for every zero-byte
    /// allocation, as export_test.go's <c>ZeroBase</c> reads it.
    /// </summary>
    public static @unsafe.Pointer GoZeroBaseProbe() => @unsafe.Pointer.FromBox(Ꮡzerobase);
}
