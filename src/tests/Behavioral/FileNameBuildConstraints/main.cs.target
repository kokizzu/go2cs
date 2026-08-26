namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    fmt.Printf("%s:"u8, tableName);
    foreach (var (_, v) in lookupTable) {
        fmt.Printf(" %d"u8, v);
    }
    fmt.Println();
}

} // end main_package
