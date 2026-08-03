namespace go;

using fmt = fmt_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string helloWorldˢ = "Hello World"u8;

internal static void Main() {
    ref var a = ref heap(new @string(), out var Ꮡa);
    a = helloWorldˢ;
    test(a);
    fmt.Println(a);
    fmt.Println();
    a = helloWorldˢ;
    test2(Ꮡa);
    fmt.Println(a);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string goodbyeWorldˢ = "Goodbye World"u8;

internal static void test(@string a) {
    fmt.Println(a);
    a = goodbyeWorldˢ;
    fmt.Println(a);
}

internal static void test2(ж<@string> Ꮡa) {
    ref var a = ref Ꮡa.DerefOrNull();

    fmt.Println(a);
    a = goodbyeWorldˢ;
    fmt.Println(a);
}

} // end main_package
