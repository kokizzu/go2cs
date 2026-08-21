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

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("sync/cond_test.go", "cond_test.cs", "ABIcgoKCgoKCgrKCgoKCpoKUgqTWgoKCgqTWlKaCgoKCgoKCsoKCgoKUgoKCpoKCgviCgoKCgoKCgrKCgoKClKaCgpSCgoKUpNaCgoKCgoKClKak1uaCgoKCooKCgoKUgoKClKKCgoKCgpSCgpSClKKCgoKCgpSUgpSCgpSClIKCpqKCgpaCooKCgoSWgoIACRaCkpaSgoKUqKiCgoK4ooKCgqaSgoKC1oKmgqaCpoKmgqaCpoKCgoSCooKCgoKUgoKClJSUgoKCgqaC")]
[assembly: go.GoPositionMap("sync/example_pool_test.go", "example_pool_test.cs", "ABYykqaCgpSCgoKCgoLmgg==")]
[assembly: go.GoPositionMap("sync/example_test.go", "example_test.cs", "AA8ezKKCypSUtJTo1oKCgpSCgqKCpoIACBDSgoKClIKUgoKigoKClKaCAAgOwoKClIKCooKClIKmgg==")]
[assembly: go.GoPositionMap("sync/map_bench_test.go", "map_bench_test.cs", "ABQogqKigoKWhIKigtyClJSCpoK6gtyClJSCpoK6ggAIDIKUlICCpIKmgrqCgoKAgraAggAIEIKUgILKgtyClKiC3IKUlICCpIKmgrqCgoKU7oKUgILKgtyClKiCgIL+goSUgrqCAAUW0pSSgoKAgoKCAAcY0oSUgrqChIKCgpTugpSogtyClKiC3IKUlIKmgrqCgoKUgu6ClJSCpoK6goKClILugpSogoLugpSCgu6ClKiC3IKUlICCpoKmgrqCgoKU3IKUlIKmgrqCgoKU3IKUqIKC7oKUlICCpoKmgrqCgoKUgu6ClJSCpoK6goKClILugpSCkoI=")]
[assembly: go.GoPositionMap("sync/map_reference_test.go", "map_reference_test.cs", "ACVOwoKCgqaigoKUgqbCgoKCgoKUlIKmwoKCloKCgqbCgoKCgpSCgqaigoKm0oKCgpaCgoKU5tKCgoKWgoKClOaigoKClISCgoKUgsrCgoQADRaygoKmgoKCgoKmwoKCgpaUgoKCgoKClIKmsoKCgoKCgqaygoKCgoKCpoKCgoKCprKCgIKmgoKCgoKCgpTmsoKAgqaChIKCgoKClOaCgoKCyoKCgoKUpqKChA==")]
[assembly: go.GoPositionMap("sync/map_test.go", "map_test.cs", "ADNkgpSkgqSkpIKkpIKClKSCgIK2pIKkAAkSooKClKaCgpSkprKCgpaCkoKWpoKmgqaCpoKAgsiCgILIooSCgpaCgpKClIKCgtKCgqTGgoKUAAgOgoKUgoSSgoKUgpSCloL6grqEqIKCgpSCgviygoKUgqaAgtyAgu6CgoCCpKiCuoKCgpaCuIKClLiSgoKCmIK8ooSSloKiguqCooKAgpQACAyCooLYhISChLiCgoKClII=")]
[assembly: go.GoPositionMap("sync/mutex_test.go", "mutex_test.cs", "ABYqgoKClKaCgoKCgpSCuKKCgqbCgoKCgoKEgoKC1oKCgoKUlIKUpqKAgqSEhIKClIKClISCgpSCAA8WgtyCgoLcgtyCgtyCgoLcgtyCgtyCgoLKgoKygrKAksSCpoL4goKCgoLKwoKCgpKCgoKCpOqCkoKCgpSU5gAMCIKKgoKCyqKCgpSCgoKCgoKCgri4gqaCpoKmgqau4pKCgoKCgoKCgoKUgriSlNyKwpKCgoKCgoKCgg==")]
[assembly: go.GoPositionMap("sync/once_test.go", "once_test.cs", "AA4cgqaygJKAgqSmgoKCgoKClIKUgriCgqKCgIK2gtiCuIKCkpKC")]
[assembly: go.GoPositionMap("sync/oncefunc_test.go", "oncefunc_test.cs", "ABMokoKAkoKClIK4goKCgpSQkoKClIKUgriCgoKClJCSgoKUgpSCuIKCgsrGgoKCooKUgsSClJSCuIKSgoKUpoKSgoKUkLaCkoKClJC2gpKCgpSCpKS4xoKCgpSCgoKygoCSxJSC6MaEgoCCpIKCgqbWguaCpJC2kICSkLaQgJKQtoKSgoKClIKCgpSCgqaUzJKCqAAJEIIACAaCgoKUpoKCuKaCuJKCAAkYgoKUppSCgoKAksiCgoKAksiCgoKCgJI=")]
[assembly: go.GoPositionMap("sync/pool_test.go", "pool_test.cs", "ABkq1IKCgrqCgoKAgqSAgqSAgqSogqaCgIK2goCC+NSEgqSCpoCCpICCyoKCgIKkhICC+pKokqaCgoKCgoKUkoKCgpSUgoKmgoKUgIK22IKCgoKUgoKCkoKCgpSCgoKCpqaCuIKmgqaCgoKClIKCgpKCgrqCgpKCgoKCgriAgsi6goKSgpSUgoKCgriUloKCAAcQggAJCIKigtiCwpKClMTCkoLogoKCgoLKgoKCgoKUggAFEKKCpoKCgoKUggAMENSEgoSChJKCgqaUgqiCgoKmgoIACQYACAyCgJaSgoKCgpSSgqaCgpSCppSCgqaUhII=")]
[assembly: go.GoPositionMap("sync/runtime_sema_test.go", "runtime_sema_test.cs", "ABEagoqCgoLKwoKUkoKCkoKUlJKmgoKCgoKCgqaUguiCpoKmgqaC")]
[assembly: go.GoPositionMap("sync/rwmutex_test.go", "rwmutex_test.cs", "ABEoooKCgoKmgoKCgoKCgqaClIKmgriigoKC1oKCgoKCgpSUgpSmgoKCgoKClJSClKaChJKCgoKCgpSCgqaCuKKEgoKUgpSEgpSEgpSClIKUgoSCgoKUgoKCgoKCgoKCAAkGgoKCgoKCgrKCgoKCgqaCgoKk1oKCpNYACAiCioKCgoKCgoLKgoKCgoKCgoKUgoKClKa4gqaCpoKmgg==")]
[assembly: go.GoPositionMap("sync/waitgroup_test.go", "waitgroup_test.cs", "ABEagoKCgoKCkoKCpoKCpNaUgriCgpaC6KKCgoKmgoKCggAIBpSCgpSCooKmgqKCpoKCAAkKgoqCgpT2goqCgoKCyoKCgoKCgoKClJS4gqaCpoKCgoKCgoKCpriCpoKmooKCgoKCgpQ=")]
// </GoSourcePositionMaps>

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
