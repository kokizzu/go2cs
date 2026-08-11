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
}

} // end main_package
