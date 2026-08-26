// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using asn1 = go.encoding.asn1_package;
using testing = testing_package;
using cryptobyte_asn1 = vendor.golang.org.x.crypto.cryptobyte.asn1_package;
using go.encoding;
using static go.crypto.x509_package;

partial class x509_internal_test_package {

[GoType("dyn")] internal partial struct TestParseASN1String_tests {
    internal @string name;
    internal cryptobyte_asn1.Tag tag;
    internal slice<byte> value;
    internal @string expected;
    internal @string expectedErr;
}

public static void TestParseASN1String(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    var tests = new TestParseASN1String_tests[]{
        new(
            name: "T61String"u8,
            tag: cryptobyte_asn1.T61String,
            value: new byte[]{80, 81, 82}.slice(),
            expected: ((@string)"PQR"u8)
        ),
        new(
            name: "PrintableString"u8,
            tag: cryptobyte_asn1.PrintableString,
            value: new byte[]{80, 81, 82}.slice(),
            expected: ((@string)"PQR"u8)
        ),
        new(
            name: "PrintableString (invalid)"u8,
            tag: cryptobyte_asn1.PrintableString,
            value: new byte[]{1, 2, 3}.slice(),
            expectedErr: "invalid PrintableString"u8
        ),
        new(
            name: "UTF8String"u8,
            tag: cryptobyte_asn1.UTF8String,
            value: new byte[]{80, 81, 82}.slice(),
            expected: ((@string)"PQR"u8)
        ),
        new(
            name: "UTF8String (invalid)"u8,
            tag: cryptobyte_asn1.UTF8String,
            value: new byte[]{255}.slice(),
            expectedErr: "invalid UTF-8 string"u8
        ),
        new(
            name: "BMPString"u8,
            tag: ((cryptobyte_asn1.Tag)asn1.TagBMPString),
            value: new byte[]{80, 81}.slice(),
            expected: ((@string)"偑"u8)
        ),
        new(
            name: "BMPString (invalid length)"u8,
            tag: ((cryptobyte_asn1.Tag)asn1.TagBMPString),
            value: new byte[]{255}.slice(),
            expectedErr: "invalid BMPString"u8
        ),
        new(
            name: "IA5String"u8,
            tag: cryptobyte_asn1.IA5String,
            value: new byte[]{80, 81}.slice(),
            expected: ((@string)"PQ"u8)
        ),
        new(
            name: "IA5String (invalid)"u8,
            tag: cryptobyte_asn1.IA5String,
            value: new byte[]{255}.slice(),
            expectedErr: "invalid IA5String"u8
        ),
        new(
            name: "NumericString"u8,
            tag: ((cryptobyte_asn1.Tag)asn1.TagNumericString),
            value: new byte[]{49, 50}.slice(),
            expected: ((@string)"12"u8)
        ),
        new(
            name: "NumericString (invalid)"u8,
            tag: ((cryptobyte_asn1.Tag)asn1.TagNumericString),
            value: new byte[]{80}.slice(),
            expectedErr: "invalid NumericString"u8
        )
    }.slice();
    foreach (var (_, vᴛ1) in tests) {
        ref var tc = ref heap(new TestParseASN1String_tests(), out var Ꮡtc);
        tc = vᴛ1;

        var tcʗ1 = tc;
        Ꮡt.Run(tc.name, (ж<testing.T> tΔ1) => {
            var (@out, err) = parseASN1String(tcʗ1.tag, tcʗ1.value);
            if (err != default! && err.Error() != tcʗ1.expectedErr){
                tΔ1.Fatalf("parseASN1String returned unexpected error: got %q, want %q"u8, err, tcʗ1.expectedErr);
            } else 
            if (err == default! && tcʗ1.expectedErr != ""u8) {
                tΔ1.Fatalf("parseASN1String didn't fail, expected: %s"u8, tcʗ1.expectedErr);
            }
            if (@out != tcʗ1.expected) {
                tΔ1.Fatalf("parseASN1String returned unexpected value: got %q, want %q"u8, @out, tcʗ1.expected);
            }
        });
    }
}

} // end x509_internal_test_package
