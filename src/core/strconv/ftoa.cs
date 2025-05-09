// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// Binary to decimal floating point conversion.
// Algorithm:
//   1) store mantissa in multiprecision decimal
//   2) shift decimal by exponent
//   3) read digits out & format
namespace go;

using math = math_package;

partial class strconv_package {

// TODO: move elsewhere?
[GoType] partial struct floatInfo {
    internal nuint mantbits;
    internal nuint expbits;
    internal nint bias;
}

internal static floatInfo float32info = new floatInfo(23, 8, -127);

internal static floatInfo float64info = new floatInfo(52, 11, -1023);

// FormatFloat converts the floating-point number f to a string,
// according to the format fmt and precision prec. It rounds the
// result assuming that the original was obtained from a floating-point
// value of bitSize bits (32 for float32, 64 for float64).
//
// The format fmt is one of
// 'b' (-ddddp±ddd, a binary exponent),
// 'e' (-d.dddde±dd, a decimal exponent),
// 'E' (-d.ddddE±dd, a decimal exponent),
// 'f' (-ddd.dddd, no exponent),
// 'g' ('e' for large exponents, 'f' otherwise),
// 'G' ('E' for large exponents, 'f' otherwise),
// 'x' (-0xd.ddddp±ddd, a hexadecimal fraction and binary exponent), or
// 'X' (-0Xd.ddddP±ddd, a hexadecimal fraction and binary exponent).
//
// The precision prec controls the number of digits (excluding the exponent)
// printed by the 'e', 'E', 'f', 'g', 'G', 'x', and 'X' formats.
// For 'e', 'E', 'f', 'x', and 'X', it is the number of digits after the decimal point.
// For 'g' and 'G' it is the maximum number of significant digits (trailing
// zeros are removed).
// The special precision -1 uses the smallest number of digits
// necessary such that ParseFloat will return f exactly.
public static @string FormatFloat(float64 f, byte fmt, nint prec, nint bitSize) {
    return ((@string)genericFtoa(new slice<byte>(0, max(prec + 4, 24)), f, fmt, prec, bitSize));
}

// AppendFloat appends the string form of the floating-point number f,
// as generated by [FormatFloat], to dst and returns the extended buffer.
public static slice<byte> AppendFloat(slice<byte> dst, float64 f, byte fmt, nint prec, nint bitSize) {
    return genericFtoa(dst, f, fmt, prec, bitSize);
}

internal static slice<byte> genericFtoa(slice<byte> dst, float64 val, byte fmt, nint prec, nint bitSize) {
    uint64 bits = default!;
    ж<floatInfo> flt = default!;
    switch (bitSize) {
    case 32: {
        bits = ((uint64)math.Float32bits(((float32)val)));
        flt = Ꮡ(float32info);
        break;
    }
    case 64: {
        bits = math.Float64bits(val);
        flt = Ꮡ(float64info);
        break;
    }
    default: {
        throw panic("strconv: illegal AppendFloat/FormatFloat bitSize");
        break;
    }}

    var neg = bits >> (int)(((~flt).expbits + (~flt).mantbits)) != 0;
    nint exp = (nint)(((nint)(bits >> (int)((~flt).mantbits))) & (1 << (int)((~flt).expbits) - 1));
    var mant = (uint64)(bits & (((uint64)1) << (int)((~flt).mantbits) - 1));
    switch (exp) {
    case 1 << (int)((~flt).expbits) - 1: {
        // Inf, NaN
        @string s = default!;
        switch (ᐧ) {
        case {} when mant is != 0: {
            s = "NaN"u8;
            break;
        }
        case {} when neg: {
            s = "-Inf"u8;
            break;
        }
        default: {
            s = "+Inf"u8;
            break;
        }}

        return append(dst, s.ꓸꓸꓸ);
    }
    case 0: {
        exp++;
        break;
    }
    default: {
        mant |= (uint64)(((uint64)1) << (int)((~flt).mantbits));
        break;
    }}

    // denormalized
    // add implicit top bit
    exp += flt.val.bias;
    // Pick off easy binary, hex formats.
    if (fmt == (rune)'b') {
        return fmtB(dst, neg, mant, exp, flt);
    }
    if (fmt == (rune)'x' || fmt == (rune)'X') {
        return fmtX(dst, prec, fmt, neg, mant, exp, flt);
    }
    if (!optimize) {
        return bigFtoa(dst, prec, fmt, neg, mant, exp, flt);
    }
    ref var digs = ref heap(new decimalSlice(), out var Ꮡdigs);
    var ok = false;
    // Negative precision means "only as much as needed to be exact."
    var shortest = prec < 0;
    if (shortest){
        // Use Ryu algorithm.
        array<byte> bufΔ1 = new(32);
        digs.d = bufΔ1[..];
        ryuFtoaShortest(Ꮡdigs, mant, exp - ((nint)(~flt).mantbits), flt);
        ok = true;
        // Precision for shortest representation mode.
        switch (fmt) {
        case (rune)'e' or (rune)'E': {
            prec = max(digs.nd - 1, 0);
            break;
        }
        case (rune)'f': {
            prec = max(digs.nd - digs.dp, 0);
            break;
        }
        case (rune)'g' or (rune)'G': {
            prec = digs.nd;
            break;
        }}

    } else 
    if (fmt != (rune)'f') {
        // Fixed number of digits.
        nint digits = prec;
        switch (fmt) {
        case (rune)'e' or (rune)'E': {
            digits++;
            break;
        }
        case (rune)'g' or (rune)'G': {
            if (prec == 0) {
                prec = 1;
            }
            digits = prec;
            break;
        }
        default: {
            digits = 1;
            break;
        }}

        // Invalid mode.
        array<byte> buf = new(24);
        if (bitSize == 32 && digits <= 9){
            digs.d = buf[..];
            ryuFtoaFixed32(Ꮡdigs, ((uint32)mant), exp - ((nint)(~flt).mantbits), digits);
            ok = true;
        } else 
        if (digits <= 18) {
            digs.d = buf[..];
            ryuFtoaFixed64(Ꮡdigs, mant, exp - ((nint)(~flt).mantbits), digits);
            ok = true;
        }
    }
    if (!ok) {
        return bigFtoa(dst, prec, fmt, neg, mant, exp, flt);
    }
    return formatDigits(dst, shortest, neg, digs, prec, fmt);
}

// bigFtoa uses multiprecision computations to format a float.
internal static slice<byte> bigFtoa(slice<byte> dst, nint prec, byte fmt, bool neg, uint64 mant, nint exp, ж<floatInfo> Ꮡflt) {
    ref var flt = ref Ꮡflt.val;

    var d = @new<@decimal>();
    d.Assign(mant);
    d.Shift(exp - ((nint)flt.mantbits));
    decimalSlice digs = default!;
    var shortest = prec < 0;
    if (shortest){
        roundShortest(d, mant, exp, Ꮡflt);
        digs = new decimalSlice(d: (~d).d[..], nd: (~d).nd, dp: (~d).dp);
        // Precision for shortest representation mode.
        switch (fmt) {
        case (rune)'e' or (rune)'E': {
            prec = digs.nd - 1;
            break;
        }
        case (rune)'f': {
            prec = max(digs.nd - digs.dp, 0);
            break;
        }
        case (rune)'g' or (rune)'G': {
            prec = digs.nd;
            break;
        }}

    } else {
        // Round appropriately.
        switch (fmt) {
        case (rune)'e' or (rune)'E': {
            d.Round(prec + 1);
            break;
        }
        case (rune)'f': {
            d.Round((~d).dp + prec);
            break;
        }
        case (rune)'g' or (rune)'G': {
            if (prec == 0) {
                prec = 1;
            }
            d.Round(prec);
            break;
        }}

        digs = new decimalSlice(d: (~d).d[..], nd: (~d).nd, dp: (~d).dp);
    }
    return formatDigits(dst, shortest, neg, digs, prec, fmt);
}

internal static slice<byte> formatDigits(slice<byte> dst, bool shortest, bool neg, decimalSlice digs, nint prec, byte fmt) {
    switch (fmt) {
    case (rune)'e' or (rune)'E': {
        return fmtE(dst, neg, digs, prec, fmt);
    }
    case (rune)'f': {
        return fmtF(dst, neg, digs, prec);
    }
    case (rune)'g' or (rune)'G': {
        nint eprec = prec;
        if (eprec > digs.nd && digs.nd >= digs.dp) {
            // trailing fractional zeros in 'e' form will be trimmed.
            eprec = digs.nd;
        }
        if (shortest) {
            // %e is used if the exponent from the conversion
            // is less than -4 or greater than or equal to the precision.
            // if precision was the shortest possible, use precision 6 for this decision.
            eprec = 6;
        }
        nint exp = digs.dp - 1;
        if (exp < -4 || exp >= eprec) {
            if (prec > digs.nd) {
                prec = digs.nd;
            }
            return fmtE(dst, neg, digs, prec - 1, fmt + (rune)'e' - (rune)'g');
        }
        if (prec > digs.dp) {
            prec = digs.nd;
        }
        return fmtF(dst, neg, digs, max(prec - digs.dp, 0));
    }}

    // unknown format
    return append(dst, (rune)'%', fmt);
}

// roundShortest rounds d (= mant * 2^exp) to the shortest number of digits
// that will let the original floating point value be precisely reconstructed.
internal static void roundShortest(ж<@decimal> Ꮡd, uint64 mant, nint exp, ж<floatInfo> Ꮡflt) {
    ref var d = ref Ꮡd.val;
    ref var flt = ref Ꮡflt.val;

    // If mantissa is zero, the number is zero; stop now.
    if (mant == 0) {
        d.nd = 0;
        return;
    }
    // Compute upper and lower such that any decimal number
    // between upper and lower (possibly inclusive)
    // will round to the original floating point number.
    // We may see at once that the number is already shortest.
    //
    // Suppose d is not denormal, so that 2^exp <= d < 10^dp.
    // The closest shorter number is at least 10^(dp-nd) away.
    // The lower/upper bounds computed below are at distance
    // at most 2^(exp-mantbits).
    //
    // So the number is already shortest if 10^(dp-nd) > 2^(exp-mantbits),
    // or equivalently log2(10)*(dp-nd) > exp-mantbits.
    // It is true if 332/100*(dp-nd) >= exp-mantbits (log2(10) > 3.32).
    nint minexp = flt.bias + 1;
    // minimum possible exponent
    if (exp > minexp && 332 * (d.dp - d.nd) >= 100 * (exp - ((nint)flt.mantbits))) {
        // The number is already shortest.
        return;
    }
    // d = mant << (exp - mantbits)
    // Next highest floating point number is mant+1 << exp-mantbits.
    // Our upper bound is halfway between, mant*2+1 << exp-mantbits-1.
    var upper = @new<@decimal>();
    upper.Assign(mant * 2 + 1);
    upper.Shift(exp - ((nint)flt.mantbits) - 1);
    // d = mant << (exp - mantbits)
    // Next lowest floating point number is mant-1 << exp-mantbits,
    // unless mant-1 drops the significant bit and exp is not the minimum exp,
    // in which case the next lowest is mant*2-1 << exp-mantbits-1.
    // Either way, call it mantlo << explo-mantbits.
    // Our lower bound is halfway between, mantlo*2+1 << explo-mantbits-1.
    uint64 mantlo = default!;
    nint explo = default!;
    if (mant > 1 << (int)(flt.mantbits) || exp == minexp){
        mantlo = mant - 1;
        explo = exp;
    } else {
        mantlo = mant * 2 - 1;
        explo = exp - 1;
    }
    var lower = @new<@decimal>();
    lower.Assign(mantlo * 2 + 1);
    lower.Shift(explo - ((nint)flt.mantbits) - 1);
    // The upper and lower bounds are possible outputs only if
    // the original mantissa is even, so that IEEE round-to-even
    // would round to the original mantissa and not the neighbors.
    var inclusive = mant % 2 == 0;
    // As we walk the digits we want to know whether rounding up would fall
    // within the upper bound. This is tracked by upperdelta:
    //
    // If upperdelta == 0, the digits of d and upper are the same so far.
    //
    // If upperdelta == 1, we saw a difference of 1 between d and upper on a
    // previous digit and subsequently only 9s for d and 0s for upper.
    // (Thus rounding up may fall outside the bound, if it is exclusive.)
    //
    // If upperdelta == 2, then the difference is greater than 1
    // and we know that rounding up falls within the bound.
    uint8 upperdelta = default!;
    // Now we can figure out the minimum number of digits required.
    // Walk along until d has distinguished itself from upper and lower.
    for (nint ui = 0; ᐧ ; ui++) {
        // lower, d, and upper may have the decimal points at different
        // places. In this case upper is the longest, so we iterate from
        // ui==0 and start li and mi at (possibly) -1.
        nint mi = ui - (~upper).dp + d.dp;
        if (mi >= d.nd) {
            break;
        }
        nint li = ui - (~upper).dp + (~lower).dp;
        var l = ((byte)(rune)'0');
        // lower digit
        if (li >= 0 && li < (~lower).nd) {
            l = (~lower).d[li];
        }
        var m = ((byte)(rune)'0');
        // middle digit
        if (mi >= 0) {
            m = d.d[mi];
        }
        var u = ((byte)(rune)'0');
        // upper digit
        if (ui < (~upper).nd) {
            u = (~upper).d[ui];
        }
        // Okay to round down (truncate) if lower has a different digit
        // or if lower is inclusive and is exactly the result of rounding
        // down (i.e., and we have reached the final digit of lower).
        var okdown = l != m || inclusive && li + 1 == (~lower).nd;
        switch (ᐧ) {
        case {} when upperdelta == 0 && m + 1 < u: {
            upperdelta = 2;
            break;
        }
        case {} when upperdelta == 0 && m != u: {
            upperdelta = 1;
            break;
        }
        case {} when upperdelta == 1 && (m != (rune)'9' || u != (rune)'0'): {
            upperdelta = 2;
            break;
        }}

        // Example:
        // m = 12345xxx
        // u = 12347xxx
        // Example:
        // m = 12345xxx
        // u = 12346xxx
        // Example:
        // m = 1234598x
        // u = 1234600x
        // Okay to round up if upper has a different digit and either upper
        // is inclusive or upper is bigger than the result of rounding up.
        var okup = upperdelta > 0 && (inclusive || upperdelta > 1 || ui + 1 < (~upper).nd);
        // If it's okay to do either, then round to the nearest one.
        // If it's okay to do only one, do it.
        switch (ᐧ) {
        case {} when okdown && okup: {
            d.Round(mi + 1);
            return;
        }
        case {} when okdown: {
            d.RoundDown(mi + 1);
            return;
        }
        case {} when okup: {
            d.RoundUp(mi + 1);
            return;
        }}

    }
}

[GoType] partial struct decimalSlice {
    internal slice<byte> d;
    internal nint nd;
    internal nint dp;
}

// %e: -d.ddddde±dd
internal static slice<byte> fmtE(slice<byte> dst, bool neg, decimalSlice d, nint prec, byte fmt) {
    // sign
    if (neg) {
        dst = append(dst, (rune)'-');
    }
    // first digit
    var ch = ((byte)(rune)'0');
    if (d.nd != 0) {
        ch = d.d[0];
    }
    dst = append(dst, ch);
    // .moredigits
    if (prec > 0) {
        dst = append(dst, (rune)'.');
        nint i = 1;
        nint m = min(d.nd, prec + 1);
        if (i < m) {
            dst = append(dst, d.d[(int)(i)..(int)(m)].ꓸꓸꓸ);
            i = m;
        }
        for (; i <= prec; i++) {
            dst = append(dst, (rune)'0');
        }
    }
    // e±
    dst = append(dst, fmt);
    nint exp = d.dp - 1;
    if (d.nd == 0) {
        // special case: 0 has exponent 0
        exp = 0;
    }
    if (exp < 0){
        ch = (rune)'-';
        exp = -exp;
    } else {
        ch = (rune)'+';
    }
    dst = append(dst, ch);
    // dd or ddd
    switch (ᐧ) {
    case {} when exp is < 10: {
        dst = append(dst, (rune)'0', ((byte)exp) + (rune)'0');
        break;
    }
    case {} when exp is < 100: {
        dst = append(dst, ((byte)(exp / 10)) + (rune)'0', ((byte)(exp % 10)) + (rune)'0');
        break;
    }
    default: {
        dst = append(dst, ((byte)(exp / 100)) + (rune)'0', ((byte)(exp / 10)) % 10 + (rune)'0', ((byte)(exp % 10)) + (rune)'0');
        break;
    }}

    return dst;
}

// %f: -ddddddd.ddddd
internal static slice<byte> fmtF(slice<byte> dst, bool neg, decimalSlice d, nint prec) {
    // sign
    if (neg) {
        dst = append(dst, (rune)'-');
    }
    // integer, padded with zeros as needed.
    if (d.dp > 0){
        nint m = min(d.nd, d.dp);
        dst = append(dst, d.d[..(int)(m)].ꓸꓸꓸ);
        for (; m < d.dp; m++) {
            dst = append(dst, (rune)'0');
        }
    } else {
        dst = append(dst, (rune)'0');
    }
    // fraction
    if (prec > 0) {
        dst = append(dst, (rune)'.');
        for (nint i = 0; i < prec; i++) {
            var ch = ((byte)(rune)'0');
            {
                nint j = d.dp + i; if (0 <= j && j < d.nd) {
                    ch = d.d[j];
                }
            }
            dst = append(dst, ch);
        }
    }
    return dst;
}

// %b: -ddddddddp±ddd
internal static slice<byte> fmtB(slice<byte> dst, bool neg, uint64 mant, nint exp, ж<floatInfo> Ꮡflt) {
    ref var flt = ref Ꮡflt.val;

    // sign
    if (neg) {
        dst = append(dst, (rune)'-');
    }
    // mantissa
    (dst, _) = formatBits(dst, mant, 10, false, true);
    // p
    dst = append(dst, (rune)'p');
    // ±exponent
    exp -= ((nint)flt.mantbits);
    if (exp >= 0) {
        dst = append(dst, (rune)'+');
    }
    (dst, _) = formatBits(dst, ((uint64)exp), 10, exp < 0, true);
    return dst;
}

// %x: -0x1.yyyyyyyyp±ddd or -0x0p+0. (y is hex digit, d is decimal digit)
internal static slice<byte> fmtX(slice<byte> dst, nint prec, byte fmt, bool neg, uint64 mant, nint exp, ж<floatInfo> Ꮡflt) {
    ref var flt = ref Ꮡflt.val;

    if (mant == 0) {
        exp = 0;
    }
    // Shift digits so leading 1 (if any) is at bit 1<<60.
    mant <<= (nuint)(60 - flt.mantbits);
    while (mant != 0 && (uint64)(mant & (1 << (int)(60))) == 0) {
        mant <<= (UntypedInt)(1);
        exp--;
    }
    // Round if requested.
    if (prec >= 0 && prec < 15) {
        nuint shift = ((nuint)(prec * 4));
        var extra = (uint64)((mant << (int)(shift)) & (1 << (int)(60) - 1));
        mant >>= (nuint)(60 - shift);
        if ((uint64)(extra | ((uint64)(mant & 1))) > 1 << (int)(59)) {
            mant++;
        }
        mant <<= (nuint)(60 - shift);
        if ((uint64)(mant & (1 << (int)(61))) != 0) {
            // Wrapped around.
            mant >>= (UntypedInt)(1);
            exp++;
        }
    }
    @string hex = lowerhex;
    if (fmt == (rune)'X') {
        hex = upperhex;
    }
    // sign, 0x, leading digit
    if (neg) {
        dst = append(dst, (rune)'-');
    }
    dst = append(dst, (rune)'0', fmt, (rune)'0' + ((byte)((uint64)((mant >> (int)(60)) & 1))));
    // .fraction
    mant <<= (UntypedInt)(4);
    // remove leading 0 or 1
    if (prec < 0 && mant != 0){
        dst = append(dst, (rune)'.');
        while (mant != 0) {
            dst = append(dst, hex[(uint64)((mant >> (int)(60)) & 15)]);
            mant <<= (UntypedInt)(4);
        }
    } else 
    if (prec > 0) {
        dst = append(dst, (rune)'.');
        for (nint i = 0; i < prec; i++) {
            dst = append(dst, hex[(uint64)((mant >> (int)(60)) & 15)]);
            mant <<= (UntypedInt)(4);
        }
    }
    // p±
    var ch = ((byte)(rune)'P');
    if (fmt == lower(fmt)) {
        ch = (rune)'p';
    }
    dst = append(dst, ch);
    if (exp < 0){
        ch = (rune)'-';
        exp = -exp;
    } else {
        ch = (rune)'+';
    }
    dst = append(dst, ch);
    // dd or ddd or dddd
    switch (ᐧ) {
    case {} when exp is < 100: {
        dst = append(dst, ((byte)(exp / 10)) + (rune)'0', ((byte)(exp % 10)) + (rune)'0');
        break;
    }
    case {} when exp is < 1000: {
        dst = append(dst, ((byte)(exp / 100)) + (rune)'0', ((byte)((exp / 10) % 10)) + (rune)'0', ((byte)(exp % 10)) + (rune)'0');
        break;
    }
    default: {
        dst = append(dst, ((byte)(exp / 1000)) + (rune)'0', ((byte)(exp / 100)) % 10 + (rune)'0', ((byte)((exp / 10) % 10)) + (rune)'0', ((byte)(exp % 10)) + (rune)'0');
        break;
    }}

    return dst;
}

} // end strconv_package
