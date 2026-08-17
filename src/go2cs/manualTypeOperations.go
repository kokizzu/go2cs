// manualTypeOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/ast"
	"go/types"
)

// Some Go declarations cannot be faithfully auto-converted because their semantics depend on
// hiding a managed pointer inside an integer (e.g. runtime's guintptr family): the CLR cannot
// hold a managed reference as a number across a GC move, so the managed conversion must store
// the ж<T> box DIRECTLY (model precedent: core/sync/atomic Pointer<T>). Those declarations are
// hand-converted in the package's *_impl.cs (marked [module: GoManualConversion], kept in
// src/core/<pkg>/ and restored over auto output by the overlay). The converter SKIPS emitting:
//   - the type declaration itself (a marker comment is left in its place),
//   - every method declared on the type,
//   - adjacent free functions / methods on other types listed in manualConversionFuncs,
//   - GoImplicitConv assembly attributes referencing the type (the manual file declares any
//     conversion operators its call sites need).
//
// Call-site emission is unchanged except conversions handled in convCallExpr: a manual-type
// conversion from unsafe.Pointer(x) emits the referent-preserving ctor form `new T(x)` instead
// of the numeric cast chain `(T)(uintptr)new Pointer(x)` (which would lose the referent).
//
// Keys are RAW Go identifiers (rename analyses apply downstream at emission).
var manualConversionTypes = map[string]map[string]bool{
	"runtime": {
		"guintptr": true,
		"puintptr": true,
		"muintptr": true,
	},
}

// goosScope names the target operating systems a manual-conversion entry applies to. The EMPTY
// scope (goosAny) means every target, which is what all but a handful of entries want.
//
// The scope exists because the registry is keyed by NAME, and a Go name is not unique across
// platforms: Go selects one of several files declaring the same function by build constraint, and
// those declarations are not interchangeable. Name-keyed alone, one entry silently spoke for all of
// them — turning every flavor's declaration into a placeholder while an implementation existed for
// only the one somebody hand-wrote. That is load-bearing in both directions:
//
//   - A declaration only Go's Windows sources carry (syscall's generated wrappers,
//     os.readReparseLink) is inert on other targets today, so scoping it moves no emission — but it
//     records WHY, instead of leaving the next reader to re-derive it from Go's file set.
//   - A declaration EVERY target carries, where only some flavors need hand-owning, cannot be
//     expressed at all without a scope. os.(*File).readdir is that case: the windows and darwin
//     flavors hand OS memory to a Go struct and must be hand-owned, while dir_unix.go's is pure Go
//     over internal/poll and converts faithfully — yet the unscoped entry deleted its body too, and
//     left every Linux os build with a placeholder and nothing to link against.
//
// What a scope deliberately does NOT express is a per-platform SIGNATURE. runtime's
// notetsleep_internal is four parameters in lock_sema.go and two in lock_futex.go; the answer is one
// goosAny entry — both flavors need hand-owning — with each flavor's own *_impl.cs declaring its own
// signature, which layout L3 already routes per GOOS. The registry decides WHETHER a declaration is
// hand-owned; the hand-owned file decides what it looks like.
type goosScope []string

// goosAny scopes an entry to every target — the common case, and the zero value, so an entry that
// says nothing keeps the registry's original name-keyed behavior.
var goosAny goosScope

// goosWindows scopes an entry to Windows alone.
var goosWindows = goosScope{"windows"}

// goosWindowsDarwin scopes an entry to the two targets whose Go flavor reinterprets or hands OS
// memory to a Go struct — the raw-metal-on-non-native-types fork — where the third does not.
var goosWindowsDarwin = goosScope{"windows", "darwin"}

// includes reports whether the scope covers the named target operating system.
func (scope goosScope) includes(goos string) bool {
	if len(scope) == 0 {
		return true
	}

	for _, name := range scope {
		if name == goos {
			return true
		}
	}

	return false
}

