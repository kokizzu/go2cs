namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() => func((defer, recover) => {
    deferǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), (@string)"Deferred result:", add(3, 4), defer);
    fmt.Println((@string)"Doing something else"u8);
});

internal static nint add(nint x, nint y) {
    nint result = x + y;
    fmt.Println((@string)"Calculate:"u8, result);
    return result;
}

} // end main_package
