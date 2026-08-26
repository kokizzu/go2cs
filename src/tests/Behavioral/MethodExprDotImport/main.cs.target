namespace go;

using fmt = fmt_package;
using static MethodExprDotImport.mep_package;
using mep = MethodExprDotImport.mep_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    var readers = new Func<ж<mep.Reader>, byte, (@string, error)>[]{
        (Func<ж<mep.Reader>, byte, (@string, error)>)(mep.Read),
        (Func<ж<mep.Reader>, byte, (@string, error)>)(mep.Peek)
    }.slice();
    var r = Ꮡ(new Reader(Name: "go2cs"u8));
    foreach (var (_, read) in readers) {
        var (s, _) = read(r, (rune)',');
        fmt.Println(s);
    }
}

} // end main_package
