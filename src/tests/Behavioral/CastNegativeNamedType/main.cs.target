namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:int8")] partial struct level;

internal static level neg() {
    return ((level)(-1));
}

internal static int64 negWide(nint n) {
    return (int64)(-2) * (int64)n;
}

internal static void Main() {
    var lvl = ((level)(-1));
    fmt.Println((nint)(int8)lvl);
    fmt.Println((nint)(int8)neg());
    fmt.Println(negWide(3));
}

} // end main_package
