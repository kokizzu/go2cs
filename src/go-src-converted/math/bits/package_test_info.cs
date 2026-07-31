// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.math.bits_package;
global using static global::go.math.bits_internal_test_package;

// <ImportedTypeAliases>
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.math.bits_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.math;

[GoPackage("bits_test")]
public static partial class bits_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct entryᴛ1 {}
    public partial struct TestAddSubUint32_type {}
    public partial struct TestAddSubUint64_type {}
    public partial struct TestAddSubUint_type {}
    public partial struct TestMulDiv32_type {}
    public partial struct TestMulDiv64_type {}
    public partial struct TestMulDiv_type {}
    public partial struct TestRem64Overflow_Rem64Tests {}
    public partial struct TestReverseBytes_type {}
    public partial struct TestReverse_type {}
    // </TypeAccessibility>
}
