// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("time/export_windows_test.go", "export_windows_test.cs", "AAwOgoKAtoKCgLaC")]

namespace go;

using static go.time_package;
using syscall = syscall_package;

partial class time_internal_test_package {

public static void ForceAusFromTZIForTesting() {
    ResetLocalOnceForTest();
    ᏑlocalOnce.Do(() => {
        initLocalFromTZI(Ꮡaus);
    });
}

public static void ForceUSPacificFromTZIForTesting() {
    ResetLocalOnceForTest();
    ᏑlocalOnce.Do(() => {
        initLocalFromTZI(ᏑusPacific);
    });
}

public static (@string, error) ToEnglishName(@string stdname, @string dstname) {
    return toEnglishName(stdname, dstname);
}

} // end time_internal_test_package
