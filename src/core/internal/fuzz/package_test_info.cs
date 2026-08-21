// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.fuzz_package;

// <ImportedTypeAliases>
global using CorpusEntry = go.@internal.fuzz_package.CorpusEntryᴛ1;
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.fuzz_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<mockRand, global::go.@internal.fuzz_package.mutatorRand>(Pointer = true)]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
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
[assembly: global::go.GoPositionMap("internal/fuzz/encoding_test.go", "encoding_test.cs", "AB0cggCKAfgClKioAAoUspKCgoKUlIKUgoKWgoKUgoKCAAUSsoKCloKCkoKCAAUSsoKCloKCgpKCgtyCgoKCgoKUgoLKgoKCgoKClIKCyqKCgoKCgoKChIKEgoSCgpSClIKCgsqigoKCgoKCgoKChIKChIKClIKUgoLKooKChIKChIKClIKUgoI=")]
[assembly: global::go.GoPositionMap("internal/fuzz/minimize_test.go", "minimize_test.cs", "AB4qggADFIKCgoKmgpQABxCCgpSClAAHEIKClIKUAAcQgoKUAAgQgoKCgqaClAAHEIKClAAHEIKCgpTegpKSgrqCgoKClIKUgIKkggAJErKWgoKCgoKUgpSAgg==")]
[assembly: global::go.GoPositionMap("internal/fuzz/mutator_test.go", "mutator_test.cs", "AA4eooKAkoKEAAcQkoKElIKCAAgMooKAkoKEAAcQkoKElIKCAAgMooKAkoKEAA8igqKCggAIDIKCgoKCgoKClII=")]
[assembly: global::go.GoPositionMap("internal/fuzz/mutators_byteslice_test.go", "mutators_byteslice_test.cs", "ABEkgoKCpoKCgqaCgoKmgoKCpoKCgqaCpoIACgaCAHHqAZKCgpSCgoI=")]
[assembly: global::go.GoPositionMap("internal/fuzz/queue_test.go", "queue_test.cs", "AAoSpJKAgqSAgriCgoKAgqSAgqTcgoKCgoCCpJSkgoCCtoKCgII=")]
[assembly: global::go.GoPositionMap("internal/fuzz/worker_test.go", "worker_test.cs", "ABkwgoKCgpTWwoKUgoCShLqCgpSSgIK4goKEhIKCgoKE7MKClIKCgoCCAAgOwoKUgoKCgoK4goKUgpSClAADEMKCuIKUgoKCgoKClJKAgraAgqSSgIK2pqKCkoKAggAKCKKClqiSgpSCgIK2hIKCgoKCgoKCgqKCgoKClJSCgg==")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("fuzz")]
public static partial class fuzz_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
