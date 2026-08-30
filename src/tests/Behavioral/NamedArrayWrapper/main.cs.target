global using feAlias = go.array<ulong>;

namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("[4]uint64")] partial struct pageBits;

[GoType("pageBits")] partial struct pallocBits;

[GoType("[]uint32")] partial struct pm;

[GoType("[4]byte")] partial struct tb;

internal static void zeroTB(ref tb buf) {
    buf = new tb(new byte[4].array());
}

[GoRecv] internal static void set(this ref pageBits b, nuint i, uint64 v) {
    b.Value[i] = v;
}

[GoRecv] internal static uint64 get(this ref pageBits b, nuint i) {
    return b.Value[i];
}

internal static void fill(this ж<pallocBits> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    for (nint i = 0; i < 4; i++) {
        b.Value[i] = (uint64)(i * 10 + 1);
    }
}

internal static void Main() {
    ref var e = ref heap(new pallocBits(), out var Ꮡe);
    Ꮡe.fill();
    fmt.Println((Ꮡ((pageBits)(~Ꮡe))).get(0), (Ꮡ((pageBits)(~Ꮡe))).get(3));
    (Ꮡ((pageBits)(~Ꮡe))).set(1, 99);
    fmt.Println(e[1]);
    var arr = new uint32[]{10, 20, 30, 40, 50, 60}.array();
    var p = ((pm)(arr[2..5]));
    var d = new slice<uint32>(3);
    nint n = copy(d, p);
    fmt.Println(n, d[0], d[1], d[2]);
    array<uint32> arr2 = new(6);
    nint n2 = copy(arr2[3..], new pm(new uint32[]{7, 8, 9}.slice()));
    fmt.Println(n2, arr2[3], arr2[4], arr2[5], arr2[0]);
    var arr3 = new uint32[]{1, 2, 3, 4, 5, 6}.array();
    var ov = ((pm)(arr3[0..4]));
    nint n3 = copy(arr3[2..6], ov);
    fmt.Println(n3, arr3[2], arr3[3], arr3[4], arr3[5]);
    ref var b = ref heap(new pallocBits(), out var Ꮡb);
    b[0] = 5;
    (Ꮡ((pageBits)(~Ꮡb))).set(2, 30);
    fmt.Println(len(b), b[0] + b[2]);
    var src = new pm(new uint32[]{1, 2, 3, 4}.slice());
    var dd = new slice<uint32>(3);
    fmt.Println(copy(dd, src), dd[2]);
    src[0] = 100;
    fmt.Println(src[0], dd[0]);
    var t = new tb(new byte[4].array());
    fmt.Println(len(t), t[0], t[3]);
    t[2] = 9;
    fmt.Println(t[2]);
    var w = new pm(new uint32[]{}.slice());
    fmt.Println(len(w), cap(w));
    w = append(w, (uint32)(42));
    fmt.Println(len(w), w[0]);
    zeroTB(ref t);
    fmt.Println(t[2], len(t));
    var seen = new map<tb, nint>{};
    tb k1 = default!;
    tb k2 = default!;
    (k1[0], k1[3]) = (7, 9);
    (k2[0], k2[3]) = (7, 9);
    seen[k1] = 42;
    var (v, ok) = seen[k2, ꟷ];
    fmt.Println(v, ok, len(seen), k1 == k2);
    seen[k2] = 43;
    fmt.Println(seen[k1], len(seen));
    tb k3 = default!;
    k3[1] = 1;
    seen[k3] = 5;
    fmt.Println(len(seen), seen[k1], seen[k3]);
    tb z1 = default!;
    tb z2 = default!;
    var zeroSeen = new map<tb, bool>{};
    zeroSeen[z1] = true;
    fmt.Println(zeroSeen[z2], len(zeroSeen), z1 == z2);
    ref var c = ref heap(new callers(), out var Ꮡc);
    var h = new holder(trace: Ꮡc);
    h.trace.Value[0] = 0x10;
    h.trace.Value[1] = h.trace.Value[0] + 2;
    fmt.Println(4, h.trace.Value[0], h.trace.Value[1], c[0]);
    var dst = new slice<uintptr>(2);
    nint nc = copy(dst, (~h.trace).Value[..2]);
    fmt.Println(nc, dst[1]);
    ref var cs = ref heap(new counters(), out var Ꮡcs);
    var pcs = Ꮡcs;
    fmt.Println(pcs.at<counter2>(0).bump(), pcs.at<counter2>(0).bump(), cs[0].n);
    slots sl = default!;
    sl.at(1).Value.v = 77;
    sl.at(2).Value.v = (~sl.at(1)).v + 1;
    fmt.Println(sl[1].v, sl[2].v, (~sl.at(1)).v, sl.sum());
    scal sm = new();
    var ᴛ1 = sm.s.Value;
    fromBytes(ref ᴛ1, 7);
    var ᴛ2 = (nonMont)((sm.s).Value);
    @double(ref sm.s, ref ᴛ2);
    fmt.Println(sm.s[0], sm.s[3]);
    nonMont dm = default!;
    var ᴛ3 = dm.Value;
    fromBytes(ref ᴛ3, 3);
    fmt.Println(dm[1], dm[2]);
    var ᴛ4 = sm.s.Value;
    fromBytes(ref ᴛ4, 20);
    fmt.Println(sm.s[0], sm.s[3]);
    var grid = new Grid(new unit[]{2, 3, 4}.array());
    fmt.Println(grid.Total(), (nint)grid[0], len(grid));
}

[GoType("num:nint")] public partial struct unit;

[GoType("[3]unit")] partial struct Grid;

public static nint Total(this Grid g) {
    g = g.Clone();

    return (nint)g[0] + (nint)g[1] + (nint)g[2];
}

[GoType("[4]uint64")] partial struct mont;

[GoType("[4]uint64")] partial struct nonMont;

[GoType] partial struct scal {
    internal mont s;
}

internal static void fromBytes(ref array<uint64> @out, uint64 seed) {
    foreach (var (i, _) in @out) {
        @out[i] = seed + (uint64)i;
    }
}

internal static void @double(ref mont @out, ref nonMont arg) {
    foreach (var (i, _) in @out) {
        @out[i] = arg[i] * 2;
    }
}

[GoType("[4]uintptr")] partial struct callers;

[GoType] partial struct holder {
    internal ж<callers> trace;
}

[GoType] partial struct slot {
    internal nint v;
}

[GoType("[4]slot")] partial struct slots;

[GoRecv] internal static ж<slot> at(this ref slots s, nint i) {
    return Ꮡ(s.Value, i);
}

[GoRecv] internal static nint sum(this ref slots s) {
    return s.Value[0].v + s.Value[1].v + s.Value[2].v + s.Value[3].v;
}

[GoType] partial struct counter2 {
    internal int32 n;
}

[GoRecv] internal static int32 bump(this ref counter2 c) {
    c.n++;
    return c.n;
}

[GoType("[3]counter2")] partial struct counters;

} // end main_package
