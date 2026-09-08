namespace go;

using fmt = fmt_package;

partial class main_package {

internal static ж<byte> Ꮡsentinel = new StandardBox<byte>(default(byte));
internal static ref byte sentinel => ref Ꮡsentinel.Value;

internal static ж<byte> Ꮡother = new StandardBox<byte>(default(byte));
internal static ref byte other => ref Ꮡother.Value;

internal static nint classify(ж<byte> Ꮡp) {
    ref var p = ref Ꮡp.DerefOrNull();

    nint result = 0;
    while (ᐧ) {
        var exprᴛ1 = p;
        if (exprᴛ1 == Ꮡsentinel) {
            result = 1;
            Ꮡp = default!; p = ref Ꮡp.DerefOrNull();
            continue;
        }
        else if (exprᴛ1 == default!) {
            if (result == 0) {
                result = 2;
            }
            Ꮡp = Ꮡother; p = ref Ꮡp.DerefOrNull();
            continue;
        }
        else { /* default: */
            return result;
        }

    }
}

[GoType] partial struct mu {
    internal uintptr key;
}

[GoType] partial struct schedt {
    internal mu @lock;
}

internal static ж<schedt> ᏑtheSched = new StandardBox<schedt>(default(schedt));
internal static ref schedt theSched => ref ᏑtheSched.Value;

internal static bool preferLowLatency(ж<mu> Ꮡp) {
    ref var p = ref Ꮡp.DerefOrNull();

    var exprᴛ1 = p;
    if (exprᴛ1 == ᏑtheSched.of(schedt.Ꮡlock)) {
        return true;
    }
    { /* default: */
        return false;
    }

}

internal static void Main() {
    fmt.Println(classify(Ꮡsentinel));
    fmt.Println(classify(nil));
    fmt.Println(classify(Ꮡother));
    var second = Ꮡsentinel;
    fmt.Println(classify(second));
    fmt.Println(second == Ꮡsentinel, Ꮡother == Ꮡsentinel);
    ref var elsewhere = ref heap(new mu(), out var Ꮡelsewhere);
    fmt.Println(preferLowLatency(ᏑtheSched.of(schedt.Ꮡlock)), preferLowLatency(Ꮡelsewhere), preferLowLatency(nil));
}

} // end main_package
