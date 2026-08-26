namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    nint size = 5;
    slice<nint> s = default!;
    foreach (var i in range(size)) {
        s = append(s, i);
    }
    fmt.Println(s);
}

} // end main_package
