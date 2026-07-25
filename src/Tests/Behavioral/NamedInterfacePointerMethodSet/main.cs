namespace go;

using fmt = fmt_package;
using pmslib = NamedInterfacePointerMethodSet.pmslib_package;
using NamedInterfacePointerMethodSet;

partial class main_package {

[GoType] partial struct PtrOnly {
    internal @string name;
}

[GoRecv] public static @string Speak(this ref PtrOnly p) {
    return "ptr:"u8 + p.name;
}

[GoType] partial struct ValOnly {
    internal @string name;
}

public static @string Speak(this ValOnly v) {
    return "val:"u8 + v.name;
}

[GoType] partial struct Mixed {
    internal nint n;
}

public static @string Speak(this Mixed m) {
    return fmt.Sprintf("mixed:%d"u8, m.n);
}

[GoRecv] public static void Bump(this ref Mixed m) {
    m.n++;
}

[GoType] partial struct WrongSig {
}

public static @string Speak(this WrongSig w, nint times) {
    return "wrong"u8;
}

[GoType] partial struct gbox<T> {
    internal T v;
}

internal static T Get<T>(this gbox<T> b) {
    return b.v;
}

internal static void report(@string label, @string s, bool ok) {
    fmt.Println(label, ok, s);
}

internal static void Main() {
    var (s, ok) = pmslib.TrySpeak(new PtrOnly(name: "a"u8));
    report("PtrOnly-value  "u8, s, ok);
    (s, ok) = pmslib.TrySpeak(Ꮡ(new PtrOnly(name: "a"u8)));
    report("PtrOnly-pointer"u8, s, ok);
    (s, ok) = pmslib.TrySpeak(new ValOnly(name: "b"u8));
    report("ValOnly-value  "u8, s, ok);
    (s, ok) = pmslib.TrySpeak(Ꮡ(new ValOnly(name: "b"u8)));
    report("ValOnly-pointer"u8, s, ok);
    (s, ok) = pmslib.TrySpeak(new Mixed(n: 41));
    report("Mixed-value    "u8, s, ok);
    var pm = Ꮡ(new Mixed(n: 41));
    pm.Bump();
    (s, ok) = pmslib.TrySpeak(pm);
    report("Mixed-pointer  "u8, s, ok);
    (s, ok) = pmslib.TrySpeak(new WrongSig(nil));
    report("WrongSig       "u8, s, ok);
    (s, ok) = pmslib.TrySpeak((nint)(42));
    report("int            "u8, s, ok);
    (s, ok) = pmslib.TryGet(new gbox<nint>(v: 7));
    report("gbox-int       "u8, s, ok);
    (s, ok) = pmslib.TryGet(new gbox<@string>(v: "g"u8));
    report("gbox-string    "u8, s, ok);
}

} // end main_package
