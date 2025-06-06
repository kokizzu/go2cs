// Code generated by running "go generate" in golang.org/x/text. DO NOT EDIT.
// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.vendor.golang.org.x.net;

// This file implements the Punycode algorithm from RFC 3492.
using math = math_package;
using strings = strings_package;
using utf8 = unicode.utf8_package;
using unicode;

partial class idna_package {

// These parameter values are specified in section 5.
//
// All computation is done with int32s, so that overflow behavior is identical
// regardless of whether int is 32-bit or 64-bit.
internal const int32 @base = 36;

internal const int32 damp = 700;

internal const int32 initialBias = 72;

internal const int32 initialN = 128;

internal const int32 skew = 38;

internal const int32 tmax = 26;

internal const int32 tmin = 1;

internal static error punyError(@string s) {
    return new labelError(s, "A3");
}

// decode decodes a string as specified in section 6.2.
internal static (@string, error) decode(@string encoded) {
    if (encoded == ""u8) {
        return ("", default!);
    }
    nint pos = 1 + strings.LastIndex(encoded, "-"u8);
    if (pos == 1) {
        return ("", punyError(encoded));
    }
    if (pos == len(encoded)) {
        return (encoded[..(int)(len(encoded) - 1)], default!);
    }
    var output = new slice<rune>(0, len(encoded));
    if (pos != 0) {
        foreach (var (_, r) in encoded[..(int)(pos - 1)]) {
            output = append(output, r);
        }
    }
    var (i, n, bias) = (((int32)0), initialN, initialBias);
    var overflow = false;
    while (pos < len(encoded)) {
        var (oldI, w) = (i, ((int32)1));
        for (var k = @base; ᐧ ; k += @base) {
            if (pos == len(encoded)) {
                return ("", punyError(encoded));
            }
            var (digit, ok) = decodeDigit(encoded[pos]);
            if (!ok) {
                return ("", punyError(encoded));
            }
            pos++;
            (i, overflow) = madd(i, digit, w);
            if (overflow) {
                return ("", punyError(encoded));
            }
            var t = k - bias;
            if (k <= bias){
                t = tmin;
            } else 
            if (k >= bias + tmax) {
                t = tmax;
            }
            if (digit < t) {
                break;
            }
            (w, overflow) = madd(0, w, @base - t);
            if (overflow) {
                return ("", punyError(encoded));
            }
        }
        if (len(output) >= 1024) {
            return ("", punyError(encoded));
        }
        var x = ((int32)(len(output) + 1));
        bias = adapt(i - oldI, x, oldI == 0);
        n += i / x;
        i %= x;
        if (n < 0 || n > utf8.MaxRune) {
            return ("", punyError(encoded));
        }
        output = append(output, 0);
        copy(output[(int)(i + 1)..], output[(int)(i)..]);
        output[i] = n;
        i++;
    }
    return (((@string)output), default!);
}

// encode encodes a string as specified in section 6.3 and prepends prefix to
// the result.
//
// The "while h < length(input)" line in the specification becomes "for
// remaining != 0" in the Go code, because len(s) in Go is in bytes, not runes.
internal static (@string, error) encode(@string prefix, @string s) {
    var output = new slice<byte>(len(prefix), len(prefix) + 1 + 2 * len(s));
    copy(output, prefix);
    var (delta, n, bias) = (((int32)0), initialN, initialBias);
    var (b, remaining) = (((int32)0), ((int32)0));
    foreach (var (_, r) in s) {
        if (r < 128){
            b++;
            output = append(output, ((byte)r));
        } else {
            remaining++;
        }
    }
    var h = b;
    if (b > 0) {
        output = append(output, (rune)'-');
    }
    var overflow = false;
    while (remaining != 0) {
        var m = ((int32)2147483647);
        foreach (var (_, r) in s) {
            if (m > r && r >= n) {
                m = r;
            }
        }
        (delta, overflow) = madd(delta, m - n, h + 1);
        if (overflow) {
            return ("", punyError(s));
        }
        n = m;
        foreach (var (_, r) in s) {
            if (r < n) {
                delta++;
                if (delta < 0) {
                    return ("", punyError(s));
                }
                continue;
            }
            if (r > n) {
                continue;
            }
            var q = delta;
            for (var k = @base; ᐧ ; k += @base) {
                var t = k - bias;
                if (k <= bias){
                    t = tmin;
                } else 
                if (k >= bias + tmax) {
                    t = tmax;
                }
                if (q < t) {
                    break;
                }
                output = append(output, encodeDigit(t + (q - t) % (@base - t)));
                q = (q - t) / (@base - t);
            }
            output = append(output, encodeDigit(q));
            bias = adapt(delta, h + 1, h == b);
            delta = 0;
            h++;
            remaining--;
        }
        delta++;
        n++;
    }
    return (((@string)output), default!);
}

// madd computes a + (b * c), detecting overflow.
internal static (int32 next, bool overflow) madd(int32 a, int32 b, int32 c) {
    int32 next = default!;
    bool overflow = default!;

    var p = ((int64)b) * ((int64)c);
    if (p > math.MaxInt32 - ((int64)a)) {
        return (0, true);
    }
    return (a + ((int32)p), false);
}

internal static (int32 digit, bool ok) decodeDigit(byte x) {
    int32 digit = default!;
    bool ok = default!;

    switch (ᐧ) {
    case {} when (rune)'0' <= x && x <= (rune)'9': {
        return (((int32)(x - ((rune)'0' - 26))), true);
    }
    case {} when (rune)'A' <= x && x <= (rune)'Z': {
        return (((int32)(x - (rune)'A')), true);
    }
    case {} when (rune)'a' <= x && x <= (rune)'z': {
        return (((int32)(x - (rune)'a')), true);
    }}

    return (0, false);
}

internal static byte encodeDigit(int32 digit) {
    switch (ᐧ) {
    case {} when 0 <= digit && digit < 26: {
        return ((byte)(digit + (rune)'a'));
    }
    case {} when 26 <= digit && digit < 36: {
        return ((byte)(digit + ((rune)'0' - 26)));
    }}

    throw panic("idna: internal error in punycode encoding");
}

// adapt is the bias adaptation function specified in section 6.1.
internal static int32 adapt(int32 delta, int32 numPoints, bool firstTime) {
    if (firstTime){
        delta /= damp;
    } else {
        delta /= 2;
    }
    delta += delta / numPoints;
    var k = ((int32)0);
    while (delta > ((@base - tmin) * tmax) / 2) {
        delta /= @base - tmin;
        k += @base;
    }
    return k + (@base - tmin + 1) * delta / (delta + skew);
}

} // end idna_package
