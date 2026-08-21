[assembly: go.GoPositionMap("main.go", "main.cs", "AAgKgoKU1qKCgpTWgoKCgoI=")]

namespace go;

using fmt = fmt_package;

partial class main_package {

internal static bool /*_*/ valid(bool flag) {
    if (flag) {
        return true;
    }
    return default!;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string setˢ = "set"u8;

internal static (nint, @string label) pair(@string tag) {
    @string label = default!;

    label = "empty:"u8 + tag;
    if (tag == "set"u8) {
        return (7, setˢ);
    }
    return (default!, label);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string baseˢ = "base"u8;

internal static void Main() {
    fmt.Println(valid(true), valid(false));
    var (n, label) = pair(baseˢ);
    fmt.Println(n, label);
    (n, label) = pair(setˢ);
    fmt.Println(n, label);
}

} // end main_package
