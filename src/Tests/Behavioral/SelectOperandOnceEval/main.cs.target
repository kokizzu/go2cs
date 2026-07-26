namespace go;

using fmt = fmt_package;

partial class main_package {

internal static nint made;

internal static channel<nint> fresh() {
    made++;
    var ch = new channel<nint>(1);
    ch.ᐸꟷ(made * 10);
    return ch;
}

internal static nint afterCalls;

internal static channel<nint> after() {
    afterCalls++;
    var ch = new channel<nint>(0);
    var chʗ1 = ch;
    goǃ(() => {
        chʗ1.ᐸꟷ(99);
    });
    return ch;
}

internal static array<nint> sink = new(2);

internal static nint swap(ж<channel<nint>> Ꮡch, channel<nint> repl) {
    ref var ch = ref Ꮡch.ValueSlot;

    ch = repl;
    return 0;
}

internal static void Main() {
    var selᴛ1 = fresh();
    switch (select(ᐸꟷ(selᴛ1, ꓸꓸꓸ))) {
    case 0 when selᴛ1.ꟷᐳ(out var v): {
        fmt.Println((@string)"S1 got:"u8, v, (@string)"made ="u8, made);
        break;
    }}
    var selᴛ2 = after();
    switch (select(ᐸꟷ(selᴛ2, ꓸꓸꓸ))) {
    case 0 when selᴛ2.ꟷᐳ(out var v): {
        fmt.Println((@string)"S2 got:"u8, v, (@string)"afterCalls ="u8, afterCalls);
        break;
    }}
    ref var ch = ref heap<channel<nint>>(out var Ꮡch);
    ch = new channel<nint>(1);
    ch.ᐸꟷ(7);
    var repl = new channel<nint>(1);
    repl.ᐸꟷ(8);
    var selᴛ3 = ch;
    switch (select(ᐸꟷ(selᴛ3, ꓸꓸꓸ))) {
    case 0 when selᴛ3.ꟷᐳ(out sink[swap(Ꮡch, repl)]): {
        fmt.Println((@string)"S3 sink[0] ="u8, sink[0], (@string)"len(ch) ="u8, len(ch), (@string)"len(repl) ="u8, len(repl));
        break;
    }}
    var selᴛ4 = fresh();
    switch (trySelect(ᐸꟷ(selᴛ4, ꓸꓸꓸ))) {
    case 0 when selᴛ4.ꟷᐳ(out var v): {
        fmt.Println((@string)"S4 got:"u8, v, (@string)"made ="u8, made);
        break;
    }
    default: {
        fmt.Println((@string)"S4 default (wrong)"u8);
        break;
    }}
}

} // end main_package
