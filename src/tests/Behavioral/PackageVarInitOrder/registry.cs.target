namespace go;

partial class main_package {

[GoType] partial struct reg {
    internal slice<@string> entries;
    internal nint count;
}

internal static ж<reg> newReg() {
    return Ꮡ(new reg(nil));
}

[GoRecv] internal static @string add(this ref reg r, @string name) {
    r.entries = append(r.entries, name);
    r.count++;
    return name + "-added"u8;
}

internal static ж<reg> registry = newReg();

internal static slice<@string> names = new @string[]{"stdin"u8, "stdout"u8}.slice();

internal static UntypedInt chunkBits => 4;

internal static UntypedInt numChunks => /* 1 << chunkBits */ 16;

internal static UntypedInt tableSize => 37;

[GoType] partial struct holder {
    internal array<uint32> chunks = new(numChunks);
}

[GoType] partial struct table {
    internal slice<byte> codes;
}

internal static ж<table> newTable(nint n) {
    return Ꮡ(new table(codes: new slice<byte>(n)));
}

[GoType("num:uint8")] partial struct kind;

internal static kind kindNone => /* iota */ 0;
internal static kind kindFile => 1;
internal static kind kindPipe => 2;

[GoType("@string")] partial struct label;

internal static readonly label labelPipe = "pipe"u8;

} // end main_package
