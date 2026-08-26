namespace go.go;

using math = math_package;
using rand = global::go.math.rand_package;
using global::go.math;

partial class nsshadow_package {

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

public static nint Add(nint x, nint y) {
    return x + y;
}

public static nint Max8() {
    return math.MaxInt8;
}

public static nint Pad() {
    return rand.Intn(1);
}

} // end nsshadow_package
