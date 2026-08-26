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

[GoType("map[CrossPkgLib.Ticks, Func<nint, nint, nint>]")] partial struct opTable;

internal static void Main() {
    var ops = new opTable(new map<CrossPkgLib.Ticks, Func<nint, nint, nint>>{
        [((CrossPkgLib.Ticks)2)] = (nint a, nint b) => a + b,
        [((CrossPkgLib.Ticks)3)] = (nint a, nint b) => a * b
    });
    fmt.Println(ops[((CrossPkgLib.Ticks)2)](4, 5), ops[((CrossPkgLib.Ticks)3)](4, 5), len(ops));
}

} // end main_package
