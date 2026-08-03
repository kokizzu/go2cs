namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct Box<T> {
    internal T v;
}

internal static void setT<T>(ж<T> Ꮡp, T val) {
    ref var p = ref Ꮡp.DerefOrNull();

    p = val;
}

internal static T getT<T>(ж<T> Ꮡp) {
    ref var p = ref Ꮡp.DerefOrNull();

    return p;
}

public static void Set<T>(this ж<Box<T>> Ꮡb, T val) {
    setT(Ꮡb.of(Box<T>.Ꮡv), val);
}

public static T Get<T>(this ж<Box<T>> Ꮡb) {
    return getT(Ꮡb.of(Box<T>.Ꮡv));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object intˢ = (@string)"int:"u8;
private static readonly @string helloˢ = "hello"u8;
private static readonly object stringˢ = (@string)"string:"u8;

internal static void Main() {
    ref var bi = ref heap(new Box<nint>(), out var Ꮡbi);
    Ꮡbi.Set(42);
    Ꮡbi.Set(Ꮡbi.Get() + 1);
    fmt.Println(intˢ, Ꮡbi.Get());
    ref var bs = ref heap(new Box<@string>(), out var Ꮡbs);
    Ꮡbs.Set(helloˢ);
    fmt.Println(stringˢ, Ꮡbs.Get());
}

} // end main_package
