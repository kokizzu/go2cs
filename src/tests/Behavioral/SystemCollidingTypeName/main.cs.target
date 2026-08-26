namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct ValueType {
    internal @string unit;
}

internal static @string describe(this ValueType v) {
    return v.unit;
}

[GoType] partial interface message {
    @string describe();
}

internal static void show(message m) {
    fmt.Println(m.describe());
}

internal static void Main() {
    message m = new ValueType(unit: "cpu in nanoseconds"u8);
    show(m);
    ref var v = ref heap<ValueType>(out var Ꮡv);
    v = new ValueType(unit: "heap in bytes"u8);
    message mp = new ValueTypeжmessage(Ꮡv);
    show(mp);
    fmt.Println(mp.describe());
    show(new ValueType(unit: "v1.4"u8));
}

} // end main_package
