// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.encoding.json_package;
using static go.encoding.json_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<nilJSONMarshaler, global::go.encoding.json_package.Marshaler>(Pointer = true)]
[assembly: GoImplement<nilTextMarshaler, encoding_package.TextMarshaler>(Pointer = true)]
[assembly: GoImplement<u8marshal, encoding_package.TextUnmarshaler>(Pointer = true)]
[assembly: GoImplement<unmarshalerText, encoding_package.TextUnmarshaler>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<byteWithPtrMarshalJSON, byteWithMarshalJSON>(Inverted = true, ValueType = "byte")]
[assembly: GoImplicitConv<byteWithPtrMarshalText, byteWithMarshalText>(Inverted = true, ValueType = "byte")]
[assembly: GoImplicitConv<intWithPtrMarshalJSON, intWithMarshalJSON>(Inverted = true, ValueType = "nint")]
[assembly: GoImplicitConv<intWithPtrMarshalText, intWithMarshalText>(Inverted = true, ValueType = "nint")]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("encoding/json/bench_test.go", "bench_test.cs", "ADxcooKClJKCgpSCgpaEgIKmgIKmgoKCgoKCgqboooKCgoKUgoKCgILIAAsGooKCgoKYmoSCgoKAgqSAgsimooKCgoKUgoKAgsgACAaigoKCgpiahIKCgIKkgILI5oKmqpKCgIIAEAyCpq6ahJKCgIKkgILsopSmlKailKaU1qKCypKCgILsooKCgoKUgoKCgoKUgoKCgILIpqKCgoKCgoKCgoCCpLiigoKCgoKCgIKkgoKCgpSCgKS0+qKCgoKClIKCgoCCyKaigoKCgpSCgoKAgsimooKCkoKCgILsooKCkoKCgILsooKCkoKCgILsooKCkpKCgILsooKCkoKCgIIACwyigrqCgoCC7KKCgpKCgoCC7KKCgoKogsiCgqiCuoKCooKCgoKCgpKClKbelIKClJKSggALDqKEmoKEgoCCAAoMooKIkoKAguyigoLoooKCguiigoKCgoCC")]
[assembly: go.GoPositionMap("encoding/json/decode_test.go", "decode_test.cs", "AD94ggAdPoKCAAoYktaCgoKUggAKGILKgoKUgoKUggCMAYQCyIKmgoKUgoKUgsqCpoLKgqaCgpSCgpSCyoKmgsqCpoKClIKClILKgqaCyoKmgoKUgoKUgsqCpoIAzQSmCoKCgpSCgoKWgoKUgoKCAAkIggAHGLKSgoLcgoKCgpSCguiCAB4+goKUgoK4goKUpqKykoKCgIKCtoKWgoKUloTuloKClIKUgIKkpICCgoK4goKClIKUgoKClICCpIIACBKCgoKAgqSCgpSCgoIADAqSAAQYspKCgIKkgIKkpICCpOyCgoKUgoKUgoCCpIKCAAcQgoKigIKkgriCgpKAgqSCuIKCgoKClIIADgykAAccspKCgoKCggCbAYgFggAFGoKEgoCCpIIADQyiioKCgKS0AA8MooyCgoKCgKS0tOiCgoKmgoKCAA0GggANKLKSkoKAgqSCACZW/gACQAAdPISCgpSGloKUgpSClIKUgpSClIKUgpSClIKWgpSClIIACQyCAAgKgsaChIKClIKCgpSC3rKEgoKUgoKClILcooSCgpSCgoKUggAJCIIACRiykoKAggAMDoKCAAkaspKCgIIAFiKCgoSCgoKUggAFEIKClIKClILWgoKApLTusoKEgoIACg7EAB9IspKCgoKUggALDIKCAAQSsqKCgpSAggAMDIKCAAUUsqKCgpSAggAVEKKSAAESgoKWgoIASRwACgIARewBspKCgpSCAAsMggAVMrKSgoKCgpSCAAgQgKSigoCCtoLaooKChICC7IKC2qKCgIKmgIIACgiEkoCCpICCqIqCgpSCgIKkgqiEgoKUgoCCpIKCACEIggAZPKyCypbKlsqSuLKyooKCgqaC")]
[assembly: go.GoPositionMap("encoding/json/encode_test.go", "encode_test.cs", "AEFeggAAFoKCgoSCgpSAggAbGKIAGEaygoKClICCqJKAgqSCAA4WgoKCgpSCgpSCgoKUggAbPIKCgoSChIKCgoKUpoKAgsiCgIIACQiCAAkaspKAgoCCtgAJDpK4goKUgoLegqaCgsyCzIKmgoLMggANBoIACyaCgoKUgILugsyC5oKCgoKClICCpoKCgoKUgIIAhwEIggAFFAAHIAAGIAAGFgAHGIqCAAcQioIACBIACCIAByAACDYABBKykoKClIIAGz6igpQABBCigpQAGAiSAA4mspKApLQADA6SuIKClIKCgqamgoKUgoKCABIekriCgpSCgoIAChaCAAcQgoKUgoKCAAoIgpqAggAPCLSChpqCgIKmioKCloKAgqSC6IKSgoKCggALCpKYgoKUgJKkgoCktLQALlSCgoKCgpSCggAJDoD4gPiA+ICkooKC+oAACAaSAA4mspKCgpSSgtyC3IKUgoK6kriClIKCACw4goKCgoKCgpSCgoKClJaCgoKClJKCgpaCgpSCgoKCupqCgoKUgoKCgoKClIKCmIKCgoLcgoKCACMGggAIHgBFjgGykoKAgoKUtoCCAAkQgNSigoCCtoIADAaChoKClIKCggAMCIKCgoQACRyykoKCAAcQgqaCAAsYgoI=")]
[assembly: go.GoPositionMap("encoding/json/fold_test.go", "fold_test.cs", "AAsYogAcNpSCkoKCgg==")]
[assembly: go.GoPositionMap("encoding/json/fuzz_test.go", "fuzz_test.cs", "AAwaogACIILKgoCCpoKCloCC7KIAAiCCgoKCgoKClA==")]
[assembly: go.GoPositionMap("encoding/json/number_test.go", "number_test.cs", "AAsYhJQAL2KCgpaCgIKmgqgAFjCCgpaCgIKmgg==")]
[assembly: go.GoPositionMap("encoding/json/scanner_test.go", "scanner_test.cs", "ABAggqaCgoKUAAkIggAHGLKSgIIADAyiAA0wgrKSgoCCooK2goCCooK2goCCooK2goCCooIADAymvrKSgoCCooIAChCSgoKAgqSCgoKC+KKCgoKAgqSCppiSgIKkgoKCgqiCgIKkgoKCggAJCIIAAxCykoKCgIKC/oKCgoKCgpSCyoLegoKClIKClKaCgpSktpSkpKSmgoKCgoKClJSmgoKClIKUgoKUpoKCgpSClIKClA==")]
[assembly: go.GoPositionMap("encoding/json/stream_test.go", "stream_test.cs", "ACFAsoKC/KKCggAUNIKCgpSCgoKAgraAkoKCABAKtIKGmoSCgoCCpoqAgqaCgIKkggANKoKCgoKClICSguyCyoIAHwaCgoIAAxCciAATMrKSgoKAgqSAgqSCgoCCpICC/oKM0oKCpoKCgoCCtoKCgoKmAAwKgoKGgoKClIKUgoKUgJLIgoKUgoKAgsgACAaCioKCgoKUgpSCgpSCAAoIgoyCgoKUgJKkgpSCgpSCAAgIgr6ykoKSqICCpIIADhKCAESUAbKSgqKChICCgpSkgIKClKSkggAKELKEgpSSgoKUlJiCgoKUgqiCgg==")]
[assembly: go.GoPositionMap("encoding/json/tagkey_test.go", "tagkey_test.cs", "AGCWAYIAES6ykoKClIKCgpSCgoCCtg==")]
[assembly: go.GoPositionMap("encoding/json/tags_test.go", "tags_test.cs", "ABIWgoKClAAEEII=")]
// </GoSourcePositionMaps>

namespace go.encoding;

[GoPackage("json")]
public static partial class json_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
