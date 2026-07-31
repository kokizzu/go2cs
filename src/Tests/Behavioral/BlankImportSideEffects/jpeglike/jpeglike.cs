namespace go.BlankImportSideEffects;

using registry = go.BlankImportSideEffects.registry_package;
using go.BlankImportSideEffects;

partial class jpeglike_package {

[GoInit] internal static void init() {
    registry.Register("jpeglike"u8, "jpeglike decoder"u8);
}

} // end jpeglike_package
