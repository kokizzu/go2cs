global using aliased = go.array<byte>;

[assembly: go.GoPositionMap("main.go", "main.cs", "ACEihIKEgoaChoKIgoSChoKChIKChoaChIKGhoKC")]

namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("[6]byte")] partial struct named;

internal static array<byte> pkgEmpty = new byte[]{}.array(8);

internal static array<byte> pkgPartial = new byte[]{1, 2}.array(8);

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object emptyˢ = (@string)"empty"u8;
private static readonly object partialˢ = (@string)"partial"u8;
private static readonly object fullˢ = (@string)"full"u8;
private static readonly object ellipsisˢ = (@string)"ellipsis"u8;
private static readonly object keyedˢ = (@string)"keyed"u8;
private static readonly object keyedZeroˢ = (@string)"keyedZero"u8;
private static readonly object namedEmptyˢ = (@string)"named empty"u8;
private static readonly object namedPartialˢ = (@string)"named partial"u8;
private static readonly object aliasEmptyˢ = (@string)"alias empty"u8;
private static readonly object aliasPartialˢ = (@string)"alias partial"u8;
private static readonly object pkgˢ = (@string)"pkg"u8;
private static readonly object intsˢ = (@string)"ints"u8;
private static readonly object strsˢ = (@string)"strs"u8;
private static readonly object sliceCtlˢ = (@string)"slice ctl"u8;
private static readonly object writeˢ = (@string)"write"u8;

internal static void Main() {
    var empty = new byte[]{}.array(8);
    fmt.Println(emptyˢ, len(empty), empty[0], empty[7]);
    var partial = new byte[]{1, 2}.array(8);
    fmt.Println(partialˢ, len(partial), partial[0], partial[1], partial[2], partial[7]);
    var full = new byte[]{1, 2, 3}.array();
    fmt.Println(fullˢ, len(full), full[0], full[2]);
    var ellipsis = new byte[]{4, 5, 6}.array();
    fmt.Println(ellipsisˢ, len(ellipsis), ellipsis[2]);
    var keyed = new array<byte>(8){[5] = 1};
    fmt.Println(keyedˢ, len(keyed), keyed[5], keyed[0], keyed[7]);
    var keyedZero = new array<byte>(8){[0] = 9};
    fmt.Println(keyedZeroˢ, len(keyedZero), keyedZero[0], keyedZero[7]);
    fmt.Println(namedEmptyˢ, len(new named(new byte[6].array())));
    var np = new named(new byte[]{1, 2}.array(6));
    fmt.Println(namedPartialˢ, len(np), np[0], np[5]);
    fmt.Println(aliasEmptyˢ, len(new byte[]{}.array(5)));
    var ap = new byte[]{9}.array(5);
    fmt.Println(aliasPartialˢ, len(ap), ap[0], ap[4]);
    fmt.Println(pkgˢ, len(pkgEmpty), len(pkgPartial), pkgPartial[1], pkgPartial[7]);
    var ints = new nint[]{7}.array(4);
    fmt.Println(intsˢ, len(ints), ints[0], ints[3]);
    var strs = new @string[]{"a"u8}.array(3);
    fmt.Println(strsˢ, len(strs), strs[0], strs[2] == "");
    fmt.Println(sliceCtlˢ, len(new byte[]{}.slice()), len(new byte[]{1, 2}.slice()));
    var w = new byte[]{1}.array(8);
    w[7] = 42;
    fmt.Println(writeˢ, len(w), w[0], w[6], w[7]);
}

} // end main_package
