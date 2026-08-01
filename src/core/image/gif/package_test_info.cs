// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.image.gif_package;

// <ImportedTypeAliases>
global using colorꓸRGBA = go.image.color_package.ΔRGBA;
global using imageꓸRGBA = go.image_package.ΔRGBA;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.image.gif_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.image.gif_package.blockReader, io_package.ByteReader>(Pointer = true)]
[assembly: GoImplement<global::go.image.gif_package.blockWriter, global::go.image.gif_package.writer>]
[assembly: GoImplement<go.image.color_package.Palette, go.image.color_package.Model>]
[assembly: GoImplement<image_package.Paletted, go.image.draw_package.Image>(Pointer = true)]
[assembly: GoImplement<image_package.ΔRGBA, go.image.draw_package.Image>(Pointer = true)]
[assembly: GoImplement<image_package.ΔRGBA, image_package.Image>(Pointer = true)]
[assembly: GoImplement<offsetImage, image_package.Image>(Promoted = true)]
[assembly: GoImplement<offsetImage, image_package.Image>]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

namespace go.image;

[GoPackage("gif")]
public static partial class gif_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
