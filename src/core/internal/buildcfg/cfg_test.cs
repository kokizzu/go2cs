// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using os = os_package;
using testing = testing_package;
using static go.@internal.buildcfg_package;

partial class buildcfg_internal_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string rva20u64ˢ = "rva20u64"u8;
internal static readonly @string rva22u64ˢ = "rva22u64"u8;
internal static readonly @string rva22ˢ = "rva22"u8;
internal static readonly @string v70ˢ = "v7.0"u8;
internal static readonly @string v80Lsbˢ = "v8.0,lsb"u8;
internal static readonly @string v80Lseˢ = "v8.0,lse"u8;
internal static readonly @string v80Cryptoˢ = "v8.0,crypto"u8;
internal static readonly @string v80CryptoLseˢ = "v8.0,crypto,lse"u8;
internal static readonly @string v80LseCryptoˢ = "v8.0,lse,crypto"u8;
internal static readonly @string v90ˢ = "v9.0"u8;

public static void TestConfigFlags(ж<testing.T> Ꮡt) {
    os.Setenv(goamd64ˢ, "v1"u8);
    if (goamd64() != 1) {
        Ꮡt.Errorf("Wrong parsing of GOAMD64=v1"u8);
    }
    os.Setenv(goamd64ˢ, "v4"u8);
    if (goamd64() != 4) {
        Ꮡt.Errorf("Wrong parsing of GOAMD64=v4"u8);
    }
    Error = default!;
    os.Setenv(goamd64ˢ, "1"u8);
    {
        goamd64(); if (Error == default!) {
            Ꮡt.Errorf("Wrong parsing of GOAMD64=1"u8);
        }
    }
    os.Setenv(goriscv64ˢ, rva20u64ˢ);
    if (goriscv64() != 20) {
        Ꮡt.Errorf("Wrong parsing of RISCV64=rva20u64"u8);
    }
    os.Setenv(goriscv64ˢ, rva22u64ˢ);
    if (goriscv64() != 22) {
        Ꮡt.Errorf("Wrong parsing of RISCV64=rva22u64"u8);
    }
    Error = default!;
    os.Setenv(goriscv64ˢ, rva22ˢ);
    {
        _ = goriscv64(); if (Error == default!) {
            Ꮡt.Errorf("Wrong parsing of RISCV64=rva22"u8);
        }
    }
    Error = default!;
    os.Setenv(goarm64ˢ, v70ˢ);
    {
        _ = goarm64(); if (Error == default!) {
            Ꮡt.Errorf("Wrong parsing of GOARM64=7.0"u8);
        }
    }
    Error = default!;
    os.Setenv(goarm64ˢ, "8.0"u8);
    {
        _ = goarm64(); if (Error == default!) {
            Ꮡt.Errorf("Wrong parsing of GOARM64=8.0"u8);
        }
    }
    Error = default!;
    os.Setenv(goarm64ˢ, v80Lsbˢ);
    {
        _ = goarm64(); if (Error == default!) {
            Ꮡt.Errorf("Wrong parsing of GOARM64=v8.0,lsb"u8);
        }
    }
    os.Setenv(goarm64ˢ, v80Lseˢ);
    if (goarm64().Version != "v8.0"u8 || goarm64().LSE != true || goarm64().Crypto != false) {
        Ꮡt.Errorf("Wrong parsing of GOARM64=v8.0,lse"u8);
    }
    os.Setenv(goarm64ˢ, v80Cryptoˢ);
    if (goarm64().Version != "v8.0"u8 || goarm64().LSE != false || goarm64().Crypto != true) {
        Ꮡt.Errorf("Wrong parsing of GOARM64=v8.0,crypto"u8);
    }
    os.Setenv(goarm64ˢ, v80CryptoLseˢ);
    if (goarm64().Version != "v8.0"u8 || goarm64().LSE != true || goarm64().Crypto != true) {
        Ꮡt.Errorf("Wrong parsing of GOARM64=v8.0,crypto,lse"u8);
    }
    os.Setenv(goarm64ˢ, v80LseCryptoˢ);
    if (goarm64().Version != "v8.0"u8 || goarm64().LSE != true || goarm64().Crypto != true) {
        Ꮡt.Errorf("Wrong parsing of GOARM64=v8.0,lse,crypto"u8);
    }
    os.Setenv(goarm64ˢ, v90ˢ);
    if (goarm64().Version != "v9.0"u8 || goarm64().LSE != true || goarm64().Crypto != false) {
        Ꮡt.Errorf("Wrong parsing of GOARM64=v9.0"u8);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string v93ˢ = "v9.3"u8;
internal static readonly @string v94ˢ = "v9.4"u8;
internal static readonly @string v88ˢ = "v8.8"u8;
internal static readonly @string v89ˢ = "v8.9"u8;
internal static readonly @string lseˢ = ",lse"u8;

public static void TestGoarm64FeaturesSupports(ж<testing.T> Ꮡt) {
    var (g, _) = ParseGoarm64(v93ˢ);
    if (!g.Supports(v93ˢ)) {
        Ꮡt.Errorf("Wrong goarm64Features.Supports for v9.3, v9.3"u8);
    }
    if (g.Supports(v94ˢ)) {
        Ꮡt.Errorf("Wrong goarm64Features.Supports for v9.3, v9.4"u8);
    }
    if (!g.Supports(v88ˢ)) {
        Ꮡt.Errorf("Wrong goarm64Features.Supports for v9.3, v8.8"u8);
    }
    if (g.Supports(v89ˢ)) {
        Ꮡt.Errorf("Wrong goarm64Features.Supports for v9.3, v8.9"u8);
    }
    if (g.Supports(lseˢ)) {
        Ꮡt.Errorf("Wrong goarm64Features.Supports for v9.3, ,lse"u8);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string arm64ˢ = "arm64"u8;
internal static readonly @string v95ˢ = "v9.5"u8;
internal static readonly object wrongTagsForGoarm64V95ˢ = (@string)"Wrong tags for GOARM64=v9.5"u8;

public static void TestGogoarchTags(ж<testing.T> Ꮡt) {
    @string old_goarch = GOARCH;
    var old_goarm64 = GOARM64;
    GOARCH = arm64ˢ;
    os.Setenv(goarm64ˢ, v95ˢ);
    GOARM64 = goarm64();
    var tags = gogoarchTags();
    var want = new @string[]{"arm64.v9.0"u8, "arm64.v9.1"u8, "arm64.v9.2"u8, "arm64.v9.3"u8, "arm64.v9.4"u8, "arm64.v9.5"u8,
        "arm64.v8.0"u8, "arm64.v8.1"u8, "arm64.v8.2"u8, "arm64.v8.3"u8, "arm64.v8.4"u8, "arm64.v8.5"u8, "arm64.v8.6"u8, "arm64.v8.7"u8, "arm64.v8.8"u8, "arm64.v8.9"u8}.slice();
    if (len(tags) != len(want)){
        Ꮡt.Errorf("Wrong number of tags for GOARM64=v9.5"u8);
    } else {
        foreach (var (i, v) in tags) {
            if (v != want[i]) {
                Ꮡt.Error(wrongTagsForGoarm64V95ˢ);
                break;
            }
        }
    }
    GOARCH = old_goarch;
    GOARM64 = old_goarm64;
}

} // end buildcfg_internal_test_package
