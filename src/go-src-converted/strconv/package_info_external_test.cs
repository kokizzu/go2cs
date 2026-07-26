// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.strconv_package;
using static go.strconv_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

public static partial class strconv_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct appendBoolTest {}
    internal partial struct atobTest {}
    internal partial struct atocTest {}
    internal partial struct atofSimpleTest {}
    internal partial struct atofTest {}
    internal partial struct benchCase {}
    internal partial struct canBackquoteTest {}
    internal partial struct ftoaBenchesᴛ1 {}
    internal partial struct ftoaTest {}
    internal partial struct itob64Test {}
    internal partial struct mallocTestᴛ1 {}
    internal partial struct numErrorTest {}
    internal partial struct parseErrorTest {}
    internal partial struct parseInt32Test {}
    internal partial struct parseInt64BaseTest {}
    internal partial struct parseInt64Test {}
    internal partial struct parseUint32Test {}
    internal partial struct parseUint64BaseTest {}
    internal partial struct parseUint64Test {}
    internal partial struct quoteRuneTest {}
    internal partial struct quoteTest {}
    internal partial struct roundIntTest {}
    internal partial struct roundTest {}
    internal partial struct roundTripCasesᴛ1 {}
    internal partial struct shiftTest {}
    internal partial struct uitob64Test {}
    internal partial struct unQuoteTest {}
    internal partial struct varlenUintsᴛ1 {}
    public partial struct Sinkᴛ1 {}
    public partial struct TestAllocationsFromBytes_bytes {}
    public partial struct TestErrorPrefixes_vectors {}
    public partial struct TestFormatComplex_tests {}
    public partial struct TestUnquoteInvalidUTF8_tests {}
    // </TypeAccessibility>
}
