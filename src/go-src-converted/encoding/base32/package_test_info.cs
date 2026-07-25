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
}

[GoPackage("base32_test")]
public static partial class base32_test_package
{
}
