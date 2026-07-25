namespace go;

using fmt = fmt_package;

partial class main_package {

[GoType("@string")] partial struct relationship;

internal static readonly relationship equivalent = "equivalent"u8;
internal static readonly relationship moreGeneral = "moreGeneral"u8;
internal static readonly relationship moreSpecific = "moreSpecific"u8;

internal static readonly relationship opLoad = "opLoad"u8;
internal static readonly relationship opStore = "opStore"u8;
internal static readonly relationship opDelete = "opDelete"u8;

internal static readonly @string prefix = "op"u8;

internal static @string tag(this relationship r) {
    return "rel:"u8 + ((@string)r);
}

internal static @string describe(relationship r) {
    if (r == equivalent) {
        return "same"u8;
    }
    if (r == moreGeneral || r == moreSpecific) {
        return "related:"u8 + ((@string)r);
    }
    return "other"u8;
}

internal static void Main() {
    fmt.Println(describe(equivalent));
    fmt.Println(describe(moreGeneral));
    fmt.Println(describe(((relationship)(@string)"x"u8)));
    relationship localRel = "moreSpecific"u8;
    fmt.Println(describe(localRel));
    fmt.Println(equivalent.tag());
    @string plain = "plain"u8;
    fmt.Println(plain);
    fmt.Println(opLoad == ((relationship)(@string)"opLoad"u8), opStore == opLoad, opStore.tag(), opDelete.tag());
    fmt.Println(describe(opLoad));
    relationship localOp = "opLocal"u8;
    fmt.Println(localOp.tag(), localOp == ((relationship)(@string)"opLocal"u8));
    @string untypedOp = "opUntyped";
    fmt.Println(untypedOp, len(untypedOp));
}

} // end main_package
