namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoInit] internal static void initΔ4() {
    order = append(order, "main#1"u8);
}

internal static void Main() {
    fmt.Println(len(order));
    foreach (var (_, name) in order) {
        fmt.Println(name);
    }
}

} // end main_package
