// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.debug.gosym_package;

// <ImportedTypeAliases>
global using elfꓸData = go.debug.elf_package.ΔData;
global using elfꓸSection = go.debug.elf_package.ΔSection;
global using execꓸError = go.os.exec_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.debug.gosym_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<go.compress.gzip_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
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
[assembly: go.GoPositionMap("debug/gosym/pclntab_test.go", "pclntab_test.cs", "AGM0gpSCpoKUgoKClIKCgoKCgoCCyIKCgoK8orjYgoKCppSCgpQACAaigoKUgoKClIKCgpaCgoKCluaihIKUqIKCuoKCgpSCgpSkpoKCpoKCpJSUgriihIKUlriCgoKCqIKWgIKUtoSCgqQACgyigoSCkoKCgqiCgoKCgpSCgoKCpLqCgoKCgoKCgpSCgoKClIKUgoKUgrakpgAJCIKEgoKUgoIABxwACQKCgpSCgoKUgoKClOiSgoKCgoKUgoKCgpSClIKUggAMEoKChJKCgqiCkoKCgoKCupKClqKCgoKCgoKUgpSClIK6koKCgoKUgpSC")]
[assembly: go.GoPositionMap("debug/gosym/symtab_test.go", "symtab_test.cs", "ABEYgoLogoKCgoKC5oKCgoKCggAKBoKCgoKCgoKCgoKCgoKCgoLmgoKCgoKC9oIADiai")]
// </GoSourcePositionMaps>

namespace go.debug;

[GoPackage("gosym")]
public static partial class gosym_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
