// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.sort_package;
global using static global::go.sort_internal_test_package;

// <ImportedTypeAliases>
global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;
global using reflectliteꓸType = go.@internal.reflectlite_package.ΔType;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.sort_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<ByName, sort_package.Interface>]
[assembly: GoImplement<ByWeight, sort_package.Interface>]
[assembly: GoImplement<adversaryTestingData, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<intPairs, sort_package.Interface>]
[assembly: GoImplement<multiSorter, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<myStructs, sort_package.Interface>]
[assembly: GoImplement<nonDeterministicTestingData, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<planetSorter, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<testingData, sort_package.Interface>(Pointer = true)]
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
[assembly: go.GoPositionMap("sort/example_keys_test.go", "example_keys_test.cs", "ABg2ksgACBSSqJKokgAPFqSGhoaqgoSChIKEgg==")]
[assembly: go.GoPositionMap("sort/example_multi_test.go", "example_multi_test.cs", "ABo4soKqwsySqJIAAhLihJKCgpa2/AAVJMSGhoaagpaChIKEgoSC")]
[assembly: go.GoPositionMap("sort/example_wrapper_test.go", "example_wrapper_test.cs", "AA0cgAAJEoCigAAIDIAACAyA5IIABxKCgoSCggACJgAPAoI=")]
[assembly: go.GoPositionMap("sort/search_test.go", "search_test.cs", "AA4cggAlSoKCgoIACwqCgoKClpaEACNgspKYgoIABRCigpSmpoKCgqaCkoKAgJKClIKmggAXLIKCgsqCgoKCgoLmgoKUgpSCgriigr6ygpKCgt7agpKChJSUhIKClIKUgg==")]
[assembly: go.GoPositionMap("sort/sort_slices_benchmark_test.go", "sort_slices_benchmark_test.cs", "ABIosoKCgpSmgoKClKaCgoKUpoKCgpSCyqKCgoKCuKKCgoKCuKKCgoKCuKKCgoKCvKKCgoKCgoKClJSmooKCgoK4ooKCgoK4ooKEgriigoSCAAwagKKAooCkgoKCgpSmgoKCgpaChIKCyqKCgoKCuKKCgoKCgg==")]
[assembly: go.GoPositionMap("sort/sort_test.go", "sort_test.cs", "ABwwgoKCgoKCuIKCgoKCgrqSgoSCloK4goKCgoKCuIKCgoKCuIKCgoKCuIKCgoKCuIKSlpKCuKKCgpSCgpSClIKCuKKCgoKCgoKCgpSCypSCgpSCgoKmooKCgoKogoKCgoIACBKCpIKClKSCgrjGgoCCuKiC6KKCgoKUhIKCgoK4ooKCgpSEgoKCkriigoKClISCgoKCuKKCgoKClIKCuKKCgoKClIKCuKKCgoKClIKCuKKCgoKClIKCuKKCgoKUgoKCgoK4ooKCgpSCgoKCkriigoKCgpSCgriigoKCgpSCkriigoKCgpSCggAaPoCigoKkgoKUgqaCgoKUpqKCgpSCgpKCgoKCgoKClKSkpKSCgpSCyoKClIK2graClIK2gpSCtoKYpKK4koIAChiCAAcQgqaCpoIADh6ApIKClISClIKmgqiCpJamgqaCkoKClKaCgoKClIKCyoKCgoIACRKQooCigKaSgrqSkoKCgoKUgpSUpqKSgpSWgpSClIKCgpSCqIKCgpSCqIKUgoKClIIACAyCgoKUgpSC7IKUguiA0oDkooKUgoKCgoKCgoKClJSCgoKCgpSCAAgMgKKAooCigKKAooA=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("sort_test")]
public static partial class sort_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct adversaryTestingData {}
    internal partial struct au {}
    internal partial struct earthMass {}
    internal partial struct intPairs {}
    internal partial struct intPairsᴛ1 {}
    internal partial struct myStruct {}
    internal partial struct myStructs {}
    internal partial struct nonDeterministicTestingData {}
    internal partial struct planetSorter {}
    internal partial struct testingData {}
    internal partial struct testsᴛ1 {}
    internal partial struct wrappertestsᴛ1 {}
    public partial struct ByName {}
    public partial struct ByWeight {}
    public partial struct Change {}
    public partial struct Grams {}
    public partial struct Organ {}
    public partial struct Organs {}
    public partial struct Planet {}
    public partial struct TestFind_tests {}
    public partial struct multiSorter {}
    // </TypeAccessibility>
}
