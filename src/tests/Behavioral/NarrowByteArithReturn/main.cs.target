namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static byte lower(byte c) {
    if ((rune)'A' <= c && c <= (rune)'Z') {
        return (byte)(c + ((rune)'a' - (rune)'A'));
    }
    return c;
}

internal static byte wrapRet(byte x) {
    return (byte)(x + x + 1);
}

internal static void Main() {
    fmt.Println(lower((rune)'A'), lower((rune)'Z'), lower((rune)'a'));
    fmt.Println(wrapRet(200));
}

} // end main_package
