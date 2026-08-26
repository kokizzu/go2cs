namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:nuint")] partial struct Flags;

[GoType("num:uintptr")] partial struct Mask;

internal static void Main() {
    Flags a = 6;
    Flags b = 2;
    fmt.Println(a + b);
    fmt.Println(a - b);
    fmt.Println(a * b);
    fmt.Println(a / b);
    fmt.Println(((Flags)0 - a) + a);
    Mask m = 5;
    fmt.Println((Mask)(m | 2));
}

} // end main_package
