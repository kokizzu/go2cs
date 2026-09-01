namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string negˢ = "neg"u8;
private static readonly @string equalˢ = "equal"u8;
private static readonly @string greaterˢ = "greater"u8;
private static readonly @string lessˢ = "less"u8;

internal static @string classify(nint x, nint y) {
    switch (ᐧ) {
    case {} when x is < 0: {
        return negˢ;
    }
    case {} when x == y: {
        return equalˢ;
    }
    case {} when x > y: {
        return greaterˢ;
    }}

    return lessˢ;
}

internal static void Main() {
    fmt.Println(classify(-1, 5));
    fmt.Println(classify(5, 5));
    fmt.Println(classify(7, 3));
    fmt.Println(classify(2, 8));
}

} // end main_package
