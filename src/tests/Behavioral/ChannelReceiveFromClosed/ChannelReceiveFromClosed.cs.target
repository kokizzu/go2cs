namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    var c = new channel<nint>(3);
    c.ᐸꟷ(1);
    c.ᐸꟷ(2);
    c.ᐸꟷ(3);
    close(c);
    for (nint i = 0; i < 4; i++) {
        fmt.Printf("%d "u8, ᐸꟷ(c));
    }
}

} // end main_package
