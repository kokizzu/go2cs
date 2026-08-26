// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.log.slog_package;
global using static global::go.log.slog_internal_test_package;

// <ImportedTypeAliases>
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using httpꓸCookie = go.net.http_package.ΔCookie;
global using httpꓸHandler = go.net.http_package.ΔHandler;
global using httpꓸHeader = go.net.http_package.ΔHeader;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using slogꓸHandler = go.log.slog_package.ΔHandler;
global using slogꓸKind = go.log.slog_package.ΔKind;
global using slogꓸLevel = go.log.slog_package.ΔLevel;
global using slogꓸLogValuer = go.log.slog_package.ΔLogValuer;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.log.slog_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Handler", "ΔHandler")]
[assembly: GoTypeAlias("Kind", "ΔKind")]
[assembly: GoTypeAlias("Level", "ΔLevel")]
[assembly: GoTypeAlias("LogValuer", "ΔLogValuer")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<LevelHandler, go.log.slog_package.ΔHandler>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog_package.LevelVar, encoding_package.TextMarshaler>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog_package.LevelVar, encoding_package.TextUnmarshaler>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog_package.ΔLevel, encoding_package.TextMarshaler>]
[assembly: GoImplement<global::go.log.slog_package.ΔLevel, encoding_package.TextUnmarshaler>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog_package.ΔLevel, global::go.log.slog_package.Leveler>]
[assembly: GoImplement<go.log.slog.@internal.buffer_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
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
[assembly: go.GoPositionMap("log/slog/example_level_handler_test.go", "example_level_handler_test.cs", "AC8utICCpKqiqLKokqiSqJIABhYACAKCgoI=")]
[assembly: go.GoPositionMap("log/slog/example_wrap_test.go", "example_wrap_test.cs", "ACwm0oKUgoKC1oKUgqaCgpSUgg==")]
[assembly: go.GoPositionMap("log/slog/slogtest_test.go", "slogtest_test.cs", "AD4kggAGEJKCgpKCgpSUgILsgoKCgpSCgpSUpoKCgIKkAAIS4oKCgoKCgpSUgoKCgoKClIKCqJSClA==")]
// </GoSourcePositionMaps>

namespace go.log;

[GoPackage("slog_test")]
public static partial class slog_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    public partial struct LevelHandler {}
    public partial struct TestSlogtest_type {}
    // </TypeAccessibility>
}
