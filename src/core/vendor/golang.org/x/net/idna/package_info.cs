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
global using bidiꓸClass = go.vendor.golang.org.x.text.unicode.bidi_package.ΔClass;
global using bidiꓸDirection = go.vendor.golang.org.x.text.unicode.bidi_package.ΔDirection;
global using bidiꓸRun = go.vendor.golang.org.x.text.unicode.bidi_package.ΔRun;
global using normꓸProperties = go.vendor.golang.org.x.text.unicode.norm_package.ΔProperties;
// </ImportedTypeAliases>

using go;
using static go.vendor.golang.org.x.net.idna_package;

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
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<labelError, error>(Pointer = true)]
[assembly: GoImplement<runeError, error>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<info, Δcategory>(Inverted = true, ValueType = "uint16")]
// </ImplicitConversions>

namespace go.vendor.golang.org.x.net;

[GoPackage("idna")]
public static partial class idna_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct idnaTrie {}
    internal partial struct info {}
    internal partial struct joinState {}
    internal partial struct labelError {}
    internal partial struct labelIter {}
    internal partial struct runeError {}
    internal partial struct sparseBlocks {}
    internal partial struct valueRange {}
    public partial struct Profile {}
    public partial struct options {}
    public partial struct Δcategory {}
    // </TypeAccessibility>
}
