namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct node {
    internal @string name;
}

internal static slice<ж<node>> collect(this ж<node> Ꮡc, slice<ж<node>> chain) {
    var describe = @string () => Ꮡc.Value.name;
    slice<ж<node>> toCheck = default!;
    toCheck = append(toCheck, Ꮡc);
    foreach (var (_, cΔ1) in chain) {
        toCheck = append(toCheck, cΔ1);
    }
    fmt.Println((@string)"describe:"u8, describe());
    return toCheck;
}

internal static ж<node> firstOr(this ж<node> Ꮡc, slice<ж<node>> chain) {
    foreach (var (_, cΔ1) in chain) {
        return cΔ1;
    }
    return Ꮡc;
}

internal static void Main() {
    var root = Ꮡ(new node(name: "root"u8));
    var a = Ꮡ(new node(name: "a"u8));
    var b = Ꮡ(new node(name: "b"u8));
    foreach (var (_, n) in root.collect(new ж<node>[]{a, b}.slice())) {
        fmt.Println((@string)"got:"u8, (~n).name);
    }
    fmt.Println((@string)"first:"u8, (~root.firstOr(new ж<node>[]{a, b}.slice())).name);
    fmt.Println((@string)"empty:"u8, (~root.firstOr(default!)).name);
}

} // end main_package
