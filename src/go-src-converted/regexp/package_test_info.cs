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
global using syntaxꓸError = go.regexp.syntax_package.ΔError;
using syntax = go.regexp.syntax_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.regexp_package;
using static go.regexp_test_package;

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
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<inputBytes, input>(Pointer = true)]
[assembly: GoImplement<inputReader, input>(Pointer = true)]
[assembly: GoImplement<inputString, input>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.RuneReader>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<FindTest, ж<FindTest>>(Indirect = true)]
[assembly: GoImplicitConv<syntax.Prog, ж<syntax.Prog>>(Indirect = true)]
[assembly: GoImplicitConv<testing.T, ж<testing.T>>(Indirect = true)]
[assembly: GoImplicitConv<thread, ж<thread>>(Indirect = true)]
// </ImplicitConversions>

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
    internal partial struct benchDataᴛ1 {}
    internal partial struct benchSizesᴛ1 {}
    internal partial struct bitState {}
    internal partial struct compileBenchDataᴛ1 {}
    internal partial struct entry {}
    internal partial struct inputBytes {}
    internal partial struct inputReader {}
    internal partial struct inputString {}
    internal partial struct inputs {}
    internal partial struct job {}
    internal partial struct lazyFlag {}
    internal partial struct machine {}
    internal partial struct minInputLenTestsᴛ1 {}
    internal partial struct onePassMachine {}
    internal partial struct onePassProg {}
    internal partial struct onePassTests1ᴛ1 {}
    internal partial struct onePassTestsᴛ1 {}
    internal partial struct queue {}
    internal partial struct queueOnePass {}
    internal partial struct runeMergeTestsᴛ1 {}
    internal partial struct splitTestsᴛ1 {}
    internal partial struct stringError {}
    internal partial struct subexpCase {}
    internal partial struct subexpIndex {}
    internal partial struct thread {}
    public partial struct FindTest {}
    public partial struct MetaTest {}
    public partial struct Regexp {}
    public partial struct ReplaceFuncTest {}
    public partial struct ReplaceTest {}
    public partial struct TestParseAndCompile_type {}
    public partial struct onePassInst {}
    // </TypeAccessibility>
}

[GoPackage("regexp_test")]
public static partial class regexp_test_package
{
}
