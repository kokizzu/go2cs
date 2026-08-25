// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.encoding.gob_package;
using static go.encoding.gob_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<Float, Squarer>]
[assembly: GoImplement<Int, Squarer>]
[assembly: GoImplement<benchmarkBuf, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.encoding.gob_internal_test_package.Point, Squarer>]
[assembly: GoImplement<go.encoding.gob_internal_test_package.Vector, Squarer>]
[assembly: GoImplement<interfaceIndirectTestT, interfaceIndirectTestI>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<LargeSliceByte, ж<LargeSliceByte>>(Indirect = true)]
[assembly: GoImplicitConv<LargeSliceInt8, ж<LargeSliceInt8>>(Indirect = true)]
[assembly: GoImplicitConv<LargeSliceString, ж<LargeSliceString>>(Indirect = true)]
[assembly: GoImplicitConv<LargeSliceStruct, ж<LargeSliceStruct>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("encoding/gob/codec_test.go", "codec_test.cs", "AC9ewoCC+ILMsoKCgoKCgoKmgoKCgoKClIL6ooKCgoKCgoLqkpSCgoKCgqYAEiKCgoKmooKCgtqiloKCgoKCgrqCgoKCgoKCuoKCgoKCgoK6goKCgoKCgrqCgoKCgoKCuoKCgoKCgoK6goKCgoKCgrqCgoKCgoKCuoKCgoKCgoK6goKCgoKCgrqCgoKCgoKCuoKCgoKCgoK6goKCgoKCgrqCgoKCgoKCuoKCgoKCgoLK0oKCgpSC1oKCgoLaopaCgoKCgoK4goKCgoKCuoKCgoKCgrqCgoKCgoK6goKCgoKCuoKCgoKCgrqCgoKCgoK6goKCgoKCuoKCgoKCgrqCgoKCgoK6goKCgoKCuoKCgoKCgrqCgoKCgoK6goKCgoKCuoKCgoKCgrqCgoKCgoK6goKCgoKCuoKCgoKCggAoCoIAATKSkpKSkpKCgoKCgoKCgoKCgoKCggASJoKCgpSCgoKUgqaClIIAPwiCAAAUgoKCloKmioKCgpSmgoKCgqiCpoqCgoKUpoKCgoKogqaKgoKClKaCgoKCqIKmiIKCgqiCpoiCgoKogqaIgoKCqIKmioKCgqiCpoqCgoIADQiCioKCgoKCgoKCgpSClIKUggAZMISSgoKCgoKCgoKCgoKCgoKCgoKYkoKCgoKCgoKCgoKCgoKCgqiCgoKCgoKogoKCgoKogoKCgoKCgoIAEyCCgoKCgoKCgpSCgpSCABUkgoKCgoKCgoKCgoKCgoSCgoKUgoKUggAKCIKEgoKCpAAZIpSSgoKCgoKCgoKCgoKCgoKCgoKCgpSClIKUgqaCgoKCgoKCgoKUgpSClIIAChaCyoLKgoKClO6CABgigpK2goKUgoKCgoKWkoKClIKUgpSClIKUgqaClIKCgoK2ABIggoIACBKCgpaSgoKUgqaAgqSAggAPGqKCkpLKgoKClpKCgqaAgqSAgsiCkraSgpSCgoKCgpaSgoKUgpSCAA0WgoKCgoKCgoKCgoKCgpSClIIADh6CgpSUgoKCpgAUKoKCgoKCgoKCgoKCpoKClIKCgoKClIKSgoKUprKCgoCCpoKCgqiCgoCCpKiSgqgACRSmgoKopqaigoKCgs6igpaCgoKAgqSEgoKUuOSUgpSCkoKCgrKCgIKCtoIACBDCgtyCgoKCABgygoKCgoKUgoKUggAJCIKCgpSCgpSCgpSCgpSCgpSCgpSCgpSCgoKUgoKUgoKUgoKUgoLolISCgoKC")]
[assembly: go.GoPositionMap("encoding/gob/encoder_test.go", "encoder_test.cs", "ABcqkgATKIKCgoKClIKCgoKUggAJCoSCgoKEkoKChIKShIKokoKChIKShIKokoKChIKShIKokoKChIKShIIALEaigoKCgoKCgriCgoKCloKUgqiCgoKCgoKClIKCgoKWgpSCloKCgoKUgpSCqIKClIKCgvyigoKCgoKCgpSCgoKUggAICpKCggAIEoKCgoKCgsqCgoKCgpSCgoKU9oSIgoCCAAkIhIiCgIIACAiCiIKCgoKCgoCCpIL4goiCgIIADQiEiIKAggAQCIIAABCCgIKkgoCC6IKEkoCC6IKEkoCCAAsKkgAFGIKAggApPIKCgoKCgoKCgpSClIK0grSClMaCggARCoKagoCCpIKAgqSmkoCCpICCpAALFILcsoKCgoKCloKCggApUIKCgoKCgoKClIKCgoKmkoKClIKCgpSCygAKEu6UgoIAEBqCgoKCgoKCooKClIKCpoKClIKClIKClIIADxqCgoKEgoKCgpSCkoKClIK4ggATKIKCgviClIKCgoKCppKCgoKClIKUgriCgoKCgqaSgoKCgpSClIIACAySgoCCpP6ygoKCgoKAgraCgoKAgqSCABEcgpKikoKCgoCCpIKCgpSClILogoKCgIIAChSCyIKCgpaCgoKUggAIDKKCgoKChIKCgpSCgoKUguyigoKUgqQACQqSgoKClIKUgoK4woKCgpTEyoIACgaCAAsaAAsggoKCgpSUggANCoKCqoKCgpSCggARHqK4goKCgpSCgoKClIIAChiCgoKClIKCgoKWgoKCgvy0gpKCgqaUgoKUggAZNKKCgoKUgoKCgpSCAAwKgoqEkoKCgpaCgoKCpAAICpKChMyCgoCCppKCgIKmmoKUgoKClJSWgoKCAAgIgoySkoSCgIKmgoCCpoKChqKCgoKClpaSuJKUgpSCvsLslLiCgoI=")]
[assembly: go.GoPositionMap("encoding/gob/gobencdec_test.go", "gobencdec_test.cs", "ACxikoKCgoLmooKmgpSCgoKmpoKmlIKUgoKCpoKmgtaCgpSCpoKmgoKmgqaCgqaCpoKCpoKmgoKmgqaCgqaCpoKCAFOqAYKUgoKClIKCgoKUgqaCkpKSgoKUgoKClIK8opSCgoKUgoKCgpSCvKKUgpKSgoKUgoKCgpSCupKCgoKCgpSCgpSCgoKClIKCgsySgoKCgoKSkoKClIKClIKCgoKUgoKCAAQU5IKCgoKUgoKCgpSCpoKCgpSCgoKUgrqkgoKCgpSCgoKClIIACQyigpKCkoK6goKAgqSCgoCCpoCSpICSpKaAkqSAkqSmgJKkgJKkzKKSgpKCkoSCgoCCpIKCgIKmgJKkpICSpoCSpKSAkqaAkqSkgJIACQiUgoKCgpSCgoKClIKmgoKClIKCgpSCupKCgoKClIKCgoKUgriCgoKSgoKUgoKCgpSCuIKUgoKClIKCgoKUgriClIKSkpKCgpSCgoKClIK4gpSCgoKUgoKCgpSClIIABxCCpoKmgoKCqqKCgoKAgqSCgoCCpIIAECTCgqaigpSCgu6CpoKmgoKCgoKClIKCgoKUgriCgoKCgoKUgpKCgpSCuJSEgoKClIL45oKCgJKCloKCgpSCgoKCgoCCuIKClIKCgoCC")]
[assembly: go.GoPositionMap("encoding/gob/timing_test.go", "timing_test.cs", "ABkuooKCgoKUgoKCgoCCpICC7IK2kriChoK4goKCgpKClJSC+IKClIKWhIKChKKCgqaCuIKClIKWhIKCppKCgqiCkoKCgqaCuKKCgoKChIKCgoLcgoKClKaCgoKUpoKCgpTWgoKClKaCgoKUAAgSsoKClIKmsoKUgoKmgqaigoKCgpaSgoSCtIKChJSCgoKCgtyCgoKUpoKCgpSmgoKClKaCgoKUpIKCgpSkgoKClKaCgoKUpqKCgoKUgoKCgpSSgoKCgoKCgoI=")]
[assembly: go.GoPositionMap("encoding/gob/type_test.go", "type_test.cs", "AB04ooKCgoKU2JKCgpSCAAgMkoKClIKClIKC+IKCgoKClIKCgpSCgoKUgoKCAAgIgoKCgoKClIKCgpSCgoIACAiCgoKCgoKUgoKClIKCggAYKoKClIKC/KKEAA0OsgADEIKEgoKCpoKUgIIACQqChIKCkoKCgoKCgpSCgoKUpoLqkoKCgoKygoKCgoKUtLSCpJKCgIKCpICCgqSAgoL4gg==")]
// </GoSourcePositionMaps>

namespace go.encoding;

[GoPackage("gob")]
public static partial class gob_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
