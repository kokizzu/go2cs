namespace go;

using fmt = fmt_package;
using CrossPkgFuncLib = CrossPkgFuncLib_package;
using CrossPkgLib = CrossPkgLib_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸCrossPkgFuncLib() {
    builtin.initPackage(typeof(CrossPkgFuncLib_package));
}

internal static void Main() {
    fmt.Println(CrossPkgFuncLib.Count(new Func<CrossPkgLibꓸStatus, bool>(CrossPkgFuncLib.Hot)));
    Func<CrossPkgLibꓸStatus, bool> pick = CrossPkgFuncLib.Hot;
    fmt.Println(CrossPkgFuncLib.Count(pick));
}

} // end main_package
