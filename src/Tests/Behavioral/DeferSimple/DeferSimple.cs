namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() => func((defer, recover) => {
    fmt.Println((@string)"Open file"u8);
    deferǃ(ᴛ1 => fmt.Println(ᴛ1), (@string)"Close file", defer);
    fmt.Println((@string)"Write data to file"u8);
});

} // end main_package
