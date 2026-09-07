// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using fs = go.io.fs_package;
using static go.os_package;
using time = time_package;

partial class os_internal_test_package {

// Export for testing.
public static Func<fs.FileInfo, time.Time> Atime = atime;

public static ж<Func<@string, (fs.FileInfo, error)>> LstatP;
internal static void initᴛLstatP() { LstatP = Ꮡlstat; }

public static error ErrWriteAtInAppendMode;
internal static void initᴛErrWriteAtInAppendMode() { ErrWriteAtInAppendMode = errWriteAtInAppendMode; }

public static ж<bool> TestingForceReadDirLstat;
internal static void initᴛTestingForceReadDirLstat() { TestingForceReadDirLstat = ᏑtestingForceReadDirLstat; }

public static error ErrPatternHasSeparator;
internal static void initᴛErrPatternHasSeparator() { ErrPatternHasSeparator = errPatternHasSeparator; }

[GoInit] internal static void init() {
    checkWrapErr = true;
}

} // end os_internal_test_package
