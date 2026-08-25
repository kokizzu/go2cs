// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.archive.tar_package;
using static go.archive.tar_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<failOnceWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<fileInfoNames, go.io.fs_package.FileInfo>(Pointer = true)]
[assembly: GoImplement<readBadSeeker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<readSeeker, io_package.ReadSeeker>(Promoted = true)]
[assembly: GoImplement<readSeeker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<reader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testError, error>(Promoted = true)]
[assembly: GoImplement<testError, error>]
[assembly: GoImplement<testFile, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testFile, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<testNonEmptyReader, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<testNonEmptyReader, io_package.Reader>]
[assembly: GoImplement<testNonEmptyWriter, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<testNonEmptyWriter, io_package.Writer>]
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
[assembly: go.GoPositionMap("archive/tar/fuzz_test.go", "fuzz_test.cs", "ABEaooKCgsqClIKClICCpISCgoqCgoKUgpSCgIKkuoKWgoKCgIKkgIK2gII=")]
[assembly: go.GoPositionMap("archive/tar/reader_test.go", "reader_test.cs", "ACkwggDCBK4JsrKCgpSUgoKY/IKCgoKClJSEgpSCgoKUloKClIKmgpaCgpSCqIKUABEKggAQMLKygoKUlIKCgoKUgoCCpIKogIIADAyigoKUlIKCggASEoAAEAqygtyCgpSWsoKEADBegoKClJSCtIK0grSCtIK0graCgoKAgqSCgoCCyIKmggAJEsKCgpSUgoKCgoKUgoKUloKCzIKUgoKCggANCoIAJFKCgoKClICCAAsKggASOIKCgoKUgIIADwqCgoKClIKUlrSSgoKCgoKUloKCgpSWkoKCgoKClJYANHSigoKCgoKClIKUggANCoKIAJEBsgKCgpTIgpS4goKCgoKCgpSClIKUgpSCAA0SgoKUACYGggCvAaQDgoKUkrSClIKSgsSmgpSCgoCC1oKCgIKkpILGgIKkgILmAAoMgoL6goKmgqaEgoKCgpSCpoKClIL6goKCgoKmgoKCgpSC")]
[assembly: go.GoPositionMap("archive/tar/strconv_test.go", "strconv_test.cs", "ABMcggAQLIKCggAKCoIAJViCgoKCgoKUpoIACgqCACxmgoKCgoKCgpSmggAKCoIAGDyCgoIACgqCADt+goKCgoKUpoIACgyCAB5GgoKCAA0MgoKEABpEgoKCgoKUpoKmggAMDIKChAAOKoKCgoKClKaC")]
[assembly: go.GoPositionMap("archive/tar/tar_test.go", "tar_test.cs", "ACpGgoKUgpSCgpaCgpSUgtaCgpSClIKCloKUgpSUgtaCgpSClIKCloKUgoKmggAKBoIATqwBgoKClIKUgoKUgoIACAqCgoKUgoKUgJKkgIKkgIKkgIK2gIIACAiCgoKUgoKUgJK2gIKkgIKkgIL4goSEgoKAgqSCgpaCgpSAkqSAkqSAgsiChIKCAAkSgIKkgIKkgIK4goKClIKUgoKUggAIEqIAdOoBgoKCgoKUgpSCgpSAkqSAgqSAkqSAkqSAkqSAkqSAkqSAgoKkgIKkgIKkgIKkgIKkgIKkgIIACwqCANsBwAOCgoKUgpSClIIAEgqCAB9KkrKSgqaCgoCCpICCtoCC7pKCgpaCgoKUgoKUgoKCgIKkgIIAEBqCpoKmgqaCpoKmgqaCpoKmgoKCgpSClII=")]
[assembly: go.GoPositionMap("archive/tar/writer_test.go", "writer_test.cs", "AB4wgqqCgoKCgpSClJSCgpSCgpQAIwaCAIsDzAaCgoKClJSyooKChIKkgoLGgoLGgoKAgqSkgsaCgta4goKClIKCAAoOlIKClIKCpoKCgoKCgIKkgIKkgIK2gqaCgoKUguiUgoKUgoKUlIKEgoKCgIKkgIK2gqaCgoKUggAICKaCgpaCgqiCgoSCgoSEgoKAgqSAgqSAgraCpoKCgpSClIKUguiCuoKClIKClIKCgoKAgqSAgqSAgraCgoKUgvqCgoKUgoKUhN6CgoCCpICCpICCtoKo3ILolIKClIKClKaChIKCgoCCpICCtoKCgpSCuIKChJTKgIKkgIKkhISCgoKUgpSCAAYQgoKUggALBoKCgoKAgqSAgriCgoKAgriCgoCCuIKCgoCCpICCpICCpICCpICCuIKCgoCCpICCuIKCgoCCpICCuIKCgIKkgIKkgIKkgIKkgIIADAqChAAPLIKCggAFErIABxKyhIKAyKSAgqiSgoKCgpaCgoKUggAJCoIAECaCgIIADhKCgpQAJgaCAMcB1AOCgoKSgpSCtIKUgoKCxKaClIKCxoKCgIKkpILGgIKkgILmuICC2oK4goKAgriEgoKWgoKCgoKUgoKUgpaCgpaCqIKCAAgMgriCgoCC")]
// </GoSourcePositionMaps>

namespace go.archive;

[GoPackage("tar")]
public static partial class tar_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
