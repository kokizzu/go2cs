// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.sync.atomic_package;

// <ImportedTypeAliases>
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
using static global::go.sync.atomic_test_package;

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
[assembly: go.GoPositionMap("sync/atomic/atomic_test.go", "atomic_test.cs", "ACdGgoqCgoKCgoKUlIIACQiCioKCgoKCgpSUggAJCIKKgoKCgoKClJSCAAkIgoqCgoKCgoKUlIIACQiCioKCgoKCgoKUlIIACQiCioKCgoKCgoKUlIIACQiCioKCgoKCgoKUlIIACQiCioKCgoKCgoKUlIIACQiCioKCgoKCgoKClJSCAAkIgoqCgoKCgoKCgpSUguyClIKmlIIACAaCioKCgoKEgoKClJSCAAkIgoqCgoKCgoKCgoKUlIIACQiCioKCgoKCgoKmggAJCIKKgoKCgoKCgqaCAAkIgoqCgoKCgoKCpoIACQiCioKCgoKCgoKmggAJCIKKgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCpoIACQiCioKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCpoIACQiCioKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKCpoIACQiCioKCgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCpoIACQiCioKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKmggAJCIKKgoKCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKCpoIACQiCioKCgoKClIKUgoKUgqaCAAkIgoqCgoKCgpSClIKClIKmggAJCIKKgoKCgoKUgpSCgpSCpoIACQiCioKCgoKClIKUgoKUgqaCAAkIgoqCgoKCgoKUgpSCgpSCpoIACQiCioKCgoKCgpSClIKClIKmggAJCIKKgoKCgoKClIKUgoKUgqaCuIIACAaCioKCgoKCgpSClIKClIKmggAJCIKKgoKCgoKCgpSClIKClIKmggAJCIKKgoKCgoKCgpSClIKClIKmggAJCIKKgoKCgoKCgoKUgpSClIKmggAJCIKKgoKCgoKCgoKClIKUgpSCpoIACQiCioKCgoKClJSCAAkIgoqCgoKCgoKUgpSCAAkIgoqCgoKCgpSUggAJCIKKgoKCgoKClIKUggAJCIKKgoKCgoKClJSCAAkIgoqCgoKCgoKClIKUggAJCIKKgoKCgoKClJSCAAkIgoqCgoKCgoKClIKUggAJCIKKgoKCgoKCgpSUggAJCIKKgoKCgoKCgoKUgpSCAAkIgoqCgoKCgoKCgqaCAAkIgoqCgoKCgoKCgoKmggAJCIKKgoKCgoKClJSCAAkIgoqCgoKCgoKUlIIACQiCioKCgoKCgpSUggAJCIKKgoKCgoKClJSCAAkIgoqCgoKCgoKClJSCAAkIgoqCgoKCgoKClJSCAAkIgoqCgoKCgoKClJSCAAkIgoqCgoKCgoKClJSCAAkIgoqCgoKCgoKCgpSUggAJCIKKgoKCgoKCgoKUlIIACQiCioKCgoKCgoKmggAJCIKKgoKCgoKCgoKmggAjTIKClIKCgoKCuKKCgoKCgoLKooKCgoKCgsqigoKCgoLKooKCgoKCgsrGgoKCgoKCysaCgoKCgoLKgoKCuIKCgriCgriCgoK4poKCuKaCgriCgoKCgoLcgoKCgoKC3IKCgoKC3IKCgoKCgtymgoKCgoLcpoKCgoKCAAgMooKCgpSEgoKCgsKSgIKklNaClIIAHDaCgpSCgoKCgriigoKCgoKCyqKCgoKCgoLKooKCgoKCyqKCgoKCgoLuxoKCgoKCgoLcxoKCgoKCgoLcgoKCuIKCgriCgriCgoK4poKCuKaCgriCgoKCgoLcgoKCgoKC3IKCgoKC3IKCgoKCgtymgoKCgoLcpoKCgoKC3KKCgoKUhIKCgoLCkoCCpJTWgpSC+oKCgoKCgpSCgpSmgoKCgoKClIKClKaCgoKCgoKUgoKUpoKCgoKCgpSCgpSmgoKCgoKClIKmgoKCgoKClIKmgoKCgoKClIKmgoKCgoKClIKmgoKCgoKCgoKUgoKmgoKClIKUqJKCgoKCgoKClIKCpoKCgpSClK7CgoKCgoKCgpSCgqaCgoKUgpSuwoKCgoKCgoKUgoKmgoKClIKUpsIABxCCgpSCppSCgoKCgqKClKaC+qKClIKCgpSCkpKCkoKCgoKCgoKmgoKClIKClJSmgtaigpSCgoKUgpKSgpKCgoKCgoKCpoKCgpSCgpSUpoIADQaigpSCgoKUggAAEIKSgoKCgpSCgqaCgoKCuKaCAA0GooKUgoKClIIAABCCkoKCgoKUgoKmgoKCgrimggAIBqKUhIKCgqKCtgAPBriCloKEkJKQkpCSkJKQkpCSkJKQAA0GgoiAgqSAgqaIgIKkgILIgpKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKioqKkgrKCgpQ=")]
[assembly: go.GoPositionMap("sync/atomic/example_test.go", "example_test.cs", "AAwagqaCqqKUgqaCgsqCgoKUAAgQooSClIKCpqKCgoKCgpSC+g==")]
[assembly: go.GoPositionMap("sync/atomic/value_test.go", "value_test.cs", "ABQigoKClIKCgIKkgoKAggAICIKCgoKAgqSCgoCCyIKCgoKigoKCpsSCooKCgqbEooKCgqbogtyCgoKClIKCgoKigoKCgoKCgoKCpoKCtKaCgtyCgoKCgoKCABMkgrKygoKUkoKUtMaAgqSAggAJDIKCgoKSgoKUgoKCgoKCgIK2gqaCgIIAHi6CsrKCgpSSgpS0xoCCAAkMgoKCgpKCgpSCgoKCgoKmpoKAgg==")]
// </GoSourcePositionMaps>

