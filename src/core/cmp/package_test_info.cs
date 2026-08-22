// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.cmp_package;

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static global::go.cmp_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
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
[assembly: go.GoPositionMap("cmp/cmp_test.go", "cmp_test.cs", "ADxyooKClLC0tLS0gsqCgoKUsLS0tLSCyqaCgoKClIIACQqCAAgYgoCCAAkKpoKEgoIACA7CAAgcvoI=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("cmp_test")]
public static partial class cmp_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct testsᴛ1 {}
    [GoLocalName("Order")] public partial struct ExampleOr_sort_Order {}
    public partial struct TestOr_cases {}
    // </TypeAccessibility>
}
