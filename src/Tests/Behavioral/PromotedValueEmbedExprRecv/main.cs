namespace go;

using fmt = fmt_package;
using PromotedValueEmbedLib = PromotedValueEmbedLib_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string boxedˢ = "boxed"u8;

internal static map<@string, any> registry() {
    return new map<@string, any>{["w"u8] = PromotedValueEmbedLib.New(boxedˢ)};
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string directˢ = "direct"u8;

internal static void Main() {
    var m = registry();
    fmt.Println(m["w"u8]._<ж<PromotedValueEmbedLib.Widget>>().Name());
    fmt.Println(PromotedValueEmbedLib.New(directˢ).Name());
}

} // end main_package
