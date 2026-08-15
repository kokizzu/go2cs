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
