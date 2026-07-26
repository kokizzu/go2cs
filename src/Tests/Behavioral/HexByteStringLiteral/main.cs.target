namespace go;

using fmt = fmt_package;

partial class main_package {

internal static readonly @string data = ((@string)(new byte[]{0x50, 0x4b, 0x03, 0x04, 0xdb, 0x35, 0x30, 0xff, 0x92, 0x00, 0x4c, 0x4d, 0x54}));

internal static readonly @string octalData = ((@string)(new byte[]{0xff, 0x80, 0xc3, 0xbf, 0x41, 0x00, 0x7f, 0x5a}));

internal static readonly @string asciiOctal = "\u0041\u0042\u0009\u0043"u8;

internal static nint get4(@string s, nint i) {
    return (nint)((nint)((nint)((nint)s[i] | ((nint)s[i + 1] << (int)(8))) | ((nint)s[i + 2] << (int)(16))) | ((nint)s[i + 3] << (int)(24)));
}

internal static void dump(@string label, @string s) {
    fmt.Println(label);
    fmt.Println(len(s));
    for (nint i = 0; i < len(s); i++) {
        fmt.Printf("%d "u8, s[i]);
    }
    fmt.Println();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string octalDataˢ = "octalData"u8;
private static readonly @string asciiOctalˢ = "asciiOctal"u8;
private static readonly @string localˢ = "local"u8;

internal static void Main() {
    fmt.Println(len(data));
    for (nint i = 0; i < len(data); i++) {
        fmt.Printf("%d "u8, data[i]);
    }
    fmt.Println();
    fmt.Println(get4(data, 0));
    dump(octalDataˢ, octalData);
    dump(asciiOctalˢ, asciiOctal);
    @string local = ((@string)(new byte[]{0xfe, 0xfd}));
    dump(localˢ, local);
}

} // end main_package
