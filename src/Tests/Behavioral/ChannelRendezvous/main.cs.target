namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    var ch = new channel<nint>(0);
    fmt.Println((@string)"cap:"u8, cap(ch), (@string)"len:"u8, len(ch));
    var selᴛ1 = ch.ᐸꟷ(1, ꓸꓸꓸ);
    switch (trySelect(selᴛ1)) {
    case 0: {
        fmt.Println((@string)"send: ready (wrong for unbuffered)"u8);
        break;
    }
    default: {
        fmt.Println((@string)"send: not ready (no receiver)"u8);
        break;
    }}
    var selᴛ2 = ch;
    switch (trySelect(ᐸꟷ(selᴛ2, ꓸꓸꓸ))) {
    case 0 when selᴛ2.ꟷᐳ(out var v): {
        fmt.Println((@string)"recv: ready (wrong):"u8, v);
        break;
    }
    default: {
        fmt.Println((@string)"recv: not ready (no sender)"u8);
        break;
    }}
    fmt.Println((@string)"after probes: len:"u8, len(ch), (@string)"cap:"u8, cap(ch));
    var reply = new channel<nint>(0);
    var chʗ1 = ch;
    var replyʗ1 = reply;
    goǃ(() => {
        nint v = ᐸꟷ(chʗ1);
        replyʗ1.ᐸꟷ(v * 2);
    });
    ch.ᐸꟷ(21);
    fmt.Println((@string)"reply:"u8, ᐸꟷ(reply));
    fmt.Println((@string)"after rendezvous: len:"u8, len(ch), (@string)"cap:"u8, cap(ch));
    var ping = new channel<nint>(0);
    var pong = new channel<nint>(0);
    var pingʗ1 = ping;
    var pongʗ1 = pong;
    goǃ(() => {
        for (nint i = 0; i < 3; i++) {
            nint v = ᐸꟷ(pingʗ1);
            pongʗ1.ᐸꟷ(v + 1);
        }
    });
    for (nint i = 0; i < 3; i++) {
        ping.ᐸꟷ(i * 10);
        fmt.Println((@string)"pong:"u8, ᐸꟷ(pong));
    }
}

} // end main_package
