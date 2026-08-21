// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.expvar_package;

// <ImportedTypeAliases>
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
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
// </ImportedTypeAliases>

using go;
using static global::go.expvar_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("String", "ΔString")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.expvar_package.ΔString, global::go.expvar_package.Var>(Pointer = true)]
[assembly: GoImplement<go.net.http.httptest_package.ResponseRecorder, go.net.http_package.ResponseWriter>(Pointer = true)]
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
[assembly: go.GoPositionMap("expvar/expvar_test.go", "expvar_test.cs", "ABswwoKCgpQACAaCgoKC6IKCgoCCpIKWgoKAgqaAgqaCgILIgoSCgsqChIKC+oKCgoKUgpaCgoCCpoCCpoKAgsiChIKCyoKEgoIACgqCgoKAgqaCgJKkgJK4goCS+IKEgoIACQqCgoKCgoSCgJKCloSCgJKC6IKChIKChIKAkoKWgoCCpIKAkoKWgoKAkoKWgoKAgqSCgJKCAAgIooKEgoKCgoCCpICCpICCyoKCgoKUgoKUgoKClILoooKCgoKCgoCCpIKClIKClIK4goSEooLKooKCgoKUloKChIKygoSCggAFEtKCgpaChIKCgvqChIKEooLKgoKCgoKCgsqigoKCgpSWhIKSgoSCgoIABRLSgoKWhIKCgsqCgpKCyqKCgoKClJaChIKigoSCgtyCgqKCgJKkgIKmgoCSAAsIgoKCgoKCgpSCgoKKgIIACgiiooKCgoKClJKCgoKChIKC6MIABRSEgoKWgoKCgpSSgrKCgoKCgpSUlIKCgoKUlIKWhIKClKKCgoKCgpSCgoKCluqWsoKCgoSCgpaC6rKCgoKCgpSCgoKClpTYooKCgoSCgpaU1taigoKUgoKKgg==")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("expvar")]
public static partial class expvar_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
