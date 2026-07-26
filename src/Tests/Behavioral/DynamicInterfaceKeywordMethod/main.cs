namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial interface TB {
    @string Name();
    void @private();
}

[GoType] partial struct harness {
    internal @string name;
    internal nint deadline;
}

internal static @string Name(this harness h) {
    return h.name;
}

internal static void @private(this harness h) {
}

internal static (nint, bool) Deadline(this harness h) {
    return (h.deadline, true);
}

[GoType("dyn")] partial interface commandContext_type :
    TB
{
    (nint, bool) Deadline();
}

internal static void commandContext(TB t) {
    {
        var (td, ok) = t._<commandContext_type>(ᐧ); if (ok){
            var (d, _) = td.Deadline();
            fmt.Println((@string)"Name:"u8, td.Name(), (@string)"deadline:"u8, d);
        } else {
            fmt.Println((@string)"no deadline"u8);
        }
    }
}

[GoType] partial struct gate {
    internal @string name;
}

internal static void @private(this gate g) {
    fmt.Println((@string)"sealed:"u8, g.name);
}

internal static @string Kind(this gate g) {
    return "gate:"u8 + g.name;
}

[GoType("dyn")] partial interface direct_type {
    void @private();
    @string Kind();
}

internal static void direct(any v) {
    {
        var (d, ok) = v._<direct_type>(ᐧ); if (ok){
            d.@private();
            fmt.Println((@string)"direct:"u8, d.Kind());
        } else {
            fmt.Println((@string)"no direct"u8);
        }
    }
}

internal static void Main() {
    commandContext(new harness(name: "cmd"u8, deadline: 100));
    direct(new gate(name: "v"u8));
    direct(Ꮡ(new gate(name: "p"u8)));
}

} // end main_package
