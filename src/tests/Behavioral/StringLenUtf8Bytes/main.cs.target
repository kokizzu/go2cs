namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}


[GoType("dyn")] partial struct testsᴛ1 {
    internal @string name;
    internal nint n;
}
internal static slice<testsᴛ1> tests = new testsᴛ1[]{
    new("ascii"u8, len("abc")),
    new("bmp2"u8, len("aΩb")),
    new("bmp3"u8, len("a☺b☻c")),
    new("astral"u8, len("z\U0001d56b"))
}.slice();

internal static void Main() {
    foreach (var (_, t) in tests) {
        fmt.Println(t.name, t.n);
    }
    fmt.Println(len("a☺b☻"), len("héllo"), len("日本語"));
}

} // end main_package
