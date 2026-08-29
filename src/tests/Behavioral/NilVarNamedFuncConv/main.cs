namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

public delegate void Greeter(@string name);

public static void Greet(this Greeter g, @string name) {
    g(name);
}

[GoType] partial interface Greetable {
    void Greet(@string name);
}

internal static Greetable wrap(Action<@string> handler) {
    return new GreeterᴠGreetable(NilSafeDelegateConversion<Greeter, Action<@string>>(handler));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object helloˢ = (@string)"hello"u8;
private static readonly @string worldˢ = "world"u8;

internal static void Main() {
    var nilGreetable = wrap(default!);
    var (nilGreeter, nilOk) = nilGreetable._<Greeter>(ᐧ);
    fmt.Println(nilOk, nilGreeter == default!);
    var realGreetable = wrap((@string name) => {
        fmt.Println(helloˢ, name);
    });
    var (realGreeter, realOk) = realGreetable._<Greeter>(ᐧ);
    fmt.Println(realOk, realGreeter == default!);
    realGreeter.Greet(worldˢ);
}

} // end main_package
