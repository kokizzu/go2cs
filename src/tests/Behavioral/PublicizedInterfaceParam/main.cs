namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] public partial interface describer {
    @string Describe();
}

[GoType] partial struct Label {
    internal @string text;
}

public static @string Describe(this Label l) {
    return "label:"u8 + l.text;
}

public static @string Show(describer d) {
    return d.Describe();
}

internal static void Main() {
    fmt.Println(Show(new Label(text: "A"u8)));
    fmt.Println(Show(new Label(text: "B"u8)));
}

} // end main_package
