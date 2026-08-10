// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using strings = go.strings_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.archive.zip_package;
using static go.archive.zip_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestWriterFlush_w, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestWriterFlush_w, io_package.Writer>]
[assembly: GoImplement<fakeHash32, hash_package.Hash32>(Promoted = true)]
[assembly: GoImplement<fakeHash32, hash_package.Hash32>]
[assembly: GoImplement<rleBuffer, io_package.ReaderAt>(Pointer = true)]
[assembly: GoImplement<rleBuffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<rleBuffer, sizedReaderAt>(Pointer = true)]
[assembly: GoImplement<suffixSaver, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<suffixSaver, sizedReaderAt>(Pointer = true)]
[assembly: GoImplement<zeros, io_package.Reader>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<WriteTest, ж<WriteTest>>(Indirect = true)]
[assembly: GoImplicitConv<rleBuffer, ж<rleBuffer>>(Indirect = true)]
[assembly: GoImplicitConv<suffixSaver, ж<suffixSaver>>(Indirect = true)]
// </ImplicitConversions>

namespace go.archive;

[GoPackage("zip")]
public static partial class zip_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
