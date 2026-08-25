// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.cipher_package;
global using static global::go.crypto.cipher_internal_test_package;

// <ImportedTypeAliases>
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.cipher_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<noopBlock, go.crypto.cipher_package.Block>]
[assembly: GoImplement<wrapper, go.crypto.cipher_package.Block>(Pointer = true)]
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
[assembly: go.GoPositionMap("crypto/cipher/benchmark_test.go", "benchmark_test.cs", "AA4cooKEgoKCgoKEgoK4ooKEgoKCgoKEhIKCuIKCgpSCloKUgsqihIKCgoSCggALFoKmgqaCpoKmgqaCpqKChIKCgoKCuKKChIKCgoKC")]
[assembly: go.GoPositionMap("crypto/cipher/cbc_aes_test.go", "cbc_aes_test.cs", "AEOEAYKCgoKCloSChIKCyoKCgoKCloSChIKC")]
[assembly: go.GoPositionMap("crypto/cipher/cfb_test.go", "cfb_test.cs", "ADNggoKCgpSCgpSCgpSCgpaCgpaCgoSCloKChILKgoKCgpaCgoKCgoKEgoKChII=")]
[assembly: go.GoPositionMap("crypto/cipher/cipher_test.go", "cipher_test.cs", "ABMegoKEkpCSkJSCkJKQtqKCgoKkpgANBoKCgoKUgoKWgoKCgpaCgqiCgoKChIKChIKChIKChIKChIKC")]
[assembly: go.GoPositionMap("crypto/cipher/ctr_aes_test.go", "ctr_aes_test.cs", "AD2IAYKChIKCgpaCgoKCgoCCuIKCgoKCgIK4gg==")]
[assembly: go.GoPositionMap("crypto/cipher/ctr_test.go", "ctr_test.cs", "AA8egKKAooCkgoKCgsqCgriCgoKCgoKUgoKCgoKUgoKC")]
[assembly: go.GoPositionMap("crypto/cipher/gcm_test.go", "gcm_test.cs", "AJkD7AWigoKCgpaCgoKEgpaSgsqSgpS4koLIgoLIgoCCgqaCgoKWgoKWgoKAgqSWgoCCpISCgIKkuIKEhIKCggAJCsyCgoSChIKCloKCloKWgoIACAqUAA8WgoKUgoKCgoKClIKClIKCAAoWgKKAooCoogAMBqaygoKUgoKUgoKUmJKCgpSCmISCgoKClIKCgoKUupKCgIKkgoKUgIKkgIKkgoCCpIKCgpSCgpSClJSCgoKCgoKCgIKCgoKCgg==")]
[assembly: go.GoPositionMap("crypto/cipher/ofb_test.go", "ofb_test.cs", "AEOIAYKChIKCgpaCgoKCgoKogoKCgoKCqII=")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("cipher_test")]
public static partial class cipher_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct aesGCMTestsᴛ1 {}
    internal partial struct cbcAESTestsᴛ1 {}
    internal partial struct cfbTestsᴛ1 {}
    internal partial struct noopBlock {}
    internal partial struct ofbTest {}
    internal partial struct wrapper {}
    [GoLocalName("pair")] public partial struct TestGCMAsm_pair {}
    public partial struct TestGCMCounterWrap_tests {}
    // </TypeAccessibility>
}
