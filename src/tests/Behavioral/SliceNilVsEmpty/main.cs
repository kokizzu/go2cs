namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

internal static void probe(@string name, bool isNil, nint length, nint capacity) {
    fmt.Println(name, isNil, length, capacity);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string zeroValueˢ = "zeroValue"u8;
private static readonly @string emptyLiteralˢ = "emptyLiteral"u8;
private static readonly @string emptyStringConvˢ = "emptyStringConv"u8;
private static readonly @string nilConversionˢ = "nilConversion"u8;
private static readonly @string nilResliceˢ = "nilReslice"u8;
private static readonly @string makeZeroˢ = "makeZero"u8;
private static readonly @string resliceToEmptyˢ = "resliceToEmpty"u8;
private static readonly @string resliceTailCapZeroˢ = "resliceTailCapZero"u8;
private static readonly @string appendNilNothingˢ = "appendNilNothing"u8;
private static readonly @string appendEmptyNothingˢ = "appendEmptyNothing"u8;
private static readonly @string cloneShapeˢ = "cloneShape"u8;
private static readonly @string trimShapeˢ = "trimShape"u8;
private static readonly @string stringSliceZeroˢ = "stringSliceZero"u8;
private static readonly @string stringSliceEmptyˢ = "stringSliceEmpty"u8;

internal static void Main() {
    slice<byte> zero = default!;
    probe(zeroValueˢ, zero == default!, len(zero), cap(zero));
    var emptyLit = new byte[]{}.slice();
    probe(emptyLiteralˢ, emptyLit == default!, len(emptyLit), cap(emptyLit));
    var emptyStr = slice<byte>(""u8);
    probe(emptyStringConvˢ, emptyStr == default!, len(emptyStr), cap(emptyStr));
    var nilConv = slice<byte>(default!);
    probe(nilConversionˢ, nilConv == default!, len(nilConv), cap(nilConv));
    var nilReslice = zero[0..0];
    probe(nilResliceˢ, nilReslice == default!, len(nilReslice), cap(nilReslice));
    var made = new slice<byte>(0);
    probe(makeZeroˢ, made == default!, len(made), cap(made));
    var x = new byte[]{1, 2}.slice();
    probe(resliceToEmptyˢ, x[..0] == default!, len(x[..0]), cap(x[..0]));
    var tail = x[2..2];
    probe(resliceTailCapZeroˢ, tail == default!, len(tail), cap(tail));
    var appendNilNothing = append(slice<byte>(default!));
    probe(appendNilNothingˢ, appendNilNothing == default!, len(appendNilNothing), cap(appendNilNothing));
    var appendEmptyNothing = append(new byte[]{}.slice());
    probe(appendEmptyNothingˢ, appendEmptyNothing == default!, len(appendEmptyNothing), cap(appendEmptyNothing));
    var cloneShape = appendꓸꓸꓸ(new byte[]{}.slice(), x[..0]);
    probe(cloneShapeˢ, cloneShape == default!, len(cloneShape), cap(cloneShape));
    var trim = x;
    while (len(trim) > 0) {
        trim = trim[..(int)(len(trim) - 1)];
    }
    probe(trimShapeˢ, trim == default!, len(trim), cap(trim));
    slice<@string> zs = default!;
    probe(stringSliceZeroˢ, zs == default!, len(zs), cap(zs));
    probe(stringSliceEmptyˢ, new @string[]{}.slice() == default!, len(new @string[]{}.slice()), cap(new @string[]{}.slice()));
}

} // end main_package
