namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct header {
    internal ж<node> next;
    internal nint tag;
}

[GoType] partial struct node {
    internal partial ref header header { get; }
    internal nint val;
}

[GoType] partial struct list {
    internal ж<node> head;
}

internal static void Main() {
    var a = Ꮡ(new node(val: 1));
    a.Value.tag = 7;
    var b = Ꮡ(new node(val: 2));
    b.Value.tag = 9;
    a.Value.next = b;
    list l = default!;
    l.head = a;
    var x = l.head;
    while (x != nil) {
        fmt.Println((~x).val, (~x).tag);
        x = x.Value.next;
    }
    var y = l.head;
    y.Value.tag = 99;
    fmt.Println((~a).tag);
}

} // end main_package
