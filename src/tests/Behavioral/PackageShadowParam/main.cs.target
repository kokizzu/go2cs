namespace go;

using fmt = fmt_package;
using Δio = io_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸio() {
    builtin.initPackage(typeof(io_package));
}

internal static nint total(nint ioΔ1, Δio.Writer w) {
    nint sum = 0;
    for (nint i = 0; i < ioΔ1; i++) {
        sum += ioΔ1;
    }
    _ = w;
    return sum;
}

internal static void Main() {
    fmt.Println(total(4, default!));
}

} // end main_package
