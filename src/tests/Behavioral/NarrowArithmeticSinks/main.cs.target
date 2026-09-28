global using I = object;

namespace go;

using fmt = fmt_package;
using ꓸꓸꓸany = Span<any>;

partial class main_package {
// Descriptor carrier for `I` — uninhabited; see GoDescriptorTypeAttribute.
[GoLocalName("I")] public interface Iᴅ { }


[GoType] partial struct holder {
    internal any v;
}

internal static int8 a = 100;
internal static uint8 u = 200;
internal static int16 w = 30000;
internal static uint16 z = 60000;
internal static byte c = (rune)'/';
internal static uint8 d = 250;
internal static int8 m8 = -128;
internal static int8 n1 = -1;

internal static @string show(any x) {
    return fmt.Sprintf("%v %T"u8, x, x);
}

internal static any ret(int8 a) {
    return (int8)(a + a);
}

internal static @string variadic(params ꓸꓸꓸany xsʗp) {
    var xs = xsʗp.sslice();

    return fmt.Sprint(xs.ꓸꓸꓸ);
}

internal static @string gen<T>(T x) {
    return fmt.Sprintf("%v %T"u8, x, x);
}

internal static void arm(@string @class, Func<@string> f) {
    GoFrame ᒐ = default;
    try {
        defer(() => {
            {
                var r = recover(); if (r != default!) {
                    fmt.Println(@class + ": panic:", r);
                }
            }
        }, ref ᒐ);
        fmt.Println(@class + ":", f());
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string fmtArgˢ = "fmt arg"u8;
private static readonly @string fmtTˢ = "fmt %T"u8;
private static readonly @string anyVarˢ = "any var"u8;
private static readonly @string anyParamˢ = "any param"u8;
private static readonly @string anyReturnˢ = "any return"u8;
private static readonly @string variadicAnyˢ = "variadic any"u8;
private static readonly @string compositeAnyˢ = "composite any"u8;
private static readonly @string chanAnyˢ = "chan any"u8;
private static readonly @string typeOnlyˢ = "type only"u8;
private static readonly @string namedAnyˢ = "named any"u8;
private static readonly @string genericˢ = "generic"u8;
private static readonly @string wideningˢ = "widening"u8;
private static readonly @string sliceIndexˢ = "slice index"u8;
private static readonly @string arrayIndexˢ = "array index"u8;
private static readonly @string stringIndexˢ = "string index"u8;
private static readonly @string sliceBoundˢ = "slice bound"u8;
private static readonly @string shiftCountˢ = "shift count"u8;
private static readonly @string divremOperandˢ = "divrem operand"u8;
private static readonly @string shrOperandˢ = "shr operand"u8;
private static readonly @string minint81ˢ = "minint8 / -1"u8;
private static readonly @string parenCompareˢ = "paren compare"u8;
private static readonly @string switchTagˢ = "switch tag"u8;
private static readonly @string case144ˢ = "case 144"u8;
private static readonly @string defaultˢ = "default"u8;
private static readonly @string caseValueˢ = "case value"u8;
private static readonly @string wrappedˢ = "wrapped"u8;
private static readonly @string unwrappedˢ = "unwrapped"u8;
private static readonly @string builtinArgˢ = "builtin arg"u8;
private static readonly @string parenDefineˢ = "paren define"u8;

internal static void Main() {
    arm(fmtArgˢ, () => fmt.Sprint((int8)(a + a), (@string)" "u8, (uint8)(u + u), (@string)" "u8, (int16)(w + w), (@string)" "u8, (uint16)(z + z)));
    arm(fmtTˢ, () => fmt.Sprintf("%T %T %T %T"u8, (int8)(a + a), (uint8)(u + u), (int8)(-a - a), ((uint8)(~u))));
    arm(anyVarˢ, () => {
        any x = (int8)(a + a);
        return show(x);
    });
    arm(anyParamˢ, () => show((uint8)(u * 2)) + " "u8 + show((uint8)(((uint8)0 - u))));
    arm(anyReturnˢ, () => show(ret(a)));
    arm(variadicAnyˢ, () => variadic((int8)(a + a), (uint8)(u + u)));
    arm(compositeAnyˢ, () => {
        var s = new any[]{(int8)(a + a), (uint8)(u + u)}.slice();
        var m = new map<@string, any>{["k"u8] = (int16)(w + w)};
        var h = new holder(v: (uint16)(z + z));
        return fmt.Sprint(s, m, h.v, ((any)((int8)(a + a))));
    });
    arm(chanAnyˢ, () => {
        var ch = new channel<any>(1);
        ch.ᐸꟷ((uint8)(u + u));
        return show(ᐸꟷ(ch));
    });
    arm(typeOnlyˢ, () => show((uint8)(u / 3)) + " "u8 + show((int8)(a % 7)) + " "u8 + show((uint8)((u >> (int)(1)))) + " "u8 + show((int8)(+a)) + " "u8 + show((int8)(~a)));
    arm(namedAnyˢ, () => {
        I i = (int8)(a + a);
        return show(i);
    });
    arm(genericˢ, () => gen((int8)(a + a)) + " "u8 + gen((uint8)(u + u)));
    arm(wideningˢ, () => fmt.Sprint((nint)((uint8)(u + u)), (@string)" "u8, (int64)((int8)(a + a)), (@string)" "u8, (float64)((int8)(a + a)), (@string)" "u8, (nint)((byte)(c - (rune)'0')), (@string)" "u8, (uint32)((uint16)(z + z)), (@string)" "u8, (int32)((int16)(w + w))));
    var tbl = new slice<nint>(256);
    foreach (var (k, _) in tbl) {
        tbl[k] = k;
    }
    ref var arr = ref heap<array<nint>>(out var Ꮡarr);
    arr = new array<nint>(256){[144] = 7};
    @string str = "0123456789"u8;
    var tblʗ1 = tbl;
    arm(sliceIndexˢ, () => fmt.Sprint(tblʗ1[(uint8)(u + u)]));
    var arrʗ1 = arr;
    arm(arrayIndexˢ, () => fmt.Sprint(arrʗ1[(uint8)(u + u)]));
    arm(stringIndexˢ, () => fmt.Sprint(str[(uint8)(d + 10)]));
    var tblʗ2 = tbl;
    arm(sliceBoundˢ, () => fmt.Sprint(len(tblʗ2[(int)((uint8)(u + u))..]), (@string)" "u8, len(tblʗ2[..(int)((uint8)(u + u))]), (@string)" "u8, str[(int)((uint8)(d + 10))..]));
    arm(shiftCountˢ, () => {
        var y = (uint32)1;
        y.LshAssign((uint64)((uint8)(d + 10)));
        return fmt.Sprint(((nint)1).Lsh((uint64)((uint8)(u + u - 140))), (@string)" "u8, ((uint32)1).Lsh((uint64)((uint8)(d + 10))), (@string)" "u8, y);
    });
    arm(divremOperandˢ, () => fmt.Sprint((int8)((int8)(a + a) / 2), (@string)" "u8, (int8)((int8)(a + a) % 7), (@string)" "u8, (uint8)((uint8)(u + u) / 3)));
    arm(shrOperandˢ, () => fmt.Sprint((int8)(((int8)(a + a) >> (int)(1))), (@string)" "u8, (uint8)(((uint8)(u + u) >> (int)(4)))));
    arm(minint81ˢ, () => fmt.Sprint((int8)(m8 / n1), (@string)" "u8, (int8)(m8 % n1)));
    arm(parenCompareˢ, () => fmt.Sprint((int8)(a + a) < 0, (@string)" "u8, (uint8)(u + u) == 144));
    arm(switchTagˢ, () => {
        switch ((uint8)(u + u)) {
        case 144: {
            return case144ˢ;
        }}

        return defaultˢ;
    });
    arm(caseValueˢ, () => {
        switch (ᐧ) {
        case {} when (uint8)(u + u) is < 150: {
            return wrappedˢ;
        }}

        return unwrappedˢ;
    });
    arm(builtinArgˢ, () => {
        var b = append(new byte[]{}.slice(), (byte)(u + u), (byte)(u * 2), (byte)(u - 1));
        return fmt.Sprint(min((int8)(a + a), 0), (@string)" "u8, max((uint8)(u + u), 0), (@string)" "u8, b);
    });
    arm(parenDefineˢ, () => {
        var t2 = (int8)(a + a);
        return show(t2);
    });
}

} // end main_package
