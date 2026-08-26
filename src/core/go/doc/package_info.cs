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
global using commentꓸText = go.go.doc.comment_package.ΔText;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
using ast = go.go.ast_package;
// </ImportedTypeAliases>

using go;
using static go.go.doc_package;

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
[assembly: GoImplicitConv<ast.FieldList, ж<ast.FieldList>>(Indirect = true)]
[assembly: GoImplicitConv<ast.InterfaceType, ж<ast.InterfaceType>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/doc/comment.go", "comment.cs", "ABdAABQCgoKCggACNAAXAoLK")]
[assembly: global::go.GoPositionMap("go/doc/doc.go", "doc.cs", "AH3yAeKCgoKCAA4igoKChKaCgoLKgoKUlIKCgoK4goKCgoCCpJQABzQAGQSClILIgqKUtLi4goKClICktLQABxCCgoKuwoKUgoKUAAIU8oKUAAIU8oCCgpSkgpSuwgAFFOYAAhDyAAIQ8gACFAAJAg==")]
[assembly: global::go.GoPositionMap("go/doc/example.go", "example.cs", "AE9mABECgoKCgoKCgIKCpIKClIKCgoKUgpSAgqSSlIKClKIACxi4gpSmhsyygJSCgIKClJSCgpTGrLKClJKUguqihKaogoSClIKUgoKC2IKUtIIACBCWgoKusrqCkoKCppaCgoKUppSCgoKUgqbGlJKSgoKCzIKYkoKAgsqCgsyWgpSCxoLc6pbMgoKChIaaAAYcAAsMgoKChILClIKihIKCgraqsqqylJaCgoLMpoKWhIKGssaClIKCtIKUggAHHgAIAoKUtISSgpSCxoKe0oKUkoKCgoKCgriCyJSCppKClJTapoKCpoCCgqSUqqKCgoKUqKSClIqigoKClJSCptq0goKYkqKAlJKCgqSokoKCgqrUgoKoyoKCgqjSgpSSgoKUgpSUAAIcAA0CgqaCgoKClJSCgpSCgoKUlIKClLrugoKClIKClIKCuoIAAxbSgoKUgpSUAAISAAkCgpSClIKmgoI=")]
[assembly: global::go.GoPositionMap("go/doc/exports.go", "exports.cs", "AAwgooKCgoKmyoKCgoK4goKClLSAgqSAgsaClKrCgoKUpqiSgoKmqJKCgoKCgJSAgsaCgqaClK7ygpSCgoKCgJSCgoLaggAHEKaCgpSCtoKCgqaClIKosoKCAAQQssjExJLGkoLGtILGgoK0gsaCtMiClqSS7oKCpoKCgtyAooKkgpTGrLKUpICU/KaChqKCgpSUlJS6goKCgqamgpSCrNSosoKCgoKm")]
[assembly: global::go.GoPositionMap("go/doc/filter.go", "filter.cs", "AAwWooKCgoLKpoKClIKC2IKYlKLGguqmgoKCgoKmpoKCgoKCpqaCgoKCgqaCgoKClIKCpqqigoKCgg==")]
[assembly: global::go.GoPositionMap("go/doc/reader.go", "reader.cs", "ABtA8pSkpraSgoKCgoKClIK21oKAgqSu4pKA3LaSgpSAgqSU7oK+0oKCgpSUAAYY0pSkpKSAptaklAAxcoKuwoKUgIK23IKu8oKClICCgoKkpsaCgoKUpoKClKaCgpSCpqgACAqCgoKCgoKClIKWgJLa1JSmgpSClIKogqiCgpSAgrjcgtyowoKUgrSkgpSowoKCupaClJSCgpSKsoKCgsySqMSCqJSmlIKmlICC/rqCgoKCgoCmpICCppSAgoKCggAIDoKCuqrCgpSCgoK4AAYWooKCgoKClIKCuICCpKiSgoDKkoKCAAoe0oKCgoKCgpSmgszUgoKCuoKUlpKAgoCCgoKCgoKmgoKUgoKkAAkQtvyAgqSUgoDKAAoWAAoQgoK4xIKCgoKCgqiCgoKUloKCgpSWgoK6goKAggAGEsKCqJKSgoKCgoKUlpKWkpaSgpSEqLKC3IKUgqaCpqiSlJQABxKCggAEEsKCgoTulKamgpSAguyCAAQQooKCgoKUgqiSgoCCtqaCgoKCgoKmhIKCgpSWpoKCgoIACBKWiKaCgpSmgoKClMyC9oKGqqKCgpSuwgA3drKOgoKAgoKCyIKAgqQ=")]
[assembly: global::go.GoPositionMap("go/doc/synopsis.go", "synopsis.cs", "AAwgsqKigpSClIKUlK7CggANKAAIAoKCgoKmgoKCgpSAgqSC")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("doc")]
public static partial class doc_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct embeddedSet {}
    internal partial struct methodSet {}
    internal partial struct namedType {}
    internal partial struct reader {}
    public partial struct Example {}
    public partial struct Func {}
    public partial struct Mode {}
    public partial struct Note {}
    public partial struct Package {}
    public partial struct Type {}
    public partial struct Value {}
    // </TypeAccessibility>
}
