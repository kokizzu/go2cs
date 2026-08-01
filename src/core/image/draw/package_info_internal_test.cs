// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.image.draw_package;
using static go.image.draw_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<embeddedPaletted, global::go.image.draw_package.Image>]
[assembly: GoImplement<slowerRGBA, global::go.image.draw_package.Image>(Pointer = true)]
[assembly: GoImplement<slowerRGBA, image_package.Image>(Pointer = true)]
[assembly: GoImplement<slowestRGBA, global::go.image.draw_package.Image>(Pointer = true)]
[assembly: GoImplement<slowestRGBA, image_package.Image>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.image;

[GoPackage("draw")]
public static partial class draw_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
