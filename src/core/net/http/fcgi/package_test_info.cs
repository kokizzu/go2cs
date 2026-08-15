// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net.http.fcgi_package;

// <ImportedTypeAliases>
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.net.http.fcgi_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.ReadWriter>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.net.http_package.HandlerFunc, go.net.http_package.ΔHandler>]
[assembly: GoImplement<go.net.http_package.ResponseWriter, io_package.Writer>]
[assembly: GoImplement<io_package.ReadCloser, io_package.Reader>]
[assembly: GoImplement<nilCloser, io_package.ReadWriteCloser>(Pointer = true)]
[assembly: GoImplement<nilCloser, io_package.ReadWriter>(Promoted = true)]
[assembly: GoImplement<nopWriteCloser, io_package.ReadWriteCloser>]
[assembly: GoImplement<nopWriteCloser, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<rwNopCloser, io_package.ReadWriteCloser>]
[assembly: GoImplement<rwNopCloser, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<rwNopCloser, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<signalingNopWriteCloser, io_package.ReadCloser>(Promoted = true)]
[assembly: GoImplement<signalingNopWriteCloser, io_package.ReadWriteCloser>(Pointer = true)]
[assembly: GoImplement<writeOnlyConn, io_package.ReadWriteCloser>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.net.http;

[GoPackage("fcgi")]
public static partial class fcgi_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
