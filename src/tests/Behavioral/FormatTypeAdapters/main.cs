[assembly: go.GoPositionMap("main.go", "main.cs", "ABUqgAAJCoAABhCCgoSChIKEhIKG")]

namespace go;

using fmt = fmt_package;
using typelib = FormatTypeAdapters.typelib_package;
using FormatTypeAdapters;

partial class main_package {

[GoType] partial interface greeter {
    @string greet();
}

[GoType] partial struct loud {
    internal nint n;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string loudˢ = "LOUD"u8;

[GoRecv] internal static @string greet(this ref loud l) {
    return loudˢ;
}

[GoType] partial struct soft {
    internal nint n;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string softˢ = "soft"u8;

internal static @string greet(this soft s) {
    return softˢ;
}

[GoType] partial interface stamper {
    @string Stamp();
}

internal static void Main() {
    greeter g = new loudжgreeter(Ꮡ(new loud(n: 1)));
    fmt.Printf("%T\n"u8, g);
    greeter h = new soft(n: 2);
    fmt.Printf("%T\n"u8, h);
    any raw = Ꮡ(new loud(n: 3));
    fmt.Printf("%T\n"u8, raw);
    fmt.Printf("%T\n"u8, new soft(n: 4));
    stamper m = new typelib_Markᴠstamper(typelib.NewMark("x"u8));
    fmt.Printf("%T\n"u8, m);
    fmt.Println(g.greet(), h.greet(), m.Stamp());
}

} // end main_package
