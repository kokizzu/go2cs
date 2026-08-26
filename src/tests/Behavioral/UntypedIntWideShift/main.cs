namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static UntypedInt bias => 1023;

internal static UntypedInt bits => 0x7FF8000000000000;

internal static void Main() {
    var s = new nint[]{52, 40, 33}.slice();
    fmt.Println((uint64)(((bias - 1) << (int)(s[0]))));
    fmt.Println((uint64)(((bias - 1) << (int)(s[1]))));
    fmt.Println((uint64)(((bits - 1) >> (int)(s[0]))));
    fmt.Println((uint64)(((bits - 1) >> (int)(s[2]))));
}

} // end main_package
