# DESIGN — `runtime.Pinner` over the CLR heap (Q45)

**STATUS: DESIGN + PREDICTION ON RECORD (SUB-Q45, 2026-09-04). Not a cut.** Coordinator-minted
from C1's runtime Linux bill (the `runtime.Pinner` family, 19 divergences at `44b5089b2`,
board block on `claude/c1-runtime-bill-docs` @ `a70e99c1a`); dispatched at the train-25 master
`db9e95841`, and every file:line below was read at that SHA. Parent designs:
[`DESIGN-pointer-provenance.md`](DESIGN-pointer-provenance.md) (RATIFIED — the
`ManagedPointerTokens` record and its validate-on-read) and
[`DESIGN-managed-pointer-token.md`](DESIGN-managed-pointer-token.md) (Q44, C2 — the record applied to
reference-bearing boxes, seated on train 26). Sibling classes:
[`DESIGN-object-lifetime-disclosure.md`](DESIGN-object-lifetime-disclosure.md) and the
`runtime-capability` ruling (board, 2026-08-20). C2 is cutting Q44 on the same seam as this is
written; this document deliberately contains no code and the cut is a later item.

**Two records were available and neither carries a per-row first line.** The only runtime comparison
record on this box is the getg-era run at `43cdb04fa` (2026-09-01), in which all 21 `TestPinner*`
rows read `Go="pass" C#=""` — the contiguous empty tail from `TestCaller`, unusable for
attribution. C1's bill at `44b5089b2` is the count of record (9 / 8 / 2) and names the mechanisms,
not the rows. **Every first line in §1 is therefore DERIVED from the code at `db9e95841` and stands
as a prediction**, reconciled against C1's counts in §1.2; the cut's gated re-read is the
measurement that scores it.

---

## 0. The finding in one paragraph

The converted runtime's `Pinner` is not "unimplemented over the CLR heap"; it is **two
half-implementations that contradict each other.** `Pinner.Pin`/`Unpin` are already hand-owned as
NO-OPS (`runtime/managed_impl.cs:1346–1361`, registered `manualTypeOperations.go:320–321`) on the
argument that a managed `ж<T>` box is address-stable by construction — which is true of address
stability and false of the two things Go's tests actually observe: the **pin BIT** (`isPinned`,
consulted by the cgo argument check) and the **lifetime HOLD** (a pinned object stays alive until
`Unpin`). Meanwhile `isPinned`, `pinnerGetPinCounter` and `cgoCheckPointer` stay CONVERTED and walk
the span table: `isPinned` dies in `spanOf` on the first read (`mheap_.arenas[0]` is never
allocated, so `l2.Value` is a nil dereference — `runtime/windows/mheap.cs:582–605`, identical in
the linux and darwin flavours), and `cgoCheckPointer` returns silently at its first line because
`debug.cgocheck == 0` — Go's default is 1, set by `parsedebugvars` (`runtime1.cs:441`), which sits
on the `schedinit` path the managed host never runs: the same *silently-UNREACHED init* class as
`internal/cpu`'s feature flags. The honest implementation is small — a pin COUNT keyed by the
allocation the pointer names, and a two-level walk of the argument's pointer words — and it needs
no byte on `ж<T>`, no CLR pin, and nothing from the token record beyond the `Resolve` it already
exports. 17 of the 19 rows are predicted to move; one is disclosed by its exact signature (the
RODATA/heap distinction has no managed truth); one is blocked on a sibling defect this design names
and prices but does not own (`unsafe.String` copies where Go aliases).

## 1. The population, row by row

`runtime/pinner_test.go` (go1.23.12) declares **21** `TestPinner*` functions and 13 benchmarks
(the benchmarks are Phase-4D-excluded by the pipeline and are not rows). No build tag gates the
file, `runtime.CgoCheckPointer` is exported by `export_test.go:53` unconditionally, and the check's
only gate is `GODEBUG=cgocheck` (default 1) — so **the cgo check IS observable without cgo**: the
oracle in C1's bill ran under the corpus's `CGO_ENABLED=0` and reported every row `pass`. Nothing
here is cgo-gated in Go; the dispatch's question is answered by the record.

### 1.1 Mechanisms at `db9e95841`

