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
global using oldtraceꓸSTWReason = go.@internal.trace.@internal.oldtrace_package.ΔSTWReason;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using bufio = go.bufio_package;
using oldtrace = go.@internal.trace.@internal.oldtrace_package;
// </ImportedTypeAliases>

using go;
using static go.@internal.trace_package;

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

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<bandUtilHeap, go.container.heap_package.Interface>(Pointer = true)]
[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bufio_package.Reader, readBatch_r>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.ByteReader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<utilHeap, go.container.heap_package.Interface>(Pointer = true)]
[assembly: GoImplement<utilHeap, sort_package.Interface>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<dataTable<stringID, @string>, ж<dataTable<stringID, @string>>>(Indirect = true)]
[assembly: GoImplicitConv<evTable, ж<evTable>>(Indirect = true)]
[assembly: GoImplicitConv<spilledBatch, ж<spilledBatch>>(Indirect = true)]
[assembly: GoImplicitConv<ΔTime, oldtrace.Timestamp>(Inverted = false, ValueType = "oldtrace.Timestamp")]
[assembly: GoImplicitConv<ΔTime, timestamp>(Inverted = true, ValueType = "ΔTime")]
// </ImplicitConversions>

namespace go.@internal;

[GoPackage("trace")]
public static partial class trace_package
{
    // A C# nested type declared with no access modifier is PRIVATE, and the `[GoType]`
    // declarations in this package's converted sources are deliberately bare so they read
    // like the Go original. Their real accessibility — public for a Go-exported name,
    // internal otherwise — is supplied by the partial that go2cs-gen's TypeGenerator emits,
    // and a source generator cannot see its own output: while the generators run, every one
    // of those types is still private, so a semantic query that reaches across package
    // classes resolves them as Inaccessible and silently drops whatever it was about to
    // build from them.

    // The declarations below close that gap. A C# partial type may carry its access modifier
    // on any ONE of its parts, so pinning it here fixes each type's accessibility IN SOURCE,
    // ahead of generation, while the `[GoType]` declaration itself stays Go-shaped — the
    // section declares `public partial interface Closer {}` for a `[GoType] partial interface
    // Closer`, and `internal partial struct dirEntry {}` for an unexported one.

    // <TypeAccessibility>
    internal partial interface readBatch_r {}
    internal partial struct accumulator {}
    internal partial struct bandUtil {}
    internal partial struct bandUtilHeap {}
    internal partial struct baseEvent {}
    internal partial struct batch {}
    internal partial struct batchCursor {}
    internal partial struct cpuSample {}
    internal partial struct dataTable<EI, E> {}
    internal partial struct edge {}
    internal partial struct evTable {}
    internal partial struct extraStringID {}
    internal partial struct frame {}
    internal partial struct frequency {}
    internal partial struct gState {}
    internal partial struct gcState {}
    internal partial struct generation {}
    internal partial struct goroutineSummary {}
    internal partial struct integrator {}
    internal partial struct mState {}
    internal partial struct mmuBand {}
    internal partial struct mmuSeries {}
    internal partial struct mud {}
    internal partial struct oldTraceConverter {}
    internal partial struct ordering {}
    internal partial struct pState {}
    internal partial struct queue<T> {}
    internal partial struct rangeP {}
    internal partial struct rangeState {}
    internal partial struct rangeType {}
    internal partial struct schedCtx {}
    internal partial struct seqCounter {}
    internal partial struct spilledBatch {}
    internal partial struct stack {}
    internal partial struct stackID {}
    internal partial struct stringID {}
    internal partial struct taskState {}
    internal partial struct timedEventArgs {}
    internal partial struct timestamp {}
    internal partial struct totalUtil {}
    internal partial struct userRegion {}
    internal partial struct utilHeap {}
    public partial struct EventKind {}
    public partial struct ExperimentalBatch {}
    public partial struct ExperimentalData {}
    public partial struct ExperimentalEvent {}
    public partial struct Frame {}
    public partial struct GoID {}
    public partial struct GoState {}
    public partial struct GoroutineExecStats {}
    public partial struct GoroutineSummary {}
    public partial struct MMUCurve {}
    public partial struct MutatorUtil {}
    public partial struct MutatorUtilizationV2_perP {}
    public partial struct MutatorUtilizationV2_procsCount {}
    public partial struct ProcID {}
    public partial struct ProcState {}
    public partial struct RangeAttribute {}
    public partial struct Reader {}
    public partial struct RelatedGoroutinesV2_unblockEdge {}
    public partial struct ResourceID {}
    public partial struct ResourceKind {}
    public partial struct StackFrame {}
    public partial struct Summarizer {}
    public partial struct Summary {}
    public partial struct TaskID {}
    public partial struct ThreadID {}
    public partial struct UserRegionSummary {}
    public partial struct UserTaskSummary {}
    public partial struct UtilFlags {}
    public partial struct UtilWindow {}
    public partial struct Value {}
    public partial struct ValueKind {}
    public partial struct ΔEvent {}
    public partial struct ΔLabel {}
    public partial struct ΔLog {}
    public partial struct ΔMetric {}
    public partial struct ΔRange {}
    public partial struct ΔRegion {}
    public partial struct ΔStack {}
    public partial struct ΔStateTransition {}
    public partial struct ΔTask {}
    public partial struct ΔTime {}
    // </TypeAccessibility>
}
