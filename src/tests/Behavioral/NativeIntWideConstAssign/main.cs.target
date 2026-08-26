namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static UntypedInt maxBits => 57;

internal static (uintptr, nuint, nint) run() {
    uintptr p = default!;
    p = (uintptr)(144115188075855872L - 1);
    nuint u = default!;
    u = (nuint)(144115188075855872L + 5);
    nint n = default!;
    n = unchecked((nint)(144115188075855772L));
    return (p, u, n);
}

internal static void Main() {
    var (p, u, n) = run();
    fmt.Println(p);
    fmt.Println(u);
    fmt.Println(n);
}

} // end main_package
