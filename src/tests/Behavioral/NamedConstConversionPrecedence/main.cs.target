namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:float64")] partial struct rf;

[GoType("num:float32")] partial struct rf32;

[GoType("num:nint")] partial struct ri;

[GoType("num:complex64")] partial struct rc64;

[GoType("num:complex128")] partial struct rc128;

internal static rf divA = ((rf)(3 / 2));
internal static rf divB = ((rf)(7 / 2));
internal static rf divC = ((rf)(1 / 3));
internal static rf32 divD = ((rf32)(5 / 4));
internal static rf sumA = ((rf)(1.5D + 2.5D));
internal static ri mulA = ((ri)(3 * 4));
internal static ri subA = ((ri)(10 - 3));
internal static ri shfA = ((ri)((1 << (int)(4)) - 1));
internal static rc64 cA = ((rc64)(3F + 4F.i()));
internal static rc128 cB = ((rc128)(4D + -3D.i()));
internal static rc64 cC = ((rc64)(11F + 60F.i()));
internal static rc128 cD = ((rc128)(-11D + 70D.i()));
internal static ri negA = ((ri)(-5));
internal static ri notA = ((ri)~0);
internal static ri litA = ((ri)42);

internal static void Main() {
    fmt.Println(divA, divB, divC, divD);
    fmt.Println(sumA, mulA, subA, shfA);
    fmt.Println(cA, cB, cC, cD);
    fmt.Println(negA, notA, litA);
}

} // end main_package
