namespace go.BlankImportSideEffects;

using registry = go.BlankImportSideEffects.registry_package;
using go.BlankImportSideEffects;

partial class pnglike_package {

[GoInit] internal static void init() {
    registry.Register("pnglike"u8, "pnglike decoder"u8);
}

} // end pnglike_package
