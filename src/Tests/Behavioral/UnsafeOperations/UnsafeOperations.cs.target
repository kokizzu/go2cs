namespace go;

using fmt = fmt_package;
using @unsafe = unsafe_package;

partial class main_package {

[GoType] partial struct T1 {
    internal int32 a;
}

[GoType] partial struct T2 {
    internal int32 a;
}

public static uint64 Float64bits(float64 f) {
    return ~Ꮡ(f).Reinterpret<float64, uint64>();
}

public static float64 Float64frombits(uint64 b) {
    return ~Ꮡ(b).Reinterpret<uint64, float64>();
}

[GoType("dyn")] partial struct main_x {
    internal int64 a;
    internal bool b;
    internal @string c;
}

internal static void Main() {
    var b = new byte[]{}.slice();
    for (nint ch = 32; ch < 80; ch++) {
        b = append(b, ((@string)(rune)ch).ꓸꓸꓸ);
    }
    @string str = @unsafe.String(Ꮡ(b, 0), len(b));
    fmt.Println(str);
    var strptr = @unsafe.StringData(str);
    fmt.Println(@unsafe.String(strptr, len(str)));
    ref var arr = ref heap<array<nint>>(out var Ꮡarr);
    arr = new nint[]{1, 2, 3, 4}.array();
    var arrptr = Ꮡarr.at<nint>(0);
    @unsafe.Pointer nextPtr = (@unsafe.Pointer)((uintptr)new @unsafe.Pointer(arrptr) + @unsafe.Sizeof(arr[0]));
    fmt.Println((@string)"Value of the next element:", ~(ж<nint>)(uintptr)(nextPtr));
    ref var t1 = ref heap(new T1(), out var Ꮡt1);
    t1.a = 42;
    var t2 = ~Ꮡt1.Reinterpret<T1, T2>();
    fmt.Println((@string)"Value of t2.a:", t2.a);
    ref var i = ref heap(new int8(), out var Ꮡi);
    i = -1;
    int16 j = (int16)i;
    fmt.Println(i, j);
    uint8 k = ~Ꮡi.Reinterpret<int8, uint8>();
    fmt.Println(k);
    main_x x = default!;
    uintptr M = /* unsafe.Sizeof(x.c) */ 16;
    uintptr N = /* unsafe.Sizeof(x) */ 32;
    fmt.Println(M, N);
    fmt.Println(@unsafe.Alignof(x.GetType(), "a"));
    fmt.Println(@unsafe.Alignof(x.GetType(), "b"));
    fmt.Println(@unsafe.Alignof(x.GetType(), "c"));
    fmt.Println(@unsafe.Offsetof(x.GetType(), "a"));
    fmt.Println(@unsafe.Offsetof(x.GetType(), "b"));
    fmt.Println(@unsafe.Offsetof(x.GetType(), "c"));
    var i2 = Float64bits(9.5D);
    var f2 = Float64frombits(i2);
    fmt.Println(i2);
    fmt.Println(f2);
}

} // end main_package
