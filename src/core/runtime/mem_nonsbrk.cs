// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !plan9 && !wasm
namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

internal const bool isSbrkPlatform = false;

internal static (@unsafe.Pointer, uintptr) sysReserveAlignedSbrk(uintptr size, uintptr align) {
    throw panic("unreachable");
}

} // end runtime_package
