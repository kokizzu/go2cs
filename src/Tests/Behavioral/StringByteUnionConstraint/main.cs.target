namespace go;

using fmt = fmt_package;

partial class main_package {

internal static uint32 byteSum<T>(T s)
    where T : /* string | []byte */ IByteSeq<byte>, new()
{
    uint32 h = default!;
    for (nint i = 0; i < len(s); i++) {
        h = h * 31 + (uint32)s[i];
    }
    return h;
}

internal static bool prefixMatch<T>(T s, T sep)
    where T : /* string | []byte */ IByteSeq<byte>, new()
{
    nint n = len(sep);
    if (len(s) < n) {
        return false;
    }
    return new @string(((T)(s[..(int)(n)]))) == new @string(sep);
}

internal static void Main() {
    @string str = "hello"u8;
    var bs = slice<byte>(str);
    @string hel = "hel"u8;
    var helBytes = slice<byte>(hel);
    fmt.Println((@string)"sum(string):"u8, byteSum(str));
    fmt.Println((@string)"sum([]byte):"u8, byteSum(bs));
    fmt.Println((@string)"prefix string:"u8, prefixMatch(str, hel));
    fmt.Println((@string)"prefix []byte:"u8, prefixMatch(bs, helBytes));
    fmt.Println((@string)"prefix miss:"u8, prefixMatch(str, (@string)"xyz"));
    fmt.Println((@string)"prefix too long:"u8, prefixMatch(str, (@string)"hello world"));
    fmt.Println((@string)"last(string):"u8, lastByte(str));
    fmt.Println((@string)"last([]byte):"u8, lastByte(bs));
    fmt.Println((@string)"sum:"u8, digitSum((@string)"12:34"), digitSum(slice<byte>("56:78"u8)));
    fmt.Println((@string)"head:"u8, headSum((@string)"x98:76"), headSum(slice<byte>("y10:23"u8)));
    fmt.Println((@string)"appendRun(string):"u8, ((@string)appendRun(default!, (@string)"abcde")));
    fmt.Println((@string)"appendRun([]byte):"u8, ((@string)appendRun(slice<byte>("<"u8), slice<byte>("abcde"u8))));
}

internal static nint digitSum<T>(T s)
    where T : /* []byte | string */ IByteSeq<byte>, new()
{
    var parse = (T part) => {
        nint n = 0;
        foreach (var (_, c) in new slice<byte>(part)) {
            n = n * 10 + (nint)(c - (rune)'0');
        }
        return n;
    };
    return parse(((T)(s[0..2]))) + parse(((T)(s[3..5])));
}

internal static T trimHead<T>(T s, nint n)
    where T : /* []byte | string */ IByteSeq<byte>, new()
{
    for (nint i = 0; i < n; i++) {
        if (len(s) > 1) {
            s = ((T)(s[1..]));
        }
    }
    return ((T)(s[0..]));
}

internal static nint headSum<T>(T s)
    where T : /* []byte | string */ IByteSeq<byte>, new()
{
    return digitSum(trimHead(s, 1));
}

internal static byte lastByte<T>(T s)
    where T : /* []byte | string */ IByteSeq<byte>, new()
{
    return s[len(s) - 1];
}

internal static slice<byte> appendRun<Bytes>(slice<byte> dst, Bytes src)
    where Bytes : /* []byte | string */ IByteSeq<byte>, new()
{
    dst = append(dst, (byte)((rune)'['));
    if (len(src) > 1) {
        dst = append(dst, ((Bytes)(src[1..(int)(len(src) - 1)])).ꓸꓸꓸ);
        dst = append(dst, ((Bytes)(src[(int)(len(src) - 1)..])).ꓸꓸꓸ);
    }
    dst = append(dst, (byte)((rune)']'));
    return dst;
}

} // end main_package
