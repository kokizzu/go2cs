namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:nint")] partial struct Counter;

public static @string String(this Counter c) {
    return fmt.Sprint((nint)c);
}

internal static void Main() {
    Counter c = 5;
    var f = () => c.String();
    fmt.Println(f());
}

} // end main_package
