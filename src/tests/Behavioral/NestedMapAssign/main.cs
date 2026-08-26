namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void addEdge(map<@string, map<@string, bool>> g, @string from, @string to) {
    if (g[from] == default!) {
        g[from] = new map<@string, bool>{};
    }
    g[from].Set(to, true);
}

internal static void Main() {
    var g = new map<@string, map<@string, bool>>{};
    addEdge(g, "a"u8, "b"u8);
    addEdge(g, "a"u8, "c"u8);
    addEdge(g, "b"u8, "c"u8);
    fmt.Println(g["a"u8]["b"u8], g["a"u8]["c"u8], g["b"u8]["c"u8], g["a"u8]["z"u8]);
    fmt.Println(len(g["a"u8]));
}

} // end main_package
