namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

[GoType] partial struct point {
    internal nint x, y;
    internal slice<@string> tags;
}

[GoType] partial struct node {
    internal nint val;
    internal ж<node> next;
}

[GoType("map[@string, nint]")] partial struct named;

[GoType("map[any, nint]")] partial struct namedAny;

[GoType("map[@string, slice<nint>]")] partial struct namedSlices;

[GoType] partial struct wrap {
    internal named m;
}

internal static void Main() {
    fmt.Println(reflect.DeepEqual(new @string[]{"a"u8, "bc"u8}.slice(), new @string[]{"a"u8, "bc"u8}.slice()));
    fmt.Println(reflect.DeepEqual(new @string[]{"a"u8}.slice(), new @string[]{"b"u8}.slice()));
    fmt.Println(reflect.DeepEqual(new nint[]{1, 2, 3}.slice(), new nint[]{1, 2, 3}.slice()));
    fmt.Println(reflect.DeepEqual(new nint[]{1, 2, 3}.slice(), new nint[]{1, 2, 4}.slice()));
    fmt.Println(reflect.DeepEqual(new slice<byte>[]{slice<byte>("ab"u8), default!}.slice(), new slice<byte>[]{slice<byte>("ab"u8), default!}.slice()));
    fmt.Println(reflect.DeepEqual(new slice<byte>[]{slice<byte>("ab"u8)}.slice(), new slice<byte>[]{slice<byte>("ac"u8)}.slice()));
    fmt.Println(reflect.DeepEqual(slice<byte>(default!), new byte[]{}.slice()));
    fmt.Println(reflect.DeepEqual(new byte[]{}.slice(), new byte[]{}.slice()));
    slice<nint> nilInts = default!;
    fmt.Println(reflect.DeepEqual(nilInts, nilInts));
    fmt.Println(reflect.DeepEqual(nilInts, new nint[]{}.slice()));
    var zero = 0.0D;
    var nan = new float64[]{zero / zero}.slice();
    fmt.Println(reflect.DeepEqual(nan, nan));
    fmt.Println(reflect.DeepEqual(nan, new float64[]{nan[0]}.slice()));
    var p1 = new point(1, 2, new @string[]{"n"u8}.slice());
    var p2 = new point(1, 2, new @string[]{"n"u8}.slice());
    var p3 = new point(1, 3, new @string[]{"n"u8}.slice());
    fmt.Println(reflect.DeepEqual(p1, p2));
    fmt.Println(reflect.DeepEqual(p1, p3));
    var m1 = new map<@string, nint>{["a"u8] = 1, ["b"u8] = 2};
    var m2 = new map<@string, nint>{["b"u8] = 2, ["a"u8] = 1};
    fmt.Println(reflect.DeepEqual(m1, m2));
    fmt.Println(reflect.DeepEqual(m1, new map<@string, nint>{["a"u8] = 1}));
    fmt.Println(reflect.DeepEqual(m1, new map<@string, nint>{["a"u8] = 1, ["c"u8] = 2}));
    fmt.Println(reflect.DeepEqual(m1, new map<@string, nint>{["a"u8] = 1, ["b"u8] = 3}));
    map<@string, nint> nilMap = default!;
    fmt.Println(reflect.DeepEqual(nilMap, nilMap));
    fmt.Println(reflect.DeepEqual(nilMap, new map<@string, nint>{}));
    fmt.Println(reflect.DeepEqual(m1, m1));
    var q1 = Ꮡ(new point(1, 2, default!));
    var q2 = Ꮡ(new point(1, 2, default!));
    var q3 = q1;
    fmt.Println(reflect.DeepEqual(q1, q2));
    fmt.Println(reflect.DeepEqual(q1, q3));
    q2.Value.y = 9;
    fmt.Println(reflect.DeepEqual(q1, q2));
    ж<point> np1 = default!;
    ж<point> np2 = default!;
    fmt.Println(reflect.DeepEqual(np1, np2));
    fmt.Println(reflect.DeepEqual(np1, q1));
    var a = Ꮡ(new node(val: 1));
    a.Value.next = a;
    var b = Ꮡ(new node(val: 1));
    b.Value.next = b;
    fmt.Println(reflect.DeepEqual(a, b));
    var c = Ꮡ(new node(val: 2));
    c.Value.next = c;
    fmt.Println(reflect.DeepEqual(a, c));
    fmt.Println(reflect.DeepEqual(new nint[]{1}.slice(), new @string[]{"1"u8}.slice()));
    fmt.Println(reflect.DeepEqual(default!, default!));
    var n1 = new named(new map<@string, nint>{["a"u8] = 1, ["b"u8] = 2});
    var n2 = new named(new map<@string, nint>{["b"u8] = 2, ["a"u8] = 1});
    var n3 = new named(new map<@string, nint>{["a"u8] = 1, ["b"u8] = 3});
    var n4 = new named(new map<@string, nint>{["a"u8] = 1, ["c"u8] = 2});
    fmt.Println(reflect.DeepEqual(n1, n2));
    fmt.Println(reflect.DeepEqual(n1, n3));
    fmt.Println(reflect.DeepEqual(n1, n4));
    fmt.Println(reflect.DeepEqual(n1, new named(new map<@string, nint>{["a"u8] = 1})));
    fmt.Println(reflect.DeepEqual(n1, n1));
    named nilNamed = default!;
    fmt.Println(reflect.DeepEqual(nilNamed, nilNamed));
    fmt.Println(reflect.DeepEqual(nilNamed, new named(new map<@string, nint>{})));
    fmt.Println(reflect.DeepEqual(new wrap(n1), new wrap(n2)));
    fmt.Println(reflect.DeepEqual(new wrap(n1), new wrap(n3)));
    var s1 = new namedSlices(new map<@string, slice<nint>>{["k"u8] = new nint[]{1, 2}.slice()});
    var s2 = new namedSlices(new map<@string, slice<nint>>{["k"u8] = new nint[]{1, 2}.slice()});
    var s3 = new namedSlices(new map<@string, slice<nint>>{["k"u8] = new nint[]{1, 3}.slice()});
    fmt.Println(reflect.DeepEqual(s1, s2));
    fmt.Println(reflect.DeepEqual(s1, s3));
    var k1 = new map<any, nint>{[default!] = 1, [(@string)"b"u8] = 2};
    var k2 = new map<any, nint>{[(@string)"b"u8] = 2, [default!] = 1};
    var k3 = new map<any, nint>{[default!] = 9, [(@string)"b"u8] = 2};
    var k4 = new map<any, nint>{[(@string)"b"u8] = 2, [(@string)"c"u8] = 1};
    fmt.Println(reflect.DeepEqual(k1, k2));
    fmt.Println(reflect.DeepEqual(k1, k3));
    fmt.Println(reflect.DeepEqual(k1, k4));
    var j1 = new namedAny(new map<any, nint>{[default!] = 1, [(@string)"b"u8] = 2});
    var j2 = new namedAny(new map<any, nint>{[(@string)"b"u8] = 2, [default!] = 1});
    var j3 = new namedAny(new map<any, nint>{[default!] = 9, [(@string)"b"u8] = 2});
    var j4 = new namedAny(new map<any, nint>{[(@string)"b"u8] = 2, [(@string)"c"u8] = 1});
    fmt.Println(reflect.DeepEqual(j1, j2));
    fmt.Println(reflect.DeepEqual(j1, j3));
    fmt.Println(reflect.DeepEqual(j1, j4));
}

} // end main_package
