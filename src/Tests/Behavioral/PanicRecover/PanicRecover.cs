namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    f();
    fmt.Println((@string)"Returned normally from f.");
}

internal static void f() => func((defer, recover) => {
    defer(() => {
        {
            var r = recover(); if (r != default!) {
                fmt.Println((@string)"Recovered in f", r);
            }
        }
    });
    fmt.Println((@string)"Calling g.");
    g(0);
    fmt.Println((@string)"Returned normally from g.");
});

internal static void g(nint i) => func((defer, recover) => {
    if (i > 3) {
        fmt.Println((@string)"Panicking!");
        throw panic(fmt.Sprintf("%v"u8, i));
    }
    deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"Defer in g", i, defer);
    fmt.Println((@string)"Printing in g", i);
    g(i + 1);
});

} // end main_package