namespace go.sync;

[GoPackage("atomic_test")]
public static partial class atomic_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct heapAᴛ1 {}
    internal partial struct testCompareAndSwapUint64_x {}
    public partial struct ExampleValue_readMostly_Map {}
    public partial struct List {}
    public partial struct TestAddInt32Method_x {}
    public partial struct TestAddInt32_x {}
    public partial struct TestAddInt64Method_x {}
    public partial struct TestAddInt64_x {}
    public partial struct TestAddUint32Method_x {}
    public partial struct TestAddUint32_x {}
    public partial struct TestAddUint64Method_x {}
    public partial struct TestAddUint64_x {}
    public partial struct TestAddUintptrMethod_x {}
    public partial struct TestAddUintptr_x {}
    public partial struct TestAndInt32Method_x {}
    public partial struct TestAndInt32_x {}
    public partial struct TestAndInt64Method_x {}
    public partial struct TestAndInt64_x {}
    public partial struct TestAndUint32Method_x {}
    public partial struct TestAndUint32_x {}
    public partial struct TestAndUint64Method_x {}
    public partial struct TestAndUint64_x {}
    public partial struct TestAndUintptrMethod_x {}
    public partial struct TestAndUintptr_x {}
    public partial struct TestAutoAligned64_signed {}
    public partial struct TestAutoAligned64_unsigned {}
    public partial struct TestCompareAndSwapInt32Method_x {}
    public partial struct TestCompareAndSwapInt32_x {}
    public partial struct TestCompareAndSwapInt64Method_x {}
    public partial struct TestCompareAndSwapInt64_x {}
    public partial struct TestCompareAndSwapPointerMethod_x {}
    public partial struct TestCompareAndSwapPointer_x {}
    public partial struct TestCompareAndSwapUint32Method_x {}
    public partial struct TestCompareAndSwapUint32_x {}
    public partial struct TestCompareAndSwapUint64Method_x {}
    public partial struct TestCompareAndSwapUintptrMethod_x {}
    public partial struct TestCompareAndSwapUintptr_x {}
    public partial struct TestLoadInt32Method_x {}
    public partial struct TestLoadInt32_x {}
    public partial struct TestLoadInt64Method_x {}
    public partial struct TestLoadInt64_x {}
    public partial struct TestLoadPointerMethod_x {}
    public partial struct TestLoadPointer_x {}
    public partial struct TestLoadUint32Method_x {}
    public partial struct TestLoadUint32_x {}
    public partial struct TestLoadUint64Method_x {}
    public partial struct TestLoadUint64_x {}
    public partial struct TestLoadUintptrMethod_x {}
    public partial struct TestLoadUintptr_x {}
    public partial struct TestOrInt32Method_x {}
    public partial struct TestOrInt32_x {}
    public partial struct TestOrInt64Method_x {}
    public partial struct TestOrInt64_x {}
    public partial struct TestOrUint32Method_x {}
    public partial struct TestOrUint32_x {}
    public partial struct TestOrUint64Method_x {}
    public partial struct TestOrUint64_x {}
    public partial struct TestOrUintptrMethod_x {}
    public partial struct TestOrUintptr_x {}
    public partial struct TestStoreInt32Method_x {}
    public partial struct TestStoreInt32_x {}
    public partial struct TestStoreInt64Method_x {}
    public partial struct TestStoreInt64_x {}
    [GoLocalName("Data")] [GoValueClone("pad1", "pad2")] public partial struct TestStoreLoadRelAcq32_Data {}
    [GoLocalName("Data")] [GoValueClone("pad1", "pad2")] public partial struct TestStoreLoadRelAcq64_Data {}
    public partial struct TestStorePointerMethod_x {}
    public partial struct TestStorePointer_x {}
    public partial struct TestStoreUint32Method_x {}
    public partial struct TestStoreUint32_x {}
    public partial struct TestStoreUint64Method_x {}
    public partial struct TestStoreUint64_x {}
    public partial struct TestStoreUintptrMethod_x {}
    public partial struct TestStoreUintptr_x {}
    public partial struct TestSwapInt32Method_x {}
    public partial struct TestSwapInt32_x {}
    public partial struct TestSwapInt64Method_x {}
    public partial struct TestSwapInt64_x {}
    public partial struct TestSwapPointerMethod_x {}
    public partial struct TestSwapPointer_x {}
    public partial struct TestSwapUint32Method_x {}
    public partial struct TestSwapUint32_x {}
    public partial struct TestSwapUint64Method_x {}
    public partial struct TestSwapUint64_x {}
    public partial struct TestSwapUintptrMethod_x {}
    public partial struct TestSwapUintptr_x {}
    public partial struct Value_CompareAndSwapTestsᴛ1 {}
    public partial struct Value_SwapTestsᴛ1 {}
    // </TypeAccessibility>
}
