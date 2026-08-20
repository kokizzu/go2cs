// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.encoding.xml_package;
using static go.encoding.xml_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<MyAttr, global::go.encoding.xml_package.UnmarshalerAttr>(Pointer = true)]
[assembly: GoImplement<MyCharData, global::go.encoding.xml_package.Unmarshaler>(Pointer = true)]
[assembly: GoImplement<MyMarshalerAttrTest, global::go.encoding.xml_package.MarshalerAttr>(Pointer = true)]
[assembly: GoImplement<MyMarshalerTest, global::go.encoding.xml_package.Marshaler>(Pointer = true)]
[assembly: GoImplement<downCaser, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<errWriter, io_package.Writer>]
[assembly: GoImplement<limitedBytesWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<mapper, global::go.encoding.xml_package.TokenReader>]
[assembly: GoImplement<tokReader, global::go.encoding.xml_package.TokenReader>]
[assembly: GoImplement<toks, global::go.encoding.xml_package.TokenReader>(Pointer = true)]
[assembly: GoImplement<toksNil, global::go.encoding.xml_package.TokenReader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("xml")]
public static partial class xml_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
