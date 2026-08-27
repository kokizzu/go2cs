// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.@internal.edwards25519_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.@internal.edwards25519_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.crypto.@internal.edwards25519_package.Point, ж<global::go.crypto.@internal.edwards25519_package.Point>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.@internal.edwards25519_package.affineCached, ж<global::go.crypto.@internal.edwards25519_package.affineCached>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.crypto.@internal.edwards25519_package.projCached, ж<global::go.crypto.@internal.edwards25519_package.projCached>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/internal/edwards25519/edwards25519_test.go", "edwards25519_test.cs", "ADUkooKCsoKCgorSgoKCpoKCgsrGgoKAgqSAgqSCpgAIBoKEgoKCgoKUhIKCgoKUgpSClNaCggAICJSCgoCCpKSk9oIAjgGoArKSgoKUgoKUgpSAgqTugoSAgoKCgoKUyIKCgpSmooKCgoKC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalar_alias_test.go", "scalar_alias_test.cs", "ABIYgqK2gIK4gIK4lrLGgIK4goCCtoKAgriAgriCgIK2goCCtoKAgriWABAE1tbW5urqqoKC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalar_test.go", "scalar_test.cs", "ACYmooKClAAMEsKCgsa0toK2goK6ggAIBpKSlKaCkpSAgsiCtIKAgqSClICCpqKAgqSUgIKmgoKCgIKkpMiCgoKygoKClIKClICCyJaCgoKCgIKmgoKCgoCCpoKCgoKAgsiCgoKUpoK0koKGkoKCgoSEloCCyIKkkoaSgoSWgILIgt4ACRaEgoLugoKCgoKkpoKClII=")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalarmult_test.go", "scalarmult_test.cs", "ABMmgoKCgoKUhIKCgpTWgoKCgpSmgoKCgpSmgoKCgoKUgoKClKaCooKCsoKCgoKCloCCyKiSkoKEgoSEloCCyKaEgoKCgoKUlIKogoKClIKCuIKSkoKCgpaAgviCgoSCuIKitISCgoSCloCCzLKEgriihIK4ooSC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/tables_test.go", "tables_test.cs", "AAoWgoKEooKCloKEgoKCgoKEgriCgoSigoKWgoSCgoKCgoSCuIKChLKCgoKWgoKEgoKChIKCgoSCuIKChLKCgoKWgoKEgoKChIKCgoSC")]
// </GoSourcePositionMaps>

namespace go.crypto.@internal;

[GoPackage("edwards25519")]
public static partial class edwards25519_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
