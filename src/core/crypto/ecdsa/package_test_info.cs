// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.ecdsa_package;
global using static global::go.crypto.ecdsa_internal_test_package;

// <ImportedTypeAliases>
global using bigmodꓸNat = go.crypto.@internal.bigmod_package.ΔNat;
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
using big = go.math.big_package;
using nistec = go.crypto.@internal.nistec_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.ecdsa_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.ecdsa_package.zr, io_package.Reader>]
[assembly: GoImplement<go.crypto.ecdsa_package.PrivateKey, crypto_package.Signer>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<bigꓸInt, ж<bigꓸInt>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.ecdsa_package.nistCurve<P224PointжnistPoint>, ж<global::go.crypto.ecdsa_package.nistCurve<P224PointжnistPoint>>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.ecdsa_package.nistCurve<P256PointжnistPoint>, ж<global::go.crypto.ecdsa_package.nistCurve<P256PointжnistPoint>>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.ecdsa_package.nistCurve<P384PointжnistPoint>, ж<global::go.crypto.ecdsa_package.nistCurve<P384PointжnistPoint>>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.ecdsa_package.nistCurve<P521PointжnistPoint>, ж<global::go.crypto.ecdsa_package.nistCurve<P521PointжnistPoint>>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/ecdsa/equal_test.go", "equal_test.cs", "ABIgooKEgpSClIKWgoKUgoKUgpSCloKClIK6goKClJSCAAkIgoCSgpSAkoCSgA==")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("ecdsa_test")]
public static partial class ecdsa_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
