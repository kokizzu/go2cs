// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// TODO: This file contains the special casing rules for Turkish and Azeri only.
// It should encompass all the languages with special casing rules
// and be generated automatically, but that requires some API
// development first.

// package unicode -- go2cs converted at 2022 March 13 05:28:20 UTC
// import "unicode" ==> using unicode = go.unicode_package
// Original source: C:\Program Files\Go\src\unicode\casetables.go
namespace go;

public static partial class unicode_package {

public static SpecialCase TurkishCase = _TurkishCase;
private static SpecialCase _TurkishCase = new SpecialCase(CaseRange{0x0049,0x0049,d{0,0x131-0x49,0}},CaseRange{0x0069,0x0069,d{0x130-0x69,0,0x130-0x69}},CaseRange{0x0130,0x0130,d{0,0x69-0x130,0}},CaseRange{0x0131,0x0131,d{0x49-0x131,0,0x49-0x131}},);

public static SpecialCase AzeriCase = _TurkishCase;

} // end unicode_package
