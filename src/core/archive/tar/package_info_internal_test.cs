// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.archive.tar_package;
using static go.archive.tar_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<failOnceWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<fileInfoNames, go.io.fs_package.FileInfo>(Pointer = true)]
[assembly: GoImplement<readBadSeeker, io_package.ReadSeeker>(Promoted = true)]
[assembly: GoImplement<readBadSeeker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<readSeeker, io_package.ReadSeeker>(Promoted = true)]
[assembly: GoImplement<readSeeker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<reader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testError, error>(Promoted = true)]
[assembly: GoImplement<testError, error>]
[assembly: GoImplement<testFile, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testFile, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testNonEmptyReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testNonEmptyReader, io_package.Reader>]
[assembly: GoImplement<testNonEmptyWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<testNonEmptyWriter, io_package.Writer>]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.archive;

[GoPackage("tar")]
public static partial class tar_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
