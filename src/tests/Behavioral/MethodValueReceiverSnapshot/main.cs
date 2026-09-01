namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct frame {
    public @string Name;
    public bool Inlined;
}

internal static @string label(this frame f) {
    return f.Name;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object typedˢ = (@string)"typed   "u8;
private static readonly object boxedˢ = (@string)"boxed   "u8;
private static readonly object distinctˢ = (@string)"distinct"u8;
private static readonly object argumentˢ = (@string)"argument"u8;
private static readonly object assignˢ = (@string)"assign  "u8;

internal static void Main() {
    ref var a = ref heap<frame>(out var Ꮡa);
    a = new frame(Name: "a"u8);
        var aʗ1 = a;
    var typed = new Func<@string>[]{
        () => aʗ1.label(),
        () => Ꮡa.Value.Name
    }.slice();
    a.Name = "A"u8;
    fmt.Println(typedˢ, typed[0](), typed[1]());
    ref var b = ref heap<frame>(out var Ꮡb);
    b = new frame(Name: "b"u8, Inlined: true);
        var bʗ1 = b;
    var boxed = new any[]{
        () => bʗ1.label(),
        () => Ꮡb.Value.Inlined
    }.slice();
    b.Name = "B"u8;
    fmt.Println(boxedˢ, boxed[0]._<Func<@string>>()(), boxed[1]._<Func<bool>>()());
    var c = new frame(Name: "p"u8);
    
    var cʗ1 = c;
    var first = () => cʗ1.label();
    c.Name = "q"u8;
    
    var cʗ2 = c;
    var second = () => cʗ2.label();
    c.Name = "r"u8;
    fmt.Println(distinctˢ, first(), second(), c.label());
    var d = new frame(Name: "d"u8);
    @string call(Func<@string> f) => f();
    var dʗ1 = d;
    @string got = call(() => dʗ1.label());
    d.Name = "D"u8;
    var dʗ2 = d;
    fmt.Println(argumentˢ, got, call(() => dʗ2.label()));
    ref var e = ref heap<frame>(out var Ꮡe);
    e = new frame(Name: "e"u8);
    @string watch() => Ꮡe.Value.Name;
    var eʗ1 = e;
    
    var bound = () => eʗ1.label();
    e.Name = "E"u8;
    fmt.Println(assignˢ, bound(), watch());
}

} // end main_package
