// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net.rpc.jsonrpc_package;

// <ImportedTypeAliases>
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using rpcꓸCall = go.net.rpc_package.ΔCall;
// </ImportedTypeAliases>

using go;
using static global::go.net.rpc.jsonrpc_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestServerErrorHasNullResult_conn, io_package.Closer>(Promoted = true)]
[assembly: GoImplement<TestServerErrorHasNullResult_conn, io_package.ReadWriteCloser>]
[assembly: GoImplement<TestServerErrorHasNullResult_conn, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestServerErrorHasNullResult_conn, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<io_package.ReadCloser, io_package.Closer>]
[assembly: GoImplement<net_package.Conn, io_package.ReadWriteCloser>]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<net_package.Conn, io_package.Writer>]
[assembly: GoImplement<pipe, io_package.ReadWriteCloser>(Pointer = true)]
[assembly: GoImplement<pipeAddr, net_package.ΔAddr>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
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
[assembly: global::go.GoPositionMap("net/rpc/jsonrpc/all_test.go", "all_test.cs", "AF1GsoKmsoLWsoKUgqaC2qKCpqKCpqKCpoKCpqKCkoKEgoKAgqSC6KKCkoKEgoKAgqSC6KKCkoKWgoKCgoKUgpSClIIADgrGgoSCpoKCgoKUgpaCgoKClIKogoKCgoSCgpSCloKClIKogoKUgqQACwiigoSCpoKSgoKUgqiSgoKUgIK4koKClICC+IKCktaigpKEgpSCgoKCAA8IooIABBKCgIKkgoLKgpSClILogoKSqJKChAAMFIKmgqaCgoKClKaCpoLWgqaCpoI=")]
// </GoSourcePositionMaps>

namespace go.net.rpc;

[GoPackage("jsonrpc")]
public static partial class jsonrpc_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
