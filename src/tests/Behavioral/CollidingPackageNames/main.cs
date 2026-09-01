namespace go;

using fmt = fmt_package;
using dupmeta = collidea.dup_package;
using dup = collideb.dup_package;
using collideb;

partial class main_package {

internal static void Main() {
    fmt.Println(dupmeta.Greeting());
    fmt.Println(dup.Marker());
}

} // end main_package
