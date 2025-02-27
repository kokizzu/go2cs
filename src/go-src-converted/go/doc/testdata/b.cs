// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package b -- go2cs converted at 2022 March 13 05:52:38 UTC
// import "go/doc.b" ==> using b = go.go.doc.b_package
// Original source: C:\Program Files\Go\src\go\doc\testdata\b.go
namespace go.go;

using a = a_package;

public static partial class b_package {

// ----------------------------------------------------------------------------
// Basic declarations

public static readonly float Pi = 3.14F; // Pi
 // Pi
public static nint MaxInt = default; // MaxInt
public partial struct T {
} // T
public static T V = default; // v
public static nint F(nint x) {
} // F
private static void M(this ptr<T> _addr_x) {
    ref T x = ref _addr_x.val;

} // M

// Corner cases: association with (presumed) predeclared types

// Always under the package functions list.
public static nint NotAFactory() {
}

// Associated with uint type if AllDecls is set.
public static nuint UintFactory() {
}

// Associated with uint type if AllDecls is set.
private static nuint uintFactory() {
}

// Should only appear if AllDecls is set.
private partial struct @uint {
} // overrides a predeclared type uint

// ----------------------------------------------------------------------------
// Exported declarations associated with non-exported types must always be shown.

private partial struct notExported { // : nint
}

public static readonly notExported C = 0;



public static readonly notExported C1 = iota;
public static readonly var C2 = 0;
private static readonly var c3 = 1;
public static readonly var C4 = 2;
public static readonly var C5 = 3;

public static notExported V = default;
public static notExported V1 = default;public static notExported V2 = default;private static notExported v3 = default;public static notExported V4 = default;public static notExported V5 = default;



public static notExported U1 = default;public static notExported U2 = default;private static notExported u3 = default;public static notExported U4 = default;public static notExported U5 = default;
private static notExported u6 = default;public static notExported U7 = 7;

public static notExported F1() {
}
private static notExported f2() {
}

} // end b_package
