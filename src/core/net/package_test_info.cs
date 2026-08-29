// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net_package;
global using static global::go.net_internal_test_package;

// <ImportedTypeAliases>
global using dnsmessageꓸAAAAResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔAAAAResource;
global using dnsmessageꓸAResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔAResource;
global using dnsmessageꓸCNAMEResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔCNAMEResource;
global using dnsmessageꓸMXResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔMXResource;
global using dnsmessageꓸNSResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔNSResource;
global using dnsmessageꓸOPTResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔOPTResource;
global using dnsmessageꓸPTRResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔPTRResource;
global using dnsmessageꓸQuestion = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔQuestion;
global using dnsmessageꓸSOAResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔSOAResource;
global using dnsmessageꓸSRVResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔSRVResource;
global using dnsmessageꓸTXTResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔTXTResource;
global using dnsmessageꓸUnknownResource = go.vendor.golang.org.x.net.dns.dnsmessage_package.ΔUnknownResource;
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using socktestꓸSockets = go.net.@internal.socktest_package.ΔSockets;
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.net_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Addr", "ΔAddr")]
[assembly: GoTypeAlias("Error", "ΔError")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.net_package.Conn, io_package.Reader>]
[assembly: GoImplement<global::go.net_package.Conn, io_package.Writer>]
[assembly: GoImplement<global::go.net_package.InvalidAddrError, global::go.net_package.ΔError>]
[assembly: GoImplement<global::go.net_package.TCPConn, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.net_package.TCPConn, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.net_package.UnknownNetworkError, error>]
[assembly: GoImplement<global::go.net_package.UnknownNetworkError, global::go.net_package.ΔError>]
[assembly: GoImplement<global::go.net_package.canceledError, error>]
[assembly: GoImplement<global::go.net_package.timeoutError, global::go.net_package.ΔError>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.net_package.Resolver, ж<global::go.net_package.Resolver>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("net/pipe_test.go", "pipe_test.cs", "AC0ggsKCgoKUuIKChICCpICCpICCpICCpICCpICC", "17-24:1;19-22:1.1")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("net_test")]
public static partial class net_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
