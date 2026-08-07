// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.testing.slogtest_package;

// <ImportedTypeAliases>
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
global using slogꓸHandler = go.log.slog_package.ΔHandler;
global using slogꓸKind = go.log.slog_package.ΔKind;
global using slogꓸLevel = go.log.slog_package.ΔLevel;
global using slogꓸLogValuer = go.log.slog_package.ΔLogValuer;
// </ImportedTypeAliases>

using go;
using static global::go.testing.slogtest_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.testing;

[GoPackage("slogtest_test")]
public static partial class slogtest_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
