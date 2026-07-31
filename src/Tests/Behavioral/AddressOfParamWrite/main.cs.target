namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct Rect {
    public nint Min, Max;
}

[GoType] partial struct Box {
    public Rect R;
    public nint Tag;
}

internal static void clip(ж<Rect> Ꮡr, nint lo, nint hi) {
    ref var r = ref Ꮡr.Value;

    if (r.Min < lo) {
        r.Min = lo;
    }
    if (r.Max > hi) {
        r.Max = hi;
    }
}

internal static void bump(ж<nint> Ꮡp) {
    ref var p = ref Ꮡp.Value;

    p += 10;
}

internal static Rect clipParam(Rect rʗp) {
    ref var r = ref heap(rʗp, out var Ꮡr);

    clip(Ꮡr, 5, 5);
    return r;
}

internal static Box bumpParamField(Box bʗp) {
    ref var b = ref heap(bʗp, out var Ꮡb);

    bump(Ꮡb.of(Box.ᏑR).of(Rect.ᏑMin));
    return b;
}

internal static array<nint> bumpParamElem(array<nint> aʗp) {
    ref var a = ref heap(aʗp.Clone(), out var Ꮡa);

    bump(Ꮡa.at<nint>(1));
    return a.Clone();
}

internal static (nint, nint) readOnlyParam(Rect rʗp) {
    ref var r = ref heap(rʗp, out var Ꮡr);

    var p = Ꮡr;
    return ((~p).Min, (~p).Max);
}

internal static Rect clipLocal() {
    ref var r = ref heap<Rect>(out var Ꮡr);
    r = new Rect(0, 16);
    clip(Ꮡr, 5, 5);
    return r;
}

internal static nint plainParam(Rect r) {
    return r.Max - r.Min;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object paramˢ = (@string)"param       :"u8;
private static readonly object paramFieldˢ = (@string)"param field :"u8;
private static readonly object paramElemˢ = (@string)"param elem  :"u8;
private static readonly object paramRoˢ = (@string)"param ro    :"u8;
private static readonly object localˢ = (@string)"local       :"u8;
private static readonly object plainˢ = (@string)"plain       :"u8;
private static readonly object callerˢ = (@string)"caller      :"u8;

internal static void Main() {
    var p = clipParam(new Rect(0, 16));
    fmt.Println(paramˢ, p.Min, p.Max);
    var b = bumpParamField(new Box(new Rect(1, 2), 3));
    fmt.Println(paramFieldˢ, b.R.Min, b.R.Max, b.Tag);
    var a = bumpParamElem(new nint[]{7, 8, 9}.array());
    fmt.Println(paramElemˢ, a[0], a[1], a[2]);
    var (lo, hi) = readOnlyParam(new Rect(3, 4));
    fmt.Println(paramRoˢ, lo, hi);
    var l = clipLocal();
    fmt.Println(localˢ, l.Min, l.Max);
    fmt.Println(plainˢ, plainParam(new Rect(2, 20)));
    var orig = new Rect(0, 16);
    var clipped = clipParam(orig);
    fmt.Println(callerˢ, orig.Min, orig.Max, (@string)"->"u8, clipped.Min, clipped.Max);
}

} // end main_package
