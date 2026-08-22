// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.image_package;
using static go.image_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<global::go.image_package.Alpha, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.Alpha16, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.Gray, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.Gray16, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.NRGBA, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.NRGBA64, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.Paletted, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.RGBA64, image>(Pointer = true)]
[assembly: GoImplement<global::go.image_package.ΔRGBA, image>(Pointer = true)]
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
[assembly: go.GoPositionMap("image/geom_test.go", "geom_test.cs", "AAsYtIKClIKCgoK4lgAOIIKCgoKC3oKCgoCCpICCpICCtoKCgoKCgpSUggAHEoKCgoCCpICCpJSUgoKCgoKCgg==")]
[assembly: go.GoPositionMap("image/image_test.go", "image_test.cs", "ABYogoKCABUugoKCgoKUgoKUgoKClIKClIKCgpSCgpSCgpSCgoKmgoKCAAgItLKCgqaC1piioqKioqKioqKioqamgoK4gpSCloKCggAGEIKCgoK4gtyCnIKCgoKCppyCgoKCggAUCpSCgqiCAAEgggABILaCgoK2toKC/qK4goKClIKChKjKgrKSgoKCgtyCkrKigoKCgtyigoSCuKKCgoSCuKKChIK4ooKChIK4ooKEgriigoKEgriigoSCuKKCgoSCuKKChIK4ooKChIK4ooKEgriigoKEgriigoSCuKKCgoSCuKKChIK4ooKChII=")]
[assembly: go.GoPositionMap("image/ycbcr_test.go", "ycbcr_test.cs", "AAwYggAVLAAHENyCgoKmgsqUgpaClLqCgoKCgoK6goKCgoKWgoKCgoKUAAkUgoKCyoKCgqaCgoKC")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("image")]
public static partial class image_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
