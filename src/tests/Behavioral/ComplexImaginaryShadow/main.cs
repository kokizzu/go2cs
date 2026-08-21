[assembly: go.GoPositionMap("main.go", "main.cs", "AAwWgoKClKaCgoI=")]

namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string nonzeroˢ = "nonzero"u8;
private static readonly @string zeroˢ = "zero"u8;

internal static @string encComplex(nint i, complex128 c) {
    _ = i;
    if (c != 0D.i()) {
        return nonzeroˢ;
    }
    return zeroˢ;
}

internal static void Main() {
    fmt.Println(encComplex(1, complex(0D, 0D)));
    fmt.Println(encComplex(2, complex(3D, -4D)));
    fmt.Println(encComplex(3, 5D.i()));
}

} // end main_package
