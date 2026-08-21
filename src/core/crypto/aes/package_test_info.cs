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

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/aes/aes_test.go", "aes_test.cs", "AAwewoKCgpSCgsySgoKClpSCqIKCpqiSgpSCgoKCuICCAAYUwoKAgqSAgt6igoKCgoKCgoCCpM6igoKCgoKCgoKCgIKkAF/CAbKCgoKCgsqCgoKCpoKCggAtWpKCgoKClIKCgoKC3pKCgoKClIKCgoKCAAkSsoSEoJKgkqCSoJKgkqC2ooKCgqSmAAoGgoCSgJKA5qKCgpSCgoKCuIKAkoCSgLaigoKUgoKCgriCgJKAkoC2ooKCgriCgJKAkoC2ooKCgII=")]
[assembly: go.GoPositionMap("crypto/aes/modes_test.go", "modes_test.cs", "ABoygKKiooKkgqSCpILsgKKAooCigKKAppKCgIKkgoKUgIIACA6AoqKAppKCgIKkgoCCypKCgIKkgoCCAAgOooCmkoKAgqSCgII=")]
// </GoSourcePositionMaps>

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