// Free functions ("funcName") and methods on other types ("recvTypeName.funcName") owned by the
// same manual files — declarations whose bodies are inseparable from the manual types' semantics.
var manualConversionFuncs = map[string]map[string]goosScope{
	"runtime": {
		"g.guintptr": goosAny,
		"setGNoWB":   goosAny,
		"setMNoWB":   goosAny,
		// The mutex/note key-slot protocol. Go has TWO flavors of it and selects one per GOOS:
		// lock_sema.go (windows, darwin, plan9, aix …) smuggles an *m address through the uintptr
		// slot and parks waiters on OS semaphores; lock_futex.go (linux, freebsd, dragonfly) uses a
		// {0,1,2} slot and parks on a futex. Neither OS primitive has a managed realization, so BOTH
		// flavors need hand-owning and both converge on the same managed model — a {0, keyLocked}
		// latch with SpinWait escalation — which is why the scope is goosAny and why the managed
		// core is ONE flat file (runtime/lock_managed_impl.cs) rather than a copy per flavor. Thin
		// wrappers (lock/unlock/noteclear/notetsleep) and the consts stay auto on both.
		//
		// The flavors DO differ in one signature: notetsleep_internal is (n, ns, gp, deadline) in
		// lock_sema.go and (n, ns) in lock_futex.go. A name-keyed registry cannot express that and
		// does not try — each flavor's own *_impl.cs declares its own, delegating to the shared core
		// (windows/darwin/lock_sema_impl.cs, linux/lock_futex_impl.cs), and layout L3 routes each to
		// exactly the platforms its principal is built on.
		//
		// notetsleepg is the one thin wrapper that could NOT stay auto. Its Go body opens with getg()
		// — still an unimplemented intrinsic — then semacreate/entersyscallblock over the same dead
		// g/m graph, so every caller threw before reaching the note at all. Its two callers are
		// exactly the ones that must WAIT rather than poll: sigqueue's signal_recv (idle until a
		// signal arrives, possibly for the life of the process) and profbuf's reader. Both run on a
		// goroutine, which golib gives a dedicated thread, so the managed body is a real blocking
		// wait — the only member of this family that blocks rather than spins. Its g0 sibling
		// notetsleep shares the getg() prologue but has no reachable caller, so it stays auto and
		// stays throwing rather than being hand-owned speculatively.
		"mutexContended":      goosAny,
		"lock2":               goosAny,
		"unlock2":             goosAny,
		"notewakeup":          goosAny,
		"notesleep":           goosAny,
		"notetsleep_internal": goosAny,
		"notetsleepg":         goosAny,
		// The PROCESS-CONTROL surface (managed_impl.cs). Each of these is a public runtime API
		// whose converted body drives Go's own scheduler / GC pacer — stopTheWorld, gcStart,
		// mcall(gosched_m), the g/m/p stack walk — machinery that has no managed counterpart and
		// dies on the first getg()/mcall() assembly stub. The CLR does, however, answer every one
		// of these API CONTRACTS natively, so they are reimplemented at the API boundary the same
		// way sync's Mutex/notifyList were: honor the observable contract, never emulate the
		// mechanism. Everything BELOW them (the scheduler, the pacer, the mark/sweep engine) stays
		// auto-converted and simply becomes unreachable.
		"GC":         goosAny,
		"GOMAXPROCS": goosAny,
		"Gosched":    goosAny,
		// Goexit belongs to the same surface for the same reason: its converted body drives Go's
		// own _panic record and stack unwinder (p.start(getcallerpc(), getcallersp()) → nextDefer →
		// goexit1), all of it assembly. The managed shape unwinds the calling goroutine with a
		// golib GoexitException, which the defer machinery and the goroutine root already handle —
		// see managed_impl.cs and docs/phase4/DESIGN-goexit.md.
		"Goexit":         goosAny,
		"Stack":          goosAny,
		"ReadMemStats":   goosAny,
		"LockOSThread":   goosAny,
		"UnlockOSThread": goosAny,
		// The lower-case pair is the runtime-internal variant of the same contract (syscall and
		// mime's registry reader reach it through startTemplateThread); it takes the same body.
		"lockOSThread":   goosAny,
		"unlockOSThread": goosAny,
		// Pinner: the "address is stable while pinned" contract already holds for managed ж<T>
		// boxes (the GC tracks them through moves), so the pin set is a no-op by construction —
		// the auto bodies walk the scheduler (acquirem) and span table (setPinned → findObject).
		// internal/fmtsort's test init is the demonstrated consumer.
		"Pinner.Pin":   goosAny,
		"Pinner.Unpin": goosAny,
		// The traceback surface (managed_impl.cs). Callers' auto body enters the raw-metal
		// unwinder on its first step (callers → getcallersp, an assembly stub), and Frames.Next
		// reads linker funcInfo tables (findfunc) that have no managed form. Both API contracts —
		// "record the calling goroutine's frames as opaque PCs" / "expand PCs to function/file/
		// line" — the CLR answers natively via System.Diagnostics.StackTrace, projected to
		// GO-LOGICAL frames (converted-source frames only; go2cs-gen shells and forwarders are
		// invisible, exactly as Go's interface dispatch adds no frame). getcallersp itself stays
		// an honest stub — a caller's stack pointer has no managed answer; the chain is severed
		// here, at the semantic boundary that does (the reflection bridge's methodName pattern).
		// io's TestMultiReaderFlatten / TestMultiWriterSingleChainFlatten (relative stack-depth
		// asserts over runtime.Callers) are the demonstrated consumers.
		"Callers":     goosAny,
		"Frames.Next": goosAny,
		// The metrics-table mutex (managed_impl.cs). Go's bodies acquire metricsSema, a runtime
		// sleeping semaphore whose acquire path is getg() → sudog → gopark — the scheduler
		// machinery that has no managed counterpart — so every path into the metrics table
		// (readMetrics behind runtime/metrics.Read, readMetricNames behind the metrics_test push)
		// died on the getg stub. The CONTRACT is mutual exclusion with waiter handoff over the
		// metrics map and agg scratch state; SemaphoreSlim(1, 1) is the CLR's spelling of exactly
		// that. Everything the lock protects (initMetrics' map build, readMetricsLocked's compute
		// closures) stays auto-converted.
		"metricsLock":   goosAny,
		"metricsUnlock": goosAny,
		// NumCgoCall (managed_impl.cs): its body walks the scheduler's `allm` thread list summing
		// per-m cgo-call counters — a list the managed model never populates (the walk nil-derefs
		// where Go always has at least m0). The CONTRACT is "number of cgo calls made by the
		// current process", and the managed model makes no cgo calls at all, so zero is the true
		// count rather than an approximation. Reached by the /cgo/go-to-c-calls:calls metric's
		// compute closure for every metrics.Read.
		"NumCgoCall": goosAny,
		// totalMutexWaitTimeNanos (managed_impl.cs): the same `allm` walk as NumCgoCall, summing
		// per-m lock-profile wait times that never exist here. The managed body keeps the two REAL
		// counter loads (sched.totalMutexWaitTime, sched.totalRuntimeLockWaitTime) and drops only
		// the walk. Reached by the /sync/mutex/wait/total:seconds metric's compute closure.
		"totalMutexWaitTimeNanos": goosAny,
		// The consistent heap-stats snapshot read (managed_impl.cs), the one call below the metrics
		// computes that still walked the scheduler: its body disables preemption (acquirem → getg)
		// to hold `allp` stable while merging every P's heap-stats delta. The managed model has no
		// Ps and nothing ever writes a heapStatsDelta (the CLR allocator does not populate Go's
		// allocator bookkeeping), so the faithful snapshot is the zero delta — the same class of
		// honest zero ReadMemStats' hand-own documents for the identical fields. Reached from
		// heapStatsAggregate.compute for every heap-dependent metric.
		"consistentHeapStats.read": goosAny,
		// The lower-case `callers` is the FUNNEL every other traceback entry point goes through
		// (Caller, mprof's profile recorders, proc's createstack, tracestack) and the one that
		// actually reaches getcallersp — so severing it here, one level below Callers, is what
		// makes runtime.Caller work rather than hand-owning Caller itself. Caller stays
		// auto-converted and Go-shaped; its `callers(skip+1, rpc)` now lands on the managed walk.
		// Go's own comment on the declaration ("almost identical to Callers", linkname'd by the
		// ecosystem, do not change the signature) says the same thing: it is an API boundary with
		// a managed answer. log's Output → Caller(calldepth) and testing/slogtest's withSource →
		// Caller(1) are the demonstrated consumers.
		"callers": goosAny,
	},
	// internal/abi.TypeOf reads an interface's type-word via unsafe.Pointer to reach a Go runtime
	// type descriptor that has no managed form (the reflection bridge — Phase 4). type_impl.cs
	// synthesizes an abi.Type whose Kind_ is classified from the value's managed System.Type. See
	// docs/phase4/DESIGN-reflection-bridge.md.
	// Type.StructType / Type.ArrayType are Go's PREFIX-DOWNCAST idiom —
	// `(*structType)(unsafe.Pointer(t))` reaches a sub-record the linker really allocated behind
	// the Type header. Nothing sits behind a ж<abi.Type>, and golib's Reinterpret rightly refuses
	// to alias managed storage for a reference-bearing pair, so the auto forms read the
	// specialization's fields out of the memory that follows the value slot: `Fields` came back as
	// a fabricated StructField[] of length 8830452760576 — an IndexOutOfRangeException on the
	// FIRST iteration of unique.buildStructCloneSeq, and internal/reflectlite's NumField/Len read
	// the same garbage. type_impl.cs synthesizes both specializations from the descriptor's
	// carried System.Type over the same golib layout machinery that stamps Size_/Align_.
	// Type.Elem / Type.Key are the SAME idiom one level in — Elem downcasts the header to the
	// slice/array/chan/map/ptr record that sits behind it and reads its Elem field, Key does it for
	// a map — so both inherited the defect and answered nil for every non-scalar descriptor. Nil is
	// not a state Go's callers test here: reflect's haveIdenticalType recurses straight into
	// nameFor(t), which nil-dereferences, so every ConvertibleTo/AssignableTo over a slice, map,
	// pointer, chan or array died (database/sql's TestConversions and TestUserDefinedBytes are the
	// measured pair). type_impl.cs synthesizes both from the carried System.Type over the SAME
	// golib element/key resolution reflect's own rtype.Elem/rtype.Key use one layer up.
	"internal/abi": {
		"TypeOf":          goosAny,
		"Type.StructType": goosAny,
		"Type.ArrayType":  goosAny,
		"Type.Elem":       goosAny,
		"Type.Key":        goosAny,
		// Type.Len is the third accessor of that same recursion, and the one whose failure is
		// nastiest: it reads a length out of the memory following the descriptor's value slot, so
		// two array descriptors read two different pieces of garbage and haveIdenticalUnderlyingType
		// reports [3]byte and [3]byte as different types. The carried dims already answer it (that
		// is what reflect's own rtype.Len reads), so hand-owning it turns a garbage read into the
		// same truthful one, one layer down.
		"Type.Len": goosAny,
		// Type.ChanDir is the FOURTH member of that family and the only one with no synthesis
		// waiting for it: the direction is not unpopulated, it is not in the managed type at all.
		// `<-chan int`, `chan<- int` and `chan int` all emit as golib's `channel<T>`, so the
		// bridge can only ever describe the BIDIRECTIONAL type — and BothDir is that type's real
		// direction, which Type.String() has always agreed with (`chan T`). The downcast instead
		// read a direction out of the memory following the value slot, non-deterministically.
		// A directional Go channel type remains undescribable here; that is a limit of the
		// converter's channel emission one layer up, recorded in ConversionStrategies-Reference.md.
		"Type.ChanDir": goosAny,
	},
	// internal/cpu.getGOAMD64level is declared in cpu_x86.s and its body is a COMPILE-TIME constant:
	// the GOAMD64_vN define the toolchain sets from `go env GOAMD64`, with `#else MOVL $1` as the
	// fall-through. It answers "which amd64 microarchitecture level was this BINARY built for", not
	// "which does this CPU support" — so probing the host would answer a different question. go2cs
	// emits portable C# with no GOAMD64 define and no microarchitecture-gated emission, which makes
	// the faithful answer the same constant Go's own assembly produces for a build without one.
	// cpu_x86_impl.cs returns it. Reached by doinit's option table (whose level < 2/3/4 gates decide
	// which cpu.* GODEBUG knobs stay switchable) and by internal/cpu's own TestDisableSSE3, whose
	// first line is `if GetGOAMD64level() > 1 { t.Skip(…) }` — the unimplemented stub turned that
	// guard into an infrastructure-error where Go reads 1 and walks on to a matching skip.
	"internal/cpu": {
		"getGOAMD64level": goosAny,
	},
	// reflect.Value's entry + value-reader methods (the reflection bridge, Phase 2). Go reads the
	// value through v.ptr as flat memory at computed offsets — no managed form. value_impl.cs carries
	// the boxed managed value directly (a companion `partial struct Value { object boxed }` field) and
	// reads it with System.Reflection + the golib container interfaces. Only the value READERS are
	// hand-owned; Kind/Type/IsValid/CanAddr work from the flag/typ_ the entry sets. Increment 1
	// (scalars, slices, arrays, pointers); struct Field/NumField + map MapRange land next.
	"reflect": {
		"ValueOf":             goosAny,
		"unpackEface":         goosAny,
		"valueInterface":      goosAny, // a free function `valueInterface(v Value, safe bool)`, not a method
		"Value.Interface":     goosAny,
		"Value.Bool":          goosAny,
		"Value.Int":           goosAny,
		"Value.Uint":          goosAny,
		"Value.Float":         goosAny,
		"Value.Complex":       goosAny,
		"Value.String":        goosAny,
		"Value.IsNil":         goosAny,
		"Value.Len":           goosAny,
		"Value.Index":         goosAny,
		"Value.Elem":          goosAny,
		"Value.Bytes":         goosAny,
		// The WRITE half of Bytes, hand-owned for the same reason one layer down: the auto body is
		// `*(*[]byte)(v.ptr) = x`, a store through the Go data word this bridge never populates, so
		// it wrote nowhere for EVERY byte slice — silently. See reflect/value_impl.cs.
		"Value.SetBytes":      goosAny,
		"Value.NumField":      goosAny,
		"Value.Field":         goosAny,
		"Value.UnsafePointer": goosAny,
		"Value.Pointer":       goosAny,
		"Value.MapRange":      goosAny,
		"MapIter.Next":        goosAny,
		"MapIter.Key":         goosAny,
		"MapIter.Value":       goosAny,
		// The map READ pair, same root as MapRange and landed against it: both auto bodies do
		// `v.typ().Reinterpret<abi.Type, mapType>()` to reach the key/elem types off the embedded
		// abi.MapType, and a synthesized descriptor has no abi.MapType behind it — the emitted
		// mapType holds that embed as a promoted REFERENCE box, so the reinterpret reads the
		// descriptor's first word as an object. go/ast's TestPrint (ast.Fprint over a map) died
		// there. Both also index through Go's mapaccess/hiter intrinsics, which MapRange already
		// replaced; MapKeys is now MapRange collected and MapIndex the golib comma-ok lookup, so
		// one key/element typing rule serves the walk and the lookup alike.
		"Value.MapKeys":  goosAny,
		"Value.MapIndex": goosAny,
		// Type side: reflect.rtype's ΔType methods over the abi.Type's System.Type (%T, %+v names).
		"rtype.String": goosAny,
		"rtype.Name":   goosAny,
		// rtype.PkgPath reads the descriptor's TFlagNamed bit and uncommon().PkgPath name-offset —
		// sub-records a synthesized abi.Type never populates, so it answered "" for every type and
		// gob's Register keyed its registry on the bare "N2" instead of "encoding/gob.N2"
		// (TestRegistrationNaming). The managed nesting carries the package identity
		// (GoReflect.GoPackagePath).
		"rtype.PkgPath":  goosAny,
		"rtype.Elem":     goosAny,
		"rtype.Field":    goosAny,
		"rtype.NumField": goosAny,
		// rtype.NumMethod reads uncommon() method tables a synthesized descriptor never
		// populates, so it answered 0 for every concrete type — and encoding/json's indirect()
		// gates its Unmarshaler/TextUnmarshaler discovery on NumMethod() > 0, so no custom
		// UnmarshalJSON was ever dispatched (time's TestTimeJSON / TestUnmarshalInvalidTimes).
		// Hand-owned over the same golib method-set machinery the emitted asserts resolve
		// through (GoReflect.GoMethodCount), so the gate and the assert cannot disagree.
		//
		// The COUNT and the WALK are one increment, not two: a truthful NumMethod is what lets a
		// consumer's `for i := 0; i < n; i++` reach Method(i) at all, and the auto Method(i) reads
		// the same absent uncommon() table — loudly (panic: reflect: Method index out of range,
		// math/rand and math/rand/v2's TestRegress) where the count failed silently. Value.Method
		// binds the receiver into a managed delegate, so the method VALUE is an ordinary Kind-Func
		// Value and Type()/NumIn/In/Out/Call are the existing bridge surface unchanged.
		"rtype.NumMethod":    goosAny,
		"rtype.Method":       goosAny,
		"rtype.MethodByName": goosAny,
		"Value.Method":       goosAny,
		// reflect.Type must be CANONICAL (Go interns type descriptors so `aType == bType` holds for
		// equal types — internal/fmtsort.compare relies on `aType != bType`). The auto Value.Type()
		// and toType() mint a fresh wrapper per call over a fresh abi.Type box, so identity-equality
		// never matched → map-key sorting reversed. The hand-owned forms in value_impl.cs intern the
		// ΔType wrapper by the underlying System.Type (canonType). See docs/phase4/DESIGN-reflection-bridge.md.
		"Value.Type": goosAny,
		"toType":     goosAny,
		// deepValueEqual keys its cycle-detection visited map on the values' internal data words
		// (v.ptr / v.pointer()) — eface addresses the bridge never populates, so the auto form NREs
		// converting the null unsafe.Pointer slot (strings/bytes TestSplit/TestSplitAfter, R5).
		// deepequal_impl.cs recurses over the bridge's boxed values and keys cycle detection on
		// managed reference identity. DeepEqual itself stays auto (it only uses the bridged
		// ValueOf/Type/AreEqual).
		"deepValueEqual": goosAny,
		// Phase-3 write-back (the chip): Set writes through the addressable Value's aliased ж box
		// (Go's assignTo semantics over the golib assert machinery); Zero builds valid zero Values
		// (a pointer kind yields the canonical typed-nil box); methodName walks the managed stack
		// (runtime.Caller has no managed form — its getcallersp chain NotImplementedException'd
		// every mustBe* panic path, errors TestAs's first operational hit).
		"Value.Set":  goosAny,
		"Zero":       goosAny,
		"methodName": goosAny,
		// Phase-3 increment 2 (the chip): the call & construction half. Value.Call invokes the
		// boxed delegate (DynamicInvoke; results typed by the STATIC out types); the Set* family
		// coerces through GoReflect.TryConvertTo and writes through the aliased box; New/
		// MakeSlice/MakeMap construct golib containers/boxes (named wrappers included, via
		// ISupportMake); Slice windows the shared backing; the rtype func-introspection methods
		// derive from the delegate Invoke signature; Key/Len read GoReflect/descriptor cargo.
		// See docs/phase4/DESIGN-reflection-bridge-phase3-plan.md (INCREMENT 2).
		"Value.Call":        goosAny,
		"Value.CallSlice":   goosAny,
		"Value.Slice":       goosAny,
		"Value.SetBool":     goosAny,
		"Value.SetInt":      goosAny,
		"Value.SetUint":     goosAny,
		"Value.SetFloat":    goosAny,
		"Value.SetComplex":  goosAny,
		"Value.SetString":   goosAny,
		"Value.SetZero":     goosAny,
		"Value.SetMapIndex": goosAny,
		"New":               goosAny,
		"MakeSlice":         goosAny,
		"MakeMap":           goosAny,
		"MakeMapWithSize":   goosAny,
		// Copy reinterprets BOTH operands' data words as flat `unsafeheader.Slice` headers
		// (`*(*unsafeheader.Slice)(dst.ptr)`) and hands them to typedslicecopy — a raw memory move
		// with no managed form, which on the bridge's never-populated ptr slot dereferences a nil ж
		// outright. encoding/asn1's parseField copies every parsed []byte into its destination
		// through it, which is crypto/x509's ParsePKCS8PrivateKey and so crypto/ecdsa's TestEqual.
		// Bridged element-wise over the same golib container interfaces every other container
		// method uses, so a window slice writes the backing store it shares with its parent.
		"Copy": goosAny,
		// valueMethodName is runtime.Callers-based (getcallersp) — managed stack walk instead.
		"valueMethodName":  goosAny,
		"rtype.Key":        goosAny,
		"rtype.Len":        goosAny,
		"rtype.NumIn":      goosAny,
		"rtype.In":         goosAny,
		"rtype.NumOut":     goosAny,
		"rtype.Out":        goosAny,
		"rtype.IsVariadic": goosAny,
		// Phase-3 continuation: the type-relation mirrors + conversion. The auto forms walk
		// descriptor sub-records that only exist in Go's runtime layout: implements() does
		// Reinterpret<abi.Type, interfaceType> and reads .Methods off a promoted-embed box that
		// is default behind a synthesized descriptor (gob's init died there); PointerTo builds
		// a ptrType prototype through an eface Reinterpret; Convert dispatches into the cvt*
		// family, which allocates through the nil unsafe_New stub (internal/fmtsort's ct()
		// table, R-13/R-14). All are bridged in value_impl.cs over the shared golib
		// machinery (GoReflect.GoImplements / TryConvertTo) — one method-set/convertibility
		// rule everywhere.
		//
		// `implements` is the FREE function behind rtype.Implements, and it is registered
		// separately because it is what Go's OWN directlyAssignable / AssignableTo / convertOp /
		// Value.assignTo route through. Bridging the method alone left those four on the
		// throwing downcast, and it is also what let rtype.AssignableTo RETIRE from this list:
		// with `implements` and haveIdenticalUnderlyingType answerable and the descriptor
		// carrying TFlagNamed, Go's own `directlyAssignable(uu.t, t.t) || implements(uu.t, t.t)`
		// is correct, and a hand-own that restated it as identity-on-the-managed-type was
		// strictly narrower than the spec — database/sql's TestUserDefinedBytes is the measured
		// consumer that named the gap.
		"rtype.Implements": goosAny,
		"implements":       goosAny,
		// haveIdenticalUnderlyingType is THE seat of Go's type-identity relation (ConvertibleTo
		// through convertOp, AssignableTo through directlyAssignable, assignTo/Convert through
		// both). Five of its eight arms already worked — Array/Map/Pointer/Slice recurse through
		// the Elem()/Key()/Len() internal/abi synthesizes, and the scalar arm needs nothing. The
		// STRUCT, FUNC and INTERFACE arms reached their operands by the prefix-downcast idiom
		// instead, and did not fail loudly: they read ZERO fields / ZERO in-out counts / ZERO
		// methods off a default promoted-embed box and returned TRUE, so any two structs and any
		// two funcs compared IDENTICAL. Measured: `struct{B []byte; M map[string]int}` reported
		// convertible to the same struct with `M map[string]int64`, to one whose field is merely
		// renamed, and to one with a different field COUNT. A false positive in an identity
		// relation is read by every caller as permission, which is why it is fixed in the same
		// change that lets AssignableTo reach it.
		"haveIdenticalUnderlyingType": goosAny,
		// rtype.ChanDir downcasts onto the chanType record and reads a direction out of the
		// memory following the value slot — NON-DETERMINISTICALLY, so MakeChan's
		// `ChanDir() != BothDir` guard and the identity walk's chan arm each answered differently
		// run to run. The bridge answers BothDir because that is the only channel type it can
		// describe: a Go channel emits as golib's `channel<T>` whatever its direction, which
		// Type.String() has always reported as `chan T`. See internal/abi's Type.ChanDir.
		"rtype.ChanDir": goosAny,
		"PointerTo":     goosAny,
		"Value.Convert": goosAny,
		// rtype.FieldByName Reinterprets the descriptor as a structType and reads .Fields off
		// the default promoted-embed box (gob's compileDec matching wire fields to the local
		// struct). Bridged over the shared GoFields projection — the SAME field table
		// NumField/Field/the value side use, single-hop Index included.
		"rtype.FieldByName": goosAny,
		// Value.Cap reads the never-populated v.ptr slice header (gob's decodeSlice probes
		// `value.Cap() < n`); Value.SetLen writes a new header length through it. Bridged over
		// the golib container interfaces; SetLen re-windows the live slice (same backing/cap,
		// Go's s[:n]) and writes it back through the aliased box.
		"Value.Cap":    goosAny,
		"Value.SetLen": goosAny,
		// Value.Grow reads a *unsafeheader.Slice off the same never-populated v.ptr, so it
		// nil-deref'd for every caller (gob's decUint8Slice / decodeArrayHelper Grow(1) in a
		// loop past internal/saferio's 10 MiB chunk). Bridged as an ordinary managed
		// reallocation written back through the aliased box, exactly like SetLen.
		"Value.Grow": goosAny,
		// Value.IsZero is three descriptor reads a synthesized descriptor never populates —
		// an Equal function pointer against the shared zeroVal buffer, a TFlagRegularMemory
		// all-bits-zero scan, and `v.ptr == nil` for a non-indirect value. The Array and
		// Struct arms both fell to that last one, so EVERY array and EVERY struct reported
		// itself zero whatever it held — silently, `true` being right for the zero value.
		// Bridged as Go's own recursive definition with the memory shortcuts removed.
		"Value.IsZero": goosAny,
		// Value.Addr derives the pointer type through ptrTo → typesByString → the typelinks()
		// runtime stub (the linker-built type table has no managed form), so every Addr threw.
		// The bridge already holds the address: an addressable Value ALIASES the ж<T> box its
		// storage lives in, so Addr surfaces that box (gob's gobEncodeOpFor/gobDecodeOpFor climb
		// one level with Addr for every GobEncoder-implementing field).
		"Value.Addr": goosAny,
	},
	// internal/reflectlite mirrors the reflect bridge for the mini-surface sort.Slice
	// exercises (ValueOf → Len, Swapper — sort's TestSlice was the first operational hit):
	// the auto forms reinterpret the interface's eface words, so the first touch derefs a
	// nil ж<abi.Type>. value_impl.cs carries the boxed managed value on a companion
	// `partial struct Value { object boxed }` field (typ_/flag set from the Phase-1
	// synthetic abi.Type, so Kind()/IsValid() work from value.cs unchanged); swapper_impl.cs
	// swaps through golib's non-generic ISlice indexer. See docs/phase4/DESIGN-reflection-bridge.md.
	"internal/reflectlite": {
		"ValueOf":     goosAny,
		"unpackEface": goosAny,
		"Value.Len":   goosAny,
		"Swapper":     goosAny,
		// Phase-3 write-back — the errors.As surface. The auto forms read the never-populated
		// v.ptr eface word (IsNil answered TRUE for every pointer; Elem returned the invalid
		// Value) or descriptor sub-records synthType never populates (rtype.Elem panicked;
		// implements() reinterpreted the descriptor). Bridged in value_impl.cs / type_impl.cs
		// over the carried System.Type + the golib method-set machinery.
		"Value.Elem":         goosAny,
		"Value.IsNil":        goosAny,
		"Value.Set":          goosAny,
		"rtype.Elem":         goosAny,
		"rtype.Implements":   goosAny,
		"rtype.AssignableTo": goosAny,
		"methodName":         goosAny,
		// rtype.String reads a type-descriptor NAME OFFSET into the linker-built name blob
		// (`t.nameOff(t.Str).Name()`) that a synthesized descriptor never populates, so it
		// answered "" for EVERY type — silently, since the empty string is a legal name for an
		// unnamed type. reflect's own rtype.String is already hand-owned over GoReflect.GoTypeName;
		// this is the same answer for the mini-bridge, so the two can never disagree.
		"rtype.String": goosAny,
	},
	// os.(*File).readdir walks the raw buffer GetFileInformationByHandleEx fills by REINTERPRETING
	// it as a Go struct — `(*windows.FILE_ID_BOTH_DIR_INFO)(entry)`. That struct is managed-referent
	// (its trailing `FileName [1]uint16` / `ShortName [12]uint16` are golib `array<uint16>` object
	// references, 8 bytes where the OS wrote 2/24 inline), so the managed layout does NOT match the
	// bytes: `&info.FileName[0]` read a zero-length array (IndexOutOfRangeException at the FIRST
	// directory read — path/filepath.Glob, os.ReadDir, every testdata-reading test), and any copy of
	// the reinterpreted struct hands the GC a fabricated object reference. This is the raw-metal-on-
	// non-native-types fork: dir_windows_impl.cs decodes the entry fields from the byte slice at
	// their documented offsets and never materializes a managed struct over OS memory.
	// The DARWIN flavor is the same fork at a third site and is scoped in for it: dir_darwin.go's
	// readdir hands libc's readdir_r the ADDRESS of a Go `syscall.Dirent` (`readdir_r(d.dir,
	// &dirent, &entptr)`), whose converted form carries a managed `array<uint8>` reference where the
	// C struct has inline storage — the same non-blittable-struct-by-address seam as syscall's
	// wrappers below. Its hand-own is owed with macOS (increment 5); until then darwin emits a
	// placeholder, exactly as it did before this entry carried a scope.
	//
	// LINUX is scoped OUT, and that is what the scope bought: dir_unix.go's readdir is pure Go over
	// internal/poll's ReadDirent and the dirent_linux.go accessors, so it converts faithfully and
	// needs no hand-own at all. Name-keyed, this entry deleted that body too and left every Linux
	// `os` build with a placeholder and nothing to link against (54 CS0103 two layers below, then 1
	// here) — a hand-own gap invented by the registry rather than by the Go source.
	//
	// os.readReparseLink (file_windows.go) is the SAME fork at a second site: it reinterprets the
	// byte buffer DeviceIoControl fills as windows.{SymbolicLink,MountPoint}ReparseBuffer, both of
	// which end in `PathBuffer [1]uint16` — a Go inline array standing in for the variable-length
	// name the OS wrote after it, and an 8-byte MANAGED REFERENCE in the conversion. golib refuses
	// to alias managed storage for a reference-bearing struct (correctly — that is the fabrication
	// case), so the reinterpret takes the raw-address route and `&rb.PathBuffer[0]` resolves an
	// object reference synthesized out of path bytes: ACCESS_VIOLATION inside array<uint16>.get_Item,
	// which KILLED the whole C# test host mid-run at os's TestReadlink and emptied every verdict
	// after it. file_windows_impl.cs decodes the record from the byte slice at its documented
	// offsets. openSymlink and normaliseLinkPath stay auto — they pass scalars, handles and strings.
	"os": {
		"File.readdir":    goosWindowsDarwin,
		"readReparseLink": goosWindows,
	},
	// net.adapterAddresses is the SAME fork as os.readdir/readReparseLink above, at the biggest
	// record in the corpus — and it is the single producer behind every Windows interface and
	// DNS-configuration answer net can give (interfaceTable, interfaceAddrTable,
	// interfaceMulticastAddrTable, and dnsReadConfig, which is getSystemDNSConfig's ONLY source of
	// DNS servers on Windows). Go fills a []byte with GetAdaptersAddresses and then walks it as a
	// linked record: `(*windows.IpAdapterAddresses)(unsafe.Pointer(&b[0]))`. The CALL is legitimate;
	// the WALK is not. IpAdapterAddresses carries nine `ж<T>` fields, an `array<byte>` and an
	// `array<uint32>` where the native record has raw pointers and inline storage, so golib
	// correctly refuses to alias the byte run as that struct and the reinterpret falls to a
	// native-address box — after which the loop's own nil test fabricates a managed reference out of
	// adapter bytes and the PROCESS dies (ACCESS_VIOLATION in ж<IpAdapterAddresses>.op_Equality,
	// measured from crypto/tls's TestVerifyHostname through dnsReadConfig). It is what stood between
	// the corpus and any name resolution at all on Windows, one layer behind the GetAddrInfoW fix.
	//
	// The remedy is ADDRINFOW's, one structure size up: net/windows/interface_windows_impl.cs holds
	// the buffer in NATIVE memory that never escapes the function and transcribes the whole chain —
	// including each record's SIX nested lists and every sockaddr — into managed boxes, freeing the
	// native buffer eagerly. Its sockaddrs need no ManagedPointerTokens (unlike ADDRINFOW's) because
	// Go declares that field as a TYPED `*syscall.RawSockaddrAny`, so there is no unsafe.Pointer
	// round trip to survive — the consumers' `.Sockaddr()` is syscall's own hand-owned decode, and
	// the transcription writes the managed image that decode reads.
	//
	// ONLY adapterAddresses is hand-owned: its three interface_windows.go siblings and
	// dnsconfig_windows.go's dnsReadConfig read the managed records it returns and convert
	// faithfully. The generated windows.GetAdaptersAddresses wrapper is left auto too — it is
	// CORRECT for the native-address box the hand-own hands it, so hand-owning it would have fixed
	// nothing and frozen a faithful conversion for no gain.
	//
	// Declared only in net's interface_windows.go, so the entry is inert elsewhere; scoped anyway so
	// a same-named unix declaration cannot silently inherit a Windows hand-own the way readdir did.
	"net": {
		"adapterAddresses": goosWindows,
	},
	// sync's copyChecker detects a copied Cond by storing its OWN ADDRESS in itself and comparing:
	// `uintptr(*c) != uintptr(unsafe.Pointer(c))`. Both halves are raw-metal on a managed referent.
	// The stored word cannot be an address at all (the GC moves boxes, so a compaction between two
	// Wait calls would make a perfectly valid Cond look copied — a SPURIOUS panic), and the
	// address-of-self operand converts to `unsafe.Pointer.FromRef(ref c)` while the CAS operand
	// converts to `Ꮡ((uintptr)(c))`, which boxes a COPY of the value — so the auto body never
	// initializes the checker and never panics, however often the Cond is copied (sync's
	// TestCondCopy). cond_impl.cs compares the pointer's ROOT ALLOCATION IDENTITY instead, which is
	// stable across GC moves and is the managed spelling of the same question. Only this one method
	// is hand-owned; the copyChecker type and every Cond method stay auto.
	"sync": {
		"copyChecker.check": goosAny,
	},
	// syscall's generated GetTimeZoneInformation wrapper hands the native call the ADDRESS of the
	// managed Timezoneinformation box — `Syscall(…, uintptr(unsafe.Pointer(tzi)), …)`. Go's struct
	// is 172 bytes with two INLINE [32]uint16 name buffers; the converted one is ~64 bytes with two
	// `array<uint16>` MANAGED REFERENCES in their place, so the kernel writes 172 bytes of native
	// TIME_ZONE_INFORMATION over a smaller managed object and fabricates object references in the
	// name fields. The next `z.StandardName[:]` then faults (ACCESS_VIOLATION inside
	// slice<ushort>..ctor), which takes down every converted program that reaches
	// time.initLocal — i.e. any Weekday()/Location()/Local use on Windows.
	//
	// This is the same struct-passing seam as exec_windows.go's StartProcess and takes the same
	// remedy: a blittable [StructLayout(LayoutKind.Sequential)] mirror with `fixed` name buffers and
	// a direct P/Invoke, copying field-for-field into the converted struct at the boundary
	// (zsyscall_windows_impl.cs). ONLY this one wrapper is hand-owned; every other declaration in
	// the generated file stays auto — they pass scalars and handles, which convert faithfully.
	//
	// All five are declared ONLY in Go's Windows sources (zsyscall_windows.go / syscall_windows.go),
	// so a non-Windows conversion never sees the declaration and the entries were already inert
	// there. The scope changes no emission; it states the fact instead of leaving it to be
	// re-derived from Go's file set, and it is what keeps a future same-named unix declaration from
	// silently inheriting a Windows hand-own the way os.(*File).readdir did.
	"syscall": {
		"GetTimeZoneInformation": goosWindows,
		// The same seam over a bigger record, and the first member of the class an actual suite
		// reached: the kernel writes a 592-byte WIN32_FIND_DATAW, whose cFileName[260] and
		// cAlternateFileName[14] are 520 and 28 bytes of INLINE storage where the converted
		// win32finddata1 has two one-word `array<uint16>` references. path/filepath's EvalSymlinks
		// → toNorm → normBase asks FindFirstFile for the on-disk spelling of every path element,
		// and the clobbered reference took the test host down BOTH ways — IndexOutOfRangeException
		// inside PinnedBuffer where it still resolved to something, ACCESS_VIOLATION in
		// slice<ushort>..ctor where it did not. Only the two *1 wrappers are hand-owned: Go itself
		// puts the native-layout boundary at win32finddata1 (syscall_windows.go's FindFirstFile
		// allocates one, calls the wrapper, then copies out), so the public FindFirstFile /
		// FindNextFile and copyFindData above them are pure Go logic and convert faithfully.
		"findFirstFile1": goosWindows,
		"findNextFile1":  goosWindows,
		// The third member, and the one that fails SILENTLY: PROCESSENTRY32W is 568 bytes ending in
		// szExeFile[260] INLINE, where the converted ProcessEntry32 holds that as one
		// `array<uint16>` reference — the record is ~56 bytes, every field past th32DefaultHeapID
		// reads from the wrong offset, and nothing faults. syscall.Getppid therefore answered 0,
		// which os's TestGetppid is the demonstrated consumer of. dwSize is an INPUT the mirror has
		// to own as well: Go sets it from `unsafe.Sizeof(procEntry)`, which is the MANAGED size here.
		"Process32First": goosWindows,
		"Process32Next":  goosWindows,
		// The SOCKET-ADDRESS family — the member `net` forces, and the first that is two defects
		// rather than one (syscall_windows_impl.cs carries the full write-up).
		//
		// The two encode methods first. Go writes the port in network byte order through a
		// `(*[2]byte)(unsafe.Pointer(&raw.Port))` alias, and an `array<T>` rebuilt from a raw
		// address is a LENGTH-ZERO array, so `p[0]` panics before any socket call happens —
		// net.Listen never reached bind. Hand-owning them replaces the alias with a plain field
		// write; `raw` is left in exactly the state Go leaves it.
		//
		// RawSockaddrAny.Sockaddr — the DECODE — carries the SAME alias, and is here NOW; it was
		// deliberately absent until 2026-08-14 and that reason is worth keeping, because it is the
		// clearest case in the corpus of a hand-own gated on a CONVERTER CAPABILITY rather than on
		// effort. The only casts of the three Sockaddr types to ΔSockaddr in the package used to live
		// in ITS body, so displacing it dropped the `[assembly: GoImplement<…>(Pointer = true)]`
		// records from package_info.cs — and a MEASURED reconvert of `net` against that shortened
		// package_info showed net minting its OWN `syscall_SockaddrInet4жΔSockaddr` adapters instead
		// of using syscall's: the SECOND-IDENTITY regression samePackageImplements.go exists to
		// prevent.
		//
		// What changed is that recordSamePackageImplements now records the POINTER method set as well
		// as the value one, so the three records are sourced from types.Implements(*T, Sockaddr)
		// rather than from this body's casts. They survive its suppression, and the netpoll S2b lane
		// re-measured exactly that on its own build before adding the line below: with the body
		// displaced, syscall's package_info.cs still carries all three records and a reconvert of
		// `net` still references syscall's adapters rather than minting its own.
		//
		// Why it is taken at all: net's ACCEPT path decodes the AcceptEx output through it
		// (net/windows/fd_windows.cs:255-256), the one route to a Sockaddr that the hand-owned
		// Getsockname/Getpeername do not already cover — so no TCP round trip is reachable while it
		// panics. The old note said this path was "walled independently"; that wall is this arc.
		//
		"SockaddrInet4.sockaddr":  goosWindows,
		"SockaddrInet6.sockaddr":  goosWindows,
		"RawSockaddrAny.Sockaddr": goosWindows,
		// Then the seam itself. RawSockaddrInet4's `Addr [4]byte` / `Zero [8]uint8` are golib
		// `array<byte>` MANAGED REFERENCES, so `unsafe.Pointer(&sa.raw)` names a ~24-byte object
		// with references where Windows expects a 16-byte sockaddr_in with the octets inline —
		// the same class as GetTimeZoneInformation above, and the case golib's own ж.cs note
		// describes as having "no native layout either". Each of these builds a blittable mirror
		// in a stack buffer and passes its ADDRESS to the package's generated wrapper, which was
		// never the broken part; only the layout translation is hand-owned.
		//
		// Getsockname/Getpeername are here for the same reason a level down: their generated
		// wrappers take a typed `ж<RawSockaddrAny>` instead of an address, so the hand-owned
		// forms call the Syscall trampoline directly. The UDP senders (WSASendto and its Inet4/
		// Inet6 variants) are deliberately NOT listed — nothing on the TCP listen/dial/accept
		// path reaches them, and the board's ruling is to fix a censused wrapper when a suite
		// reaches it rather than speculatively.
		"Bind":        goosWindows,
		"Connect":     goosWindows,
		"ConnectEx":   goosWindows,
		"Getsockname": goosWindows,
		"Getpeername": goosWindows,
		// The OVERLAPPED family — the SUBMIT SEAM of the managed netpoller arc
		// (docs/phase4/DESIGN-netpoll-managed-poller.md §4.3/§4.4/§4.5;
		// syscall/windows/zsyscall_windows_wsa_impl.cs carries the full write-up). Same
		// struct-passing class as the sockaddr members above, plus the dimension async adds: the
		// kernel keeps the OVERLAPPED and the buffer pointers until COMPLETION, and `&o.o` is an
		// interior field address inside a reference-bearing `operation`, which golib's address model
		// explicitly cannot hold still — a transient address with an UNBOUNDED window. The OVERLAPPED
		// is also the operation's kernel-side IDENTITY: execIO names the same one at three call sites
		// (submit, CancelIoEx, WSAGetOverlappedResult) and cancellation matches BY ADDRESS, so a
		// fresh native copy per call would break cancellation outright. The hand-owns key a
		// per-operation record off the ж<Overlapped> and own the native lifetime.
		//
		// GetAcceptExSockaddrs is here for a DIFFERENT reason and is the one member with no identity
		// of its own — no handle, no overlapped, just the caller's buffer, which under go2cs is an
		// unpinned reinterpret over a managed array. It consumes a goroutine-keyed handoff AcceptEx
		// parks; the coupling is documented at both ends (coordinator ruling 2026-08-14).
		"WSARecv":              goosWindows,
		"WSASend":              goosWindows,
		"AcceptEx":             goosWindows,
		"GetAcceptExSockaddrs": goosWindows,
		"CancelIoEx":           goosWindows,
		// The UDP family (WSARecvFrom, WSASendto and its Inet4/Inet6 variants) and TransmitFile are
		// the same machinery with more staging and are deliberately absent: nothing on the TCP
		// listen/dial/accept/read/write path reaches them, and the board's ruling is to fix a
		// censused wrapper when a suite REACHES it.
		//
		// LoadConnectEx is NOT overlapped at all, and is here because the netpoll design recorded the
		// extension-pointer lookup as "synchronous and already working" and the crypto/tls census
		// MEASURED otherwise: nine loopback dials died in ~2 ms with "failed to find ConnectEx: An
		// invalid argument was supplied". Its single WSAIoctl is defective at both ends. IN:
		// `WSAID_CONNECTEX.Reinterpret<GUID, byte>()` — syscall.GUID holds `Data4 [8]byte` as a golib
		// array<byte> MANAGED REFERENCE, so the struct is reference-bearing, the reinterpret falls
		// back to an unpinned raw-address box, and the 16 bytes Windows compares are a CLR auto-layout
		// image with an object reference in them. Windows answers WSAEINVAL, every time, on every
		// host. OUT: the result address is an interior field of a struct holding an `error`, so it is
		// unpinnable and transient — even a successful call could write the function pointer into
		// memory the GC has moved. Both ends are fixed with the package's established stack-mirror
		// pattern; the GUID value still comes from the converted declaration.
		"LoadConnectEx": goosWindows,
		// The INITIALISATION pair, and the last two struct-passing members `net` reaches. Both run
		// inside internal/poll's InitWSA, once per process that imports `net`, one statement apart —
		// which is why they arrive together even though the chip named only the second.
		//
		// WSAEnumProtocols is the WORST OVERWRITE in the census by an order of magnitude.
		// WSAPROTOCOL_INFOW is 628 bytes native and ends in szProtocol[256] INLINE, with a GUID
		// ([8]byte) and a WSAPROTOCOLCHAIN ([7]uint32) nested inside it — three inline arrays the
		// conversion holds as three `array<T>` MANAGED REFERENCES, so the managed record is roughly
		// 120 bytes. checkSetFileCompletionNotificationModes asks for 32 of them and tells the kernel
		// so in BYTES: `len = unsafe.Sizeof(buf)`, which the converter answers with Go's 20,096 —
		// correct as a native size, and four fifths of it past the end of the ~3.8 KB managed array
		// the same call hands over. Compounding it, `Ꮡbuf.at<WSAProtocolInfo>(0)` cannot even be
		// pinned (PinnedBuffer.PinOnly refuses a reference-bearing element type), so the address was
		// transient as well as wrong. SIZE IS AN INPUT here exactly as it is for Process32First's
		// dwSize, with a second edge: the count Windows RETURNS interprets against native strides,
		// and on WSAENOBUFS it rewrites the caller's length with a required size that is a whole
		// number of NATIVE records.
		//
		// WSAStartup is the same class over WSADATA (~408 bytes native, szDescription[257] and
		// szSystemStatus[129] inline) and is here because it is UPSTREAM: the probe that reproduces
		// the enumeration defect could not reach it. `net` never reads the WSAData it passes, so the
		// overwrite has been silent since the corpus first dialled a socket — but a program that
		// reads Description dies with an ACCESS_VIOLATION in slice<byte>..ctor before
		// WSAEnumProtocols is called at all, the same signature GetTimeZoneInformation had. Silent is
		// not benign: this one runs in every converted program that imports `net`.
		//
		// Both answers are load-bearing beyond the corruption. useSetFileCompletionNotificationModes
		// is set only when EVERY enumerated entry carries XP1_IFS_HANDLES, and it decides
		// FD.skipSyncNotif — whether a synchronously-completing overlapped operation returns at once
		// or waits for a completion packet, which is the behaviour the netpoll design's OQ5 ratified
		// keeping. A corrupt enumeration silently picks the other IO path.
		"WSAStartup":       goosWindows,
		"WSAEnumProtocols": goosWindows,
		// The NAME-RESOLUTION pair, and the first member of this class whose OUTPUT is a LINKED
		// structure (zsyscall_windows_addrinfo_impl.cs carries the full write-up). Native ADDRINFOW
		// is 48 bytes of scalars and raw pointers where the converted AddrinfoW holds Canonname, Addr
		// and Next as MANAGED REFERENCES, so both directions are wrong: the hints Windows READS are
		// garbage, and the `*ADDRINFOW` it WRITES lands in a reference slot. Measured as a process
		// kill — `Fatal error. 0xC0000005` inside Syscall6 — with crypto/tls's TestVerifyHostname as
		// the first consumer; every converted program that resolves a name or a service reaches it
		// (net.Dial → resolveAddrList → LookupPort).
		//
		// Copying the top level alone would not be enough, which is what makes this one long: net
		// reads the sockaddr THROUGH the result (`(*RawSockaddrInet4)(unsafe.Pointer(result.Addr))`),
		// and RawSockaddrInet4's `Addr [4]byte` is an array<byte> BACKING REFERENCE — reading it out
		// of a native sockaddr_in is the fabricated-reference deref the sha3 arc named, which has no
		// general fix. So the whole chain is transcribed into managed records and the sockaddr with
		// it, carried across the `unsafe.Pointer` field by golib's ManagedPointerTokens (this is the
		// second minter of those tokens, and exactly the round trip they were written for).
		//
		// FreeAddrInfoW is hand-owned as a NO-OP for the same reason: the native chain is freed
		// eagerly at the copy, so nothing native escapes, and handing a managed object's address to
		// ws2_32's real free would release memory it does not own. GetAddrInfoW is the only producer
		// of a *AddrinfoW in Go's syscall package, so no caller can reach the free holding a chain
		// this file did not build.
		"GetAddrInfoW":  goosWindows,
		"FreeAddrInfoW": goosWindows,
		// The `**T` OUT-PARAMETER class — a SECOND syscall class, distinct from every member
		// above (zsyscall_windows_ptrout_impl.cs carries the full write-up). The members above
		// fail on a struct's LAYOUT; these fail with no struct in sight, because the argument is
		// one machine word and a golib pointer box has no such word to lend. `&p` for a Go
		// `var p *T` renders as ж<ж<T>>, whose storage is an OBJECT REFERENCE, and BOTH answers
		// golib's ж→uintptr operator can give are wrong: 0 while the held pointer is still null
		// (which is every out-parameter before the call), telling Windows "no output wanted" so
		// the call SUCCEEDS and the caller reads back its own nil; and a live MANAGED address
		// once it is not, which would have the kernel write a raw pointer over a slot the
		// collector reads as an object reference. Neither is fixable in the operator — no single
		// address is both kernel-writable as eight raw bytes and managed-readable as a ж<T> — so
		// the remedy is a native out-cell plus a publish at the one moment that can reconcile
		// them, which only the wrapper knows. The operator is deliberately unchanged.
		//
		// FIVE of the corpus's thirteen wrappers of this shape are taken, on the standing
		// fix-it-when-a-suite-reaches-it rule plus the requirement that a VALUE-LEVEL guard can
		// prove each one ("it no longer returns nil" is exactly the evidence this class's history
		// says not to trust). The SID pair is a round trip through advapi32 over `**uint16` and
		// `**SID` (Go's empty struct — an opaque handle nothing reads through, so a native box is
		// not merely safe but exactly right); NetGetJoinInformation adds a third DLL and a
		// different free routine, which is what makes the guard evidence for a CLASS; and the two
		// crypt32 members are crypto/x509's measured consumer. All five are guarded by the
		// PointerOutParameter behavioral output test.
		//
		// The eight left are left for stated reasons, not for lack of effort: DnsQuery/_DnsQuery
		// return a LINKED chain whose converted record holds managed references (the OTHER class,
		// wanting the ADDRINFOW treatment, in a `net` DNS arc); getQueuedCompletionStatus and its
		// exported sibling carry an OVERLAPPED whose identity the netpoll arc's per-operation
		// record owns; and GetFullPathName / NetUserGetInfo (plus internal/syscall/windows'
		// CreateEnvironmentBlock / NetUserGetLocalGroups) are the same safe shape with no corpus
		// consumer, hence no available proof.
		"ConvertSidToStringSid":            goosWindows,
		"ConvertStringSidToSid":            goosWindows,
		"NetGetJoinInformation":            goosWindows,
		"CertAddCertificateContextToStore": goosWindows,
		"CertGetCertificateChain":          goosWindows,
	},
	// The SECOND package holding the syscall struct-passing class, and the one member of it whose
	// established remedy is measured UNREACHABLE — so this entry declares a CAPABILITY LIMIT rather
	// than repairing a layout. Coordinator ruling 2026-08-14; the mechanism, the three costed
	// remedies and the six same-shape wrappers behind this one are in
	// docs/phase4/BOARD-next-validation-candidates.md, "RETRACTED — `os`'s REGRESSION is a HOST
	// CAPABILITY, and the killer is SHARE_INFO_2".
	//
	// Why the blittable-mirror remedy cannot reach it: the wrapper never sees the struct. os's
	// TestNetworkSymbolicLink writes `(*byte)(unsafe.Pointer(&p))`, which converts to
	// `Ꮡp.Reinterpret<windows.SHARE_INFO_2, byte>()`, and Reinterpret correctly REFUSES to alias a
	// reference-bearing struct as byte — so it falls to `(ж<byte>)(uintptr)box` and NetShareAdd
	// receives a native-address box with the managed identity already gone. There is nothing left to
	// copy from, and recovering the struct by reading that raw address would fabricate managed
	// references out of it, which ж.PointerExtensions.cs names as a CLR type-safety break.
	//
	// What the hand-own buys: netapi32 dereferences shi2_path — which, under the CLR's
	// reference-first auto-layout of SHARE_INFO_2, holds the integer 1 — and the process DIES with
	// 0xC0000005 partway through os's suite, turning 683 measurable verdicts into an unknown
	// remainder. Failing BY NAME converts a whole-suite process death into ONE loud row.
	"internal/syscall/windows": {
		"NetShareAdd": goosWindows,
		// The HARVEST half of the netpoll submit seam, and the only member of it outside `syscall`.
		// execIO harvests by naming the SAME `&o.o` it submitted, but the operation's real control
		// block is the native OVERLAPPED syscall's record allocated — so this wrapper must call the
		// real WSAGetOverlappedResult against THAT address. It reads it through golib's GoAsyncIO,
		// which is the whole contract between the two packages: syscall cannot expose the record
		// (a public seam on a published package is a non-Go symbol) and this package cannot reach
		// into it. Deriving the result from the completion callback instead was rejected on a
		// measured fidelity ground: a callback's errorCode is a WIN32 code where execIO and net
		// branch on WSA ones (ERROR_NETNAME_DELETED vs WSAECONNRESET).
		//
		// ⚠ This package's generated csproj emits AllowUnsafeBlocks=false, so its hand-own carries
		// [module: go.GoRequiresUnsafe] and the csproj flipping to true is part of the intended
		// footprint — unlike `syscall`, which already emits true.
		"WSAGetOverlappedResult": goosWindows,
	},
}

