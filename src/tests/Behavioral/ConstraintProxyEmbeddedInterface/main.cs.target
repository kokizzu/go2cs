namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial interface Base {
    @string Name();
}

[GoType] partial interface Middle :
    Base
{
    nint Size();
}

[GoType] partial interface Constrained<T> :
    Middle
{
    T Clone();
}

[GoType] partial struct Impl {
    internal @string n;
    internal nint s;
}

[GoRecv] public static @string Name(this ref Impl p) {
    return p.n;
}

[GoRecv] public static nint Size(this ref Impl p) {
    return p.s;
}

[GoRecv] public static ж<Impl> Clone(this ref Impl p) {
    return Ꮡ(new Impl(p.n, p.s + 1));
}

internal static void use<T>(T v)
    where T : Constrained<T>
{
    var c = v.Clone();
    fmt.Println(c.Name(), c.Size(), v.Name(), v.Size());
}

internal static @string second<T>(T v)
    where T : Constrained<T>
{
    var c = v.Clone();
    return fmt.Sprint(c.Name(), (@string)"/"u8, c.Size(), (@string)"/"u8, v.Name(), (@string)"/"u8, v.Size());
}

internal static void callback<T>(T v, Action<T, nint> f)
    where T : Constrained<T>
{
    f(v, 7);
    f(v.Clone(), 8);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string outerˢ = "outer"u8;

internal static void Main() {
    use<ImplжConstrained>(Ꮡ(new Impl("alpha"u8, 1)));
    use<ImplжConstrained>(Ꮡ(new Impl("beta"u8, 10)));
    fmt.Println(second<ImplжConstrained>(Ꮡ(new Impl("gamma"u8, 100))));
    callback<ImplжConstrained>(Ꮡ(new Impl("delta"u8, 1000)), (ImplжConstrained tΔ1Δp, nint mode) => {
        var tΔ1 = (ж<Impl>)tΔ1Δp;
        fmt.Println(tΔ1.Name(), tΔ1.Size(), mode);
    });
    @string t = outerˢ;
    callback<ImplжConstrained>(Ꮡ(new Impl("epsilon"u8, 2000)), (ImplжConstrained tΔ2Δp, nint mode) => {
        var tΔ2 = (ж<Impl>)tΔ2Δp;
        fmt.Println(tΔ2.Name(), tΔ2.Size(), mode);
    });
    fmt.Println(t);
}

} // end main_package
