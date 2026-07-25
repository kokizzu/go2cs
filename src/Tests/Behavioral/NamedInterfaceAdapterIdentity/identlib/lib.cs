namespace go.NamedInterfaceAdapterIdentity;

using fmt = fmt_package;

partial class identlib_package {

[GoType] partial interface Greeter {
    @string Greet();
}

public static (Greeter, bool) TryGreet(any v) {
    var (g, ok) = v._<Greeter>(ᐧ);
    return (g, ok);
}

public static @string Describe(Greeter g, any original) {
    any v = g;
    var (_, again) = v._<Greeter>(ᐧ);
    @string kind = "other"u8;
    switch (v.type()) {
    case @string: {
        kind = "string"u8;
        break;
    }
    case {} ᴛ0 when ᴛ0._<Greeter>(out var _): {
        kind = "greeter"u8;
        break;
    }}

    return fmt.Sprintf("%T again=%v kind=%s equal=%v text=%s"u8, v, again, kind, AreEqual(v, original), g.Greet());
}

} // end identlib_package
