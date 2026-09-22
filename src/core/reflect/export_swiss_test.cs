// Copyright 2024 Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build goexperiment.swissmap
namespace go;

using static global::go.reflect_package;

partial class reflect_internal_test_package {

public static global::go.reflect_package.ΔType MapGroupOf(global::go.reflect_package.ΔType x, global::go.reflect_package.ΔType y) {
    var (grp, _) = groupAndSlotOf(x, y);
    return grp;
}

} // end reflect_internal_test_package
