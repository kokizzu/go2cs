namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void Main() {
    channel<nint> c = new channel<nint>(100);
    for (nint i = 0; i < 10; i++) {
        var cʗ1 = c;
        goǃ(() => {
            for (nint j = 0; j < 10; j++) {
                cʗ1.ᐸꟷ(j);
            }
            close(cʗ1);
        });
    }
    foreach (var i in c) {
        fmt.Println(i);
    }
}

} // end main_package
