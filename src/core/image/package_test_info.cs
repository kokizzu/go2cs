// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.image_package;
global using static global::go.image_internal_test_package;

// <ImportedTypeAliases>
global using colorꓸRGBA = go.image.color_package.ΔRGBA;
global using imageꓸRGBA = go.image_package.ΔRGBA;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
// </ImportedTypeAliases>

using go;
using static global::go.image_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Opaque", "const:ΔOpaque")]
[assembly: GoTypeAlias("RGBA", "ΔRGBA")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.Rectangle, global::go.image_package.Image>]
[assembly: GoImplement<global::go.image_package.Uniform, go.image.color_package.Color>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.ΔRGBA, global::go.image_package.Image>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("image/decode_example_test.go", "decode_example_test.cs", "ACAwgoKCgpQACQYABxCCgoKUjtKCgqaCgoK6gqI=")]
[assembly: go.GoPositionMap("image/decode_test.go", "decode_test.cs", "ADlaooKClJLWooKClJLWgoKClKaCgoKCgoKCpoKCgpaCgoKCgoKCgoKUlIKCgpSCgoKUgoKClLi4lIKCgpSCgg==")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("image_test")]
public static partial class image_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct imageTest {}
    // </TypeAccessibility>
}
