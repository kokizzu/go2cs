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
global using syntaxꓸError = go.regexp.syntax_package.ΔError;
using syntax = go.regexp.syntax_package;
// </ImportedTypeAliases>

using go;
using static go.regexp_package;

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
[assembly: GoImplement<inputBytes, input>(Pointer = true)]
[assembly: GoImplement<inputReader, input>(Pointer = true)]
[assembly: GoImplement<inputString, input>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<syntax.Prog, ж<syntax.Prog>>(Indirect = true)]
[assembly: GoImplicitConv<thread, ж<thread>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("regexp/backtrack.go", "backtrack.cs", "ADpigoKClKaigqqigpSqoqzShIKUloKClIKWgpSUgpaClJSCvKKCgpSCquaCusKEgoKUgoKCAAcSgoKClISEmAACFAAJBIKClIKCupaigoKmkoKmgoKUgoKmgoKUgoKmgoKUgoKmgoKUgoKmlIKUlIKUgriCgpSCpoKqopyylICCqJKYkpgAAoECAASKAqiygpKUlJaCgpaCgpSCggAIFIKClIKCgpSWgpSUlJSCloKCgg==")]
[assembly: go.GoPositionMap("regexp/exec.go", "exec.cs", "ADxwooKmooKmooKCgqamgqSUuIKClIKUpoKClKqigoCCgpSCpIIABBSCpoKClIKCgpSUgoKUlIKUgoKClJSCgpSUgpSClJSs0oKSlIKClIKCkoKClIKClJSCgpSUlJSUgoKUgoKmgoKUlIKCgpSmlIKCgpSUgqiygoKmAAIQ8oKCgoKClIKClIKCmoKClJSCgqaUpqSkpAACMQACNoKUgqauAAgCgoKUgIKmgoKCgoKEgrySgqSCgraCpIKCgoKUgraClJSClIIAAkUAAkoAChSCgoKUpqKCqLKCkpaCgpSWgoKWhIKSgoKUgoKUlIKUloKUgoKCgpSCgoKYgoKClKSCtoLakriSpKSkgpSkgpQAAk0AAlKClIKCgoKogoKCloKCqJKu4pSWgpaClIKWgoSCgoKWgoI=")]
[assembly: go.GoPositionMap("regexp/onepass.go", "onepass.cs", "ADxSAAkCgoKUgoKCgqaCmJKCgpSGlK7igoKUgpSmgoKUpAAJFIKmooKCpoKCpoKClKaCgriCgpSCgqaCAA8k0oKCgpSWkpKCgoKCqIKCgpSCgoKCloKUtLS0tIKm2JKCtqSC3JLKggAGEIKagoSCooKCgqaEpJiCgoKipIKUgpqiAANLAANUAAYWAAgEgpYABRLCgoKClIKkhIKCkoKWkoKUgoKYpIKCxoKEkoKCtoKCgoKCtrSCgpSCgoKClIKCgoKClJSUgoKClMSCgpSChJKCgoKUlJSCgoKUxIKClIKCxIKClIKCgoLGloKCgoKCgoKCpoKCpq7igqaEpoKCnIK2goKUAAMXggAGIpaEgpQ=")]
[assembly: go.GoPositionMap("regexp/regexp.go", "regexp.cs", "AHLOAZIAAhYACAKSAAIaAAoCAAIsABMCAAIS4qaCgoKUkoSCgoKUkoKUAAoWgoKUlKaCloKCgpSEAA0i0oKClIKCgoKCzIKSlIKClKiygoKCqJKYpIKCgpSmpKSkgoKUpIKCgoKCpgACPwAETrKCgpSssoKClKaCgpSokgACENIAAhTygoKCuAATKIKCgoKUlKaCpqKmoqaClIKCgriCgoKmAAcQgoKCgpSUpoKmoqaipoKUgoKCuIKCgqYACRSCgpaCgoKUgqaCpoKmgqaCrLKqoqqiqqKs0oKClKzSgoKUrNKCgpSs0oKClIassgACEsKGpqKCgoKCgpSUgpaCgoKCqIKU3oKUhpKClJSCyJS6gpSWrNKCgpSCooKUlKyyAAMSwgAGEpKmgoK+tJKCgriCloKCgoKCgpSClAACENKUlIKClKzSgoKUloKCgpaClKaUgoKClIKUgpSmlISCgs6igoKClK7CgoKUAAIQ0oKCgpSuwoKClAACENKCgpQAAhDygoKClIKCgqYAAigAEQKssqaCgoKClIKClIKClIKUgpSCgoKClLiCgoKUlMqCrAAIAoKUgoKClIKCgoKUlJSUgoKUlKiCgoKClKaCloKCAAIQ8gACEPKCgoKUgoKCpgACEPIAAhDyAAQSwoKUgpKClJSuwoKUgoKClJSuwoKUgoKClJSuwoKUgoKClJSuwoKUgpKClIKCgqaUrsKClIKCgpSUrsKClIKCgpSCgoKmlAACENKClIKCgpSUAAImABIEgpaCloKEgoKCgpaCgpSWgpYAAhLiqqKCgpSC")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("regexp")]
public static partial class regexp_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface input {}
    internal partial struct bitState {}
    internal partial struct entry {}
    internal partial struct inputBytes {}
    internal partial struct inputReader {}
    internal partial struct inputString {}
    internal partial struct inputs {}
    internal partial struct job {}
    internal partial struct lazyFlag {}
    internal partial struct machine {}
    internal partial struct onePassMachine {}
    internal partial struct onePassProg {}
    internal partial struct queue {}
    internal partial struct queueOnePass {}
    internal partial struct thread {}
    public partial struct Regexp {}
    public partial struct onePassInst {}
    // </TypeAccessibility>
}
