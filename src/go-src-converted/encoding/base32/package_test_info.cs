// go2cs code converter defines `global using` statements here for imported type
// aliases as package references are encountered via `import' statements. Exported
// type aliases that need a `global using` declaration will be loaded from the
// referenced package by parsing its 'package_info.cs' source file and reading its
// defined `GoTypeAlias` attributes.

// Package name separator "dot" used in imported type aliases is extended Unicode
// character '\uA4F8' which is a valid character in a C# identifier name. This is
// used to simulate Go's package level type aliases since C# does not yet support
// importing type aliases at a namespace level.

// <ImportedTypeAliases>
using bytes = go.bytes_package;
using io = go.io_package;
using strings = go.strings_package;
// </ImportedTypeAliases>

using go;
using static go.encoding.base32_package;
using static go.encoding.base32_test_package;

// For encountered type alias declarations, e.g., `type Table = map[string]int`,
// go2cs code converter will generate a `global using` statement for the alias in
// the converted source, e.g.: `global using Table = go.map<go.@string, nint>;`.
// Although scope of `global using` is available to all files in the project, all
// converted Go code for the project targets the same package, so `global using`
// statements will effectively have package level scope.

// Additionally, `GoTypeAlias` attributes will be generated here for exported type
// aliases. This allows the type alias to be imported and used from other packages
// when referenced.

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<CorruptInputError, error>]
[assembly: GoImplement<badReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<decoder, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<encoder, io_package.WriteCloser>(Pointer = true)]
[assembly: GoImplement<newlineFilteringReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<badReader, ж<badReader>>(Indirect = true)]
[assembly: GoImplicitConv<bytes.Buffer, ж<bytes.Buffer>>(Indirect = true)]
[assembly: GoImplicitConv<io.PipeReader, ж<io.PipeReader>>(Indirect = true)]
[assembly: GoImplicitConv<strings.Builder, ж<strings.Builder>>(Indirect = true)]
[assembly: GoImplicitConv<strings.Reader, ж<strings.Reader>>(Indirect = true)]
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("base32")]
public static partial class base32_package
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
    internal partial struct badReader {}
    internal partial struct decoder {}
    internal partial struct encoder {}
    internal partial struct newlineFilteringReader {}
    internal partial struct testpair {}
    public partial struct CorruptInputError {}
    public partial struct Encoding {}
    public partial struct TestBufferedDecodingPadding_testcases {}
    public partial struct TestBufferedDecodingSameError_testcases {}
    public partial struct TestDecodeCorrupt_testCases {}
    public partial struct TestDecodedLen_test {}
    public partial struct TestEncodedLen_test {}
    public partial struct TestIssue20044_testCases {}
    // </TypeAccessibility>
}

[GoPackage("base32_test")]
public static partial class base32_test_package
{
}
