// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build 386 || amd64
[assembly: go.GoPositionMap("math/hypot_asm.go", "hypot_asm.cs", "AAwW")]

namespace go;

partial class math_package {

internal const bool haveArchHypot = true;

internal static partial float64 archHypot(float64 p, float64 q);

} // end math_package
