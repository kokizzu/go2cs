// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.hash.maphash_package;
global using static global::go.hash.maphash_internal_test_package;

// <ImportedTypeAliases>
global using runtimeꓸError = go.runtime_package.ΔError;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.hash.maphash_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Seed", "ΔSeed")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.hash.maphash_package.Hash, hash_package.Hash64>(Pointer = true)]
[assembly: GoImplement<global::go.hash.maphash_package.Hash, hash_package.Hash>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.hash;

[GoPackage("maphash_test")]
public static partial class maphash_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
