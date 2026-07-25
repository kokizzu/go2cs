namespace go;

using fmt = fmt_package;
using time = time_package;

partial class main_package {

internal static void Main() {
    goǃ(ᴛ1 => fmt.Println(ᴛ1), (@string)"First");
    goǃ(ᴛ1 => fmt.Println(ᴛ1), (@string)"Second");
    goǃ(ᴛ1 => fmt.Println(ᴛ1), (@string)"Third");
    var f1 = fmt.Println;
    var f1ʗ1 = f1;
    goǃ(ᴛ1 => f1ʗ1(ᴛ1), (@string)"Fourth");
    goǃ(GetPrintLn(), (@string)"Fifth");
    goǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"Function result:", add(3, 4));
    printSquare(5);
    nint count = 1;
    goǃ(() => {
        fmt.Println((@string)"Go count (closure):", count);
    });
    count = 10;
    fmt.Println((@string)"Count before Go:", count);
    time.Sleep(200);
    var done = new channel<EmptyStruct>(0);
    runPair(done);
    ᐸꟷ(done);
    var acc = Ꮡ(new accum(nil));
    bindAdd(acc);
    fmt.Println((@string)"accum total:", (~acc).total);
    fmt.Println((@string)"Main function");
}

[GoType] partial struct accum {
    internal nint total;
}

[GoRecv] internal static nint add(this ref accum a, nint n) {
    a.total += n;
    return a.total;
}

internal static void bindAdd(ж<accum> Ꮡa) {
    var add = (nint p1) => Ꮡa.add(p1);
    fmt.Println((@string)"bound add:", add(5), add(7));
}

public static Action<@string> GetPrintLn() {
    return (@string src) => {
        fmt.Println(src);
    };
}

internal static nint add(nint x, nint y) {
    nint result = x + y;
    fmt.Println((@string)"Calculate:", result);
    return result;
}

internal static void runPair(channel<EmptyStruct> done) {
    @string tag = "pair"u8;
    var handler = (channel<EmptyStruct> ch, Action fn) => {
        fn();
        fmt.Println((@string)"handled:", tag);
        ch.ᐸꟷ(new EmptyStruct());
    };
    var handlerʗ1 = handler;
    goǃ(handlerʗ1, done, () => {
        fmt.Println((@string)"inner fn ran");
    });
}

internal static void printSquare(nint n) {
    goǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"Go thread square:", n * n);
    n++;
    fmt.Println((@string)"Immediate n:", n);
}

} // end main_package
