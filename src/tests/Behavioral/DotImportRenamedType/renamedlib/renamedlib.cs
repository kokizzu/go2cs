namespace go.DotImportRenamedType;

using fmt = fmt_package;

partial class renamedlib_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

[GoType] partial struct ΔMarker {
    public @string Name;
    public nint Size;
}

public static @string Marker(this ΔMarker m) {
    return fmt.Sprintf("%s/%d"u8, m.Name, m.Size);
}

[GoType] partial struct ΔDetail {
    public @string Label;
    public nint Rank;
}

public static @string Show(this ΔDetail d) {
    return fmt.Sprintf("%s#%d"u8, d.Label, d.Rank);
}

public static ΔDetail Detail(this ΔMarker m) {
    return new ΔDetail(Label: m.Name, Rank: m.Size);
}

[GoType] partial struct Plain {
    public @string Note;
}

public static any Describe(@string name, nint size) {
    return new ΔMarker(Name: name, Size: size);
}

public static any Wrap(@string label, nint rank) {
    return new ΔDetail(Label: label, Rank: rank);
}

public static any Boxed(@string note) {
    return new Plain(Note: note);
}

} // end renamedlib_package
