namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct Message {
    public @string Text;
}

public static void Print(this Message m) {
    fmt.Println(m.Text);
}

[GoType("dyn")] partial interface main_Printer {
    void Print();
}

internal static void Main() {
    main_Printer p = new Message("Hello, from a function-scoped interface!"u8);
    p.Print();
}

} // end main_package
