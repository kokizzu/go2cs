namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object beforeGoroutinePanicˢ = (@string)"before goroutine panic"u8;

internal static void Main() {
    fmt.Println(beforeGoroutinePanicˢ);
    var done = new channel<EmptyStruct>(0);
    goǃ(() => {
        throw panic("goroutine boom");
    });
    ᐸꟷ(done);
}

} // end main_package
