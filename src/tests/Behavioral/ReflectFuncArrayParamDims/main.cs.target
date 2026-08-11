namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

[GoType] partial struct wrap {
    public array<byte> Buf = new(8);
}

internal static nint declared([GoArrayDims(16)] array<byte> @in) {
    @in = @in.Clone();

    return len(@in);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string f32ˢ = "f32"u8;
private static readonly @string f64ˢ = "f64"u8;
private static readonly @string nestedˢ = "nested"u8;
private static readonly @string plainˢ = "plain"u8;
private static readonly @string declaredˢ = "declared"u8;
private static readonly object distinctIn0Typesˢ = (@string)"distinct in0 types:"u8;
private static readonly object sameAsItselfˢ = (@string)"same as itself:    "u8;
private static readonly object nestedElemˢ = (@string)"nested elem:"u8;
private static readonly object structFieldˢ = (@string)"struct field:"u8;
private static readonly object generatedCallˢ = (@string)"generated call:"u8;

internal static void Main() {
    var f32 = ([GoArrayDims(32)] array<byte> @in) => {
        @in = @in.Clone();
        return len(@in) == 32;
    };
    var f64 = ([GoArrayDims(64)] array<byte> @in, wrap w) => {
        @in = @in.Clone();
        w = w.ΔClone();
        return len(@in) + len(w.Buf);
    };
    var nested = ([GoArrayDims(2, 3)] array<array<nint>> @in) => {
        @in = @in.Clone();
        return len(@in) * len(@in[0]);
    };
    var plain = (nint a, slice<byte> s) => a + len(s);
    report(f32ˢ, reflect.TypeOf(f32));
    report(f64ˢ, reflect.TypeOf(f64));
    report(nestedˢ, reflect.TypeOf(nested));
    report(plainˢ, reflect.TypeOf(plain));
    report(declaredˢ, reflect.TypeOf(declared));
    fmt.Println(distinctIn0Typesˢ, !AreEqual(reflect.TypeOf(f32).In(0), reflect.TypeOf(f64).In(0)));
    fmt.Println(sameAsItselfˢ, AreEqual(reflect.TypeOf(f32).In(0), reflect.TypeOf(f32).In(0)));
    var inner = reflect.TypeOf(nested).In(0).Elem();
    fmt.Println(nestedElemˢ, inner, inner.Len());
    var field = reflect.TypeOf(f64).In(1).Field(0);
    fmt.Println(structFieldˢ, field.Name, field.Type, field.Type.Len());
    fmt.Println(generatedCallˢ, generateAndCall(reflect.ValueOf(f32)));
    fmt.Println(generatedCallˢ, generateAndCall(reflect.ValueOf(declared)));
}

internal static void report(@string name, reflectꓸType t) {
    var in0 = t.In(0);
    @string line = fmt.Sprintf("%-9s in0=%-10v kind=%-7v len=%d"u8, name, in0, in0.Kind(), lenOf(in0));
    if (in0.Kind() == reflect.Array) {
        line += fmt.Sprintf(" new=%d zero=%d"u8, reflect.New(in0).Elem().Len(), reflect.Zero(in0).Len());
    }
    fmt.Println(line);
}

internal static nint lenOf(reflectꓸType t) {
    if (t.Kind() == reflect.Array) {
        return t.Len();
    }
    return 0;
}

internal static slice<any> generateAndCall(reflectꓸValue fn) {
    var argType = fn.Type().In(0);
    var arg = reflect.New(argType).Elem();
    for (nint i = 0; i < arg.Len(); i++) {
        arg.Index(i).SetUint((uint64)(i % 251));
    }
    var @out = fn.Call(new reflectꓸValue[]{arg}.slice());
    var results = new slice<any>(len(@out));
    foreach (var (i, r) in @out) {
        results[i] = r.Interface();
    }
    return results;
}

} // end main_package
