namespace go.ForeignIfaceFieldPointer;

partial class addrlib_package {

[GoType] partial interface Addr {
    @string Network();
    @string String();
}

[GoType] partial struct UnixAddr {
    public @string Name;
    public @string Net;
}

[GoRecv] public static @string Network(this ref UnixAddr a) {
    return a.Net;
}

[GoRecv] public static @string String(this ref UnixAddr a) {
    return a.Name;
}

} // end addrlib_package
