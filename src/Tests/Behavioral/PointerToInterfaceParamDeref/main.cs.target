namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void handle(@string label, ж<error> Ꮡerr) {
    GoFrame ᒐ = default;
    try {
    ref var err = ref Ꮡerr.DerefOrNull();

        {
            var e = recover(); if (e != default!) {
                var prev = err;
                throw panic(fmt.Sprintf("wrapped %s: %v (prev=%v)"u8, label, e, prev));
            }
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string doitˢ = "doit"u8;

internal static error /*err*/ doit() {
    heap<error>(out var Ꮡerr);
    GoFrame ᒐ = default;
    try {
    ref var err = ref Ꮡerr.ValueSlot;

        deferǃ(handle, doitˢ, Ꮡerr, ref ᒐ);
        throw panic("boom");
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    return Ꮡerr.ValueSlot;
}

internal static void Main() {
    GoFrame ᒐ = default;
    try {
        deferǃ(() => {
            {
                var e = recover(); if (e != default!) {
                    fmt.Println(e);
                }
            }
        }, ref ᒐ);
        doit();
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end main_package
