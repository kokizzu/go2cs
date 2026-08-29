// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.poll_package;
global using static global::go.@internal.poll_internal_test_package;

// <ImportedTypeAliases>
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.poll_test_package;

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
[assembly: go.GoPositionMap("internal/poll/error_stub_test.go", "error_stub_test.cs", "AB8egqaC")]
[assembly: go.GoPositionMap("internal/poll/error_test.go", "error_test.cs", "ADEggqKCgpS4hIKCgIIACAqCgoCCpICCpICCpICCpA==", "17-33:1")]
[assembly: go.GoPositionMap("internal/poll/fd_mutex_test.go", "fd_mutex_test.cs", "ACcggoSClIKWgpSCloKUgriCgoKWgpSClIKUgriCgoKCgpKCgpS4gqTWgoLmtoKUguiCooKCpsaCgJKAkoCUgICAkoCAgJKAgICmgoKCgoL2ooKCgpSCgpSCqIKC6KKCgoKClIKCgoKCgrKSlIKClIKClIKCxoKClpKClIKCgoLGgoKWkoKUgoKCggAKDIKCpoKUgg==", "66-72:1;98-105:1;99-103:1.1;108-108:2;109-109:3;110-110:4;112-112:5;113-113:6;114-114:7;126-138:1;159-209:1;160-162:1.1")]
[assembly: go.GoPositionMap("internal/poll/fd_posix_test.go", "fd_posix_test.cs", "ACtIgoKCgg==")]
[assembly: go.GoPositionMap("internal/poll/fd_windows_test.go", "fd_windows_test.cs", "ADA8woKEAAkOgoKEpsKChILawoKClIKUpqKCgpSCgpSUgoIACQiCgqLugoCCloLGlIKUgoIACwyigoKUkoKCgpSEgpKClKaCgpSEgAAIFKamgII=", "88-114:1")]
[assembly: go.GoPositionMap("internal/poll/read_test.go", "read_test.cs", "ABEegoKCgoKCooKCgIKCpPq4goKUyOyCgoKClIKU", "16-34:1;21-30:1.1")]
[assembly: go.GoPositionMap("internal/poll/writev_test.go", "writev_test.cs", "ABkaggAkUoKSgoI=")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("poll_test")]
public static partial class poll_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct TestConsume_tests {}
    internal partial struct _TCP_INFO_v0 {}
    internal partial struct eofErrorTestsᴛ1 {}
    internal partial struct loggedFD {}
    // </TypeAccessibility>
}
