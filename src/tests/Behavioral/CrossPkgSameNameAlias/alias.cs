global using Int32 = go.sync.atomic_package.Int32;

namespace go;

using atomic = sync.atomic_package;
using sync;

partial class atomic_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyncꓸatomic() {
    builtin.initPackage(typeof(sync.atomic_package));
}

public static ж<Int32> Wrap(ж<atomic.Int32> Ꮡv) {
    return Ꮡv;
}

} // end atomic_package
