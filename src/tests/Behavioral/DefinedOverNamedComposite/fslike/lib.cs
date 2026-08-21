[assembly: go.GoPositionMap("lib.go", "lib.cs", "AAgUgKSAyoKCgpTMoA==")]

namespace go.DefinedOverNamedComposite;

partial class fslike_package {

[GoType("map[@string, nint]")] partial struct MapFS;

public static nint Get(this MapFS m, @string k) {
    return m[k];
}

public static nint Size(this MapFS m) {
    return len(m);
}

[GoType("[]nint")] partial struct List;

public static nint Sum(this List l) {
    nint t = 0;
    foreach (var (_, v) in l) {
        t += v;
    }
    return t;
}

[GoType("[2]nint")] partial struct Buf;

public static nint First(this Buf b) {
    b = b.Clone();

    return b[0];
}

} // end fslike_package
