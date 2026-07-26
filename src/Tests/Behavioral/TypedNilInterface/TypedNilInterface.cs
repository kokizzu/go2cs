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

[GoType("dyn")] partial struct main_type {
    internal nint r;
}

internal static void Main() {
    any x = ((ж<nint>)nil);
    fmt.Printf("%T\n"u8, x);
    fmt.Println((@string)"x==nil"u8, x == default!);
    any y = ((ж<nint>)nil);
    fmt.Println((@string)"x==y"u8, AreEqual(x, y));
    ж<nint> p = default!;
    fmt.Println((@string)"p==typednil"u8, p == ((ж<nint>)nil));
    error e1 = new AErrжerror(((ж<AErr>)nil));
    error e2 = new BErrжerror(((ж<BErr>)nil));
    fmt.Println((@string)"e1==nil"u8, e1 == default!);
    fmt.Println((@string)"e2==nil"u8, e2 == default!);
    fmt.Println((@string)"e1==e2"u8, AreEqual(e1, e2));
    any ea = ((ж<AErr>)nil);
    fmt.Println((@string)"ea==e1"u8, AreEqual(ea, e1));
    any sp = ((ж<slice<byte>>)nil);
    any sp2 = ((ж<slice<byte>>)nil);
    any mp = ((ж<map<@string, nint>>)nil);
    any stp = ((ж<main_type>)nil);
    fmt.Println((@string)"sp==nil"u8, sp == default!);
    fmt.Println((@string)"sp==sp2"u8, AreEqual(sp, sp2));
    fmt.Println((@string)"sp==mp"u8, AreEqual(sp, mp));
    fmt.Println((@string)"stp==nil"u8, stp == default!);
    switch (e1.type()) {
    case ж<AErr> v: {
        fmt.Println((@string)"switch-AErr"u8, v == nil);
        break;
    }
    default: {
        var v = e1;
        fmt.Println((@string)"switch-other"u8);
        break;
    }}
}

} // end main_package
