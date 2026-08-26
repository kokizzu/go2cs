namespace go;

using CrossPkgLib = CrossPkgLib_package;

partial class CrossPkgBox_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸCrossPkgLib() {
    builtin.initPackage(typeof(CrossPkgLib_package));
}

[GoType] partial struct Box {
    public CrossPkgLibꓸStatus S;
}

public static Box New(nint code) {
    return new Box(S: new CrossPkgLibꓸStatus(Code: code));
}

} // end CrossPkgBox_package
