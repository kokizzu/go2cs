namespace go;

using fmt = fmt_package;
using ga = GenericTypeAliasLib_package;
using GenericTypeAliasLib = GenericTypeAliasLib_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object aliasedˢ = (@string)"aliased:"u8;

internal static void aliasedImport() {
    ga.Box<@string> a = ga.NewBox<@string>((@string)"g");
    slice<ga.Box<nint>> l = new ga.Box<nint>[]{new(V: 1), new(V: 2)}.slice();
    map<nint, EmptyStruct> s = new map<nint, EmptyStruct>{[3] = new()};
    fmt.Println(aliasedˢ, a.Get(), len(l), l[1].Get(), len(s));
}

} // end main_package
