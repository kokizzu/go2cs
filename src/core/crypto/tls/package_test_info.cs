// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.tls_package;
global using static global::go.crypto.tls_internal_test_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.tls_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("ConnectionState", "ΔConnectionState")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.tls_package.AlertError, error>]
[assembly: GoImplement<global::go.crypto.tls_package.Conn, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.tls_package.Conn, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.tls_package.SessionState, global::go.crypto.tls_package.handshakeMessage>(Pointer = true)]
[assembly: GoImplement<global::go.crypto.tls_package.alert, error>]
[assembly: GoImplement<go.crypto.rsa_package.PrivateKey, crypto_package.Signer>(Pointer = true)]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.crypto;

[GoPackage("tls_test")]
public static partial class tls_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
