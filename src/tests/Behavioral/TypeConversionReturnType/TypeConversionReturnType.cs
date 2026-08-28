global using P = go.ж<bool>;
global using M = go.map<nint, nint>;

namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("dyn")] internal partial struct test_R0 {
    internal @string @string;
    internal ж<nint> @int;
    public P P;
    public M M;
}

internal static test_R0 test() {
    test_R0 x = default!;
    x.@string = "Go"u8;
    x.@int = @new<nint>();
    x.P = @new<bool>();
    x.M = new M();
    return x;
}

internal static void Main() {
    var x = test();
    fmt.Println(x);
}

} // end main_package
