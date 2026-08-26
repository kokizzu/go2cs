namespace go;

using CrossPkgBox = CrossPkgBox_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸCrossPkgBox() {
    builtin.initPackage(typeof(CrossPkgBox_package));
}

internal static any peekPtr(nint code) {
    var b = CrossPkgBox.New(code);
    ref var s = ref heap<CrossPkgLibꓸStatus>(out var Ꮡs);
    s = b.S;
    return Ꮡs;
}

} // end main_package
