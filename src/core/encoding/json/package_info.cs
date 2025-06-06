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
// </ImportedTypeAliases>

using go;
using static go.encoding.json_package;

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
[assembly: GoImplement<(Marshaler, bool), Marshaler>]
[assembly: GoImplement<(Unmarshaler, bool), Unmarshaler>]
[assembly: GoImplement<(Unmarshaler, encoding.TextUnmarshaler, reflect.Value), Unmarshaler>]
[assembly: GoImplement<(encoding.TextMarshaler, bool), encoding_package.TextMarshaler>]
[assembly: GoImplement<(encoding.TextUnmarshaler, bool), encoding_package.TextUnmarshaler>]
[assembly: GoImplement<InvalidUnmarshalError, error>]
[assembly: GoImplement<MarshalerError, error>]
[assembly: GoImplement<RawMessage, Marshaler>]
[assembly: GoImplement<RawMessage, Unmarshaler>]
[assembly: GoImplement<SyntaxError, error>]
[assembly: GoImplement<UnmarshalTypeError, error>]
[assembly: GoImplement<UnsupportedTypeError, error>]
[assembly: GoImplement<UnsupportedValueError, error>]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>]
[assembly: GoImplement<jsonError, error>(Promoted = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("json")]
public static partial class json_package
{
}