| id | mechanism | where |
|:--|:--|:--|
| **M1** | the test's first `IsPinned(addr)` reaches the converted `isPinned` → `spanOfHeap` → `spanOf`, which reads `mheap_.arenas[ri.l1()]` (nil: no arena was ever allocated; on amd64 `arenaL1Bits == 0`, so the nil guard on the next line is skipped) and dereferences `l2.Value` — a `NullReferenceException` surfaced as Go's nil-pointer panic and a failed row | `pinner.cs:75–91`, `windows/mheap.cs:582–605` |
| **M2** | `cgoCheckPointer` returns at `if (!goexperiment.CgoCheck2 && debug.cgocheck == 0)`: `CgoCheck2` is the constant `false` (`internal/goexperiment/exp_cgocheck2_off.cs:7`) and `debug.cgocheck` is the zero value because `parsedebugvars` never runs; the test's deferred `recover()` sees no panic | `windows/cgocall.cs:504`, `runtime1.cs:441` |
| **M3** | the no-op `Pin` performs no argument-kind check, so `Pin(int)` does not panic | `managed_impl.cs:1354` |
| **M4** | the no-op `Pin` holds nothing and sets no finalizer on the pinner, so the object dies at the first `runtime.GC()` and the leak finalizer never exists — the finalizer BRIDGE itself is fine (`mfinal.cs:435`, `SetFinalizer` → `ConditionalWeakTable` + sentinel → `GoFinalizerQueue`; `sync`'s `TestPoolGC` measures 98/100 on the first collection) | `managed_impl.cs:1354–1361`, `mfinal.cs` |

### 1.2 The 21 rows, with the first line each is predicted to print today

| # | row | state at master | first observable line (derived) | M | after the cut (§7) |
|--:|:--|:--|:--|:-:|:--|
| 1 | `TestPinnerSimple` | FAIL | nil-pointer panic from `isPinned` on the first `IsPinned(addr)` | M1 | PASS |
| 2 | `TestPinnerPinKeepsAliveAndReleases` | FAIL | `Pin() didn't keep object alive` | M4 | PASS |
| 3 | `TestPinnerMultiplePinsSame` | FAIL | nil-pointer panic (first `IsPinned`) | M1 | PASS |
| 4 | `TestPinnerTwoPinner` | FAIL | nil-pointer panic (first `IsPinned`) | M1 | PASS |
| 5 | `TestPinnerPinZerosizeObj` | FAIL | nil-pointer panic (`IsPinned` after the no-op `Pin`) | M1 | PASS |
| 6 | `TestPinnerPinGlobalPtr` | **PASS** | — (four `Pin`s, no assertion) | — | PASS |
| 7 | `TestPinnerPinTinyObj` | FAIL | nil-pointer panic (first `IsPinned`, i = 0) | M1 | PASS |
| 8 | `TestPinnerInterface` | FAIL | nil-pointer panic (first `IsPinned`) | M1 | PASS |
| 9 | `TestPinnerPinNonPtrPanics` | FAIL | `did not panic` (assertDidPanic; the kind check never runs) | M3 | PASS |
| 10 | `TestPinnerReuse` | FAIL | `cgoCheckPointer() did not panic, make sure the tests run with cgocheck=1` | M2 | PASS |
| 11 | `TestPinnerEmptyUnpin` | **PASS** | — | — | PASS |
| 12 | `TestPinnerLeakPanics` | FAIL | `leak didn't make GC to panic` | M4 | PASS |
| 13 | `TestPinnerCgoCheckPtr2Ptr` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 14 | `TestPinnerCgoCheckPtr2UnsafePtr` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 15 | `TestPinnerCgoCheckPtr2UnknownPtr` | FAIL | `did not panic` (assertDidPanic around `CgoCheckPointer(p2, nil)`) | M2 | PASS |
| 16 | `TestPinnerCgoCheckInterface` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 17 | `TestPinnerCgoCheckSlice` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 18 | `TestPinnerCgoCheckString` | FAIL | `cgoCheckPointer() did not panic…` | M2 | **REMAINS** — blocked on §6.2 (`unsafe.String` copies); PASS if the sibling rides |
| 19 | `TestPinnerCgoCheckPinned2UnpinnedPanics` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 20 | `TestPinnerCgoCheckPtr2Pinned2Unpinned` | FAIL | `cgoCheckPointer() did not panic…` | M2 | PASS |
| 21 | `TestPinnerConstStringData` | FAIL | nil-pointer panic (first `IsPinned`) | M1 | **DISCLOSED** `runtime-capability`, signature `not marked as pinned` (§6.1) |

**Reconciliation with C1's 9 / 8 / 2.** M2 is exactly nine rows (10, 13–20) — C1's
`cgoCheckPointer() did not panic` bucket to the row, with row 15's `did not panic` folded in by
mechanism. M1 is seven rows (1, 3, 4, 5, 7, 8, 21) and M3 one (row 9); C1's "nil deref inside `Pin`
8" is those eight — the label is right by mechanism (both are the no-op `Pin`'s consequence: it
neither marks nor checks) even though row 9's first LINE is `did not panic`. M4 is the two keep-
alive/leak rows. 7 + 1 + 9 + 2 = 19; 19 + 2 passing = 21. **One correction to the dispatch text:**
Go has no "double-pin panics" contract — a second `Pin` of the same object sets the multi-pin bit
and counts (`pinner.go:176–186`; `TestPinnerMultiplePinsSame` asserts the counter reads N−1).

## 2. What `Pin`, `Unpin`, `isPinned` and `cgoCheckPointer` MEAN over the CLR heap

Go's `Pinner` makes one promise with two observables. The promise — "not moved or freed until
`Unpin`" — exists so an address can be handed to non-GC-aware code. Over golib, the ADDRESS half
is already unconditional: an address is only ever minted by the `ж<T> → uintptr`/`void*`
conversions, which pin the storage for the box's whole life (`EnsureStableAddress`, `ж.cs:444–451`;
`m_pin` at `ж.cs:63`), and a reachable box is never freed. **That is the half the no-op hand-own
got right, and it is the half no test measures.** The observables are:

