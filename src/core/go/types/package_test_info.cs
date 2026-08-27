// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.go.types_package;
global using static global::go.go.types_internal_test_package;

// <ImportedTypeAliases>
global using constantꓸKind = go.go.constant_package.ΔKind;
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
global using scannerꓸError = go.go.scanner_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
global using typesꓸError = go.go.types_package.ΔError;
global using typesꓸInfo = go.go.types_package.ΔInfo;
global using typesꓸScope = go.go.types_package.ΔScope;
global using typesꓸSignature = go.go.types_package.ΔSignature;
global using typesꓸTerm = go.go.types_package.ΔTerm;
global using typesꓸType = go.go.types_package.ΔType;
// </ImportedTypeAliases>

using go;
using static global::go.go.types_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Error", "ΔError")]
[assembly: GoTypeAlias("Info", "ΔInfo")]
[assembly: GoTypeAlias("Scope", "ΔScope")]
[assembly: GoTypeAlias("Signature", "ΔSignature")]
[assembly: GoTypeAlias("String", "const:ΔString")]
[assembly: GoTypeAlias("Term", "ΔTerm")]
[assembly: GoTypeAlias("Type", "ΔType")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<global::go.go.types_package.atPos, global::go.go.types_package.positioner>]
[assembly: GoImplement<global::go.go.types_package.ΔSignature, global::go.go.types_package.ΔType>(Pointer = true)]
[assembly: GoImplement<go.go.scanner_package.ΔError, error>(Pointer = true)]
[assembly: GoImplement<importHelper, go.go.types_package.Importer>]
[assembly: GoImplement<resolveTestImporter, go.go.types_package.Importer>(Pointer = true)]
[assembly: GoImplement<stdlibChecker, go.go.types_package.Importer>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testImporter, go.go.types_package.Importer>]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.go.types_package.Basic, ж<global::go.go.types_package.Basic>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.go.types_package.Interface, ж<global::go.go.types_package.Interface>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.go.types_package.Tuple, ж<global::go.go.types_package.Tuple>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/types/api_test.go", "api_test.cs", "AHQ6goKClKaigoKC6qaCgoKUqJKCgIKCgoCCpKQACQaCAD6cAYK2hpKCgoKmgoKUloCCgriCgIK2ggALDKSUAMIBrgOCkoKCgoKClIKmmJKCgoKmgoKogIIAFAqCrgCXAdACsoKSgoKylIKUgpSChNSCgJK4goKClISCgIKkgpSCgIK2gILKgoKClIKClIIACxiigpSGAAgGggALJIK2hpKCgoKmgoKWgIIACwqiACBSgraGkoKCgpSmgoKWgIIACgqCAAAQgoLagoKClpaCgpiikoKCgpSClIKCgIKCgoKClIKUlKTGtriClIKUgpSCAA0IooQAGzyCtpaCgpiSgpS0tMSkqIIACQqChAACKAAGFoKCyIKCgoKogoKAgriCgoKUgoKUgrqAggAQCIKCgoKClKiCgoKCgoKCgoSClAALBoKEADOCAYKSloKUgoKoggAVCqKEAEKKAYKSloKogoKUtLS0tLS0tLS0yIKCgoKCpoIACgyiAEqQAoKSloKCqIKCgoIADwyCgoK6voKAgqSAgtqC3oKCgoKEgoCCupKCgqaAkuyCgIKk1qKogoKSsoKClJYAAhAAApoBAB5CgoSChIKChIKClLiCgpS6goKCgoK2uIK4ooKCorailq6ugqaigoKitqKWqqyCprSCgoKAgrYADAaMAChggoSCgoCCgriCgoKUpoKUggAIDJIAAiCCgoKAgqaCpoKClIKCpgAKCsKCgpKCsoKCgqi4AABWgoKCgoKClIKClJSmgoKUlIKAgoKkggAHFIKCgpTegpaChJKC3IKmhAAHEM6igvaiAA4kgIIACgqiAAwggIIADQq0ABU+goKCgoKUgIIACgq0AAUSgIIADQqigoLukoKCgoKClJSUAAgYgoKAgtregoS6poKCpoKCgqaCgoKC1qaCgoCCgpS27LIADxiCgoKAgqaigoKClIKClICCyoKWvKIAAiqChKaAgqaCpoKUuoKUgsaSuIKUtAAJDqKEAAIUgoKWgoK2gpS6poKCgpaCgoKUgoKCqIKCgIKCpv6EkpaCgILKgoKogILIggACGoTMgpiCgoKCsoSCguiEgoKUgoCSAAwMggAFFoKChISCgpaCgpaC+oK4goKUggAICIKCkpKCgoKCgpSUgoKCgoKCAAgKsgACQqaCgpKCgpaShAASNoKSgIKkloKSsoCCpIKCgpSAgqSClIKUgpSClIKUgt6SlKSUAA4GogACTIKCooSSAA0iABpAgoCCyoKCgoCCpICC2oIAAiqEgrqCgpaSgoK6lpaWAAgGgoKGuIKCmLqCguiUgtySgoIACgqCAFFggoKUhJKCgoKEgoKCgpSUggAPDqIABxSCgpSEgrSkgoKCgqqSgoKCpoKCAAgOkoIAAhCChJKCuIKCroKEkoLswoKCgoKCgqKCgoKAgvj+woKCgoKAggALDIKClA==")]
[assembly: global::go.GoPositionMap("go/types/builtins_test.go", "builtins_test.cs", "AJoBygKihIKCgqiCgIK2goCC2oKEgoKWgoKCgIKCtoKCqJSCgoKUgIKCypSCgoKUgoKClIKClKa4tpI=")]
[assembly: global::go.GoPositionMap("go/types/check_test.go", "check_test.cs", "AHh+goKCgoKClIKCgIKCpsimgpSklKiSgpSs1JKClIKAgqSCgoKWAAIcAA4EgoKCAA8I0oKokoKClIKCgoKqkoKCooKUgoK4guqCmKKCgoKCgoCCpoKClJKSlJaCqAALGIKCqIKCgIK6koKWgoKCgoKogoKCgoKCgqaCgoKUgoKmgoKClIKmlIKCuoKCgoCCupKCqICUgqa4grqCgoKCggAMDoKCqqKCqqKCAAYiAA4ChIKCloKCloKCgpSU6IKCgqaCgr6ygqaGotaigoKUhIIACASA0oDSgNKApIKChIKCgpaCloKUgtyChIKCgpaCgpaSuIKCgoKClJQ=")]
[assembly: global::go.GoPositionMap("go/types/commentMap_test.go", "commentMap_test.cs", "ABU+AAwCgoSCgoSCgqSkgpSCgoKCgpS4kpSkAAgKggACEoKCgoKCgqiCgoKClKiCgg==")]
[assembly: global::go.GoPositionMap("go/types/errorcalls_test.go", "errorcalls_test.cs", "ABUssoKCgpaCkoKClIKClIK4gIKCpIKSgIKAgoK2pJTKgoCCpKaCgoKClIK0tLS0xoKClJQ=")]
[assembly: global::go.GoPositionMap("go/types/eval_test.go", "eval_test.cs", "AB0wgoKCgpSCgqiUgoK4goKCuoKClIK4goKCuIKCguiCAAkUgoLoggAAEAATigGCgoKCgsyUgriWkoKCloKCgoKCgoIACRaSgtaCjAAGNoKCgpaSgoKWooKClriAgqSUgILWgIKkgILGloKCgoKCgoKCgpSCAAoQgqqChIKCgpaCgJSAgraAgg==")]
[assembly: global::go.GoPositionMap("go/types/example_test.go", "example_test.cs", "AC9EtIKCAAcgzJKCgpqigoIABkoAIgQAAxSCgoLMkoKCqIKCgoKClKiCggAHIgAOBAAFHoK62oKCgqiogoKCgoKUgoKCuJSCgoSCgoKCgoKCpqaUggAHdgA3ApSkpKSkgpSkpMiCgoI=")]
[assembly: global::go.GoPositionMap("go/types/exprstring_test.go", "exprstring_test.cs", "AGfyAYKCgoKClICC")]
[assembly: global::go.GoPositionMap("go/types/generate_test.go", "generate_test.cs", "AClI1oKCgpSCqIK4lIKCgqiWgIKokoKCgoCCpJaCgoKogIKCgoCCtgAQFIKC3IKCgoKmgoL+xqKigLaAoqLIgoKkgKKAuoKCytqAtLSA6oAAChKAAAUU4oKCgoKUlKiygILMwoCCgoCCgoKUgu7CgpKUgpS6soKSlIKUvsKCkpSClLqygpKUgpSClLzCgoCCgraqooKSlpKUgqa2gKKCxrqSgpSCgIKC2L6ygpSUgoKCgtiAgpaSkoKCgriSgoKCuJKCgoL6vKKClICCgoCUgpKCggAIDPqSgpSAgoKWgJKSgoIACQy6koKUgIKCloCSkoKCAAkMvrKClpKCtrqSgpSSgra6koCCpKiSqJKCgqiSgoKC")]
[assembly: global::go.GoPositionMap("go/types/hilbert_test.go", "hilbert_test.cs", "ABgylIKCgpaCpoKEAAQmgoKCgoKChO6iAAkGgoaCgoKClJSCgoKClKaUAAgGgoaCgoKClKaUAAkGgoaCgoKCgpSUlJQACgaChoKCgpSUgoKUlJSWggAICIKCgoKCgpSUlAAIBoKGgoKUgqYACAaCioKU")]
[assembly: global::go.GoPositionMap("go/types/instantiate_test.go", "instantiate_test.cs", "ABseooIATr4BsoSihIKCgpaCgoKWgILsgoKCpoKCgoKClIKClIKUggAICIKuAAgagoKCgoKCgpSAggAICoIAAhCCgoKCuoKAgsqSgpSSpoKU")]
[assembly: global::go.GoPositionMap("go/types/issues_test.go", "issues_test.cs", "AB0wgoKCggAICIIAABaChIKClJSkpKSk1oK2ggAJCoIAABCChIKCgoCCgpS4ggAICIKKhIKCgoKWgoKEgu7SAAQWgoQAAxiQkoKCgoCCpoKCgoKmgoKUhIKCAAgU4oQAAhoAAyIABB6CgoSCgoKCgqaClIKogoKmgoSCkJKCjIK4gqYACBKEooKCgoCCuJKAgoCCgoCC6PqmzpKCqAAKEoKCmJKCgoKYkoKCgqaClKiCgoKCAAkMlIKClIKCgqaCgpSCgviCgpaCgoKAgrYABBCSgoKAgqSWgoKAgqSAgqSEgriCgoSEkgAHEIKClIKUAAISAAgCgoCCuLqCgpLoAAkUgoKCgoKCgoKWggAIBoKEgoKCloKClJSCgoK4tooAD1yClJKWgoIACwaCgoSClIKWgpSCloKUgriUgoKCupaWgoKogoKogoKCgoIACAiKABAkgoKChIKCgJSCgoKUgIK2gsqCAAsKpABDyAGCkoKCgpKCgqKCpLaC6IKGAAMoAANIguiCAAIUgoKCloKmgq6SlpLWggACHLaWgoKYkoKCgqaCqILogq6ChIKCloKCggAICKaiiKKEABtKlJKCgoKUlIKogpaCyoIABBiSpoIAAhyCxKSEhIK4ggACEoKCgoKCgg==")]
[assembly: global::go.GoPositionMap("go/types/main_test.go", "main_test.cs", "ABcigoI=")]
[assembly: global::go.GoPositionMap("go/types/methodset_test.go", "methodset_test.cs", "ABciogABEAAnYAAJGoKEgoKClIKCgpaCgJKCpIKCgJKkgIKkgILKgpaC6pIAAhyCgoKUgoCCpoLmgqyCgoKWgoKCqIKCpoKCgqaCgoI=")]
[assembly: global::go.GoPositionMap("go/types/mono_test.go", "mono_test.cs", "ABEggoSCoraCgpSmgoKAgtqCgoCClA==")]
[assembly: global::go.GoPositionMap("go/types/named_test.go", "named_test.cs", "ABoeggACJoS+AAUUkrKkgoKCzJKypIKCgu6igoKUqJIAAiSCgoKAgqaIgoKCgpSCvtIAAhSSlIKWgpKolIKCuICCgqSCgII=")]
[assembly: global::go.GoPositionMap("go/types/object_test.go", "object_test.cs", "ABsggoKAgsqCgoCCyoKCgoKCABUevKKCloKCgqiCgoKoggAhQoKEspKCloKCgpaCgpSCgpSCgIKCgqa4gILsooKCgqY=")]
[assembly: global::go.GoPositionMap("go/types/resolver_test.go", "resolver_test.cs", "ABkwgqaCgpSCgpSCgpSC5qKEABCGAcyCgoKogpKCgoKCqIKCuoKigIKAgoKCgpSAgoKkpKSogoKqkoKygIKCgIKCpICCgqSCpJSkuoKAkriClII=")]
[assembly: global::go.GoPositionMap("go/types/self_test.go", "self_test.cs", "ACYqgoSCgoKWkoKCAAsIgoTugoKCgoKUgoKUggAKEKKCgoKmgoKCloKCgsiCggAIEoCCtoKmgoKCloKCgoKUlg==")]
[assembly: global::go.GoPositionMap("go/types/sizes_test.go", "sizes_test.cs", "ABEkkqaCgoKCgIK2gqiSAAIUgpKAgqSCgILKkgACEIKClIKUgoK4gq6SyIKCgrqShAACGIKCksiClIKWgoIAHGqCgoKSkoKS")]
[assembly: global::go.GoPositionMap("go/types/stdlib_test.go", "stdlib_test.cs", "ADNSooKWloKCkpa6AAYQgoSChIKCwpKCloKC6oSCABIogqaClJaCgpaCppSu4oKClKaCgoKCypSmgpQABhDygoKUlIKEgoKSgoKWkpSCgpSCugAMCsKCuICCgILGloKCloKUgqiCgoKAgoKUgpSkgoLKgpSCgv6CgsiWgoKmggAODKKEgpYAMRayhIKWADlIgoQAFSiShpKCgoKWloKCgoKUmJKkuJKCgoK8goKCgoKClLqokpKCgoKAgqSUgpSCgpSCgqamgoIABxCCgoKCvIKCgpSCqIKC")]
[assembly: global::go.GoPositionMap("go/types/typestring_test.go", "typestring_test.cs", "ABUqkgBMtAGUhIKChIKCgoKClIKCgpSCgIIADwqCgoSCAAoYkoKUlICC")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("types_test")]
public static partial class types_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct builtinCallsᴛ1 {}
    internal partial struct comment {}
    internal partial struct futurePackage {}
    internal partial struct gcSizeTest {}
    internal partial struct gen {}
    internal partial struct importHelper {}
    internal partial struct recordedInstance {}
    internal partial struct renameMap {}
    internal partial struct resolveTestImporter {}
    internal partial struct stdlibChecker {}
    internal partial struct testEntry {}
    internal partial struct testImporter {}
    internal partial struct testObjectsᴛ1 {}
    internal partial struct walker {}
    public partial interface TestInstanceInfo_typeᴛ1 {}
    public partial interface TestObjectString_type {}
    public partial struct BenchmarkNamed_tests {}
    public partial struct TestAssignableTo_type {}
    public partial struct TestCompositeLitTypes_type {}
    public partial struct TestConvertibleTo_type {}
    public partial struct TestDefsInfo_type {}
    public partial struct TestFileVersions_type {}
    public partial struct TestIdenticalUnions_type {}
    public partial struct TestIdentical_issue15173_type {}
    public partial struct TestIdentical_tests {}
    public partial struct TestImplements_tests {}
    public partial struct TestImplicitsInfo_type {}
    public partial struct TestInitOrderInfo_type {}
    [GoLocalName("testInst")] public partial struct TestInstanceInfo_testInst {}
    public partial struct TestInstanceInfo_type {}
    public partial struct TestInstantiateEquality_tests {}
    public partial struct TestInstantiateErrors_tests {}
    public partial struct TestInstantiatedObjects_tests {}
    public partial struct TestIsAlias_type {}
    public partial struct TestIssue51093_type {}
    public partial struct TestIssue54258_tests {}
    public partial struct TestIssue59831_tests {}
    public partial struct TestLookupFieldOrMethod_type {}
    public partial struct TestMethodInstantiation_tests {}
    public partial struct TestMultiFileInitOrder_type {}
    [GoLocalName("method")] public partial struct TestNewMethodSet_method {}
    public partial struct TestPkgNameOf_type {}
    public partial struct TestPredicatesInfo_type {}
    public partial struct TestQualifiedTypeString_type {}
    public partial struct TestScopesInfo_type {}
    public partial struct TestTooNew_type {}
    public partial struct TestTypesInfo_type {}
    public partial struct TestUsesInfo_type {}
    public partial struct TestValuesInfo_type {}
    // </TypeAccessibility>
}
