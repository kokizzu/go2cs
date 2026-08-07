// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.aes_package;

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static global::go.crypto.aes_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("BlockSize", "const:ΔBlockSize")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<testAEAD, go.crypto.cipher_package.AEAD>(Pointer = true)]
[assembly: GoImplement<testBlock, go.crypto.cipher_package.Block>(Pointer = true)]
[assembly: GoImplement<testBlockMode, go.crypto.cipher_package.BlockMode>(Pointer = true)]
[assembly: GoImplement<testStream, go.crypto.cipher_package.Stream>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.crypto;

[GoPackage("aes")]
public static partial class aes_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