// isManualType reports whether the named type (raw Go name) is hand-converted in this package.
func (v *Visitor) isManualType(goTypeName string) bool {
	if typeNames, ok := manualConversionTypes[v.pkg.Path()]; ok {
		return typeNames[goTypeName]
	}

	return false
}

// isManualBoxReceiverMethod reports whether obj is a listed foreign-receiver manual method
// (a manualConversionFuncs "recvTypeName.funcName" entry). Such a method captures the
// receiver's IDENTITY (e.g. g.guintptr wraps the *g itself), so its manual implementation
// takes the receiver BOX (`this ж<T>`) — a deref-aliased call site must pass the box, not
// the value alias (see convSelectorExpr).
func (v *Visitor) isManualBoxReceiverMethod(obj types.Object) bool {
	fn, ok := obj.(*types.Func)

	if !ok || fn.Pkg() == nil {
		return false
	}

	funcScopes, ok := manualConversionFuncs[fn.Pkg().Path()]

	if !ok {
		return false
	}

	sig, ok := fn.Type().(*types.Signature)

	if !ok || sig.Recv() == nil {
		return false
	}

	recvType := sig.Recv().Type()

	if ptr, ok := recvType.(*types.Pointer); ok {
		recvType = ptr.Elem()
	}

	named, ok := recvType.(*types.Named)

	if !ok {
		return false
	}

	scope, listed := funcScopes[named.Obj().Name()+"."+fn.Name()]

	return listed && scope.includes(goosOfTarget(v.options.targetPlatform))
}

