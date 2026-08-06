namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("@string")] partial struct version;

internal static bool isValid(this version v) {
    return v != ""u8;
}

internal static @string tag(this version v) {
    return ((@string)v) + "!"u8;
}

internal static version asVersion(@string s) {
    return ((version)s);
}

internal static void mutate(ж<@string> Ꮡs) {
    ref var s = ref Ꮡs.DerefOrNull();

    s = s + "-mutated"u8;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string go121ˢ = "go1.21"u8;

internal static void Main() {
    version fileVersion = asVersion(go121ˢ);
    if (fileVersion.isValid()) {
        fmt.Println(fileVersion.tag());
    }
    ref var cause = ref heap<@string>(out var Ꮡcause);
    cause = ""u8;
    mutate(Ꮡcause);
    fmt.Println("cause:" + cause);
    @string label = "x"u8;
    for (nint i = 0; i < 2; i++) {
        label = label + "y"u8;
    }
    fmt.Println(label);
}

} // end main_package
