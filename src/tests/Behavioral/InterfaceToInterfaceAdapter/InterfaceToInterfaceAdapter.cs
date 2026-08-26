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

[GoType] partial interface localLabel {
    @string Label();
}

internal static @string labelOf(localLabel l) {
    return l.Label();
}

internal static void Main() {
    CrossPkgLib.Labeled foreign = new CrossPkgLib.Sensor(Name: "adapter"u8, Temp: 21D);
    localLabel local = new CrossPkgLib_LabeledᴠlocalLabel(foreign);
    fmt.Println(labelOf(new CrossPkgLib_LabeledᴠlocalLabel(foreign)));
    fmt.Println(local.Label());
    fmt.Println(CrossPkgLib.Describe(foreign));
}

} // end main_package
