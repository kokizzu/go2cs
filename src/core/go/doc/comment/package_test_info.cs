// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.go.doc.comment_package;

// <ImportedTypeAliases>
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using bytes = go.bytes_package;
// </ImportedTypeAliases>

using go;
using static global::go.go.doc.comment_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Text", "ΔText")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.go.doc.comment_package.Doc, ж<global::go.go.doc.comment_package.Doc>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.go.doc.comment_package.LinkDef, ж<global::go.go.doc.comment_package.LinkDef>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.go.doc.comment_package.ListItem, ж<global::go.go.doc.comment_package.ListItem>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/doc/comment/old_test.go", "old_test.cs", "ACREgoKCACdIgoKCgg==")]
[assembly: global::go.GoPositionMap("go/doc/comment/parse_test.go", "parse_test.cs", "AAoUkg==")]
[assembly: global::go.GoPositionMap("go/doc/comment/std_test.go", "std_test.cs", "ABcegoKCloKCgqaEgoKC")]
[assembly: global::go.GoPositionMap("go/doc/comment/testdata_test.go", "testdata_test.cs", "ABskgoKClIK4goKUlIKGlJbKlIKSgoKClIKCgqaClIKCgoKUgpikpKSkAAITAAIYgu6CgoKmgqS2goKCgoKUtoKCyLaCgsiCtoK2goLIgraCtoK2goLIgpSCyIKUgsiCgoLagraCyKKCgg==")]
[assembly: global::go.GoPositionMap("go/doc/comment/wrap_test.go", "wrap_test.cs", "ABckgoKUgpaCgoKCgoKUgpSWkpKCgpKSloKCgpSClIKClIKUgoKUtqSUlIKogoIABxgACgqCgoKClIKUgoKCgoKClIKClIKCgsyCgpSCgoKU")]
// </GoSourcePositionMaps>

namespace go.go.doc;

[GoPackage("comment")]
public static partial class comment_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
