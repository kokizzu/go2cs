namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static nint accumulate(ж<nint> Ꮡcounter, slice<nint> vals) {
    ref var counter = ref Ꮡcounter.DerefOrNull();

    void add(nint v) {
        if (Ꮡcounter == nil) {
            Ꮡcounter = @new<nint>();
        }
        Ꮡcounter.Value += v;
    }
    foreach (var (_, v) in vals) {
        add(v);
    }
    return counter;
}

internal static void Main() {
    ref var n = ref heap<nint>(out var Ꮡn);
    n = 0;
    fmt.Println(accumulate(Ꮡn, new nint[]{1, 2, 3, 4}.slice()));
    fmt.Println(n);
}

} // end main_package
