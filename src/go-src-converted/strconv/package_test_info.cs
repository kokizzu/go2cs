// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.strconv_package;
global using static global::go.strconv_internal_test_package;

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
// </ImportedTypeAliases>

using go;
using static global::go.strconv_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

[GoPackage("strconv_test")]
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
