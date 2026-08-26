namespace go;

using fmt = fmt_package;
using Δvlib = go.vlib.vlib_package;
using go.vlib;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct wrapper {
    internal ж<Δvlib.Rand> r;
}

internal static void Main() {
    var w = new wrapper(r: Δvlib.New(new Δvlib.PCGжSource(Δvlib.NewPCG(42))));
    for (nint i = 0; i < 3; i++) {
        fmt.Println(w.r.IntN(100));
    }
}

} // end main_package
