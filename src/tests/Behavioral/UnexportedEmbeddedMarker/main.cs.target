[assembly: go.GoPositionMap("main.go", "main.cs", "AAsSogAHFoCigOSCgoKChoI=")]

namespace go;

using fmt = fmt_package;
using System.Runtime.InteropServices;

partial class main_package {

[GoType] partial struct noCopy {
}

[GoRecv] internal static void Lock(this ref noCopy _) {
}

[GoRecv] internal static void Unlock(this ref noCopy _) {
}

[GoType] [StructLayout(LayoutKind.Explicit, Size = 8)] partial struct Counter {
    [FieldOffset(0)] internal readonly noCopy _;
    [FieldOffset(0)] internal int64 v;
}

[GoRecv] public static void Add(this ref Counter c, int64 n) {
    c.v += n;
}

[GoRecv] public static int64 Value(this ref Counter c) {
    return c.v;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object counterˢ = (@string)"counter:"u8;
private static readonly object zeroˢ = (@string)"zero:"u8;

internal static void Main() {
    Counter c = default!;
    c.Add(5);
    c.Add(3);
    fmt.Println(counterˢ, c.Value());
    Counter d = default!;
    fmt.Println(zeroˢ, d.Value());
}

} // end main_package
