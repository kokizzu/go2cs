// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using static go.fmt_package;

partial class fmt_internal_test_package {

public static Func<rune, bool> IsSpace;
internal static void initᴛIsSpace() { IsSpace = isSpace; }

public static Func<@string, nint, nint, (nint, bool, nint)> Parsenum = parsenum;

} // end fmt_internal_test_package
