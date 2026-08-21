// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.image.png_package;
using static go.image.png_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<pool, global::go.image.png_package.EncoderBufferPool>(Pointer = true)]
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
[assembly: go.GoPositionMap("image/png/fuzz_test.go", "fuzz_test.cs", "ABggooKWgoKUgoKUgoKUloKCgpSClIKClNyygoKCgoKUgoKClIKCgg==")]
[assembly: go.GoPositionMap("image/png/paeth_test.go", "paeth_test.cs", "AA0agoKUqqKCgoKCgqSUqJKClIK4goKCgoKCgu6igriCgoKCgoKCgoKCgoKUgoKCgoKCgoI=")]
[assembly: go.GoPositionMap("image/png/reader_test.go", "reader_test.cs", "AEiQAaKCgpSSADxgsoKCgoKUpKSCgoKUtLS0tKiCgoCClJS0tLS01rqAgpS4goKCgoKylLTEpIKUlIKAgqSCgoKCgpS2gILIlIKCgpjEpMaCgoKYxKTcgoKUgoLGgoLGgoLGgoLGgoKUpIKU+oKClKSClPqSgoKCgoKCpoKCgpTGlAAIBqKCgpSUgoKClqaCgpSWgoKCpoKCgpSSloKCgoKUgoKUggAIFIKAgriCgqaClIIAEx6CgoKCgpSClILKooKCgoKUkoKCgpSCgoKUgoIACwqCgoKUgoKUguiI+oKC6IYACAyCgpqkgoKUgrgAABoACQ6CgoKCgoKUgoSCgpSCgpS0goKUtIKUtICC2qLKgoLolIKCgqiCgILIlAAOHoKWgoKCgoKAgszulgAAGoIADQiigoQAPZABgoKCgqKCpKSCpIKkgpaA3AAIELiAgriCuAAKFgAZNoKCooKmgoKigsiigoKUgoKUgoKCguiC1oLWgtaC1oLWgg==")]
[assembly: go.GoPositionMap("image/png/writer_test.go", "writer_test.cs", "ABUmgoKClIKCgoKCgoKCgrimgoKCgpSmgoKCgqaUgoKUgpSCgoKmgoKClIKCgqaCgoIACgqClAAaSrKkgoLuhIKCgoKqkoCCgqSCgoSCgoSUgoK4gpKClIKClIK4AAoOgoSSgIKkgoCCpoKUgIKkgILIgoKCgqaCgoKClIKCggAICIKSgoKCgoKCgoKCgoK6AAUUspKCgoKUgoLcooKCgoKCAAcQgqaipqKCtoKCgoLoopSCgoKmgpSCgoKC6KKCgpSCgoKCuKKYgoKCgriilIKCgqaClIKCgoK4opKCgoKClMTE6IKUgoKCgg==")]
// </GoSourcePositionMaps>

namespace go.image;

[GoPackage("png")]
public static partial class png_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
