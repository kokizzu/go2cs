namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:nuint")] partial struct arenaIdx;

[GoType("num:uint8")] partial struct tag;

[GoType("num:uint64")] partial struct big;

internal static UntypedInt bits => 6;

internal static void Main() {
    arenaIdx a = (arenaIdx)((nuint)1 << (int)(bits));
    tag t = (tag)(uint8)(1 << (int)(3));
    big b = (big)((uint64)1 << (int)(40));
    fmt.Println((nuint)a, (uint8)t, (uint64)b);
}

} // end main_package
