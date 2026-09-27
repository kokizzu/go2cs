namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object mapKeyˢ = (@string)"map key:"u8;
private static readonly object rangeBoundˢ = (@string)"range bound:"u8;
private static readonly object chanSendˢ = (@string)"chan send:"u8;
private static readonly object mapValueˢ = (@string)"map value:"u8;
private static readonly object keyedArrayElementˢ = (@string)"keyed array element:"u8;
private static readonly object parenAssignˢ = (@string)"paren assign:"u8;

internal static void Main() {
    uint8 u = 200;
    uint8 d = 250;
    int16 w = 30000;
    var mk = new map<uint8, @string>{[144] = "wrapped"u8};
    fmt.Println(mapKeyˢ, mk[(uint8)(u + u)]);
    nint n = 0;
    foreach (var _ᴛ1 in range<uint8>((uint8)(d + 10))) {
        n++;
    }
    fmt.Println(rangeBoundˢ, n);
    var ch = new channel<uint8>(1);
    ch.ᐸꟷ((uint8)(u + u));
    fmt.Println(chanSendˢ, ᐸꟷ(ch));
    var m = new map<@string, int16>{["k"u8] = (int16)(w + w)};
    fmt.Println(mapValueˢ, m["k"u8]);
    var arr = new array<uint8>(2){[1] = (uint8)(u * 2)};
    fmt.Println(keyedArrayElementˢ, arr);
    uint8 x = default!;
    x = (uint8)(u + u);
    fmt.Println(parenAssignˢ, x);
}

} // end main_package
