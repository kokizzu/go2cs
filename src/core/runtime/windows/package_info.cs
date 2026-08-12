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
global using abiꓸArrayType = go.@internal.abi_package.ΔArrayType;
global using abiꓸChanDir = go.@internal.abi_package.ΔChanDir;
global using abiꓸFuncType = go.@internal.abi_package.ΔFuncType;
global using abiꓸInterfaceType = go.@internal.abi_package.ΔInterfaceType;
global using abiꓸKind = go.@internal.abi_package.ΔKind;
global using abiꓸMapType = go.@internal.abi_package.ΔMapType;
global using abiꓸName = go.@internal.abi_package.ΔName;
global using abiꓸStructType = go.@internal.abi_package.ΔStructType;
using abi = go.@internal.abi_package;
// </ImportedTypeAliases>

using go;
using static go.runtime_package;

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
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<boundsError, ΔError>]
[assembly: GoImplement<errorAddressString, ΔError>]
[assembly: GoImplement<errorString, ΔError>]
[assembly: GoImplement<plainError, ΔError>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<_type, ж<_type>>(Indirect = true)]
[assembly: GoImplicitConv<abi.RegArgs, ж<abi.RegArgs>>(Indirect = true)]
[assembly: GoImplicitConv<abi.Type, ж<abi.Type>>(Indirect = true)]
[assembly: GoImplicitConv<abiꓸMapType, ж<maptype>>(Indirect = true)]
[assembly: GoImplicitConv<adjustinfo, ж<adjustinfo>>(Indirect = true)]
[assembly: GoImplicitConv<bmap, ж<bmap>>(Indirect = true)]
[assembly: GoImplicitConv<childInfo, ж<childInfo>>(Indirect = true)]
[assembly: GoImplicitConv<context, ж<context>>(Indirect = true)]
[assembly: GoImplicitConv<funcval, ж<funcval>>(Indirect = true)]
[assembly: GoImplicitConv<g, ж<g>>(Indirect = true)]
[assembly: GoImplicitConv<g, ж<g>>]
[assembly: GoImplicitConv<gQueue, ж<gQueue>>(Indirect = true)]
[assembly: GoImplicitConv<gcWork, ж<gcWork>>(Indirect = true)]
[assembly: GoImplicitConv<hiter, ж<hiter>>(Indirect = true)]
[assembly: GoImplicitConv<hmap, ж<hmap>>(Indirect = true)]
[assembly: GoImplicitConv<itab, ж<itab>>(Indirect = true)]
[assembly: GoImplicitConv<lfstack, Δhex>(Inverted = true, ValueType = "uint64")]
[assembly: GoImplicitConv<limiterEventStamp, limiterEventType>(Inverted = true, ValueType = "uint64")]
[assembly: GoImplicitConv<m, ж<m>>(Indirect = true)]
[assembly: GoImplicitConv<metricValue, ж<metricValue>>(Indirect = true)]
[assembly: GoImplicitConv<mspan, ж<mspan>>(Indirect = true)]
[assembly: GoImplicitConv<nameOff, Δhex>(Inverted = true, ValueType = "int32")]
[assembly: GoImplicitConv<pollDesc, ж<pollDesc>>(Indirect = true)]
[assembly: GoImplicitConv<ptrtype, ж<ptrtype>>(Indirect = true)]
[assembly: GoImplicitConv<spanClass, traceArg>(Inverted = true, ValueType = "uint8")]
[assembly: GoImplicitConv<stackScanState, ж<stackScanState>>(Indirect = true)]
[assembly: GoImplicitConv<stkframe, ж<stkframe>>]
[assembly: GoImplicitConv<sudog, ж<sudog>>(Indirect = true)]
[assembly: GoImplicitConv<sweepClass, spanClass>(Inverted = true, ValueType = "uint32")]
[assembly: GoImplicitConv<textOff, Δhex>(Inverted = true, ValueType = "int32")]
[assembly: GoImplicitConv<traceGoStatus, traceArg>(Inverted = true, ValueType = "uint8")]
[assembly: GoImplicitConv<traceProcStatus, traceArg>(Inverted = true, ValueType = "uint8")]
[assembly: GoImplicitConv<typeOff, Δhex>(Inverted = true, ValueType = "int32")]
[assembly: GoImplicitConv<Δp, ж<Δp>>(Indirect = true)]
[assembly: GoImplicitConv<Δsliceᴛ, ж<Δsliceᴛ>>(Indirect = true)]
// </ImplicitConversions>

