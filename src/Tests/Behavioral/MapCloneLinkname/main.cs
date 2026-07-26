namespace go;

using fmt = fmt_package;
// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)

partial class main_package {

internal static any clone(any m) {
    return mapclone(m);
}

internal static void Main() {
    var m = new map<@string, nint>{["a"u8] = 1, ["b"u8] = 2, ["c"u8] = 3};
    var mc = clone(m)._<map<@string, nint>>();
    mc["a"u8] = 99;
    mc["d"u8] = 4;
    delete(mc, "b"u8);
    fmt.Println((@string)"orig len:"u8, len(m), (@string)"clone len:"u8, len(mc));
    fmt.Println((@string)"orig a:"u8, m["a"u8], (@string)"clone a:"u8, mc["a"u8]);
    var (_, origHasD) = m["d"u8, ꟷ];
    var (_, origHasB) = m["b"u8, ꟷ];
    fmt.Println((@string)"orig has d:"u8, origHasD, (@string)"orig has b:"u8, origHasB);
    var (_, cloneHasB) = mc["b"u8, ꟷ];
    fmt.Println((@string)"clone c:"u8, mc["c"u8], (@string)"clone d:"u8, mc["d"u8], (@string)"clone has b:"u8, cloneHasB);
}

} // end main_package
