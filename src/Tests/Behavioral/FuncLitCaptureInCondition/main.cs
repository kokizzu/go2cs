namespace go;

using fmt = fmt_package;

partial class main_package {

internal static bool anyMatch(slice<nint> vals, Func<nint, bool> f) {
    foreach (var (_, v) in vals) {
        if (f(v)) {
            return true;
        }
    }
    return false;
}

internal static void Main() {
    var vals = new nint[]{1, 2, 3}.slice();
    var lookup = new map<nint, bool>{[2] = true, [5] = true};
    var lookupʗ1 = lookup;
    if (anyMatch(vals, (nint u) => lookupʗ1[u])) {
        fmt.Println((@string)"if:"u8, true);
    }
    {
        nint extra = 7;
        var lookupʗ3 = lookup;
        if (anyMatch(vals, (nint u) => lookupʗ3[u] || u == extra)) {
            fmt.Println((@string)"if-init:"u8, true);
        }
    }
    var lookupʗ5 = lookup;
    for (nint i = 0; i < 2 && anyMatch(vals, (nint u) => lookupʗ5[u + i]); i++) {
        fmt.Println((@string)"for:"u8, i);
    }
    nint n = 0;
    var lookupʗ7 = lookup;
    while (anyMatch(vals, (nint u) => lookupʗ7[u] && n < 2)) {
        fmt.Println((@string)"while:"u8, n);
        n++;
    }
}

} // end main_package
