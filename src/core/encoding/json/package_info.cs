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
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
// </ImportedTypeAliases>

using go;
using static go.encoding.json_package;

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
[assembly: GoTypeAlias("Token", "ΔToken")]
[assembly: GoTypeAlias("ΔToken", "object")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<InvalidUnmarshalError, error>(Pointer = true)]
[assembly: GoImplement<MarshalerError, error>(Pointer = true)]
[assembly: GoImplement<RawMessage, Marshaler>(Pointer = true)]
[assembly: GoImplement<RawMessage, Marshaler>]
[assembly: GoImplement<RawMessage, Unmarshaler>(Pointer = true)]
[assembly: GoImplement<SyntaxError, error>(Pointer = true)]
[assembly: GoImplement<UnmarshalTypeError, error>(Pointer = true)]
[assembly: GoImplement<UnsupportedTypeError, error>(Pointer = true)]
[assembly: GoImplement<UnsupportedValueError, error>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<jsonError, error>(Promoted = true)]
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
[assembly: go.GoPositionMap("encoding/json/decode.go", "decode.cs", "AEDCAQBKCLKCgpaCABgygoKUAAwaggALEoKCloKUpqKCgpaCpoKClM6QppKokgAULJIABxCigoKCgpSUqqKCupKClIK2qLKigoKCgoKCgsyygoKUgrzCooKCgoKCgqiCAAIWAAoCkoKUkpS0gviSyPi0tPSClJSs0pqCgIK2lKaCgIK2lKiChJKAggAFOQACQgAFEuKagqaCpAAEEwACGgACEAAQGIK6goKUpoKCgoKCqIKWgsyCgpSClIKAgqSCgILKgoKUpqrUgoKCgpSCgoKUlrSUgoKWpJKCtLaClIKCqIKClIKolICCyICCtpaClIKUgqiCgoKmpoKUAAYQ1IKCgoKUgoKClIKWgoKClsyYuqKCgraCtqaSgqaCgoKWlIKUlIKogoKCgoKYkoSCgoKUlJSCgpSCgoKCgtyCpoKClJSUlIKUgqS6gpSClISCpICC1oCC5raAgtyCgoKCgoCCpJSUgrSCgoKClILEgoKCgpSCtLaCuoKUuIKUgpSCpqqigpSCgpQACRTklIKUgoKClIKCgoKUkpS0tIKUgoKClJSWhICosoKUlNiGwoKUAAEQpIKUAAMVgpQABhqCkoKUlKiCgpSCgoKClLSCgpSkgpQAAy0ABTiSgpSUAAIagqKClIKClMaCgoKUxoKCgpTGgoKClAAEV4aClIKUAAdMAAIQAAgCmIKkgqSvAAIUqLKClIKClpaClIKUgqaosoKUgpSUgqiCgoKCgqiClIKUlpaClIKUgqas5IKEhICkpqaCspSmkpSCgpTcooKUgqKUtLS0pJSq0oKCAAIYAAwCgpS6goKCgpSCgpSCgpSUgpaCgriCgoKUgKSCgpSUpIKCtIKCtIKCtIKCtIKCtIKCtIKCgpSCgoKAlIKCtpTqqIKCuIKCAAgG")]
[assembly: go.GoPositionMap("encoding/json/encode.go", "encode.cs", "ADjAAgCEAQKChIKClITcsoKClIKCgpQADh6CAAkUggAMGoIADRaCgoKUrJAAFSqCgIKCgoKUgqQACRCygoCCgJKU2ILokqaClKykpoIADhyCgpSmgoCCruiCgoKUgqiCgoIABRTqgpSClIKUgpaUpKSkpKSkpKSkpKSkyIKmooKClIKCgpSCgoKCgpSCuKKCgoKUgoKCgoKClIK4ooKClIKCgpSCgpSmooKCgpSCgoKUpoKCgoKCpoKCgoKCpoKCgoKCyqKCggAGEIKCgpSCgqaClIKCgqaCAAUQooKmgpSClIKCgoKClIKClAADHgAOCoKogoKCupSmtoKC2oKCgsyCgoKCgqaCuqaCgoKUpqIADBqCgoKCloKCgoKUlJaClIKCgpSUgrSClLiCgu7CgoKUgKaCgIKkgqSG2oKAgqSUiIKClIKClILWgrqCtoKmgoKCloKCgoKCAAwQwoKClIDKmICCpIKkgtaUgoKCpoLugoKCgoKUlKaCgu7CgoKUgKaCgIKkgqSC1oKC7oKClLyigqaCgpSCzOamgoKClJQABxCCgpSAgoKUgqSUpKSmooKCgoCCgoKUgpS0tLS0tL4ACASCguyCgpSCgoKCgoIACBKCgoKCgpSUgoIAFUIADgSChraGlpSCgoSCgpSWgoKCgoKUlOyUgoKUgoKUgoKEgpSogoKcyoKCgpQABxCWgoKEgsqUqIKCzLiAgqSAgqSCgpSUAAYSgqaCgoKCgqaSgpSCgqiCiIKClIKCgpSAgrYAAhIACQiClNySgIKkgqaCgpQ=")]
[assembly: go.GoPositionMap("encoding/json/fold.go", "fold.cs", "AAscpJKmgpSAgoKUgoK2goKUqJKCgoKU")]
[assembly: go.GoPositionMap("encoding/json/indent.go", "indent.cs", "AAkc8oKmpoKCgoKCpoKCgqaqwoKCgoKmooKCgoKCgoKUgqaCgpSClIKCgpSClKaClIKU1oKCgoKUAAosAA0CgoKCgqaigoKCgoKCgoKClIKUgoKCuoKCqJaCxIK0tJSUgpS0xoKU")]
[assembly: go.GoPositionMap("encoding/json/scanner.go", "scanner.cs", "ABIssoKC3NKCgoKCpoKUAAkUgAAkUoKUgoKmtIKUAC9YooKCgqrCgpSClIKClIKU2qKCgpSqooKCgoKUuIKokoKUgpTYsoKUlIKkgqSCpIKkgqSCpIKkguSSgpSosoKUgoKClNiygpSCgpT6woKUgoKUgoKUgpSCgoKUpIKCgpSCgpSkgoKUgoKUpNzSlJTYsoKClIKClIKU2LKUgqSCpNiygoKmqLKCgqaosoKCpqiygoKm2LKCgpSCgpSqwoKClKiygoKUgoKU2sKCgpSqwoKUgoKUqsKCgpTawoKClKyygpTYsoKClNiygoKU2LKCgpTYsoKClNiygoKU2LKCgpTYsoKClNiygoKU2LKCgpSosoKClKqiqJKCgqikgpSCqII=")]
[assembly: go.GoPositionMap("encoding/json/stream.go", "stream.cs", "ACY+wqqgqrCu8oKWgIKmgqiCgpSCupaEqqKqwoSCgoa2goKCmoKq4oK2gtyCgoKUgqaCloKCtKamgoKCgpiSgoKCqIKEpoKCgqYADB6SAAISAAoCgpaChIKCAAcShIKCgoKUlICCpNyyggACFPIABBKSgpTYsoKUggAQOsiUgoKUgpSCpIKClIKUgqSmgpSkpoKUpO6CAAIcAA0CgoKClKSClIKCgqaClIKCgoKmgpSCgoKmgpSCgoKCpoKUgoKmgoKClIKCgpSmgoKCgoKCgpSClKaClIKAgqQADAqCgpSkpKSkpKSqooKmgoKCgoKClIKmgpS+sg==")]
[assembly: go.GoPositionMap("encoding/json/tags.go", "tags.cs", "AAsiooKssoKUgoKCgoKm")]
// </GoSourcePositionMaps>

