// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("strconv/export_test.go", "export_test.cs", "")]

namespace go;

using static go.strconv_package;

partial class strconv_internal_test_package {

public static Func<@string, @string, nint, ж<global::go.strconv_package.NumError>> BitSizeError;
internal static void initᴛBitSizeError() { BitSizeError = bitSizeError; }
public static Func<@string, @string, nint, ж<global::go.strconv_package.NumError>> BaseError;
internal static void initᴛBaseError() { BaseError = baseError; }

} // end strconv_internal_test_package
