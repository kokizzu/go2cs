namespace go;

using fmt = fmt_package;

partial class main_package {

internal static Action makeGreeter(@string name) {
    return () => func((defer, recover) => {
        deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"bye", name, defer);
        fmt.Println((@string)"hi"u8, name);
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deferredˢ = (@string)"deferred"u8;
private static readonly object bodyˢ = (@string)"body"u8;
private static readonly object closureRecoveredˢ = (@string)"closure recovered:"u8;
private static readonly @string go2csˢ = "go2cs"u8;
private static readonly object argClosureDeferredˢ = (@string)"arg-closure deferred"u8;
private static readonly object argClosureBodyˢ = (@string)"arg-closure body"u8;
private static readonly object outerRecoveredˢ = (@string)"outer recovered:"u8;
private static readonly object fetchDeferredˢ = (@string)"fetch deferred"u8;
private static readonly object taskˢ = (@string)"task:"u8;
private static readonly object fetchedˢ = (@string)"fetched:"u8;
private static readonly object doneˢ = (@string)"done"u8;

internal static void Main() {
    var f = () => func((defer, recover) => {
        deferǃ(ᴛ1 => fmt.Println(ᴛ1), deferredˢ, defer);
        fmt.Println(bodyˢ);
    });
    f();
    var divPrint = (nint a, nint b) => func((defer, recover) => {
        defer(() => {
            {
                var r = recover(); if (r != default!) {
                    fmt.Println(closureRecoveredˢ, r);
                }
            }
        });
        fmt.Println(a / b);
    });
    divPrint(20, 4);
    divPrint(1, 0);
    var safeDiv = (nint a, nint b) => {
        nint result = default!;
        func((defer, recover) => {
            defer(() => {
                {
                    var r = recover(); if (r != default!) {
                        result = -1;
                    }
                }
            });
            result = a / b; return;
        });
        return result;
    };
    fmt.Println(safeDiv(20, 4));
    fmt.Println(safeDiv(1, 0));
    var counted = () => {
        nint n = default!;
        func((defer, recover) => {
            defer(() => {
                n++;
            });
            n = 10;
            return;
        });
        return n;
    };
    fmt.Println(counted());
    var greet = makeGreeter(go2csˢ);
    greet();
    var run = (Action fn) => {
        fn();
    };
    run(() => func((defer, recover) => {
        deferǃ(ᴛ1 => fmt.Println(ᴛ1), argClosureDeferredˢ, defer);
        fmt.Println(argClosureBodyˢ);
    }));
    ((Action)(() => func((defer, recover) => {
        defer(() => {
            {
                var r = recover(); if (r != default!) {
                    fmt.Println(outerRecoveredˢ, r);
                }
            }
        });
        throw panic("from-iife");
    })))();
    var fetch = (nint, error) () => func<(nint, error)>((defer, recover) => {
        deferǃ(ᴛ1 => fmt.Println(ᴛ1), fetchDeferredˢ, defer);
        return (42, default!);
    });
    var (v, err) = fetch();
    var tk = makeTask(5);
    fmt.Println(taskˢ, (~tk).fn(), (~tk).name);
    fmt.Println(fetchedˢ, v, err);
    fmt.Println(doneˢ);
}

[GoType] partial struct task {
    internal Func<nint> fn;
    internal @string name;
}

internal static ж<task> makeTask(nint @base) {
    nint bonus = @base * 2;
    return Ꮡ(new task(fn: () => bonus + 1, name: "t"u8));
}

} // end main_package
