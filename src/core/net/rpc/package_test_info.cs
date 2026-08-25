// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net.rpc_package;

// <ImportedTypeAliases>
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using templateꓸError = go.html.template_package.ΔError;
global using templateꓸFuncMap = go.text.template_package.FuncMap;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.net.rpc_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("ArgNotPublic", "ΔArgNotPublic")]
[assembly: GoTypeAlias("Call", "ΔCall")]
[assembly: GoTypeAlias("NeedsPtrType", "ΔNeedsPtrType")]
[assembly: GoTypeAlias("ReplyNotPointer", "ΔReplyNotPointer")]
[assembly: GoTypeAlias("ReplyNotPublic", "ΔReplyNotPublic")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<CodecEmulator, global::go.net.rpc_package.ServerCodec>(Pointer = true)]
[assembly: GoImplement<WriteFailCodec, global::go.net.rpc_package.ClientCodec>]
[assembly: GoImplement<net_package.Conn, io_package.ReadWriteCloser>]
[assembly: GoImplement<shutdownCodec, global::go.net.rpc_package.ClientCodec>(Pointer = true)]
[assembly: GoImplement<writeCrasher, io_package.ReadWriteCloser>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.net.rpc_package.Server, ж<global::go.net.rpc_package.Server>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("net/rpc/client_test.go", "client_test.cs", "ABMogKKA0oKCpIKC1oKCgoKCggALGKKCAAkGooKCgpSCpoSCgpSEgoKWgoKCloKE")]
[assembly: global::go.GoPositionMap("net/rpc/server_test.go", "server_test.cs", "ADBcsoKmsoLWooKUgqaygqaigqaCpqKCyqKCAAkSsoKmsoKmsoKCpoKCgpTmgoKCgoSCgoKEgvaCgoKCgoSCgoKEgtaCgoKmgoKCgoIAGAbCgoKUpoKCgoKUgqiCgoKClIKogoKUgqSogoKCgqSogoKCgoSCgpSCloKClIKogoKUgqSogoKCpJiSkoKCgqSogoKCgpSCgpaCgoKClIKogoKCgpSCAAkIooKClKaCgoKClILogoKCgqaigoKClJSClKaCgoKClIIACwiihIKClKaCkoKClIKogpKCgpSAgriCkoKClICCABEcwoKCgoKCgpSUgpSmooKCpoKClIKmooKUlKaCpoKCgoKmopKEgoKCgpSCloKCABEUgqaCpoKmggAKCJKCgpSCgpSCgpSCgqQACAyUpoLGgsaC1qKClIKigoKUtKT4ooKUgoLWgqaC1qKCgoKUlIKCsoKClIIACwqCgpSClKaCgpSClO6CpoKC1oL2ooKClJKCgpSClAAIBqKEgoKUlIKCgoKUgoIACQiChIKClIKClIKCupKCgoKEgoKC1oKCgoLCkoKClMSCgpSCgpaCgpSCgoKCgt6CgoKCgoLowoKCgpSmgoSigoKCgpSCAAgMwoKUgoKCgpSmgoKSkoKCgoKEgsKCgoKmooKCgoKCgpSCgqam1oKmgqaCpoI=")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("rpc")]
public static partial class rpc_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
