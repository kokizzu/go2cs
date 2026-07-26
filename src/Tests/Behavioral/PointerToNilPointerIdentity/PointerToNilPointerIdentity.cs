namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object pp1Pp2ˢ = (@string)"pp1==pp2"u8;
private static readonly object pp1Nilˢ = (@string)"pp1==nil"u8;
private static readonly object pp1Nilˢ2 = (@string)"*pp1==nil"u8;
private static readonly object pp1Pp2ˢ2 = (@string)"*pp1==*pp2"u8;
private static readonly object aliasPp1ˢ = (@string)"alias==pp1"u8;
private static readonly object p1Setˢ = (@string)"p1 set"u8;

internal static void Main() {
    ref var p1 = ref heap<ж<nint>>(out var Ꮡp1);
    p1 = ((ж<nint>)nil);
    ref var p2 = ref heap<ж<nint>>(out var Ꮡp2);
    p2 = ((ж<nint>)nil);
    var pp1 = Ꮡp1;
    var pp2 = Ꮡp2;
    fmt.Println(pp1Pp2ˢ, pp1 == pp2);
    fmt.Println(pp1Nilˢ, pp1 == nil);
    fmt.Println(pp1Nilˢ2, pp1.ValueSlot == nil);
    fmt.Println(pp1Pp2ˢ2, pp1.ValueSlot == pp2.ValueSlot);
    var alias = pp1;
    fmt.Println(aliasPp1ˢ, alias == pp1);
    ref var n = ref heap<nint>(out var Ꮡn);
    n = 42;
    alias.ValueSlot = Ꮡn;
    fmt.Println(p1Setˢ, pp1.ValueSlot != nil, (pp1.ValueSlot).Value);
}

} // end main_package
