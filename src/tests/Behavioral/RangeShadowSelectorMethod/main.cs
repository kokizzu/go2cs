namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct writer {
    internal slice<@string> @out;
}

[GoRecv] internal static void typ(this ref writer w, @string typ) {
    w.@out = append(w.@out, "t:"u8 + typ);
    if (typ == "top"u8) {
        foreach (var (_, typΔ1) in new @string[]{"a"u8, "b"u8}.slice()) {
            w.typ(typΔ1);
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string topˢ = "top"u8;

internal static void Main() {
    var w = Ꮡ(new writer(nil));
    w.typ(topˢ);
    foreach (var (_, line) in (~w).@out) {
        fmt.Println(line);
    }
}

} // end main_package
