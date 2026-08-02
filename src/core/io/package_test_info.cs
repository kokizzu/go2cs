// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.io_package;
global using static global::go.io_internal_test_package;

// <ImportedTypeAliases>
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using bytes = go.bytes_package;
using os = go.os_package;
using testing = go.testing_package;
using Δio = go.io_package;
using Δsync = go.sync_package;
// </ImportedTypeAliases>

using go;
using static global::go.io_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestMultiWriter_WriteStringSingleAlloc_simpleWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestMultiWriter_WriteStringSingleAlloc_simpleWriter, io_package.Writer>]
[assembly: GoImplement<TestMultiWriter_sink, fmt_package.Stringer>(Promoted = true)]
[assembly: GoImplement<TestMultiWriter_sink, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestMultiWriter_sink, testMultiWriter_sink>]
[assembly: GoImplement<TestNopCloserWriterToForwarding_typeᴛ1, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestNopCloserWriterToForwarding_typeᴛ1, io_package.Reader>]
[assembly: GoImplement<TestNopCloserWriterToForwarding_typeᴛ1, io_package.WriterTo>(Promoted = true)]
[assembly: GoImplement<TestPipeCloseError_testError1, error>(Promoted = true)]
[assembly: GoImplement<TestPipeCloseError_testError1, error>]
[assembly: GoImplement<TestPipeCloseError_testError2, error>(Promoted = true)]
[assembly: GoImplement<TestPipeCloseError_testError2, error>]
[assembly: GoImplement<byteAndEOFReader, io_package.Reader>]
[assembly: GoImplement<bytes_package.Buffer, fmt_package.Stringer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.ReadWriter>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, testMultiWriter_sink>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<dataAndErrorBuffer, io_package.ReadWriter>(Pointer = true)]
[assembly: GoImplement<errWriter, io_package.Writer>]
[assembly: GoImplement<go.io_test_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.io_test_package.Buffer, io_package.ReaderFrom>(Promoted = true)]
[assembly: GoImplement<go.io_test_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.io_test_package.Buffer, io_package.WriterTo>(Promoted = true)]
[assembly: GoImplement<io_package.OffsetWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<io_package.PipeReader, closer>(Pointer = true)]
[assembly: GoImplement<io_package.PipeReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<io_package.PipeWriter, closer>(Pointer = true)]
[assembly: GoImplement<io_package.PipeWriter, io_package.WriteCloser>(Pointer = true)]
[assembly: GoImplement<io_package.PipeWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<largeWriter, io_package.Writer>]
[assembly: GoImplement<noReadFrom, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.WriterAt>(Pointer = true)]
[assembly: GoImplement<readerFunc, io_package.Reader>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<wantedAndErrReader, io_package.Reader>]
[assembly: GoImplement<writeStringChecker, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<writeToChecker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<writerFunc, io_package.Writer>]
[assembly: GoImplement<zeroErrReader, io_package.Reader>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<bytes.Buffer, ж<bytes.Buffer>>(Indirect = true)]
[assembly: GoImplicitConv<dataAndErrorBuffer, ж<dataAndErrorBuffer>>(Indirect = true)]
[assembly: GoImplicitConv<os.File, ж<os.File>>(Indirect = true)]
[assembly: GoImplicitConv<Δio.PipeReader, ж<Δio.PipeReader>>(Indirect = true)]
[assembly: GoImplicitConv<Δio.PipeWriter, ж<Δio.PipeWriter>>(Indirect = true)]
// </ImplicitConversions>

namespace go;

[GoPackage("io_test")]
public static partial class io_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface closer {}
    internal partial interface testMultiWriter_sink {}
    internal partial struct byteAndEOFReader {}
    internal partial struct dataAndErrorBuffer {}
    internal partial struct errWriter {}
    internal partial struct largeWriter {}
    internal partial struct noReadFrom {}
    internal partial struct pipeReturn {}
    internal partial struct pipeTest {}
    internal partial struct wantedAndErrReader {}
    internal partial struct writeStringChecker {}
    internal partial struct writeToChecker {}
    internal partial struct zeroErrReader {}
    public partial struct Buffer {}
    public partial struct TestMultiWriter_WriteStringSingleAlloc_simpleWriter {}
    public partial struct TestMultiWriter_sink {}
    public partial struct TestNopCloserWriterToForwarding_type {}
    public partial struct TestNopCloserWriterToForwarding_typeᴛ1 {}
    public partial struct TestOffsetWriter_Seek_tests {}
    public partial struct TestPipeCloseError_testError1 {}
    public partial struct TestPipeCloseError_testError2 {}
    public partial struct TestSectionReader_ReadAt_tests {}
    public partial struct TestSectionReader_Size_tests {}
    // </TypeAccessibility>
}
