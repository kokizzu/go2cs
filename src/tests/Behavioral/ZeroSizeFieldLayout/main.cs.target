namespace go;

using fmt = fmt_package;
using @unsafe = unsafe_package;
using System.Runtime.InteropServices;

partial class main_package {

[GoType] partial struct nocopy {
}

[GoType] [StructLayout(LayoutKind.Explicit, Size = 4)] partial struct Counter {
    [FieldOffset(0)] internal readonly nocopy _;
    [FieldOffset(0)] internal int32 v;
}

[GoType] [StructLayout(LayoutKind.Explicit, Size = 8)] partial struct Wide {
    [FieldOffset(0)] internal readonly nocopy _;
    [FieldOffset(0)] internal readonly nocopy __;
    [FieldOffset(0)] internal int64 v;
}

[GoType] partial struct Plain {
    internal int32 a;
    internal int64 b;
}

[GoType] partial struct Managed {
    internal nocopy _;
    internal @string s;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object counterSizeˢ = (@string)"Counter size:"u8;
private static readonly object vOffsetˢ = (@string)"v offset:"u8;
private static readonly object wideSizeˢ = (@string)"Wide size:"u8;
private static readonly object plainSizeˢ = (@string)"Plain size:"u8;
private static readonly object bOffsetˢ = (@string)"b offset:"u8;
private static readonly object viewReadsˢ = (@string)"view reads:"u8;
private static readonly object writeThroughViewReachesˢ = (@string)"write through view reaches the original:"u8;
private static readonly object clearedˢ = (@string)"cleared:"u8;

internal static void Main() {
    fmt.Println(counterSizeˢ, /* unsafe.Sizeof(Counter{}) */ (uintptr)4, vOffsetˢ, /* unsafe.Offsetof(Counter{}.v) */ (uintptr)0);
    fmt.Println(wideSizeˢ, /* unsafe.Sizeof(Wide{}) */ (uintptr)8, vOffsetˢ, /* unsafe.Offsetof(Wide{}.v) */ (uintptr)0);
    fmt.Println(plainSizeˢ, /* unsafe.Sizeof(Plain{}) */ (uintptr)16, bOffsetˢ, /* unsafe.Offsetof(Plain{}.b) */ (uintptr)8);
    ref var raw = ref heap(new int32(), out var Ꮡraw);
    raw = 7;
    var view = Ꮡraw.Reinterpret<int32, Counter>();
    fmt.Println(viewReadsˢ, (~view).v);
    view.Value.v = 42;
    fmt.Println(writeThroughViewReachesˢ, raw);
    var c = new Counter(v: 3);
    var w = new Wide(v: 4);
    var p = new Plain(a: 1, b: 2);
    var m = new Managed(s: "managed"u8);
    fmt.Println(c.v, w.v, p.a, p.b, m.s);
    c = new Counter(nil);
    fmt.Println(clearedˢ, c.v);
}

} // end main_package
