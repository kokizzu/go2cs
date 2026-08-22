// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.errors_package;

// <ImportedTypeAliases>
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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.errors_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<MyError, error>]
[assembly: GoImplement<errorT, error>]
[assembly: GoImplement<errorUncomparable, error>(Pointer = true)]
[assembly: GoImplement<errorUncomparable, error>]
[assembly: GoImplement<multiErr, error>]
[assembly: GoImplement<poser, error>(Pointer = true)]
[assembly: GoImplement<wrapped, error>]
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
[assembly: go.GoPositionMap("errors/errors_test.go", "errors_test.cs", "AA8YlIKUgqiCgriCgoI=")]
[assembly: go.GoPositionMap("errors/example_test.go", "example_test.cs", "ABQqgqaC3IKAgvqSgoK+wpKCggAJCpKCgoKCgpSCAAcS0oCCgpQACBCigIKCgpQACBCigoKC")]
[assembly: go.GoPositionMap("errors/join_test.go", "join_test.cs", "AAsagoCCpICCpICCAA0IgoKCAAoagoKUggAJCoKCggAKGoKC")]
[assembly: go.GoPositionMap("errors/wrap_test.go", "wrap_test.cs", "ABUggoKChIS4AB9IspKAggANGoCigKKClLS0xJQAEAaCgoKCgoKEAFvAAbKUkoKigoKUgpSAggAJDIKC3IKCwoKUgoKUAAoKooKEgoL6ooKCgoIACQqCgoQABhSCgIIACQ6A/oCigPiAooAACQyCpoKC")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("errors_test")]
public static partial class errors_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct errorT {}
    internal partial struct errorUncomparable {}
    internal partial struct multiErr {}
    internal partial struct poser {}
    internal partial struct wrapped {}
    public partial interface TestAs_timeout {}
    public partial interface TestJoin_typeᴛ1 {}
    public partial struct MyError {}
    public partial struct TestAs_testCases {}
    public partial struct TestIs_testCases {}
    public partial struct TestJoinErrorMethod_type {}
    public partial struct TestJoin_type {}
    public partial struct TestUnwrap_testCases {}
    // </TypeAccessibility>
}
