namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct box {
    internal nint v;
}

internal static nint calls;

internal static (ж<box>, error) makeBox(nint v) {
    return (Ꮡ(new box(v: v)), default!);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string helloˢ = "hello"u8;

internal static (nint, @string) pair() {
    calls++;
    return (42, helloˢ);
}

internal static ж<box> defaultBox = makeBox(7).Item1;
internal static error _ᴛ1ʗ;

internal static (nint, @string) tupleᴛ1ʗ = pair();
internal static nint n = tupleᴛ1ʗ.Item1;
internal static @string s = tupleᴛ1ʗ.Item2;

internal static void Main() {
    fmt.Println((~defaultBox).v);
    fmt.Println(n, s);
    fmt.Println(calls);

    var (ln, ls) = pair();
    fmt.Println(ln, ls, calls);

    var (si, fi) = ifaceAndFunc();
    fmt.Println(si.String(), fi());
}

[GoType] partial interface stringer {
    @string String();
}

[GoType] partial struct sval {
    internal @string s;
}

internal static @string String(this sval v) {
    return v.s;
}

internal static (stringer, Func<nint>) ifaceAndFunc() {
    return (new sval(s: "iface"u8), () => 9);
}

} // end main_package
