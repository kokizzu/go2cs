namespace go;

using fmt = fmt_package;
using time = time_package;

partial class main_package {

[GoType("num:nint")] partial struct counter;

internal static nint Len(this counter c) {
    return (nint)c;
}

[GoType] partial struct box {
    internal nint n;
}

[GoRecv] internal static nint Len(this ref box b) {
    return b.n;
}

[GoType("dyn")] partial interface run_type {
    nint Len();
}

[GoType("dyn")] partial interface run_typeᴛ1 {
    nint Len();
}

internal static nint run(nint n) {
    var values = new any[]{((counter)3), Ꮡ(new box(n: 5))}.slice();
    nint total = 0;
    for (nint i = 0; i < n; i++) {
        {
            var (v, ok) = values[0]._<run_type>(ᐧ); if (ok) {
                total += v.Len();
            }
        }
        {
            var (v, ok) = values[1]._<run_typeᴛ1>(ᐧ); if (ok) {
                total += v.Len();
            }
        }
    }
    return total;
}

internal static void Main() {
    var start = time.Now().UnixNano();
    nint total = run(5000000);
    var elapsed = time.Now().UnixNano() - start;
    fmt.Println((@string)"checksum:"u8, total);
    fmt.Println((@string)"elapsed_ns:"u8, elapsed);
}

} // end main_package
