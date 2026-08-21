// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("sort/export_test.go", "export_test.cs", "AAsOgqaC")]

namespace go;

using static go.sort_package;

partial class sort_internal_test_package {

public static void Heapsort(global::go.sort_package.Interface data) {
    heapSort(data, 0, data.Len());
}

public static void ReverseRange(global::go.sort_package.Interface data, nint a, nint b) {
    reverseRange(data, a, b);
}

} // end sort_internal_test_package
