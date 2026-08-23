namespace go.EmbeddedInterfaceWitness;

partial class iolike_package {

[GoType] partial interface Reader {
    @string Read();
}

[GoType] partial interface ReadWriter {
    @string Read();
    @string Write(@string s);
}

[GoType] partial struct Base {
    public @string Tag;
}

public static @string Read(this Base b) {
    return "read:"u8 + b.Tag;
}

} // end iolike_package
