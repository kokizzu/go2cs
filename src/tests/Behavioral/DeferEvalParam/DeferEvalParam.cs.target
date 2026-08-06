namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    printSquare(5);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deferredSquareˢ = (@string)"Deferred square:"u8;
private static readonly object immediateNˢ = (@string)"Immediate n:"u8;

internal static void printSquare(nint n) => func((defer, recover) => {
    deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), deferredSquareˢ, n * n, defer);
    n++;
    fmt.Println(immediateNˢ, n);
});

} // end main_package
