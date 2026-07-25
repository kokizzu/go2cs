namespace go;

using fmt = fmt_package;

partial class main_package {

internal static bool isHost(byte c) {
    GoUntyped mask = /* 0 |
	(1<<26-1)<<'A' |
	(1<<26-1)<<'a' |
	(1<<10-1)<<'0' |
	1<<'_' |
	1<<'@' |
	1<<'-' |
	1<<'.' |
	1<<'[' |
	1<<']' |
	1<<':' */
            GoUntyped.Parse("10633823862292363665388054147449749504");
    return ((uint64)((uint64)((((uint64)1).Lsh((uint64)(c))) & (576284830442979328UL)) | (uint64)((((uint64)1).Lsh((uint64)((c - 64)))) & (576460746666278911UL)))) != 0;
}

internal static bool validHeaderValueByte(byte c) {
    GoUntyped mask = /* 0 |
	(1<<(0x7f-0x21)-1)<<0x21 |
	1<<0x20 |
	1<<0x09 */
            GoUntyped.Parse("170141183460469231731687303711589138944");
    return ((uint64)((uint64)((((uint64)1).Lsh((uint64)(c))) & ~(18446744069414584832UL)) | (uint64)((((uint64)1).Lsh((uint64)((c - 64)))) & ~(9223372036854775807UL)))) == 0;
}

internal static uint64 smallHigh(byte c) {
    GoUntyped mask = /* 1<<70 | 1<<3 */
            GoUntyped.Parse("1180591620717411303432");
    return (uint64)((uint64)((((uint64)1).Lsh((uint64)(c))) & (8UL)) | (uint64)((((uint64)1).Lsh((uint64)((c - 64)))) & (64UL)));
}

internal static uintptr nativeWidth(byte c) {
    GoUntyped mask = /* 1<<126 | 1<<65 | 1<<7 */
            GoUntyped.Parse("85070591730234615902737140005361156224");
    return (uintptr)((uintptr)((((uintptr)1).Lsh((uint64)(c))) & (uintptr)((nuint)(128UL))) | (uintptr)((((uintptr)1).Lsh((uint64)((c - 64)))) & (uintptr)((nuint)(4611686018427387906UL))));
}

internal static void Main() {
    nint hosts = 0;
    nint values = 0;
    for (nint i = 0; i < 256; i++) {
        var c = (byte)i;
        if (isHost(c)) {
            hosts++;
        }
        if (validHeaderValueByte(c)) {
            values++;
        }
    }
    fmt.Println("isHost count:", hosts);
    fmt.Println("validHeaderValueByte count:", values);
    fmt.Println("isHost samples:", isHost((rune)'a'), isHost((rune)'Z'), isHost((rune)'9'), isHost((rune)':'), isHost((rune)' '), isHost(200));
    fmt.Println("value samples:", validHeaderValueByte((rune)'a'), validHeaderValueByte(0x09), validHeaderValueByte(0x00), validHeaderValueByte(0x80));
    foreach (var (_, c) in new byte[]{3, 6, 70, 8, 64}.slice()) {
        fmt.Printf("smallHigh(%d) = %d\n"u8, c, smallHigh(c));
    }
    foreach (var (_, c) in new byte[]{7, 65, 126, 8, 64}.slice()) {
        fmt.Printf("nativeWidth(%d) = %d\n"u8, c, nativeWidth(c));
    }
}

} // end main_package
