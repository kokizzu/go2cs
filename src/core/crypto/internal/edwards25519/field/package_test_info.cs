// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.crypto.@internal.edwards25519.field_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
// </ImportedTypeAliases>

using go;
using static global::go.crypto.@internal.edwards25519.field_internal_test_package;

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
[assembly: go.GoPositionMap("crypto/internal/edwards25519/field/fe_alias_test.go", "fe_alias_test.cs", "ABgYgqK2gIK4gIK4uIKyxoCCuIKAgraCgIK4gIK4goCCtoKAgraCgIK4AAkcAAoCABAwggALIIKUtLSC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/field/fe_test.go", "fe_test.cs", "AEcogqqigoKUpqKCAClWogAIEqKClKrCroLEgoKWgoKCgoSWgILIgoKCgoKWgoKCgpaCgoKCgoKCggAJCIKiqISUgIKmorqCgpSAgqgACR6ygoKCyoKClKaCsoSCgoSCloKElICCyrKCloKigoKUgqiCpoKCgpSokoSCgoKClIKopoKCgIIABBL+0oKUgoKEgpaEgoKUhIKEgriCkpSCgpaCgriCkoKUgoKEgpaEgoKUhIKChIKWkoKAgqTIgpKUlIKEgpaEgpaEgriCkoKCloKEgoKWloCCAAsIhAAnXIKCgoKCgsqCkoKEgoSClpaAgqaCuIKCkpSChIKWloCCyIKCkpKSlIKEgoKWmICCyIKCgpQ=")]
// </GoSourcePositionMaps>

namespace go.crypto.@internal.edwards25519;

[GoPackage("field")]
public static partial class field_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
