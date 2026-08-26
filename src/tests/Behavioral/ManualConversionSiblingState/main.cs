namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object newprocsˢ = (@string)"newprocs:"u8;

internal static void reportProcs() {
    fmt.Println(newprocsˢ, newprocs);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object disableˢ = (@string)"disable:"u8;
private static readonly object labelˢ = (@string)"label:"u8;

internal static void Main() {
    newprocs = 4;
    sched.disable.user = true;
    var np = Ꮡsched.of(schedlike.Ꮡdisable).of(schedlike_disable.Ꮡn);
    np.Value = 7;
    sched.label = "ok"u8;
    reportProcs();
    fmt.Println(disableˢ, sched.disable.user, sched.disable.n);
    fmt.Println(labelˢ, sched.label);
}

} // end main_package
