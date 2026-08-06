namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void describe(any f) {
    _ = f;
    fmt.Println((@string)"ok"u8);
}

[GoType("dyn")] partial struct main_a {
    public nint X;
}

[GoLocalName("point")] [GoType("dyn")] partial struct main_point {
    public nint X, Y;
}

internal static void Main() {
    var a = new main_a(X: 1);
    main_a b = default!;
    b = a;
    fmt.Println(a == b);
    var p = new main_point(X: 1, Y: 2);
    fmt.Println(p.X + p.Y);
    describe(nint (nint x) => 0);
}

} // end main_package
