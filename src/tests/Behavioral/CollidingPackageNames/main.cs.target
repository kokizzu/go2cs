namespace go;

using fmt = fmt_package;
using dupmeta = collidea.dup_package;
using dup = collideb.dup_package;
using collideb;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    fmt.Println(dupmeta.Greeting());
    fmt.Println(dup.Marker());
}

} // end main_package
