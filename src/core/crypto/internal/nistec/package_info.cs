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
using static go.crypto.@internal.nistec_package;

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
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/internal/nistec/p224.go", "p224.cs", "ADI4kgAGELKCgoKowoKCgu7ilqiCopSCgpSAgqSCgoKogpKYkoKagqKChIKCgqYACQ6CgpSokoKEgoKE1sSCgoKUrLaipqKCloKChIKCgqqmotaigpaChKy2oqaigpaCgqiCgoKo+IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo6IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo0oKCggAEFKKClIKCgrrG2IKCgrqCgqaCgoKCloKChIKCgoSCgpYABxKygoKCgoKClIKCgqbawoKUAAYQgoKCgoKCgoSCgoKWqqKCgoKClII=")]
[assembly: go.GoPositionMap("crypto/internal/nistec/p224_sqrt.go", "p224_sqrt.cs", "ABEgAAwWgoKCgsoABToAFQKEgoKCgoKClIKCgoKUgoKClIKCgpSCgpSCgoKUgoKUloKWAAUQloKCgpSCgg==")]
[assembly: go.GoPositionMap("crypto/internal/nistec/p256.go", "p256.cs", "ABs8kgAGELKCgoKowoKCgu7ilqiCopSCgpSAgqSCgoKogpKYkoKagqKChIKCgqYACQ6CgpSokoKEgoKE1sSCgoKUrLaipqKCloKChIKCgqqmotaigpaChKy2oqaigpaCgqiCgoKo+IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo6IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo0oKCggAEFKKClIKCgrrG2IKCgrqCgqaCgoKCloKChIKCgoSCgpYABxKygoKCgoKClIKCgqaqwoKUAAYQgoKCgoKCgoSCgoKWqqKCgoKClIKoAAEgAA8EgoKCgpSCgoKUgoKClIKCgpSCgpSCgpSCgg==")]
[assembly: go.GoPositionMap("crypto/internal/nistec/p256_ordinv_noasm.go", "p256_ordinv_noasm.cs", "AA0Wgg==")]
[assembly: go.GoPositionMap("crypto/internal/nistec/p384.go", "p384.cs", "ABo4kgAGELKCgoKowoKCgu7ilqiCopSCgpSAgqSCgoKogpKYkoKagqKChIKCgqYACQ6CgpSokoKEgoKE1sSCgoKUrLaipqKCloKChIKCgqqmotaigpaChKy2oqaigpaCgqiCgoKo+IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo6IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo0oKCggAEFKKClIKCgrrG2IKCgrqCgqaCgoKCloKChIKCgoSCgpYABxKygoKCgoKClIKCgqaqwoKUAAYQgoKCgoKCgoSCgoKWqqKCgoKClIKoAAEwABcCgoSCgoKCgoKUgoKCgpSCgoKUgoKUgoKCgoKUgoKClIKCgpSCgpSCgpSCgpSCgg==")]
[assembly: go.GoPositionMap("crypto/internal/nistec/p521.go", "p521.cs", "ABo4kgAGELKCgoKowoKCgu7ilqiCopSCgpSAgqSCgoKogpKYkoKagqKChIKCgqYACQ6CgpSokoKEgoKE1sSCgoKUrLaipqKCloKChIKCgqqmotaigpaChKy2oqaigpaCgqiCgoKo+IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo6IKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoSCgoKo0oKCggAEFKKClIKCgrrG2IKCgrqCgqaCgoKCloKChIKCgoSCgpYABxKygoKCgoKClIKCgqaqwoKUAAYQgoKCgoKCgoSCgoKWqqKCgoKClIKoAAgSgoI=")]
// </GoSourcePositionMaps>

namespace go.crypto.@internal;

[GoPackage("nistec")]
public static partial class nistec_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct p224Table {}
    internal partial struct p256Table {}
    internal partial struct p384Table {}
    internal partial struct p521Table {}
    public partial struct P224Point {}
    public partial struct P256Point {}
    public partial struct P384Point {}
    public partial struct P521Point {}
    // </TypeAccessibility>
}
