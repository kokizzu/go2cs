namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct counter {
    internal nint n;
}

[GoRecv] internal static void bump(this ref counter c) {
    c.n++;
}

internal static nint apply(Func<nint> f) {
    return f();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string startˢ = "start"u8;
private static readonly @string builtˢ = "built"u8;
private static readonly object innerˢ = (@string)"inner:"u8;
private static readonly @string appliedˢ = "applied"u8;
private static readonly object appliedˢ2 = (@string)"applied:"u8;

[GoType("dyn")] partial struct run_rep {
    internal partial ref counter counter { get; }
    internal @string label;
}

internal static (@string, nint) run() {
    ref var rep = ref heap(new run_rep(), out var Ꮡrep);
    rep.label = startˢ;
    nint build() {
        Ꮡrep.of(run_rep.Ꮡcounter).bump();
        Ꮡrep.Value.label = builtˢ;
        return Ꮡrep.Value.n;
    }
    fmt.Println(innerˢ, build(), rep.label);
    nint got = apply(() => {
        void touch() {
            Ꮡrep.of(run_rep.Ꮡcounter).bump();
            Ꮡrep.Value.label = appliedˢ;
        }
        touch();
        return Ꮡrep.Value.n;
    });
    fmt.Println(appliedˢ2, got);
    return (rep.label, rep.n);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object outerˢ = (@string)"outer:"u8;

internal static void Main() {
    var (label, n) = run();
    fmt.Println(outerˢ, label, n);
}

} // end main_package
