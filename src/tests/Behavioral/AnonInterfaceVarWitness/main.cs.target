namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct thing {
    internal nint n;
}

[GoRecv] internal static bool Equal(this ref thing t, any x) {
    var (other, ok) = x._<ж<thing>>(ᐧ);
    return ok && (~other).n == t.n;
}

[GoRecv] internal static any Public(this ref thing t) {
    return t.n;
}

[GoRecv] internal static @string Label(this ref thing t) {
    return fmt.Sprintf("thing(%d)"u8, t.n);
}


[GoType("dyn")] partial interface _ᴛ1 {
    bool Equal(any x);
}
internal static _ᴛ1 _ᴛ1ʗ = new thingж_ᴛ1(Ꮡ(new thing(nil)));


[GoType("dyn")] partial interface _ᴛ2 {
    any Public();
    bool Equal(any x);
}
internal static _ᴛ2 _ᴛ2ʗ = new thingж_ᴛ2(Ꮡ(new thing(nil)));


[GoType("dyn")] partial interface labelerᴛ1 {
    @string Label();
}
internal static labelerᴛ1 labeler = new thingжlabelerᴛ1(Ꮡ(new thing(n: 41)));


[GoType("dyn")] partial struct originᴛ1 {
    internal nint x, y;
}
internal static originᴛ1 origin = new originᴛ1(x: 3, y: 4);

[GoType("dyn")] partial interface main_eq {
    bool Equal(any x);
}

internal static void Main() {
    var a = Ꮡ(new thing(n: 7));
    var b = Ꮡ(new thing(n: 7));
    var c = Ꮡ(new thing(n: 8));
    fmt.Println(a.Equal(b.OrTypedNil()), a.Equal(c.OrTypedNil()), a.Public());
    fmt.Println(labeler.Label());
    fmt.Println(origin.x, origin.y);
    main_eq eq = new thingжmain_eq(a);
    fmt.Println(eq.Equal(b.OrTypedNil()), eq.Equal(c.OrTypedNil()));
}

} // end main_package
