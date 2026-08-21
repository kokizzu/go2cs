// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !arm64 && !ppc64 && !ppc64le
[assembly: go.GoPositionMap("math/modf_noasm.go", "modf_noasm.cs", "AAwWgg==")]

namespace go;

partial class math_package {

internal const bool haveArchModf = false;

internal static (float64 @int, float64 frac) archModf(float64 f) {
    throw panic("not implemented");
}

} // end math_package
