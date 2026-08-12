// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.bytes_package;
global using static global::go.bytes_internal_test_package;

// <ImportedTypeAliases>
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
// </ImportedTypeAliases>

using go;
using static global::go.bytes_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestReaderCopyNothing_justReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestReaderCopyNothing_justReader, io_package.Reader>]
[assembly: GoImplement<TestReaderCopyNothing_justWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestReaderCopyNothing_justWriter, io_package.Writer>]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<negativeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<panicReader, io_package.Reader>]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

[GoPackage("bytes_test")]
public static partial class bytes_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

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
    [GoLocalName("justReader")] public partial struct TestReaderCopyNothing_justReader {}
    [GoLocalName("justWriter")] public partial struct TestReaderCopyNothing_justWriter {}
    [GoLocalName("nErr")] public partial struct TestReaderCopyNothing_nErr {}
    public partial struct TestReader_tests {}
    [GoLocalName("testCase")] public partial struct TestRepeatCatchesOverflow_testCase {}
    public partial struct TestTrimFunc_trimmers {}
    public partial struct TitleTest {}
    public partial struct TrimFuncTest {}
    public partial struct TrimNilTest {}
    public partial struct TrimTest {}
    public partial struct UnreadRuneErrorTestsᴛ1 {}
    // </TypeAccessibility>
}
