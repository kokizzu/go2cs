namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct inner {
    internal uintptr hi, lo;
}

[GoType] partial struct outer {
    internal inner stack;
    internal uintptr guard;
}

internal static ж<outer> get(ж<outer> Ꮡo) {
    return Ꮡo;
}

internal static void Main() {
    ref var @base = ref heap(new outer(), out var Ꮡbase);
    var o = get(Ꮡbase);
    o.Value.stack.hi = 100;
    o.Value.stack.lo = 50;
    o.Value.guard = 5;
    fmt.Println(@base.stack.hi, @base.stack.lo, @base.guard);
}

} // end main_package
