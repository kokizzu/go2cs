namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static int64 f(int64 ns) {
    var total = ns;
    if (ns < 0) {
        UntypedInt nsΔ1 = 10;
        total += (int64)nsΔ1 + (int64)nsΔ1;
    }
    return total + ns;
}

internal static void Main() {
    fmt.Println(f(5));
    fmt.Println(f(-3));
}

} // end main_package
