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
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

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
[assembly: GoImplicitConv<funcval, ж<funcval>>(Indirect = true)]
[assembly: GoImplicitConv<g, ж<g>>(Indirect = true)]
[assembly: GoImplicitConv<g, ж<g>>]
[assembly: GoImplicitConv<gQueue, ж<gQueue>>(Indirect = true)]
[assembly: GoImplicitConv<gcWork, ж<gcWork>>(Indirect = true)]
[assembly: GoImplicitConv<gsignalStack, ж<gsignalStack>>(Indirect = true)]
[assembly: GoImplicitConv<hiter, ж<hiter>>(Indirect = true)]
[assembly: GoImplicitConv<hmap, ж<hmap>>(Indirect = true)]
[assembly: GoImplicitConv<itab, ж<itab>>(Indirect = true)]
[assembly: GoImplicitConv<lfstack, Δhex>(Inverted = true, ValueType = "lfstack")]
[assembly: GoImplicitConv<limiterEventStamp, limiterEventType>(Inverted = true, ValueType = "limiterEventStamp")]
[assembly: GoImplicitConv<m, ж<m>>(Indirect = true)]
[assembly: GoImplicitConv<metricValue, ж<metricValue>>(Indirect = true)]
[assembly: GoImplicitConv<mspan, ж<mspan>>(Indirect = true)]
[assembly: GoImplicitConv<nameOff, Δhex>(Inverted = true, ValueType = "nameOff")]
[assembly: GoImplicitConv<pollDesc, ж<pollDesc>>(Indirect = true)]
[assembly: GoImplicitConv<pthreadmutex, ж<pthreadmutex>>(Indirect = true)]
[assembly: GoImplicitConv<ptrtype, ж<ptrtype>>(Indirect = true)]
[assembly: GoImplicitConv<sigctxt, ж<sigctxt>>(Indirect = true)]
[assembly: GoImplicitConv<spanClass, traceArg>(Inverted = true, ValueType = "spanClass")]
[assembly: GoImplicitConv<stackScanState, ж<stackScanState>>(Indirect = true)]
[assembly: GoImplicitConv<stkframe, ж<stkframe>>]
[assembly: GoImplicitConv<sudog, ж<sudog>>(Indirect = true)]
[assembly: GoImplicitConv<sweepClass, spanClass>(Inverted = true, ValueType = "sweepClass")]
[assembly: GoImplicitConv<textOff, Δhex>(Inverted = true, ValueType = "textOff")]
[assembly: GoImplicitConv<timespec, ж<timespec>>(Indirect = true)]
[assembly: GoImplicitConv<traceGoStatus, traceArg>(Inverted = true, ValueType = "traceGoStatus")]
[assembly: GoImplicitConv<traceProcStatus, traceArg>(Inverted = true, ValueType = "traceProcStatus")]
[assembly: GoImplicitConv<typeOff, Δhex>(Inverted = true, ValueType = "typeOff")]
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
    internal partial class machVMRegionInfo {}
    internal partial interface floaty<ΔT> {}
    internal partial interface ifaceHash_i {}
    internal partial interface stringer {}
    internal partial struct _typePair {}
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
    internal partial struct bmap {}
    internal partial struct boundsError {}
    internal partial struct boundsErrorCode {}
    internal partial struct bucket {}
    internal partial struct bucketType {}
    internal partial struct buckhashArray {}
    internal partial struct cgoContextArg {}
    internal partial struct cgoSymbolizerArg {}
    internal partial struct cgoTracebackArg {}
    internal partial struct cgothreadstart {}
    internal partial struct check_x1t {}
    internal partial struct check_y1t {}
    internal partial struct checkmarksMap {}
    internal partial struct childInfo {}
    internal partial struct chunkIdx {}
    internal partial struct consistentHeapStats {}
    internal partial struct coro {}
    internal partial struct cpuProfile {}
    internal partial struct cpuStats {}
    internal partial struct cpuStatsAggregate {}
    internal partial struct crypto_x509_syscall_args {}
    internal partial struct dbgVar {}
    internal partial struct debugCallWrapArgs {}
    internal partial struct debugLogBuf {}
    internal partial struct debugLogReader {}
    internal partial struct debugLogWriter {}
    internal partial struct debugPtrmaskᴛ1 {}
    internal partial struct debugᴛ1 {}
    internal partial struct dlogPerM {}
    internal partial struct dlogger {}
    internal partial struct errorAddressString {}
    internal partial struct errorString {}
    internal partial struct evacDst {}
    internal partial struct exceptionstate32 {}
    internal partial struct exceptionstate64 {}
    internal partial struct fcntl_args {}
    internal partial struct find_firstFree {}
    internal partial struct findfuncbucket {}
    internal partial struct fixalloc {}
    internal partial struct floatstate32 {}
    internal partial struct floatstate64 {}
    internal partial struct fpcontrol {}
    internal partial struct fpstatus {}
    internal partial struct functab {}
    internal partial struct gList {}
    internal partial struct gQueue {}
    internal partial struct gTraceState {}
    internal partial struct gcBgMarkWorkerNode {}
    internal partial struct gcBits {}
    internal partial struct gcBitsArena {}
    internal partial struct gcBitsArenasᴛ1 {}
    internal partial struct gcBitsHeader {}
    internal partial struct gcCPULimiterState {}
    internal partial struct gcCPULimiterState_bucket {}
    internal partial struct gcControllerState {}
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
    internal partial struct globalRandᴛ1 {}
    internal partial struct godebugInc {}
    internal partial struct goroutineProfileState {}
    internal partial struct goroutineProfileStateHolder {}
    internal partial struct goroutineProfileᴛ1 {}
    internal partial struct gsignalStack {}
    internal partial struct headTailIndex {}
    internal partial struct heapArena {}
    internal partial struct heapStatsAggregate {}
    internal partial struct heapStatsDelta {}
    internal partial struct hiter {}
    internal partial struct hmap {}
    internal partial struct initTask {}
    internal partial struct inlineFrame {}
    internal partial struct inlineUnwinder {}
    internal partial struct inlinedCall {}
    internal partial struct itabTableType {}
    internal partial struct itimerval {}
    internal partial struct keventt {}
    internal partial struct lfstack {}
    internal partial struct limiterEvent {}
    internal partial struct limiterEventStamp {}
    internal partial struct limiterEventType {}
    internal partial struct linearAlloc {}
    internal partial struct liveUserArenaChunk {}
    internal partial struct lockRank {}
    internal partial struct lockRankStruct {}
    internal partial struct lockTimer {}
    internal partial struct mLockProfile {}
    internal partial struct mOS {}
    internal partial struct mProfCycleHolder {}
    internal partial struct mSpanList {}
    internal partial struct mSpanState {}
    internal partial struct mSpanStateBox {}
    internal partial struct mTraceState {}
    internal partial struct machMsgTypeNumber {}
    internal partial struct machPort {}
    internal partial struct machTimebaseInfo {}
    internal partial struct machVMAddress {}
    internal partial struct machVMMapRead {}
    internal partial struct machVMRegionFlavour {}
    internal partial struct machVMSize {}
    internal partial struct mach_vm_region_args {}
    internal partial struct mapextra {}
    internal partial struct markBits {}
    internal partial struct mcache {}
    internal partial struct mcentral {}
    internal partial struct mcontext32 {}
    internal partial struct mcontext64 {}
    internal partial struct memRecord {}
    internal partial struct memRecordCycle {}
    internal partial struct metricData {}
    internal partial struct metricFloat64Histogram {}
    internal partial struct metricKind {}
    internal partial struct metricName {}
    internal partial struct metricSample {}
    internal partial struct metricValue {}
    internal partial struct mheap {}
    internal partial struct mheap_central {}
    internal partial struct mheap_curArena {}
    internal partial struct mheap_userArena {}
    internal partial struct mlink {}
    internal partial struct mmap_args {}
    internal partial struct moduledata {}
    internal partial struct modulehash {}
    internal partial struct mspan {}
    internal partial struct mstats {}
    internal partial struct nanotime1_r {}
    internal partial struct neverCallThisFunction {}
    internal partial struct newmHandoffᴛ1 {}
    internal partial struct notInHeap {}
    internal partial struct notInHeapSlice {}
    internal partial struct notifyList {}
    internal partial struct offAddr {}
    internal partial struct pMask {}
    internal partial struct pTraceState {}
    internal partial struct pageAlloc {}
    internal partial struct pageAlloc_scav {}
    internal partial struct pageBits {}
    internal partial struct pageCache {}
    internal partial struct pallocBits {}
    internal partial struct pallocData {}
    internal partial struct pallocSum {}
    internal partial struct pcHeader {}
    internal partial struct pcvalueCache {}
    internal partial struct pcvalueCacheEnt {}
    internal partial struct persistentAlloc {}
    internal partial struct piController {}
    internal partial struct pinState {}
    internal partial struct pinner {}
    internal partial struct pinnerBits {}
    internal partial struct plainError {}
    internal partial struct pollCache {}
    internal partial struct pollDesc {}
    internal partial struct pollInfo {}
    internal partial struct printDebugLog_best {}
    internal partial struct printDebugLog_readState {}
    internal partial struct proc_regionfilename_args {}
    internal partial struct profAtomic {}
    internal partial struct profBuf {}
    internal partial struct profBufReadMode {}
    internal partial struct profIndex {}
    internal partial struct profᴛ1 {}
    internal partial struct ptabEntry {}
    internal partial struct pthread {}
    internal partial struct pthreadattr {}
    internal partial struct pthreadcond {}
    internal partial struct pthreadcondattr {}
    internal partial struct pthreadmutex {}
    internal partial struct pthreadmutexattr {}
    internal partial struct randomEnum {}
    internal partial struct randomOrder {}
    internal partial struct readmemstats_m_bySize {}
    internal partial struct reflectMethodValue {}
    internal partial struct reflectOffsᴛ1 {}
    internal partial struct regmmst {}
    internal partial struct regs32 {}
    internal partial struct regs64 {}
    internal partial struct regxmm {}
    internal partial struct runtimeSelect {}
    internal partial struct rwmutex {}
    internal partial struct scase {}
    internal partial struct scavChunkData {}
    internal partial struct scavChunkFlags {}
    internal partial struct scavengeIndex {}
    internal partial struct scavengerState {}
    internal partial struct selectDir {}
    internal partial struct semTable {}
    internal partial struct semTableᴛ1 {}
    internal partial struct semaProfileFlags {}
    internal partial struct semaRoot {}
    internal partial struct sigTabT {}
    internal partial struct sigactiont {}
    internal partial struct sigctxt {}
    internal partial struct siginfo {}
    internal partial struct sigset {}
    internal partial struct sigᴛ1 {}
    internal partial struct sliceInterfacePtr {}
    internal partial struct spanAllocType {}
    internal partial struct spanClass {}
    internal partial struct spanSet {}
    internal partial struct spanSetBlock {}
    internal partial struct spanSetBlockAlloc {}
    internal partial struct spanSetSpinePointer {}
    internal partial struct special {}
    internal partial struct specialPinCounter {}
    internal partial struct specialReachable {}
    internal partial struct specialWeakHandle {}
    internal partial struct specialfinalizer {}
    internal partial struct specialprofile {}
    internal partial struct specialsIter {}
    internal partial struct stackLargeᴛ1 {}
    internal partial struct stackObject {}
    internal partial struct stackObjectBuf {}
    internal partial struct stackObjectBufHdr {}
    internal partial struct stackObjectRecord {}
    internal partial struct stackScanState {}
    internal partial struct stackWorkBuf {}
    internal partial struct stackWorkBufHdr {}
    internal partial struct stackfreelist {}
    internal partial struct stackmap {}
    internal partial struct stackpoolItem {}
    internal partial struct stackpoolᴛ1 {}
    internal partial struct stackt {}
    internal partial struct statAggregate {}
    internal partial struct statDep {}
    internal partial struct statDepSet {}
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
    internal partial struct syscall_rawSyscall6_args {}
    internal partial struct syscall_rawSyscall_args {}
    internal partial struct syscall_syscall6X_args {}
    internal partial struct syscall_syscall6_args {}
    internal partial struct syscall_syscallPtr_args {}
    internal partial struct syscall_syscallX_args {}
    internal partial struct syscall_syscall_args {}
    internal partial struct sysmontick {}
    internal partial struct taggedPointer {}
    internal partial struct textsect {}
    internal partial struct throwType {}
    internal partial struct ticksType {}
    internal partial struct timeHistogram {}
    internal partial struct timeTimer {}
    internal partial struct timer {}
    internal partial struct timerWhen {}
    internal partial struct timers {}
    internal partial struct timespec {}
    internal partial struct timeval {}
    internal partial struct tmpBuf {}
    internal partial struct traceAdvance_untracedG {}
    internal partial struct traceAdvancerState {}
    internal partial struct traceArg {}
    internal partial struct traceBlockReason {}
    internal partial struct traceBuf {}
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
    internal partial struct traceMapNode {}
    internal partial struct traceProcStatus {}
    internal partial struct traceRegionAlloc {}
    internal partial struct traceRegionAllocBlock {}
    internal partial struct traceRegionAllocBlockHeader {}
    internal partial struct traceSchedResourceState {}
    internal partial struct traceStackTable {}
    internal partial struct traceStringTable {}
    internal partial struct traceTime {}
    internal partial struct traceTypeTable {}
    internal partial struct traceWriter {}
    internal partial struct tracestat {}
    internal partial struct typeCacheBucket {}
    internal partial struct typePointers {}
    internal partial struct ucontext {}
    internal partial struct uint16InterfacePtr {}
    internal partial struct uint32InterfacePtr {}
    internal partial struct uint64InterfacePtr {}
    internal partial struct unwindFlags {}
    internal partial struct unwinder {}
    internal partial struct userArena {}
    internal partial struct userArenaStateᴛ1 {}
    internal partial struct usigactiont {}
    internal partial struct waitq {}
    internal partial struct wakeableSleep {}
    internal partial struct wbBuf {}
    internal partial struct winlibcall {}
    internal partial struct workType {}
    internal partial struct workType_assistQueue {}
    internal partial struct workType_sweepWaiters {}
    internal partial struct workType_wbufSpans {}
    internal partial struct workbuf {}
    internal partial struct workbufhdr {}
    internal partial struct worldStop {}
    internal partial struct writeBarrierᴛ1 {}
    public partial interface ΔError {}
    public partial struct BlockProfileRecord {}
    public partial struct Frame {}
    public partial struct Frames {}
    public partial struct Func {}
    public partial struct MemProfileRecord {}
    public partial struct MemStats {}
    public partial struct MemStats_BySize {}
    public partial struct PanicNilError {}
    public partial struct Pinner {}
    public partial struct StackRecord {}
    public partial struct TypeAssertionError {}
    public partial struct ΔcgoCallers {}
    public partial struct ΔfuncInfo {}
    public partial struct Δhchan {}
    public partial struct Δhex {}
    public partial struct Δrtype {}
    public partial struct Δscavengeᴛ1 {}
    public partial struct Δsliceᴛ {}
    public partial struct ΔsrcFunc {}
    public partial struct Δtraceᴛ1 {}
    public partial struct ΔwriteUserArenaHeapBits {}
    // </TypeAccessibility>
}
