namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    channel<nint> nilCh = default!;
    fmt.Println((@string)"nil len:"u8, len(nilCh), (@string)"cap:"u8, cap(nilCh));
    var live = new channel<nint>(1);
    live.ᐸꟷ(42);
    var selᴛ1 = nilCh;
    var selᴛ2 = nilCh.ᐸꟷ(1, ꓸꓸꓸ);
    var selᴛ3 = live;
    switch (select(ᐸꟷ(selᴛ1, ꓸꓸꓸ), selᴛ2, ᐸꟷ(selᴛ3, ꓸꓸꓸ))) {
    case 0 when selᴛ1.ꟷᐳ(out var v): {
        fmt.Println((@string)"nil recv (wrong):"u8, v);
        break;
    }
    case 1: {
        fmt.Println((@string)"nil send (wrong)"u8);
        break;
    }
    case 2 when selᴛ3.ꟷᐳ(out var v): {
        fmt.Println((@string)"live recv:"u8, v);
        break;
    }}
    var u = new channel<nint>(0);
    var uʗ1 = u;
    goǃ(() => {
        uʗ1.ᐸꟷ(7);
    });
    var selᴛ4 = nilCh;
    var selᴛ5 = u;
    switch (select(ᐸꟷ(selᴛ4, ꓸꓸꓸ), ᐸꟷ(selᴛ5, ꓸꓸꓸ))) {
    case 0 when selᴛ4.ꟷᐳ(out var v): {
        fmt.Println((@string)"nil recv (wrong):"u8, v);
        break;
    }
    case 1 when selᴛ5.ꟷᐳ(out var v): {
        fmt.Println((@string)"rendezvous recv:"u8, v);
        break;
    }}
    var @out = new channel<nint>(1);
    var selᴛ6 = nilCh.ᐸꟷ(9, ꓸꓸꓸ);
    var selᴛ7 = @out.ᐸꟷ(5, ꓸꓸꓸ);
    switch (select(selᴛ6, selᴛ7)) {
    case 0: {
        fmt.Println((@string)"nil send (wrong)"u8);
        break;
    }
    case 1: {
        fmt.Println((@string)"live send fired"u8);
        break;
    }}
    fmt.Println((@string)"out:"u8, ᐸꟷ(@out));
}

} // end main_package
