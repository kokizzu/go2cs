namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void setErr(ж<error> Ꮡerr) {
    ref var err = ref Ꮡerr.DerefOrNull();

    err = fmt.Errorf("written via pointer"u8);
}

internal static void addOne(ж<nint> Ꮡn) {
    ref var n = ref Ꮡn.DerefOrNull();

    n++;
}

internal static void handlePanic(ж<error> Ꮡerr) {
    GoFrame ᒐ = default;
    try {
    ref var err = ref Ꮡerr.DerefOrNull();

        {
            var e = recover(); if (e != default!) {
                err = fmt.Errorf("recovered: %v"u8, e);
            }
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

internal static error /*err*/ errResult(bool fail) {
    heap<error>(out var Ꮡerr);
    GoFrame ᒐ = default;
    try {
    ref var err = ref Ꮡerr.ValueSlot;

        defer(setErr, Ꮡerr, ref ᒐ);
        if (fail) {
            err = fmt.Errorf("original"u8); goto ᒐdone;
        }
        err = default!;
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    ᒐdone: return Ꮡerr.ValueSlot;
}

internal static nint /*n*/ intResult() {
    heap<nint>(out var Ꮡn);
    GoFrame ᒐ = default;
    try {
    ref var n = ref Ꮡn.Value;

        defer(addOne, Ꮡn, ref ᒐ);
        n = 5;
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    return Ꮡn.Value;
}

internal static error /*err*/ recoverResult() {
    heap<error>(out var Ꮡerr);
    GoFrame ᒐ = default;
    try {
    ref var err = ref Ꮡerr.ValueSlot;

        defer(handlePanic, Ꮡerr, ref ᒐ);
        throw panic("boom");
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    return Ꮡerr.ValueSlot;
}

internal static void Main() {
    fmt.Println(errResult(false));
    fmt.Println(errResult(true));
    fmt.Println(intResult());
    fmt.Println(recoverResult());
}

} // end main_package
