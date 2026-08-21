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

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("strconv/atob_test.go", "atob_test.cs", "ACRIooKClIKmgriClIIAChaCgoCCABAggoKCgg==")]
[assembly: go.GoPositionMap("strconv/atoc_test.go", "atoc_test.cs", "AB9CggCSAbYCgoKClIKClIKWgoKClIKC3pKChIKCgpSC")]
[assembly: go.GoPositionMap("strconv/atof_test.go", "atof_test.cs", "AMwD+AaCpqaCgoKmgoKCuoKUlIKCgoKWgoKCloKCuIKCgoLKgoKCgpSC3IKCgoKCgoKCqIKCgoKClIKCyoKCgoKClIKCuKaApICkgoKCgpQACgoAEiKCgoKCgpSCgpSCgoKUgoKUupKCgpSCgoKClISCgpS0tLSUqqKChIKCgpSCyqKCuKKCuKKCuKKCuKKCgoK4ooKCgriigoKClIKCgoKCgsqigriigriigriigoKCgpSCgriigoKCgpSCgg==")]
[assembly: go.GoPositionMap("strconv/atoi_test.go", "atoi_test.cs", "AMQC/gSmgoKCpoKCgqaCgoKmgoKCpoKCgqaCgoLKgoKCgoLcgoKCgoLcgoKCgoLcgoKCgoLcgoKCgoLcgoKCgoLcgpSCgoKC2oKCgoL+gpSCgoKC2oKCgoL+gpSCgoKCgpSC2oKCgoKClIL+gqaCpoIAGTCCgpSClNaCgoKCgoIACAyCgoKCgoLcgoKCgoKC3IKCgoKCggAIDIKCyoCCAAgKgoKC+IKClIIACBKC7rKSgoKC3IKClIK4gsqCyrKSgoKC")]
[assembly: go.GoPositionMap("strconv/ctoa_test.go", "ctoa_test.cs", "ABMYggAOMIKCgtyigoCCtg==")]
[assembly: go.GoPositionMap("strconv/decimal_test.go", "decimal_test.cs", "ACBAgoKCgoKCggAdPoKCgoKCgoKmgoKCgqaCgoKCABgygoKCgoKCgg==")]
[assembly: go.GoPositionMap("strconv/fp_test.go", "fp_test.cs", "ABAggpSkpKSqooCCgoKUgoKClKaCgoKCgpSUgoKCgoKUlKSCgpSqooCCgoKClIKCgpSkgoKClAANBqKCgpSUhIKCgpSCgoKUgoKUgoKCgpSkgoKClIKkgriC")]
[assembly: go.GoPositionMap("strconv/ftoa_test.go", "ftoa_test.cs", "ABUqgACPAbgCgoKCgoKUgoKUgoKClIKC3IKCgoKCgIK2goKCgILsgoKClIKCgoSCgoKCgpaCgoKCgoLKooKAgrYAMmSCspKC3IKCsqKC")]
[assembly: go.GoPositionMap("strconv/ftoaryu_test.go", "ftoaryu_test.cs", "AAwagoKCgoLKgoKCgoI=")]
[assembly: go.GoPositionMap("strconv/itoa_test.go", "itoa_test.cs", "ADx8ooKCgqaCgqiCgoKmgoK6goKC3oKAgrYAFCSCgoKCpoKCACFCgoKCgsqigoKCyqKCgoKCyqKCgoLKooKCgoLKgoKCgoKC3KKCgoKCuIKykoKCgg==")]
[assembly: go.GoPositionMap("strconv/quote_test.go", "quote_test.cs", "AA0esoKCgoKCgt6ygoKCgoKCABgygoKAgqSAgtqCgoCCpICC2oKCgIKkgILaooK4ooLcooLcooIAGziCgoCCpICC2oKCgIKkgILagoKAgqSAggA5cIKCgIIAT6IBgoKUgpSCAAoKsgAGGoK4tIKCzIKUgoKUgoKCgpSC6KKC6KKC")]
[assembly: go.GoPositionMap("strconv/strconv_test.go", "strconv_test.cs", "ABswgqSkgqSmoqKkpgAJDoKClIKmgoKUgoKAggAhIoKC7gAHEpKUkpSSlJKUkpSSlJKUkpSSlJIACwiCgoKCgoQABhaCgoKClICC")]
// </GoSourcePositionMaps>

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
