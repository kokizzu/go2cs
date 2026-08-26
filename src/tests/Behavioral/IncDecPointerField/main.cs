namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct inner {
    internal nint k;
}

[GoType] partial struct counter {
    internal nint n;
    internal inner sub;
}

internal static ж<counter> get(ж<counter> Ꮡc) {
    return Ꮡc;
}

internal static void Main() {
    var @base = Ꮡ(new counter(n: 5));
    @base.Value.sub.k = 3;
    var c = get(@base);
    c.Value.n++;
    c.Value.n++;
    c.Value.sub.k--;
    fmt.Println((~@base).n, (~@base).sub.k);
}

} // end main_package
