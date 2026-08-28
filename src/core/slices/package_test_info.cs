// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.slices_package;

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static global::go.slices_test_package;

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
[assembly: go.GoPositionMap("slices/iter_test.go", "iter_test.cs", "ACUcgoKCgpSSgoKClIKClILKgoKCgpSSgoKClIKClILKgoKCgpSCgoKClIKUgsqCgoLugoKCgriCgoKCAAwYgoKCgoKC3IKCgoLKgoKCgriCgoKCuKKSgoKUhIKClIKoAAYUgoKUggAKCIIAJViykoKCloKWgrqCgoIACwyCAAkWkJLKhJKClJaWgILIog==")]
[assembly: go.GoPositionMap("slices/slices_test.go", "slices_test.cs", "AFZ8ooKAgraCgILckqiSgqiSpqKCgIK2goCCpICCuIKCgpSCloKCgpaGguiihoKCoAB46AGCgoKUlIKAgraCgIK4goCCtoKAgtqCgoKUuIKCgpSUgoCCtoKAgriCgIK2goCCuIKCgIKmgoKAgqaGgIKmhoCCACZIgoKAgtqC2qKGgriCgoCCuIKAgqSAgviihoKSyqKCgILaooKAgriCgIKkgIKkgIIAJkiCgoCCpIKCgIK4hJKCgoKmgsqCgoKCkpKSkoKUgoKCgoKCAA4QgoKCAA4kkJIAK1SCgoKAggAsVIKCgoCC2rKCgIK2ggAMBoKCgoQADByQksqCgoSElJSCuIKCgoSIlJSCuIKCgoKUgoKClICCpICCACtSgoKCgILagrKSgoKCggALDIKGgoKCgqaCgoKUgoLKgoKCgIK4goKCgILIgvKCgoSEgIKmlJSCuIIACwKCgoSCgpSWgIKmlJSCuIKykoKCgoLcgoaCgoKCpoKCgpSCgsqChIKCgpSCqIKCgpSCgqiAkJKkgJCSkoKUqJKygJLEgriCgoKClIKUgoKUgviCgoKAgqaCgoCCpoKCgIKmgoKAgqao0oKC9oIAECaCgoKC7oKCgoIACgqCgoKEAAgUgrCSysYACwKCgoKClpKEgIKmgpaUuIIACwKCgoSyhICCppSUgriCgoKCkpKSkpKClIKCgoKCggAIEoKClJKEgoCCAAoIggAPOrKSgoKCppKCgoLegoKCgoKCgoKCpoKCuIKCgoKCgoKCgqaCgriCyJKCgoCCpoSCgIIACQiCABEqsoKClIKSlIKCkoKUAA0KhAAdRIKCkoKClKKClMSAggASDJQAFjaAgtwACh6AggAMDIIABxKQkg==")]
[assembly: go.GoPositionMap("slices/sort_test.go", "sort_test.cs", "ACksgoKCgoK4goKCgoK4goKCgoK4goKCgoK4ooKClIKClIKUgoIACRaSqJKCvKKSgoKCgpSCgqaCppSmopKClJaClIKUgoKClIKogoKClIKogpSCgoKUggAIEoIACAaChAAKHrKigoKWgoKWgoKWgoK63oKCgpaCgoK4goKClIK6goKEgoKWgoLKgoKEkJKWkJKWoJKWoJIACgiCgoKClpaEACNespKCgoKogoKCAAwOgoIABRSyooKCgqiChoKCAAwOgoIABxiyooKCgu6CgoaCgg==")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("slices_test")]
public static partial class slices_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct compactTestsᴛ1 {}
    internal partial struct compareFloatTestsᴛ1 {}
    internal partial struct compareIntTestsᴛ1 {}
    internal partial struct deleteFuncTestsᴛ1 {}
    internal partial struct deleteTestsᴛ1 {}
    internal partial struct equalFloatTestsᴛ1 {}
    internal partial struct equalIntTestsᴛ1 {}
    internal partial struct indexTestsᴛ1 {}
    internal partial struct insertTestsᴛ1 {}
    internal partial struct intPair {}
    internal partial struct intPairs {}
    public partial struct BenchmarkCompact_Large_Large {}
    public partial struct BenchmarkEqualFunc_Large_Large {}
    public partial struct BenchmarkIndexFunc_Large_Large {}
    public partial struct BenchmarkIndex_Large_Large {}
    public partial struct BenchmarkReplace_cases {}
    public partial struct S {}
    public partial struct TestBinarySearchFloats_tests {}
    public partial struct TestBinarySearchInts_tests {}
    public partial struct TestBinarySearch_tests {}
    public partial struct TestChunkPanics_type {}
    public partial struct TestChunk_cases {}
    public partial struct TestConcat_cases {}
    public partial struct TestConcat_too_large_cases {}
    [GoLocalName("void")] public partial struct TestConcat_too_large_void {}
    public partial struct TestDeletePanics_type {}
    public partial struct TestInference_S {}
    public partial struct TestInsertPanics_type {}
    public partial struct TestMinMax_tests {}
    public partial struct TestRepeatPanics_type {}
    public partial struct TestRepeat_type {}
    public partial struct TestRepeat_typeᴛ1 {}
    public partial struct TestReplacePanics_type {}
    public partial struct TestReplace_type {}
    // </TypeAccessibility>
}
