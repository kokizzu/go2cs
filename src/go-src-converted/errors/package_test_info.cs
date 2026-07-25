// go2cs metadata anchor for a REFERENCE-model test project (black-box, external-only
// suite): the test assembly REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the external test package class the go2cs-gen generators anchor
// generated adapters and partials to.

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
using static go.errors_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<MyError, error>]
[assembly: GoImplement<errorT, error>]
[assembly: GoImplement<errorUncomparable, error>(Pointer = true)]
[assembly: GoImplement<errorUncomparable, error>]
[assembly: GoImplement<multiErr, error>]
[assembly: GoImplement<poser, error>(Pointer = true)]
[assembly: GoImplement<wrapped, error>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

[GoPackage("errors_test")]
public static partial class errors_test_package
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
    internal partial struct errorT {}
    internal partial struct errorUncomparable {}
    internal partial struct multiErr {}
    internal partial struct poser {}
    internal partial struct wrapped {}
    public partial interface TestAs_timeout {}
    public partial interface TestJoin_typeᴛ1 {}
    public partial struct MyError {}
    public partial struct TestAs_testCases {}
    public partial struct TestIs_testCases {}
    public partial struct TestJoinErrorMethod_type {}
    public partial struct TestJoin_type {}
    public partial struct TestUnwrap_testCases {}
    // </TypeAccessibility>
}
