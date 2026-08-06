namespace go;

using fmt = fmt_package;

partial class main_package {

internal static (nint, error) pair(nint n) {
    if (n < 0) {
        return (0, fmt.Errorf("negative %d"u8, n));
    }
    return (n * 2, default!);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object hookˢ = (@string)"hook:"u8;

internal static (int64 written, error err) send(nint n) {
    int64 written = default!;
    heap<error>(out var Ꮡerr);
    func((defer, recover) => {
    ref var err = ref Ꮡerr.ValueSlot;

        defer(() => {
            fmt.Println(hookˢ, written, Ꮡerr.ValueSlot, written > 0);
        });
        (var v, err) = pair(n);
        if (err != default!) {
            (written, err) = (0, fmt.Errorf("send: %w"u8, err));
            return;
        }
        written = (int64)v;
    });
    return (written, Ꮡerr.ValueSlot);
}

internal static nint /*x*/ addrv() {
    ref var x = ref heap(new nint(), out var Ꮡx);

    nint y = 1;
    x = 2;
    _ = y;
    var p = Ꮡx;
    p.Value = 5;
    return x;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object litHookˢ = (@string)"lit hook:"u8;

internal static (int64, error) lit(nint n) {
    var f = () => {
        int64 w = default!;
        heap<error>(out var Ꮡe);
        func((defer, recover) => {
            ref var e = ref Ꮡe.ValueSlot;
            defer(() => {
                fmt.Println(litHookˢ, w, Ꮡe.ValueSlot);
            });
            (var v, Ꮡe.ValueSlot) = pair(n);
            if (Ꮡe.ValueSlot != default!) {
                return;
            }
            w = (int64)v;
            return;
        });
        return (w, Ꮡe.ValueSlot);
    };
    return f();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object clsPairˢ = (@string)"cls pair:"u8;

internal static error /*err*/ cls(nint n) {
    ref var err = ref heap<error>(out var Ꮡerr);

    void set() {
        Ꮡerr.ValueSlot = fmt.Errorf("cls %d"u8, n);
    }
    (var v, err) = pair(n);
    fmt.Println(clsPairˢ, v, err);
    set();
    return err;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object sendˢ = (@string)"send:"u8;
private static readonly object addrvˢ = (@string)"addrv:"u8;
private static readonly object litˢ = (@string)"lit:"u8;
private static readonly object clsˢ = (@string)"cls:"u8;

internal static void Main() {
    var (w, e) = send(3);
    fmt.Println(sendˢ, w, e);
    (w, e) = send(-1);
    fmt.Println(sendˢ, w, e);
    fmt.Println(addrvˢ, addrv());
    (w, e) = lit(4);
    fmt.Println(litˢ, w, e);
    (w, e) = lit(-2);
    fmt.Println(litˢ, w, e);
    fmt.Println(clsˢ, cls(5));
}

} // end main_package
