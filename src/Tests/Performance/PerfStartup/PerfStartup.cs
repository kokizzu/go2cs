namespace go;

using fmt = fmt_package;

partial class main_package {

internal static void Main() {
    fmt.Println((@string)"checksum:"u8, (nint)(42));
    fmt.Println((@string)"elapsed_ns:"u8, (nint)(0));
}

} // end main_package
