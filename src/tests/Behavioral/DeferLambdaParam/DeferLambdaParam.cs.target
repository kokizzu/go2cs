namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deferredCountClosureˢ = (@string)"Deferred count (closure):"u8;
private static readonly object countBeforeDeferˢ = (@string)"Count before defer:"u8;

internal static void Main() => func((defer, recover) => {
    nint count = 1;
    deferǃ((nint cnt) => {
        fmt.Println(deferredCountClosureˢ, cnt);
    }, count, defer);
    count = 10;
    fmt.Println(countBeforeDeferˢ, count);
});

} // end main_package
