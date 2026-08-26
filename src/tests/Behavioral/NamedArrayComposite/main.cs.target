namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("[3]rune")] partial struct triple;

[GoType("[]nint")] partial struct nums;

internal static void Main() {
    var t = new triple(new rune[]{7, 32, 9}.array());
    fmt.Println(t[0], t[1], t[2]);
    var u = new triple(new rune[]{0, 305 - 73, 0}.array());
    fmt.Println(u[1]);
    var n = new nums(new nint[]{1, 2, 3, 4}.slice());
    fmt.Println(len(n), n[3]);
    fmt.Println(first(new triple(new rune[]{11, 22, 33}.array())));
}

internal static rune first(triple t) {
    t = t.Clone();

    return t[0];
}

} // end main_package
