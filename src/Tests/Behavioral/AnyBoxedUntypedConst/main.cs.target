namespace go;

using fmt = fmt_package;
using ꓸꓸꓸany = Span<any>;

partial class main_package {

internal static readonly UntypedInt namedInt = 7;

internal static readonly UntypedInt namedRune = /* 'A' */ 65;

internal static readonly UntypedFloat namedFloat = 2.5;

internal static readonly UntypedInt namedWide = /* 1 << 40 */ 1099511627776;

internal static readonly @string namedStr = "seed"u8;

internal static readonly @string typedStr = "seed"u8;

[GoType] partial struct holder {
    internal any v;
}

internal static bool variadicEq(params ꓸꓸꓸany argsʗp) {
    var args = argsʗp.sslice();

    return AreEqual(args[0], args[1]);
}

internal static bool paramEq(any a, any b) {
    return AreEqual(a, b);
}

internal static any retLit() {
    return (nint)(0);
}

internal static any retNamed() {
    return (nint)(namedInt);
}

internal static any retRuneL() {
    return (rune)'A';
}

internal static any retFloatL() {
    return 2.5D;
}

internal static @string kindOf(any v) {
    switch (v.type()) {
    case nint: {
        return "int"u8;
    }
    case int32: {
        return "int32"u8;
    }
    case float64: {
        return "float64"u8;
    }
    case @string: {
        return "string"u8;
    }}

    return "other"u8;
}

internal static any retStrL() {
    return (@string)"seed";
}

internal static (nint, error) readN() {
    return (0, default!);
}

internal static void Main() {
    var (n, _) = readN();
    nint i = 7;
    rune r = (rune)'A';
    var f = 2.5D;
    fmt.Println((@string)"var lit zero  :", variadicEq(n, (nint)(0)));
    fmt.Println((@string)"var lit int   :", variadicEq(i, (nint)(7)));
    fmt.Println((@string)"var named     :", variadicEq(i, (nint)(namedInt)));
    fmt.Println((@string)"var named+1   :", variadicEq(i + 1, (nint)(namedInt + 1)));
    fmt.Println((@string)"var lit expr  :", variadicEq(i + 1, (nint)(3 + 5)));
    fmt.Println((@string)"var neg lit   :", variadicEq(-i, (nint)(-7)));
    fmt.Println((@string)"var rune lit  :", variadicEq(r, (rune)'A'));
    fmt.Println((@string)"var rune named:", variadicEq(r, (int32)(namedRune)));
    fmt.Println((@string)"var float lit :", variadicEq(f, 2.5D));
    fmt.Println((@string)"var float nmd :", variadicEq(f, (float64)(namedFloat)));
    fmt.Println((@string)"par lit       :", paramEq(i, (nint)(7)));
    fmt.Println((@string)"par named     :", paramEq(i, (nint)(namedInt)));
    fmt.Println((@string)"par rune lit  :", paramEq(r, (rune)'A'));
    fmt.Println((@string)"par float lit :", paramEq(f, 2.5D));
    any av = (nint)(7);
    any ar = (rune)'A';
    any af = 2.5D;
    fmt.Println((@string)"asn lit       :", AreEqual(av, ((any)i)));
    fmt.Println((@string)"asn rune lit  :", AreEqual(ar, ((any)r)));
    fmt.Println((@string)"asn float lit :", AreEqual(af, ((any)f)));
    var sl = new any[]{(nint)(7), (rune)'A', 2.5D, (nint)(namedInt)}.slice();
    fmt.Println((@string)"slc lit       :", AreEqual(sl[0], ((any)i)));
    fmt.Println((@string)"slc rune lit  :", AreEqual(sl[1], ((any)r)));
    fmt.Println((@string)"slc float lit :", AreEqual(sl[2], ((any)f)));
    fmt.Println((@string)"slc named     :", AreEqual(sl[3], ((any)i)));
    var mk = new map<any, @string>{[(nint)(7)] = "seven"u8, [(rune)'A'] = "letter"u8, [2.5D] = "half"u8};
    fmt.Println((@string)"mapkey lit    :", mk[((any)i)], mk[((any)r)], mk[((any)f)]);
    fmt.Println((@string)"mapkey lookup :", mk[(nint)(7)], mk[(rune)'A'], mk[2.5D], mk[(nint)(namedInt)]);
    var (_, mkMiss) = mk[(int32)7, ꟷ];
    fmt.Println((@string)"mapkey miss   :", mkMiss);
    var mv = new map<@string, any>{["i"u8] = (nint)(7), ["r"u8] = (rune)'A', ["f"u8] = 2.5D};
    fmt.Println((@string)"mapval lit    :", AreEqual(mv["i"u8], ((any)i)), AreEqual(mv["r"u8], ((any)r)), AreEqual(mv["f"u8], ((any)f)));
    var hk = new holder(v: (nint)(7));
    var hp = new holder((nint)(7));
    fmt.Println((@string)"struct keyed  :", AreEqual(hk.v, ((any)i)));
    fmt.Println((@string)"struct posit  :", AreEqual(hp.v, ((any)i)));
    var ch = new channel<any>(1);
    ch.ᐸꟷ((nint)(7));
    fmt.Println((@string)"chan send     :", AreEqual(ᐸꟷ(ch), ((any)i)));
    fmt.Println((@string)"ret lit       :", AreEqual(retLit(), ((any)n)));
    fmt.Println((@string)"ret named     :", AreEqual(retNamed(), ((any)i)));
    fmt.Println((@string)"ret rune lit  :", AreEqual(retRuneL(), ((any)r)));
    fmt.Println((@string)"ret float lit :", AreEqual(retFloatL(), ((any)f)));
    nint wide = (nint)(1099511627776L);
    fmt.Println((@string)"wide lit      :", variadicEq(wide, (nint)(1099511627776L)));
    fmt.Println((@string)"wide named    :", variadicEq(wide, (nint)(namedWide)));
    fmt.Println((@string)"wide kinds    :", kindOf((nint)(1099511627776L)), kindOf((nint)(namedWide)));
    fmt.Println((@string)"kinds lit     :", kindOf((nint)(0)), kindOf((rune)'A'), kindOf(2.5D));
    fmt.Println((@string)"kinds named   :", kindOf((nint)(namedInt)), kindOf((int32)(namedRune)), kindOf((float64)(namedFloat)));
    fmt.Println((@string)"kinds value   :", kindOf(i), kindOf(r), kindOf(f));
    var (iv, iok) = ((any)(nint)(7))._<nint>(ᐧ);
    var (rv, rok) = ((any)(rune)'A')._<int32>(ᐧ);
    var (fv, fok) = ((any)2.5D)._<float64>(ᐧ);
    fmt.Println((@string)"assert lit    :", iv, iok, rv, rok, fv, fok);
    @string s = "seed"u8;
    fmt.Println((@string)"str var lit   :", variadicEq(s, (@string)"seed"));
    fmt.Println((@string)"str var cat   :", variadicEq(s, (@string)("se" + "ed")));
    fmt.Println((@string)"str var named :", variadicEq(s, namedStr), variadicEq(s, typedStr));
    fmt.Println((@string)"str par lit   :", paramEq(s, (@string)"seed"), paramEq((@string)"seed", (@string)"seed"));
    any sv = (@string)"seed";
    any sc = (@string)("se" + "ed");
    fmt.Println((@string)"str asn lit   :", AreEqual(sv, ((any)s)), AreEqual(sc, ((any)s)));
    var ssl = new any[]{(@string)"seed", (@string)("se" + "ed")}.slice();
    fmt.Println((@string)"str slc       :", AreEqual(ssl[0], ((any)s)), AreEqual(ssl[1], ((any)s)));
    var smk = new map<any, @string>{[(@string)"seed"] = "hit"u8};
    fmt.Println((@string)"str mapkey    :", smk[(@string)"seed"], smk[((any)s)]);
    var smv = new map<@string, any>{["k"u8] = (@string)"seed", ["c"u8] = (@string)("se"u8 + "ed"u8)};
    fmt.Println((@string)"str mapval    :", AreEqual(smv["k"u8], ((any)s)), AreEqual(smv["c"u8], ((any)s)));
    var shk = new holder(v: (@string)"seed");
    var shp = new holder((@string)"seed");
    var shc = new holder(v: (@string)("se"u8 + "ed"u8));
    fmt.Println((@string)"str struct    :", AreEqual(shk.v, ((any)s)), AreEqual(shp.v, ((any)s)), AreEqual(shc.v, ((any)s)));
    var sch = new channel<any>(1);
    sch.ᐸꟷ((@string)"seed");
    fmt.Println((@string)"str chan      :", AreEqual(ᐸꟷ(sch), ((any)s)));
    fmt.Println((@string)"str ret       :", AreEqual(retStrL(), ((any)s)));
    fmt.Println((@string)"str conv      :", AreEqual(((any)(@string)("seed"u8)), ((any)s)));
    var (stv, stok) = ((any)(@string)("seed"u8))._<@string>(ᐧ);
    fmt.Println((@string)"str assert    :", stv, stok);
    fmt.Println((@string)"str kinds     :", kindOf((@string)"seed"), kindOf((@string)("se" + "ed")), kindOf(namedStr), kindOf(typedStr), kindOf(s));
}

} // end main_package
