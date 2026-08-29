// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.go.build_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
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
global using scannerꓸError = go.go.scanner_package.ΔError;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
// </ImportedTypeAliases>

using go;
using static global::go.go.build_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<readNopCloser, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<readNopCloser, io_package.Reader>(Promoted = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
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
[assembly: global::go.GoPositionMap("go/build/build_test.go", "build_test.cs", "AEwmgoLmgpKCgoKCgpSCpoKCgoKUgqiCgoSCgoKCgoIACAaCgoKUgpaCgpSClIKC+IKCgpSClIL4goKAggAJCIKEgoKUyoKogIKmgIKmgIL4goKWgoKWgoKUggCVAYQDgrKSgoKCggAKFIKCgoKClIIABxCCAB08grKSkoKUlJKUgoIACQqCgpaCgpSCABYugoKCgpQACQqiAAkagoKClIIAEQ7CgpSEgoQABRaEgpKEgoSCgoKClJSCuIKmggAPDoKEhJKCgpSCgoKUgoK4ooSEkoKClIKCgoKCggAICoKEhJKCgpSCgoKWgoK4goSEkoKClJSCgpSClIKCAAsO0qiogoCCpICCpoKCkoKEgoCCooKUAAgM2IKCloLs2IKCloIACg7SgoKAgqSCgoSShIKCgrq6hICC/rKSgoKClIKCAAsSwpKCgoKClIKClIKCloKCgoKUgpSCguiCgoKUggAOCIKCgpaCgpSCgoKmlJQ=")]
[assembly: global::go.GoPositionMap("go/build/deps_test.go", "deps_test.cs", "AEymC5SUgoKCloKCloKCloKUgIKkpoKmloKCgpSEgoSCgoKClIKUgoKCgqaCAAkOgoKClIKCgpSCgoKUgoKClJSClMiCgpSCgoKUgpSClIKCgoK4gqiSgoKUqqKCloKCgpaCgsySgoKUgoKCgoKCpg==")]
[assembly: global::go.GoPositionMap("go/build/read_test.go", "read_test.cs", "AFnwAYKCgoKEgIKCpoKCgoKklJSCgpaCgsqCgoKCuIIASIwBuIKCgoKCpoKCggBTogGCgoLIgoKClIKCgoKUgoKC")]
[assembly: global::go.GoPositionMap("go/build/syslist_test.go", "syslist_test.cs", "ABAmgoKUpoKClAAZNIKCgg==")]
[assembly: global::go.GoPositionMap("go/build/vendor_test.go", "vendor_test.cs", "ACI4koKCgoKClIKCgoKCpoIABxKCgoKm9qIABhSCgg==")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("build")]
public static partial class build_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
