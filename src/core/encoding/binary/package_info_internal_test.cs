// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.encoding.binary_package;
using static go.encoding.binary_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<byteSliceReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.encoding.binary_package.bigEndian, TestByteOrder_byteOrder>]
[assembly: GoImplement<global::go.encoding.binary_package.littleEndian, TestByteOrder_byteOrder>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<Struct, ж<Struct>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("encoding/binary/binary_test.go", "binary_test.cs", "AGzqAYKCgpSCAA0WgoLchIKCloKClAAVLoKClMqCsqKCgsqCsqKCyoCigKKApICigKKA9IKCgoKWgoKCuIKmgrKSgoKCgoKCgoKCzIKykoKCABAigrKyooKSsoKUpIKClKaCgoKUgoKCgpSCggALEoKykoKAgqaCgoKClICCpAAjPoKykpKCgpiSgIK4gqiCgpiSgIKkggAYDLSEgoKCgpSWgpKCgpYACjKCgpaAgtqCABcwgoCCAA8agoKSgIKmsrKCgqaC/IKykpKCgoKClICCtoKSgpIACgqChKKMgoKUpKSmgIKkgILasoKUgoLmsoKUgoIADAaCioKCAAgSgoKAgqSCgIKkgpaCgoCCpIKAgqSCloKCgIKkgoCCpILcooKUgriCgqaCAAocgoKCggAKCoIABRiykoKClIKCAAgMgoKCkpSClIIAGDKCgsiSlIIACRSCgoKmooKCgoKCgoK4ooKCgoKSgoKClIKCuKKCgoK4ooKChIK4ooKCgoKCgoKUpqKCgoKCgpSmooKCgoKCgoK4ooKCgoKCgoKCgoKCgoKClIKCgoKCgoKCgoK4ooKCgoKCgoKCgoKCgoKUgoK4ooKCgoKCgoKCgoKCgpSCgriigoKCgoKCgpSmooKCgoKClKaigoK4ooKCuKKCgriigoK4ooKCuKKCgriigoK4ooKCuKKCgriigoK4ooKCuKKCgriigoKCgoKCgoKUgoKCgoKCgoKCgoKCgoKCgriigoKCgoKCgpSCgriigoKCgoKCgriigoKCgoKCgpSmooKCgoKCgoK4ooKCgoKCgoK4goKSgtyCgpKCgII=")]
[assembly: go.GoPositionMap("encoding/binary/varint_test.go", "varint_test.cs", "AA0cgoKCgriCgoKmgoKCgoKUgpaCgoKWgoKUgriCgoKCgpSCloKCgpaCgpSCABgygoKClIKCuIKClIK4goKCgoKCloKCgpSCAAsOogADEoKClIIAFzKCkpKCgJKkgILsgoKCloKCgoKUgILIgoKCpoKCgoK6ooKCgoLKooKCgoI=")]
// </GoSourcePositionMaps>

namespace go.encoding;

[GoPackage("binary")]
public static partial class binary_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
