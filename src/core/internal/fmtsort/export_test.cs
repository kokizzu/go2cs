// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using reflect = reflect_package;
using static go.@internal.fmtsort_package;

partial class fmtsort_internal_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸreflect() {
    builtin.initPackage(typeof(reflect_package));
}

public static nint Compare(reflectꓸValue a, reflectꓸValue b) {
    return compare(a, b);
}

} // end fmtsort_internal_test_package
