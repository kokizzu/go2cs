namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct counter {
    internal nint n;
}

[GoRecv] internal static void reset(this ref counter c) {
    c.n = 0;
}

[GoRecv] internal static void inc(this ref counter c) {
    c.n++;
}

[GoType] partial struct builder {
    internal counter c;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object insideˢ = (@string)"inside:"u8;

internal static void run(this ж<builder> Ꮡb) => func((defer, recover) => {
    ref var b = ref Ꮡb.DerefOrNull();

    defer(Ꮡb.of(builder.Ꮡc).reset);
    b.c.inc();
    b.c.inc();
    fmt.Println(insideˢ, b.c.n);
});

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object afterˢ = (@string)"after:"u8;
private static readonly object inside2ˢ = (@string)"inside2:"u8;
private static readonly object after2ˢ = (@string)"after2:"u8;

internal static void Main() {
    var b = Ꮡ(new builder(nil));
    b.run();
    fmt.Println(afterˢ, (~b).c.n);
    var x = Ꮡ(new builder(nil));
    var xʗ1 = x;
    ((Action)(() => func((defer, recover) => {
        var xʗ2 = xʗ1;
        defer(xʗ2.of(builder.Ꮡc).reset);
        xʗ1.of(builder.Ꮡc).inc();
        fmt.Println(inside2ˢ, (~xʗ1).c.n);
    })))();
    fmt.Println(after2ˢ, (~x).c.n);
}

} // end main_package
