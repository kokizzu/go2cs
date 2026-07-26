namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct Counter {
    internal int32 n;
}

internal static int32 bump(ж<int32> Ꮡp, int32 delta) {
    ref var p = ref Ꮡp.Value;

    p += delta;
    return p;
}

public static int32 Add(this ж<Counter> Ꮡc, int32 delta) {
    return bump(Ꮡc.of(Counter.Ꮡn), delta);
}

public static void Set(this ж<Counter> Ꮡc, int32 v) {
    (Ꮡc.of(Counter.Ꮡn)).Value = v;
}

[GoRecv] public static int32 Get(this ref Counter c) {
    return c.n;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object afterSetˢ = (@string)"after Set:"u8;
private static readonly object add10ˢ = (@string)"Add 10:"u8;
private static readonly object add5ˢ = (@string)"Add 5:"u8;
private static readonly object finalˢ = (@string)"final:"u8;

internal static void Main() {
    ref var c = ref heap(new Counter(), out var Ꮡc);
    Ꮡc.Set(100);
    fmt.Println(afterSetˢ, c.Get());
    fmt.Println(add10ˢ, Ꮡc.Add(10));
    fmt.Println(add5ˢ, Ꮡc.Add(5));
    fmt.Println(finalˢ, c.Get());
}

} // end main_package
