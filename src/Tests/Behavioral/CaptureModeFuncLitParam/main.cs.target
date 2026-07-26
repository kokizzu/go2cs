namespace go;

using fmt = fmt_package;
using ꓸꓸꓸnint = Span<nint>;

partial class main_package {

[GoType] partial struct Tally {
    internal nint total;
    internal @string log;
}

public static void Add(this ж<Tally> Ꮡt, nint n) => func((defer, recover) => {
    ref var t = ref Ꮡt.Value;

    defer(() => {
        Ꮡt.Value.log = fmt.Sprintf("%s+%d"u8, Ꮡt.Value.log, n);
    });
    t.total += n;
});

internal static void runDeferred(Tally @base) => func((defer, recover) => {
    deferǃ((Tally tʗp) => {
        ref var t = ref heap(tʗp, out var Ꮡt);
        Ꮡt.Add(4);
        fmt.Println((@string)"deferred:"u8, t.total, t.log);
    }, @base, defer);
});

internal static void Main() {
    var f = (Tally tʗp, nint m) => {
        ref var t = ref heap(tʗp, out var Ꮡt);
        t.total++;
        Ꮡt.Add(m);
        return (t.total, t.log);
    };
    var seed = new Tally(total: 5, log: "s"u8);
    var (total, log) = f(seed, 3);
    fmt.Println((@string)"assigned:"u8, total, log);
    fmt.Println((@string)"caller copy untouched:"u8, seed.total, seed.log);
    var (total2, log2) = ((Func<Tally, (nint, @string)>)(tʗp => {
        ref var t = ref heap(tʗp, out var Ꮡt);
        Ꮡt.Add(7);
        return (t.total, t.log);
    }))(new Tally(total: 1, log: "i"u8));
    fmt.Println((@string)"iife:"u8, total2, log2);
    var @base = new Tally(total: 2, log: "d"u8);
    runDeferred(@base);
    fmt.Println((@string)"deferred source untouched:"u8, @base.total, @base.log);
    var g = (Tally tʗp) => {
        ref var t = ref heap(tʗp, out var Ꮡt);
        var bump = () => {
            Ꮡt.Add(9);
        };
        bump();
        t.total++;
        return (t.total, t.log);
    };
    var (total4, log4) = g(new Tally(total: 3, log: "n"u8));
    fmt.Println((@string)"nested:"u8, total4, log4);
    var h = (Tally tʗp, params ꓸꓸꓸnint nsʗp) => {
        var ns = nsʗp.sslice();
        ref var t = ref heap(tʗp, out var Ꮡt);
        foreach (var (_, n) in ns) {
            Ꮡt.Add(n);
        }
        return (t.total, t.log);
    };
    var (total5, log5) = h(new Tally(total: 10, log: "v"u8), 1, 2);
    fmt.Println((@string)"variadic:"u8, total5, log5);
}

} // end main_package
