namespace go;

using fmt = fmt_package;
using PromotedValueEmbedLib = PromotedValueEmbedLib_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string matchˢ = "match"u8;
private static readonly @string nomatchˢ = "nomatch"u8;

internal static @string describe(ж<PromotedValueEmbedLib.Widget> Ꮡw) {
    if (Ꮡw.Name() == "widget"u8) {
        return matchˢ;
    }
    return nomatchˢ;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string widgetˢ = "widget"u8;
private static readonly @string gadgetˢ = "gadget"u8;

internal static void Main() {
    var w = PromotedValueEmbedLib.New(widgetˢ);
    fmt.Println(w.Name(), w.Name() == "widget"u8, describe(w));
    var g = PromotedValueEmbedLib.NewGadget(gadgetˢ);
    fmt.Println(g.Name());
}

} // end main_package
