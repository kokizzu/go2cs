// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.compress.flate_package;
using static go.compress.flate_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestReaderReusesReaderBuffer_encodedNotByteReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestReaderReusesReaderBuffer_encodedNotByteReader, io_package.Reader>]
[assembly: GoImplement<TestWriteError_src, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestWriteError_src, io_package.Reader>]
[assembly: GoImplement<errorWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<failWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<sparseReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<syncBuffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<syncBuffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testDeterministic_src, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testDeterministic_src, io_package.Reader>]
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
[assembly: go.GoPositionMap("compress/flate/deflate_test.go", "deflate_test.cs", "AE6cAYKCgpSmgoKCgpSCgoKCgpSCgoKCpO6CgoKCgoKUgoKCyoKCgoKWgIKmgIKmhICCpoCCpoCCpoIAChaygpSCgoKClIKClKaC1oKClIKCgpSAgoIACxaCpsKCgoKCgpS4ggAJDLKCgqaCpqKCpoKCgqaCgpaCgoKCgoKClJaCkoKUlIKAgoKkgoCCgraAgraCgoKCgoKUgoIACxiUgoKAgqSClJaCgoKClIKCuIKCgoKClIKCgoKUgpSCgoKClIKCgpSmooKmpoKCgoKUuIKCgIIAHDKCgoKUooKClIKCyoKYgoKClIKCgoKEgoKClIK4gpiCgoKUgoKCgoSCgoKEgrqSgoKUgoKUgoKAgrbWooKCgpSCgpSCgoKUgpSEgoKogoKClIKCppaCgqiCgoKokoKCAAgMgoKCgoSCgpaCloKCgoKUlIKWgoKEgoKCgoSCgpSCgoKClIKCuK7igoKClIKEABEmgoKUgoKCgoKEgoKClIKCgIKCpIKUgIKCtoCCgqaCgoKClISCggAPGIKCgpTWgoKCgpSEgoLeooKEgoKCgpSClIKUptiCgoKUgoKCppSCgpSCgoKIooKCgpSCgpaCgqaigoKCAAkKooIAZ9QBgoKCggAKCqKCkoKCkoKoAAgUgoKClISCgoKClICCgqSAgoKmgoKCgoKWgoLuppaCgoK6goSCppaCgqiCqIKCqIKogpaCgrjGgoSChIKUgrKCgoKUgIKkgIKk")]
[assembly: go.GoPositionMap("compress/flate/dict_decoder_test.go", "dict_decoder_test.cs", "ABEaggACPAAWNpKChIKCgoKWgoK4goKCgoKCuoKEgoKClJSUhIKEgoKEgoKEgoKEgoKEgoSCgg==")]
[assembly: go.GoPositionMap("compress/flate/flate_test.go", "flate_test.cs", "ABIokqaCgrqSlIKCupKCgoKClIL4hJKCmJKEgoL4goKCgoKUggAJCAAGEABxrAKCgoKUgoKCpoKClICC7oKEgoKCggAEGPKCAAcUgoKCqIKCgpaCgpSChIKCgriClISCgoLKgpaClIKUlII=")]
[assembly: go.GoPositionMap("compress/flate/huffman_bit_writer_test.go", "huffman_bit_writer_test.cs", "ABgu1IKCloKCgpQACAiigoKClIKCgoKEgoKCloKCgoKCgIKkppaCgIKklJaCgoKCgoKCgIKklIIARI4BooLsooLswoKUgoKCgoKCgpaCgoKUkoKWgoKCgpSSgoKWgoKCgoKUgoKClIKChIKCgoCCtpaCgoKCgoKCgIKklIKUgoKCgpSCgoSCgoKAgsaClpaCgoKCgoKCgIKklILWopSkpKaCgpaCgoL6koKUgoKCgoKCpoKClKSkpKSCgpaCgoKUgoKClIKClA==")]
[assembly: go.GoPositionMap("compress/flate/inflate_test.go", "inflate_test.cs", "AA4egrqCgoKCloSCgoKChIKCAAgKggALGoKCgoKClILKgoK6goKCgpaEgoKClISCggALCoKClJKCgoKUgoKmooKAgqSCgqaigoKClIKAgg==")]
[assembly: go.GoPositionMap("compress/flate/reader_test.go", "reader_test.cs", "AA8gpgAQHoKCgoKEgoKClIKClJSCgoKCgoIAGzCCgoKClIKUsrKy")]
[assembly: go.GoPositionMap("compress/flate/writer_test.go", "writer_test.cs", "ABAggoKChIKCgpSUgoKClIKCgoKCAAkUgoKUggAPCJKCgoKClIKUlIKCtIKCgpSCgpSCgpSClIKClIKCloKCgpSClIIACRCigpKApID2goSSgqiCgoKYkoKCgqaCgoKUiKKCgoKClIKClISChIK+soKEgpaSgoKCmJKCgqiCgoKEgoKCupSUgpaCgoKmgoKUgg==")]
// </GoSourcePositionMaps>

namespace go.compress;

[GoPackage("flate")]
public static partial class flate_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
