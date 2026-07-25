// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
using bytes = go.bytes_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.bytes_package;
using static go.bytes_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestReaderCopyNothing_justReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestReaderCopyNothing_justReader, io_package.Reader>]
[assembly: GoImplement<TestReaderCopyNothing_justWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestReaderCopyNothing_justWriter, io_package.Writer>]
[assembly: GoImplement<negativeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<panicReader, io_package.Reader>]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

public static partial class bytes_test_package
{
    // A C# nested type declared with no access modifier is PRIVATE, and the `[GoType]`
    // declarations in this package's converted sources are deliberately bare so they read
    // like the Go original. Their real accessibility — public for a Go-exported name,
    // internal otherwise — is supplied by the partial that go2cs-gen's TypeGenerator emits,
    // and a source generator cannot see its own output: while the generators run, every one
    // of those types is still private, so a semantic query that reaches across package
    // classes resolves them as Inaccessible and silently drops whatever it was about to
    // build from them.

    // The declarations below close that gap. A C# partial type may carry its access modifier
    // on any ONE of its parts, so pinning it here fixes each type's accessibility IN SOURCE,
    // ahead of generation, while the `[GoType]` declaration itself stays Go-shaped — the
    // section declares `public partial interface Closer {}` for a `[GoType] partial interface
    // Closer`, and `internal partial struct dirEntry {}` for an unexported one.

    // <TypeAccessibility>
    internal partial struct bytesdataᴛ1 {}
    internal partial struct compareTestsᴛ1 {}
    internal partial struct containsTestsᴛ1 {}
    internal partial struct cutPrefixTestsᴛ1 {}
    internal partial struct cutSuffixTestsᴛ1 {}
    internal partial struct cutTestsᴛ1 {}
    internal partial struct negativeReader {}
    internal partial struct panicReader {}
    internal partial struct predicate {}
    internal partial struct readBytesTestsᴛ1 {}
    internal partial struct runIndexTests_type {}
    internal partial struct toValidUTF8Testsᴛ1 {}
    public partial struct BenchmarkToValidUTF8_tests {}
    public partial struct BenchmarkTrimSpace_tests {}
    public partial struct BinOpTest {}
    public partial struct ContainsAnyTestsᴛ1 {}
    public partial struct ContainsRuneTestsᴛ1 {}
    public partial struct EqualFoldTestsᴛ1 {}
    public partial struct FieldsTest {}
    public partial struct IndexFuncTest {}
    public partial struct RepeatTest {}
    public partial struct ReplaceTest {}
    public partial struct RunesTest {}
    public partial struct SplitTest {}
    public partial struct StringTest {}
    public partial struct TestIndexRune_tests {}
    public partial struct TestReaderAt_tests {}
    public partial struct TestReaderCopyNothing_justReader {}
    public partial struct TestReaderCopyNothing_justWriter {}
    public partial struct TestReaderCopyNothing_nErr {}
    public partial struct TestReader_tests {}
    public partial struct TestRepeatCatchesOverflow_testCase {}
    public partial struct TestTrimFunc_trimmers {}
    public partial struct TitleTest {}
    public partial struct TrimFuncTest {}
    public partial struct TrimNilTest {}
    public partial struct TrimTest {}
    public partial struct UnreadRuneErrorTestsᴛ1 {}
    // </TypeAccessibility>
}
