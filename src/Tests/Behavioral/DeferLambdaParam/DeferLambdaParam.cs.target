namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() => func((defer, recover) => {
    nint count = 1;
    deferǃ((nint cnt) => {
        fmt.Println((@string)"Deferred count (closure):"u8, cnt);
    }, count, defer);
    count = 10;
    fmt.Println((@string)"Count before defer:"u8, count);
});

} // end main_package