| Go observable | Go's representation | managed representation in this design |
|:--|:--|:--|
| the pin BIT — `isPinned(p)` | two bits per object in the span's `pinnerBits`, keyed by **object index** (`span.objIndex(p)`): pinning `&sl[0]` pins the whole backing array, so `isPinned(&sl[1])` and `isPinned(slice.array)` both read true | a pin COUNT keyed by the pointer's **`ReferentObject`** (`INilPointer`, `ж.cs:429`): a standard box → the box; an element ref → its canonical backing (`ж.ElemRefBox.cs:161`); a field ref → its source allocation, recursively (`ж.FieldRefBox.cs:94`). Same equivalence classes as Go's object index — `TestPinnerCgoCheckSlice` (pin `&sl[0]`, check `&sl`) and `TestPinnerInterface` (pin the interface CELL, not the pointee it holds) both depend on exactly this keying |
| the multi-pin COUNTER — `pinnerGetPinCounter(p)` | a `specialPinCounter` record per object holding the ADDITIONAL pins; nil when the object is pinned once | the same count minus one; nil when the count is one or zero — returned as a fresh `ж<uintptr>` snapshot (the test dereferences it immediately; Go hands back a pointer into the special record, and nothing writes through it) |
| the lifetime HOLD | `pinner.refs` holds `unsafe.Pointer`s — GC roots — until `Unpin` clears them | the Pinner holds the REFERENT objects strongly until `Unpin` (Go's `refs`, one level down); no CLR pin is taken (§4) |
| non-Go pointer | `spanOfHeap == nil` → `Pin` silently no-ops, `isPinned` answers **true** (globals, RODATA, stack) | a `NativeBox`, a native-backed slice, or a bare number nothing resolves → `Pin` no-ops, `isPinned` answers true. **The one place this diverges is a string LITERAL** (§6.1): Go's RODATA is not a heap span; golib's literal bytes are a heap array |
| the leak PANIC | a finalizer set once per `pinner`, calling `pinnerLeakPanic` if `refs` is non-empty when the pinner dies | the same finalizer through the hand-owned `runtime.SetFinalizer` bridge, calling the CONVERTED `pinnerLeakPanic` variable (`pinner.cs:336`) so the test's `SetPinnerLeakPanic` swap is observed |
| `Pin` argument validation | `pinnerGetPtr`: nil → panic `argument is nil`; kind not pointer/unsafe.Pointer → panic `argument is not a pointer: <type>`; arena → panic | nil → the same panic; a `ж<T>` or `@unsafe.Pointer` → accepted; anything else → the same panic with `GetGoTypeName` (`builtin.cs:3006`); **plus one accommodation stated in §3.4** (a `uintptr` argument) |
| `cgoCheckPointer(ptr, arg)` | "panics if the argument contains a Go pointer that points to an unpinned Go pointer" — the two-level walk of §3.3, gated on `debug.cgocheck` | the same walk over managed values by their `GoType` structure, gated on `GODEBUG=cgocheck` read directly (§3.5) |

The consequence for the E3 question the dispatch raised: **no Pinner row's subject is the replaced
representation.** Every assertion above is about a bit, a count, a lifetime or a panic — all of
which have a truthful managed form — with the single exception of row 21, whose subject is the
RODATA-versus-heap-span distinction (§6.1). "A test asserting that GC panics on a leaked pin" was
named as structurally untestable in the dispatch; it is not — the bridge runs Go finalizers with
the target resurrected, and `TestPinnerLeakPanics` asks nothing more than that.

## 3. Mechanism

### 3.1 Placement and footprint

One new flat, marked companion — `runtime/pinner_impl.cs`, `[module: GoManualConversion]`, the
`netpoll_impl.cs`/`goenvs_impl.cs` shape — carrying the pin table, the five bodies, and the walk.
The two existing no-op bodies MOVE from `managed_impl.cs` into it (their registry entries are
unchanged), so the whole seam has one scope header, and that header is corrected in the same commit
(the doctrine rule: a scope header that lies reads as the census). **Three registry additions**, all
`goosAny` in `manualConversionFuncs["runtime"]`:

| name | today | why the registry (not a bodyless partial) | principal file |
|:--|:--|:--|:--|
| `isPinned` | bodied, converted | displaced only through the registry | `pinner.cs` (flat) |
| `pinnerGetPinCounter` | bodied, converted | same | `pinner.cs` (flat) |
| `cgoCheckPointer` | bodied, converted | same | `cgocall.cs` — **per-GOOS**, three copies |

A flat companion that DECLARES every suppressed member answers `TestScopedSuppressionsHaveCompanions`
for every target (C2's member-keyed arm, Q44 item 3), which is why the file is flat rather than
routed. `setPinned`, `(*pinner).unpin`, `pinnerGetPtr`, `cgoCheckArg`, `cgoCheckUnknownPointer`
and `cgoIsGoPointer` stay converted and become DEAD — reachable only from the displaced bodies (the
converted `isPinned`'s other readers are `cgocheck.cs:53/191/223`, the `GOEXPERIMENT=cgocheck2`
write-barrier paths behind a constant-false gate, and `cgoCheckArg`, whose only other caller
`cgoCheckResult` has no managed caller) — the `mfinal.cs` "vestigial machinery kept for
compilation" precedent. The hoisted literals `triedToUnpinNonGoPointerˢ`/`runtimePinnerObjectˢ`
belong to `setPinned` and stay; the hand-own spells its own panic texts (the hoist rule: a hand-own
depends on no hoist a displacement can remove).

### 3.2 The pin table and the Pinner's state

- **`PinTable`**: a `ConditionalWeakTable<object, PinRecord>` keyed by `ReferentObject`, `PinRecord`
  holding one `int` count; one lock around count transitions (Go takes `span.speciallock` at the
  same point; pinning is not a hot path and the lock is not this row's subject). A CWT so the table
  is never the reason an allocation stays alive (the token record's LIFETIME rule, applied here) —
  the HOLD lives on the Pinner, exactly as Go's `refs` does.
- **The Pinner**: the converted `Pinner` struct keeps its one embedded `ж<pinner>`; the companion
  adds a partial part to the converted `pinner` struct with one field — the strong list of
  referents (Go's `refs`, one level down; `refStore`'s inline 5-slot optimisation is not
  reproduced). `Pin` allocates the `pinner` box on first use through the generated ref-returning
  embedded-pointer property and registers the leak finalizer ONCE, via `runtime.SetFinalizer` on
  that box with a delegate that takes the box as its argument and captures nothing — the sentinel's
  `DynamicInvoke(target)` (`mfinal.cs:688`) hands it back resurrected, as Go's does. Go's per-P
  `pinnerCache` is not reproduced (there is no P; a fresh `pinner` per Pinner is the cost stated
  in §4).
- **`Unpin`**: decrement each referent's count, remove the record at zero, clear the list. A second
  `Unpin`, or an `Unpin` on a Pinner that never pinned, is a no-op (`TestPinnerEmptyUnpin`).

### 3.3 The cgo argument check — the walk, stated so the cut can be diffed against it

Go's `cgoCheckPointer(ptr, arg)` (`cgocall.go`) resolves to one rule: **every Go pointer WORD found
at level 1 must be pinned, and every Go pointer word found at level 2 must be pinned; level 3 is not
inspected.** Level 1 is the argument's POINTEE (with `arg == true`, the element type's fields; with
`arg == nil`, the pointee walked as an unknown object; with a slice or array `arg`, that container
instead of the pointer); level 2 is, for each level-1 pointer, the pointer words of ITS pointee
(`cgoCheckUnknownPointer`: the object's pointer words, no recursion). `TestPinnerCgoCheckPtr2Pinned2Unpinned`
is the whole rule in one test: `p3 → p2 → p` must have `p2` (level 1) AND `p` (level 2) pinned.

The managed walk is over the pointee's VALUE by its `GoType` structure, with a per-`Type`
"contains pointer words" predicate cached by reflection:

| value kind | Go's arm | managed rule |
|:--|:--|:--|
| `ж<T>` | `abi.Pointer` | nil → ok; `NativeBox` → not a Go pointer, ok; managed → at level > 0 must be pinned, then its pointee's pointer words are the next level |
| `@unsafe.Pointer` | `abi.UnsafePointer` | referent = `RetainedSource` (`unsafe.cs:245`) else `ManagedPointerTokens.Resolve(number)`; nothing → not a Go pointer; else as a pointer, pointee walked by DYNAMIC type |
| `slice<T>` | `abi.Slice` | backing (`m_array`, reachable through golib's existing `InternalsVisibleTo("runtime")`, `ж.cs:16`) nil or native → ok; else must be pinned at level > 0; if `T` has pointer words, each element over `cap` is walked at the same level |
| `@string` | `abi.String` | backing = `unsafe.StringData(s).ReferentObject` (no golib change: `StringData` is `Ꮡ(str.Slice(0, len), 0)`, an element ref sharing the window's backing, `unsafe.cs:946–985`); empty → ok; else must be pinned at level > 0 |
| `any` / interface | `abi.Interface` | null → ok; the boxed value walked by its dynamic type at the SAME level, with the Go rule that the data word itself must be pinned at level > 0 (`TestPinnerCgoCheckInterface`: pin `&o`, check `&ifc`) |
| `channel<T>`, `map<K,V>` | `abi.Chan`, `abi.Map` | panic unconditionally — Go: "never OK to pass them to C" |
| delegate | `abi.Func` | panic — a managed delegate is always a Go pointer here |
| struct (`GoType`) | `abi.Struct` | each field with pointer words walked at the same level |
| `array<T>` | `abi.Array` | each element walked at the same level |
| scalars | `!t.Pointers()` | nothing |

The panic text is Go's own `cgoCheckPointerFail` (a package variable in `cgocall.cs`, not a hoist
of the displaced body — it survives the displacement). Go's `Interface` arm also panics when the
dynamic type descriptor is heap-allocated (`reflect`-made types); managed types never are, so that
arm is a no-op and is SAID to be, not silently dropped.

### 3.4 `Pin`'s argument acceptance, and one converter shape it must tolerate

The corpus's only production-side consumer is `internal/fmtsort`'s test init (`sort_test.cs:269–277`,
a BANKED row, 3 verdicts): Go's `pin.Pin(reflect.ValueOf(cs[i]).UnsafePointer())` is emitted as
`pin.Pin((uintptr)reflect.ValueOf(cs[i]).UnsafePointer())` — the converter wraps an
`unsafe.Pointer`-typed call result in `(uintptr)` on its way into the `any` parameter, so the
hand-own receives a boxed `uintptr`, the `RetainedSource` already dropped. A faithful kind check
would panic `argument is not a pointer: uintptr` and turn a banked row red on the cut's own
account. **Chosen:** `Pin` accepts a `uintptr` as the projected form of a pointer — resolves it
through `ManagedPointerTokens.Resolve`, pins the referent when one answers, no-ops when none does
(a bare number IS a non-Go pointer under Go's own rule). `TestPinnerPinNonPtrPanics` passes an
`int`, not a `uintptr`, so the kind panic it asserts is untouched. The emission itself is a
converter question outside this item, routed as SUGGEST S1 (§9); the accommodation is stated here
so that a later converter fix retiring it is one deleted arm with this paragraph as its reason.

### 3.5 `GODEBUG=cgocheck`

Go's gate is `debug.cgocheck`, defaulted to 1 by `parsedebugvars`. The hand-own does not set the
converted `debug` struct (a `[ModuleInitializer]` writing one field of a struct whose other readers
are init-path-only would be a second policy for one variable): it reads `GODEBUG` once for a
`cgocheck=0` setting and is otherwise ON — Go's default, the default under which every oracle run
of these rows passed. The `cgocheck > 1` mode Go 1.23 rejects at init is not reproduced.

## 4. Cost — sized against the byte-cost rule, and why the pin is NOT on the token

**`ж<T>` gains no instance state: +0 B per box**, asserted by a guard arm and not merely stated.
The pin bit lives beside the object, as Go's own `pinnerBits` live in the span rather than the
object. The other costs, **PROVISIONAL until the cut's guard measures them** (the extrapolation-
in-a-measurement's-voice rule):

| unit | Go | this design (provisional) |
|:--|:--|:--|
| a Pinner that has pinned at least once | one 64-byte `pinner` (5 inline ref slots) | the `pinner` box (~56 B) + one `List<object>` (~32 B + 8 B per pin) + one CWT registration for the leak finalizer via the existing bridge |
| a DISTINCT pinned object | 2 bits in the span's pinner bitmap; +1 special record for multi-pins | one CWT entry (a dependent handle plus a table slot, on the order of 32–48 B) + one 24 B `PinRecord`; no per-multi-pin record |
| a repeat pin of a pinned object | a counter increment | a counter increment |
| an address handed to native code from a pinned box | — | unchanged: the address-take's own pin (the measured ~163 B per distinct pin, gate #1 of the provenance design) — this design adds no second CLR pin |

**No CLR pin is taken by `Pin`.** Address stability is the address-take's contract already
(`EnsureStableAddress` at the pin moment, held for the box's life), and the Pinner's strong hold
keeps that box — and so its pin — alive for exactly the Go-visible pin's duration. A second
`GCHandle` per `Pin` would double the measured per-pin cost for no observable; the warm-design rule
says delete it before it is written.

**The pin does not ride on `ManagedPointerTokens`, for three reasons and one falsifier:**

1. **Lifetime.** The record is weak by ratified rule ("must never be the reason a box stays
   alive"); a pin is a STRONG hold (Go's `refs`). A weak table cannot carry a strong property.
2. **Key.** The record is keyed per PROJECTION (an address or an order token — a standard box, a
   field ref and an element ref of one allocation carry three different keys); Go's pin is per
   ALLOCATION. `TestPinnerCgoCheckSlice` pins `&sl[0]` and checks `slice.array`: only the
   `ReferentObject` key makes those the same entry.
3. **Meaning.** The record's validate-on-read (`IsPinnedAt`, `ж.cs:460`) answers "is this number
   the CLR-pinned address of this box's storage" — a fact about the address-take. Go's `isPinned`
   answers "did a `Pinner` mark this object". Conflating them would make every address ever taken
   read as Pinner-pinned, which launders the cgo check into never firing.

*The falsifier is in the suite:* `TestPinnerSimple` takes `addr := unsafe.Pointer(p)` — emitted
`@unsafe.Pointer.FromPinnedBox(p)` at master (`convCallExpr.go:5965–5976`), which pins and
REGISTERS the address — and then asserts `!IsPinned(addr)` BEFORE any `Pin`. A pin carried by the
record would print `already marked as pinned` on the first line.

**What the record IS used for:** `Resolve`, read-only, in the `@unsafe.Pointer`-with-no-retained-
source and bare-`uintptr` arms of §3.3/§3.4. The design writes nothing into it.

## 5. Coupling to Q44 (and what needs it, which is nothing on this row)

Every `unsafe.Pointer` these 21 rows construct carries its box: the converter emits
`FromPinnedBox` for a pointer operand (retaining, `unsafe.cs:399–404`), and the address-of forms
(`Ꮡifc`, `Ꮡsl`, `Ꮡ(b, 0)`) are boxes outright. So `isPinned`, `Pin` and the walk resolve through
`RetainedSource`/`ReferentObject` and never through a number — **no Pinner row needs Q44's token to
land first**, including `TestPinnerInterface`, whose `&ifc` is a reference-bearing box (the class
Q44 fixes): its `FromPinnedBox` retains the box, so the number's validity is irrelevant here.

Where Q44 does touch this seam: the `(uintptr)` shape of §3.4 resolves a CHANNEL's projected token
only once Q44's mint arm registers reference-bearing tokens; before that the `Resolve` misses and
`Pin` no-ops, which the `fmtsort` row cannot observe either way (nothing asserts the pin). C2's
item-3 finding about cross-typed recovery minting a `NativeBox` over a token is not reachable from
here: this design mints no `ж<T>` from a number.

## 6. Classification — the two rows that do not simply move

### 6.1 `TestPinnerConstStringData` → DISCLOSED, `runtime-capability`, signature `not marked as pinned`

The test's own comment states its subject: *"const string data is not in span."* It asserts that
`unsafe.StringData("test-const-string")` reads `isPinned == true` before, during and after a
`Pin`/`Unpin` — true in Go because RODATA is not a heap span, so `spanOfHeap` is nil and the
non-Go-pointer arm answers pinned. golib has no RODATA: a literal's bytes are a heap `byte[]`
(`(@string)"…"u8` → `AllocationCounter.CopyOf`, `string.cs:92–94`), rooted for the process's life
but movable, and `StringData` returns an element reference into it (`unsafe.cs:946`). The truthful
managed answer for that storage is "a Go heap object, not pinned", and that is the answer the row
will read: `not marked as pinned` on its FIRST assertion, before `Pin` is ever called.

Admission under the class's own test (*does a truthful managed implementation of the asserted
behaviour exist at any cost?*): a form exists in principle — literal storage allocated on the
Pinned Object Heap (never moved, never freed while rooted) plus a golib registry of literal-backed
arrays so `isPinned` can recognise them — and it is REFUSED here as one row's consumer paying a
corpus-wide, unpriced allocation-policy change (every hoisted literal), with no other reader. The
disclosure names that form as its rejoin clause. The manifest entry pins the row AS FAILING on this
one signature; every other assertion in the file — `Pin` and `Unpin` on that pointer not panicking
— stays live, so a regression in the no-op arm still reads as a defect, not as this disclosure.
Nothing about the class is new: `runtime`'s manifest already carries one `runtime-capability` row
(`TestCaller`), and the entry lands beside it.

### 6.2 `TestPinnerCgoCheckString` → REMAINS, blocked on a named sibling defect (not disclosed)

`str := unsafe.String(&b[0], 6)` then `pinner.Pin(&b[0])` then `CgoCheckPointer(&str, true)` passes
in Go because the string's data IS `b`'s backing. golib's `unsafe.String` over a managed byte
pointer COPIES (`unsafe.cs:937–938`: `new @string(new ReadOnlySpan<byte>(pointer, n))`), so the
string's backing is a fresh array, unpinned, and the second check panics unrecovered with
`cgo argument has Go pointer to unpinned Go pointer` — the row's predicted signature after the cut.
This is not a Pinner defect and not disclosable: Go's contract ("the bytes passed to `String` must
not be modified as long as the returned string value exists") makes the aliasing OBSERVABLE through
mutation, so the copy is a semantic divergence with an implementable fix — construct the `@string`
as a WINDOW over the element ref's canonical backing at its absolute index. Priced: the private
`@string(byte[], int, int)` constructor (`string.cs:71`) and `ElemRefBox`'s private canonical pair
(`ж.ElemRefBox.cs:44–50`) would need `internal` accessibility (golib already grants
`InternalsVisibleTo("unsafe")`, `ж.cs:14`) — a two-accessibility golib touch plus one `unsafe.cs`
hunk, which owes the golib gates (`go2cs.slnx` build, CNR) that the Pinner cut alone does not. It
RIDES in the Pinner cut only if the cut owner accepts that scope; otherwise it is SUGGEST S2 (§9)
and the row is carried by name.

### 6.3 The two finalizer rows are NOT `object-lifetime` and NOT `codegen-liveness` — predicted PASS at the configuration of record

`TestPinnerPinKeepsAliveAndReleases` and `TestPinnerLeakPanics` both give the collector a full
second, and the converted `runtime.GC()` drains the Go finalizer queue synchronously with a 10 s
budget (`managed_impl.cs:225–283`, `GoFinalizerQueue.DrainBudgetMs`, `mfinal.cs:573`) — the class's
third clause ("no patience window the test itself provides is sufficient") is not met, so
`object-lifetime` refuses them by its own admission test. The liveness question is the one to
watch, and it is a CONFIGURATION axis: the keep-alive row's emission passes the box through
`p.OrTypedNil()` temporaries; at Release + `TieredCompilation=0` (the validation configuration of
record) a temporary dies at its last use, while a non-optimizing JIT reports every temp live for the
whole method, which would hold the object past `Unpin` and print `Unpin() didn't release object`.
Prediction: PASS at Release+TC0; a Debug-only failure on that exact signature is `codegen-liveness`
by A/B, not a Pinner defect. The leak row has no such exposure — its `pinner` lives only in a
lambda's frame that has RETURNED before `runtime.GC()` runs.

## 7. Prediction on record

Against the 21-row population, scored on the cut's gated re-read (`^TestPinner`, Release+TC0, the
Linux host of record and the Windows flavour, verdicts read from the comparison record):

| outcome | rows | names |
|:--|--:|:--|
| PASS | **19** | rows 1–17, 19, 20 of §1.2 (17 movers + the 2 passing today) |
| DISCLOSED | **1** | `TestPinnerConstStringData` — `runtime-capability`, `not marked as pinned` |
| REMAINS | **1** | `TestPinnerCgoCheckString` — `cgo argument has Go pointer to unpinned Go pointer` at its second check; **PASS instead if §6.2's sibling rides** |

So **N = 17 move, M = 1 disclosed, K = 1 remains (0 with the sibling)**; the family's 19
divergences become 1 (or 0) plus a disclosure. Two side predictions: `internal/fmtsort` (banked, 3)
is **unchanged** under the §3.4 accommodation; CNR is **byte-identical** (no golib change, no
emission change beyond the three placeholders).

**Falsifiers, each naming what it would mean:**

- (a) any cgo-check row failing with the panic at a call the test expects to PASS → the walk
  over-reaches (a level-3 inspection, or a scalar field misread as a pointer word) — a §3.3 error.
- (b) `TestPinnerInterface` printing `marked as pinned` on either negative assertion → the referent
  keying conflates the interface cell with its pointee — a §2 error, back to design.
- (c) `TestPinnerPinKeepsAliveAndReleases` failing at Release+TC0 → a retention this design did not
  model; the Debug/Release one-axis A/B decides between `codegen-liveness` and a hold in the
  hand-own itself (e.g. the finalizer delegate capturing the box).
- (d) `internal/fmtsort` moving → the `uintptr` accommodation is wrong, or `Resolve` answers
  something for a channel token that `Pin` mishandles.
- (e) `TestPinnerPinTinyObj` failing on a counter → the multi-pin count is off by one against Go's
  "additional pins" definition.
- (f) any row differing between the Windows and Linux flavours → the hand-own is not the flat,
  target-neutral file §3.1 claims.

## 8. Acceptance gates the cut owes

1. **Converter suite** — `go test -timeout 30m ./...` from `src/go2cs`: the both-sides ledger
   (`TestManualConversionRegistrationsHaveBodies` sees the three bodies in `pinner_impl.cs`;
   `TestManualConversionRegistrationsDisplaceSomething` needs the three PLACEHOLDERS on disk — so
   the corpus footprint lands in the SAME commit as the registration, never split);
   `TestScopedSuppressionsHaveCompanions` satisfied by the flat companion declaring all three.
2. **Two-seeded emission diff, three targets, hunks predicted:** `runtime/pinner.cs` — two bodies
   to placeholders; `runtime/{windows,linux,darwin}/cgocall.cs` — one body to a placeholder each,
   with the `canTHappenˢ` hoist line possibly relocating (it is shared with `cgoCheckArg`);
   `package_info.cs` ×3 — position-map lines, left to the regen and named in the commit. **Zero
   other lines.** Applied as HUNKS against the committed files (the whole-file trap), byte-identity
   asserted per hunk.
3. **Runtime builds** `-p:GoTargetOS=` windows, linux, darwin — 0 strict errors each;
   `check-solution-integrity.ps1` per target, 0 cycles.
4. **GolibTests**, both configurations, count-matched against the compile set, with the new guard
   (`GolibTests` references `runtime.csproj`, `GolibTests.csproj:116`): (i) pin / count / unpin round
   trip, counters N−1; (ii) referent keying — pin `Ꮡ(sl, 0)`, then an element ref at index 1 and
   the slice's own backing both read pinned; (iii) the two-level walk — `p3 → p2 → p` panics with
   `p` unpinned and passes with both pinned, and the **negative arm** neuters the table (or simply
   unpins) and requires the "passes" assertion to go RED — the arm that proves the gate can fail;
   (iv) a `NativeBox` pointer: `Pin` no-ops, `isPinned` answers true; (v) the leak finalizer fires
   with a non-empty list and not with an empty one, through the real `SetFinalizer` bridge;
   (vi) `ж<T>`'s instance field set is unchanged (the +0 B claim as an assertion); (vii) the
   provisional costs of §4 MEASURED by `AllocationCounter` per distinct pin and per Pinner,
   replacing the table's numbers in the design's §4 addendum.
5. **CNR** — run, not skipped: predicted byte-identical.
6. **`go2cs.slnx` Debug build** — the runtime assembly changed and `GolibTests` is a member;
   run it because the rule is by file, not by signature.
7. **The runtime row re-read**, gated `-test-filter '^TestPinner'`, Release+TC0, on C1's Linux host
   AND the Windows flavour, the tail read FIRST, verdicts from the comparison record; the gated
   run's record files DELETED afterwards (a gated record is poisoned for banking). No bank —
   `runtime` is unbanked and this item does not bank it.
8. **`internal/fmtsort` filtered sweep at the MERGE RESULT** — the Pinner's only other corpus
   consumer, and the canary for §3.4.
9. **The disclosure manifest entry** for row 21 in `runtime/go2cs_test_disclosures.json` with the
   exact signature and the §6.1 reason, beside the existing `TestCaller` entry.
10. Docs: `ConversionStrategies-Reference.md` gains the Pinner entry (a notable hand-own decision:
    the referent-keyed pin bit and the two-level walk), and this file gains a dated §7 scoring
    block — appended, never rewritten.

## 9. SUGGEST items for COORD (outside this item's scope)

- **S1 — converter:** an `unsafe.Pointer`-typed CALL RESULT passed to an `any` parameter is emitted
  wrapped in `(uintptr)` (`internal/fmtsort/sort_test.cs:276` for Go's
  `pin.Pin(reflect.ValueOf(cs[i]).UnsafePointer())`), which drops the pointer's retained source and
  changes the dynamic type a callee's kind switch sees from `unsafe.Pointer` to `uintptr`. Census the
  shape (a call result, versus the `FromPinnedBox` a pointer OPERAND takes at
  `convCallExpr.go:5965`); §3.4's accommodation retires with the fix.
- **S2 — golib/unsafe:** `unsafe.String(ptr, n)` over a managed byte pointer copies where Go
  aliases (`unsafe.cs:938`), observable through mutation of the source bytes; the aliasing form and
  its price are in §6.2. It is the only thing between `TestPinnerCgoCheckString` and PASS.

-- SUB-Q45

---

## 10. The cut, MEASURED (SUB-Q45, 2026-09-04/05) — appended; the sections above are unchanged

Rulings taken at the design's acceptance: S2 became **Q50** (`unsafe.String` aliasing, a separate
item — `TestPinnerCgoCheckString` stays the named REMAINS row, deliberately NOT given a disclosure
class: an implementable defect is not a disclosure by the class bar); S1 folded into C2's **Q49**
bridge class (the `uintptr` accommodation stays, stated in `pinner_impl.cs` with the Q49 reference);
row 21 disclosed as designed; the three registry additions land with their footprint in ONE commit;
the +0 B claim is a GolibTests arm. The cut is the second commit on `claude/sub-q45`.

### 10.1 The footprint, read against §8 gate 2

The two-seeded three-target diff could not be read at the merged seeds: the L3 MERGE refuses at
master `db9e95841` on the pre-existing `runtime/{windows,linux}/trace_impl.cs` divergence (G's Q48,
train 27, is the fix) — master's condition, not the cut's (the cut touches neither file). So the
comparison was taken one step earlier, at the per-target STAGING roots, write-evidence-classified
(only files newer than the seeding sentinel on both arms), CR-stripped:

| target | `pinner.cs` | `<goos>/cgocall.cs` | `<goos>/package_info.cs` | other files |
|:--|:--|:--|:--|:--|
| windows/amd64 | −34 +2 | −57 +4 | −2 +2, 4 of 4 `GoPositionMap` | none |
| linux/amd64 | −34 +2 | −57 +4 | −2 +2, 4 of 4 `GoPositionMap` | none |
| darwin/amd64 | −34 +2 | −57 +4 | −2 +2, 4 of 4 `GoPositionMap` | none |

Against the prediction: the two `pinner.cs` placeholders **as predicted**; `cgocall.cs` is the
body-to-placeholder hunk (−53 +1) PLUS the `canTHappenˢ` hoist relocating to its next first user,
`cgoCheckArg` (−4 +3) — the "possibly relocating" clause **held**; the position-map lines
**as predicted**, left to the regen; zero other lines **as predicted**. Control: the OLD arm's
emission is byte-identical (CR-stripped) to master's committed `pinner.cs` and each `cgocall.cs`, so
the NEW emission is the committed file plus this change's hunks and nothing foreign — which is why
the three `cgocall.cs` were taken from the NEW emission's bytes once the hand application was found
short by exactly the hoist relocation; all four hand-applied files then read IDENTICAL to the
emission. Seeds: 3,714 `.cs` each, both carrying the companion and the placeholders; the OLD run
895 s, the NEW 964 s, `-convert-timeout 90m`, three targets each.

### 10.2 Gates read before the battery's `CHAIN DONE` (allowed by the ruling)

- Converter suite: `ok go2cs 799.119s`, rc 0 — both ledger sides, the companions guard (the flat
  companion declares all five displaced members), metadata in sync.
- `runtime.csproj` on windows, linux and darwin: 0 strict errors each (`--no-incremental` on the
  target switches). `check-solution-integrity.ps1` ×3: 0 cycles across 307 projects, 717
  behavioral projects registered.
- GolibTests, `go2csPath` pinned, count-matched against the compile set (638 declared − 20 in the
  five linux-only files = 618): **Debug 614 passed / 4 skipped / 0 failed of 618; Release+TC0 617 /
  1 / 0 of 618**, both `Test Run Successful`, no abort. The new `RuntimePinnerTests` (10 arms)
  green at both, every "passes" arm followed by the same check with the pin removed going red.
- **Cost, measured by the guard** (`GC.GetAllocatedBytesForCurrentThread`, warm list and table):
  first pin on a fresh Pinner **328 B** (pinner box + list + finalizer registration); a DISTINCT
  pinned object **24 B** (one `PinRecord`; the table entry and list slot amortize); a REPEAT pin
  **0 B**. The first reading was 24 B per repeat pin — one boxed `nuint` from a reflection read of
  `NativeAddress` — replaced by a generic-definition type test (`NativeBox<>` is golib's only
  override), which is what the arm asserting `repeatPin == 0` exists to catch. The +0 B claim is
  `TheBoxGainsNoInstanceState`: `ж<>`'s instance fields are exactly `{m_isNull, m_pin,
  m_publishedArrayBacking}` and `StandardBox<>`'s `{m_slot, m_val}`.

### 10.3 Gates read after `CHAIN DONE`

- **The gated `^TestPinner` re-read, Windows flavour, Release+TC0 — the §7 prediction HELD to the
  row, read TWICE.** First at the design's base `db9e95841` (with the diagnostic patch described
  under the finding below), then on the tree rebased onto train 26's master `dde657009` with NO
  patch, the runtime test assembly building clean (0 strict errors). Both readings identical,
  verdicts from the comparison record (preserved to scratch before each restore; the gated records
  then deleted): Go 21 pass; C# **19 pass**, `TestPinnerCgoCheckString` **fail** with
  `panic: runtime error: cgo argument has Go pointer to unpinned Go pointer` at its SECOND check
  (the §6.2 REMAINS row, signature as predicted — on a train carrying SUB-Q50's cut `e8fcb6703`,
  seated train 28, it is predicted to move to PASS: the design's own "if §6.2's sibling rides"
  line), `TestPinnerConstStringData` **fail** with `not marked as pinned` at `pinner_test.go:527` —
  its FIRST assertion, before `Pin` is called, as §6.1 predicted — **absorbed by the manifest
  entry** (`disclosed: 1`). The results tail carries no timeout event; the two finalizer rows
  passed at 10.06 s each (the finalizer's blocking send parks the runner for `runtime.GC()`'s 10 s
  drain budget — the §6.3 mechanism, observed). N = 17 moved, M = 1 disclosed, K = 1 remains: the
  prediction table's numbers and names, exactly.
- **FINDING outside the prediction, measured and then CLOSED by train 26: at `db9e95841` runtime's
  `-tests` assembly did not compile on this flavour** — `hash_test.cs(540,52): error CS1503: cannot
  convert from 'go.runtime_test_package.IfaceKey_i' to 'go.runtime_package.ifaceHash_i'`, the
  external test's lift of `interface{ F() }` failing to bind to `alg.cs`'s `internal` `ifaceHash_i`
  (the cross-assembly lift-REACHABILITY class). Attributed by two controls before anything was
  claimed: the master converter and the cut's emit a byte-identical `hash_test.cs` (and identical
  `*_test.cs` throughout), so it was not the cut's; and C1's bill tree `44b5089b2` emits the same
  file, so it predated that converter too. The first reading was taken with a DIAGNOSTIC patch on
  the UNTRACKED pipeline output (`IfaceKey.hash()`'s body → `return 0`, a function no `TestPinner*`
  row reaches), stated as such and removed with the emission. Train 26 then landed SUB-Q39's
  lift-dedup fix ("the external test variant adopts production lifts on every target"), and the
  second reading above — no patch, 0 strict errors — is the measurement that it closes this.
- **`internal/fmtsort` filtered sweep (the §3.4 canary): `PASS internal/fmtsort 3 [110s]`**, Release,
  3 of 3 expected verdicts — the banked row is UNCHANGED under the `uintptr` accommodation, as
  predicted. The sweep left the standing re-emission dirt under the package (eight files: the test
  csproj, `package_test_info.cs`, the init-hook and `GoPositionMap` forms, `sort.cs` −12) plus its
  rewritten proof page — the documented classify-and-restore class, none of it the cut's (the cut
  touches nothing in that package); restored by filename, records deleted, emission cleaned.

- **CNR (rebased tree): NO REGRESSION — generated C# and .csproj byte-identical across all 715 behavioral packages, 6 platform-exclusives skipped by name, 0 NOT MEASURED, 2 advisory converter warnings, 1,481 s** — as predicted, no emission change beyond the three placeholders (which live in src/core, outside CNR's behavioral scope).
- **`go2cs.slnx` Debug `--no-incremental`: exit 0 / 0 strict errors / 2201s on the rebased tree (the behavioral COMPILE gate; GolibTests is a member)**.
- The Linux flavour re-read is C1's host and is posted as OWED, not read here.

### 10.4 Rebased onto train 26 (`dde657009`); the final gates re-read there

Train 26 touched none of the cut's files (both commits rebased clean; the placeholders and the three
registry entries verified intact) but changed the converter's lift dedup, so every gate that reads
the converter or the assembly was re-run on the rebased tree: converter suite `ok go2cs 412.392s`;
`runtime.csproj` windows/linux/darwin 0 strict errors; GolibTests count-matched at **630** (train
26's 620 + the ten new arms): Debug 624 passed / 6 skipped / 0 failed, Release+TC0 627 / 3 / 0; and
the §10.3 re-read without the diagnostic patch. The design commit's posted SHA `49ccad282` is
superseded by the rebase (`2e7113fdb`); the form is announce-then-push with `--force-with-lease`.

-- SUB-Q45
