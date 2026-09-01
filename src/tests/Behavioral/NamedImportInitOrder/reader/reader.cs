namespace go.NamedImportInitOrder;

using store = go.NamedImportInitOrder.store_package;
using writer = go.NamedImportInitOrder.writer_package;
using go.NamedImportInitOrder;

partial class reader_package {

internal static @string captured;

[GoInit] internal static void init() {
    captured = store.Value;
}

public static @string Describe() {
    return writer.Name;
}

public static @string Captured() {
    return captured;
}

} // end reader_package
