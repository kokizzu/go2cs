// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.sync_package;
global using static global::go.sync_internal_test_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.sync_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<DeepCopyMap, mapInterface>(Pointer = true)]
[assembly: GoImplement<RWMutexMap, mapInterface>(Pointer = true)]
[assembly: GoImplement<sync_package.Map, mapInterface>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

[GoPackage("sync_test")]
public static partial class sync_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface mapInterface {}
    internal partial struct bench {}
    internal partial struct httpPkg {}
    internal partial struct mapCall {}
    internal partial struct mapOp {}
    internal partial struct mapResult {}
    internal partial struct misuseTestsᴛ1 {}
    internal partial struct one {}
    [GoLocalName("PaddedMutex")] [GoValueClone("pad")] public partial struct BenchmarkMutexUncontended_PaddedMutex {}
    [GoLocalName("PaddedRWMutex")] [GoValueClone("pad")] public partial struct BenchmarkRWMutexUncontended_PaddedRWMutex {}
    [GoLocalName("PaddedSem")] [GoValueClone("pad")] public partial struct BenchmarkSemaUncontended_PaddedSem {}
    [GoLocalName("PaddedWaitGroup")] [GoValueClone("pad")] public partial struct BenchmarkWaitGroupUncontended_PaddedWaitGroup {}
    public partial struct DeepCopyMap {}
    public partial struct RWMutexMap {}
    [GoLocalName("X")] public partial struct TestWaitGroupAlign_X {}
    // </TypeAccessibility>
}
