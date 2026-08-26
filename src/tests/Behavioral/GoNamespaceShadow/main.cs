namespace go;

using fmt = fmt_package;
using Δmath = math_package;
using rand = global::go.math.rand_package;
using nsshadow = global::go.go.nsshadow_package;
using global::go.go;
using global::go.math;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸmath() {
    builtin.initPackage(typeof(math_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸmathꓸrand() {
    builtin.initPackage(typeof(global::go.math.rand_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸgoꓸnsshadow() {
    builtin.initPackage(typeof(global::go.go.nsshadow_package));
}

internal static void Main() {
    fmt.Println(nsshadow.Add(Δmath.MaxInt8, rand.Intn(1)));
    fmt.Println(nsshadow.Max8() + nsshadow.Pad());
}

} // end main_package
