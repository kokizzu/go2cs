namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct gadget {
    internal nint n;
}

internal static @string Foo(this gadget g) {
    return fmt.Sprintf("foo %d"u8, g.n);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string barˢ = "bar"u8;

[GoRecv] internal static @string Bar(this ref gadget g) {
    return barˢ;
}

[GoType] partial interface fooer {
    @string Foo();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object valueWidenedWrongˢ = (@string)"value-widened-wrong"u8;
private static readonly object valueNotWidenedOkˢ = (@string)"value-not-widened-ok"u8;
private static readonly object pointerWidenedOkˢ = (@string)"pointer-widened-ok"u8;
private static readonly object pointerNotWidenedWrongˢ = (@string)"pointer-not-widened-wrong"u8;

[GoType("dyn")] internal partial interface main_type {
    @string Foo();
    @string Bar();
}

[GoType("dyn")] internal partial interface main_typeᴛ1 {
    @string Foo();
    @string Bar();
}

internal static void Main() {
    fooer v = new gadget(1);
    {
        var (_, ok) = v._<main_type>(ᐧ); if (ok){
            fmt.Println(valueWidenedWrongˢ);
        } else {
            fmt.Println(valueNotWidenedOkˢ);
        }
    }
    fooer p = new gadgetжfooer(Ꮡ(new gadget(2)));
    {
        var (b, ok) = p._<main_typeᴛ1>(ᐧ); if (ok){
            fmt.Println(pointerWidenedOkˢ, b.Foo(), b.Bar());
        } else {
            fmt.Println(pointerNotWidenedWrongˢ);
        }
    }
}

} // end main_package
