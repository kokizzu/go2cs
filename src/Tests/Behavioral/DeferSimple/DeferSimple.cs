namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object openFileˢ = (@string)"Open file"u8;
private static readonly object closeFileˢ = (@string)"Close file"u8;
private static readonly object writeDataToFileˢ = (@string)"Write data to file"u8;

internal static void Main() {
    GoFrame ᒐ = default;
    try {
        fmt.Println(openFileˢ);
        deferǃ(ᴛ1 => fmt.Println(ᴛ1), closeFileˢ, ref ᒐ);
        fmt.Println(writeDataToFileˢ);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end main_package
