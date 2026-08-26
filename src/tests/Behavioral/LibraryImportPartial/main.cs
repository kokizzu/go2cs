namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object libraryImportPartialˢ = (@string)"library import partial:"u8;

internal static void Main() {
    fmt.Println(libraryImportPartialˢ, describe());
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string compiledInitializedAndˢ = "compiled, initialized and ran"u8;

internal static @string describe() {
    return compiledInitializedAndˢ;
}

} // end main_package
