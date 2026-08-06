namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object openFileˢ = (@string)"Open file"u8;
private static readonly object closeFileˢ = (@string)"Close file"u8;
private static readonly object writeDataToFileˢ = (@string)"Write data to file"u8;

internal static void Main() => func((defer, recover) => {
    fmt.Println(openFileˢ);
    deferǃ(ᴛ1 => fmt.Println(ᴛ1), closeFileˢ, defer);
    fmt.Println(writeDataToFileˢ);
});

} // end main_package
