// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.math.bits_package;
using static go.math.bits_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.math;

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
