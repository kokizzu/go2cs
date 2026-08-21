[assembly: go.GoPositionMap("main.go", "main.cs", "ABs6gKSCgoKEgoSC")]

namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct sub {
    internal nint mu, n;
}

[GoType] partial struct item {
    internal sub inner;
}

internal static ж<array<item>> Ꮡpool = new(new array<item>(3));
internal static ref array<item> pool => ref Ꮡpool.Value;


[GoType("dyn")] partial struct gridᴛ1 {
    internal sub cell;
    internal array<byte> pad = new(4);
}
internal static ж<array<gridᴛ1>> Ꮡgrid = new(new array<gridᴛ1>(3, () => new()));
internal static ref array<gridᴛ1> grid => ref Ꮡgrid.Value;

internal static void setInt(ref nint p) {
    p = 7;
}

internal static void Main() {
    pool[1].inner.n = 5;
    setInt(ref pool[1].inner.mu);
    setInt(ref pool[2].inner.mu);
    grid[0].cell.n = 9;
    setInt(ref grid[0].cell.mu);
    fmt.Println(pool[1].inner.mu, pool[1].inner.n, pool[2].inner.mu);
    fmt.Println(grid[0].cell.mu, grid[0].cell.n);
}

} // end main_package
