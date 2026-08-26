namespace go.BlankImportSideEffects;

using registry = go.BlankImportSideEffects.registry_package;
using go.BlankImportSideEffects;

partial class pnglike_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸBlankImportSideEffectsꓸregistry() {
    builtin.initPackage(typeof(go.BlankImportSideEffects.registry_package));
}

[GoInit] internal static void init() {
    registry.Register("pnglike"u8, "pnglike decoder"u8);
}

} // end pnglike_package
