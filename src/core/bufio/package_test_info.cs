// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.bufio_package;
global using static global::go.bufio_internal_test_package;

// <ImportedTypeAliases>
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.bufio_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<StringReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<alwaysError, io_package.Reader>]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bufio_package.Writer, io_package.Writer>(Pointer = true)]
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

[GoPackage("bufio_test")]
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
