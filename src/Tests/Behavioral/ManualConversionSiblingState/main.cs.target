namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void reportProcs() {
    fmt.Println((@string)"newprocs:"u8, newprocs);
}

internal static void Main() {
    newprocs = 4;
    sched.disable.user = true;
    var np = Ꮡsched.of(schedlike.Ꮡdisable).of(schedlike_disable.Ꮡn);
    np.Value = 7;
    sched.label = "ok"u8;
    reportProcs();
    fmt.Println((@string)"disable:"u8, sched.disable.user, sched.disable.n);
    fmt.Println((@string)"label:"u8, sched.label);
}

} // end main_package
