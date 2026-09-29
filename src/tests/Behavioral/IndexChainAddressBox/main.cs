namespace go;

using fmt = fmt_package;
using atomic = sync.atomic_package;
using sync;

partial class main_package {

[GoType] partial struct counter {
    internal nint n;
}

[GoRecv] internal static void inc(this ref counter c) {
    c.n++;
}

[GoType] partial struct hist {
    internal array<atomic.Uint64> counts = new(4);
    internal array<counter> local = new(2);
    internal array<nint> plain = new(3);
    internal slice<nint> sl;
}

[GoType] partial struct wrap {
    internal hist h;
}

internal static uint64 closureLocal() {
    uint64 record(slice<nint> samples) {
        hist h = new();
        foreach (var (_, s) in samples) {
            Ꮡh.counts.at<atomic.Uint64>(s % 4).Add(1);
        }
        return Ꮡh.counts.at<atomic.Uint64>(1).Load();
    }
    return record(new nint[]{1, 5, 2, 9}.slice());
}

internal static uint64 fieldArrayMethod() {
    hist h = new();
    Ꮡh.counts.at<atomic.Uint64>(2).Add(3);
    Ꮡh.counts.at<atomic.Uint64>(2).Add(4);
    return Ꮡh.counts.at<atomic.Uint64>(2).Load();
}

internal static uint64 nestedFieldArrayMethod() {
    wrap w = new();
    Ꮡw.h.counts.at<atomic.Uint64>(3).Add(5);
    return Ꮡw.h.counts.at<atomic.Uint64>(3).Load();
}

internal static uint64 localArrayMethod() {
    array<atomic.Uint64> a = new(4);
    Ꮡa.at<atomic.Uint64>(2).Add(6);
    return Ꮡa.at<atomic.Uint64>(2).Load();
}

internal static nint explicitAddress() {
    hist h = new();
    var p = Ꮡh.plain.at<nint>(1);
    p.Value = 7;
    p.Value += 1;
    return h.plain[1];
}

internal static nint paramAddress(hist h) {
    h = h.ΔClone();

    var p = Ꮡh.plain.at<nint>(2);
    p.Value = 9;
    return h.plain[2];
}

internal static nint methodValue() {
    hist h = new();
    var f = Ꮡh.local.at<counter>(1).inc;
    f();
    f();
    return h.local[1].n;
}

internal static nint localTypeCall() {
    hist h = new();
    h.local[0].inc();
    return h.local[0].n;
}

internal static nint sliceFieldAddress() {
    hist h = new();
    h.sl = new slice<nint>(2);
    var p = Ꮡ(h.sl, 1);
    p.Value = 11;
    return h.sl[1];
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object closureLocalˢ = (@string)"closureLocal:"u8;
private static readonly object fieldArrayMethodˢ = (@string)"fieldArrayMethod:"u8;
private static readonly object nestedFieldArrayMethodˢ = (@string)"nestedFieldArrayMethod:"u8;
private static readonly object localArrayMethodˢ = (@string)"localArrayMethod:"u8;
private static readonly object explicitAddressˢ = (@string)"explicitAddress:"u8;
private static readonly object paramAddressˢ = (@string)"paramAddress:"u8;
private static readonly object methodValueˢ = (@string)"methodValue:"u8;
private static readonly object localTypeCallˢ = (@string)"localTypeCall:"u8;
private static readonly object sliceFieldAddressˢ = (@string)"sliceFieldAddress:"u8;

internal static void Main() {
    fmt.Println(closureLocalˢ, closureLocal());
    fmt.Println(fieldArrayMethodˢ, fieldArrayMethod());
    fmt.Println(nestedFieldArrayMethodˢ, nestedFieldArrayMethod());
    fmt.Println(localArrayMethodˢ, localArrayMethod());
    fmt.Println(explicitAddressˢ, explicitAddress());
    fmt.Println(paramAddressˢ, paramAddress(new hist(nil)));
    fmt.Println(methodValueˢ, methodValue());
    fmt.Println(localTypeCallˢ, localTypeCall());
    fmt.Println(sliceFieldAddressˢ, sliceFieldAddress());
}

} // end main_package
