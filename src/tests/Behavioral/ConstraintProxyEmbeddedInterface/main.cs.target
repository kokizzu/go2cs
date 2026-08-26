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

internal static void Main() {
    use<ImplжConstrained>(Ꮡ(new Impl("alpha"u8, 1)));
    use<ImplжConstrained>(Ꮡ(new Impl("beta"u8, 10)));
    fmt.Println(second<ImplжConstrained>(Ꮡ(new Impl("gamma"u8, 100))));
}

} // end main_package
