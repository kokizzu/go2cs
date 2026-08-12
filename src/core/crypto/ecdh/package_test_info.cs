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
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
global using execꓸError = go.os.exec_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
// </ImportedTypeAliases>

using go;
using static go.crypto.ecdh_package;
using static go.crypto.ecdh_test_package;

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
[assembly: GoTypeAlias("Curve", "ΔCurve")]
[assembly: GoTypeAlias("PublicKey", "ΔPublicKey")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<PrivateKey, _ᴛ2>(Pointer = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P256Point, nistPoint<go.crypto.@internal.nistec_package.P256Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P384Point, nistPoint<go.crypto.@internal.nistec_package.P384Point>>(ConstraintProxy = true)]
[assembly: GoImplement<go.crypto.@internal.nistec_package.P521Point, nistPoint<go.crypto.@internal.nistec_package.P521Point>>(ConstraintProxy = true)]
[assembly: GoImplement<nistCurve<P256PointжnistPoint>, ΔCurve>(Pointer = true)]
[assembly: GoImplement<nistCurve<P384PointжnistPoint>, ΔCurve>(Pointer = true)]
[assembly: GoImplement<nistCurve<P521PointжnistPoint>, ΔCurve>(Pointer = true)]
[assembly: GoImplement<x25519Curve, ΔCurve>(Pointer = true)]
[assembly: GoImplement<ΔPublicKey, _ᴛ1>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<ΔPublicKey, ж<ΔPublicKey>>(Indirect = true)]
// </ImplicitConversions>

namespace go.crypto;

[GoPackage("ecdh")]
public static partial class ecdh_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface nistPoint<T> {}
    internal partial struct nistCurve<Point> {}
    internal partial struct x25519Curve {}
    public partial interface ΔCurve {}
    public partial struct PrivateKey {}
    public partial struct ΔPublicKey {}
    // </TypeAccessibility>
}

[GoPackage("ecdh_test")]
public static partial class ecdh_test_package
{
}
