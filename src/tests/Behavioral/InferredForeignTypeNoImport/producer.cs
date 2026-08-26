namespace go;

using strings = strings_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸstrings() {
    builtin.initPackage(typeof(strings_package));
}

internal static ж<strings.Reader> makeReader() {
    return strings.NewReader("hi"u8);
}

} // end main_package
