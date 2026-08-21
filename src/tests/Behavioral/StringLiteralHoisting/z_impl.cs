[assembly: go.GoPositionMap("z_impl.go", "z_impl.cs", "AAkQgqqCqIKC")]

namespace go;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string derivedThroughALaterFileˢ = "derived through a later file"u8;

internal static @string describe() {
    return derivedThroughALaterFileˢ;
}

internal static @string reuseShared() {
    return sharedAcrossFilesˢ;
}

[GoInit] internal static void init() {
    if (reuseShared() != "shared across files"u8) {
        throw panic("init literal must not be hoisted");
    }
}

} // end main_package
