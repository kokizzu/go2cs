// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.net.textproto_package;

// <ImportedTypeAliases>
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.net.textproto_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Error", "ΔError")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<net_package.Conn, io_package.ReadWriteCloser>]
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
[assembly: go.GoPositionMap("net/textproto/header_test.go", "header_test.cs", "ACJIgoKAggAIDJKmgoKC")]
[assembly: go.GoPositionMap("net/textproto/reader_test.go", "reader_test.cs", "ABImgtaCgoKClIKClIKCuIKCgoKClILogoKCgpSCgpSCgpSCguiCgoKClIKClIKClICCpIKC6IKCgoKCloKCguiCgoKCgpaCgoLogoKCuILogoKCgoIACAyiABYygoKC+oKCgoKC6IKCgpSCgoKClIKCvLSKgu6CuKIADRyCgoCC2qKCgoKClLS0tLa0xIKCpoKCgoKUtLS0tIKCzOiegoKUyoK6koKCgoKSgoKClIK4gIIAIlCSgoKCgoKUgpSCzJKMjIKClIKUgpSC6IKCgoKmgoKSgIK2gujMgqqCpJSWhIIAEDqigu6SgoKEgoKAggAMDqKCgoKCgoKCgpSAgg==")]
[assembly: go.GoPositionMap("net/textproto/writer_test.go", "writer_test.cs", "ABAagoKCgoCC+IKCgoKCgpSCgoCCyIKCgoKCgpSCgoCCyIKCgoKCgoCC")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("textproto")]
public static partial class textproto_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
