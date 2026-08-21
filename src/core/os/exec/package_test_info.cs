// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.os.exec_package;
global using static global::go.os.exec_internal_test_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.os.exec_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Error", "ΔError")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<badWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<delayedInfiniteReader, io_package.Reader>]
[assembly: GoImplement<go.net.http_package.HandlerFunc, go.net.http_package.ΔHandler>]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<syscall_package.ΔSignal, os_package.ΔSignal>]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<tickReader, io_package.Reader>(Pointer = true)]
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
[assembly: go.GoPositionMap("os/exec/dot_test.go", "dot_test.cs", "ABcmgpQACAailoKAgqaCgpSAgqSCgoSogoKCgoKCgoCCpIKCgIK4goKCpKaCqIKCgqSUlILMgIKkABYugoKCgIKmgIKmgoKoyoKCgIKkAAgUgoKClICCpISCgoKCpoKC")]
[assembly: go.GoPositionMap("os/exec/exec_test.go", "exec_test.cs", "ADJWgoKClIKEgpSClIKCloKCAAoS4oSCgoQACxiWgoKCgIKCyqaClpaCgoKWkoKCgpSCAAIQ0oKUrLKosoKq0oSChIKClJSokriWgoKUlgALEqKEgoKUgIKkhIKAuAAWKKKCgpSmooK4woKClIKCgoKClJKm1oKCgoKCpJSCgqSClMqCgoKClICCgsiigqaigoKCgriCgqaigpSCgoKCAAoKgoSCgpSAkgAJCIKEqIKCgpSCgoKWgoSCgpSAkgAICIKWgoKCgoKUgoLogoSCgoKUgIKkgsKCxICCpAAIBoKWgoCCpIKClIKUgviiloKC+IKWgoKClKSAgoCCtsiCloKCgoKUgoKWgoKCgpSCgpaCgoKClIKCloKCgoKCqIKCgoIAFAiChIKCuIKCgoKCgoSCgoKCgpSWgoSCgoCSpoKCgJKmgoKAkqaCggANDLKEgoKmgoKUgIakhIKCgsKEgpaAgugACBIACgKEgoKClICCqIKChLLuxrLcgoCC6ICCAAwKsoKogoKCgqaWgoKWgoKCgoKAggAIEoKCAAsKwoKWAA0cloKohIK6goKUpoKClJKCgpS4pIKCkoKCloKClIKUgoKClIKCloKEpoKAgoK4goCmhIKCtoKSgoKCAAkUlIKClIIACgiCgoKUhIKCgpSUgoKClJSCgoKUqIKClIKCgoKCgoKCkpKAkqSAkqSCgoKUgv6CgoKU2JKEABEggtqC1oKEgoKCgviChIKCgoKUgoKC6IKEgoKCgpSCgpSAgqaAgqSCgoKUlICCAAgIwoKCuoSCkoSCgpSUgIK4gIKmqIKCgoCCpqaCuoKClJaAgpQADAqShIKCgoKUgJIACAiCgpSCgoKCAAkIgoSCgpQABBKCgoCCAAsKgoSCgpaCgoCC+IKCgoLusoSCgoKUgoKWgIKkkoCCuICCpoKigoKUloSCgpSEgoK4ooKCloKCgoKChISCgoKCgoKClISCgIKCgoKkgqaCgoKSgoKmqISCgpSCgoKUlIKCuoKCkoKAgtyCggALGIKmooKAgqSCloKCpqKEgoKCgoKUqIKCgpaCgIK4goCCgoKCpIKWAA8GoorYgoKCgoKEgpSAgqKC7IKClISCgoS4gIKmgoLegpSAgqKCAAgQgoSCgoKCgt6AggAIEoKEgoKCzILegoKUhIKCgoKCqICCAAcQgoKUhIKCgoKChIKUgIKigv6CgpSEgoKCgoKEgIKmgIKilLaCAAwKoqiihIKUgoKCloKigoKUgIKkhIKCggAJEKKEgpSCgoKWgoLMgoKigoKCloCCpoKCgoKEgoKCAAgOooSClIKCgpSUgoKSgoKUgoCCpISCqILMgIIACQ6ihIKUgoKClJSCgoKClIKAgqSEgqiCzICCAAkOooSClIKCgpaCgqKClICCpIKChIKEgIIACBLiAAASAAwKgozChIKyhIKCgoKClJKEgoCCgpSm2IKyhIKEgoCC+oKCqqKEgqKCgpaCAAkGgoKokoKClIKCloKCgpaCgoK6griUgoKCgoKCgg==")]
[assembly: go.GoPositionMap("os/exec/exec_windows_test.go", "exec_windows_test.cs", "ABk0gqaigoKCgoKU1oKEgoKUgoKCgoKUgoKClIKClIKC+IKEgoKCgoKUgv6ygoaSgoKCpoSCgpSC")]
[assembly: go.GoPositionMap("os/exec/lp_windows_test.go", "lp_windows_test.cs", "ABswgqaigoKClAACENKCgpS0tMbaooKEgoCCpoKUgpQABBTygoKUlIKClJKAgriCguzCgoKUkoCCuICCAKMBtgLsuoKClrKSgpaChIKCloKCgoKCgqaUlIKEhKaCgoKCgoKUlLaAgqSogoKCgqaClIIAsQHkAuyEspKCloKEgoKEhIKCgoKUqIKCgIKUpIKUloKCgoKmgpaCgoKCpJSmggAIDIIABxSCgoSCgoKCloKC")]
// </GoSourcePositionMaps>

namespace go.os;

[GoPackage("exec_test")]
public static partial class exec_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct badWriter {}
    internal partial struct commandTest {}
    internal partial struct delayedInfiniteReader {}
    internal partial struct exeOnceᴛ1 {}
    internal partial struct lookPathTest {}
    internal partial struct tickReader {}
    public partial interface TestStdinClose_type {}
    public partial struct TestString_tests {}
    // </TypeAccessibility>
}
