namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial interface speaker {
    @string speak();
}

[GoType] partial struct dog {
    internal @string name;
}

internal static @string speak(this dog d) {
    return "woof:"u8 + d.name;
}

[GoType] partial struct cat {
    internal @string name;
}

[GoRecv] internal static @string speak(this ref cat c) {
    return "meow:"u8 + c.name;
}

internal static void describe(@string prefix, any v) {
    switch (v.type()) {
    case @string s: {
        fmt.Println(prefix, (@string)"string:"u8, s);
        break;
    }
    case nint s: {
        fmt.Println(prefix, (@string)"int:"u8, s);
        break;
    }
    case int32 s: {
        fmt.Println(prefix, (@string)"int:"u8, s);
        break;
    }
    default: {
        var s = v;
        fmt.Println(prefix, (@string)"other:"u8, s);
        break;
    }}
}

internal static void Main() {
    var ch = new channel<any>(2);
    ch.ᐸꟷ((@string)"text"u8);
    ch.ᐸꟷ((nint)(42));
    describe("send"u8, ᐸꟷ(ch));
    describe("send"u8, ᐸꟷ(ch));
    var sel = new channel<any>(1);
    var selᴛ1 = sel.ᐸꟷ((@string)"sel"u8, ꓸꓸꓸ);
    switch (select(selᴛ1)) {
    case 0: {
        describe("select"u8, ᐸꟷ(sel));
        break;
    }}
    var sc = new channel<@string>(1);
    sc.ᐸꟷ("plain"u8);
    fmt.Println((@string)"string chan:"u8, ᐸꟷ(sc));
    var vs = new channel<speaker>(1);
    vs.ᐸꟷ(new dog(name: "rex"u8));
    fmt.Println((ᐸꟷ(vs)).speak());
    var ps = new channel<speaker>(1);
    var c = Ꮡ(new cat(name: "tom"u8));
    ps.ᐸꟷ(new catжspeaker(c));
    fmt.Println((ᐸꟷ(ps)).speak());
    var rt = new channel<any>(1);
    rt.ᐸꟷ((@string)"assert"u8);
    var got = ᐸꟷ(rt);
    {
        var (s, ok) = got._<@string>(ᐧ); if (ok){
            fmt.Println((@string)"assert string:"u8, s);
        } else {
            fmt.Println((@string)"assert missed"u8);
        }
    }
}

} // end main_package
