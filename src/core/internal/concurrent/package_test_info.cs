// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.concurrent_package;

// <ImportedTypeAliases>
global using abiꓸArrayType = go.@internal.abi_package.ΔArrayType;
global using abiꓸChanDir = go.@internal.abi_package.ΔChanDir;
global using abiꓸFuncType = go.@internal.abi_package.ΔFuncType;
global using abiꓸInterfaceType = go.@internal.abi_package.ΔInterfaceType;
global using abiꓸKind = go.@internal.abi_package.ΔKind;
global using abiꓸMapType = go.@internal.abi_package.ΔMapType;
global using abiꓸName = go.@internal.abi_package.ΔName;
global using abiꓸStructType = go.@internal.abi_package.ΔStructType;
global using runtimeꓸError = go.runtime_package.ΔError;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.concurrent_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.@internal.concurrent_package.HashTrieMap<@string, nint>, ж<global::go.@internal.concurrent_package.HashTrieMap<@string, nint>>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("internal/concurrent/hashtriemap_bench_test.go", "hashtriemap_bench_test.cs", "ABASgqaCpoKmooKCgpSCooKCgoKC3IKmgqaigoSigoKCgoI=", "28-37:1;52-61:1")]
[assembly: go.GoPositionMap("internal/concurrent/hashtriemap_test.go", "hashtriemap_test.cs", "ADgkgqqCpoKGAAwIgoKEgraChIKSkpKkgpK2goSCgpKSkqSCkoKCgqSCyIKEgpKSkqSCgoKCgqTIgoSCkpKSpIKCgpSCgqTIgoSYgoSSgpSCtoKEgoKCgrKEhoKCkpKSpIKCkoKkgoL4lIKWgpKkgoKCgrKEgoKCpIL4uIKCpIKigoKClIKClIKUgoLKgoKChIKUgsqCgqaUgoSClILKgoKChIKUgsqCgoKEgpSCyoKCgoSCyoKCgoSCyoKCgpQABhKCgpSClIK4oqaygoKUgoKCgoKUlIKCgoKCgqaCgoKC", "19-21:1;25-33:1;29-31:1.1;37-43:1;44-57:2;58-79:3;80-99:4;100-121:5;122-128:6;125-127:6.1;129-139:7;132-135:7.1;140-173:8;147-170:8.1;150-152:8.1.1;174-200:9;186-197:9.1;208-220:1;230-239:1;249-258:1;263-272:1;277-286:1;291-297:1;302-308:1")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("concurrent")]
public static partial class concurrent_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
    // Go runs every `init` in the package under test - the production files' included -
    // before the first test. The production package is a REFERENCED assembly here, whose
    // module constructor .NET would not run until something in it is touched, so that
    // initialization is forced before anything else in this test module runs.
    [GoInit] internal static void initᴛᴛproduction() {
        builtin.initPackage(typeof(global::go.@internal.concurrent_package));
    }
}
