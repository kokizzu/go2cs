namespace go;

using fmt = fmt_package;
using CrossPkgLib = CrossPkgLib_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸCrossPkgLib() {
    builtin.initPackage(typeof(CrossPkgLib_package));
}

internal static void Main() {
    var p = peekPtr(7)._<ж<CrossPkgLibꓸStatus>>();
    fmt.Println((~p).Code);
    fmt.Println((float64)CrossPkgLib.Freezing());
}

} // end main_package
