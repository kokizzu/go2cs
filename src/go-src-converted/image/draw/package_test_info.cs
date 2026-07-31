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
using image = go.image_package;
// </ImportedTypeAliases>

using go;
using static go.image.draw_package;
using static go.image.draw_test_package;

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
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<Op, Drawer>]
[assembly: GoImplement<embeddedPaletted, Image>]
[assembly: GoImplement<floydSteinberg, Drawer>]
[assembly: GoImplement<go.image.color_package.Alpha, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.Alpha16, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.CMYK, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.Gray, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.Gray16, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.Palette, go.image.color_package.Model>]
[assembly: GoImplement<go.image.color_package.RGBA64, go.image.color_package.Color>(Pointer = true)]
[assembly: GoImplement<go.image.color_package.RGBA64, go.image.color_package.Color>]
[assembly: GoImplement<go.image.color_package.ΔRGBA, go.image.color_package.Color>]
[assembly: GoImplement<image_package.NRGBA, Image>(Pointer = true)]
[assembly: GoImplement<image_package.NRGBA64, Image>(Pointer = true)]
[assembly: GoImplement<image_package.Paletted, Image>(Pointer = true)]
[assembly: GoImplement<image_package.RGBA64, Image>(Pointer = true)]
[assembly: GoImplement<image_package.Uniform, image_package.Image>(Pointer = true)]
[assembly: GoImplement<image_package.ΔRGBA, Image>(Pointer = true)]
[assembly: GoImplement<image_package.ΔRGBA, image_package.Image>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<slowerRGBA, Image>(Pointer = true)]
[assembly: GoImplement<slowerRGBA, image_package.Image>(Pointer = true)]
[assembly: GoImplement<slowestRGBA, Image>(Pointer = true)]
[assembly: GoImplement<slowestRGBA, image_package.Image>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<image.Alpha, ж<image.Alpha>>(Indirect = true)]
[assembly: GoImplicitConv<image.CMYK, ж<image.CMYK>>(Indirect = true)]
[assembly: GoImplicitConv<image.Gray, ж<image.Gray>>(Indirect = true)]
[assembly: GoImplicitConv<image.NRGBA, ж<image.NRGBA>>(Indirect = true)]
[assembly: GoImplicitConv<image.Uniform, ж<image.Uniform>>(Indirect = true)]
[assembly: GoImplicitConv<image.YCbCr, ж<image.YCbCr>>(Indirect = true)]
// </ImplicitConversions>

namespace go.image;

[GoPackage("draw")]
public static partial class draw_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct clipTest {}
    internal partial struct drawTest {}
    internal partial struct embeddedPaletted {}
    internal partial struct floydSteinberg {}
    internal partial struct slowerRGBA {}
    internal partial struct slowestRGBA {}
    public partial interface Drawer {}
    public partial interface Image {}
    public partial interface Quantizer {}
    public partial interface RGBA64Image {}
    public partial struct Op {}
    // </TypeAccessibility>
}

[GoPackage("draw_test")]
public static partial class draw_test_package
{
}
