// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("crypto/internal/hpke/hpke_test.go", "hpke_test.cs", "ACQqgoKClKaCgoKClKaCgoKCgoKUlAAZBqKCgpaKgIKmspKEgoKUgIKkgoKUgIKkgoKUgIKmgoKCgpaEloCU3oKWgoKUgoKUgoKUgoKUgoKWgqKCgpSCpoKClIKWgoKClII=")]

namespace go.crypto.@internal;

using bytes = bytes_package;
using hex = encoding.hex_package;
using json = encoding.json_package;
using os = os_package;
using strconv = strconv_package;
using strings = strings_package;
using testing = testing_package;
using ecdh = go.crypto.ecdh_package;
// blank import: go.crypto.sha256_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
// blank import: go.crypto.sha512_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
using crypto = crypto_package;
using encoding;
using go.crypto;
using static go.crypto.@internal.hpke_package;

partial class hpke_internal_test_package {

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸcryptoꓸsha256() {
    builtin.initPackage(typeof(go.crypto.sha256_package));
}

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸcryptoꓸsha512() {
    builtin.initPackage(typeof(go.crypto.sha512_package));
}

internal static slice<byte> mustDecodeHex(ж<testing.T> Ꮡt, @string @in) {
    var (b, err) = hex.DecodeString(@in);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    return b;
}

internal static map<@string, @string> parseVectorSetup(@string vector) {
    var vals = new map<@string, @string>{};
    foreach (var (_, l) in strings.Split(vector, "\n"u8)) {
        var fields = strings.Split(l, ": "u8);
        vals[fields[0]] = fields[1];
    }
    return vals;
}

internal static slice<map<@string, @string>> parseVectorEncryptions(@string vector) {
    var vals = new map<@string, @string>[]{}.slice();
    foreach (var (_, section) in strings.Split(vector, "\n\n"u8)) {
        var e = new map<@string, @string>{};
        foreach (var (_, l) in strings.Split(section, "\n"u8)) {
            var fields = strings.Split(l, ": "u8);
            e[fields[0]] = fields[1];
        }
        vals = append(vals, e);
    }
    return vals;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string testdataRfc9180Vectorsˢ = "testdata/rfc9180-vectors.json"u8;
internal static readonly @string kemIdˢ = "kem_id"u8;
internal static readonly object unsupportedKemˢ = (@string)"unsupported KEM"u8;
internal static readonly @string kdfIdˢ = "kdf_id"u8;
internal static readonly object unsupportedKdfˢ = (@string)"unsupported KDF"u8;
internal static readonly @string aeadIdˢ = "aead_id"u8;
internal static readonly object unsupportedAeadˢ = (@string)"unsupported AEAD"u8;
internal static readonly @string infoˢ = "info"u8;
internal static readonly @string pkRmˢ = "pkRm"u8;
internal static readonly @string skEmˢ = "skEm"u8;
internal static readonly @string encˢ = "enc"u8;
internal static readonly @string exporterSecretˢ = "exporter_secret"u8;
internal static readonly @string sequenceNumberˢ = "sequence number"u8;
internal static readonly @string nonceˢ = "nonce"u8;
internal static readonly @string aadˢ = "aad"u8;

[GoType("dyn")] internal partial struct TestRFC9180Vectors_vectors {
    public @string Name;
    public @string Setup;
    public @string Encryptions;
}

public static void TestRFC9180Vectors(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    var (vectorsJSON, err) = os.ReadFile(testdataRfc9180Vectorsˢ);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    ref var vectors = ref heap<slice<TestRFC9180Vectors_vectors>>(out var Ꮡvectors);
    {
        var errΔ1 = json.Unmarshal(vectorsJSON, Ꮡvectors); if (errΔ1 != default!) {
            Ꮡt.Fatal(errΔ1);
        }
    }
    foreach (var (_, vᴛ1) in vectors) {
        ref var vector = ref heap(new TestRFC9180Vectors_vectors(), out var Ꮡvector);
        vector = vᴛ1;

        var vectorʗ1 = vector;
        Ꮡt.Run(vector.Name, (ж<testing.T> tΔ1) => {
            var setup = parseVectorSetup(vectorʗ1.Setup);
            var (kemID, errΔ2) = strconv.Atoi(setup[kemIdˢ]);
            if (errΔ2 != default!) {
                tΔ1.Fatal(errΔ2);
            }
            {
                var (_, ok) = SupportedKEMs[(uint16)kemID, ꟷ]; if (!ok) {
                    tΔ1.Skip(unsupportedKemˢ);
                }
            }
            (var kdfID, errΔ2) = strconv.Atoi(setup[kdfIdˢ]);
            if (errΔ2 != default!) {
                tΔ1.Fatal(errΔ2);
            }
            {
                var (_, ok) = SupportedKDFs[(uint16)kdfID, ꟷ]; if (!ok) {
                    tΔ1.Skip(unsupportedKdfˢ);
                }
            }
            (var aeadID, errΔ2) = strconv.Atoi(setup[aeadIdˢ]);
            if (errΔ2 != default!) {
                tΔ1.Fatal(errΔ2);
            }
            {
                var (_, ok) = SupportedAEADs[(uint16)aeadID, ꟷ]; if (!ok) {
                    tΔ1.Skip(unsupportedAeadˢ);
                }
            }
            var info = mustDecodeHex(tΔ1, setup[infoˢ]);
            var pubKeyBytes = mustDecodeHex(tΔ1, setup[pkRmˢ]);
            (var pub, errΔ2) = ParseHPKEPublicKey((uint16)kemID, pubKeyBytes);
            if (errΔ2 != default!) {
                tΔ1.Fatal(errΔ2);
            }
            var ephemeralPrivKey = mustDecodeHex(tΔ1, setup[skEmˢ]);
            var ephemeralPrivKeyʗ1 = ephemeralPrivKey;
            testingOnlyGenerateKey = () => SupportedKEMs[(uint16)kemID].curve.NewPrivateKey(ephemeralPrivKeyʗ1);
            tΔ1.Cleanup(() => {
                testingOnlyGenerateKey = default!;
            });
            (var encap, var context, errΔ2) = SetupSender(
                (uint16)kemID,
                (uint16)kdfID,
                (uint16)aeadID,
                pub.OrTypedNil(),
                info);
            if (errΔ2 != default!) {
                tΔ1.Fatal(errΔ2);
            }
            var expectedEncap = mustDecodeHex(tΔ1, setup[encˢ]);
            if (!bytes_package.Equal(encap, expectedEncap)) {
                tΔ1.Errorf("unexpected encapsulated key, got: %x, want %x"u8, encap, expectedEncap);
            }
            var expectedSharedSecret = mustDecodeHex(tΔ1, setup[sharedSecretˢ]);
            if (!bytes_package.Equal((~context).sharedSecret, expectedSharedSecret)) {
                tΔ1.Errorf("unexpected shared secret, got: %x, want %x"u8, (~context).sharedSecret, expectedSharedSecret);
            }
            var expectedKey = mustDecodeHex(tΔ1, setup[keyˢ]);
            if (!bytes_package.Equal((~context).key, expectedKey)) {
                tΔ1.Errorf("unexpected key, got: %x, want %x"u8, (~context).key, expectedKey);
            }
            var expectedBaseNonce = mustDecodeHex(tΔ1, setup[baseNonceˢ]);
            if (!bytes_package.Equal((~context).baseNonce, expectedBaseNonce)) {
                tΔ1.Errorf("unexpected base nonce, got: %x, want %x"u8, (~context).baseNonce, expectedBaseNonce);
            }
            var expectedExporterSecret = mustDecodeHex(tΔ1, setup[exporterSecretˢ]);
            if (!bytes_package.Equal((~context).exporterSecret, expectedExporterSecret)) {
                tΔ1.Errorf("unexpected exporter secret, got: %x, want %x"u8, (~context).exporterSecret, expectedExporterSecret);
            }
            foreach (var (_, enc) in parseVectorEncryptions(vectorʗ1.Encryptions)) {
                var contextʗ1 = context;
                var encʗ1 = enc;
                tΔ1.Run("seq num " + enc[sequenceNumberˢ], (ж<testing.T> tΔ2) => {
                    var (seqNum, errΔ3) = strconv.Atoi(encʗ1[sequenceNumberˢ]);
                    if (errΔ3 != default!) {
                        tΔ2.Fatal(errΔ3);
                    }
                    contextʗ1.Value.seqNum = new uint128(lo: (uint64)seqNum);
                    var expectedNonce = mustDecodeHex(tΔ2, encʗ1[nonceˢ]);
                    // We can't call nextNonce, because it increments the sequence number,
                    // so just compute it directly.
                    var computedNonce = (~contextʗ1).seqNum.bytes()[(int)(16 - (~contextʗ1).aead.NonceSize())..];
                    foreach (var (i, _) in (~contextʗ1).baseNonce) {
                        computedNonce[i] ^= (byte)((~contextʗ1).baseNonce[i]);
                    }
                    if (!bytes_package.Equal(computedNonce, expectedNonce)) {
                        tΔ2.Errorf("unexpected nonce: got %x, want %x"u8, computedNonce, expectedNonce);
                    }
                    var expectedCiphertext = mustDecodeHex(tΔ2, encʗ1["ct"u8]);
                    (var ciphertext, errΔ3) = contextʗ1.Seal(mustDecodeHex(tΔ2, encʗ1[aadˢ]), mustDecodeHex(tΔ2, encʗ1["pt"u8]));
                    if (errΔ3 != default!) {
                        tΔ2.Fatal(errΔ3);
                    }
                    if (!bytes_package.Equal(ciphertext, expectedCiphertext)) {
                        tΔ2.Errorf("unexpected ciphertext: got %x want %x"u8, ciphertext, expectedCiphertext);
                    }
                });
            }
        });
    }
}

} // end hpke_internal_test_package
