namespace go;

using fmt = fmt_package;
using @unsafe = unsafe_package;

partial class main_package {

[GoType] partial struct Pt {
    public nint X, Y;
}

[GoType("num:uint32")] partial struct Count;

internal static Pt derefStruct(ж<Pt> Ꮡp) {
    return ~Ꮡp;
}

internal static Count derefNamedNum(ж<Count> Ꮡp) {
    return ~Ꮡp;
}

internal static ж<Pt> viaUnsafe(ж<Pt> Ꮡp) {
    return Ꮡp;
}

internal static ж<Pt> convIdentity(ж<Pt> Ꮡp) {
    return Ꮡp;
}

internal static void assignThrough(ж<Pt> Ꮡp) {
    (Ꮡp).Value = new Pt(7, 8);
}

internal static Pt derefLocal() {
    ref var pt = ref heap<Pt>(out var Ꮡpt);
    pt = new Pt(3, 4);
    var q = Ꮡpt;
    return ~q;
}

internal static ж<Pt> advance(ж<Pt> Ꮡp) {
    ref var p = ref Ꮡp.Value;

    p.X += 100;
    return Ꮡp;
}

internal static void Main() {
    ref var pt = ref heap<Pt>(out var Ꮡpt);
    pt = new Pt(1, 2);
    ref var c = ref heap(new Count(), out var Ꮡc);
    c = 42;
    fmt.Println((@string)"struct:"u8, derefStruct(Ꮡpt));
    fmt.Println((@string)"named num:"u8, derefNamedNum(Ꮡc));
    viaUnsafe(Ꮡpt).Value.Y = 20;
    fmt.Println((@string)"via unsafe writes through:"u8, pt);
    convIdentity(Ꮡpt).Value.X = 10;
    fmt.Println((@string)"conv identity writes through:"u8, pt);
    var got = derefStruct(Ꮡpt);
    got.X = 555;
    fmt.Println((@string)"copy:"u8, got, (@string)"original:"u8, pt);
    assignThrough(Ꮡpt);
    fmt.Println((@string)"assigned through:"u8, pt);
    fmt.Println((@string)"local:"u8, derefLocal());
    fmt.Println((@string)"call survives:"u8, advance(Ꮡpt).Value);
}

} // end main_package
