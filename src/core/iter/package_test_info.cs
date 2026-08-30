// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.iter_package;

// <ImportedTypeAliases>
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static global::go.iter_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
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
[assembly: go.GoPositionMap("iter/pull_test.go", "pull_test.cs", "ACUcgoKCgtyCgoKC3IKSgoKCgIKCtoKCgoKCgpSUgoKClIKCgpSUhIKCgsqCkoKCgoCCgraCgoKCgoKUlIKCgpSCgoKUlISCgoLOAA0UhIKCgoKClIKUlJSUAAgGgoKCgoLcgqKCgqbogoKCgoLcgqKCgqYACQiigpKClJSCguiCgoKC7qKCkoKUlIKC6IKCgoIADg6igoKQkqaAgraUgoKCgpSQkqaAgra4goK4goKCgtyigoKQkqaAgraUgoKCgpSQkqaAgra4goK4goKCgtyygoCCgpS2ggALBqKCgoKCgpSUgIKklIKCgoKUkpSmgIK2uIKCuIKCgoIACAyigoKCgoKUlICCpJSCgoKClJKUpoCCtriCgriCgoKC3IKEgrKCkpSCxNaigpSAgsiigpSAgg==", "15-21:1;25-31:1;36-72:1;38-43:1.1;78-114:1;80-85:1.1;163-170:1;164-168:1.1;185-192:1;186-190:1.1;197-202:1;210-215:1;222-227:1;235-240:1;246-257:1;248-248:1.1;258-273:2;264-264:2.1;277-279:1;283-289:1;293-304:1;295-295:1.1;305-320:2;311-311:2.1;324-326:1;330-336:1;340-347:1;353-366:1;356-359:1.1;367-384:2;373-375:2.1;388-390:1;394-400:1;404-417:1;407-410:1.1;418-435:2;424-426:2.1;439-441:1;445-451:1;458-465:1;460-462:1.1")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("iter_test")]
public static partial class iter_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
