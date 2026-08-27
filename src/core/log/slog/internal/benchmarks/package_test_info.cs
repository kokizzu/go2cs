// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.log.slog.@internal.benchmarks_package;

// <ImportedTypeAliases>
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using slogꓸHandler = go.log.slog_package.ΔHandler;
global using slogꓸKind = go.log.slog_package.ΔKind;
global using slogꓸLevel = go.log.slog_package.ΔLevel;
global using slogꓸLogValuer = go.log.slog_package.ΔLogValuer;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.log.slog.@internal.benchmarks_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog.@internal.benchmarks_package.asyncHandler, go.log.slog_package.ΔHandler>(Pointer = true)]
[assembly: GoImplement<global::go.log.slog.@internal.benchmarks_package.disabledHandler, go.log.slog_package.ΔHandler>]
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
[assembly: go.GoPositionMap("log/slog/internal/benchmarks/benchmarks_test.go", "benchmarks_test.cs", "ADIiggAVDrKCAAkWgrKClAAPFgAKGAAKGAAQJAAuWpKCkoI=")]
[assembly: go.GoPositionMap("log/slog/internal/benchmarks/handlers_test.go", "handlers_test.cs", "ACEegoKSgqKCgoCCpIKCpqKCgIKkgoLKooKAgJI=")]
// </GoSourcePositionMaps>

namespace go.log.slog.@internal;

[GoPackage("benchmarks")]
public static partial class benchmarks_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