namespace go;

[GoPackage("runtime")]
public static partial class runtime_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface floaty<ΔT> {}
    internal partial interface ifaceHash_i {}
    internal partial interface stringer {}
    internal partial struct _DISPATCHER_CONTEXT {}
    [GoValueClone("csdVersion")] internal partial struct _OSVERSIONINFOW {}
    internal partial struct _typePair {}
    internal partial struct abiDesc {}
    internal partial struct abiPart {}
    internal partial struct abiPartKind {}
    internal partial struct activeSweep {}
    internal partial struct addrRange {}
    internal partial struct addrRanges {}
    internal partial struct adjustinfo {}
    internal partial struct arenaHint {}
    internal partial struct arenaIdx {}
    internal partial struct argset {}
    internal partial struct atomicHeadTailIndex {}
    internal partial struct atomicMSpanPointer {}
    internal partial struct atomicOffAddr {}
    internal partial struct atomicScavChunkData {}
    internal partial struct atomicSpanSetSpinePointer {}
    internal partial struct bitvector {}
    internal partial struct blockRecord {}
    [GoValueClone("tophash")] internal partial struct bmap {}
    internal partial struct boundsError {}
    internal partial struct boundsErrorCode {}
    internal partial struct bucket {}
    internal partial struct bucketType {}
    internal partial struct buckhashArray {}
    internal partial struct callbackArgs {}
    [GoValueClone("ctxt")] internal partial struct cbsᴛ1 {}
    internal partial struct cgoContextArg {}
    internal partial struct cgoSymbolizerArg {}
    internal partial struct cgoTracebackArg {}
    internal partial struct cgothreadstart {}
    [GoLocalName("x1t")] internal partial struct check_x1t {}
    [GoLocalName("y1t")] internal partial struct check_y1t {}
    [GoValueClone("b")] internal partial struct checkmarksMap {}
    internal partial struct childInfo {}
    internal partial struct chunkIdx {}
    [GoValueClone("stats")] internal partial struct consistentHeapStats {}
    [GoValueClone("anon0", "vectorregister")] internal partial struct context {}
    internal partial struct coro {}
    [GoValueClone("extra")] internal partial struct cpuProfile {}
    internal partial struct cpuStats {}
    internal partial struct cpuStatsAggregate {}
    internal partial struct dbgVar {}
    internal partial struct debugCallWrapArgs {}
    [GoValueClone("b")] internal partial struct debugLogBuf {}
    internal partial struct debugLogReader {}
    [GoValueClone("data", "buf")] internal partial struct debugLogWriter {}
    internal partial struct debugPtrmaskᴛ1 {}
    internal partial struct debugᴛ1 {}
    internal partial struct dlogPerM {}
    [GoValueClone("w")] internal partial struct dlogger {}
    internal partial struct errorAddressString {}
    internal partial struct errorString {}
    internal partial struct evacDst {}
    internal partial struct exceptionpointers {}
    [GoValueClone("exceptioninformation")] internal partial struct exceptionrecord {}
    internal partial struct find_firstFree {}
    [GoValueClone("subbuckets")] internal partial struct findfuncbucket {}
    internal partial struct fixalloc {}
    internal partial struct functab {}
    internal partial struct gList {}
    internal partial struct gQueue {}
    internal partial struct gTraceState {}
    internal partial struct gcBgMarkWorkerNode {}
    internal partial struct gcBits {}
    [GoValueClone("bits")] internal partial struct gcBitsArena {}
    internal partial struct gcBitsArenasᴛ1 {}
    internal partial struct gcBitsHeader {}
    internal partial struct gcCPULimiterState {}
    internal partial struct gcCPULimiterState_bucket {}
    [GoValueClone("lastConsMark")] internal partial struct gcControllerState {}
    internal partial struct gcDrainFlags {}
    internal partial struct gcMarkWorkerMode {}
    internal partial struct gcMode {}
    internal partial struct gcStatsAggregate {}
    internal partial struct gcTrigger {}
    internal partial struct gcTriggerKind {}
    internal partial struct gcWork {}
    internal partial struct gclink {}
    internal partial struct gclinkptr {}
    internal partial struct globalAllocᴛ1 {}
    [GoValueClone("seed", "state")] internal partial struct globalRandᴛ1 {}
    internal partial struct godebugInc {}
    internal partial struct goroutineProfileState {}
    internal partial struct goroutineProfileStateHolder {}
    internal partial struct goroutineProfileᴛ1 {}
    internal partial struct gsignalStack {}
    internal partial struct headTailIndex {}
    [GoValueClone("spans", "pageInUse", "pageMarks", "pageSpecials")] internal partial struct heapArena {}
    internal partial struct heapStatsAggregate {}
    [GoValueClone("smallAllocCount", "smallFreeCount")] internal partial struct heapStatsDelta {}
    internal partial struct hiter {}
    internal partial struct hmap {}
    internal partial struct initTask {}
    internal partial struct inlineFrame {}
    internal partial struct inlineUnwinder {}
    internal partial struct inlinedCall {}
    [GoValueClone("entries")] internal partial struct itabTableType {}
    internal partial struct lfstack {}
    internal partial struct limiterEvent {}
    internal partial struct limiterEventStamp {}
    internal partial struct limiterEventType {}
    internal partial struct linearAlloc {}
    internal partial struct liveUserArenaChunk {}
    internal partial struct lockRank {}
    internal partial struct lockRankStruct {}
    internal partial struct lockTimer {}
    internal partial struct m128a {}
    internal partial struct mLockProfile {}
    internal partial struct mOS {}
    internal partial struct mProfCycleHolder {}
    internal partial struct mSpanList {}
    internal partial struct mSpanState {}
    internal partial struct mSpanStateBox {}
    [GoValueClone("buf")] internal partial struct mTraceState {}
    internal partial struct mapextra {}
    internal partial struct markBits {}
    [GoValueClone("alloc", "stackcache")] internal partial struct mcache {}
    [GoValueClone("partial", "full")] internal partial struct mcentral {}
    [GoValueClone("future")] internal partial struct memRecord {}
    internal partial struct memRecordCycle {}
    internal partial struct memoryBasicInformation {}
    [GoValueClone("deps")] internal partial struct metricData {}
    internal partial struct metricFloat64Histogram {}
    internal partial struct metricKind {}
    internal partial struct metricName {}
    internal partial struct metricSample {}
    internal partial struct metricValue {}
    [GoValueClone("pages", "arenas", "central")] internal partial struct mheap {}
    [GoValueClone("mcentral", "pad")] internal partial struct mheap_central {}
    internal partial struct mheap_curArena {}
    internal partial struct mheap_userArena {}
    internal partial struct mlink {}
    internal partial struct moduledata {}
    internal partial struct modulehash {}
    [GoLocalName("_DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS")] internal partial struct monitorSuspendResume__DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS {}
    internal partial struct mspan {}
    [GoValueClone("heapStats", "pause_ns", "pause_end")] internal partial struct mstats {}
    internal partial struct neverCallThisFunction {}
    internal partial struct newmHandoffᴛ1 {}
    internal partial struct notInHeap {}
    internal partial struct notInHeapSlice {}
    internal partial struct notifyList {}
    internal partial struct offAddr {}
    [GoValueClone("anon0")] internal partial struct overlapped {}
    internal partial struct overlappedEntry {}
    internal partial struct pMask {}
    internal partial struct pTraceState {}
    [GoValueClone("summary", "chunks")] internal partial struct pageAlloc {}
    internal partial struct pageAlloc_scav {}
    internal partial struct pageBits {}
    internal partial struct pageCache {}
    internal partial struct pallocBits {}
    [GoValueClone("scavenged")] internal partial struct pallocData {}
    internal partial struct pallocSum {}
    internal partial struct pcHeader {}
    [GoValueClone("entries")] internal partial struct pcvalueCache {}
    internal partial struct pcvalueCacheEnt {}
    internal partial struct persistentAlloc {}
    internal partial struct piController {}
    internal partial struct pinState {}
    [GoValueClone("refStore")] internal partial struct pinner {}
    internal partial struct pinnerBits {}
    internal partial struct plainError {}
    internal partial struct pollCache {}
    internal partial struct pollDesc {}
    internal partial struct pollInfo {}
    internal partial struct pollOperation {}
    internal partial struct printDebugLog_best {}
    [GoLocalName("readState")] internal partial struct printDebugLog_readState {}
    internal partial struct profAtomic {}
    internal partial struct profBuf {}
    internal partial struct profBufReadMode {}
    internal partial struct profIndex {}
    internal partial struct profᴛ1 {}
    internal partial struct ptabEntry {}
    internal partial struct randomEnum {}
    internal partial struct randomOrder {}
    internal partial struct readmemstats_m_bySize {}
    internal partial struct reflectMethodValue {}
    internal partial struct reflectOffsᴛ1 {}
    internal partial struct runtimeSelect {}
    internal partial struct rwmutex {}
    internal partial struct scase {}
    internal partial struct scavChunkData {}
    internal partial struct scavChunkFlags {}
    internal partial struct scavengeIndex {}
    internal partial struct scavengerState {}
    internal partial struct selectDir {}
    internal partial struct semTable {}
    [GoValueClone("pad")] internal partial struct semTableᴛ1 {}
    internal partial struct semaProfileFlags {}
    internal partial struct semaRoot {}
    internal partial struct sigset {}
    [GoValueClone("mask", "wanted", "ignored", "recv")] internal partial struct sigᴛ1 {}
    internal partial struct sliceInterfacePtr {}
    internal partial struct spanAllocType {}
    internal partial struct spanClass {}
    internal partial struct spanSet {}
    [GoValueClone("spans")] internal partial struct spanSetBlock {}
    internal partial struct spanSetBlockAlloc {}
    internal partial struct spanSetSpinePointer {}
    internal partial struct special {}
    internal partial struct specialPinCounter {}
    internal partial struct specialReachable {}
    internal partial struct specialWeakHandle {}
    internal partial struct specialfinalizer {}
    internal partial struct specialprofile {}
    internal partial struct specialsIter {}
    [GoValueClone("free")] internal partial struct stackLargeᴛ1 {}
    internal partial struct stackObject {}
    [GoValueClone("obj")] internal partial struct stackObjectBuf {}
    internal partial struct stackObjectBufHdr {}
    internal partial struct stackObjectRecord {}
    internal partial struct stackScanState {}
    [GoValueClone("obj")] internal partial struct stackWorkBuf {}
    internal partial struct stackWorkBufHdr {}
    internal partial struct stackfreelist {}
    [GoValueClone("bytedata")] internal partial struct stackmap {}
    internal partial struct stackpoolItem {}
    internal partial struct stackpoolᴛ1 {}
    [GoValueClone("ensured")] internal partial struct statAggregate {}
    internal partial struct statDep {}
    internal partial struct statDepSet {}
    internal partial struct stdFunction {}
    internal partial struct stkframe {}
    internal partial struct stringInterfacePtr {}
    internal partial struct stringStruct {}
    internal partial struct stringStructDWARF {}
    internal partial struct stwReason {}
    internal partial struct suspendGState {}
    internal partial struct sweepClass {}
    internal partial struct sweepLocked {}
    internal partial struct sweepLocker {}
    internal partial struct sweepdata {}
    internal partial struct sysMemStat {}
    internal partial struct sysStatsAggregate {}
    internal partial struct sysmontick {}
    [GoValueClone("anon0")] internal partial struct systeminfo {}
    internal partial struct taggedPointer {}
    internal partial struct textsect {}
    internal partial struct throwType {}
    internal partial struct ticksType {}
    [GoValueClone("counts")] internal partial struct timeHistogram {}
    internal partial struct timeTimer {}
    internal partial struct timer {}
    internal partial struct timerWhen {}
    internal partial struct timers {}
    internal partial struct tmpBuf {}
    [GoLocalName("untracedG")] internal partial struct traceAdvance_untracedG {}
    internal partial struct traceAdvancerState {}
    internal partial struct traceArg {}
    internal partial struct traceBlockReason {}
    [GoValueClone("arr")] internal partial struct traceBuf {}
    internal partial struct traceBufHeader {}
    internal partial struct traceBufQueue {}
    internal partial struct traceEv {}
    internal partial struct traceEventWriter {}
    internal partial struct traceExpWriter {}
    internal partial struct traceExperiment {}
    internal partial struct traceFrame {}
    internal partial struct traceGoStatus {}
    internal partial struct traceGoStopReason {}
    internal partial struct traceLocker {}
    internal partial struct traceMap {}
    [GoValueClone("children")] internal partial struct traceMapNode {}
    internal partial struct traceProcStatus {}
    internal partial struct traceRegionAlloc {}
    [GoValueClone("data")] internal partial struct traceRegionAllocBlock {}
    internal partial struct traceRegionAllocBlockHeader {}
    [GoValueClone("statusTraced", "seq")] internal partial struct traceSchedResourceState {}
    internal partial struct traceStackTable {}
    internal partial struct traceStringTable {}
    internal partial struct traceTime {}
    internal partial struct traceTypeTable {}
    internal partial struct traceWriter {}
    internal partial struct tracestat {}
    [GoValueClone("t")] internal partial struct typeCacheBucket {}
    internal partial struct typePointers {}
    internal partial struct uint16InterfacePtr {}
    internal partial struct uint32InterfacePtr {}
    internal partial struct uint64InterfacePtr {}
    internal partial struct unwindFlags {}
    internal partial struct unwinder {}
    internal partial struct userArena {}
    internal partial struct userArenaStateᴛ1 {}
    internal partial struct waitq {}
    internal partial struct wakeableSleep {}
    [GoValueClone("buf")] internal partial struct wbBuf {}
    internal partial struct winCallback {}
    internal partial struct winCallbackKey {}
    internal partial struct winlibcall {}
    internal partial struct workType {}
    internal partial struct workType_assistQueue {}
    internal partial struct workType_sweepWaiters {}
    internal partial struct workType_wbufSpans {}
    [GoValueClone("obj")] internal partial struct workbuf {}
    internal partial struct workbufhdr {}
    internal partial struct worldStop {}
    [GoValueClone("pad")] internal partial struct writeBarrierᴛ1 {}
    public partial interface ΔError {}
    public partial struct BlockProfileRecord {}
    public partial struct Frame {}
    [GoValueClone("frameStore")] public partial struct Frames {}
    public partial struct Func {}
    [GoValueClone("Stack0")] public partial struct MemProfileRecord {}
    [GoValueClone("PauseNs", "PauseEnd", "BySize")] public partial struct MemStats {}
    public partial struct MemStats_BySize {}
    public partial struct PanicNilError {}
    public partial struct Pinner {}
    [GoValueClone("Stack0")] public partial struct StackRecord {}
    public partial struct TypeAssertionError {}
    public partial struct ΔcgoCallers {}
    public partial struct ΔfuncInfo {}
    public partial struct Δhchan {}
    public partial struct Δhex {}
    public partial struct Δrtype {}
    public partial struct Δscavengeᴛ1 {}
    public partial struct Δsliceᴛ {}
    public partial struct ΔsrcFunc {}
    [GoValueClone("full", "doneSema", "stackTab", "stringTab", "typeTab", "cpuLogRead", "cpuLogWrite", "cpuBuf", "markWorkerLabels", "goStopReasons", "goBlockReasons")] public partial struct Δtraceᴛ1 {}
    public partial struct ΔwriteUserArenaHeapBits {}
    // </TypeAccessibility>
}
