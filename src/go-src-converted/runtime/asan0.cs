// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !asan
// Dummy ASan support API, used when not built with -asan.
namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

internal const bool asanenabled = false;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string asanˢ = "asan"u8;

// Because asanenabled is false, none of these functions should be called.
internal static void asanread(@unsafe.Pointer addr, uintptr sz) {
    @throw(asanˢ);
}

internal static void asanwrite(@unsafe.Pointer addr, uintptr sz) {
    @throw(asanˢ);
}

internal static void asanunpoison(@unsafe.Pointer addr, uintptr sz) {
    @throw(asanˢ);
}

internal static void asanpoison(@unsafe.Pointer addr, uintptr sz) {
    @throw(asanˢ);
}

internal static void asanregisterglobals(@unsafe.Pointer addr, uintptr sz) {
    @throw(asanˢ);
}

} // end runtime_package
