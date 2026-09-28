namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] public partial struct counts {
    internal array<nint> vals = new(4);
    internal nint n;
}

[GoType("counts")] partial struct Counts;

[GoType] public partial struct grid {
    internal array<array<nint>> cells = new(2, () => new(3));
}

[GoType("grid")] partial struct Grid;

[GoType] partial struct pt {
    public nint X, Y;
}

[GoType] public partial struct path {
    internal array<pt> pts = new(3);
}

[GoType("path")] partial struct Path;

internal static Counts global = new();

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object zeroˢ = (@string)"zero:"u8;
private static readonly object newˢ = (@string)"new:"u8;
private static readonly object literalˢ = (@string)"literal:"u8;
private static readonly object copyˢ = (@string)"copy:"u8;
private static readonly object globalˢ = (@string)"global:"u8;
private static readonly object nestedˢ = (@string)"nested:"u8;
private static readonly object structsˢ = (@string)"structs:"u8;

internal static void Main() {
    Counts c = new();
    c.vals[1] = 5;
    fmt.Println(zeroˢ, len(c.vals), c.vals, c.n);
    var p = @new<Counts>();
    p.Value.vals[3] = 7;
    fmt.Println(newˢ, len((~p).vals), (~p).vals);
    var l = new Counts(new counts(n: 1));
    l.vals[2] = 9;
    fmt.Println(literalˢ, len(l.vals), l.vals, l.n);
    var d = c.ΔClone();
    d.vals[0] = 11;
    fmt.Println(copyˢ, c.vals, d.vals);
    global.vals[0] = 3;
    fmt.Println(globalˢ, len(global.vals), global.vals);
    Grid g = new();
    g.cells[1][2] = 6;
    fmt.Println(nestedˢ, len(g.cells), len(g.cells[1]), g.cells);
    Path pa = new();
    pa.pts[2].Y = 8;
    fmt.Println(structsˢ, len(pa.pts), pa.pts);
}

} // end main_package
