namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}


    [GoType("dyn")] partial struct Δtype {
        internal nint bx;
    }
internal static ж<Δtype> bReserved = ((ж<Δtype>)nil);

[GoType] partial struct Bee {
}

[GoType("dyn")] internal partial struct probe_inner {
    internal nint n;
}

internal static @string probe(this Bee _) {
    var v = new probe_inner(n: 7);
    return fmt.Sprintf("bee inner n=%d reserved=%v"u8, v.n, bReserved == nil);
}

} // end main_package
