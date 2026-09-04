namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct S {
    public nint A;
    public @string B;
}

[GoType] partial struct withArray {
    public array<array<nint>> A = new(2, () => new(3));
}

[GoType("[4]byte")] partial struct nb;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object paExplicitˢ = (@string)"paExplicit:"u8;
private static readonly object paPopˢ = (@string)"paPop:"u8;
private static readonly object paShortˢ = (@string)"paShort:"u8;
private static readonly object pnaExplicitˢ = (@string)"pnaExplicit:"u8;
private static readonly object pnestˢ = (@string)"pnest:"u8;
private static readonly object ps2ˢ = (@string)"ps2:"u8;
private static readonly object pslˢ = (@string)"psl:"u8;
private static readonly object nestedˢ = (@string)"nested:"u8;
private static readonly object nestedPopˢ = (@string)"nestedPop:"u8;
private static readonly object nestedShortˢ = (@string)"nestedShort:"u8;
private static readonly object nestedKeyedˢ = (@string)"nestedKeyed:"u8;
private static readonly object structElemˢ = (@string)"structElem:"u8;
private static readonly object arrNestedˢ = (@string)"arrNested:"u8;
private static readonly object mapNestedˢ = (@string)"mapNested:"u8;
private static readonly object sanˢ = (@string)"san:"u8;
private static readonly object ctrlˢ = (@string)"ctrl:"u8;
private static readonly object ctrlLitˢ = (@string)"ctrlLit:"u8;

[GoType("dyn")] internal partial struct main_sa {
    public array<array<nint>> A = new(2, () => new(3));
}

