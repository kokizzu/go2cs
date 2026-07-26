namespace go;

using fmt = fmt_package;
using @unsafe = unsafe_package;

partial class main_package {

[GoType] partial struct header {
    internal int32 kind;
    internal uint32 size;
    internal @string name;
}

[GoType] partial struct view {
    internal int32 kind;
    internal uint32 size;
    internal @string name;
}

internal static ж<view> asView(ж<header> Ꮡh) {
    return Ꮡh.Reinterpret<header, view>();
}

internal static void churn() {
    var keep = new slice<slice<byte>>(64);
    for (nint i = 0; i < 400000; i++) {
        keep[(nint)(i & 63)] = new slice<byte>(24);
    }
}

internal static void Main() {
    var h = Ꮡ(new header(kind: 24, size: 16, name: "string"u8));
    var v = asView(h);
    v.Value.kind = 25;
    fmt.Println((@string)"alias:"u8, (~h).kind == 25, (~v).size == 16, (~v).name == "string"u8);
    var retained = asView(Ꮡ(new header(kind: 24, size: 16, name: "retained"u8)));
    churn();
    fmt.Println((@string)"lifetime:"u8, (~retained).kind == 24, (~retained).size == 16, (~retained).name == "retained"u8);
}

} // end main_package
