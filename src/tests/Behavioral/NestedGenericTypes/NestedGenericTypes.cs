namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct Box<T> {
    internal T v;
}

[GoType] partial struct Pair<A, B> {
    internal A a;
    internal B b;
}

internal static void Main() {
    Box<Box<nint>> nested = default!;
    nested.v.v = 42;
    fmt.Println(nested.v.v);
    Pair<Box<nint>, Box<nint>> p = default!;
    p.a.v = 7;
    p.b.v = 8;
    fmt.Println(p.a.v, p.b.v);
    Box<Box<Box<nint>>> deep = default!;
    deep.v.v.v = 99;
    fmt.Println(deep.v.v.v);
}

} // end main_package
