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

// Free functions ("funcName") and methods on other types ("recvTypeName.funcName") owned by the
// same manual files — declarations whose bodies are inseparable from the manual types' semantics.
var manualConversionFuncs = map[string]map[string]bool{
	"runtime": {
		"g.guintptr": true,
		"setGNoWB":   true,
		"setMNoWB":   true,
		// lock_sema.go (lock_sema_impl.cs): the mutex/note key-slot protocol smuggles an *m
		// address through the uintptr slot and parks on OS semaphores — the managed model is a
		// {0, locked} spinlock/latch. Thin wrappers (lock/unlock/noteclear/notetsleep[g]) and
		// the consts stay auto. ⚠ These entries encode lock_SEMA semantics (the windows/darwin/
		// plan9 family — the default host platform): a futex-platform conversion (-platforms
		// linux/amd64) includes lock_futex.go instead, whose notetsleep_internal is 2-parameter
		// and whose key protocol is {0,1,2} — the name-keyed skip would mismatch (CS7036).
		"mutexContended":      true,
		"lock2":               true,
		"unlock2":             true,
		"notewakeup":          true,
		"notesleep":           true,
		"notetsleep_internal": true,
		// The PROCESS-CONTROL surface (managed_impl.cs). Each of these is a public runtime API
		// whose converted body drives Go's own scheduler / GC pacer — stopTheWorld, gcStart,
		// mcall(gosched_m), the g/m/p stack walk — machinery that has no managed counterpart and
		// dies on the first getg()/mcall() assembly stub. The CLR does, however, answer every one
		// of these API CONTRACTS natively, so they are reimplemented at the API boundary the same
		// way sync's Mutex/notifyList were: honor the observable contract, never emulate the
		// mechanism. Everything BELOW them (the scheduler, the pacer, the mark/sweep engine) stays
		// auto-converted and simply becomes unreachable.
		"GC":         true,
		"GOMAXPROCS": true,
		"Gosched":    true,
		// Goexit belongs to the same surface for the same reason: its converted body drives Go's
		// own _panic record and stack unwinder (p.start(getcallerpc(), getcallersp()) → nextDefer →
		// goexit1), all of it assembly. The managed shape unwinds the calling goroutine with a
		// golib GoexitException, which the defer machinery and the goroutine root already handle —
		// see managed_impl.cs and docs/Phase4/DESIGN-goexit.md.
		"Goexit":         true,
		"Stack":          true,
		"ReadMemStats":   true,
		"LockOSThread":   true,
		"UnlockOSThread": true,
		// The lower-case pair is the runtime-internal variant of the same contract (syscall and
		// mime's registry reader reach it through startTemplateThread); it takes the same body.
		"lockOSThread":   true,
		"unlockOSThread": true,
		// Pinner: the "address is stable while pinned" contract already holds for managed ж<T>
		// boxes (the GC tracks them through moves), so the pin set is a no-op by construction —
		// the auto bodies walk the scheduler (acquirem) and span table (setPinned → findObject).
		// internal/fmtsort's test init is the demonstrated consumer.
		"Pinner.Pin":   true,
		"Pinner.Unpin": true,
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
		"Callers":     true,
		"Frames.Next": true,
	},
	// internal/abi.TypeOf reads an interface's type-word via unsafe.Pointer to reach a Go runtime
	// type descriptor that has no managed form (the reflection bridge — Phase 4). type_impl.cs
	// synthesizes an abi.Type whose Kind_ is classified from the value's managed System.Type. See
	// docs/Phase4/DESIGN-reflection-bridge.md.
	"internal/abi": {
		"TypeOf": true,
	},
	// reflect.Value's entry + value-reader methods (the reflection bridge, Phase 2). Go reads the
	// value through v.ptr as flat memory at computed offsets — no managed form. value_impl.cs carries
	// the boxed managed value directly (a companion `partial struct Value { object boxed }` field) and
	// reads it with System.Reflection + the golib container interfaces. Only the value READERS are
	// hand-owned; Kind/Type/IsValid/CanAddr work from the flag/typ_ the entry sets. Increment 1
	// (scalars, slices, arrays, pointers); struct Field/NumField + map MapRange land next.
	"reflect": {
		"ValueOf":             true,
		"unpackEface":         true,
		"valueInterface":      true, // a free function `valueInterface(v Value, safe bool)`, not a method
		"Value.Interface":     true,
		"Value.Bool":          true,
		"Value.Int":           true,
		"Value.Uint":          true,
		"Value.Float":         true,
		"Value.Complex":       true,
		"Value.String":        true,
		"Value.IsNil":         true,
		"Value.Len":           true,
		"Value.Index":         true,
		"Value.Elem":          true,
		"Value.Bytes":         true,
		"Value.NumField":      true,
		"Value.Field":         true,
		"Value.UnsafePointer": true,
		"Value.Pointer":       true,
		"Value.MapRange":      true,
		"MapIter.Next":        true,
		"MapIter.Key":         true,
		"MapIter.Value":       true,
		// Type side: reflect.rtype's ΔType methods over the abi.Type's System.Type (%T, %+v names).
		"rtype.String": true,
		"rtype.Name":   true,
		// rtype.PkgPath reads the descriptor's TFlagNamed bit and uncommon().PkgPath name-offset —
		// sub-records a synthesized abi.Type never populates, so it answered "" for every type and
		// gob's Register keyed its registry on the bare "N2" instead of "encoding/gob.N2"
		// (TestRegistrationNaming). The managed nesting carries the package identity
		// (GoReflect.GoPackagePath).
		"rtype.PkgPath":  true,
		"rtype.Elem":     true,
		"rtype.Field":    true,
		"rtype.NumField": true,
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
		"rtype.NumMethod":    true,
		"rtype.Method":       true,
		"rtype.MethodByName": true,
		"Value.Method":       true,
		// reflect.Type must be CANONICAL (Go interns type descriptors so `aType == bType` holds for
		// equal types — internal/fmtsort.compare relies on `aType != bType`). The auto Value.Type()
		// and toType() mint a fresh wrapper per call over a fresh abi.Type box, so identity-equality
		// never matched → map-key sorting reversed. The hand-owned forms in value_impl.cs intern the
		// ΔType wrapper by the underlying System.Type (canonType). See docs/Phase4/DESIGN-reflection-bridge.md.
		"Value.Type": true,
		"toType":     true,
		// deepValueEqual keys its cycle-detection visited map on the values' internal data words
		// (v.ptr / v.pointer()) — eface addresses the bridge never populates, so the auto form NREs
		// converting the null unsafe.Pointer slot (strings/bytes TestSplit/TestSplitAfter, R5).
		// deepequal_impl.cs recurses over the bridge's boxed values and keys cycle detection on
		// managed reference identity. DeepEqual itself stays auto (it only uses the bridged
		// ValueOf/Type/AreEqual).
		"deepValueEqual": true,
		// Phase-3 write-back (the chip): Set writes through the addressable Value's aliased ж box
		// (Go's assignTo semantics over the golib assert machinery); Zero builds valid zero Values
		// (a pointer kind yields the canonical typed-nil box); methodName walks the managed stack
		// (runtime.Caller has no managed form — its getcallersp chain NotImplementedException'd
		// every mustBe* panic path, errors TestAs's first operational hit).
		"Value.Set":  true,
		"Zero":       true,
		"methodName": true,
		// Phase-3 increment 2 (the chip): the call & construction half. Value.Call invokes the
		// boxed delegate (DynamicInvoke; results typed by the STATIC out types); the Set* family
		// coerces through GoReflect.TryConvertTo and writes through the aliased box; New/
		// MakeSlice/MakeMap construct golib containers/boxes (named wrappers included, via
		// ISupportMake); Slice windows the shared backing; the rtype func-introspection methods
		// derive from the delegate Invoke signature; Key/Len read GoReflect/descriptor cargo.
		// See docs/Phase4/DESIGN-reflection-bridge-phase3-plan.md (INCREMENT 2).
		"Value.Call":        true,
		"Value.CallSlice":   true,
		"Value.Slice":       true,
		"Value.SetBool":     true,
		"Value.SetInt":      true,
		"Value.SetUint":     true,
		"Value.SetFloat":    true,
		"Value.SetComplex":  true,
		"Value.SetString":   true,
		"Value.SetZero":     true,
		"Value.SetMapIndex": true,
		"New":               true,
		"MakeSlice":         true,
		"MakeMap":           true,
		"MakeMapWithSize":   true,
		// valueMethodName is runtime.Callers-based (getcallersp) — managed stack walk instead.
		"valueMethodName":  true,
		"rtype.Key":        true,
		"rtype.Len":        true,
		"rtype.NumIn":      true,
		"rtype.In":         true,
		"rtype.NumOut":     true,
		"rtype.Out":        true,
		"rtype.IsVariadic": true,
		// Phase-3 continuation: the type-relation mirrors + conversion. The auto forms walk
		// descriptor sub-records that only exist in Go's runtime layout: implements() does
		// Reinterpret<abi.Type, interfaceType> and reads .Methods off a promoted-embed box that
		// is default behind a synthesized descriptor (gob's init died there); PointerTo builds
		// a ptrType prototype through an eface Reinterpret; Convert dispatches into the cvt*
		// family, which allocates through the nil unsafe_New stub (internal/fmtsort's ct()
		// table, R-13/R-14). All four are bridged in value_impl.cs over the shared golib
		// machinery (GoReflect.GoImplements / TryConvertTo) — one method-set/convertibility
		// rule everywhere.
		"rtype.Implements":   true,
		"rtype.AssignableTo": true,
		"PointerTo":          true,
		"Value.Convert":      true,
		// rtype.FieldByName Reinterprets the descriptor as a structType and reads .Fields off
		// the default promoted-embed box (gob's compileDec matching wire fields to the local
		// struct). Bridged over the shared GoFields projection — the SAME field table
		// NumField/Field/the value side use, single-hop Index included.
		"rtype.FieldByName": true,
		// Value.Cap reads the never-populated v.ptr slice header (gob's decodeSlice probes
		// `value.Cap() < n`); Value.SetLen writes a new header length through it. Bridged over
		// the golib container interfaces; SetLen re-windows the live slice (same backing/cap,
		// Go's s[:n]) and writes it back through the aliased box.
		"Value.Cap":    true,
		"Value.SetLen": true,
		// Value.Grow reads a *unsafeheader.Slice off the same never-populated v.ptr, so it
		// nil-deref'd for every caller (gob's decUint8Slice / decodeArrayHelper Grow(1) in a
		// loop past internal/saferio's 10 MiB chunk). Bridged as an ordinary managed
		// reallocation written back through the aliased box, exactly like SetLen.
		"Value.Grow": true,
		// Value.IsZero is three descriptor reads a synthesized descriptor never populates —
		// an Equal function pointer against the shared zeroVal buffer, a TFlagRegularMemory
		// all-bits-zero scan, and `v.ptr == nil` for a non-indirect value. The Array and
		// Struct arms both fell to that last one, so EVERY array and EVERY struct reported
		// itself zero whatever it held — silently, `true` being right for the zero value.
		// Bridged as Go's own recursive definition with the memory shortcuts removed.
		"Value.IsZero": true,
		// Value.Addr derives the pointer type through ptrTo → typesByString → the typelinks()
		// runtime stub (the linker-built type table has no managed form), so every Addr threw.
		// The bridge already holds the address: an addressable Value ALIASES the ж<T> box its
		// storage lives in, so Addr surfaces that box (gob's gobEncodeOpFor/gobDecodeOpFor climb
		// one level with Addr for every GobEncoder-implementing field).
		"Value.Addr": true,
	},
	// internal/reflectlite mirrors the reflect bridge for the mini-surface sort.Slice
	// exercises (ValueOf → Len, Swapper — sort's TestSlice was the first operational hit):
	// the auto forms reinterpret the interface's eface words, so the first touch derefs a
	// nil ж<abi.Type>. value_impl.cs carries the boxed managed value on a companion
	// `partial struct Value { object boxed }` field (typ_/flag set from the Phase-1
	// synthetic abi.Type, so Kind()/IsValid() work from value.cs unchanged); swapper_impl.cs
	// swaps through golib's non-generic ISlice indexer. See docs/Phase4/DESIGN-reflection-bridge.md.
	"internal/reflectlite": {
		"ValueOf":     true,
		"unpackEface": true,
		"Value.Len":   true,
		"Swapper":     true,
		// Phase-3 write-back — the errors.As surface. The auto forms read the never-populated
		// v.ptr eface word (IsNil answered TRUE for every pointer; Elem returned the invalid
		// Value) or descriptor sub-records synthType never populates (rtype.Elem panicked;
		// implements() reinterpreted the descriptor). Bridged in value_impl.cs / type_impl.cs
		// over the carried System.Type + the golib method-set machinery.
		"Value.Elem":         true,
		"Value.IsNil":        true,
		"Value.Set":          true,
		"rtype.Elem":         true,
		"rtype.Implements":   true,
		"rtype.AssignableTo": true,
		"methodName":         true,
		// rtype.String reads a type-descriptor NAME OFFSET into the linker-built name blob
		// (`t.nameOff(t.Str).Name()`) that a synthesized descriptor never populates, so it
		// answered "" for EVERY type — silently, since the empty string is a legal name for an
		// unnamed type. reflect's own rtype.String is already hand-owned over GoReflect.GoTypeName;
		// this is the same answer for the mini-bridge, so the two can never disagree.
		"rtype.String": true,
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
	// ⚠ Name-keyed, so this entry also matches os.(*File).readdir in dir_unix.go — a
	// `-platforms linux/amd64` conversion of os would drop its (perfectly convertible) unix readdir
	// and fail to link. Same platform caveat as runtime's lock_sema entries above.
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
		"File.readdir":    true,
		"readReparseLink": true,
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
		"copyChecker.check": true,
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
	// ⚠ Name-keyed like the entries above: `zsyscall_windows.go` is the Windows generated file, so
	// a non-Windows -platforms conversion never sees this declaration and the entry is inert there.
	"syscall": {
		"GetTimeZoneInformation": true,
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
		"findFirstFile1": true,
		"findNextFile1":  true,
		// The third member, and the one that fails SILENTLY: PROCESSENTRY32W is 568 bytes ending in
		// szExeFile[260] INLINE, where the converted ProcessEntry32 holds that as one
		// `array<uint16>` reference — the record is ~56 bytes, every field past th32DefaultHeapID
		// reads from the wrong offset, and nothing faults. syscall.Getppid therefore answered 0,
		// which os's TestGetppid is the demonstrated consumer of. dwSize is an INPUT the mirror has
		// to own as well: Go sets it from `unsafe.Sizeof(procEntry)`, which is the MANAGED size here.
		"Process32First": true,
		"Process32Next":  true,
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

	funcNames, ok := manualConversionFuncs[fn.Pkg().Path()]

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

	return funcNames[named.Obj().Name()+"."+fn.Name()]
}

// isManualFuncDecl reports whether the function declaration is owned by a manual conversion:
// either any method whose receiver base type is a manual type, or an explicitly listed
// free function / foreign-receiver method.
func (v *Visitor) isManualFuncDecl(funcDecl *ast.FuncDecl) bool {
	return isManualFuncDeclInPackage(v.pkg.Path(), funcDecl)
}

// isManualFuncDeclInPackage is isManualFuncDecl's package-path-keyed core, callable from the
// whole-package ANALYSIS passes that run before any Visitor exists (the hoisted-literal pre-pass
// must know which declarations emit only a placeholder comment — such a function never renders a
// FunctionPrefixMarker, so it can never carry a hoisted field declaration).
func isManualFuncDeclInPackage(pkgPath string, funcDecl *ast.FuncDecl) bool {
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

	if funcNames, ok := manualConversionFuncs[pkgPath]; ok {
		if recvName != "" {
			return funcNames[recvName+"."+funcName]
		}

		return funcNames[funcName]
	}

	return false
}
