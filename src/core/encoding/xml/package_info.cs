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
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static go.encoding.xml_package;

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
[assembly: GoTypeAlias("Token", "ΔToken")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<(io.ByteReader, bool), io_package.ByteReader>]
[assembly: GoImplement<(io.Reader, error), io_package.Reader>]
[assembly: GoImplement<SyntaxError, error>]
[assembly: GoImplement<TagPathError, error>]
[assembly: GoImplement<UnmarshalError, error>]
[assembly: GoImplement<UnsupportedTypeError, error>]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>]
[assembly: GoImplement<printer, io_package.Writer>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<StartElement, ж<StartElement>>(Indirect = true)]
[assembly: GoImplicitConv<StartElement, ж<StartElement>>]
[assembly: GoImplicitConv<fieldInfo, ж<fieldInfo>>]
[assembly: GoImplicitConv<struct{p printer}, struct{p printer}>(Inverted = true)]
[assembly: GoImplicitConv<xml.Attr}, xml.Attr}>(Inverted = true)]
[assembly: GoImplicitConv<xml.Token; nextByte int; ns map<@string>string; err error; line int; linestart int64; offset int64; unmarshalDepth int}, xml.Token; nextByte int; ns map<@string>string; err error; line int; linestart int64; offset int64; unmarshalDepth int}>(Inverted = true)]
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("xml")]
public static partial class xml_package
{
}
