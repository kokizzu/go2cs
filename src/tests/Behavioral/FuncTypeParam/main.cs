namespace go;

using fmt = fmt_package;
using atomic = sync.atomic_package;
using sync;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyncꓸatomic() {
    builtin.initPackage(typeof(sync.atomic_package));
}

internal static nint apply(ж<atomic.Int32> Ꮡv, Func<ж<atomic.Int32>, nint> f) {
    return f(Ꮡv);
}

internal static void consume(ж<atomic.Int32> Ꮡv, Action<ж<atomic.Int32>> sink) {
    sink(Ꮡv);
}

[GoType] partial struct runner {
    internal Func<ж<atomic.Int32>, nint> gen;
}

internal static nint invoke(this runner r, ж<atomic.Int32> Ꮡv) {
    return r.gen(Ꮡv);
}

internal static void Main() {
    ref var v = ref heap(new atomic.Int32(), out var Ꮡv);
    Ꮡv.Store(10);
    nint n = apply(Ꮡv, (ж<atomic.Int32> x) => (nint)x.Load() + 5);
    fmt.Println(n);
    consume(Ꮡv, (ж<atomic.Int32> x) => {
        x.Store(99);
    });
    fmt.Println(Ꮡv.Load());
    var @double = new runner(gen: (ж<atomic.Int32> x) => (nint)x.Load() * 2
    );
    fmt.Println(@double.invoke(Ꮡv));
    var seven = new runner(gen: (ж<atomic.Int32> x) => 7
    );
    fmt.Println(seven.invoke(Ꮡv));
}

} // end main_package
