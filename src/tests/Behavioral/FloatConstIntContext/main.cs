namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static nint takesInt(nint n) {
    return n * 10;
}

internal static void Main() {
    fmt.Println(takesInt(1));
    fmt.Println(takesInt(-2));
    var x = 1.5D;
    fmt.Println(x);
}

} // end main_package
