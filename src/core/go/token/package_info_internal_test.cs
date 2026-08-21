// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.go.token_package;
using static go.go.token_internal_test_package;

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
[assembly: global::go.GoPositionMap("go/token/position_test.go", "position_test.cs", "AA8egoKUgpSClIL4goKUgoKCABQmgoKCgpSUpqKCgoKClIKCgriCgoKCpqaCgoKUgqiCgpSClIKogoKCpoKClKiAgqSClIKUloKUlIKClPiCgoKUgoKmgoKCgoK4goKCgqaUgoKCgpSClILMkoKClICCyIKCgoKClIKCgs6igoKUgoKCgoKCooKUpqqigqyigpSWsoKClJaCAAoGggACFIKCgpaCgoKCgoKCmKKCloKCgqiCgoKUgoKClIKClILogoKCgoSCgoKCAAoKgoKCgoKCgoSSgIK2koKAgJKCqIKCgoKWgoKCloKCggAIBoKaAESUAbKSgoKClILctIKCgrqCgoKWgIKkgIKkgIK4gIKkgIK4goCCpIKAgqiSgoKAgqaCgII=")]
[assembly: global::go.GoPositionMap("go/token/serialize_test.go", "serialize_test.cs", "AA8g8pSogoKChIKWgpaCgoKUgpSClIKCgqaCgoLM1oKChoCCgqSChoCCgqSAgsiCgpSCgpSCgoKCgqY=")]
[assembly: global::go.GoPositionMap("go/token/token_test.go", "token_test.cs", "ABASogAKILKSgII=")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("token")]
public static partial class token_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
