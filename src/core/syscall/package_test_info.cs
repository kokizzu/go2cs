// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.syscall_package;
global using static global::go.syscall_internal_test_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.syscall_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Handle", "ΔHandle")]
[assembly: GoTypeAlias("Signal", "ΔSignal")]
[assembly: GoTypeAlias("Sockaddr", "ΔSockaddr")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
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
[assembly: go.GoPositionMap("syscall/exec_windows_test.go", "exec_windows_test.cs", "ABgiggAWMoKAggALCqKogpaUgoKClIKCgpSqgoKCgpSSgqqCgoKkgpSEgqaCgoKUgoKUgJI=")]
[assembly: go.GoPositionMap("syscall/syscall_test.go", "syscall_test.cs", "AA8egoKClIKClIL4gpSqooSCgoLogoKUgoCCpII=")]
[assembly: go.GoPositionMap("syscall/syscall_windows_test.go", "syscall_windows_test.cs", "ABgkgoSCgpSCgoKUlIKClOiCgoKUggAMCIKEgoKClISOlIKCgpSCgpaCuIIACAaigoKUgoKClIKCgoLWgoIADAiCgoKEhgADHoKCgpSCgoKCmAADFoKCgoKClIKCloKCloKCgoK4AAgMgoSCgpSCgriE1oKCgpS4ooKCgoKCgoSCgoKClJSEgpSC")]
[assembly: go.GoPositionMap("syscall/wtf8_windows_test.go", "wtf8_windows_test.cs", "AIUBjAKCspKCgoLcgrKSgoLcooKUlIKCuIKC3KKCgpSCgoK4goK4goI=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("syscall_test")]
public static partial class syscall_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct wtf8testsᴛ1 {}
    public partial struct TestEscapeArg_type {}
    [GoLocalName("X")] [GoValueClone("fd", "pad")] public partial struct TestWin32finddata_X {}
    // </TypeAccessibility>
}
