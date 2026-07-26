namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deferredResultˢ = (@string)"Deferred result:"u8;
private static readonly object doingSomethingElseˢ = (@string)"Doing something else"u8;

internal static void Main() => func((defer, recover) => {
    deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), deferredResultˢ, add(3, 4), defer);
    fmt.Println(doingSomethingElseˢ);
});

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object calculateˢ = (@string)"Calculate:"u8;

internal static nint add(nint x, nint y) {
    nint result = x + y;
    fmt.Println(calculateˢ, result);
    return result;
}

} // end main_package
