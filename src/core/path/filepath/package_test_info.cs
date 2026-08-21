// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.path.filepath_package;
global using static global::go.path.filepath_internal_test_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
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
// </ImportedTypeAliases>

using go;
using static global::go.path.filepath_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
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
[assembly: go.GoPositionMap("path/filepath/match_test.go", "match_test.cs", "AFioAYKClKaCgoKCgpSUgpSCggAPHIKCgoKCgpSCgoKUgqaCgoKClILKuIKCuIKCgoCC2qYADBaChIKCgoKCgpSAgqSCgpSUlIKClIIACRSCgoKUgqaCgoKClIKCgpSmgoKCgpSCgoKmgoKUpqKCloKClIKWyqaCgoKmgoKCqAAMHKKCgIK2goKAgraCgoKCgILKgoKUgoKUgoKCpqKCgpSCgpSCggAKCoKClIKCgoKUgg==")]
[assembly: go.GoPositionMap("path/filepath/path_test.go", "path_test.cs", "AHv6AYKCgoKUlJSCgIKkgIK4gpSCgpaykJKCAEWKAYKCgpSClIKAggAyXoKClKSCgrakgoKCgpSCAA0cgoKAgqSAggAmUIKCgpSCgIIAHjyCgoKClIKAggBIlgGCgpSUgoKAggARIoKCgIIAI0qigoK4goKCgoKClJTKgIC0goKClL7SgoKCpoKCgpSUqqKCgpSAgqaCgIKCAAgKgoKClIKClICCpIKAgqS8ooSCgpamgoqmggAJBqKCgqaEgoKUgIKkhIKSgpiCgpSClIKE2oKUgpSCqIKogpSCgoKClIKmgqiClIKCgoKClIKmgpaC6IKCgpSAggANCIKEgIKkgoSCgoKUgpSUkpSCgoKCgpSCqKKSgpSikoIACwiChICCpICCpoKCgoKChIKCgpaIlJaSlIKCgoCCpIKoopKClKKSggAJCKKEgoKCgIKkgoKClIKSgpSUgpKCgpSClAABEIIAEgiChIKCgIKkhIKAgqaCgIKmgoCCAA0cACFMkpKCgoKUgoKUgpaCgoKUAB9AgoKUgqaUgoCCACRIgoKUgqaUlIKAggAoUIKCgpSCpoKmloKAggAuYKKmgoKCgpSCuKKCgpSCgoKogoKWgoKClIIACQiChIiigoKogoKCgpSUgrqChIKClJaWgoKogoKUzOiihISCgpaCgpSEgoIAEAiChISCgoKUgoKClIKCgpSCgoKUgoKCqIKClIKEAAYSgoKClILMkpaCzICCpISCgpSCgpSClAAhOsKCgoKUgoKUhIKCgsyCgoKCooKUgpSWgoKWooKCgoKWgoKClIKClIKUggAHEPKEgoKUgoKUhIKCloKClIKClIKUggA/fIKCgoKUlIKCgoKUlIKUggA3cIKClIKAgtqCgpSCgpSCgoKClIKClIIACgiSgpSCgoKCgoKClpSCpIKUpJSClIK4ooSCgpSEgoKWgoKWgoKClIKClIKUgpSCgoLogoLWgoSCgoKWggAHEoKCgsyShISEgoKWgIKkgIKkgIK4goKCgpaWgoKClsS0tOyyhIKClIKEgoCCpICCpICCpoKAgqSAgqaCgoCCpJQACwiigoKAgraCgIKkgoKCgpSCgoKUlJSClIKC6IKCgoSmgpSCgpSCgpSC+oKCgoKClIKC")]
[assembly: go.GoPositionMap("path/filepath/path_windows_test.go", "path_windows_test.cs", "AB4sgoKCloLohpqSgoKUgISCpIKAgoKkgIKCpJKAgoLKhIKClILcgpSCpIKmkoL8goSEgpSClJaCgoKCgpSUgqiEhIS6uqqigt6CgoKClIKCgpSCgqaCgoKClIKCpoKCgoKUgoIADS4AFAaUgpSCgriCgpSUgpTWgoKClLiSlIKClJSCpv7CgpSCgpaCgpSCgpSCgoKUgoKmgoKUABYGooKChIKWgpaCkpSClqgAECqCgoKUgoKkqIQAG0qCgIKmgoKUgoKCqIKCloSEgoKChIKCgoSmgoKEpoKUgoKk+uaCAAkGgoKClIKCgpS4goKmgoIACQaigoKCgoKmuLqCAAkWgoKUgpSUAA0MpoKUhIKCloKCgoSCgoKUgoKUgrjGgpSEgoKCgpaChIKClIL4goKCloSCgoKUhIKCgpaCgpSCgoKUppSCqISCgoKWhIKCgpaCgpaClJSCAAgIggAMIoKCggAJCoIACBiCgg==")]
// </GoSourcePositionMaps>

namespace go.path;

[GoPackage("filepath_test")]
public static partial class filepath_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct globSymlinkTestsᴛ1 {}
    internal partial struct globTest {}
    internal partial struct globTestsᴛ1 {}
    public partial struct EvalSymlinksTest {}
    public partial struct ExtTest {}
    public partial struct IsAbsTest {}
    public partial struct IsLocalTest {}
    public partial struct JoinTest {}
    public partial struct LocalizeTest {}
    public partial struct MatchTest {}
    public partial struct Node {}
    public partial struct PathTest {}
    public partial struct RelTests {}
    public partial struct SplitListTest {}
    public partial struct SplitTest {}
    public partial struct TestAbsWindows_type {}
    public partial struct TestIssue13582_tests {}
    public partial struct TestIssue52476_tests {}
    public partial struct TestToNorm_tests {}
    public partial struct TestToNorm_testsDir {}
    public partial struct TestWalkSymlinkRoot_type {}
    public partial struct VolumeNameTest {}
    // </TypeAccessibility>
}
