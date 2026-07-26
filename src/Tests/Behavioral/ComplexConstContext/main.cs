namespace go;

using fmt = fmt_package;

partial class main_package {

internal static UntypedFloat gHalfPi => 1.5707963267948966;

internal static UntypedComplex cRational => /* 5.5 + 1.5i */ 5.5D + 1.5D.i();
internal static UntypedComplex cNegImag => /* 2.25 - 0.75i */ 2.25D + -0.75D.i();
internal static UntypedComplex cPureImag => /* 3i */ 3D.i();
internal static UntypedComplex cWideEnough => /* 1.5e308 + 1.0e307i */ 1.5e+308D + 1e+307D.i();
internal static UntypedComplex cFolded => /* (1 + 2i) * (3 + 4i) */ -5D + 10D.i();

internal static complex64 c64 => /* 1.5 + 2.5i */ 1.5F + 2.5F.i();

internal static void showComplex(@string name, complex128 c) {
    fmt.Println(name, real(c), imag(c));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string cRationalˢ = "cRational"u8;
private static readonly @string cNegImagˢ = "cNegImag"u8;
private static readonly @string cPureImagˢ = "cPureImag"u8;
private static readonly @string cWideEnoughˢ = "cWideEnough"u8;
private static readonly @string cFoldedˢ = "cFolded"u8;
private static readonly object c64ˢ = (@string)"c64"u8;
private static readonly @string localˢ = "local"u8;

internal static void Main() {
    var huge = new complex128[]{34359738368D, 1766847064778384329583297500742918515827483896875618958121606201292619776D, -1329227995784915872903807060280344576D, 1357421751691302484408532992D}.slice();
    foreach (var (_, h) in huge) {
        fmt.Println(real(h));
    }
    var c = complex(0D, gHalfPi);
    fmt.Println(real(c), imag(c));
    float64 q = 7 / 2;
    var e = complex(7 / 2, 0D);
    fmt.Println(q, real(e), imag(e));
    fmt.Println((float64)(gHalfPi / 2D));
    float64 rf = 1.5D;
    var r = complex(rf, 0D);
    fmt.Println(real(r), imag(r));
    showComplex(cRationalˢ, cRational);
    showComplex(cNegImagˢ, cNegImag);
    showComplex(cPureImagˢ, cPureImag);
    showComplex(cWideEnoughˢ, cWideEnough);
    showComplex(cFoldedˢ, cFolded);
    fmt.Println(c64ˢ, real(c64), imag(c64));
    complex128 local = /* 0.5 - 0.25i */ 0.5D + -0.25D.i();
    showComplex(localˢ, local);
}

} // end main_package
