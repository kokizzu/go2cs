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
using reflect = go.reflect_package;
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
[assembly: GoTypeAlias("ΔToken", "object")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<SyntaxError, error>(Pointer = true)]
[assembly: GoImplement<TagPathError, error>(Pointer = true)]
[assembly: GoImplement<UnmarshalError, error>]
[assembly: GoImplement<UnsupportedTypeError, error>(Pointer = true)]
[assembly: GoImplement<bufio_package.Reader, io_package.ByteReader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<printer, io_package.Writer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<StartElement, ж<StartElement>>(Indirect = true)]
[assembly: GoImplicitConv<StartElement, ж<StartElement>>]
[assembly: GoImplicitConv<fieldInfo, ж<fieldInfo>>]
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("xml")]
public static partial class xml_package
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
    internal partial struct fieldFlags {}
    internal partial struct fieldInfo {}
    internal partial struct parentStack {}
    internal partial struct printer {}
    internal partial struct stack {}
    internal partial struct typeInfo {}
    public partial interface Marshaler {}
    public partial interface MarshalerAttr {}
    public partial interface TokenReader {}
    public partial interface Unmarshaler {}
    public partial interface UnmarshalerAttr {}
    public partial struct Attr {}
    public partial struct CharData {}
    public partial struct Comment {}
    public partial struct Decoder {}
    public partial struct Directive {}
    public partial struct Encoder {}
    public partial struct EndElement {}
    public partial struct Name {}
    public partial struct ProcInst {}
    public partial struct StartElement {}
    public partial struct SyntaxError {}
    public partial struct TagPathError {}
    public partial struct UnmarshalError {}
    public partial struct UnsupportedTypeError {}
    // </TypeAccessibility>
}
