namespace go;

using fmt = fmt_package;
using time = time_package;

partial class main_package {

internal static nint run(nint n) {
    var ch = new channel<nint>(128);
    var done = new channel<nint>(0);
    var chʗ1 = ch;
    var doneʗ1 = done;
    goǃ(() => {
        nint total = 0;
        foreach (var v in chʗ1) {
            total += (nint)(v & 4095);
        }
        doneʗ1.ᐸꟷ(total);
    });
    for (nint i = 0; i < n; i++) {
        ch.ᐸꟷ(i);
    }
    close(ch);
    return ᐸꟷ(done);
}

internal static void Main() {
    var start = time.Now().UnixNano();
    nint total = run(1000000);
    var elapsed = time.Now().UnixNano() - start;
    fmt.Println((@string)"checksum:"u8, total);
    fmt.Println((@string)"elapsed_ns:"u8, elapsed);
}

} // end main_package
