// go2cs code converter defines `global using` statements here for imported type
// aliases as package references are encountered via `import' statements. Exported
// type aliases that need a `global using` declaration will be loaded from the
// referenced package by parsing its 'package_info.cs' source file and reading its
// defined `GoTypeAlias` attributes.

// Package name separator "dot" used in imported type aliases is extended Unicode
// character '\uA4F8' which is a valid character in a C# identifier name. This is
// used to simulate Go's package level type aliases since C# does not yet support
// importing type aliases at a namespace level.

// <ImportedTypeAliases>
global using bigmodꓸNat = go.crypto.@internal.bigmod_package.ΔNat;
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
using nistec = go.crypto.@internal.nistec_package;
// </ImportedTypeAliases>

using go;
using static go.crypto.ecdsa_package;

// For encountered type alias declarations, e.g., `type Table = map[string]int`,
// go2cs code converter will generate a `global using` statement for the alias in
// the converted source, e.g.: `global using Table = go.map<go.@string, nint>;`.
// Although scope of `global using` is available to all files in the project, all
// converted Go code for the project targets the same package, so `global using`
// statements will effectively have package level scope.

// Additionally, `GoTypeAlias` attributes will be generated here for exported type
// aliases. This allows the type alias to be imported and used from other packages
// when referenced.

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<PublicKey, go.crypto.elliptic_package.Curve>(Promoted = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P224Point, nistPoint<go.crypto.@internal.nistec_package.P224Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P256Point, nistPoint<go.crypto.@internal.nistec_package.P256Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P384Point, nistPoint<go.crypto.@internal.nistec_package.P384Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P521Point, nistPoint<go.crypto.@internal.nistec_package.P521Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.cipher_package.StreamReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<zr, io_package.Reader>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<PrivateKey, ж<PrivateKey>>(Indirect = true)]
[assembly: GoImplicitConv<PublicKey, ж<PublicKey>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/ecdsa/ecdsa.go", "ecdsa.cs", "AD964oKClIKUAAIQ0oKClAAQIrKCgpSCgpSmgpSkpKTKkqyygoKUqtIAAhTyAAIQ0oSCgoKUlISUpKSkpMjCgoKWgoKCgoKUqgAIAoKCgoCC7oCmgpTugIKmgqiCAAomAAoChIKCgpSUhIKCloCCppSkpKSkAAgI1oKCqIKEgoKUgoLMgpaChIKClIKCloKWpoKCooKU2sKClIKClJKClLrSgpSCgoKUzKoACAqAgoKAgoKCgoLqgoIAAxAADxqCgIK4goKCgqiCgpqiAAoWkoIAAhDygoKClJSEgIKmlKSkpKTIwoKCloKCqoKClIKCloKWgpaCgqaCgqaCgpaCgpbWsoKCipQAGSrylIKUgqaCgoKCgtjigpSUgoKC/IKCppT8goKmlPyCgqaU/IKCppSmooKCgoKClA==")]
[assembly: go.GoPositionMap("crypto/ecdsa/ecdsa_legacy.go", "ecdsa_legacy.cs", "ABIoooKCloKCgoKssoKCgpaCgoKUAAcUAAkCgoKWgoKCipSmopaCgpSygoKCgpaEgoKCqIKCgoKCgqgAAhIACQKClIKClKaCgoKUhIKEgpSCqIKEgoKChIKChIKUgs4ACAiCgoKAgqSAgqSCgg==")]
[assembly: go.GoPositionMap("crypto/ecdsa/ecdsa_noasm.go", "ecdsa_noasm.cs", "AAoWgqaC")]
[assembly: go.GoPositionMap("crypto/ecdsa/notboring.go", "notboring.cs", "AAsWgqSC")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("ecdsa")]
public static partial class ecdsa_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface nistPoint<T> {}
    internal partial struct nistCurve<Point> {}
    internal partial struct zr {}
    public partial struct PrivateKey {}
    public partial struct PublicKey {}
    // </TypeAccessibility>
}
