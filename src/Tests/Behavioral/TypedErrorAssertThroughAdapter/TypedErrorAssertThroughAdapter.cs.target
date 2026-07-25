namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct myErr {
    internal nint code;
}

[GoRecv] internal static @string Error(this ref myErr e) {
    return fmt.Sprintf("myErr %d"u8, e.code);
}

[GoType] partial struct valErr {
    internal @string tag;
}

internal static @string Error(this valErr e) {
    return "valErr "u8 + e.tag;
}

internal static error pointerSourced() {
    return new myErrжerror(Ꮡ(new myErr(code: 42)));
}

internal static error valueSourced() {
    return new valErr(tag: "v"u8);
}

[GoType("dyn")] partial interface main_type {
    @string Error();
}

internal static void Main() {
    var err = pointerSourced();
    var p = err._<ж<myErr>>();
    p.Value.code = 7;
    fmt.Println((@string)"ptr-assert", (~p).code, err.Error());
    var v = valueSourced()._<valErr>();
    fmt.Println((@string)"val-assert", v.tag, v.Error());
    {
        var (_, ok) = valueSourced()._<ж<myErr>>(ᐧ); if (ok){
            fmt.Println((@string)"ptr-miss-WRONG");
        } else {
            fmt.Println((@string)"ptr-miss-ok");
        }
    }
    {
        var (_, ok) = pointerSourced()._<valErr>(ᐧ); if (ok){
            fmt.Println((@string)"val-miss-WRONG");
        } else {
            fmt.Println((@string)"val-miss-ok");
        }
    }
    ((Action)(() => func((defer, recover) => {
        defer(() => {
            {
                var r = recover(); if (r != default!) {
                    fmt.Println((@string)"recovered");
                }
            }
        });
        _ = pointerSourced()._<valErr>();
        fmt.Println((@string)"unreachable");
    })))();
    {
        var (s, ok) = pointerSourced()._<main_type>(ᐧ); if (ok){
            fmt.Println((@string)"iface-assert", s.Error());
        } else {
            fmt.Println((@string)"iface-assert-missed");
        }
    }
}

} // end main_package
