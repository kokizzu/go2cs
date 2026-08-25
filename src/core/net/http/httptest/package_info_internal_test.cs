// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.net.http.httptest_package;
using static go.net.http.httptest_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestNewRequestWithContext_typeᴛ1, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestNewRequestWithContext_typeᴛ1, io_package.Reader>]
[assembly: GoImplement<onlyCloseListener, net_package.Listener>(Promoted = true)]
[assembly: GoImplement<onlyCloseListener, net_package.Listener>]
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
[assembly: go.GoPositionMap("net/http/httptest/httptest_test.go", "httptest_test.cs", "ABMkgoIAChaCgoIAEgiCAH6SApKCgoKUgpSCgoKUgpSClII=")]
[assembly: go.GoPositionMap("net/http/httptest/recorder_test.go", "recorder_test.cs", "ADEckpKQlAAFEAAFEAAFEAAKGgAFEAAFEAAHEAAHEJKSgoKCpqYABxCSkoKCgoKmpgAHEgAXGoKC7oKC7gAKGILu7oL+ggAJFILugoKC7oKCgoKCgoKCAA8gzAAMGoKCgoIAChiCgoL+ggAIDJKCgoKCgoCCAAwQkgAWNoKAggAJDqKmgoKigoCCuIKUgoI=")]
[assembly: go.GoPositionMap("net/http/httptest/server_test.go", "server_test.cs", "ABoygoKmgoIACQiCgoKCkJKQkpCSkJKQtoKCgpCSkNqigpSSgoKUgoKClILqkoKWgoKUgoKClIKWhIKCgrjCgpSSgoKUqIKmgpKCgoKW2LKCgpSCgoKC7MKClJKCgoKUgoKClILswpSSgoCC/MKUkoKAggALEICowrraooKygoKClIKClMaCgrKCgoKWkoKCgpTGgoKypoLGgrKCxAAKBoIAAxCysoKWlIKkppSCgpSAkg==")]
// </GoSourcePositionMaps>

namespace go.net.http;

[GoPackage("httptest")]
public static partial class httptest_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
