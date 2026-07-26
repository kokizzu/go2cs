namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial interface swapper {
    bool CompareAndSwap(any key, any old, any @new);
    @string Guard(@string @lock, @string @base, nint @event);
}

[GoType] partial struct cell {
    internal any value;
    internal @string tag;
}

[GoRecv] internal static bool CompareAndSwap(this ref cell c, any key, any old, any @new) {
    if (!AreEqual(c.value, old)) {
        return false;
    }
    c.value = @new;
    c.tag = fmt.Sprint(key);
    return true;
}

[GoRecv] internal static @string Guard(this ref cell c, @string @lock, @string @base, nint @event) {
    return fmt.Sprintf("%s|%s|%d|%s"u8, @lock, @base, @event, c.tag);
}

[GoType] partial struct frozen {
    internal @string label;
}

internal static bool CompareAndSwap(this frozen f, any key, any old, any @new) {
    return AreEqual(old, @new);
}

internal static @string Guard(this frozen f, @string @lock, @string @base, nint @event) {
    return fmt.Sprintf("%s~%s~%d~%s"u8, @lock, @base, @event, f.label);
}

internal static void exercise(swapper s) {
    fmt.Println(s.CompareAndSwap((@string)"k"u8, (nint)(2), (nint)(3)));
    fmt.Println(s.CompareAndSwap((@string)"k"u8, (nint)(1), (nint)(3)));
    fmt.Println(s.CompareAndSwap((@string)"k"u8, (nint)(3), (nint)(3)));
    fmt.Println(s.Guard("L"u8, "B"u8, 7));
}

internal static void Main() {
    exercise(new cellжswapper(Ꮡ(new cell(value: (nint)(1)))));
    exercise(new frozen(label: "static"u8));
    var c = Ꮡ(new cell(value: (nint)(42)));
    fmt.Println(c.CompareAndSwap((@string)"direct"u8, (nint)(42), (nint)(43)), c.Guard("l"u8, "b"u8, 1));
}

} // end main_package
