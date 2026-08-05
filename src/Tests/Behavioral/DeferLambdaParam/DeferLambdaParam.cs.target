namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deferredCountClosureˢ = (@string)"Deferred count (closure):"u8;
private static readonly object countBeforeDeferˢ = (@string)"Count before defer:"u8;

internal static void Main() {
    GoFrame ᒐ = default;
    try {
        nint count = 1;
        deferǃ((nint cnt) => {
            fmt.Println(deferredCountClosureˢ, cnt);
        }, count, ref ᒐ);
        count = 10;
        fmt.Println(countBeforeDeferˢ, count);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end main_package
