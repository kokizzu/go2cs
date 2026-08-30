// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using bufio = go.bufio_package;
using testing = go.testing_package;
using Δhttp = go.net.http_package;
// </ImportedTypeAliases>

using go;
using static go.net.http_package;
using static go.net.http_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestRequestWriteError_w, io_package.ByteWriter>(Promoted = true)]
[assembly: GoImplement<TestRequestWriteError_w, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<TestRequestWriteError_w, io_package.Writer>]
[assembly: GoImplement<closeChecker, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<closeChecker, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<go.net.http_internal_test_package.delegateReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.net.http_internal_test_package.dumpConn, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<go.net.http_internal_test_package.dumpConn, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<go.net.http_internal_test_package.dumpConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<go.net.http_internal_test_package.roundTripFunc, global::go.net.http_package.RoundTripper>]
[assembly: GoImplement<headerOnlyResponseWriter, global::go.net.http_package.ResponseWriter>]
[assembly: GoImplement<issue22091Error, error>]
[assembly: GoImplement<mockTransferWriter, io_package.ReaderFrom>(Pointer = true)]
[assembly: GoImplement<mockTransferWriter, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<readerAndCloser, io_package.Closer>(Promoted = true)]
[assembly: GoImplement<readerAndCloser, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<readerAndCloser, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<writerFunc, io_package.Writer>]
[assembly: GoImplement<Δhandler, global::go.net.http_package.ΔHandler>(Pointer = true)]
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
[assembly: global::go.GoPositionMap("net/http/cookie_test.go", "cookie_test.cs", "AOUB0gKigoKEgoCSuICSAAkMgqaCpoLmgoKCgoCCpICSpICSAChGgoKCgpSAggCTAaQCgoKClKaigpKCggBBgAGigpKCggALDIKCgoKCgoLcgpSCgoIADQqigoKEAA8mgoCCuICSAAsIooKChM6CgIK4gJIADAiCAA0kgoKClILKooIABxCCgoKClICSyKLMABIqgoKCgpSCuKK6AAsYgoKCgpSCAAkIggAhTIKCgpSCAAoKggCNAaQCgoKCgpSC")]
[assembly: global::go.GoPositionMap("net/http/export_test.go", "export_test.cs", "AE9SuISCgoCCtoKUggAIDIKCAAgSooKq0oKCpoCkgoKSlKaC3pKmwoKC1tKCgoKClILWwoKC1sKCgoKCgqaC1sKCgoSClIKCgrqC1sKCgoKCgoKm1sKCgtbCgoLWgqrCgpS4goKUgpYACRTClLiCgpSClgAGEsKCuIKSgriigoKUgqbCgoKCgoKm1sKCgoKCgpTWgqaCgoKAtoCu8oKCgqaqooCCpKbKqqKCgpQ=", "47-60:1;91-93:1;253-256:1;298-298:1;339-341:1")]
[assembly: global::go.GoPositionMap("net/http/filetransport_test.go", "filetransport_test.cs", "ACMegoKClAAJCKKEgoKCgoSCgoSCgoKCgpSClIKUgoKCgqiCgoKClNaChKiCgoSCgoKCgpSClIKUgoKCgqiCgoKClA==", "16-21:1")]
[assembly: global::go.GoPositionMap("net/http/h2_error_test.go", "h2_error_test.cs", "ABUugqaCgoKCgpSClIKUgg==")]
[assembly: global::go.GoPositionMap("net/http/header_test.go", "header_test.cs", "AHjYAYKCgoKClAARIIKCgoKCgpSUgoKUggAnUIKCgsqCgoKCAA4aooKCggAICIKClIKUgpSCgpSCAAsMogANJLKSgoKUgpSC", "229-232:1;260-270:1")]
[assembly: global::go.GoPositionMap("net/http/http_test.go", "http_test.cs", "AC8qggAKIoKCgpSCAAcS4oKCgoKUAAkWgoKClIIACw6igpSCgoKC7rKCgoKCAAgMooIACBKCgoKAgraCABcusoKWgoCSgpaClIaWgoKUgoKClKiU7KKEgg==", "41-43:1;159-187:1")]
[assembly: global::go.GoPositionMap("net/http/mapping_test.go", "mapping_test.cs", "ABkegoKClIKUgoKCgqaCgpSClIKAgsiCgoKCgoKYgoKClIaCAAgIgoIAIUSClIKClJKCgpSCgqaSgoKUgoKClJSSgoKUgoKClNyCgoKm", "54-57:1;58-60:2;107-143:1;109-118:1.1;119-130:1.2;131-142:1.3")]
[assembly: global::go.GoPositionMap("net/http/pattern_test.go", "pattern_test.cs", "ABkaooiIkoKClgBbtgGCggAJCqIAFzSCgsqiqIKCgoKU9oIACRqCgoKClIKCggAJCqIAdYoCgoKAgqSAgqSCgoKClIKCggAJCqIAGjSCgoKCuIKCAAkMgoKCgIKkgoKCgpSUgoIACAiCAAcWgoIACgyCAAsegoKClIKCAAkKggAaPIKCgoKUgoI=", "14-16:1;18-20:2;22-26:3")]
[assembly: global::go.GoPositionMap("net/http/proxy_test.go", "proxy_test.cs", "AB04goKCgoKClJSCgILagoKU")]
[assembly: global::go.GoPositionMap("net/http/range_test.go", "range_test.cs", "ADp2goKCgoKUgoKUgoKUgg==")]
[assembly: global::go.GoPositionMap("net/http/readrequest_test.go", "readrequest_test.cs", "AOsCygaCgoKCgoKUlIKCgoKCgoKClJSCgpSCzqIAIzaCgoKCgg==")]
[assembly: global::go.GoPositionMap("net/http/requestwrite_test.go", "requestwrite_test.cs", "AIwCvgWCggANJIKCAKUB3gOCgoSSgpSUtLaCgpaCgoCSgqSCloKCgoKogoKCgoKClIKCggAWDKKEAAUSgoKUgpSWkpKCgIK2qAA0gAGCkpTMsoKUAAkUgoKClICCAAoUgoL8soKCgpSCgIKkgpSMgriCpoKCgpTKgAAKBpKSzMyCgpSCqIKCgoKCgoKCgpSCgpSCgoKmgqaCAAYQ7tKCkpKEyqaigoKCuIKUloKClAAMFIKClAAIEoCigKKAooCigKKA", "616-626:1;670-677:1;671-676:1.1;679-687:2;689-698:3;690-697:3.1;763-769:4;765-767:4.1;866-873:1;924-926:1;931-943:2")]
[assembly: global::go.GoPositionMap("net/http/response_test.go", "response_test.cs", "ADo6gqaCAKkD1giisoKCgpSCgoKCgoKCgpSUgoLKgoKCgoKUgoKCAB8q0oKyooKUkoKUlIKCgpSUgoKUgoKUhIKClIKUlIKClIKUhIKCgoKClIKUgpSCgoKWgoKCgpSClISCgoCShtqCgoKCgpSCgoKUgoKCABMmgoKCgoKCgoKCgqiCgoKClICSgqSUgoKUgJLagtyCgoK4ooKIgoKUkoKUgoKClIKUggAmDrKOgpTegoKU3gAEEIKEACtggoKCgIKCgpTeooKClICCpJSClICCgpSkgpTYkoSCgpaCgIKkgIKmgoCCpICC", "649-652:1;653-658:2;727-729:3;856-865:1;867-876:2;878-884:3")]
[assembly: global::go.GoPositionMap("net/http/responsewrite_test.go", "responsewrite_test.cs", "ABQkogDbAYIEgoKCgoKClIKCgg==")]
[assembly: global::go.GoPositionMap("net/http/routing_index_test.go", "routing_index_test.cs", "AA4cuIKCgoKCgpS4ooKCgqaCpqKCgoKUlIKqooSEkoKCgoKClIKCgpSCgpSWvsqCAAQQkoK6kpKCzqKCgoLesoKUqJKClKqigoLKpJKCgpSCgoKCgoKUlJSAkg==", "43-48:1;58-78:1;102-104:1;109-113:1;119-125:1;120-124:1.1;121-123:1.1.1;147-151:1")]
[assembly: global::go.GoPositionMap("net/http/routing_tree_test.go", "routing_tree_test.cs", "ABwiggAGFIKCgoKClIIADRCCgrimooKCgoKUlAAcBoIAAC6CgoKCABgUhIKCgoKCgpSClIK6AAoYAAsaACNEggAGEIIABhCCAAYQABUQooIAKViSgoKCgtyigoKUgoKWgoKClISCgoKWgoI=", "105-120:1;262-269:1;284-287:1")]
[assembly: global::go.GoPositionMap("net/http/server_test.go", "server_test.cs", "ABYiggAnVoKCggAIDgAQBIKCAAYUlgAQKoKCgoKCgoLKxoKCgoKCgoIADAiCgoKEAAkWooKClIKAggAMDKIACRyCgoKUgoIADQqCABs2soCShIKCpoKCgpSCgoKUpoKAkgAIDJCSkPaCAAcUgoIAFgqigpSCgoKCgoKCgoKCgoKCgoKChJSCgoKClICCtg==", "151-160:1;219-247:1;220-220:1.1;225-225:1.2;249-249:2;250-250:3;272-274:1")]
[assembly: global::go.GoPositionMap("net/http/transfer_test.go", "transfer_test.cs", "ABYmgtqCgoKCloKCgpaCgoLoggABFIKUgoKCgoKUggAICIKCAAkggoKCAAsYgoKmgoIAEgaCgoSCooKCqICCpICCppKClpaYAA8sggAmToIAGzqysoKClJSCzICCpoKCloKCgpS4gqiCqIIADQyCAB1EgsqCggAJDJIAGj6CgII=", "119-139:1;133-136:1.1;141-143:2;142-142:2.1;166-169:3;206-209:4;238-283:5")]
[assembly: global::go.GoPositionMap("net/http/transport_internal_test.go", "transport_internal_test.cs", "ACUs0oKUgsKCgoKClMaCgoKCgoKCgpSUgpSUhIKCloKCguiCgqaCgtaCgoKUgpTWgoKClKSCgoKUpoKCguzSgAAJBIIAPogBgoKC7oL4spKClIKUoriCgoKClICCgqSogoKCABMquIKUgIKkpoKCgg==", "27-35:1;208-225:1;233-248:2;234-247:2.1;250-262:3")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("http")]
public static partial class http_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
