namespace go;

using fmt = fmt_package;
// blank import: BlankImportSideEffects.jpeglike_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
// blank import: BlankImportSideEffects.pnglike_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
using registry = BlankImportSideEffects.registry_package;
using BlankImportSideEffects;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸBlankImportSideEffectsꓸjpeglike() {
    builtin.initPackage(typeof(BlankImportSideEffects.jpeglike_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸBlankImportSideEffectsꓸpnglike() {
    builtin.initPackage(typeof(BlankImportSideEffects.pnglike_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸBlankImportSideEffectsꓸregistry() {
    builtin.initPackage(typeof(BlankImportSideEffects.registry_package));
}

internal static nint countAtInit;

[GoInit] internal static void init() {
    countAtInit = registry.Count();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object countAtInitˢ = (@string)"count at init:"u8;
private static readonly object foundˢ = (@string)"found:"u8;
private static readonly object missingˢ = (@string)"missing:"u8;
private static readonly object countˢ = (@string)"count:"u8;

internal static void Main() {
    fmt.Println(countAtInitˢ, countAtInit);
    foreach (var (_, name) in new @string[]{"jpeglike"u8, "pnglike"u8, "giflike"u8}.slice()) {
        {
            var (describe, ok) = registry.Lookup(name); if (ok){
                fmt.Println(foundˢ, name, (@string)"=>"u8, describe);
            } else {
                fmt.Println(missingˢ, name);
            }
        }
    }
    fmt.Println(countˢ, registry.Count());
}

} // end main_package
