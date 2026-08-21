[assembly: go.GoPositionMap("a_decls.go", "a_decls.cs", "")]

namespace go;

partial class main_package {

internal static @string pkgLevelLiteral = "package level literal stays inline"u8;

internal static @string derivedAtInit;
internal static void initᴛderivedAtInit() { derivedAtInit = describe(); }

[GoType] partial struct box {
    internal @string name;
    internal @string tag;
}

[GoType("@string")] partial struct label;

} // end main_package
