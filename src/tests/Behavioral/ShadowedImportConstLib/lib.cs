[assembly: go.GoPositionMap("lib.go", "lib.cs", "AA4ggA==")]

namespace go;

partial class ShadowedImportConstLib_package {

[GoType("num:int64")] partial struct Span;

public static Span ΔPeak => 60;

[GoType] partial struct Meter {
    public nint Level;
}

public static nint Peak(this Meter m) {
    return m.Level + 1;
}

} // end ShadowedImportConstLib_package
