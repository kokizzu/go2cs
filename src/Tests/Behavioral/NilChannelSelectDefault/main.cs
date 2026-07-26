namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    channel<nint> nilRecv = default!;
    var selᴛ1 = nilRecv;
    switch (trySelect(ᐸꟷ(selᴛ1, ꓸꓸꓸ))) {
    case 0 when selᴛ1.ꟷᐳ(out var v): {
        fmt.Println((@string)"nil recv took case"u8, v);
        break;
    }
    default: {
        fmt.Println((@string)"nil recv default"u8);
        break;
    }}
    fmt.Println((@string)"nil len:"u8, len(nilRecv), (@string)"nil cap:"u8, cap(nilRecv));
    var selᴛ2 = nilRecv;
    switch (trySelect(ᐸꟷ(selᴛ2, ꓸꓸꓸ))) {
    case 0 when selᴛ2.ꟷᐳ(out var v, out var ok): {
        fmt.Println((@string)"nil comma-ok took case"u8, v, ok);
        break;
    }
    default: {
        fmt.Println((@string)"nil comma-ok default"u8);
        break;
    }}
    var ready = new channel<nint>(1);
    var selᴛ3 = ready;
    switch (trySelect(ᐸꟷ(selᴛ3, ꓸꓸꓸ))) {
    case 0 when selᴛ3.ꟷᐳ(out var v): {
        fmt.Println((@string)"real recv took case"u8, v);
        break;
    }
    default: {
        fmt.Println((@string)"real empty default"u8);
        break;
    }}
    ready.ᐸꟷ(7);
    var selᴛ4 = ready;
    switch (trySelect(ᐸꟷ(selᴛ4, ꓸꓸꓸ))) {
    case 0 when selᴛ4.ꟷᐳ(out var v): {
        fmt.Println((@string)"real recv"u8, v);
        break;
    }
    default: {
        fmt.Println((@string)"real default"u8);
        break;
    }}
    ready.ᐸꟷ(9);
    var selᴛ5 = nilRecv;
    var selᴛ6 = ready;
    switch (select(ᐸꟷ(selᴛ5, ꓸꓸꓸ), ᐸꟷ(selᴛ6, ꓸꓸꓸ))) {
    case 0 when selᴛ5.ꟷᐳ(out var v): {
        fmt.Println((@string)"mixed took nil"u8, v);
        break;
    }
    case 1 when selᴛ6.ꟷᐳ(out var v): {
        fmt.Println((@string)"mixed took real"u8, v);
        break;
    }}
}

} // end main_package
