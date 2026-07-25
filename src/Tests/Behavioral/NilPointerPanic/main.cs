namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct node {
    internal @string name;
    internal ж<node> next;
}

[GoRecv] internal static @string label(this ref node n) {
    return "node:"u8 + n.name;
}

internal static bool isNil(this ж<node> Ꮡn) {
    return Ꮡn == nil;
}

[GoType] partial struct box {
    internal ж<node> p;
}

internal static void @catch(@string what, Action f) => func((defer, recover) => {
    defer(() => {
        {
            var r = recover(); if (r != default!){
                fmt.Printf("%s recovered: %v\n"u8, what, r);
            } else {
                fmt.Printf("%s NO PANIC\n"u8, what);
            }
        }
    });
    f();
});

internal static void Main() {
    ж<node> p = default!;
    var pʗ1 = p;
    @catch("method-call"u8, () => {
        fmt.Println(pʗ1.label());
    });
    var pʗ3 = p;
    @catch("field-read"u8, () => {
        fmt.Println((~pʗ3).name);
    });
    var pʗ5 = p;
    @catch("field-write"u8, () => {
        pʗ5.Value.name = "x"u8;
    });
    var pʗ7 = p;
    @catch("explicit-deref"u8, () => {
        fmt.Println(pʗ7.Value);
    });
    ref var b = ref heap(new box(), out var Ꮡb);
    var bʗ1 = b;
    @catch("nested-field"u8, () => {
        fmt.Println((~bʗ1.p).name);
    });
    var nodes = new slice<ж<node>>(2);
    var nodesʗ1 = nodes;
    @catch("slice-element"u8, () => {
        fmt.Println((~nodesʗ1[0]).name);
    });
    var m = new map<@string, ж<node>>{};
    var mʗ1 = m;
    @catch("map-value"u8, () => {
        fmt.Println((~mʗ1["absent"u8]).name);
    });
    var real = Ꮡ(new node(name: "head"u8));
    var realʗ1 = real;
    @catch("chain-walk"u8, () => {
        fmt.Println((~(~realʗ1).next).name);
    });
    var pʗ9 = p;
    @catch("nil-safe-method"u8, () => {
        fmt.Println(pʗ9.isNil());
    });
    var realʗ3 = real;
    @catch("real-method"u8, () => {
        fmt.Println(realʗ3.label());
    });
    var bʗ3 = b;
    var nodesʗ3 = nodes;
    var pʗ11 = p;
    @catch("nil-compare"u8, () => {
        fmt.Println(pʗ11 == nil, bʗ3.p == nil, nodesʗ3[0] == nil);
    });
    fmt.Println((@string)"still running:", real.label());
}

} // end main_package
