// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.html.template_package;
global using static global::go.html.template_internal_test_package;

// <ImportedTypeAliases>
global using FuncMap = go.text.template_package.FuncMap;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
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
global using templateꓸError = go.html.template_package.ΔError;
global using templateꓸFuncMap = go.text.template_package.FuncMap;
using parse = go.text.template.parse_package;
// </ImportedTypeAliases>

using go;
using static global::go.html.template_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Error", "ΔError")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, fmt_package.Stringer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.archive.zip_package.ReadCloser, go.io.fs_package.FS>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, fmt_package.Stringer>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<parse.TextNode, ж<parse.TextNode>>]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("html/template/examplefiles_test.go", "examplefiles_test.cs", "ABoqooKClIKCgpSSgoKmAAgMAAgIAAgSlrqUgoIACg4ACQi6zJKCzLiUgoIADhYADAjulqikgoKmgoK4goKUgoIAEBYADAjulqjMgoKmgoK6goKmgoK6goKUgoI=")]
[assembly: go.GoPositionMap("html/template/template_test.go", "template_test.cs", "ABcgtIKCgpSCloKSgoKClICCAAkIgoKCgqaCgoKCgvaCgoKCgtaCgoKCgtaCgoKCgqaCgoKCgvaCgoK4guaCgoKCgvaCgoKCgIKkgIKkgIL4goKCAAkGtAALGKqSsqKCgIKkgoKAgqSCAAkMgoKCgoKClIKClAAHEILcgqaigoK4ooKCuKKCgoKUgg==")]
// </GoSourcePositionMaps>

namespace go.html;

[GoPackage("template_test")]
public static partial class template_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct templateFile {}
    internal partial struct testCase {}
    public partial struct TestStringsInScriptsWithJsonContentTypeAreCorrectlyEscaped_tests {}
    // </TypeAccessibility>
}
