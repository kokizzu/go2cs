namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct slot {
    internal nint n;
}

[GoRecv] internal static void inc(this ref slot s) {
    s.n++;
}

[GoRecv] internal static void add(this ref slot s, nint d) {
    s.n += d;
}

[GoRecv] internal static nint get(this ref slot s) {
    return s.n;
}

[GoType] partial struct holder {
    internal slot s;
}

internal static ж<holder> get(ж<holder> Ꮡh) {
    return Ꮡh;
}

internal static void Main() {
    var @base = Ꮡ(new holder(nil));
    var h = get(@base);
    h.of(holder.Ꮡs).inc();
    h.of(holder.Ꮡs).inc();
    h.of(holder.Ꮡs).add(10);
    fmt.Println(@base.of(holder.Ꮡs).get());
}

} // end main_package
