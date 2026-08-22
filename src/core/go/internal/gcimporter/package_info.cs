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
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using constantꓸKind = go.go.constant_package.ΔKind;
global using execꓸError = go.os.exec_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using pkgbitsꓸCode = go.@internal.pkgbits_package.ΔCode;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
global using typesꓸError = go.go.types_package.ΔError;
global using typesꓸInfo = go.go.types_package.ΔInfo;
global using typesꓸScope = go.go.types_package.ΔScope;
global using typesꓸSignature = go.go.types_package.ΔSignature;
global using typesꓸTerm = go.go.types_package.ΔTerm;
global using typesꓸType = go.go.types_package.ΔType;
// </ImportedTypeAliases>

using go;
using static go.go.@internal.gcimporter_package;

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
[assembly: GoImplement<anyType, go.go.types_package.ΔType>]
[assembly: GoImplement<bufio_package.Reader, io_package.ByteReader>(Pointer = true)]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.ByteReader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<intReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.ReadCloser>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<readerDict, ж<readerDict>>]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/internal/gcimporter/exportdata.go", "exportdata.cs", "AA4i1IKCgqaClIKCgoKUggAFEAALBIKCgpaEkoCCuIKCuoCCgtyCgpSogoCCgqSUhA==")]
[assembly: global::go.GoPositionMap("go/internal/gcimporter/gcimporter.go", "gcimporter.cs", "ACxM8oKCqoKCgoKCgoKCgIKkloKCgpaWqAAHEgAIAoKWgpiAsqSCgoKCgoKUgqaUlLiSvILmkoK6goKCgpSCqIKClKzygoKmgpSWgIKkgoKUlIKCgoKUqICCuJKClIKUppSUgoKClpSmgoCCpIyUwoKCgILEgqSChIK0tNim")]
[assembly: global::go.GoPositionMap("go/internal/gcimporter/iimport.go", "iimport.cs", "ACA+goKClKaCgoKUACZUAAsCgoKCgIKClMqEgramgoSCloKClIKEABIqhIK4hIKCgoKChIKUgoKCpJaEgoKCloKWhIKClIKCAAYQgpaCqIKGloIAHDq0gIKmgoKWgoSmgoCCpoKClIKCgqaCgIKkgoKmooCCpoKWgoKEgpQAAhLigpSCgqYACxiigoSUhLaEtoKClIK6ooaCooKWgoSCgoKCuoKCgoKCpoQABRCymIKihqKEgoKUgoKCgpSc9oS22IKmwoKUloCkpqaCgqamgoKmgramwoKWlKSmgpSkpKSmpqKEgoKUgpaCgoKCgoKmgpaCgpSClIKCgoK4goKCgoKClKaCpoKCgqaigpSWgpSmooKCooKUgsiigoKCgoKCyqKmgoKmoKKgpKKArIKCpKSkgqSCpKSCpoSCgoKCgoKChIKUpoSCgoKWgoKCiKKCloKWgoKmgpSCgoCUppKmgpiCgoKilIiypoKUgoKUAALbAYIABeIBgqaCgoKCpoKCgpSCgpSmgoKClKaigoKCpoKmgoKClKaCgoKUpoKCgpSmlICCtoIABBDEgoKUgoKU")]
[assembly: global::go.GoPositionMap("go/internal/gcimporter/support.go", "support.cs", "ABEigoK4ogAUKt6CgoKWgpSCqKaCgoKCpoIABxKUlKSkpLIAFXSAooAADh6SgoKUgoKClA==")]
[assembly: global::go.GoPositionMap("go/internal/gcimporter/ureader.go", "ureader.cs", "ACVUkqriAAwehIKChKaCgoKWhIKWgpiSgoKmhoSCABs6gtyC3IKqsoKCqIKCgqaipqKAgqaCgs6ExoKClrSUgoKqsoKmxoCCpoKCpqKClKSkpoCCpoSChKqypoKCgpSmsoKCgoKUloCCpoKCgoSCgqaAgqaCpqKArKaCgoKClKamgqSCpKSkpKSkpAACSYIABVCCgoKCgoKCgoSCgoKUpqaCgoKUpqKCgoSCgoKCloKWgoIACBSEpoKEgoKEpoKEgoKWpoKEgoKEqrKEhIKEgoKWpqSCgoKChIKEgpaCgqiAgqaChIKEkpaagoKmgoKCpoKCgqaEgoKEhIqAsoKCgoSCloKCloKCpoSCuIKCAAJ7AAOEAaakhIKCgIKmgoKWgoKClqimos6CAAUQgoKChIKWgoIADR6CooKopoKCgoSChIKmgKKAooCkgoKuwoKU2LaUgoKk")]
// </GoSourcePositionMaps>

namespace go.go.@internal;

[GoPackage("gcimporter")]
public static partial class gcimporter_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct anyType {}
    internal partial struct derivedInfo {}
    internal partial struct fakeFileSet {}
    internal partial struct fileInfo {}
    internal partial struct iimporter {}
    internal partial struct importReader {}
    internal partial struct intReader {}
    internal partial struct itag {}
    internal partial struct pkgReader {}
    internal partial struct reader {}
    internal partial struct readerDict {}
    internal partial struct setConstraintArgs {}
    internal partial struct typeInfo {}
    public partial struct Δident {}
    // </TypeAccessibility>
}
