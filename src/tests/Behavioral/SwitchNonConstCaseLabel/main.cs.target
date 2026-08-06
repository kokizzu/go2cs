namespace go;

using fmt = fmt_package;

partial class main_package {

public static UntypedInt Width => 8;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string widthˢ = "width"u8;
private static readonly @string otherˢ = "other"u8;

internal static @string classify(uintptr n) {
    var exprᴛ1 = n;
    if (exprᴛ1 == Width) {
        return widthˢ;
    }
    { /* default: */
        return otherˢ;
    }

}

internal static @string matchVar(uintptr p, uintptr a, uintptr b) {
    var exprᴛ1 = p;
    if (exprᴛ1 == a) {
        return "A"u8;
    }
    if (exprᴛ1 == b) {
        return "B"u8;
    }
    { /* default: */
        return "?"u8;
    }

}

internal static void Main() {
    fmt.Println(classify(8));
    fmt.Println(classify(3));
    fmt.Println(matchVar(1, 1, 2));
    fmt.Println(matchVar(2, 1, 2));
    fmt.Println(matchVar(9, 1, 2));
}

} // end main_package
