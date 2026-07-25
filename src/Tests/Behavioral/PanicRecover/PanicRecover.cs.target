namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    f();
    panicValues();
    fmt.Println((@string)"Returned normally from f.");
}

internal static void f() => func((defer, recover) => {
    defer(() => {
        {
            var r = recover(); if (r != default!) {
                fmt.Println((@string)"Recovered in f", r);
            }
        }
    });
    fmt.Println((@string)"Calling g.");
    g(0);
    fmt.Println((@string)"Returned normally from g.");
});

internal static void g(nint i) => func((defer, recover) => {
    if (i > 3) {
        fmt.Println((@string)"Panicking!");
        throw panic(fmt.Sprintf("%v"u8, i));
    }
    deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"Defer in g", i, defer);
    fmt.Println((@string)"Printing in g", i);
    g(i + 1);
});

internal static @string panicValueKind(Action f) {
    @string @out = default!;
    ((Action)(() => func((defer, recover) => {
        defer(() => {
            var p = recover();
            switch (p.type()) {
            case null: {
                @out = "no panic"u8;
                break;
            }
            case @string v: {
                @out = fmt.Sprintf("string(%s) eq-x=%v"u8, v, v == "x"u8);
                break;
            }
            case {} Δv when Δv._<error>(out var v): {
                @out = "error("u8 + v.Error() + ")"u8;
                break;
            }
            case nint v: {
                @out = fmt.Sprintf("int(%d)"u8, v);
                break;
            }
            case int32 v: {
                @out = fmt.Sprintf("int(%d)"u8, v);
                break;
            }
            default: {
                var v = p;
                @out = "other (not a plain string)"u8;
                break;
            }}
        });
        f();
    })))();
    return @out;
}

[GoType("@string")] partial struct panicValues_label;

internal static void panicValues() {
    fmt.Println(panicValueKind(() => {
        throw panic("x");
    }));
    fmt.Println(panicValueKind(() => {
        throw panic(fmt.Sprintf("%s"u8, (@string)"x"));
    }));
    fmt.Println(panicValueKind(() => {
        @string s = "x"u8;
        throw panic(s);
    }));
    fmt.Println(panicValueKind(() => {
        throw panic(((panicValues_label)(@string)"x"u8));
    }));
    fmt.Println(panicValueKind(() => {
        throw panic(42);
    }));
    fmt.Println(panicValueKind(() => {
    }));
}

} // end main_package
