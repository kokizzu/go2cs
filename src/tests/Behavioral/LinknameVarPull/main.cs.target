namespace go;

using fmt = fmt_package;
// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
// blank import: LinknameVarPullLib_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static @string secret { get => go.LinknameVarPullLib_package.secret; set => go.LinknameVarPullLib_package.secret = value; }

internal static void Main() {
    fmt.Println(secret);
}

} // end main_package
