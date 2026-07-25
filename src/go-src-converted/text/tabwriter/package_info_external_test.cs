// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /
// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code
// (adapter classes, partial-struct implementations, conversion operators) must anchor to
// the test package class — the source generators host output in the first class of the
// attribute-bearing file, and test-file cast sites reference the adapters as members of
// the test package class. Production-anchored records stay in package_test_info.cs.

// <ImportedTypeAliases>
using tabwriter = go.text.tabwriter_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.text.tabwriter_package;
using static go.text.tabwriter_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<panicWriter, io_package.Writer>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<buffer, ж<buffer>>(Indirect = true)]
// </ImplicitConversions>

namespace go.text;

public static partial class tabwriter_test_package
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
    internal partial struct buffer {}
    internal partial struct panicWriter {}
    internal partial struct testsᴛ1 {}
    // </TypeAccessibility>
}
