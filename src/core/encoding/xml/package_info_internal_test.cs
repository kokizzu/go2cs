// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
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

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("encoding/xml/atom_test.go", "atom_test.cs", "AB5YgoKClKaC")]
[assembly: go.GoPositionMap("encoding/xml/marshal_test.go", "marshal_test.cs", "ALIDhAWCgoKCAAcQgqaCAEBmoqaiAJgJ5BOCsoKWkoKCgoKUgpSUgoKUgJKClABNjgGCgoKCgpSClIKAgu6SsoKUgIKkrJaCgoSylIKCuIKCgpSClJSAguyCgoKCgpSAkgANFLKCgpSCgoKClIKC1oKCgoKCgoKCgriCpoKUgpSC6IKEgoKC6IKCgoCCpIKUgIKkgriigoKCyqKCgpKCABAMkgAAFLqCgpSAgqSCgoIAiwPMBYKCsoKCgoKCgoKmopSUgrSCtIK0gIKCpICCgvqCgoSAgqaAgqaAggAJCIKSioKCgoKCAAwMkoqCgoKCpqaCAAcQAAoWgoKmgoL8koKCgIKkgIKkgIKkgIKkgIKkgIKkgpSCggAICpKChoIAExqCgoKAgqKCABcggoKCgoKUgoKCuoKClIIAHT6CgpKSgoKCgIK2gpS0tLSAgqSCgII=")]
[assembly: go.GoPositionMap("encoding/xml/read_test.go", "read_test.cs", "ABEokoKAgqSCAKoBhASCgoKAgqSCADVYooKCggARHoKCgIKkggASCIIAAByCgIKkgqSWgoCCpIKWgoCCpIKkADKAAYKCgoKCgoKUlIKClIKC+oKSgoKUgoKCAEysAYKCgoKCgoKUlIKClIKC+oKSgoKUgoKCloKAgqaCAAcQooKCkpSClICCtsqC7oKCABIago6CgIKmggAQGpKCgoKCgpSCgpSSggAIEpLcgoKCgIIADBaSgoCCpIKUggAtjgGkgoCCpgAJAgASJoK6AAgCAAkC8gAYMoCCpgAUJoIAH26SkoCCpgAWLoIANHCSkoCCpgAWLoIACAqSioSCAAsKgoiCgqQACwiigpSCgoKmhg==")]
[assembly: go.GoPositionMap("encoding/xml/xml_test.go", "xml_test.cs", "ABYsgoKUgoKClAANBoKCABIusrKCoriGgpSAggAMGIKCpoKUlIKC9qKCgoKCgriChoCCAJEBvgKCgoIAKmSCgoIABxCygoKUpoKCpoKCgoKUlKaCgoKClIKUgoKUgpSCgsqigoKCgoKClIKSgIKUpICClKSolLaSlLS0goLGABxIgoSCgoKUgsqCgoSCgoKUgsqCgoKClICCAAoKggAAFgAMGoKCgIKCpoKCADiUAYKChIKUggALEIKCgoSC6IKCgoKCgIKkgpSCgpSCuILcooKCgoCCpIKUgoKUggAICoKCgoKClIKC+IKCgoKClIKUgoL4goKCgoKUgoIACAiCgoKClIKClILogoKCgpSCuIKCgoKUguiCgoKClIIAFCaEgoKEgpSCgpSCyoL8goIAGC6CooCCpICCABUwgoSCgoKUggAIEICkgoKEguiCgoSCgIKkhILYgoSClIIAEQiEAAAYgoKAggAKCIIABRaCgIKAkrYADQqEjvaEigAIBoSeggAIBoSMpoLKgoKCgoKCpoCCAAoKggAFEoKCgoKCgoKUpoKClIIAFgqCAAAegoKCgoKWgpSCloKCgpSClIKUgoKClIKUggAMCISEAAwggoKAgoKkpAALDIIACBiCgoKCgoKClKaCgpSCyoIAChqCgoKU1oKCgoIADwiCgoKUgoLGgoK2loSagIKmguyC2oLWooKAgriC1oKCgoKCgoKClIKUgIKklICCpoKCgoKUgpSClIKClJSCuIK4goAAEwiihgAPLIKCgoKCgqaCgpSmgoKUgoIACB6CgoKCgoKCgoKCgpTuggAnUoKCgoKCgoKCgpSUlIKUgoKUgoI=")]
// </GoSourcePositionMaps>

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
