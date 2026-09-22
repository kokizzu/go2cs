// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using bytes = bytes_package;
using testenv = @internal.testenv_package;
using Δos = os_package;
using exec = global::go.os.exec_package;
using testing = testing_package;
using @internal;
using global::go.os;
using static global::go.runtime_internal_test_package;

partial class runtime_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string runˢ = "run"u8;
internal static readonly @string mklockrankGoˢ = "mklockrank.go"u8;
internal static readonly @string lockrankGoˢ = "lockrank.go"u8;

// Test that the generated code for the lock rank graph is up-to-date.
public static void TestLockRankGenerated(ж<testing.T> Ꮡt) {
    testenv.MustHaveGoRun(new runtime_test_package.testing_TжTB(Ꮡt));
    var cmd = testenv.CleanCmdEnv(testenv.Command(new runtime_test_package.testing_TжTB(Ꮡt), testenv.GoToolPath(new runtime_test_package.testing_TжTB(Ꮡt)), runˢ, mklockrankGoˢ));
    var (want, err) = cmd.Output();
    if (err != default!) {
        {
            var (ee, ok) = err._<ж<exec.ExitError>>(ᐧ); if (ok && len((~ee).Stderr) > 0) {
                Ꮡt.Fatalf("%v: %v\n%s"u8, cmd.OrTypedNil(), err, (~ee).Stderr);
            }
        }
        Ꮡt.Fatalf("%v: %v"u8, cmd.OrTypedNil(), err);
    }
    (var got, err) = Δos.ReadFile(lockrankGoˢ);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    if (!bytes.Equal(want, got)) {
        Ꮡt.Fatalf("lockrank.go is out of date. Please run go generate."u8);
    }
}

} // end runtime_test_package
