namespace go;

using fmt = fmt_package;
using strings = strings_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    ж<strings.Reader> r = makeReader();
    nint k = 5;
    fmt.Println(r != nil);
    fmt.Println(k);
}

} // end main_package
