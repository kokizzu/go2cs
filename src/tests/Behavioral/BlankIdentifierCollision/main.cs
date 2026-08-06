namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("num:nint")] partial struct Code;

public static Code A => /* iota */ 0;
internal static Code _ᴛ1ʗ => 1;
public static Code B => 2;
internal static Code _ᴛ2ʗ => 3;
public static Code C => 4;

internal static void _ᴛ3() {
    if (A + B + C < 0) {
        throw panic("unreachable");
    }
    Code x = A;
    _ = x;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object multiBlankOkˢ = (@string)"multiBlank ok"u8;

internal static void multiBlank() {
    nint a = 1;
    nint b = 2;
    nint c = 3;
    nint d = 4;
    _ = a;
    _ = b;
    _ = c;
    _ = d;
    fmt.Println(multiBlankOkˢ);
}

internal static void Main() {
    fmt.Println(A, B, C);
    multiBlank();
}

} // end main_package
