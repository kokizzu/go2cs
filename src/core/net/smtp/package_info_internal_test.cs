// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.net.smtp_package;
using static go.net.smtp_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<TestClientAuthTrimSpace_fake, io_package.ReadWriter>]
[assembly: GoImplement<TestClientAuthTrimSpace_fake, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<TestClientAuthTrimSpace_fake, io_package.Writer>(Promoted = true)]
[assembly: GoImplement<faker, io_package.ReadWriteCloser>]
[assembly: GoImplement<faker, io_package.ReadWriter>(Promoted = true)]
[assembly: GoImplement<faker, net_package.Conn>]
[assembly: GoImplement<toServerEmptyAuth, global::go.net.smtp_package.ΔAuth>]
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
[assembly: go.GoPositionMap("net/smtp/smtp_test.go", "smtp_test.cs", "ACZIgoKCgoKUgpSClIKCgoKCgpSCggASDKQAG0CCgoKCgpSCAA0MkoSCgr6CgpSCgoKCgJIADhSCpoLugKKAooCigKKAooAAEwSigoSCgoKChICCpICCpICCpoKAgqSAgqaAgqaAgqSAgqSAgriCgoCCpoCCpICCpICCpICCpI6CgpSAgqSAgqaAgqaCgoIAFFSisoSCgoKChJaSAAUYhICCpIKAgqSAgqaCgoKCqJIABRqEgIKkgIKkgIKkgIKkgIKmgoKCgqiSAAUchICCpICCpICCpICCpICCpoKCgoKokgAFHISAgqSAgqSAgqSAgqSAgqaCgoKCqJIABR6EgIKkgoCCpICCpICCpICCpoKCgoLKwoKEgoKSgpSCgoKClJKAgqSAgqSAgqaCggAOIMKChIKCgoKCgpSSgIKkgIKmgoKCAA4kwoKClpSCgoKCqLKCgoKUlIKCgtiCgoKUlIKClIIADAikgpaCgoKCgoKCgoKUkoKElIKClLSCgsa0goK0tIKCxrS0goKCgti0toKWgoKCAC5SooKCgoKCgpSWktSEgoKClJSCgoKCgpSClIKCgoKCgoKUgvyMgpaOgpaCgoKCABM4woKClJSCwoKCgoKUlIKCgoKClIKCgpSCgoLYjIKUgpSCggAKCKKCgoKCgoKCgpSUgoKEgqSWgoKCAA4kooKUgpKCopSCgpSSgIKkgIL4ooKSgoLCgoKCgpSSgILmwoKCgoKUkoKCgIKCpJKCgpSC1oIACAaCgoKUgpTuggAICLKCgoKClIKCpIKCgpSCgoKktgAJBoKCgoKUpKSkggAKDIKktqaCgoKS6IKCggAPUoA=")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("smtp")]
public static partial class smtp_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
