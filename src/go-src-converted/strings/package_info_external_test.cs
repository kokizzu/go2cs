// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
using strings = go.strings_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.strings_package;
using static go.strings_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<errWriter, io_package.Writer>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

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
    internal partial struct stringdataᴛ1 {}
    internal partial struct toValidUTF8Testsᴛ1 {}
    internal partial struct trimFuncTestsᴛ1 {}
    internal partial struct trimTestsᴛ1 {}
    public partial struct BenchmarkToValidUTF8_tests {}
    public partial struct BenchmarkTrimSpace_tests {}
    public partial struct ContainsAnyTestsᴛ1 {}
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
    public partial struct TestFinderCreation_testCases {}
    public partial struct TestFinderNext_testCases {}
    public partial struct TestGenericTrieBuilding_testCases {}
    public partial struct TestIndexRune_tests {}
    public partial struct TestReaderAt_tests {}
    public partial struct TestReader_tests {}
    public partial struct TestRepeatCatchesOverflow_testCase {}
    public partial struct TestReplacer_testCase {}
    public partial struct TestTrimFunc_trimmers {}
    public partial struct TitleTestsᴛ1 {}
    public partial struct UnreadRuneErrorTestsᴛ1 {}
    // </TypeAccessibility>
}
