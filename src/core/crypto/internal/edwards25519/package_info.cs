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
// </ImportedTypeAliases>

using go;
using static go.crypto.@internal.edwards25519_package;

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
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<affineCached, ж<affineCached>>(Indirect = true)]
[assembly: GoImplicitConv<projCached, ж<projCached>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/internal/edwards25519/edwards25519.go", "edwards25519.cs", "ADJQooKCAAwekoKCggAJEpIADBiSpoKCgoKCpoKCgoKs4oKuxqKmgoSigoKEgoIACBgAEBKCggAFEIKWgpaCgqiChIKCgoSmsoKqkoKCgqaCgoKCpoKCgoKCpoKCgoKCAAwWgoKCgoKmgoKChIKCgoKCrLKCgoKokoKCgqaC1IKEgoKChISCgoKCpoLUgoSCgoKEhIKCgoKmgtSChIKChISCgoKCpoLUgoSCgoSEgoKCgqqStIKCgoKChIKEgoKssoKCgoKCqJKEsoKCgoSssoKCgoKokoKCgqiygoKosoKC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalar.go", "scalar.cs", "ABtyAB0CquSCqOSCqOSCqNSCqOSCqMKCAAUS4oIACxyCgoKChAANGsKClIKCkpLs0oKUgpaSlAAFEKKCloKUpLYABRwADwqCmqKCgoKCqJaiprKCgpKowoKCgpKCgoKCgoKsAAgIgoKUgqSWgoSCloKEgoKCgoKClKaohO6CloKClIKWlKaCgoKWloKCqIKCgpY=")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalar_fiat.go", "scalar_fiat.cs", "ADqOAQAPAoKCAAIcAAsCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoIAAhwACwKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCAAIcAAsCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCggACGgAKAoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoIAAigAEQKCAAIaAAoCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCggACGgAKAoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoIAAigAEQKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCAAIqABQCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKC")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/scalarmult.go", "scalarmult.cs", "AA8WooKCgoKCuAAKGPIACxqEgoKWgoKCgqiCgoKCgoKCgpaCgoKWrOKEggAHEpaCgoKEgoKCgoKCgoKCgoKCgpSCqqKClAAKGPIADSCCgqaCloKCgqiCgoKCuoKWgoKCpIKCloKCgqSCgpaWgg==")]
[assembly: go.GoPositionMap("crypto/internal/edwards25519/tables.go", "tables.cs", "ACRE1oKSkri6toKSkpS6toKSgpKSgrqSgpKCkpKCvuSChIKUgqaoxIKEgpSCpqiyqLI=")]
// </GoSourcePositionMaps>

namespace go.crypto.@internal;

[GoPackage("edwards25519")]
public static partial class edwards25519_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct affineCached {}
    [GoValueClone("points")] internal partial struct affineLookupTable {}
    [GoValueClone("table")] internal partial struct basepointNafTablePrecompᴛ1 {}
    [GoValueClone("table")] internal partial struct basepointTablePrecompᴛ1 {}
    internal partial struct fiatScalarInt1 {}
    internal partial struct fiatScalarMontgomeryDomainFieldElement {}
    internal partial struct fiatScalarNonMontgomeryDomainFieldElement {}
    internal partial struct fiatScalarUint1 {}
    internal partial struct incomparable {}
    [GoValueClone("points")] internal partial struct nafLookupTable5 {}
    [GoValueClone("points")] internal partial struct nafLookupTable8 {}
    internal partial struct projCached {}
    [GoValueClone("points")] internal partial struct projLookupTable {}
    internal partial struct projP1xP1 {}
    internal partial struct projP2 {}
    public partial struct Point {}
    [GoValueClone("s")] public partial struct Scalar {}
    // </TypeAccessibility>
}
