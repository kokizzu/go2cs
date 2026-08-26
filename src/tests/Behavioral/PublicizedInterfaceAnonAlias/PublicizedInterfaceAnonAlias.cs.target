global using entry = go.main_package.entryᴛ1;

namespace go;

using fmt = fmt_package;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType("dyn")] public partial struct entryᴛ1 {
    public nint ID;
    public @string Name;
}

[GoType] public partial interface deps {
    entry Process(entry e);
}

[GoType] partial struct impl {
}

internal static entry Process(this impl _, entry e) {
    e.ID++;
    e.Name = e.Name + "!"u8;
    return e;
}

public static entry Run(deps d) {
    return d.Process(new entry(ID: 1, Name: "seed"u8));
}

internal static void Main() {
    var r = Run(new impl(nil));
    fmt.Println(r.ID, r.Name);
}

} // end main_package
