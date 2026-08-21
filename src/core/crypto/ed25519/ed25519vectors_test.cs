// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("crypto/ed25519/ed25519vectors_test.go", "ed25519vectors_test.cs", "AB5AABACgoiAgqSCgoKeqgALCIKChIKClIIAEwqClpSCgIKmgqiCgpSCgoKUhoCCpoKClKaCgoKClA==")]

namespace go.crypto;

using ed25519 = go.crypto.ed25519_package;
using hex = encoding.hex_package;
using json = encoding.json_package;
using testenv = go.@internal.testenv_package;
using os = os_package;
using exec = go.os.exec_package;
using filepath = path.filepath_package;
using testing = testing_package;
using encoding;
using fs = io.fs_package;
using go.@internal;
using go.crypto;
using go.os;
using path;
using static go.crypto.ed25519_internal_test_package;

partial class ed25519_test_package {

[GoType("dyn")] partial struct TestEd25519Vectors_vectors {
    public @string A, R, S, M;
    public slice<@string> Flags;
}

// TestEd25519Vectors runs a very large set of test vectors that exercise all
// combinations of low-order points, low-order components, and non-canonical
// encodings. These vectors lock in unspecified and spec-divergent behaviors in
// edge cases that are not security relevant in most contexts, but that can
// cause issues in consensus applications if changed.
//
// Our behavior matches the "classic" unwritten verification rules of the
// "ref10" reference implementation.
//
// Note that although we test for these edge cases, they are not covered by the
// Go 1 Compatibility Promise. Applications that need stable verification rules
// should use github.com/hdevalence/ed25519consensus.
//
// See https://hdevalence.ca/blog/2020-10-04-its-25519am for more details.
public static void TestEd25519Vectors(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    var jsonVectors = downloadEd25519Vectors(Ꮡt);
    ref var vectors = ref heap<slice<TestEd25519Vectors_vectors>>(out var Ꮡvectors);
    {
        var err = json.Unmarshal(jsonVectors, Ꮡvectors); if (err != default!) {
            Ꮡt.Fatal(err);
        }
    }
    foreach (var (i, v) in vectors) {
        var expectedToVerify = true;
        foreach (var (_, f) in v.Flags) {
            var exprᴛ1 = f;
            if (exprᴛ1 == "LowOrderResidue"u8) {
                expectedToVerify = false;
            }
            else if (exprᴛ1 == "NonCanonicalR"u8) {
                expectedToVerify = false;
            }

        }
        // We use the simplified verification formula that doesn't multiply
        // by the cofactor, so any low order residue will cause the
        // signature not to verify.
        //
        // This is allowed, but not required, by RFC 8032.
        // Our point decoding allows non-canonical encodings (in violation
        // of RFC 8032) but R is not decoded: instead, R is recomputed and
        // compared bytewise against the canonical encoding.
        var publicKey = decodeHex(Ꮡt, v.A);
        var signature = append(decodeHex(Ꮡt, v.R), decodeHex(Ꮡt, v.S).ꓸꓸꓸ);
        var message = slice<byte>(v.M);
        var didVerify = ed25519.Verify(publicKey, message, signature);
        if (didVerify && !expectedToVerify) {
            Ꮡt.Errorf("#%d: vector with flags %s unexpectedly verified"u8, i, v.Flags);
        }
        if (!didVerify && expectedToVerify) {
            Ꮡt.Errorf("#%d: vector with flags %s unexpectedly rejected"u8, i, v.Flags);
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string modcacheˢ = "modcache"u8;
internal static readonly @string go111moduleˢ = "GO111MODULE"u8;
internal static readonly @string gomodcacheˢ = "GOMODCACHE"u8;
internal static readonly @string filippoIoMostlyHarmlessˢ = "filippo.io/mostly-harmless/ed25519vectors@v0.0.0-20210322192420-30a2d7243a94"u8;
internal static readonly @string modˢ = "mod"u8;
internal static readonly @string downloadˢ = "download"u8;
internal static readonly @string modcacherwˢ = "-modcacherw"u8;
internal static readonly @string jsonˢ = "-json"u8;
internal static readonly @string ed25519vectorsJsonˢ = "ed25519vectors.json"u8;

[GoType("dyn")] partial struct downloadEd25519Vectors_dm {
    public @string Dir; // absolute path to cached source root directory
}

internal static slice<byte> downloadEd25519Vectors(ж<testing.T> Ꮡt) {
    testenv.MustHaveExternalNetwork(new ed25519_test_package.testing_TжTB(Ꮡt));
    // Create a temp dir and modcache subdir.
    @string d = Ꮡt.TempDir();
    // Create a spot for the modcache.
    @string modcache = filepath.Join(d, modcacheˢ);
    {
        var errΔ1 = os.Mkdir(modcache, 511); if (errΔ1 != default!) {
            Ꮡt.Fatal(errΔ1);
        }
    }
    Ꮡt.Setenv(go111moduleˢ, "on"u8);
    Ꮡt.Setenv(gomodcacheˢ, modcache);
    // Download the JSON test file from the GOPROXY with `go mod download`,
    // pinning the version so test and module caching works as expected.
    @string goTool = testenv.GoToolPath(new ed25519_test_package.testing_TжTB(Ꮡt));
    @string path = filippoIoMostlyHarmlessˢ;
    var cmd = exec.Command(goTool, modˢ, downloadˢ, modcacherwˢ, jsonˢ, path);
    // TODO: enable the sumdb once the TryBots proxy supports it.
    cmd.Value.Env = append(os.Environ(), "GONOSUMDB=*"u8);
    var (output, err) = cmd.Output();
    if (err != default!) {
        Ꮡt.Fatalf("failed to run `go mod download -json %s`, output: %s"u8, path, output);
    }
    ref var dm = ref heap(new downloadEd25519Vectors_dm(), out var Ꮡdm);
    {
        var errΔ2 = json.Unmarshal(output, Ꮡdm); if (errΔ2 != default!) {
            Ꮡt.Fatal(errΔ2);
        }
    }
    (var jsonVectors, err) = os.ReadFile(filepath.Join(dm.Dir, ed25519vectorsJsonˢ));
    if (err != default!) {
        Ꮡt.Fatalf("failed to read ed25519vectors.json: %v"u8, err);
    }
    return jsonVectors;
}

internal static slice<byte> decodeHex(ж<testing.T> Ꮡt, @string s) {
    Ꮡt.Helper();
    var (b, err) = hex.DecodeString(s);
    if (err != default!) {
        Ꮡt.Errorf("invalid hex: %v"u8, err);
    }
    return b;
}

} // end ed25519_test_package
