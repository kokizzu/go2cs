// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.bufio_package;
using static go.bufio_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<StringReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<alwaysError, io_package.Reader>]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<dataAndEOFReader, io_package.Reader>]
[assembly: GoImplement<emptyThenNonEmptyReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<endlessZeros, io_package.Reader>]
[assembly: GoImplement<eofReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<errorReaderFromTest, io_package.Reader>]
[assembly: GoImplement<errorReaderFromTest, io_package.Writer>]
[assembly: GoImplement<errorThenGoodReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<errorWriterTest, io_package.Writer>]
[assembly: GoImplement<errorWriterToTest, io_package.Reader>]
[assembly: GoImplement<errorWriterToTest, io_package.Writer>]
[assembly: GoImplement<largeReader, io_package.Reader>]
[assembly: GoImplement<negativeEOFReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<negativeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<onlyReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<onlyReader, io_package.Reader>]
[assembly: GoImplement<onlyWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<onlyWriter, io_package.Writer>]
[assembly: GoImplement<readFromWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<rot13Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<scriptedReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<slowReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<teststringwriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<writeCountingDiscard, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<writeErrorOnlyWriter, io_package.Writer>]
[assembly: GoImplement<writerWithReadFromError, io_package.Writer>]
[assembly: GoImplement<zeroReader, io_package.Reader>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

public static partial class bufio_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct alwaysError {}
    internal partial struct bufReader {}
    internal partial struct countdown {}
    internal partial struct dataAndEOFReader {}
    internal partial struct emptyThenNonEmptyReader {}
    internal partial struct endlessZeros {}
    internal partial struct eofReader {}
    internal partial struct errorReaderFromTest {}
    internal partial struct errorThenGoodReader {}
    internal partial struct errorWriterTest {}
    internal partial struct errorWriterToTest {}
    internal partial struct largeReader {}
    internal partial struct negativeEOFReader {}
    internal partial struct negativeReader {}
    internal partial struct onlyReader {}
    internal partial struct onlyWriter {}
    internal partial struct readFromWriter {}
    internal partial struct readLineNewlinesTestsᴛ1 {}
    internal partial struct readLineResult {}
    internal partial struct readMaker {}
    internal partial struct rot13Reader {}
    internal partial struct scriptedReader {}
    internal partial struct slowReader {}
    internal partial struct testReader {}
    internal partial struct teststringwriter {}
    internal partial struct writeCountingDiscard {}
    internal partial struct writeErrorOnlyWriter {}
    internal partial struct writerWithReadFromError {}
    internal partial struct zeroReader {}
    public partial struct StringReader {}
    public partial struct TestReaderDiscard_tests {}
    // </TypeAccessibility>
}
