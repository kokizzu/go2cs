namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void describe(any f) {
    _ = f;
    fmt.Println((@string)"ok"u8);
}

[GoType("dyn")] partial struct main_a {
    public nint X;
}

[GoType("dyn")] partial struct main_point {
    public nint X, Y;
}

internal static void Main() {
    var a = new main_a(X: 1);
    main_a b = default!;
    b = a;
    fmt.Println(a == b);
    ref var p = ref heap<main_point>(out var Ꮡp);
    p = new main_point(X: 1, Y: 2);
    fmt.Println(p.X + p.Y);
    fmt.Printf("%T %T\n"u8, p, Ꮡp);
    describe(nint (nint x) => 0);
    embeddedLocalTypes();
}

[GoType("num:nint")] partial struct embeddedLocalTypes_myInt;

[GoType("num:nint")] partial struct embeddedLocalTypes_MyInt;

[GoType("dyn")] partial struct embeddedLocalTypes_embed {
    public nint Q;
}

[GoType("dyn")] partial struct embeddedLocalTypes_holder {
    internal partial ref embeddedLocalTypes_myInt myInt { get; }
    public partial ref embeddedLocalTypes_MyInt MyInt { get; }
    internal partial ref embeddedLocalTypes_embed embed { get; }
}

[GoType("dyn")] partial struct embeddedLocalTypes_ptrHolder {
    internal partial ref ж<embeddedLocalTypes_myInt> myInt { get; }
    internal partial ref ж<embeddedLocalTypes_embed> embed { get; }
}

internal static void embeddedLocalTypes() {
    var h = new embeddedLocalTypes_holder(1, 2, new embeddedLocalTypes_embed(Q: 3));
    fmt.Println(h.myInt, h.MyInt, h.embed.Q);
    fmt.Println(h.Q);
    var k = new embeddedLocalTypes_holder(myInt: 4, MyInt: 5, embed: new embeddedLocalTypes_embed(Q: 6));
    fmt.Println(k.myInt, k.MyInt, k.Q);
    k.myInt = 7;
    fmt.Println(k.myInt);
    ref var i = ref heap<embeddedLocalTypes_myInt>(out var Ꮡi);
    i = ((embeddedLocalTypes_myInt)8);
    ref var e = ref heap<embeddedLocalTypes_embed>(out var Ꮡe);
    e = new embeddedLocalTypes_embed(Q: 9);
    var pp = new embeddedLocalTypes_ptrHolder(myInt: Ꮡi, embed: Ꮡe);
    pp.myInt.Value = 10;
    pp.embed.Value.Q = 11;
    fmt.Println(pp.myInt.Value, (~pp.embed).Q, pp.Q);
    fmt.Printf("%T\n"u8, h.embed);
}

} // end main_package
