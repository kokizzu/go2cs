namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct pusher {
    internal @string id;
}

internal static error Push(this pusher p, @string target, nint weight) {
    fmt.Println((@string)"push"u8, target, weight, (@string)"from"u8, p.id);
    return default!;
}

internal static @string Label(this pusher p) {
    return "pusher:"u8 + p.id;
}

[GoType] partial struct setter {
    internal @string id;
}

[GoRecv] internal static void Set(this ref setter s, @string value) {
    fmt.Println((@string)"set"u8, value, (@string)"on"u8, s.id);
}

[GoType("dyn")] partial interface serve_type {
    error Push(@string target, nint weight);
    @string Label();
}

internal static void serve(any v) {
    {
        var (p, ok) = v._<serve_type>(ᐧ); if (ok){
            fmt.Println((@string)"err:"u8, p.Push("/style.css"u8, 7), p.Label());
        } else {
            fmt.Println((@string)"not a pusher"u8);
        }
    }
}

[GoType("dyn")] partial interface apply_type {
    void Set(@string value);
}

internal static void apply(any v) {
    {
        var (s, ok) = v._<apply_type>(ᐧ); if (ok){
            s.Set("blue"u8);
        } else {
            fmt.Println((@string)"not a setter"u8);
        }
    }
}

internal static void Main() {
    serve(new pusher(id: "v"u8));
    serve(Ꮡ(new pusher(id: "p"u8)));
    apply(Ꮡ(new setter(id: "s"u8)));
    apply(new pusher(id: "x"u8));
}

} // end main_package
