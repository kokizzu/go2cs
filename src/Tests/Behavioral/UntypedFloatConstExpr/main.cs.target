namespace go;

using fmt = fmt_package;
using Δmath = math_package;

partial class main_package {

public static readonly UntypedFloat MaxFloat32 = /* 0x1p127 * (1 + (1 - 0x1p-23)) */ 3.4028234663852886e+38;
public static readonly UntypedFloat SmallestNonzeroFloat32 = /* 0x1p-126 * 0x1p-23 */ 1.401298464324817e-45;
public static readonly UntypedFloat MaxFloat64 = /* 0x1p1023 * (1 + (1 - 0x1p-52)) */ 1.7976931348623157e+308;
public static readonly UntypedFloat SmallestNonzeroFloat64 = /* 0x1p-1022 * 0x1p-52 */ 5e-324;

public static readonly UntypedFloat Ln10 = 2.30258509299404568401799145468436420760110148862877297603332790;
public static readonly UntypedFloat Log10E = /* 1 / Ln10 */ 0.4342944819032518;
public static readonly UntypedFloat Pi = 3.14159265358979323846264338327950288419716939937510582097494459;
internal static readonly UntypedFloat twoPi = /* 2 * Pi */ 6.283185307179586;
internal static readonly UntypedFloat halfPi = /* Pi / 2 */ 1.5707963267948966;
internal static readonly UntypedFloat third = /* 1.0 / 3.0 */ 0.3333333333333333;

[GoType("num:float64")] partial struct MyFloat;

public static float64 Abs(this MyFloat f) {
    if (f < 0D) {
        return (float64)(-f);
    }
    return (float64)f;
}

internal static bool isInf(float64 f, nint sign) {
    return sign >= 0 && f > MaxFloat64 || sign <= 0 && f < -MaxFloat64;
}

internal static void bits64(@string label, uint64 got, uint64 want) {
    fmt.Println(label, got, got == want);
}

internal static void bits32(@string label, uint32 got, uint32 want) {
    fmt.Println(label, got, got == want);
}

internal static void Main() {
    fmt.Println((@string)"-- named float conversion --"u8);
    var f = ((MyFloat)(/* -math.Sqrt2 */ -1.4142135623730951D));
    fmt.Println(f.Abs());
    fmt.Println((@string)"-- float64 constant expressions (IEEE 754 bits, want-match) --"u8);
    bits64("MaxFloat64            "u8, Δmath.Float64bits(MaxFloat64), 0x7fefffffffffffffUL);
    bits64("SmallestNonzeroFloat64"u8, Δmath.Float64bits(SmallestNonzeroFloat64), 0x1);
    bits64("Ln10                  "u8, Δmath.Float64bits(Ln10), 0x40026bb1bbb55516UL);
    bits64("Log10E                "u8, Δmath.Float64bits(Log10E), 0x3fdbcb7b1526e50eUL);
    bits64("Pi                    "u8, Δmath.Float64bits(Pi), 0x400921fb54442d18UL);
    bits64("twoPi                 "u8, Δmath.Float64bits(twoPi), 0x401921fb54442d18UL);
    bits64("halfPi                "u8, Δmath.Float64bits(halfPi), 0x3ff921fb54442d18UL);
    bits64("third                 "u8, Δmath.Float64bits(third), 0x3fd5555555555555UL);
    fmt.Println((@string)"-- float32 constant expressions (IEEE 754 bits, want-match) --"u8);
    bits32("MaxFloat32            "u8, Δmath.Float32bits(MaxFloat32), 0x7f7fffff);
    bits32("SmallestNonzeroFloat32"u8, Δmath.Float32bits(SmallestNonzeroFloat32), 0x1);
    fmt.Println((@string)"-- exact-value identities --"u8);
    fmt.Println((@string)"MaxFloat64 == literal:"u8, (float64)MaxFloat64 == 1.7976931348623157e+308D);
    fmt.Println((@string)"SmallestNonzeroFloat64 == literal:"u8, (float64)SmallestNonzeroFloat64 == 5e-324D);
    fmt.Println((@string)"MaxFloat32 == literal:"u8, (float32)MaxFloat32 == 3.4028235e+38F);
    fmt.Println((@string)"Log10E == literal:"u8, (float64)Log10E == 0.4342944819032518D);
    fmt.Println((@string)"-- IsInf boundary --"u8);
    fmt.Println((@string)"isInf(MaxFloat64):"u8, isInf(1.7976931348623157e+308D, 1));
    fmt.Println((@string)"isInf(-MaxFloat64):"u8, isInf(-1.7976931348623157e+308D, -1));
    fmt.Println((@string)"isInf(truncated 1.79769e+308):"u8, isInf(1.79769e+308D, 1));
    fmt.Println((@string)"truncated < MaxFloat64:"u8, 1.79769e+308D < (float64)MaxFloat64);
}

} // end main_package
