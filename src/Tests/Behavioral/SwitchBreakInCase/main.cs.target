namespace go;

using fmt = fmt_package;

partial class main_package {

internal static readonly UntypedInt stateA = iota;
internal static readonly UntypedInt stateB = 1;
internal static readonly UntypedInt stateC = 2;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string noneˢ = "none"u8;
private static readonly @string bNoflagˢ = "b-noflag"u8;
private static readonly @string otherˢ = "other"u8;

internal static @string classify(nint n, bool flag) {
    @string result = noneˢ;
    var exprᴛ1 = n;
    if (exprᴛ1 == stateA) {
        do {
            if (flag) {
                break;
            }
            result = "a"u8;
        } while (false);
    }
    else if (exprᴛ1 == stateB) {
        do {
            result = "b"u8;
            if (flag) {
                break;
            }
            result = bNoflagˢ;
        } while (false);
    }
    else { /* default: */
        result = otherˢ;
    }

    return result;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string oneTwoˢ = "one-two"u8;
private static readonly @string oneOtherˢ = "one-other"u8;
private static readonly @string threeˢ = "three"u8;

internal static @string pick(nint x, nint y) {
    @string @out = noneˢ;
Big:
    switch (x) {
    case 1: {
        switch (y) {
        case 2: {
            @out = oneTwoˢ;
            goto break_Big;
            break;
        }}

        @out = oneOtherˢ;
        break;
    }
    case 3: {
        @out = threeˢ;
        break;
    }}

    break_Big:;
    return @out;
}

internal static void Main() {
    fmt.Println(pick(1, 2), pick(1, 5), pick(3, 0));
    fmt.Println(classify(stateA, true));
    fmt.Println(classify(stateA, false));
    fmt.Println(classify(stateB, true));
    fmt.Println(classify(stateB, false));
    fmt.Println(classify(stateC, false));
}

} // end main_package
