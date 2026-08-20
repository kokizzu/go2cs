// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.crypto.tls_package;
using static go.crypto.tls_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<brokenConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<brokenConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<brokenSigner, crypto_package.Signer>(Promoted = true)]
[assembly: GoImplement<changeImplConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<changeImplConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<discardConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<discardConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<go.crypto.tls_internal_test_package.zeroSource, io_package.Reader>]
[assembly: GoImplement<hairpinConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<hairpinConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<opensslInput, io_package.Reader>]
[assembly: GoImplement<opensslOutputSink, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<readerFunc, io_package.Reader>]
[assembly: GoImplement<recordingConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<recordingConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<replayingConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<serializingClientCache, global::go.crypto.tls_package.ClientSessionCache>(Pointer = true)]
[assembly: GoImplement<slowConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<slowConn, net_package.Conn>(Promoted = true)]
[assembly: GoImplement<stringSlice, flag_package.Value>(Pointer = true)]
[assembly: GoImplement<writeCountingConn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<writeCountingConn, net_package.Conn>(Promoted = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<clientTest, ж<clientTest>>(Indirect = true)]
[assembly: GoImplicitConv<serverTest, ж<serverTest>>(Indirect = true)]
[assembly: GoImplicitConv<testQUICConn, ж<testQUICConn>>]
// </ImplicitConversions>

namespace go.crypto;

[GoPackage("tls")]
public static partial class tls_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
