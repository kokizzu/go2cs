namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("num:nuint")] partial struct idx;

[GoType("num:nint")] partial struct sidx;

[GoType("num:complex128")] partial struct cx;

internal static void Main() {
    nint n = 0;
    for (idx cΔ1 = ((idx)0); cΔ1 < ((idx)5); cΔ1++) {
        n++;
    }
    fmt.Println(n);
    nint m = 0;
    for (idx cΔ2 = ((idx)4); cΔ2 > ((idx)0); cΔ2--) {
        m++;
    }
    fmt.Println(m);
    sidx d = 3;
    d++;
    d++;
    d--;
    fmt.Println(d == ((sidx)4));
    var c = ((cx)complex(1D, 2D));
    c++;
    c--;
    var e = c * ((cx)complex(2D, 0D)) + ((cx)complex(0D, 1D));
    fmt.Println(c == ((cx)complex(1D, 2D)));
    fmt.Println(e / ((cx)complex(2D, 0D)) == ((cx)complex(1D, 2.5D)));
}

} // end main_package
