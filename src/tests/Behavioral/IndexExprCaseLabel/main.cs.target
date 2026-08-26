namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static array<@string> words = new @string[]{"alpha"u8, "beta"u8, "gamma"u8}.array();

internal static UntypedInt one => 1;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string endsˢ = "ends"u8;
private static readonly @string middleˢ = "middle"u8;
private static readonly @string noneˢ = "none"u8;

internal static @string classify(@string s) {
    var exprᴛ1 = s;
    if (exprᴛ1 == words[0] || exprᴛ1 == words[2]) {
        return endsˢ;
    }
    if (exprᴛ1 == words[one]) {
        return middleˢ;
    }
    { /* default: */
        return noneˢ;
    }

}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string alphaˢ = "alpha"u8;
private static readonly @string betaˢ = "beta"u8;
private static readonly @string gammaˢ = "gamma"u8;
private static readonly @string deltaˢ = "delta"u8;

internal static void Main() {
    fmt.Println(classify(alphaˢ));
    fmt.Println(classify(betaˢ));
    fmt.Println(classify(gammaˢ));
    fmt.Println(classify(deltaˢ));
}

} // end main_package
