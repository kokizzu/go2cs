namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct thing {
    internal nint n;
}

[GoRecv] internal static @string Ping(this ref thing t) {
    return fmt.Sprintf("ping %d"u8, t.n);
}

internal static @string Pong(this thing t) {
    return "pong"u8;
}

[GoType] partial interface speaker {
    @string Pong();
}

[GoType("dyn")] partial interface main_type {
    @string Ping();
}

[GoType("dyn")] partial interface main_typeᴛ1 {
    @string Ping();
    @string Pong();
}

[GoType("dyn")] partial interface main_typeᴛ2 {
    void Quit();
}

internal static void Main() {
    speaker s = new thingжspeaker(Ꮡ(new thing(7)));
    {
        var (p, ok) = s._<main_type>(ᐧ); if (ok){
            fmt.Println((@string)"ping-ok"u8, p.Ping());
        } else {
            fmt.Println((@string)"ping-missed"u8);
        }
    }
    {
        var (b, ok) = s._<main_typeᴛ1>(ᐧ); if (ok){
            fmt.Println((@string)"both-ok"u8, b.Ping(), b.Pong());
        } else {
            fmt.Println((@string)"both-missed"u8);
        }
    }
    {
        var (_, ok) = s._<main_typeᴛ2>(ᐧ); if (ok){
            fmt.Println((@string)"quit-matched-wrong"u8);
        } else {
            fmt.Println((@string)"quit-missed-ok"u8);
        }
    }
}

} // end main_package
