namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("bool")] partial struct boolVal;

internal static bool isSet(this boolVal b) {
    return (bool)b;
}

[GoType] partial interface Value {
    bool isSet();
}

internal static Value unary(Value x) {
    var y = x._<boolVal>();
    return ((boolVal)(!(bool)y));
}

internal static Value binary(@string op, Value x, Value y) {
    var a = x._<boolVal>();
    var b = y._<boolVal>();
    if (op == "and"u8) {
        return ((boolVal)((bool)a  &&  (bool)b));
    }
    return ((boolVal)((bool)a  ||  (bool)b));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string andˢ = "and"u8;

internal static void Main() {
    Value t = ((boolVal)true);
    Value f = ((boolVal)false);
    fmt.Println(unary(t).isSet());
    fmt.Println(unary(f).isSet());
    fmt.Println(binary(andˢ, t, f).isSet());
    fmt.Println(binary(andˢ, t, t).isSet());
    fmt.Println(binary("or"u8, t, f).isSet());
    fmt.Println(binary("or"u8, f, f).isSet());
}

} // end main_package
