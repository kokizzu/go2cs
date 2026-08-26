namespace go;

using fmt = fmt_package;
using reader = NamedImportInitOrder.reader_package;
using NamedImportInitOrder;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸNamedImportInitOrderꓸreader() {
    builtin.initPackage(typeof(NamedImportInitOrder.reader_package));
}

internal static void Main() {
    fmt.Println(reader.Captured());
}

} // end main_package
