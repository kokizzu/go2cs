namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct box {
    internal nint n;
}

internal static void Main() {
    safeAssertions();
    assertionsWithPanic();
    pointerAssertion();
    fmt.Println((@string)"Program completed after panic recovery");
}

internal static void pointerAssertion() {
    any i = Ꮡ(new box(n: 7));
    {
        var (bΔ1, ok) = i._<ж<box>>(ᐧ); if (ok) {
            fmt.Println((@string)"Value is a *box:", (~bΔ1).n);
        }
    }
    var b = i._<ж<box>>();
    fmt.Println((@string)"Pointer value:", (~b).n);
}

internal static void safeAssertions() {
    any i = (@string)"hello";
    {
        var (s, ok) = i._<@string>(ᐧ); if (ok){
            fmt.Println((@string)"Value is a string:", s);
        } else {
            fmt.Println((@string)"Value is not a string");
        }
    }
    {
        var (n, ok) = i._<nint>(ᐧ); if (ok){
            fmt.Println((@string)"Value is an int:", n);
        } else {
            fmt.Println((@string)"Value is not an int");
        }
    }
}

internal static void assertionsWithPanic() => func((defer, recover) => {
    defer(() => {
        {
            var r = recover(); if (r != default!) {
                fmt.Println((@string)"Recovered from panic:", r);
            }
        }
    });
    any i = (@string)"hello";
    @string s = i._<@string>();
    fmt.Println((@string)"String value:", s);
    nint n = i._<nint>();
    fmt.Println((@string)"Integer value:", n);
});

} // end main_package
