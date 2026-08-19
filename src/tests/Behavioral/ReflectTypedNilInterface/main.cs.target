namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

[GoType] partial interface Encoder {
    (@string, error) Encode();
}

[GoType] partial struct Blob {
    public @string Data;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string nilBlobˢ = "<nil blob>"u8;

public static (@string, error) Encode(this ж<Blob> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    if (Ꮡb == nil) {
        return (nilBlobˢ, default!);
    }
    return (b.Data, default!);
}

[GoType] partial struct Tag {
    public nint N;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string nilTagˢ = "<nil tag>"u8;

public static (@string, error) Encode(this ж<Tag> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    if (Ꮡt == nil) {
        return (nilTagˢ, default!);
    }
    return (fmt.Sprintf("tag:%d"u8, t.N), default!);
}

[GoType] partial struct holder {
    public ж<Blob> B;
    public ж<Tag> T;
}

internal static void report(@string label, reflectꓸValue v) {
    var iface = v.Interface();
    fmt.Printf("%s: kind=%v isNil=%v ifaceIsNil=%v type=%v\n"u8, label, v.Kind(), v.IsNil(), iface == default!, reflect.TypeOf(iface));
    var (e, ok) = iface._<Encoder>(ᐧ);
    fmt.Printf("  assert Encoder ok=%v\n"u8, ok);
    if (ok) {
        var (@out, err) = e.Encode();
        fmt.Printf("  encode=%q err=%v\n"u8, @out, err);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string newBlobElemˢ = "new(*Blob).Elem()"u8;

[GoType("dyn")] partial struct main_si {
    public any I;
}

internal static void Main() {
    var s = new slice<ж<Blob>>(2);
    s[1] = Ꮡ(new Blob(Data: "second"u8));
    var rs = reflect.ValueOf(s);
    for (nint i = 0; i < rs.Len(); i++) {
        report(fmt.Sprintf("slice[%d]"u8, i), rs.Index(i));
    }
    array<ж<Tag>> a = new(2);
    a[1] = Ꮡ(new Tag(N: 7));
    var ra = reflect.ValueOf(a);
    for (nint i = 0; i < ra.Len(); i++) {
        report(fmt.Sprintf("array[%d]"u8, i), ra.Index(i));
    }
    var h = new holder(T: Ꮡ(new Tag(N: 42)));
    var rh = reflect.ValueOf(h);
    for (nint i = 0; i < rh.NumField(); i++) {
        report("field "u8 + rh.Type().Field(i).Name, rh.Field(i));
    }
    var m = new map<@string, ж<Blob>>{["nil"u8] = default!};
    var it = reflect.ValueOf(m).MapRange();
    while (it.Next()) {
        report("map["u8 + it.Key().String() + "]"u8, it.Value());
    }
    var rn = reflect.New(reflect.TypeOf(((ж<Blob>)nil))).Elem();
    report(newBlobElemˢ, rn);
    var iface = rs.Index(0).Interface();
    fmt.Printf("roundtrip: ==(*Blob)(nil) %v, ==nil %v\n"u8, iface == ((ж<Blob>)nil), iface == default!);
    fmt.Printf("elem-of-typed-nil valid=%v\n"u8, rs.Index(0).Elem().IsValid());
    ж<Blob> np = default!;
    var si = new main_si(I: np.OrTypedNil());
    var f = reflect.ValueOf(si).Field(0);
    fmt.Printf("ifaceField: kind=%v isNil=%v isZero=%v elemKind=%v elemIsNil=%v\n"u8,
        f.Kind(), f.IsNil(), f.IsZero(), f.Elem().Kind(), f.Elem().IsNil());
    var sn = new main_si();
    var fn = reflect.ValueOf(sn).Field(0);
    fmt.Printf("nilIfaceField: isNil=%v isZero=%v elemValid=%v\n"u8, fn.IsNil(), fn.IsZero(), fn.Elem().IsValid());
}

} // end main_package
