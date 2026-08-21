namespace go;

using fmt = fmt_package;
using ꓸꓸꓸShape = Span<main_package.Shape>;
using ꓸꓸꓸany = Span<any>;

partial class main_package {

[GoType] partial interface Shape {
    nint Area();
}

[GoType] partial struct Rect {
    internal nint w, h;
}

[GoRecv] public static nint Area(this ref Rect r) {
    return r.w * r.h;
}

[GoType] partial struct Circle {
    internal nint r;
}

[GoRecv] public static nint Area(this ref Circle c) {
    return 3 * c.r * c.r;
}

internal static ж<Rect> newRect(nint w, nint h) {
    return Ꮡ(new Rect(w: w, h: h));
}

internal static nint totalArea(nint scale, params ꓸꓸꓸShape shapesʗp) {
    var shapes = shapesʗp.sslice();

    nint sum = 0;
    foreach (var (_, s) in shapes) {
        sum += s.Area();
    }
    return sum * scale;
}

internal static (nint, bool) countArgs(params ꓸꓸꓸany argsʗp) {
    var args = argsʗp.sslice();

    var first = false;
    if (len(args) > 0) {
        first = args[0] == default!;
    }
    return (len(args), first);
}

internal static nint describe(params ꓸꓸꓸShape shapesʗp) {
    var shapes = shapesʗp.sslice();

    return len(shapes);
}

internal static void Main() {
    fmt.Println(totalArea(2, new RectжShape(newRect(3, 4)), new CircleжShape(Ꮡ(new Circle(r: 2))), new RectжShape(newRect(1, 5))));
    var r = Ꮡ(new Rect(w: 4, h: 2));
    Shape s = new CircleжShape(Ꮡ(new Circle(r: 3)));
    fmt.Println(totalArea(1, s, new RectжShape(r)));
    fmt.Println(totalArea(3));
    var shapes = new Shape[]{new RectжShape(Ꮡ(new Rect(w: 2, h: 2))), new CircleжShape(Ꮡ(new Circle(r: 1)))}.slice();
    fmt.Println(totalArea(1, shapes.ꓸꓸꓸ));
    var (ᴛ1, ᴛ2) = countArgs((any)(default!));
    fmt.Println(ᴛ1, ᴛ2);
    var (ᴛ3, ᴛ4) = countArgs((any)(default!), (nint)(1));
    fmt.Println(ᴛ3, ᴛ4);
    var (ᴛ5, ᴛ6) = countArgs((nint)(1), (any)(default!));
    fmt.Println(ᴛ5, ᴛ6);
    var (ᴛ7, ᴛ8) = countArgs((any)(default!), (any)(default!), (any)(default!));
    fmt.Println(ᴛ7, ᴛ8);
    var (ᴛ9, ᴛ10) = countArgs();
    fmt.Println(ᴛ9, ᴛ10);
    var (ᴛ11, ᴛ12) = countArgs((nint)(1), (nint)(2));
    fmt.Println(ᴛ11, ᴛ12);
    fmt.Println(describe((Shape)(default!)));
    fmt.Println(describe((Shape)(default!), (Shape)(default!)));
    slice<Shape> none = default!;
    fmt.Println(describe(none.ꓸꓸꓸ));
}

} // end main_package
