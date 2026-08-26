// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.strings_package;
global using static global::go.strings_internal_test_package;

// <ImportedTypeAliases>
global using abiꓸArrayType = go.@internal.abi_package.ΔArrayType;
global using abiꓸChanDir = go.@internal.abi_package.ΔChanDir;
global using abiꓸFuncType = go.@internal.abi_package.ΔFuncType;
global using abiꓸInterfaceType = go.@internal.abi_package.ΔInterfaceType;
global using abiꓸKind = go.@internal.abi_package.ΔKind;
global using abiꓸMapType = go.@internal.abi_package.ΔMapType;
global using abiꓸName = go.@internal.abi_package.ΔName;
global using abiꓸStructType = go.@internal.abi_package.ΔStructType;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
// </ImportedTypeAliases>

using go;
using static global::go.strings_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<errWriter, io_package.Writer>]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
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
[assembly: go.GoPositionMap("strings/builder_test.go", "builder_test.cs", "AA8cooKCgoKUgIKkgIIACgiCgoKCgpSCgIKkgoKClAAJBqKCgoKCgoKCgoKWgIKkgIKkgIIACAiigoKCgoKCqIKCgILIooKCkoKCgpSCgqaCgpSAkriSgoKAgrYADAaCggAcPpKCgoKUgpSEgoKUgpTKgoKAgqSAgqTWxoKCgoKUggAJCIIABBKCgoLugoKC7oKCgu6CgpKC7oKCku6CgpLugoKS7oKCku6CgpK4soLCkJLEgILapoKCggAMEIKCgpSCgpSCgriCgoKCgpSClMqCgoKCgoKUgpTKgoKCgoKUgpTKgoKCgoKClII=")]
[assembly: go.GoPositionMap("strings/clone_test.go", "clone_test.cs", "AA4egu6CgoKWgpaCyqKCgoI=")]
[assembly: go.GoPositionMap("strings/compare_test.go", "compare_test.cs", "ACZKgoKCggAICoKCgpSCuKaIgoKUhIKWgoKCgpSCgqaCgpaSgoKUgoKClIKCpoKCgoKUgoKClJQ=")]
[assembly: go.GoPositionMap("strings/reader_test.go", "reader_test.cs", "ABkggoIACiiCgoKClIKClIKUgoKCgpSCgsqCgoCCpICCAAsIgoIABxqCgoKCgpSCysaCgoKCsoKC1qbYgoKCgrKCgsSygtamgoKCgoKCgoCCpIKUgpSC/JKCgoKUgriCgoCCpoKCgIKkgoKUgILIgoCCpoCCpoCCpoCCpoCCpoCCpoCCpoKWgpaAgg==")]
[assembly: go.GoPositionMap("strings/replace_test.go", "replace_test.cs", "ACI+koKCgoKCAC4Msp6GloKClJYACBqCgoKClJSWAAkeAAgcAAsaAAYSurzeAAYSuszMqAAJHoKClIK6AAQQgoKCgoKCggAWQIQABhSCAAUSgr6CgIKkgoKCgpSCgoKUggATJpKCgoL+gqqigoKCAAgQ0gANWoKCgoKWlIKCgqaEgsqigoKCuKKCgoLoooKCuKKCgoKCuKKmouaCpqKCgriigoK4ooKC6KKCgriigoK4ooKCgoK4ooKCgoK6soKCurKCgpSkpJSCABASgoiSsqKCzIKClIKUlpKyooI=")]
[assembly: go.GoPositionMap("strings/search_test.go", "search_test.cs", "ABEaggAQKoKCgoIACgqCABY4ooSCgoKUgqiC")]
[assembly: go.GoPositionMap("strings/strings_test.go", "strings_test.cs", "ABgsgoKUgoKmAKcBzgKigoKC+oDSgNKA0oKmgoKClIKCyoIACxCCgoLKgoKCgqamgoKCkoKClIKCgoKCgoKUgoKCAA8OggAXOoKAgriCgoCCpICCtoLcooCCpILcooCCpIK4ooCCpIK4ooCCpIK4ooCCpIK4ooCCpIIAIUSCgoKCgpSClIKClIKCggAVLIKCgoKClIKClIKCggAcOoKCgoKCAAsYgoKCgoKmgoKCggAJGKKCgoIANmaCgoKUqJKCgpSClAAMBpSUgoKCgqiCgoKCqIKCgqiCgoKogoKUlIKCgqiGgoKCqIKClJSCgoKogpSkpJSCgoKClIKCqIKClJSCgoLogNSAABkugoKCgsqCspKCgoLugrKSgoKCAAkOooaC+IKCgoKClIKClIKClIKC6IAAKEyCgoKClKSkpKSkpIKCyqKEgoKCgpSkpKSkpKSCggAKDKIABBCCgrKSggAVKoIAO2qCggAEEoKCggAhQIKCgoKUgoLKgoKUgoKCgpSCgoKmAAoGlIKClIKClJSCloCCpICCpIKUggAUKIKClAAdMoKCgoKCyrKCgIKUxNqEAA4Iko6CgoKClJaCuoQACBSCgpbKgoKUgoKmABIiooKCgoKUlIKC3IKCgoKAgqSCgoKClIKClJSCgoKUgoKClIKCpoLKgoKCgoCCpIKCgoKUgoKUlIKCgpSCgoKUgoKUgoKmggALEqKioqK2goKCgJSkgoKCACFAgoKAgqSCgoIAFSiCgoCCAFOeAYKCggATLoKCggAWKoKCgtyCspYAGjKigoCCpICCAAwKgoKCgoCC3IKEgoKogoKogoIAFiqCgoCCABYogoKAggAUJIKCgIIAFCSCgoCC2oLKgoKCgpSUyqKCuKKCuKKCuIDSgNKA0oKmgKKAooCkgKKAooDqooK4ooLoooKCgriCgpSCgoKmgpLuhIKktIKClLS2yISCgpSmAAcQgrKSkpKCgoKCAAcQgrKSkpKCgoKCAAcQooK4ooK4ooK4ooK4ooK4goKCgoKC7oKCgoKCgoKUgoKU3KKCgriCgoKSkoKCAAkOgoKCkpKCgu6CgoKSkoKC7oKCgpKSgoLugoKSkoKCggAJDqKCgriCgoKCgoIACAyCgpKSgoKCAAkMgtyykoIAChCigoI=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("strings_test")]
public static partial class strings_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct algorithmTestCasesᴛ1 {}
    internal partial struct compareTestsᴛ1 {}
    internal partial struct cutPrefixTestsᴛ1 {}
    internal partial struct cutSuffixTestsᴛ1 {}
    internal partial struct cutTestsᴛ1 {}
    internal partial struct errWriter {}
    internal partial struct indexFuncTestsᴛ1 {}
    internal partial struct mapdataᴛ1 {}
    internal partial struct predicate {}
    internal partial struct toValidUTF8Testsᴛ1 {}
    internal partial struct trimFuncTestsᴛ1 {}
    internal partial struct trimTestsᴛ1 {}
    public partial struct BenchmarkToValidUTF8_tests {}
    public partial struct BenchmarkTrimSpace_tests {}
    public partial struct ContainsRuneTestsᴛ1 {}
    public partial struct ContainsTestsᴛ1 {}
    public partial struct CountTestsᴛ1 {}
    public partial struct EqualFoldTestsᴛ1 {}
    public partial struct FieldsTest {}
    public partial struct IndexTest {}
    public partial struct RepeatTestsᴛ1 {}
    public partial struct ReplaceTestsᴛ1 {}
    public partial struct RunesTestsᴛ1 {}
    public partial struct SplitTest {}
    public partial struct StringTest {}
    public partial struct TestBuilderCopyPanic_tests {}
    public partial struct TestBuilderWrite2_type {}
    [GoValueClone("bad")] public partial struct TestFinderCreation_testCases {}
    public partial struct TestFinderNext_testCases {}
    public partial struct TestGenericTrieBuilding_testCases {}
    public partial struct TestIndexRune_tests {}
    public partial struct TestReaderAt_tests {}
    public partial struct TestReader_tests {}
    [GoLocalName("testCase")] public partial struct TestRepeatCatchesOverflow_testCase {}
    [GoLocalName("testCase")] public partial struct TestReplacer_testCase {}
    public partial struct TestTrimFunc_trimmers {}
    public partial struct TitleTestsᴛ1 {}
    public partial struct UnreadRuneErrorTestsᴛ1 {}
    // </TypeAccessibility>
}
