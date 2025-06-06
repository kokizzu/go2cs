// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

partial class math_package {

/*
	Floating-point logarithm.
*/
// The original C code, the long comment, and the constants
// below are from FreeBSD's /usr/src/lib/msun/src/e_log.c
// and came with this notice. The go code is a simpler
// version of the original C.
//
// ====================================================
// Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved.
//
// Developed at SunPro, a Sun Microsystems, Inc. business.
// Permission to use, copy, modify, and distribute this
// software is freely granted, provided that this notice
// is preserved.
// ====================================================
//
// __ieee754_log(x)
// Return the logarithm of x
//
// Method :
//   1. Argument Reduction: find k and f such that
//			x = 2**k * (1+f),
//	   where  sqrt(2)/2 < 1+f < sqrt(2) .
//
//   2. Approximation of log(1+f).
//	Let s = f/(2+f) ; based on log(1+f) = log(1+s) - log(1-s)
//		 = 2s + 2/3 s**3 + 2/5 s**5 + .....,
//	     	 = 2s + s*R
//      We use a special Reme algorithm on [0,0.1716] to generate
//	a polynomial of degree 14 to approximate R.  The maximum error
//	of this polynomial approximation is bounded by 2**-58.45. In
//	other words,
//		        2      4      6      8      10      12      14
//	    R(z) ~ L1*s +L2*s +L3*s +L4*s +L5*s  +L6*s  +L7*s
//	(the values of L1 to L7 are listed in the program) and
//	    |      2          14          |     -58.45
//	    | L1*s +...+L7*s    -  R(z) | <= 2
//	    |                             |
//	Note that 2s = f - s*f = f - hfsq + s*hfsq, where hfsq = f*f/2.
//	In order to guarantee error in log below 1ulp, we compute log by
//		log(1+f) = f - s*(f - R)		(if f is not too large)
//		log(1+f) = f - (hfsq - s*(hfsq+R)).	(better accuracy)
//
//	3. Finally,  log(x) = k*Ln2 + log(1+f).
//			    = k*Ln2_hi+(f-(hfsq-(s*(hfsq+R)+k*Ln2_lo)))
//	   Here Ln2 is split into two floating point number:
//			Ln2_hi + Ln2_lo,
//	   where n*Ln2_hi is always exact for |n| < 2000.
//
// Special cases:
//	log(x) is NaN with signal if x < 0 (including -INF) ;
//	log(+INF) is +INF; log(0) is -INF with signal;
//	log(NaN) is that NaN with no signal.
//
// Accuracy:
//	according to an error analysis, the error is always less than
//	1 ulp (unit in the last place).
//
// Constants:
// The hexadecimal values are the intended ones for the following
// constants. The decimal values may be used, provided that the
// compiler will convert from decimal to binary accurately enough
// to produce the hexadecimal values shown.

// Log returns the natural logarithm of x.
//
// Special cases are:
//
//	Log(+Inf) = +Inf
//	Log(0) = -Inf
//	Log(x < 0) = NaN
//	Log(NaN) = NaN
public static float64 Log(float64 x) {
    if (haveArchLog) {
        return archLog(x);
    }
    return log(x);
}

internal static float64 log(float64 x) {
    static readonly UntypedFloat Ln2Hi = /* 6.93147180369123816490e-01 */ 0.693147;    /* 3fe62e42 fee00000 */
    static readonly UntypedFloat Ln2Lo = /* 1.90821492927058770002e-10 */ 1.90821e-10; /* 3dea39ef 35793c76 */
    static readonly UntypedFloat L1 = /* 6.666666666666735130e-01 */ 0.666667;    /* 3FE55555 55555593 */
    static readonly UntypedFloat L2 = /* 3.999999999940941908e-01 */ 0.4;         /* 3FD99999 9997FA04 */
    static readonly UntypedFloat L3 = /* 2.857142874366239149e-01 */ 0.285714;    /* 3FD24924 94229359 */
    static readonly UntypedFloat L4 = /* 2.222219843214978396e-01 */ 0.222222;    /* 3FCC71C5 1D8E78AF */
    static readonly UntypedFloat L5 = /* 1.818357216161805012e-01 */ 0.181836;    /* 3FC74664 96CB03DE */
    static readonly UntypedFloat L6 = /* 1.531383769920937332e-01 */ 0.153138;    /* 3FC39A09 D078C69F */
    static readonly UntypedFloat L7 = /* 1.479819860511658591e-01 */ 0.147982;    /* 3FC2F112 DF3E5244 */
    // special cases
    switch (ᐧ) {
    case {} when IsNaN(x) || IsInf(x, 1): {
        return x;
    }
    case {} when x is < 0: {
        return NaN();
    }
    case {} when x is 0: {
        return Inf(-1);
    }}

    // reduce
    var (f1, ki) = Frexp(x);
    if (f1 < Sqrt2 / 2) {
        f1 *= 2;
        ki--;
    }
    var f = f1 - 1;
    var k = ((float64)ki);
    // compute
    var s = f / (2 + f);
    var s2 = s * s;
    var s4 = s2 * s2;
    var t1 = s2 * (L1 + s4 * (L3 + s4 * (L5 + s4 * L7)));
    var t2 = s4 * (L2 + s4 * (L4 + s4 * L6));
    var R = t1 + t2;
    var hfsq = 0.5F * f * f;
    return k * Ln2Hi - ((hfsq - (s * (hfsq + R) + k * Ln2Lo)) - f);
}

} // end math_package
