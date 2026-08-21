// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.encoding.csv_package;
using static go.encoding.csv_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<errorWriter, io_package.Writer>]
[assembly: GoImplement<nTimes, io_package.Reader>(Pointer = true)]
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
[assembly: go.GoPositionMap("encoding/csv/fuzz_test.go", "fuzz_test.cs", "AA8egoKEggAIEoKCgoKCgoKCgoSCgpSEgoKCgoKCgpSmlISCgoKCgoKCgoKogoLcmIKWgg==")]
[assembly: go.GoPositionMap("encoding/csv/reader_test.go", "reader_test.cs", "AI0DrAaCgoKEgpSCgpSUgoKClrKigoKAgoKUgqaClILKgoKCqIKCgoKCpJSCpoKClJSAgqSCgpSCgoCCAAgWsoKCpqaCgoKUgpSCgpSSgoKCAAIU8oKCgpKEgoKUgoK0gpS0grS0grSUAAkUsoKClIKCgoKCgpSCzsKCgoKUgoKClIIACCKCpoKAtoKAAAgGgq6CgLaCgIC2goCAtoKA")]
[assembly: go.GoPositionMap("encoding/csv/writer_test.go", "writer_test.cs", "ADdogoKCgoKClIKClIKCAAoOgtaCgoKCgoSCloKCgoSCAAkUooKCgoKU")]
// </GoSourcePositionMaps>

namespace go.encoding;

[GoPackage("csv")]
public static partial class csv_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