internal static void Main() {
    var pa = new ж<array<byte>>[]{Ꮡ(new byte[]{}.array(4))}.slice();
    fmt.Println((@string)"pa:"u8, len(pa), len(pa[0].Value), pa[0].Value);
    pa[0].Value[1] = 9;
    fmt.Printf("pa written: %v\n"u8, pa[0].Value);
    var paExplicit = new ж<array<byte>>[]{Ꮡ(new byte[]{}.array(4))}.slice();
    fmt.Println(paExplicitˢ, len(paExplicit), len(paExplicit[0].Value), paExplicit[0].Value);
    var paPop = new ж<array<byte>>[]{Ꮡ(new byte[]{1, 2, 3, 4}.array())}.slice();
    fmt.Println(paPopˢ, len(paPop[0].Value), paPop[0].Value);
    var paShort = new ж<array<byte>>[]{Ꮡ(new byte[]{1, 2}.array(4))}.slice();
    fmt.Println(paShortˢ, len(paShort[0].Value), paShort[0].Value);
    var pnaExplicit = new ж<nb>[]{Ꮡ(new nb(new byte[4].array()))}.slice();
    fmt.Println(pnaExplicitˢ, len(pnaExplicit[0].Value), pnaExplicit[0].Value);
    var pnest = new ж<array<array<nint>>>[]{Ꮡ(new array<nint>[]{}.array(2, () => new(3)))}.slice();
    fmt.Println(pnestˢ, len(pnest[0].Value), len(pnest[0].Value[0]), pnest[0].Value);
    var ps = new ж<S>[]{Ꮡ(new S())}.slice();
    fmt.Println((@string)"ps:"u8, len(ps), ps[0].Value);
    var ps2 = new ж<S>[]{Ꮡ(new S(A: 7, B: "x"u8))}.slice();
    fmt.Println(ps2ˢ, ps2[0].Value);
    var psl = new ж<slice<nint>>[]{Ꮡ(new nint[]{}.slice())}.slice();
    fmt.Println(pslˢ, len(psl), len(psl[0].ValueSlot), psl[0].ValueSlot == default!, psl[0].ValueSlot);
    var pm = new ж<map<@string, nint>>[]{Ꮡ(new map<@string, nint>{})}.slice();
    fmt.Println((@string)"pm:"u8, len(pm), len(pm[0].ValueSlot), pm[0].ValueSlot == default!, pm[0].ValueSlot);
    var mp = new map<@string, ж<array<nint>>>{["a"u8] = Ꮡ(new nint[]{}.array(2))};
    fmt.Println((@string)"mp:"u8, len(mp), len(mp["a"u8].Value), mp["a"u8].Value);
    var ap = new ж<array<nint>>[]{Ꮡ(new nint[]{}.array(3)), Ꮡ(new nint[]{}.array(3))}.array();
    fmt.Println((@string)"ap:"u8, len(ap), len(ap[0].Value), ap[0].Value, ap[1].Value);
    var nested = GoReflect.WithElemDims(new array<array<nint>>[]{new array<nint>[]{}.array(2, () => new(3))}.slice(), 2, 3);
    fmt.Println(nestedˢ, len(nested), len(nested[0]), len(nested[0][0]), nested[0]);
    nested[0][1][2] = 9;
    fmt.Printf("nested written: %v\n"u8, nested[0]);
    var nestedPop = GoReflect.WithElemDims(new array<array<nint>>[]{new array<nint>[]{new nint[]{1, 2, 3}.array(), new nint[]{4, 5, 6}.array()}.array()}.slice(), 2, 3);
    fmt.Println(nestedPopˢ, len(nestedPop[0]), len(nestedPop[0][0]), nestedPop[0]);
    var nestedShort = GoReflect.WithElemDims(new array<array<nint>>[]{new array<nint>[]{new nint[]{1, 2, 3}.array()}.array(2, () => new(3))}.slice(), 2, 3);
    fmt.Println(nestedShortˢ, len(nestedShort[0]), len(nestedShort[0][1]), nestedShort[0]);
    var nestedKeyed = GoReflect.WithElemDims(new array<array<nint>>[]{new golib.SparseArray<array<nint>>{[1] = new nint[]{7, 8, 9}.array()}.array(2, () => new(3))}.slice(), 2, 3);
    fmt.Println(nestedKeyedˢ, len(nestedKeyed[0]), len(nestedKeyed[0][0]), nestedKeyed[0]);
    var structElem = GoReflect.WithElemDims(new array<withArray>[]{new withArray[]{}.array(2, () => new())}.slice(), 2);
    fmt.Println(structElemˢ, len(structElem[0]), len(structElem[0][1].A), len(structElem[0][1].A[0]), structElem[0]);
    var arrNested = new array<array<nint>>[]{new array<nint>[]{}.array(2, () => new(3))}.array(2, () => new(2, () => new(3)));
    fmt.Println(arrNestedˢ, len(arrNested), len(arrNested[0]), len(arrNested[0][0]), len(arrNested[1][0]), arrNested);
    var mapNested = new map<@string, array<array<nint>>>{["k"u8] = new array<nint>[]{}.array(2, () => new(3))};
    fmt.Println(mapNestedˢ, len(mapNested["k"u8, () => new array<array<nint>>(2, () => new(3))]), len(mapNested["k"u8, () => new array<array<nint>>(2, () => new(3))][0]), mapNested["k"u8, () => new array<array<nint>>(2, () => new(3))]);
    var sa = new main_sa[]{new()}.slice();
    fmt.Println((@string)"sa:"u8, len(sa), len(sa[0].A), len(sa[0].A[0]), sa[0]);
    var san = new withArray[]{new()}.slice();
    fmt.Println(sanˢ, len(san[0].A), len(san[0].A[0]), san[0]);
    array<array<array<nint>>> ctrl = new(3, () => new(2, () => new(2)));
    fmt.Println(ctrlˢ, len(ctrl), len(ctrl[0]), len(ctrl[0][0]), ctrl);
    var ctrlLit = new array<array<nint>>[]{}.array(3, () => new(2, () => new(2)));
    fmt.Println(ctrlLitˢ, len(ctrlLit), len(ctrlLit[0]), len(ctrlLit[0][0]), ctrlLit);
}

} // end main_package
