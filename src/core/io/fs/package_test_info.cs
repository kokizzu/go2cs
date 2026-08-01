// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.io.fs_package;

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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.io.fs_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestReadDirPath_fsys, go.io.fs_package.FS>(Promoted = true)]
[assembly: GoImplement<TestReadDirPath_fsys, go.io.fs_package.FS>]
[assembly: GoImplement<TestReadFilePath_fsys, go.io.fs_package.FS>(Promoted = true)]
[assembly: GoImplement<TestReadFilePath_fsys, go.io.fs_package.FS>]
[assembly: GoImplement<formatTest, go.io.fs_package.DirEntry>(Pointer = true)]
[assembly: GoImplement<formatTest, go.io.fs_package.FileInfo>(Pointer = true)]
[assembly: GoImplement<globOnly, go.io.fs_package.FS>]
[assembly: GoImplement<globOnly, go.io.fs_package.GlobFS>(Promoted = true)]
[assembly: GoImplement<go.testing.fstest_package.MapFS, go.io.fs_package.GlobFS>]
[assembly: GoImplement<go.testing.fstest_package.MapFS, go.io.fs_package.ReadDirFS>]
[assembly: GoImplement<go.testing.fstest_package.MapFS, go.io.fs_package.ReadFileFS>]
[assembly: GoImplement<go.testing.fstest_package.MapFS, go.io.fs_package.StatFS>]
[assembly: GoImplement<go.testing.fstest_package.MapFS, go.io.fs_package.SubFS>]
[assembly: GoImplement<openOnly, go.io.fs_package.FS>(Promoted = true)]
[assembly: GoImplement<openOnly, go.io.fs_package.FS>]
[assembly: GoImplement<readDirOnly, go.io.fs_package.FS>]
[assembly: GoImplement<readDirOnly, go.io.fs_package.ReadDirFS>(Promoted = true)]
[assembly: GoImplement<readFileOnly, go.io.fs_package.FS>]
[assembly: GoImplement<readFileOnly, go.io.fs_package.ReadFileFS>(Promoted = true)]
[assembly: GoImplement<statOnly, go.io.fs_package.FS>]
[assembly: GoImplement<statOnly, go.io.fs_package.StatFS>(Promoted = true)]
[assembly: GoImplement<subOnly, go.io.fs_package.FS>]
[assembly: GoImplement<subOnly, go.io.fs_package.SubFS>(Promoted = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.io;

[GoPackage("fs_test")]
public static partial class fs_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct formatTest {}
    internal partial struct formatTestsᴛ1 {}
    internal partial struct globOnly {}
    internal partial struct globTestsᴛ1 {}
    internal partial struct isValidPathTestsᴛ1 {}
    internal partial struct openOnly {}
    internal partial struct readDirOnly {}
    internal partial struct readFileOnly {}
    internal partial struct statOnly {}
    internal partial struct subOnly {}
    public partial struct Node {}
    public partial struct TestFileInfoToDirEntry_tests {}
    public partial struct TestReadDirPath_fsys {}
    public partial struct TestReadFilePath_fsys {}
    // </TypeAccessibility>
}
