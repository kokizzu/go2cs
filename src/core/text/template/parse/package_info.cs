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
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static go.text.template.parse_package;

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
[assembly: GoImplement<ActionNode, Node>(Pointer = true)]
[assembly: GoImplement<BoolNode, Node>(Pointer = true)]
[assembly: GoImplement<BranchNode, Node>(Pointer = true)]
[assembly: GoImplement<BreakNode, Node>(Pointer = true)]
[assembly: GoImplement<ChainNode, Node>(Pointer = true)]
[assembly: GoImplement<CommandNode, Node>(Pointer = true)]
[assembly: GoImplement<CommentNode, Node>(Pointer = true)]
[assembly: GoImplement<ContinueNode, Node>(Pointer = true)]
[assembly: GoImplement<DotNode, Node>(Pointer = true)]
[assembly: GoImplement<FieldNode, Node>(Pointer = true)]
[assembly: GoImplement<IdentifierNode, Node>(Pointer = true)]
[assembly: GoImplement<IfNode, Node>(Pointer = true)]
[assembly: GoImplement<ListNode, Node>(Pointer = true)]
[assembly: GoImplement<NilNode, Node>(Pointer = true)]
[assembly: GoImplement<NumberNode, Node>(Pointer = true)]
[assembly: GoImplement<PipeNode, Node>(Pointer = true)]
[assembly: GoImplement<RangeNode, Node>(Pointer = true)]
[assembly: GoImplement<StringNode, Node>(Pointer = true)]
[assembly: GoImplement<TemplateNode, Node>(Pointer = true)]
[assembly: GoImplement<TextNode, Node>(Pointer = true)]
[assembly: GoImplement<VariableNode, Node>(Pointer = true)]
[assembly: GoImplement<WithNode, Node>(Pointer = true)]
[assembly: GoImplement<elseNode, Node>(Pointer = true)]
[assembly: GoImplement<endNode, Node>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.text.template;

[GoPackage("parse")]
public static partial class parse_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct elseNode {}
    internal partial struct endNode {}
    internal partial struct item {}
    internal partial struct itemType {}
    internal partial struct lexOptions {}
    internal partial struct lexer {}
    public partial interface Node {}
    public partial struct ActionNode {}
    public partial struct BoolNode {}
    public partial struct BranchNode {}
    public partial struct BreakNode {}
    public partial struct ChainNode {}
    public partial struct CommandNode {}
    public partial struct CommentNode {}
    public partial struct ContinueNode {}
    public partial struct DotNode {}
    public partial struct FieldNode {}
    public partial struct IdentifierNode {}
    public partial struct IfNode {}
    public partial struct ListNode {}
    public partial struct Mode {}
    public partial struct NilNode {}
    public partial struct NodeType {}
    public partial struct NumberNode {}
    public partial struct PipeNode {}
    public partial struct Pos {}
    public partial struct RangeNode {}
    public partial struct StringNode {}
    public partial struct TemplateNode {}
    public partial struct TextNode {}
    [GoValueClone("token")] public partial struct Tree {}
    public partial struct VariableNode {}
    public partial struct WithNode {}
    // </TypeAccessibility>
}
