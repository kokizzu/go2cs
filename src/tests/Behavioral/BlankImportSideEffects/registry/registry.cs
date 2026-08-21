[assembly: go.GoPositionMap("registry.go", "registry.cs", "AAgUgqiCgqiC")]

namespace go.BlankImportSideEffects;

partial class registry_package {

internal static map<@string, @string> decoders = new map<@string, @string>{};

public static void Register(@string name, @string describe) {
    decoders[name] = describe;
}

public static (@string, bool) Lookup(@string name) {
    var (describe, ok) = decoders[name, ꟷ];
    return (describe, ok);
}

public static nint Count() {
    return len(decoders);
}

} // end registry_package
