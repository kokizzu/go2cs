// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.fmt_package;
global using static global::go.fmt_internal_test_package;

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
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using bytes = go.bytes_package;
// </ImportedTypeAliases>

using go;
using static global::go.fmt_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<BenchmarkScanRecursiveIntReaderWrapper_buf, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<BenchmarkScanRecursiveIntReaderWrapper_buf, io_package.Reader>]
[assembly: GoImplement<TestLineByLineFscanf_r, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestLineByLineFscanf_r, io_package.Reader>]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<eofCounter, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<errString, error>]
[assembly: GoImplement<fmt_package.ScanState, io_package.Reader>]
[assembly: GoImplement<fmt_package.State, io_package.Writer>]
[assembly: GoImplement<readers_type, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<readers_type, io_package.Reader>]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testState, fmt_package.State>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<bytes.Buffer, ж<bytes.Buffer>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("fmt/errors_test.go", "errors_test.cs", "ABscpoSCAD6EAYCCpICCpICSAAkKgoCCpMqA")]
[assembly: go.GoPositionMap("fmt/fmt_test.go", "fmt_test.cs", "ADtUgoKCgoIAGjSAAA4cgsqCABMiggAKEILugu6CgIIAmQfuDqIACgaigoKCgpKUgrSCtIK0grSCtIK0goKCgqaUgoCmlAAGEKKCgoKCgoKCgpSClIKUgpSCgpSCgoKCgoKCggA8bIKCgoLcgoKCyoKCgvqCgoLKgoKCyoKCkoLKgoKCyoKCgsqCgoLKgoKCyoKCgsqCgoLKgoKCyoKCgsqCgoLKgoKSgsqCgpKCyoKCgoLKgoKSgvqCgoKCgsqigoKCuKKCgoKCuKKCgoKCABMYoqKioqKioqKAooCigKKApoIACgyClLS0tIKCgIIACA6CgoKCpoCCpICCpIIAFyyCgoKCggATCoKMgoKCAAQQgoKCpoKCyoKSgoKUgoKClIKCvKKCgoKmgoL6goKCgpSSgoKUgpSCpoKCgoKClIKCgryigoKCvKKCgoLqkoKCgoKUgoKUgoK4oAAmRoKCgoIACRaSAAcSkgAHEpIAHDCCgoKCAA8aooCCgtrmgpKSgoKUgoKCgrimgoLKgAALBKKGgoKCggBEjAGCgoKCAAwKggAHHIKCggAJGoKCgoKClIK4goKCgoKUguiCgoKCgpSC")]
[assembly: go.GoPositionMap("fmt/scan_test.go", "scan_test.cs", "AH2sAYKCgpSCgpSCAAwYooCCpoKClIIApgOEBoKCgoKCgoKUgpSCgqaCgIKkgoLKgrKSyoKyksqCgoKCgpSUgoKUgoKmgoCCpIKC+pSCgoKCgpSCyqKCgoKCgoKUgpSCuIKCuKKCgoKCgoKUgpSCgpSCuIKCuIKCgoKCgoKklJSClIKCpoKCgpSCgsqCspL6goKCgoKUgpSClIKClIKUgvqSkoKClIKUgpSCgpSCpoKClIL4goKCgoKk+IKCgoKk6IKCkoKCpAAJFLKCgpQACgqigoKCgpSClIKClIKClIKUgryikoKClIKUgoKUgqaCgpSCABw2goKAgqSAgt6igoKCgoKUgpSCgpSCAAUQooKCgoKUgoKmguaChJKCgpSClIKWkoKClIKUgpaSgoKUgqT8ooKSgoKUgoKUggALGIKCgoKmgqKCgpSClIKUggALGLKCgpSCgoKClJSCrPKCgoKUgoKClJSClIKCgpSmgoKCgpSmgoKSggAHEIKCgoKCgpSCgoKUlIK4ooKCgoKCgoK4ooKCgoKCgoL4ooKCgoKCgoIACwyikoKClIKClIKCuIKEgoKUgoKChIKClIKEgoKUgoKCqIKCAAkIgpIABRSCgoKUggALCoKSAAUWgoKClIKUggAMCoKSAC5ggoKCgpSUgpSClIIABhKSgoKClIKmgoKCgpSClII=")]
[assembly: go.GoPositionMap("fmt/state_test.go", "state_test.cs", "ABUsgqaCpoKmgsqCgoKClIKClIKClAAIBoIACyCCgoI=")]
[assembly: go.GoPositionMap("fmt/stringer_test.go", "stringer_test.cs", "ACs6gKKAooCigKKAooCigKKAooCigKKAooCigKKAooCigKSCggAJCIKCgoKCgoKC")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("fmt_test")]
public static partial class fmt_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface splitErr_type {}
    internal partial struct byteFormatter {}
    internal partial struct byteStringer {}
    internal partial struct eofCounter {}
    internal partial struct eofTestsᴛ1 {}
    internal partial struct errString {}
    internal partial struct flagPrinter {}
    internal partial struct flagtestsᴛ1 {}
    internal partial struct fmtTestsᴛ1 {}
    internal partial struct formatterFlagTestsᴛ1 {}
    internal partial struct hexBytes {}
    internal partial struct mallocTestᴛ1 {}
    internal partial struct panictestsᴛ1 {}
    internal partial struct readers_type {}
    internal partial struct readersᴛ1 {}
    internal partial struct renamedBool {}
    internal partial struct renamedBytes {}
    internal partial struct renamedComplex128 {}
    internal partial struct renamedComplex64 {}
    internal partial struct renamedFloat32 {}
    internal partial struct renamedFloat64 {}
    internal partial struct renamedInt {}
    internal partial struct renamedInt16 {}
    internal partial struct renamedInt32 {}
    internal partial struct renamedInt64 {}
    internal partial struct renamedInt8 {}
    internal partial struct renamedString {}
    internal partial struct renamedUint {}
    internal partial struct renamedUint16 {}
    internal partial struct renamedUint32 {}
    internal partial struct renamedUint64 {}
    internal partial struct renamedUint8 {}
    internal partial struct renamedUintptr {}
    internal partial struct reorderTestsᴛ1 {}
    internal partial struct runeScanner {}
    internal partial struct startestsᴛ1 {}
    internal partial struct testState {}
    internal partial struct writeStringFormatter {}
    public partial struct A {}
    public partial struct B {}
    public partial struct BenchmarkScanRecursiveIntReaderWrapper_buf {}
    public partial struct C {}
    public partial struct F {}
    public partial struct G {}
    public partial struct I {}
    public partial struct IntString {}
    public partial struct P {}
    public partial struct PanicF {}
    public partial struct PanicGo {}
    public partial struct PanicS {}
    public partial struct Recur {}
    public partial struct RecursiveInt {}
    public partial struct S {}
    public partial struct SE {}
    public partial struct SI {}
    public partial struct ScanTest {}
    public partial struct ScanfMultiTest {}
    public partial struct ScanfTest {}
    public partial struct TB {}
    public partial struct TF {}
    public partial struct TF32 {}
    public partial struct TF64 {}
    public partial struct TI {}
    public partial struct TI16 {}
    public partial struct TI32 {}
    public partial struct TI64 {}
    public partial struct TI8 {}
    public partial struct TS {}
    public partial struct TU {}
    public partial struct TU16 {}
    public partial struct TU32 {}
    public partial struct TU64 {}
    public partial struct TU8 {}
    public partial struct TUI {}
    public partial struct TestErrorf_type {}
    public partial struct TestFormatString_type {}
    public partial struct TestLineByLineFscanf_r {}
    [GoLocalName("A")] public partial struct TestNilDoesNotBecomeTyped_A {}
    [GoLocalName("B")] public partial struct TestNilDoesNotBecomeTyped_B {}
    public partial struct TestParsenum_testCases {}
    public partial struct TestScanNewlinesAreSpaces_type {}
    public partial struct TestScanfNewlineMatchFormat_type {}
    public partial struct TestScanlnNewlinesTerminate_type {}
    [GoLocalName("T")] public partial struct TestStructPrinter_T {}
    public partial struct TestStructPrinter_type {}
    public partial struct TwoLines {}
    public partial struct Xs {}
    // </TypeAccessibility>
}
