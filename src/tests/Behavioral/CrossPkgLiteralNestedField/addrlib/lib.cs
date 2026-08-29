namespace go.CrossPkgLiteralNestedField;

partial class addrlib_package {

public static UntypedInt PathMax => 12;

[GoType] partial struct rawAddr {
    public uint16 Family;
    public array<int8> Path = new(PathMax);
}

[GoType] partial struct Addr {
    public @string Name;
    internal rawAddr raw;
}

[GoRecv] public static nint Capacity(this ref Addr a) {
    return len(a.raw.Path);
}

[GoRecv] public static (nint, bool) Encode(this ref Addr a) {
    nint n = len(a.Name);
    if (n > len(a.raw.Path)) {
        return (0, false);
    }
    a.raw.Family = 1;
    for (nint i = 0; i < n; i++) {
        a.raw.Path[i] = (int8)a.Name[i];
    }
    return (2 + n + 1, true);
}

[GoRecv] public static int8 PathByte(this ref Addr a, nint i) {
    return a.raw.Path[i];
}

[GoType] partial struct slots {
    public array<int32> Cells = new(4);
}

[GoType] partial struct Embedder {
    public @string Name;
    internal partial ref slots slots { get; }
}

[GoRecv] public static nint Slots(this ref Embedder e) {
    return len(e.Cells);
}

[GoRecv] public static int32 Put(this ref Embedder e, nint i, int32 v) {
    e.Cells[i] = v;
    return e.Cells[i];
}

} // end addrlib_package
