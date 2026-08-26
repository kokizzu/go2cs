namespace go;

using fmt = fmt_package;
using ꓸꓸꓸnint = Span<nint>;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static nint sum(params ꓸꓸꓸnint numsʗp) {
    var nums = numsʗp.sslice();

    nint total = 0;
    foreach (var (_, n) in nums) {
        total += n;
    }
    return total;
}

internal static void Main() {
    var values = new nint[]{1, 2, 3}.slice();
    fmt.Println(sum(values.ꓸꓸꓸ));
    fmt.Println(sum(1, 2, 3));
}

} // end main_package