// isManualFuncDecl reports whether the function declaration is owned by a manual conversion:
// either any method whose receiver base type is a manual type, or an explicitly listed
// free function / foreign-receiver method.
func (v *Visitor) isManualFuncDecl(funcDecl *ast.FuncDecl) bool {
	return isManualFuncDeclInPackage(v.pkg.Path(), goosOfTarget(v.options.targetPlatform), funcDecl)
}

// isManualFuncDeclInPackage is isManualFuncDecl's package-path-keyed core, callable from the
// whole-package ANALYSIS passes that run before any Visitor exists (the hoisted-literal pre-pass
// must know which declarations emit only a placeholder comment — such a function never renders a
// FunctionPrefixMarker, so it can never carry a hoisted field declaration).
//
// `goos` is the conversion's TARGET operating system, which decides whether a scoped entry applies
// here at all — see goosScope. The analysis passes must pass the same value emission will, or a
// declaration would be hoisted-into and then emitted as a placeholder (or the reverse).
func isManualFuncDeclInPackage(pkgPath string, goos string, funcDecl *ast.FuncDecl) bool {
	if funcDecl == nil || funcDecl.Name == nil {
		return false
	}

	funcName := funcDecl.Name.Name
	recvName := ""

	if funcDecl.Recv != nil && len(funcDecl.Recv.List) > 0 {
		recvType := funcDecl.Recv.List[0].Type

		if starExpr, ok := recvType.(*ast.StarExpr); ok {
			recvType = starExpr.X
		}

		if ident, ok := recvType.(*ast.Ident); ok {
			recvName = ident.Name
		}
	}

	if recvName != "" {
		if typeNames, ok := manualConversionTypes[pkgPath]; ok && typeNames[recvName] {
			return true
		}
	}

	if funcScopes, ok := manualConversionFuncs[pkgPath]; ok {
		key := funcName

		if recvName != "" {
			key = recvName + "." + funcName
		}

		scope, listed := funcScopes[key]

		return listed && scope.includes(goos)
	}

	return false
}
