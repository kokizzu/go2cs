// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.trace_package;
global using static global::go.@internal.trace_internal_test_package;

// <ImportedTypeAliases>
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using oldtraceꓸSTWReason = go.@internal.trace.@internal.oldtrace_package.ΔSTWReason;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using traceꓸEvent = go.@internal.trace_package.ΔEvent;
global using traceꓸLabel = go.@internal.trace_package.ΔLabel;
global using traceꓸLog = go.@internal.trace_package.ΔLog;
global using traceꓸMetric = go.@internal.trace_package.ΔMetric;
global using traceꓸRange = go.@internal.trace_package.ΔRange;
global using traceꓸRegion = go.@internal.trace_package.ΔRegion;
global using traceꓸStack = go.@internal.trace_package.ΔStack;
global using traceꓸStateTransition = go.@internal.trace_package.ΔStateTransition;
global using traceꓸTask = go.@internal.trace_package.ΔTask;
global using traceꓸTime = go.@internal.trace_package.ΔTime;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.trace_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Event", "ΔEvent")]
[assembly: GoTypeAlias("Label", "ΔLabel")]
[assembly: GoTypeAlias("Log", "ΔLog")]
[assembly: GoTypeAlias("Metric", "ΔMetric")]
[assembly: GoTypeAlias("Range", "ΔRange")]
[assembly: GoTypeAlias("Region", "ΔRegion")]
[assembly: GoTypeAlias("Stack", "ΔStack")]
[assembly: GoTypeAlias("StateTransition", "ΔStateTransition")]
[assembly: GoTypeAlias("Task", "ΔTask")]
[assembly: GoTypeAlias("Time", "ΔTime")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
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
[assembly: go.GoPositionMap("internal/trace/gc_test.go", "gc_test.cs", "ADUkooKUgoIACAaC3gAHEIQACRyAgqTKgpSCgoIACg7GlJSiqIKCgoIABhCAkoKCgoKCgoKCgvqSgoKClIKCgpSCgoKUgpSmuKKAgqaogoKCgoKClIKUlJKCgpS6poKClIKCgpSUlII=")]
[assembly: go.GoPositionMap("internal/trace/oldtrace_test.go", "oldtrace_test.cs", "ACMgooKClIKCgoKClKKCgpSUgoKWgoKCgoKClJSAgqaCggAKFpSAgsaCtoKCAAoOgg==")]
[assembly: go.GoPositionMap("internal/trace/reader_test.go", "reader_test.cs", "AEM4goKClIKCgoKUgoKClMqItIKCgpSCgoKWgqaUpKSkpIKkpKQADBKigoKAgqSUgoKCgpSCgIKklIKUgIK2gILIgoSCgpSCgoKUgoKClIKUgIK25qKEgoKUgoKClJKAgqQ=")]
[assembly: go.GoPositionMap("internal/trace/summary_test.go", "summary_test.cs", "ABgcgoKsgoSCgoKAgqSAgraClIKUggALCIKCAAwggoKCgoKUgqaCAA4IgoIAABCCgpQAR5ABgoKClIKogoKktqiCgpSCgIKUtoKCuoKUgoLMgpSCgoKClILMlIK4goKCptaigoKUhJSClIKCuIKCgqaWgoKmgpKClIKUlOailIK2gpSCgpSCgqaCgpSClICC2KaUgraClIKClIKCpoKClIKUgILYAAsIooKUgpSClIKUgpSCgqaCgsqCgoKCqIKCmJKCgoKUgpSogoLsgoCClLaCgg==")]
[assembly: go.GoPositionMap("internal/trace/trace_test.go", "trace_test.cs", "ADYugoIACiCCgpSCgoKUgpSCgpSClIKkgqSCpIKCgriCggAIDILWgoSUpOaipoKCgoKCgoKClIKUgIKkgt6CgoKCgpSCgoKSgpSClIKUgoKCgoKCgpSUgqaCpoKkAAYQgoKCgqiCgoKClIK4goKCqIKCgpQACAqijgAMDIKCgoKUgoKClILKgoKUggAJFIKUgoKUgoKClIKUgpS2gpS2gpS2gpS2lIL6gtaCABAGooIAARYAT54B7siUggASJoKCgpKCgpSAgoKkgpSUgoKUgoKClIKUgoKUgpSCgpSCpKSCpIKkgoKCgriCggAIDIKUpNaClKTWgtaClIKk1oIAEAaCloKEgoKUgoKUgpSClJSUlIzSgpSAgoKUpJaWgqi4goLulMimkpSSgpSUkoKU")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("trace_test")]
public static partial class trace_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct TestMMU_type {}
    [GoLocalName("region")] internal partial struct TestSummarizeGoroutinesRegionsTrace_region {}
    [GoLocalName("task")] internal partial struct TestSummarizeTasksTrace_task {}
    [GoLocalName("evDesc")] internal partial struct TestTraceAnnotations_evDesc {}
    [GoLocalName("evDesc")] internal partial struct TestTraceStacks_evDesc {}
    [GoLocalName("frame")] internal partial struct TestTraceStacks_frame {}
    // </TypeAccessibility>
}
