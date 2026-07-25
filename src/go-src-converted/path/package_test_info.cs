// go2cs metadata anchor for a REFERENCE-model test project (black-box, external-only
// suite): the test assembly REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the external test package class the go2cs-gen generators anchor
// generated adapters and partials to.

// <ImportedTypeAliases>
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static go.path_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go;

[GoPackage("path_test")]
public static partial class path_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    public partial struct ExtTest {}
    public partial struct IsAbsTest {}
    public partial struct JoinTest {}
    public partial struct MatchTest {}
    public partial struct PathTest {}
    public partial struct SplitTest {}
    // </TypeAccessibility>
}
