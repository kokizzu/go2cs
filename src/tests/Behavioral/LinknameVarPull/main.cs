[assembly: go.GoPositionMap("main.go", "main.cs", "ABIkgg==")]

namespace go;

using fmt = fmt_package;
// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
// blank import: LinknameVarPullLib_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)

partial class main_package {

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸLinknameVarPullLib() {
    builtin.initPackage(typeof(LinknameVarPullLib_package));
}

internal static @string secret { get => go.LinknameVarPullLib_package.secret; set => go.LinknameVarPullLib_package.secret = value; }

internal static void Main() {
    fmt.Println(secret);
}

} // end main_package
