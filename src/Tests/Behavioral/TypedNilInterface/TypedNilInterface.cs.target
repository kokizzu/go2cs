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
    fmt.Println((@string)"x==nil", x == default!);
    any y = ((ж<nint>)nil);
    fmt.Println((@string)"x==y", AreEqual(x, y));
    ж<nint> p = default!;
    fmt.Println((@string)"p==typednil", p == ((ж<nint>)nil));
    error e1 = new AErrжerror(((ж<AErr>)nil));
    error e2 = new BErrжerror(((ж<BErr>)nil));
    fmt.Println((@string)"e1==nil", e1 == default!);
    fmt.Println((@string)"e2==nil", e2 == default!);
    fmt.Println((@string)"e1==e2", AreEqual(e1, e2));
    any ea = ((ж<AErr>)nil);
    fmt.Println((@string)"ea==e1", AreEqual(ea, e1));
    any sp = ((ж<slice<byte>>)nil);
    any sp2 = ((ж<slice<byte>>)nil);
    any mp = ((ж<map<@string, nint>>)nil);
    any stp = ((ж<main_type>)nil);
    fmt.Println((@string)"sp==nil", sp == default!);
    fmt.Println((@string)"sp==sp2", AreEqual(sp, sp2));
    fmt.Println((@string)"sp==mp", AreEqual(sp, mp));
    fmt.Println((@string)"stp==nil", stp == default!);
    switch (e1.type()) {
    case ж<AErr> v: {
        fmt.Println((@string)"switch-AErr", v == nil);
        break;
    }
    default: {
        var v = e1;
        fmt.Println((@string)"switch-other");
        break;
    }}
}

} // end main_package
