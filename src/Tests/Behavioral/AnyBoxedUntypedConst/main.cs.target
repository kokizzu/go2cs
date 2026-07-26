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
    return (@string)"seed"u8;
}

internal static (nint, error) readN() {
    return (0, default!);
}

internal static void Main() {
    var (n, _) = readN();
    nint i = 7;
    rune r = (rune)'A';
    var f = 2.5D;
    fmt.Println((@string)"var lit zero  :"u8, variadicEq(n, (nint)(0)));
    fmt.Println((@string)"var lit int   :"u8, variadicEq(i, (nint)(7)));
    fmt.Println((@string)"var named     :"u8, variadicEq(i, (nint)(namedInt)));
    fmt.Println((@string)"var named+1   :"u8, variadicEq(i + 1, (nint)(namedInt + 1)));
    fmt.Println((@string)"var lit expr  :"u8, variadicEq(i + 1, (nint)(3 + 5)));
    fmt.Println((@string)"var neg lit   :"u8, variadicEq(-i, (nint)(-7)));
    fmt.Println((@string)"var rune lit  :"u8, variadicEq(r, (rune)'A'));
    fmt.Println((@string)"var rune named:"u8, variadicEq(r, (int32)(namedRune)));
    fmt.Println((@string)"var float lit :"u8, variadicEq(f, 2.5D));
    fmt.Println((@string)"var float nmd :"u8, variadicEq(f, (float64)(namedFloat)));
    fmt.Println((@string)"par lit       :"u8, paramEq(i, (nint)(7)));
    fmt.Println((@string)"par named     :"u8, paramEq(i, (nint)(namedInt)));
    fmt.Println((@string)"par rune lit  :"u8, paramEq(r, (rune)'A'));
    fmt.Println((@string)"par float lit :"u8, paramEq(f, 2.5D));
    any av = (nint)(7);
    any ar = (rune)'A';
    any af = 2.5D;
    fmt.Println((@string)"asn lit       :"u8, AreEqual(av, ((any)i)));
    fmt.Println((@string)"asn rune lit  :"u8, AreEqual(ar, ((any)r)));
    fmt.Println((@string)"asn float lit :"u8, AreEqual(af, ((any)f)));
    var sl = new any[]{(nint)(7), (rune)'A', 2.5D, (nint)(namedInt)}.slice();
    fmt.Println((@string)"slc lit       :"u8, AreEqual(sl[0], ((any)i)));
    fmt.Println((@string)"slc rune lit  :"u8, AreEqual(sl[1], ((any)r)));
    fmt.Println((@string)"slc float lit :"u8, AreEqual(sl[2], ((any)f)));
    fmt.Println((@string)"slc named     :"u8, AreEqual(sl[3], ((any)i)));
    var mk = new map<any, @string>{[(nint)(7)] = "seven"u8, [(rune)'A'] = "letter"u8, [2.5D] = "half"u8};
    fmt.Println((@string)"mapkey lit    :"u8, mk[((any)i)], mk[((any)r)], mk[((any)f)]);
    fmt.Println((@string)"mapkey lookup :"u8, mk[(nint)(7)], mk[(rune)'A'], mk[2.5D], mk[(nint)(namedInt)]);
    var (_, mkMiss) = mk[(int32)7, ꟷ];
    fmt.Println((@string)"mapkey miss   :"u8, mkMiss);
    var mv = new map<@string, any>{["i"u8] = (nint)(7), ["r"u8] = (rune)'A', ["f"u8] = 2.5D};
    fmt.Println((@string)"mapval lit    :"u8, AreEqual(mv["i"u8], ((any)i)), AreEqual(mv["r"u8], ((any)r)), AreEqual(mv["f"u8], ((any)f)));
    var hk = new holder(v: (nint)(7));
    var hp = new holder((nint)(7));
    fmt.Println((@string)"struct keyed  :"u8, AreEqual(hk.v, ((any)i)));
    fmt.Println((@string)"struct posit  :"u8, AreEqual(hp.v, ((any)i)));
    var ch = new channel<any>(1);
    ch.ᐸꟷ((nint)(7));
    fmt.Println((@string)"chan send     :"u8, AreEqual(ᐸꟷ(ch), ((any)i)));
    fmt.Println((@string)"ret lit       :"u8, AreEqual(retLit(), ((any)n)));
    fmt.Println((@string)"ret named     :"u8, AreEqual(retNamed(), ((any)i)));
    fmt.Println((@string)"ret rune lit  :"u8, AreEqual(retRuneL(), ((any)r)));
    fmt.Println((@string)"ret float lit :"u8, AreEqual(retFloatL(), ((any)f)));
    nint wide = (nint)(1099511627776L);
    fmt.Println((@string)"wide lit      :"u8, variadicEq(wide, (nint)(1099511627776L)));
    fmt.Println((@string)"wide named    :"u8, variadicEq(wide, (nint)(namedWide)));
    fmt.Println((@string)"wide kinds    :"u8, kindOf((nint)(1099511627776L)), kindOf((nint)(namedWide)));
    fmt.Println((@string)"kinds lit     :"u8, kindOf((nint)(0)), kindOf((rune)'A'), kindOf(2.5D));
    fmt.Println((@string)"kinds named   :"u8, kindOf((nint)(namedInt)), kindOf((int32)(namedRune)), kindOf((float64)(namedFloat)));
    fmt.Println((@string)"kinds value   :"u8, kindOf(i), kindOf(r), kindOf(f));
    var (iv, iok) = ((any)(nint)(7))._<nint>(ᐧ);
    var (rv, rok) = ((any)(rune)'A')._<int32>(ᐧ);
    var (fv, fok) = ((any)2.5D)._<float64>(ᐧ);
    fmt.Println((@string)"assert lit    :"u8, iv, iok, rv, rok, fv, fok);
    @string s = "seed"u8;
    fmt.Println((@string)"str var lit   :"u8, variadicEq(s, (@string)"seed"u8));
    fmt.Println((@string)"str var cat   :"u8, variadicEq(s, (@string)("se" + "ed")));
    fmt.Println((@string)"str var named :"u8, variadicEq(s, namedStr), variadicEq(s, typedStr));
    fmt.Println((@string)"str par lit   :"u8, paramEq(s, (@string)"seed"u8), paramEq((@string)"seed"u8, (@string)"seed"u8));
    any sv = (@string)"seed"u8;
    any sc = (@string)("se" + "ed");
    fmt.Println((@string)"str asn lit   :"u8, AreEqual(sv, ((any)s)), AreEqual(sc, ((any)s)));
    var ssl = new any[]{(@string)"seed", (@string)("se" + "ed")}.slice();
    fmt.Println((@string)"str slc       :"u8, AreEqual(ssl[0], ((any)s)), AreEqual(ssl[1], ((any)s)));
    var smk = new map<any, @string>{[(@string)"seed"u8] = "hit"u8};
    fmt.Println((@string)"str mapkey    :"u8, smk[(@string)"seed"u8], smk[((any)s)]);
    var smv = new map<@string, any>{["k"u8] = (@string)"seed"u8, ["c"u8] = (@string)("se"u8 + "ed"u8)};
    fmt.Println((@string)"str mapval    :"u8, AreEqual(smv["k"u8], ((any)s)), AreEqual(smv["c"u8], ((any)s)));
    var shk = new holder(v: (@string)"seed"u8);
    var shp = new holder((@string)"seed"u8);
    var shc = new holder(v: (@string)("se"u8 + "ed"u8));
    fmt.Println((@string)"str struct    :"u8, AreEqual(shk.v, ((any)s)), AreEqual(shp.v, ((any)s)), AreEqual(shc.v, ((any)s)));
    var sch = new channel<any>(1);
    sch.ᐸꟷ((@string)"seed"u8);
    fmt.Println((@string)"str chan      :"u8, AreEqual(ᐸꟷ(sch), ((any)s)));
    fmt.Println((@string)"str ret       :"u8, AreEqual(retStrL(), ((any)s)));
    fmt.Println((@string)"str conv      :"u8, AreEqual(((any)(@string)("seed"u8)), ((any)s)));
    var (stv, stok) = ((any)(@string)("seed"u8))._<@string>(ᐧ);
    fmt.Println((@string)"str assert    :"u8, stv, stok);
    fmt.Println((@string)"str kinds     :"u8, kindOf((@string)"seed"u8), kindOf((@string)("se" + "ed")), kindOf(namedStr), kindOf(typedStr), kindOf(s));
}

} // end main_package
