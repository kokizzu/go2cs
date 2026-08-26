// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.net.http.httputil_package;
using static go.net.http.httputil_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<RoundTripperFunc, go.net.http_package.RoundTripper>]
[assembly: GoImplement<bufferPool, global::go.net.http.httputil_package.BufferPool>]
[assembly: GoImplement<checkCloser, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<eofReader, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<failingRoundTripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<mockFlusher, go.net.http_package.ResponseWriter>(Pointer = true)]
[assembly: GoImplement<mockFlusher, go.net.http_package.ResponseWriter>(Promoted = true)]
[assembly: GoImplement<roundTripperFunc, go.net.http_package.RoundTripper>]
[assembly: GoImplement<staticResponseRoundTripper, go.net.http_package.RoundTripper>]
[assembly: GoImplement<staticTransport, go.net.http_package.RoundTripper>(Pointer = true)]
[assembly: GoImplement<testResponseWriter, go.net.http_package.ResponseWriter>(Pointer = true)]
[assembly: GoImplement<wrappedRW, go.net.http_package.ResponseWriter>(Pointer = true)]
[assembly: GoImplement<wrappedRW, go.net.http_package.ResponseWriter>(Promoted = true)]
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
[assembly: go.GoPositionMap("net/http/httputil/dump_test.go", "dump_test.cs", "ABwygKSAAJABxgPYgoLKgoKCgpaCgoKWgpaClJS0xKSWgoKCgoKUgoKogoKCgpSWgoKCgoKUgoK8koKCgJS4loKCrNKAgoCU7KaCpoKCgpSmgoKClKaCgoKUAFOsAYKCgoKClIKChIIACAzSgpSEgoCCgqSCgoKUgpKCloKigpbmgoKC")]
[assembly: go.GoPositionMap("net/http/httputil/reverseproxy_test.go", "reverseproxy_test.cs", "ACVGgoIAFQbCgoKCgoKClIKUgpSAgqSAgqSAgqSAgqSAkqSCgoKCgoKCgoKCgpSSgoKUgoKCkoSCgoKCgoKCgoKClICSpICSpICCpICSpICSpICSpICCpICCpIKAkqSAkqSAksqCgoKClIKC7sKCiKSCgIKkgIKkgIKkgoKCgpSSgoKUgpKCgIKkgIKkgoKCooCCyIKCgoKmlIKCgoKCgoKUkoKClICSpICCpICCpICC+KSYpIKAgqSAgqSCgoKUkoKClIKSgoCCtpSCgoKCgoKUkoKClICSpICCpICCAAoIwoKCgoKClIKUgpSSgoKUgoKUgoKCgoKClICSpIKAkvqygoCCpJSSgoKUgoKUgpKCloKCgoKClNai3JKCgILIkoKClKSmgpSCgoKClIKClAAVHKKCgpSUgoKClIKCgoKClICSpILoooKClJSCgpaChIKUgoKCgpSSgIIADBKC7oKmooKClJSCgpaCgoKigoKWgpSCgoKClJKAgqSCAAkIooKCkoKCgpSShIKCloKEgpSChIKShIKClJSCAAoIooSCkoS4guiClpSEgoKWqISCkoSCsoKUgoKUuOiCgoKU2LKClJSSgoKCgpSUgoKUkoKClILqsoKClJKCgpaCgpKUgpKEgoKCgoKClIKAkgANFICigNSigoKUlIKClpiigoLEgoLEgqamgpSCgoKClIKCgpSClIKCgoIACgiigoKCkoKClIKUgpSUkoKClIKClIKCgpSAkqSCgJIACQyC+LKCgoKCgpSUgpSCgpSSggAJCpKCgpKCgpSWAA8UwoKUlIKCgoKClJaClAADEIKCgpSAkqQADAyC6oIADQaCAAgcAAgQgqQACRKCpMqyssqCgoKClIKClIKUgoKUgJKkAAsMsoKUgpSUgoKWgoKCgsKQksSUgILshLiCggALEoKmorjKgoSCgviigpSSgoKWioKigoKClJSEgoKUhIKCAAoMwoKCgoKUpoCCpKaEuIIACQ6C1oKCgoKCggALFoKCgoCSpIKUgJIACBCCgqaC6LKCgoKUgpSUgoKWqIKCgpSCpoLYsoKUgpSSgoKUgoKCkoSCgoLCgoKCgoKCgoL6AAwGggA/igGykoKCABEMoqKCgoKUgoKClJKCgoKClMSUgoKCgoKWkoKCgJK4gpSCgoSCgoKUgpaCgoKWgpSCgpSUgJKmgoKClIKCggAKCKKCgoaEwoCSgoKkgoKClJSCgIKCpICCgqaCgILWpKSClICC1sbElIKCgoKClqKCgqKClJaClIKChIKClJKAkqaAkqaAkqaCgpaAkqaAgrqCgoKUtqbIAAkKwoKCgpSSgoKUgoKCkoSCgpaEgJL6ooKUkoKClKSmgpKEgoKUlIKCloCSAA0IggAGFoKAggAQEoIABxyCgoIACBKigoKUkqSmgpSCgpSSgoCSAAoIAAgKgoKmkoKClIKEguaCkqSCqIL6AA0IooKCgoKEgoSUkoKClIKCgpKEgoSCloKChJaCuoKklKSkpoSmhIKClpSClISCggAJEoKCgoKSlLiCgoKCtoKUuIIABhKCAAwUwoKClJKCgpSCgqaChAAHFIKClJKCgoKUgJIADhaCgpSmgoK4goKU")]
// </GoSourcePositionMaps>

namespace go.net.http;

[GoPackage("httputil")]
public static partial class httputil_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
