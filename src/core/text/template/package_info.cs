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
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using urlꓸError = go.net.url_package.ΔError;
using parse = go.text.template.parse_package;
// </ImportedTypeAliases>

using go;
using static go.text.template_package;

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
[assembly: GoImplement<ExecError, error>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<parse.Tree, ж<parse.Tree>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("text/template/exec.go", "exec.cs", "AEkwgoKUABMqkqiSqJKqooKCgqaokqiSgoKmggALFoKokqqiAA4egqaCqLKCgpSClAANHILOwoKClLS0xAAIGAAJAoKClAACGgAKAqbigoKClMqClIIACA4ACAKClIKCgoKClIKUlJQAChbCgpiigsbmtLSCxrS0gILWxMzigoKCgpSCgpS27rKmspSUlKSkpKSkpKSkpKbSgoKAgraClIKigqaClMqmgoK4poKUgIK2xKSClIKUxIKUgoKUxIKUgoKUgoKCgpSUgpTEtKSC6KKCgoKUgqaCkoKUggACFgALAoKUgoKClIKmgoKUpqaCgriigpSkpraSpJSCgpSkpLSklILeAAkIgpSqpoKClKa0poKmgqaigqaigoKUgqaCprSCgoKClKyygoKmpqKCgoKClKyygpKUlIKCpoK6goKUgIKklJSCgoKClIKmgpS4gpKClIKCuKTGxoKCgKa2gsaCAAYYsoKUgoKClIKCgoK2lICCpoKClKiCgoKCgqam7pSolIKCpoKCgriCgoKmuKa6goKClqaCgpSokpSkpKiSgpSUlJSUgpSCgoKCAAcQlIKCxrTGpoKClKSClLSkpKSklJSkpKSkgraCtqSkgqaCgoCCgoKkgqaCgoCCgoKkgqaCgoCCgoKkgqaCgoCCgoKkgqaCgoCCgoKkgqaCgIKCgqSCpoKClKSkpKa0pKS0lIKssoKCpq7CgpSClKqigoKClIKC7KKClIKWgoKUlMg=")]
[assembly: go.GoPositionMap("text/template/funcs.go", "funcs.cs", "AC9OwgAlQKKClKiSgoKokoKClIKClICCpLyigrqkgKSkpKTakoKUgsaktqgACAKCgoKAgraAgqTqooKClJSClIKClKaClKSkqJKClKSkpKSClAACEPKCgpSigoKAgqSUgoKUpIKClICClMikxgACEgAIAoKClIKUgpSClKSktoKCgoKUpoKUgqaClKyygoKUlKSqsqrCgoKUgoKWgIKkgoKCgpSUgqaCopSCgpaCgIK2quKCgIKAgpTYgoKU6pKCqsKqwqiSABIygpSkpKSkpKSokoKUlKSqooKCgqaosoKClIKigoKClJS0tILYlKSkpKSkpIKUgpSClMiCpqikgqiSgoKClIKCgpSClJS0tLaUpKSkpKS2qKSCgpSopIKClKikgoKUAAoekoKCgpS0tLS0tLS0goKUqKSClIKCqsIADSaSgoKElJSEppS0tLS0tLS0goKC2IKClJSUlKikgpSCgqaClKSqwqrCAAIU8oKUgpSCgoKCppQ=")]
[assembly: go.GoPositionMap("text/template/helper.go", "helper.cs", "ACkw8oKUAAIYAAsCAAIcAA4CgqrSlJSCgoKUjuKClIKUlIKCpgACGAAJAgACFgAKAoKokoKClIKUruKu8oKmgoKCgoKUgpSUpsKCgqaCsoKC")]
[assembly: go.GoPositionMap("text/template/option.go", "option.cs", "ABVUABUCgoKUpoKCpoCClJSCpIKkgug=")]
[assembly: go.GoPositionMap("text/template/template.go", "template.cs", "ACRMkqaCqJIAAhTygtyokoKCgoKCAAMUAAoCgoKClIKCgoKCpoKUgoKClIKU2JIACBoACAKCgoKCgqaClNjSgqaCgoKClAAFEPKCgoIAAhIACgKCgoKCgtrigpSCggAFGgAMAoKCgoKCpoKAgras4oKUgKakgg==")]
// </GoSourcePositionMaps>

namespace go.text;

[GoPackage("template")]
public static partial class template_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct builtinFuncsOnceᴛ1 {}
    internal partial struct common {}
    internal partial struct kind {}
    internal partial struct missingKeyAction {}
    internal partial struct missingValType {}
    internal partial struct option {}
    internal partial struct state {}
    internal partial struct variable {}
    internal partial struct ΔwriteError {}
    public partial struct ExecError {}
    public partial struct FuncMap {}
    public partial struct Template {}
    // </TypeAccessibility>
}
