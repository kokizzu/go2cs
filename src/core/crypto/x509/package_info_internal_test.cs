// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using ecdsa = go.crypto.ecdsa_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.crypto.x509_package;
using static go.crypto.x509_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
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
[assembly: go.GoPositionMap("crypto/x509/cert_pool_test.go", "cert_pool_test.cs", "ABcSooKEgoKCgoKCgpSCgpSCgoKUggA9iAGykoKC")]
[assembly: go.GoPositionMap("crypto/x509/name_constraints_test.go", "name_constraints_test.cs", "AJ4MuhmigoQAChiAgqaClIKCloKClgAJBqKChAALGoKUtoKClLaCgpS2toKClLyylgAPGLiCgIKmgpaCgpamgpKikpKSkoKWgoKkpoKSgoTcotKClLaCgpS2tra4loKUxqaWgoKogoKogoKCloKCloCCpqbCgpSkpKSkpKSkuOaCpIKClKiykoKCqAAAEOKEgoKCloKWhIKCgoKEgoKCloKWgpaChIKCuoKCgoKCqIKAgqSCgtzchIKCgpSmgqSUqIKCgoKUgoCCgraWggAIDIKCgpaCgoKWpuKEgoKUlISCgoKUlJaCgpSUhIKCgoSCACI+goKChIKCloKCloKWggAOCoKCgpaaAAoegoKWgqiCgpSCAAgMuMyCgpaChIKCloCC")]
[assembly: go.GoPositionMap("crypto/x509/oid_test.go", "oid_test.cs", "ADdmgoKCgoKWgpaAgqaCgoKClJaCgIKCpoKWgoKClIIACgyCAAcYgoKCloKCloKCggAKCoIABxqCgIIAEBiCABAsgoKClAAFFoKCgoKWgoKCgpaCloKCloKCloKCgpaCgpaCgIKmgoIACgqCggAcRoKAgtqCgoKEgoKWgsqigoKCgpSCgILagoKClA==")]
[assembly: go.GoPositionMap("crypto/x509/parser_test.go", "parser_test.cs", "ABUcogBDlgGykoKCpJSC")]
[assembly: go.GoPositionMap("crypto/x509/pem_decrypt_test.go", "pem_decrypt_test.cs", "ACAggoKCgoKUgoKClICCpIKClIIACwqCgoKCgpSCgoKClIKUgpSClIKCgpSCAFPMAqaCgoKUgoCCyIA=")]
[assembly: go.GoPositionMap("crypto/x509/pkcs8_test.go", "pkcs8_test.cs", "AFp0ogAoXoKCgoKUgoKClIKClICCgqSCgoKUgoKWgIKCgoKUlIKCgpSCggAUIoKCgoKC")]
[assembly: go.GoPositionMap("crypto/x509/platform_test.go", "platform_test.cs", "ADxMgoKWgoKUgoKCloKClIKCgpaAgqaEAHWEAoKCloKSwoKCgpSCgpSCgpaCgpSClIKWgoKkloKCpKQ=")]
[assembly: go.GoPositionMap("crypto/x509/root_test.go", "root_test.cs", "AA0WooKCpoIAFQbGgoKSgKYAKWSykoKCgpSClIKUloKEhIKk")]
[assembly: go.GoPositionMap("crypto/x509/sec1_test.go", "sec1_test.cs", "ACA6ooKCgoKUgoKUgoIADSCCgoKCgg==")]
[assembly: go.GoPositionMap("crypto/x509/verify_test.go", "verify_test.cs", "AI0DiAaCgoCCpILKgoCCyIKAgsiCgoKUgriigpSAgsiCgILIgoCCyIKAgviCgoKUpqLegoKCgoK6goKCqIKCloSCgpSUgoKCpqiCgpaCgqbegoKCgoKogrqCgoKCuIKCgoIACQ6mhLKSyoKClrKSgpTKgoKCgpSUpqIAnQGgDoKykoKClIKClMqCggAaMqKChIKCloKCloIAKfoBggAQKIKAgqSAgtqigoKWgoQACRaCgpaCgpSCgpYACQaCgrqEgoKUhIKCgpSWgoKWgriEgriCgpaEgoKUhIKCgoKUloKCloKAuKTWooKWgJTMgIKmgoKWhIKAggALCIKCgoL4goKCgoCCpJSCgoKWAAoGgoKWgoCCgIKUxqqikoKWkoKCgoKogoKWoqaCgoKCABgyooSCgpTcgoKklIKWgoKWgoKUgoKUpoKEgoKCgoKClIKClJSCloKCgoKClIKCloKCgpKClJSCgoKUgpS6poKCgoKClJSCAAwGogA4fABLmAGCALEB5AIAH0AABxAABxAAESQAChYABxAAEyiCAAcQAAwagoKmACZMgoKmABMospKCuIKUgpSCggAWDIIAQp4BgoKWsqKCkoKUhIKCspKClIKWkoKWgoKkAA4MgoKClgAgRKIABxCCgpSCgpSChIKCpN6CuIKCuISCgIL4goKClO6CgpSCgpaAgg==")]
[assembly: go.GoPositionMap("crypto/x509/x509_test.go", "x509_test.cs", "AE1SgoKCgoKUirqCgILIhIKCgoKCuIKCgpS4griygoKCloKCgpSClPaCgoKCgqaCgoKCpoKCgoIAFmCEgoKCgoLsgoSCgILIgoKCpoKCgqaCgoIADh6CAAsahIKCgpSOAAgIgriCgoKUgsyCgqSUgoKClIKWAFGCAYKCgoKCpKaCgpSCggA1aKKCgoKC+pTcgoK63IKClqaCgpSCgpSCguiCgoKClIKCloKCloCCpoKAggAKCIKCkoKUgpSClIL4goKCloKCloCCpoKCAAVigoKClKaCgoKUAA8GooSCgpaCgpYACSCCgoSCggA5hgGCgoKWgoKCloKWgpaCloKWgpaCloKWgpaCloKWgpaCgoK6goKCgqaCloKWgpaCloKWgpaCloKWgpaCloKWgpaCloKWgoKCACS0AYKCgoKCgpSAgqSAgqSAgqSAggANQIIABxCCgoKUgpSCgpSIuIKCgoKUgriCgoKCpoCCABCIAYKCgoKCloKCgpaAgoIACWCCgoKWgoKWgpSCgpSCloCCABmYAYKCgoKEgoKChAADEoSCgoQAChYACRaCgoKCloKCgpSC3IKCgoKUpqKCgoKClIKCgpaCvJKCgoKUgriigoKCgpSCgoKWggAIDJKClISAggAOEKKEgoKWgoKWgoKWgoKWAAcaggAKFoKCgpaCgoKWgoKCloKkpKSkyoKCgpaCgpamgoKClgAQJISCloLOAAscgoCCpoaWgoKouoSCuIKCgoKCloKWgpaCloKCgoKmggANCoakgoKClgADEICCpoKCloLOooKCgpaCgoKW5oIACRaAgqaCgIKmgoCCpoKCgIKmgoCCyKIACRiCgIKkgpaCgoCCpIKWgoKCgIKkgriCAAoagIKmgoCCyKIACRaAgqaCgIKmgoCCAAkIggAJHIKAgtqCgIIACQiCAAgYgoCCAB1SgoKCgpSAgqSAgqSAgsiCgoKClICCpICCpICCpoKAggAOKoKCgIKkAAgkgoKAgqQADlCCgoKCloCCpoCCAAoIgoKUgoKUgoKUgpSAAAIqpIIABzCCgoKCloKAggAJCIKCgpSCgqjagpaU3gAtZIKAgriCAAgIqL4AECqCgIIACTSCgoKCloCCAAgwgoKCgpaCggAHWriCgIIACAiCyoKCloKCloKCgpSoAAhUgoKCgpa4gIIADR6CgoKCggAMCqKCgpSCgrqCgoSCgoKEAMMClgWykoKCpKSUgpaCgpaEqIKCuIK4goKCpoKmgsyClIKClLiCpoKClLiCAAcSgoKClILukoKCgqimlIKogqaCpoLuooKElKSkpgAKGLqCgoKWgoKWloKClIKCyoKEyoKCloKCABMIgoKCloKClIQAGT6yooKCpLoAECyCgoKUgoKkAAkKori+goKU3IKClLqCgoKigoKCAAoWgqaC5oK4goKCpOiCksqCgriCgoKUpqKCgpSq0oKUgpSElIKCgpSCgpSCgpSCqOaCgoKUgoKU3ISClIKUhpSCAAgIggAN3AGykoKCgoKCgu6CgoKClIIAETiigoKClIKClIKCAAhIooKClIKCAAeuAbSCgpSCggAHTKKCgpSCgpSC6KKEAAgSgoKUgoKWgoKigqbcgpSCgpaAgtyCgoKWgoK4goKCgoKUgoKCggAJCIKCgpSCgpQAQY4BAAcQsrKCgpSCgpSCgqQACAyCgoKUAAcQgoKWgoIAChqCgoKCuIKCgpQABxCCgoIACiKigoKUgoIACh6igoKUgoIACh6igoKUgoK4ggAHEKiogoKWgoKWgpaC6IIACBKCgoKWgoKWgpaChIKCloKCloK4poKCgriCAA0agoKUgoKCuIIADRqCgpSCgoK4ggANGoKClIKCgg==")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("x509")]
public static partial class x509_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
