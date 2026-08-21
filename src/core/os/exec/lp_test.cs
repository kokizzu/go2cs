// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("os/exec/lp_test.go", "lp_test.cs", "ABQggoKCgpSClIKClII=")]

namespace go.os;

using testing = testing_package;
using static go.os.exec_package;

partial class exec_internal_test_package {

internal static slice<@string> nonExistentPaths = new @string[]{
    "some-non-existent-path"u8,
    "non-existent-path/slashed"u8
}.slice();

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object lookPathErrorIsNotAnExecˢ = (@string)"LookPath error is not an exec.Error"u8;

public static void TestLookPathNotFound(ж<testing.T> Ꮡt) {
    foreach (var (_, name) in nonExistentPaths) {
        var (path, err) = LookPath(name);
        if (err == default!) {
            Ꮡt.Fatalf("LookPath found %q in $PATH"u8, name);
        }
        if (path != ""u8) {
            Ꮡt.Fatalf("LookPath path == %q when err != nil"u8, path);
        }
        var (perr, ok) = err._<ж<global::go.os.exec_package.ΔError>>(ᐧ);
        if (!ok) {
            Ꮡt.Fatal(lookPathErrorIsNotAnExecˢ);
        }
        if ((~perr).Name != name) {
            Ꮡt.Fatalf("want Error name %q, got %q"u8, name, (~perr).Name);
        }
    }
}

} // end exec_internal_test_package
