namespace go;

using fmt = fmt_package;
using ptlike = DefinedTypeOverForeignStruct.ptlike_package;
using DefinedTypeOverForeignStruct;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("DefinedTypeOverForeignStruct.ptlike_package.Outer")] partial struct alias;

[GoRecv] internal static @string describe(this ref alias a) {
    return fmt.Sprintf("%s/%d"u8, a.Name, a.In.Len());
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string firstˢ = "first"u8;
private static readonly @string secondˢ = "second"u8;

internal static void Main() {
    var a = ((alias)ptlike.New(firstˢ, 1, 2, 3));
    fmt.Println(a.Name, a.In.Len());
    fmt.Println(a.describe());
    a.Name = secondˢ;
    fmt.Println(a.Name);
    a.In.Push(4);
    a.In.Push(5);
    fmt.Println(a.In.Len(), a.In.Vals);
    fmt.Println(a.describe());
    a.In.Vals[0] = 99;
    fmt.Println(a.In.Vals);
    var o = ((ptlike.Outer)a);
    fmt.Println(o.Name, o.In.Vals);
    alias z = default!;
    fmt.Println(z.Name == ""u8, z.In.Len());
}

} // end main_package
