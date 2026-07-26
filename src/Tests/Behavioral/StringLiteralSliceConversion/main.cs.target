namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string helloˢ = "hello"u8;

internal static void Main() {
    var bs = slice<byte>("hello"u8);
    var rs = slice<rune>((@string)"héllo");
    fmt.Println(len(bs), ((@string)bs));
    fmt.Println(len(rs), ((@string)rs));
    fmt.Println(slice<byte>("hi"u8));
    fmt.Println(slice<rune>((@string)"aΩ"));
    fmt.Println(slice<byte>(@"ab"u8));
    fmt.Println(slice<byte>(((@string)(new byte[]{0xff, 0xfe}))));
    @string s = helloˢ;
    fmt.Println(((sstring)slice<byte>(s)) == ((sstring)bs));
}

} // end main_package
