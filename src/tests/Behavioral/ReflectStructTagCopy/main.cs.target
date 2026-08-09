namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

[GoType] partial struct record {
    public nint Version;
    [GoTag(@"json:""name"" asn1:""optional,explicit,tag:0""")]
    public @string Name;
    [GoTag(@"json:""data,omitempty""")]
    public slice<byte> Data;
    public bool Untagged;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string jsonˢ = "json"u8;
private static readonly @string asn1ˢ = "asn1"u8;
private static readonly object lookupJsonˢ = (@string)"lookup json:"u8;
private static readonly @string missingˢ = "missing"u8;
private static readonly object lookupMissingˢ = (@string)"lookup missing:"u8;
private static readonly object copySliceˢ = (@string)"copy slice:"u8;
private static readonly object copyShortSrcˢ = (@string)"copy short src:"u8;
private static readonly object helloˢ = (@string)"hello"u8;
private static readonly object copyStringˢ = (@string)"copy string:"u8;
private static readonly object copyWindowˢ = (@string)"copy window:"u8;
private static readonly object copyArrayˢ = (@string)"copy array:"u8;
private static readonly object copyNilDstˢ = (@string)"copy nil dst:"u8;

internal static void Main() {
    var t = reflect.TypeOf(new record(nil));
    for (nint i = 0; i < t.NumField(); i++) {
        var fΔ1 = t.Field(i);
        fmt.Printf("%s raw=%q json=%q asn1=%q\n"u8, fΔ1.Name, ((@string)fΔ1.Tag), fΔ1.Tag.Get(jsonˢ), fΔ1.Tag.Get(asn1ˢ));
    }
    var f = t.Field(1);
    var (v, ok) = f.Tag.Lookup(jsonˢ);
    fmt.Println(lookupJsonˢ, v, ok);
    (v, ok) = f.Tag.Lookup(missingˢ);
    fmt.Println(lookupMissingˢ, v, ok);
    var dst = new slice<byte>(4);
    nint n = reflect.Copy(reflect.ValueOf(dst), reflect.ValueOf(new byte[]{1, 2, 3, 4, 5, 6}.slice()));
    fmt.Println(copySliceˢ, n, dst);
    var @short = new slice<byte>(8);
    n = reflect.Copy(reflect.ValueOf(@short), reflect.ValueOf(new byte[]{9, 8, 7}.slice()));
    fmt.Println(copyShortSrcˢ, n, @short);
    var sdst = new slice<byte>(3);
    n = reflect.Copy(reflect.ValueOf(sdst), reflect.ValueOf(helloˢ));
    fmt.Println(copyStringˢ, n, sdst);
    var backing = new nint[]{0, 0, 0, 0}.slice();
    var window = backing[1..3];
    n = reflect.Copy(reflect.ValueOf(window), reflect.ValueOf(new nint[]{5, 6, 7}.slice()));
    fmt.Println(copyWindowˢ, n, backing);
    ref var arr = ref heap(new array<nint>(3), out var Ꮡarr);
    n = reflect.Copy(reflect.ValueOf(Ꮡarr).Elem(), reflect.ValueOf(new nint[]{11, 12, 13, 14}.slice()));
    fmt.Println(copyArrayˢ, n, arr);
    slice<byte> nilDst = default!;
    n = reflect.Copy(reflect.ValueOf(nilDst), reflect.ValueOf(new byte[]{1, 2}.slice()));
    fmt.Println(copyNilDstˢ, n);
}

} // end main_package
