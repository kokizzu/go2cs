namespace go;

using fmt = fmt_package;
using ꓸꓸꓸany = Span<any>;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string valueVFlagVˢ = "value=%v flag=%v"u8;
private static readonly object noMatchˢ = (@string)"no match"u8;
private static readonly object plainˢ = (@string)"plain"u8;
private static readonly object unexpectedMatchˢ = (@string)"unexpected match"u8;
private static readonly object noMatchForStringˢ = (@string)"no match for string"u8;

internal static void Main() {
    Actionꓸꓸꓸ<@string, any> fn = (@string format, params ꓸꓸꓸany argsʗp) => {
        var args = argsʗp.slice();
        fmt.Printf(format + "\n"u8, args.ꓸꓸꓸ);
    };
    any logf = fn;
    {
        var (fnΔ1, ok) = logf._<Actionꓸꓸꓸ<@string, any>>(ᐧ); if (ok){
            fnΔ1(valueVFlagVˢ, (nint)(42), true);
        } else {
            fmt.Println(noMatchˢ);
        }
    }
    any notFn = plainˢ;
    {
        var (_, ok) = notFn._<Actionꓸꓸꓸ<@string, any>>(ᐧ); if (ok){
            fmt.Println(unexpectedMatchˢ);
        } else {
            fmt.Println(noMatchForStringˢ);
        }
    }
    any plain = (@string s) => s + "!"u8;
    {
        var (fnΔ2, ok) = plain._<Func<@string, @string>>(ᐧ); if (ok) {
            fmt.Println(fnΔ2("ok"u8));
        }
    }
}

} // end main_package
