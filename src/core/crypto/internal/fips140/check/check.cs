// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package check implements the FIPS 140 load-time code+data verification.
// Every FIPS package providing cryptographic functionality except hmac and sha256
// must import crypto/internal/fips140/check, so that the verification happens
// before initialization of package global variables.
// The hmac and sha256 packages are used by this package, so they cannot import it.
// Instead, those packages must be careful not to change global variables during init.
// (If necessary, we could have check call a PostCheck function in those packages
// after the check has completed.)
namespace go.crypto.@internal.fips140;

using fips140 = go.crypto.@internal.fips140_package;
using hmac = go.crypto.@internal.fips140.hmac_package;
using sha256 = go.crypto.@internal.fips140.sha256_package;
using byteorder = go.crypto.@internal.fips140deps.byteorder_package;
using godebug = go.crypto.@internal.fips140deps.godebug_package;
using io = io_package;
using @unsafe = unsafe_package;
using go.crypto.@internal;
using go.crypto.@internal.fips140;
using go.crypto.@internal.fips140deps;

partial class check_package {

// Verified is set when verification succeeded. It can be expected to always be
// true when [fips140.Enabled] is true, or init would have panicked.
public static bool Verified;

// Linkinfo holds the go:fipsinfo symbol prepared by the linker.
// See cmd/link/internal/ld/fips.go for details.
//
//go:linkname Linkinfo go:fipsinfo

[GoType("dyn")] partial struct Linkinfoᴛ1_Sects {
    // Note: These must be unsafe.Pointer, not uintptr,
    // or else checkptr panics about turning uintptrs
    // into pointers into the data segment during
    // go test -race.
    public @unsafe.Pointer Start;
    public @unsafe.Pointer End;
}

[GoType("dyn")] partial struct Linkinfoᴛ1 {
    public array<byte> Magic = new(16);
    public array<byte> Sum = new(32);
    public uintptr Self;
    public array<Linkinfoᴛ1_Sects> Sects = new(4);
}
public static Linkinfoᴛ1 Linkinfo = new();

// "\xff"+fipsMagic is the expected linkinfo.Magic.
// We avoid writing that explicitly so that the string does not appear
// elsewhere in normal binaries, just as a precaution.
internal static readonly @string fipsMagic = ((@string)(new byte[]{0x20, 0x47, 0x6f, 0x20, 0x66, 0x69, 0x70, 0x73, 0x69, 0x6e, 0x66, 0x6f, 0x20, 0xff, 0x00}));

internal static array<byte> zeroSum = new(32);

[GoInit] internal static void init() {
    if (!fips140.Enabled) {
        return;
    }
    {
        var err = fips140.Supported(); if (err != default!) {
            throw panic("fips140: " + err.Error());
        }
    }
    if (Linkinfo.Magic[0] != 0xff || ((sstring)(Linkinfo.Magic[1..])) != fipsMagic || Linkinfo.Sum == zeroSum) {
        throw panic("fips140: no verification checksum found");
    }
    var h = hmac.New<ж<sha256.Digest>>(sha256.New, new slice<byte>(32));
    var w = ((io.Writer)new hmac_HMACжWriter(h));
    /*
		// Uncomment for debugging.
		// Commented (as opposed to a const bool flag)
		// to avoid import "os" in default builds.
		f, err := os.Create("fipscheck.o")
		if err != nil {
			panic(err)
		}
		w = io.MultiWriter(h, f)
	*/
    w.Write(slice<byte>("go fips object v1\n"u8));
    array<byte> nbuf = new(8);
    foreach (var (_, sect) in Linkinfo.Sects.ΔRangeSnapshot()) {
        var n = (uintptr)sect.End - (uintptr)sect.Start;
        byteorder.BEPutUint64(nbuf[..], (uint64)n);
        w.Write(nbuf[..]);
        w.Write(@unsafe.Slice((ж<byte>)(uintptr)(sect.Start), n));
    }
    var sum = h.Sum(default!);
    if (new array<byte>(sum, 32) != Linkinfo.Sum) {
        throw panic("fips140: verification mismatch");
    }
    // "The temporary value(s) generated during the integrity test of the
    // module’s software or firmware shall [05.10] be zeroised from the module
    // upon completion of the integrity test"
    clear(sum);
    clear(nbuf[..]);
    h.Reset();
    if (godebug.Value("#fips140"u8) == "debug"u8) {
        println((@string)"fips140: verified code+data"u8);
    }
    Verified = true;
}

} // end check_package
