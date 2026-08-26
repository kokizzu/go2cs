// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.x509_package;
global using static global::go.crypto.x509_internal_test_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
global using execꓸError = go.os.exec_package.ΔError;
global using netipꓸAddr = go.net.netip_package.ΔAddr;
global using netipꓸPrefix = go.net.netip_package.ΔPrefix;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
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
global using runtimeꓸError = go.runtime_package.ΔError;
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tlsꓸConnectionState = go.crypto.tls_package.ΔConnectionState;
global using urlꓸError = go.net.url_package.ΔError;
using ecdsa = go.crypto.ecdsa_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.x509_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.x509_package.OID, encoding_package.BinaryMarshaler>]
[assembly: GoImplement<global::go.crypto.x509_package.OID, encoding_package.BinaryUnmarshaler>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.x509_package.OID, encoding_package.TextMarshaler>]
[assembly: GoImplement<global::go.crypto.x509_package.OID, encoding_package.TextUnmarshaler>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.x509_package.SystemRootsError, error>]
[assembly: GoImplement<go.crypto.ecdsa_package.PrivateKey, crypto_package.Signer>(Pointer = true)]
[assembly: GoImplement<go.crypto.ed25519_package.PrivateKey, crypto_package.Signer>]
[assembly: GoImplement<go.crypto.rsa_package.PrivateKey, crypto_package.Signer>(Pointer = true)]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<ecdsa.PrivateKey, ж<ecdsa.PrivateKey>>]
[assembly: GoImplicitConv<global::go.crypto.x509_package.CertPool, ж<global::go.crypto.x509_package.CertPool>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.x509_package.Certificate, ж<global::go.crypto.x509_package.Certificate>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.x509_package.Certificate, ж<global::go.crypto.x509_package.Certificate>>]
[assembly: GoImplicitConv<global::go.crypto.x509_package.CertificateRequest, ж<global::go.crypto.x509_package.CertificateRequest>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/x509/hybrid_pool_test.go", "hybrid_pool_test.cs", "AGEqgoKClIKUAAoYgoKCgoKClIKClMyCgpSEAAcQgoKUgoKUgoKWgoKUhIKCloSCgpbcgoKUgoKWgoI=")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("x509_test")]
public static partial class x509_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
