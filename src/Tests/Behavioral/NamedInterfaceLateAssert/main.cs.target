namespace go;

using fmt = fmt_package;
using latelib = NamedInterfaceLateAssert.latelib_package;
using NamedInterfaceLateAssert;

partial class main_package {

[GoType("@string")] partial struct dirLike;

internal static @string Name(this dirLike d) {
    return ((@string)d);
}

internal static @string Kind(this dirLike d) {
    return "dir"u8;
}

internal static @string Detail(this dirLike d, @string prefix) {
    return prefix + ":"u8 + ((@string)d);
}

internal static @string Whisper(this dirLike d) {
    return "shh "u8 + ((@string)d);
}

[GoType] partial struct partial {
    internal @string n;
}

internal static @string Name(this partial p) {
    return p.n;
}

internal static @string Kind(this partial p) {
    return "partial"u8;
}

internal static void report(@string label, @string s, bool ok) {
    fmt.Println(label, ok, s);
}

internal static void Main() {
    ref var d = ref heap(new dirLike(), out var Ꮡd);
    d = "root"u8;
    var (s, ok) = latelib.TryTop(d);
    report("Top-dirLike    "u8, s, ok);
    (s, ok) = latelib.TryMid(d);
    report("Mid-dirLike    "u8, s, ok);
    (s, ok) = latelib.TryBase(d);
    report("Base-dirLike   "u8, s, ok);
    (s, ok) = latelib.TryQuiet(d);
    report("quiet-dirLike  "u8, s, ok);
    (s, ok) = latelib.TryTop(Ꮡd);
    report("Top-ptrDirLike "u8, s, ok);
    (s, ok) = latelib.TryQuiet(Ꮡd);
    report("quiet-ptrDir   "u8, s, ok);
    (s, ok) = latelib.TryTop(new partial(n: "p"u8));
    report("Top-partial    "u8, s, ok);
    (s, ok) = latelib.TryMid(new partial(n: "p"u8));
    report("Mid-partial    "u8, s, ok);
    (s, ok) = latelib.TryQuiet(new partial(n: "p"u8));
    report("quiet-partial  "u8, s, ok);
}

} // end main_package
