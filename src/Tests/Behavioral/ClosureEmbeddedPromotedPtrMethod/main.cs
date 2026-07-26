namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct counter {
    internal nint n;
}

[GoRecv] internal static void bump(this ref counter c) {
    c.n++;
}

internal static nint apply(Func<nint> f) {
    return f();
}

[GoType("dyn")] partial struct run_rep {
    internal partial ref counter counter { get; }
    internal @string label;
}

internal static (@string, nint) run() {
    ref var rep = ref heap(new run_rep(), out var Ꮡrep);
    rep.label = "start"u8;
    var build = () => {
        Ꮡrep.of(run_rep.Ꮡcounter).bump();
        Ꮡrep.Value.label = "built"u8;
        return Ꮡrep.Value.n;
    };
    fmt.Println((@string)"inner:"u8, build(), rep.label);
    nint got = apply(() => {
        var touch = () => {
            Ꮡrep.of(run_rep.Ꮡcounter).bump();
            Ꮡrep.Value.label = "applied"u8;
        };
        touch();
        return Ꮡrep.Value.n;
    });
    fmt.Println((@string)"applied:"u8, got);
    return (rep.label, rep.n);
}

internal static void Main() {
    var (label, n) = run();
    fmt.Println((@string)"outer:"u8, label, n);
}

} // end main_package
