namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct node {
    internal nint id;
}

internal static void Main() {
    var s = new slice<nint>(4, 8);
    foreach (var (i, _) in s) {
        s[i] = i * 10;
    }
    fmt.Println((@string)"self       :"u8, Ꮡ(s, 0) == Ꮡ(s, 0));
    fmt.Println((@string)"self mid   :"u8, Ꮡ(s, 2) == Ꮡ(s, 2));
    fmt.Println((@string)"distinct   :"u8, Ꮡ(s, 0) == Ꮡ(s, 1));
    var t = s[1..];
    fmt.Println((@string)"reslice    :"u8, Ꮡ(t, 0) == Ꮡ(s, 1));
    fmt.Println((@string)"reslice x  :"u8, Ꮡ(t, 0) == Ꮡ(s, 0));
    fmt.Println((@string)"reslice 2  :"u8, Ꮡ(t, 2) == Ꮡ(s, 3));
    var u = t[1..];
    fmt.Println((@string)"reslice^2  :"u8, Ꮡ(u, 0) == Ꮡ(s, 2));
    var w = append(s.slice(-1, 0, 1), (nint)(99));
    fmt.Println((@string)"append same:"u8, Ꮡ(w, 0) == Ꮡ(s, 0));
    ref var a = ref heap(new array<nint>(4), out var Ꮡa);
    var v = a[..];
    fmt.Println((@string)"arr self   :"u8, Ꮡa.at<nint>(1) == Ꮡa.at<nint>(1));
    fmt.Println((@string)"arr vs slc :"u8, Ꮡ(v, 2) == Ꮡa.at<nint>(2));
    fmt.Println((@string)"arr vs slc2:"u8, Ꮡ(v, 0) == Ꮡa.at<nint>(1));
    var ns = new slice<node>(3);
    fmt.Println((@string)"struct elem:"u8, Ꮡ(ns, 1) == Ꮡ(ns, 1), Ꮡ(ns, 1) == Ꮡ(ns, 2));
    var p = Ꮡ(s, 1);
    p.Value = 42;
    fmt.Println((@string)"write thru :"u8, s[1], t[0]);
    var m = new map<ж<nint>, @string>{};
    m[Ꮡ(s, 2)] = "two"u8;
    m[Ꮡ(s, 3)] = "three"u8;
    fmt.Println((@string)"map lookup :"u8, m[Ꮡ(s, 2)], m[Ꮡ(s, 3)]);
    fmt.Println((@string)"map len    :"u8, len(m));
    m[Ꮡ(s, 2)] = "TWO"u8;
    fmt.Println((@string)"map len 2  :"u8, len(m));
    fmt.Println((@string)"map reslice:"u8, m[Ꮡ(t, 1)], m[Ꮡ(t, 2)]);
    var (_, miss) = m[Ꮡ(s, 0), ꟷ];
    fmt.Println((@string)"map miss   :"u8, miss);
    var z = new slice<nint>(4);
    fmt.Println((@string)"other back :"u8, Ꮡ(z, 0) == Ꮡ(s, 0));
}

} // end main_package
