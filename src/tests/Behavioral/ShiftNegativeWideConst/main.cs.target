namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static int64 mask(nint bits) {
    return ((int64)(-1)).Lsh((nuint)bits);
}

internal static uint64 umask(nuint bits) {
    return (~(uint64)0).Lsh(bits);
}

internal static void Main() {
    fmt.Println(mask(4));
    fmt.Println(mask(8));
    fmt.Println(umask(60));
}

} // end main_package
