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
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
using parse = go.text.template.parse_package;
using template = go.text.template_package;
// </ImportedTypeAliases>

using go;
using static go.html.template_package;

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
[assembly: GoTypeAlias("Error", "ΔError")]
[assembly: GoTypeAlias("FuncMap", "go.text.template_package.FuncMap")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<ΔError, error>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<parse.ActionNode, ж<parse.ActionNode>>]
[assembly: GoImplicitConv<parse.BranchNode, ж<parse.BranchNode>>]
[assembly: GoImplicitConv<parse.ListNode, ж<parse.ListNode>>(Indirect = true)]
[assembly: GoImplicitConv<parse.ListNode, ж<parse.ListNode>>]
[assembly: GoImplicitConv<parse.TemplateNode, ж<parse.TemplateNode>>]
[assembly: GoImplicitConv<parse.TextNode, ж<parse.TextNode>>]
[assembly: GoImplicitConv<rangeContext, ж<rangeContext>>]
[assembly: GoImplicitConv<template.Template, ж<template.Template>>]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("html/template/attr.go", "attr.cs", "AI4BmAKiuKKCgqakgIK2ggAJFoaU")]
[assembly: go.GoPositionMap("html/template/attr_string.go", "attr_string.cs", "/oaigoKCgoLugoKU")]
[assembly: go.GoPositionMap("html/template/content.go", "content.cs", "ACXmAaKClICUpIKClAAFFrKClIKClKrCgpSkpKSkpKSkpoK4gpaClA==")]
[assembly: go.GoPositionMap("html/template/context.go", "context.cs", "ACJGgoKClKiSAAIWtIKUgoKUgpSClIKUgpQAI7ABopSkqJKUpKzolKQ=")]
[assembly: go.GoPositionMap("html/template/css.go", "css.cs", "AA8iooKUlIKClAAHEKjIAAIe4oKCyoKCgoKUgoK4poKClIKClLimgqaokqiSgoKClLS0tMaokoKmlKqylKSokpSkqLKCgqKUgoKUtLSClIKCgoKmgpSCAB5G8oKClAALGoKUqKLGgtiCgpQ=")]
[assembly: go.GoPositionMap("html/template/delim_string.go", "delim_string.cs", "/oaigoKC7oKClA==")]
[assembly: go.GoPositionMap("html/template/element_string.go", "element_string.cs", "/oaigoKCgu6CgpQ=")]
[assembly: go.GoPositionMap("html/template/error.go", "error.cs", "AEfOA4KUgqSkpKrC")]
[assembly: go.GoPositionMap("html/template/escape.go", "escape.cs", "ABQu8oKCgqSUlICCgoKklIKAgoKkqtSCgIK2gpQANm6SABYokpSkgoKkpIKCpKSkpKSklMyylJSUgoIABxCUgoCChP6ClKSkgqSUpMak2saEpLSkpKSkyIK0pIKUtri0pILc0pTKgoKCgIKAlNyCgriCgoKCpqb8goKCgoKAgraCyqYAHkCSqqKAgqQAFDrCgIKCgoK2qJIABTAAFAKWpqbUrLKClIKUgpSClIKWgoKUgpaCgpSCAAYQgIKAgsjesoKUgoKClIKCzIKCgoK4goKUgoKCpoKmuIKCgoKCpoKCgoKCpqiygpSCgoKmrsKSlIKUgoKUgpSClIKUgpSClIKmqLKCgpSqxpKCgJSkgqaCysqmgoKCgpSUqtSClICCyILKrNKSlJSmptyCABM6gqaCzLKygoKCgoKCgoKCuIKCgoLIAAES8pS2pJSUgpS2lIKUgoKClIKUloKClJSqooKCpqaqgoKUAAcQgILsyoKClJaWgpaUuKiSgIKkqJKAgqSokoCCpKqigriCgoCCtoKUgpSCuIKCgqi2goKUqqKClK7CqJKqwqiSqJKqwqrC")]
[assembly: go.GoPositionMap("html/template/html.go", "html.cs", "AA8esoKClIKUqLKCgpSosoKClKiygoKUAFu8AaKSkriCgoCCgpSCggAIDIKUgqaClIKqooLWgoKUgpSCgpSCgoKCgriUlIKUgoKUlJSUgqSUqsKCgpTclIKAlKSC+LYAAhIACAI=")]
[assembly: go.GoPositionMap("html/template/js.go", "js.cs", "ABZCAA0EgoKogLiEspSmlKaSlKioqKgAAhqoggAVApSCAAgMABc06oKWgoKUAAoKwoKCgpSm2NaClLiCABQqgoKCggAGEKaUgoKmgoKUpoKCgoKklIKCgpSUgoKClJSs0oKClKaigq7igoKUlAACENKCopSCgpS0tLS0tIKUgoKUgpSCAHfsAcKUpKSkpKSsAAkOgoKCAAEqpA==")]
[assembly: go.GoPositionMap("html/template/jsctx_string.go", "jsctx_string.cs", "/oaigoLugoKU")]
[assembly: go.GoPositionMap("html/template/state_string.go", "state_string.cs", "/oaigoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgu6CgpQ=")]
[assembly: go.GoPositionMap("html/template/template.go", "template.cs", "ADFc4oKCpIKClAAFKgAVAoKq4oKUgoKClNjSgoKCgoKUgILGlAAFFAAJAoCCpAACFPKCgpSsAAkCgoKCgoKUgpSClIKUgpTcsgACHAAPAoCCpoKCzIKCgoKCgpSClN4ACAKAgqaCgoKClNyCAAUWAAwCgoKClIKClIKC3IKCgoKClIIABxDYkoKC3IIAAhQACwKCgtiS3ICCgqSCqJIAAhTyggACEPKCquKCggAFENKClAACGAALAgACFgAKAqrSgIKmlJSCgoKUjuKClIKUlIKCpgACGAAJAgACGgAKAqiSgIKkgoKUgpSssq7iruKmgoKCgoKUgpSUpsKCgqaCsoKC")]
[assembly: go.GoPositionMap("html/template/transition.go", "transition.cs", "ADNmkoKCgoKklIKCgoKUlIKCgqaUAAsapIKClILKgoKUgoLMgoKUlKSkpLiClJSokoKCpJSopIKCtoKUlAALGpKCgqaClLS0gqiSgIKkAA4moqaClICCtqiSgoKUgoKUlIKUgpSUlKiSqJKCyJSokoKUgpSClLS0tJS0tLS0AAga8saCyJLKopS0gpyCwpSCtLSmgoKCgoKUlIKC/IKCgsiCtJaqooKUpKaSgoKClJSCgvy0urKkgsiSgsaWpszMkoKClJSkpKSokoKClKQAAhIACQaCgpTcqAAbOIKCgoKUlpKCgpS0tLTGgpSCpILogqSCpLqSgpSkqKTGgoKCgoKUgoKC3IKUgrqSrsKClKoACgoACRaSqJKokoKUgoKCgoKmgoKUlKiSgsjG")]
[assembly: go.GoPositionMap("html/template/url.go", "url.cs", "AAxEABgCgoKUgpT6ooCCgraqwgACEPKqwoKClIKClKqigu6CggABEOIABBC24siSlIKUgsaCgpSCqsKClKiigpa2goKCgoKCpoIABRCipoKmgoKClIKCgoKmgKaCgoKCpoKCgoK2gg==")]
[assembly: go.GoPositionMap("html/template/urlpart_string.go", "urlpart_string.cs", "/oaigoKC7oKClA==")]
// </GoSourcePositionMaps>

namespace go.html;

[GoPackage("template")]
public static partial class template_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct attr {}
    internal partial struct contentType {}
    internal partial struct context {}
    internal partial struct delim {}
    internal partial struct element {}
    internal partial struct escaper {}
    internal partial struct jsCtx {}
    internal partial struct nameSpace {}
    internal partial struct rangeContext {}
    internal partial struct state {}
    internal partial struct urlPart {}
    public partial struct CSS {}
    public partial struct ErrorCode {}
    public partial struct HTML {}
    public partial struct HTMLAttr {}
    public partial struct JS {}
    public partial struct JSStr {}
    public partial struct Srcset {}
    public partial struct Template {}
    public partial struct URL {}
    public partial struct ΔError {}
    // </TypeAccessibility>
}
