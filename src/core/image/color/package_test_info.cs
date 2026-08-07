// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.image.color_package;

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static global::go.image.color_internal_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("RGBA", "ΔRGBA")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.image.color_package.Alpha, global::go.image.color_package.Color>]
[assembly: GoImplement<global::go.image.color_package.CMYK, global::go.image.color_package.Color>]
[assembly: GoImplement<global::go.image.color_package.Gray, global::go.image.color_package.Color>]
[assembly: GoImplement<global::go.image.color_package.NYCbCrA, global::go.image.color_package.Color>]
[assembly: GoImplement<global::go.image.color_package.YCbCr, global::go.image.color_package.Color>]
[assembly: GoImplement<global::go.image.color_package.ΔRGBA, global::go.image.color_package.Color>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.image;

[GoPackage("color")]
public static partial class color_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
