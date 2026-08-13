// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.mime.multipart_package;
using static go.mime.multipart_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<failOnReadAfterErrorReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<maliciousReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<neverendingReader, io_package.Reader>]
[assembly: GoImplement<sentinelReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<slowReader, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<slowReader, ж<slowReader>>(Indirect = true)]
// </ImplicitConversions>

namespace go.mime;

[GoPackage("multipart")]
public static partial class multipart_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
