// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.abi_package;
global using static global::go.@internal.abi_internal_test_package;

// <ImportedTypeAliases>
global using abiꓸArrayType = go.@internal.abi_package.ΔArrayType;
global using abiꓸChanDir = go.@internal.abi_package.ΔChanDir;
global using abiꓸFuncType = go.@internal.abi_package.ΔFuncType;
global using abiꓸInterfaceType = go.@internal.abi_package.ΔInterfaceType;
global using abiꓸKind = go.@internal.abi_package.ΔKind;
global using abiꓸMapType = go.@internal.abi_package.ΔMapType;
global using abiꓸName = go.@internal.abi_package.ΔName;
global using abiꓸStructType = go.@internal.abi_package.ΔStructType;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.abi_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("ArrayType", "ΔArrayType")]
[assembly: GoTypeAlias("ChanDir", "ΔChanDir")]
[assembly: GoTypeAlias("FuncType", "ΔFuncType")]
[assembly: GoTypeAlias("InterfaceType", "ΔInterfaceType")]
[assembly: GoTypeAlias("Kind", "ΔKind")]
[assembly: GoTypeAlias("MapType", "ΔMapType")]
[assembly: GoTypeAlias("Name", "ΔName")]
[assembly: GoTypeAlias("String", "const:ΔString")]
[assembly: GoTypeAlias("StructType", "ΔStructType")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.@internal;

[GoPackage("abi_test")]
public static partial class abi_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
