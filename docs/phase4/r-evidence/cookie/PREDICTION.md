# Func cookie seat -- footprint PREDICTION (written before any two-seeded emission)

Base master 2ff42f7a160b4f87b2dcf3d9df45efddc9627821; cut = claude/r-func-cookie b1b8b552bf. Derived from the
12-site census of the carrying form at master (`Ꮡ(new @unsafe.Pointer((uintptr)` in *.cs and *.cs.auto) and
the cut's rule: at ONE level, a func / Go-pointer / unsafe.Pointer source reads its STORED word; uintptr,
channel, map, byte storage, type parameters and two-level reads are unchanged.

## -stdlib two-seeded diff, PER TARGET (windows, linux, darwin each)

| File | Go site | Source kind | Line change |
|---|---|---|---|
| runtime/<goos>/cgocall.cs | cgocall.go:445 `*(*unsafe.Pointer)(unsafe.Pointer(&cb)) = ...` | func | `new @unsafe.Pointer((uintptr)Ꮡcb)` -> `@unsafe.Pointer.OfFunc(cb)` |
| runtime/debugcall.cs | debugcall.go:266 `&dispatchF` | func | -> `@unsafe.Pointer.OfFunc(dispatchF)` |
| runtime/iface.cs | iface.go:502 and :605 `&s.Cache` | pointer | -> `@unsafe.Pointer.FromPinnedBox(<s.Cache>)`, 2 lines |

PREDICTED: 3 files, +4/-4, on EACH of the three targets. 0 GoPositionMap lines (every change is within one
line, so no package_info.cs moves). All four production sites are on paths nothing live reaches
(cgocallbackg1, debugCallWrap2, typeAssert, interfaceSwitch), so 0 behavior change is expected from them.

UNCHANGED, named: runtime/tracetype.cs:55 (byte storage), runtime/mcleanup.cs.auto:89 (`arg S`, a type
parameter).

## -tests emission census (production-only diff is blind to it)

runtime/syscall_windows_test.cs:207 nestedCall: `(uintptr)(~Ꮡ(new @unsafe.Pointer((uintptr)Ꮡf)))` ->
`(uintptr)(~Ꮡ(@unsafe.Pointer.OfFunc(f)))`. reflect/all_test.cs:10513/10514/10516 (chan, map, two-level
func) UNCHANGED.

## CNR: 0 of the behavioral projects change

UnsafePointerWordRead holds only uintptr and channel arms (both unchanged); ManagedAtomicPointer's
`atomic.LoadPointer((*unsafe.Pointer)(&l.p))` is lowered by the atomic special case, not this arm;
WindowsNewCallback deliberately avoids the func pun.

## Runtime rows (windows), predicted MOVE fail -> pass

TestCallback, TestCallbackGC, TestBlockingCallback, TestCallbackPanic, TestCallbackPanicLoop.
TestCallbackPanicLocked: predicted pass, UNCERTAIN on one axis only -- whether golib's LockedOSThread
state survives the exception unwinding through the native frame (the reading 906ad5eef1 measured the
unwind itself, not that state).

## Ranked uncertainties

1. TestCallbackPanicLocked (above).
2. A carrying site the committed census missed because the committed corpus is stale against the
   converter: the two-seeded diff, not the census, is the arbiter. Any extra file is scored as a miss.
3. `FromPinnedBox(<s.Cache>)`'s exact spelling at iface.cs (a field selector through a pointer
   parameter): the kind of change is predicted, not its text.
