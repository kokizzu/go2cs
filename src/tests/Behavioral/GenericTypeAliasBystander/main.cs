namespace go;

using fmt = fmt_package;
using GenericTypeAliasLib = GenericTypeAliasLib_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string bystanderˢ = "bystander"u8;

internal static void Main() {
    var b = GenericTypeAliasLib.NewBox<nint>(41);
    b.Set(b.Get() + 1);
    fmt.Println(b.Get(), GenericTypeAliasLib.Label(bystanderˢ));
}

} // end main_package
