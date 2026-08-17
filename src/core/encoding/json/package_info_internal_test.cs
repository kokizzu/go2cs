// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.encoding.json_package;
using static go.encoding.json_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<nilJSONMarshaler, global::go.encoding.json_package.Marshaler>(Pointer = true)]
[assembly: GoImplement<nilTextMarshaler, encoding_package.TextMarshaler>(Pointer = true)]
[assembly: GoImplement<u8marshal, encoding_package.TextUnmarshaler>(Pointer = true)]
[assembly: GoImplement<unmarshalerText, encoding_package.TextUnmarshaler>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<byteWithPtrMarshalJSON, byteWithMarshalJSON>(Inverted = true, ValueType = "byte")]
[assembly: GoImplicitConv<byteWithPtrMarshalText, byteWithMarshalText>(Inverted = true, ValueType = "byte")]
[assembly: GoImplicitConv<intWithPtrMarshalJSON, intWithMarshalJSON>(Inverted = true, ValueType = "nint")]
[assembly: GoImplicitConv<intWithPtrMarshalText, intWithMarshalText>(Inverted = true, ValueType = "nint")]
// </ImplicitConversions>

namespace go.encoding;

[GoPackage("json")]
public static partial class json_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
