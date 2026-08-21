[assembly: go.GoPositionMap("lib.go", "lib.cs", "AAoSgNSApIA=")]

namespace go.ForeignPairNumericConv;

partial class convlib_package {

[GoType("num:uintptr")] partial struct Handle;

[GoType("num:uintptr")] partial struct Token;

public static Token NewToken(uintptr v) {
    return ((Token)v);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string handleˢ = "handle"u8;

public static @string String(this Handle h) {
    return handleˢ;
}

public static uintptr Raw(this Token t) {
    return (uintptr)t;
}

} // end convlib_package
