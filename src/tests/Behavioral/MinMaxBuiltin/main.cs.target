[assembly: go.GoPositionMap("main.go", "main.cs", "AAxGgKaAAAgUgqaEgoaChoaChoKGgoKChoKEgoiCgoKGgoKGgoI=")]

namespace go;

using fmt = fmt_package;

partial class main_package {

internal static UntypedInt limit => /* 128 << 10 */ 131072;

internal static UntypedInt floor => 16;

internal static uintptr clampU(uintptr n) {
    return min(n, (uintptr)(limit));
}

internal static int32 clampI(int32 d) {
    return max(d, (int32)(floor));
}

[GoType("num:uint16")] partial struct fieldElement;

[GoType("num:float64")] partial struct ratio;

[GoType("num:int8")] partial struct delta;

internal static fieldElement spread(fieldElement a, fieldElement b) {
    return min((fieldElement)(a - b), (fieldElement)(b - a), (fieldElement)(a - b + 3329), (fieldElement)(b - a + 3329));
}

internal static void Main() {
    fmt.Println(min(3, 7));
    fmt.Println(max(3, 7));
    fmt.Println(min(5, 2, 9, 1, 4));
    fmt.Println(max(5, 2, 9, 1, 4));
    fmt.Println(min(42));
    fmt.Println(min(2.5D, 1.5D));
    fmt.Println(max(2.5D, 1.5D));
    fmt.Println(min("banana", "apple", "cherry"));
    fmt.Println(max("banana", "apple", "cherry"));
    var x = new byte[]{1, 2, 3}.slice();
    var y = new byte[]{1, 2, 3, 4, 5}.slice();
    nint n = min(len(x), len(y));
    fmt.Println(n);
    fmt.Println(clampU(999999), clampU(7));
    fmt.Println(clampI(3), clampI(100));
    uintptr big = 200000;
    fmt.Println(min(big, (uintptr)(limit), (uintptr)(500)));
    var (a, b, c, d) = (((fieldElement)10), ((fieldElement)3329), ((fieldElement)7), ((fieldElement)500));
    fmt.Println(min(a, b, c, d), max(a, b, c, d));
    fmt.Println(min(a, c), max(a, c));
    fmt.Println(spread(10, 3));
    var (p, q, r) = (((ratio)2.5D), ((ratio)(-1.25D)), ((ratio)8D));
    fmt.Println(min(p, q, r), max(p, q, r));
    fmt.Println(min(p, q), max(p, q));
    var (i, j, k) = (((delta)(-5)), ((delta)3), ((delta)(-100)));
    fmt.Println(min(i, j, k), max(i, j, k));
    fmt.Println(min(i, j), max(i, j));
}

} // end main_package
