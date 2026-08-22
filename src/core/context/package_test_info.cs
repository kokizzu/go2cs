// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.context_package;
global using static global::go.context_internal_test_package;

// <ImportedTypeAliases>
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;
global using reflectliteꓸType = go.@internal.reflectlite_package.ΔType;
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.context_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<afterFuncContext, context_package.Context>(Pointer = true)]
[assembly: GoImplement<customCauseContext, context_package.Context>(Pointer = true)]
[assembly: GoImplement<customDoneContext, context_package.Context>(Pointer = true)]
[assembly: GoImplement<customDoneContext, context_package.Context>(Promoted = true)]
[assembly: GoImplement<otherContext, context_package.Context>(Promoted = true)]
[assembly: GoImplement<otherContext, context_package.Context>]
[assembly: GoImplement<testing_package.T, global::go.context_internal_test_package.testingT>(Pointer = true)]
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
[assembly: go.GoPositionMap("context/afterfunc_test.go", "afterfunc_test.cs", "ABYugqaCpsKCgoKU1sKCgtaCpsKCgoKClIKygoKCggAJCMKCgoKUgoKk1qKCgpKC1qKCgpKC1qKCgpKUkoLWgoKCgoCSpIKCgJLIgoKCgJKkgoCSyIKCkoCSpIKAkg==")]
[assembly: go.GoPositionMap("context/benchmark_test.go", "benchmark_test.cs", "ABUgwoKClIKSgoKCgpSClIKCAAgMgpKCgsqigoKGkoKCgsKCgoKU1oSCkoKCgpSCuISCggAJCoKCgoKCgqaCgoKCpoKCgoLugoL4goKCkoKmkoIACxCigpSSggAOEIKCkoKWgoKCgqKCxNyCgpKCloKC")]
[assembly: go.GoPositionMap("context/example_test.go", "example_test.cs", "ABEqAAoMgoKCkoK0pMiWgpSCgoIAChoACwKCupS0tAAHEgAJBoKUtLQADxLShoCCgqSWgoSCAAIS4qLYggAIFMTKgoKCqMaEgoKCwoSClIKUgtYABRYACgKigpKClIKmgoKUloKCgpSUgoKClJSClIKCAAkQAAkGgoKSlKKCqIKUhIKUgoI=")]
[assembly: go.GoPositionMap("context/net_test.go", "net_test.cs", "AA8agoKClII=")]
[assembly: go.GoPositionMap("context/x_test.go", "x_test.cs", "ABUokqSCpIKkggAOJsKCgpaC1IKCgpSk1oCS+IKCgpSk1oCS+KKEgJKmgoKEgoCCpICCpqTqgoLWpICC2qKCgoKStNaAggAPCIKEgoCSpISCgoSCgoKEgoSC9oKEgoCSpISCgoSCgoKmooKCgoLWpICCAAgOgAARDKKCgIKkgIKkgIK4goSChICSpoKEgJKmgoSChICSpoKEgoSChIKEggAJBoKCAAoQ/oIABxCCAAcQgoIABxCCgtyCppSCgpSAgtqigoKUgoKCgoKCqJKCgoKCqIKCkJKmguaCgsiCkoKU5oKC+KKCgqKClIKCgpLmgoL4gqaCAAoGooSCgpKUhr6ClIKCtIKCtIKCgpSCgsaygoCCyIKk6ICSpIKCgoKCkuaklIKC1qTogoKChILWpICCpICCyJSCgoKUgoKAgqSAggAKCoKAkoKUgJKAkviCgJKClICSgpSAkoK4soCSggANBoKGgpSCAAoGgs4AMCqCggAHEIKCAAcQgoIABxCCgoKCAAcQgoKCggAHEIKCgoIABxCCgoKCAAcQgoKCggAHEIKCgoIABxCCggAHEIKCAAcQgoIABxCCggAHEIKCggAHEIKCggANIIKCggAHEIKCgtySkoKCgIKkgIIACQyCgoKilKaAgoKUpLiCkoKCgIKkgIKkgIKkgIIACRKCpqKCgoK4gpSChIKAgqSAggAPGrKmwoKC1sKCgtaCpqKCgoKChIK4gtaUpoKAgqSAgu6mgoKCgIKkgIKkgIKkgILcgoCCpICCpICCpICCyqaCgoKAgqSAgqSAgqSAgsiCgoKSlLTWguakgriigpKCkpTm+IKCgoKSlObIgoKCkpSClIK01oK6soKCpJSSlOY=")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("context_test")]
public static partial class context_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct afterFuncContext {}
    internal partial struct customCauseContext {}
    internal partial struct customDoneContext {}
    internal partial struct key1 {}
    internal partial struct key2 {}
    internal partial struct otherContext {}
    internal partial struct testLayers_value {}
    public partial interface TestDeadlineExceededSupportsTimeout_type {}
    public partial struct ExampleWithValue_favContextKey {}
    public partial struct TestAllocs_type {}
    public partial struct TestCause_type {}
    // </TypeAccessibility>
}
