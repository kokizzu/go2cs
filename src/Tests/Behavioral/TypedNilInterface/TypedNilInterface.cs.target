namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct AErr {
    internal nint n;
}

[GoRecv] public static @string Error(this ref AErr e) {
    return "a"u8;
}

[GoType] partial struct BErr {
    internal nint n;
}

[GoRecv] public static @string Error(this ref BErr e) {
    return "b"u8;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object xNilˢ = (@string)"x==nil"u8;
private static readonly object pTypednilˢ = (@string)"p==typednil"u8;
private static readonly object e1Nilˢ = (@string)"e1==nil"u8;
private static readonly object e2Nilˢ = (@string)"e2==nil"u8;
private static readonly object e1E2ˢ = (@string)"e1==e2"u8;
private static readonly object eaE1ˢ = (@string)"ea==e1"u8;
private static readonly object spNilˢ = (@string)"sp==nil"u8;
private static readonly object spSp2ˢ = (@string)"sp==sp2"u8;
private static readonly object spMpˢ = (@string)"sp==mp"u8;
private static readonly object stpNilˢ = (@string)"stp==nil"u8;
private static readonly object switchAErrˢ = (@string)"switch-AErr"u8;
private static readonly object switchOtherˢ = (@string)"switch-other"u8;

[GoType("dyn")] partial struct main_type {
    internal nint r;
}

internal static void Main() {
    any x = ((ж<nint>)nil);
    fmt.Printf("%T\n"u8, x);
    fmt.Println(xNilˢ, x == default!);
    any y = ((ж<nint>)nil);
    fmt.Println((@string)"x==y"u8, AreEqual(x, y));
    ж<nint> p = default!;
    fmt.Println(pTypednilˢ, p == ((ж<nint>)nil));
    error e1 = new AErrжerror(((ж<AErr>)nil));
    error e2 = new BErrжerror(((ж<BErr>)nil));
    fmt.Println(e1Nilˢ, e1 == default!);
    fmt.Println(e2Nilˢ, e2 == default!);
    fmt.Println(e1E2ˢ, AreEqual(e1, e2));
    any ea = ((ж<AErr>)nil);
    fmt.Println(eaE1ˢ, AreEqual(ea, e1));
    any sp = ((ж<slice<byte>>)nil);
    any sp2 = ((ж<slice<byte>>)nil);
    any mp = ((ж<map<@string, nint>>)nil);
    any stp = ((ж<main_type>)nil);
    fmt.Println(spNilˢ, sp == default!);
    fmt.Println(spSp2ˢ, AreEqual(sp, sp2));
    fmt.Println(spMpˢ, AreEqual(sp, mp));
    fmt.Println(stpNilˢ, stp == default!);
    switch (e1.type()) {
    case ж<AErr> v: {
        fmt.Println(switchAErrˢ, v == nil);
        break;
    }
    default: {
        var v = e1;
        fmt.Println(switchOtherˢ);
        break;
    }}
}

} // end main_package
