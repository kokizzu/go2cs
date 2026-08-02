namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType] partial struct Tally {
    internal nint total;
    internal @string log;
}

public static void Add(this ж<Tally> Ꮡt, nint n) => func((defer, recover) => {
    ref var t = ref Ꮡt.DerefOrNull();

    defer(() => {
        Ꮡt.Value.log = fmt.Sprintf("%s+%d"u8, Ꮡt.Value.log, n);
    });
    t.total += n;
});

public static void AddTwice(this ж<Tally> Ꮡt, nint n) {
    Ꮡt.Add(n);
    Ꮡt.Add(n);
}

internal static (nint, @string) bump(Tally tʗp, nint n) {
    ref var t = ref heap(tʗp, out var Ꮡt);

    t.total++;
    Ꮡt.Add(n);
    Ꮡt.AddTwice(n * 10);
    return (t.total, t.log);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object bumpedˢ = (@string)"bumped:"u8;
private static readonly object callerCopyUntouchedˢ = (@string)"caller copy untouched:"u8;

internal static void Main() {
    var t = new Tally(total: 5, log: "start"u8);
    var (total, log) = bump(t, 3);
    fmt.Println(bumpedˢ, total, log);
    fmt.Println(callerCopyUntouchedˢ, t.total, t.log);
}

} // end main_package