namespace go.encoding;

[GoPackage("json")]
public static partial class json_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct arrayEncoder {}
    internal partial struct condAddrEncoder {}
    internal partial struct decodeState {}
    internal partial struct encOpts {}
    internal partial struct encodeState {}
    internal partial struct encode_ptr {}
    internal partial struct errorContext {}
    internal partial struct field {}
    internal partial struct floatEncoder {}
    internal partial struct jsonError {}
    internal partial struct mapEncoder {}
    internal partial struct ptrEncoder {}
    internal partial struct reflectWithString {}
    internal partial struct scanner {}
    internal partial struct sliceEncoder {}
    internal partial struct structEncoder {}
    internal partial struct structFields {}
    internal partial struct tagOptions {}
    internal partial struct unquotedValue {}
    public partial interface Marshaler {}
    public partial interface Unmarshaler {}
    public partial struct Decoder {}
    public partial struct Delim {}
    public partial struct Encoder {}
    public partial struct InvalidUTF8Error {}
    public partial struct InvalidUnmarshalError {}
    public partial struct MarshalerError {}
    public partial struct Number {}
    public partial struct RawMessage {}
    public partial struct SyntaxError {}
    public partial struct UnmarshalFieldError {}
    public partial struct UnmarshalTypeError {}
    public partial struct UnsupportedTypeError {}
    public partial struct UnsupportedValueError {}
    // </TypeAccessibility>
}
