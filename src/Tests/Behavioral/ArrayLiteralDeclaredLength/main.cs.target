global using aliased = go.array<byte>;

namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("[6]byte")] partial struct named;

internal static array<byte> pkgEmpty = new byte[]{}.array(8);

internal static array<byte> pkgPartial = new byte[]{1, 2}.array(8);

internal static void Main() {
    var empty = new byte[]{}.array(8);
    fmt.Println((@string)"empty", len(empty), empty[0], empty[7]);
    var partial = new byte[]{1, 2}.array(8);
    fmt.Println((@string)"partial", len(partial), partial[0], partial[1], partial[2], partial[7]);
    var full = new byte[]{1, 2, 3}.array();
    fmt.Println((@string)"full", len(full), full[0], full[2]);
    var ellipsis = new byte[]{4, 5, 6}.array();
    fmt.Println((@string)"ellipsis", len(ellipsis), ellipsis[2]);
    var keyed = new array<byte>(8){[5] = 1};
    fmt.Println((@string)"keyed", len(keyed), keyed[5], keyed[0], keyed[7]);
    var keyedZero = new array<byte>(8){[0] = 9};
    fmt.Println((@string)"keyedZero", len(keyedZero), keyedZero[0], keyedZero[7]);
    fmt.Println((@string)"named empty", len(new named(new byte[6].array())));
    var np = new named(new byte[]{1, 2}.array(6));
    fmt.Println((@string)"named partial", len(np), np[0], np[5]);
    fmt.Println((@string)"alias empty", len(new byte[]{}.array(5)));
    var ap = new byte[]{9}.array(5);
    fmt.Println((@string)"alias partial", len(ap), ap[0], ap[4]);
    fmt.Println((@string)"pkg", len(pkgEmpty), len(pkgPartial), pkgPartial[1], pkgPartial[7]);
    var ints = new nint[]{7}.array(4);
    fmt.Println((@string)"ints", len(ints), ints[0], ints[3]);
    var strs = new @string[]{"a"}.array(3);
    fmt.Println((@string)"strs", len(strs), strs[0], strs[2] == "");
    fmt.Println((@string)"slice ctl", len(new byte[]{}.slice()), len(new byte[]{1, 2}.slice()));
    var w = new byte[]{1}.array(8);
    w[7] = 42;
    fmt.Println((@string)"write", len(w), w[0], w[6], w[7]);
}

} // end main_package
