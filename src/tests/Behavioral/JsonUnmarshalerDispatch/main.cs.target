namespace go;

using json = encoding.json_package;
using fmt = fmt_package;
using time = time_package;
using encoding;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸencodingꓸjson() {
    builtin.initPackage(typeof(encoding.json_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸtime() {
    builtin.initPackage(typeof(time_package));
}

[GoType("num:float64")] partial struct Celsius;

[GoRecv] public static error UnmarshalJSON(this ref Celsius c, slice<byte> data) {
    ref var f = ref heap(new float64(), out var Ꮡf);
    {
        var err = json.Unmarshal(data, Ꮡf); if (err != default!) {
            return err;
        }
    }
    c = ((Celsius)f) + 100D;
    return default!;
}

[GoType("dyn")] internal partial struct main_s {
    public time.Time T;
    public Celsius C;
}

internal static void Main() {
    ref var t = ref heap(new time.Time(), out var Ꮡt);
    var err = json.Unmarshal(slice<byte>(@"""2020-01-02T03:04:05Z"""u8), Ꮡt);
    fmt.Println(err, t.UTC().Format(time.RFC3339));
    err = json.Unmarshal(slice<byte>(@"{}"u8), Ꮡt);
    fmt.Println(err);
    (var b, err) = json.Marshal(t);
    fmt.Println(((@string)b), err);
    ref var c = ref heap(new Celsius(), out var Ꮡc);
    err = json.Unmarshal(slice<byte>(@"42"u8), Ꮡc);
    fmt.Println(err, (float64)c);
    ref var s = ref heap(new main_s(), out var Ꮡs);
    err = json.Unmarshal(slice<byte>(@"{""T"":""2021-06-07T08:09:10Z"",""C"":1}"u8), Ꮡs);
    fmt.Println(err, s.T.UTC().Format(time.RFC3339), (float64)s.C);
}

} // end main_package
