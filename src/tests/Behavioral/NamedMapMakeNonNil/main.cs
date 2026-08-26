namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("map[@string, nint]")] partial struct StrIntMap;

internal static void Main() {
    var m = new StrIntMap(0);
    fmt.Println(m == default!);
    fmt.Println(len(m));
    m["a"u8] = 1;
    fmt.Println(m["a"u8]);
    var m2 = new StrIntMap(0);
    fmt.Println(m2 == default!);
    StrIntMap z = default!;
    fmt.Println(z == default!);
    fmt.Println(len(z));
    var e = new StrIntMap(new map<@string, nint>{});
    fmt.Println(e == default!);
}

} // end main_package
