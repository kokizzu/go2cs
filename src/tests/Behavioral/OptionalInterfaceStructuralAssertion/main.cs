namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial interface Describer {
    @string Describe();
}

[GoType] partial interface Tagger {
    @string Describe();
    @string Tag();
}

[GoType] partial struct widget {
    internal @string name;
}

internal static @string Describe(this widget w) {
    return "describe:"u8 + w.name;
}

internal static @string Tag(this widget w) {
    return "tag:"u8 + w.name;
}

[GoType] partial struct plain {
}

internal static Describer newDescriber(@string name) {
    return new widget(name: name);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string alphaˢ = "alpha"u8;
private static readonly object notATaggerˢ = (@string)"not a Tagger"u8;

internal static void Main() {
    var d = newDescriber(alphaˢ);
    fmt.Println(d.Describe());
    var items = new any[]{new widget(name: "beta"u8), new plain(nil)}.slice();
    foreach (var (_, it) in items) {
        {
            var (t, ok) = it._<Tagger>(ᐧ); if (ok){
                fmt.Println(t.Describe(), t.Tag());
            } else {
                fmt.Println(notATaggerˢ);
            }
        }
    }
}

} // end main_package
