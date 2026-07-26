namespace go;

using fmt = fmt_package;

partial class main_package {

public static readonly GoUntyped Two129 = /* 1 << 129 */
    GoUntyped.Parse("680564733841876926926749214863536422912");
internal static readonly GoUntyped below1e23 = /* 99999999999999974834176 */
    GoUntyped.Parse("99999999999999974834176");
internal static readonly GoUntyped above1e23 = /* 100000000000000008388608 */
    GoUntyped.Parse("100000000000000008388608");
internal static readonly UntypedInt small = 1000;

[GoType] partial struct ftoaTest {
    internal float64 f;
    internal byte fmtc;
    internal nint prec;
    internal @string s;
}

internal static slice<ftoaTest> ftoatests = new ftoaTest[]{
    new((float64)below1e23, (rune)'e', 17, "9.99999999999999748e+22"u8),
    new((float64)above1e23, (rune)'f', 17, "100000000000000008388608.00000000000000000"u8),
    new(small, (rune)'g', -1, "1000"u8)
}.slice();

internal static slice<float64> floats = new float64[]{(float64)below1e23, (float64)above1e23, small}.slice();

internal static array<float64> floatArr = new float64[]{(float64)below1e23, (float64)above1e23}.array();

internal static map<@string, float64> byName = new map<@string, float64>{["below"u8] = (float64)below1e23, ["above"u8] = (float64)above1e23, ["small"u8] = small};

internal static ftoaTest keyed = new ftoaTest(f: (float64)below1e23, fmtc: (rune)'g', prec: -1, s: "keyed"u8);

internal static slice<slice<float64>> nested = new slice<float64>[]{new float64[]{(float64)below1e23}.slice(), new float64[]{(float64)above1e23}.slice()}.slice();

internal static float32 asFloat32 = (float32)below1e23;

internal static float64 take(float64 f) {
    return f;
}

internal static float64 give() {
    return (float64)above1e23;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string belowˢ = "below"u8;
private static readonly @string aboveˢ = "above"u8;
private static readonly @string smallˢ = "small"u8;

internal static void Main() {
    float64 x = 1e40D;
    fmt.Println(x > (float64)Two129);
    fmt.Println(x < (float64)Two129);
    float64 y = 1e30D;
    fmt.Println(y >= (float64)Two129);
    fmt.Println(ftoatests[0].f, ftoatests[1].f, ftoatests[2].f);
    fmt.Println(floats[0], floats[1], floats[2]);
    fmt.Println(floatArr[0], floatArr[1]);
    fmt.Println(byName[belowˢ], byName[aboveˢ], byName[smallˢ]);
    fmt.Println(keyed.f, nested[0][0], nested[1][0], asFloat32);
    fmt.Println(take((float64)below1e23), give());
    float64 v = (float64)above1e23;
    v = (float64)below1e23;
    fmt.Println(v);
    var local = new float64[]{(float64)below1e23, small}.slice();
    fmt.Println(local[0], local[1]);
    var ch = new channel<float64>(1);
    ch.ᐸꟷ((float64)below1e23);
    fmt.Println(ᐸꟷ(ch));
    fmt.Println(take(small), (nint)(small * 2), (float64)((float64)small / 4D));
}

} // end main_package
