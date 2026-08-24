# DESIGN — pointer PROVENANCE: teaching `ж<T>` which kind of address it holds

**STATUS: RATIFIED (coordinator, 2026-08-23). ⟨OQ-P1⟩ the table, ⟨OQ-P3⟩ its own increment sequenced next, ⟨OQ-P4⟩ census-first chooses — all per recommendation. **⟨OQ-P2⟩ RATIFIED WITH A REFINEMENT that closes the ABA window structurally**: weak entries dying with their box (your CWT tie), OVERWRITE-on-register (the latest pin owns the address, so same-type reuse is benign — every pin re-registers), and read = the recovered box is ALIVE and its CURRENT pinned address equals the queried one, else MISS. With validate-on-read in place the (address, type) key adds nothing — key by address alone. The residual window closes on liveness itself: a live box whose pinned address still matches genuinely occupies that storage, so no native allocation can coexist there. Gate #2 (the slice liveness audit) and gate #1 (the census, already probing) precede the mechanism as §5 orders-view SAFETY FLOOR was
withdrawn on measurement: *"the PROVENANCE amendment is the arc's next increment, yours,
design-first: extend the `ManagedPointerTokens` record to pinned-managed addresses; the per-pin
registration cost is MEASURED … and the amendment must also state how the slice dual-mode handles
this same ambiguity today."* Parent: [`DESIGN-native-array-view.md`](DESIGN-native-array-view.md)
(RATIFIED; its §4 floor withdrawn at master `8f7cf67cc`). Sibling:
[`DESIGN-native-backed-slice.md`](DESIGN-native-backed-slice.md) (RATIFIED, LANDED). §6 collects the
open questions.

---

## 1. What the withdrawn floor proved

The floor was a named panic at `ж<T>`'s native fork, thrown when `T` was managed-shaped. It was
built, gated (**GolibTests 292/292**, including three tests asserting both measured regimes), and
run against the full behavioral suite: **609/609 transpile, compile and golden — and 6 Output
failures.** An A/B against master confirmed the failures were the floor's.

**All six were correct code.** The panic named the type in each case, and they fall into three
classes:

| class | firing type | why the fork is legitimate there |
|:--|:--|:--|
| pinned MANAGED address | `main.Row` | `(uintptr)` pins the storage and returns a REAL address; converting back reinterprets the very same object. **Go requires this round-trip.** |
| POINTER-shaped `T` | `unsafe.Pointer`, `ж<array<int64>>` | storing a managed pointer through a native-looking address is how `**T` shapes work here |
| CONTAINER over pinned managed storage | `array<uintptr>` | `*(*[2]uintptr)(p)` — the defect's own shape, passing at master |

The third row is the decisive one. Narrowing the floor from "reference-bearing `T`" to "`T` is
`array<U>`" — precisely the measured defect class — **still fires** on

```go
func castDerefReturnDirect(p unsafe.Pointer) [2]uintptr {
	return *(*[2]uintptr)(p)          // emits (ж<array<uintptr>>)(uintptr)
}
```

which passes at master because there the address is pinned managed storage.

**So no test on `T` can work.** The question "is this reinterpret sound?" is not a question about
the TYPE at all — it is a question about where the ADDRESS CAME FROM, and `ж<T>` does not record
that. A pinned-managed address and a kernel address arrive in the same `m_nativeAddr` field,
indistinguishable.

## 2. The three address kinds, and what tells them apart today

| kind | how it arises | correct behavior at the fork | recorded today? |
|:--|:--|:--|:--|
| **native** | `NativeMemory.Alloc`, a kernel buffer, `stackalloc`, an mmap | reinterpret is sound for unmanaged `T`, NONSENSE for managed-shaped `T` | — |
| **pinned-managed** | `(uintptr)`/`(void*)` on a managed box: `EnsureStableAddress()` then `fixed` | reinterpret is sound and REQUIRED — it names the same object | **no** |
| **order token** | a reflect projection of a pointer with no machine address | must resolve back to the box | **yes** — `ManagedPointerTokens` |

The third kind is already solved, and by exactly the mechanism the second one needs. `ж.PointerTokens.cs`
exists because *"converting the scalar back to a pointer and dereferencing it"* had to work for
managed storage; the amendment is the observation that a PINNED address is the same question with a
real number instead of a synthetic one.

## 3. ⚠ The slice dual-mode carries the identical ambiguity — and adds a lifetime hazard

Commissioned explicitly, and the answer is yes.

`unsafe.Slice(ptr, n)` (`core/unsafe/unsafe.cs:652`) selects its native arm on **`ptr.IsNative`**,
which is `m_nativeAddr != 0` — the very field §1 just proved carries pinned-managed addresses. So

```go
unsafe.Slice((*T)(unsafe.Pointer(p)), n)     // p a pinned managed address
```

yields a **native-backed slice over MANAGED storage**. `OverNativeMemory`'s guard does not catch it:
that guard tests the ELEMENT type (`IsReferenceOrContainsReferences<T>`), which is a different
question — `slice<byte>` over a pinned managed `[N]byte` passes it and takes the native arm.

**And the slice is worse off than the array class, because it drops the pin.** The native ctor keeps
`m_nativeBase` — a bare `nuint` — plus an empty `m_array`; it retains nothing that holds the object
still. The pin lived in the `ж<T>` box that produced the address (`EnsureStableAddress`), and the
slice does not hold that box. When the box becomes unreachable the pin can release, the GC can
compact, and the slice keeps reading and WRITING the old address. `slice.cs`'s own comment says
*"lifetime is the mapping's own"* — true for a genuine mapping, and false for a pinned managed
object.

**This is stated, not measured as live.** I have not audited whether any current call site reaches
`unsafe.Slice` with a pinned-managed pointer, and after the floor I am not asserting a liveness claim
I have not run. §5 makes that audit a gate rather than a footnote. The structural point stands
either way: the ratified slice mode rests on the same undistinguished field, so provenance is not
only the array arc's prerequisite — it is a correctness precondition for a mode that has already
LANDED.

## 4. The proposal

**Record provenance at the moment it is known — the pin — and read it back at the fork.**

The forward conversions (`operator uintptr`, `operator void*`) are the only places that turn managed
storage into a number, and they already call `EnsureStableAddress()` there. That is the registration
point: address → box, in the `ManagedPointerTokens` table that already exists for order tokens.

The reverse conversion already consults that table (`Resolve(...) is ж<T>`) and falls through to a
native box when it misses. With pinned addresses registered, the miss becomes MEANINGFUL: it is the
positive statement *"this address is not managed storage this process pinned"*, which is exactly the
predicate the floor needed and could not express.

Three consumers, in the order they would land:

1. **`ж<T>`'s fork** — the withdrawn floor, now expressible: panic only when the address is unregistered AND `T` is managed-shaped.
2. **`unsafe.Slice`'s arm selection** — take the native arm only for unregistered addresses; a registered one takes the managed-aliasing arm it should have taken all along, which also restores the pin.
3. **⟨OQ-2⟩ of the parent design** — *"how far does the differently-typed MANAGED case go?"* It is answered rather than decided: a differently-typed view over a REGISTERED address is the managed case and windows the storage; over an unregistered one it is the native case. The question only looked open because provenance was missing.

### 4.1 The measured cost

Counting doctrine — allocations per operation, no timing claims. Measured against the existing
`ManagedPointerTokens.Register` (the mechanism being extended), `GC.GetAllocatedBytesForCurrentThread`:

| operation | bytes |
|:--|--:|
| 10,000 registrations of an ALREADY-registered (address, box) | **88 total** (~0.009/op — the documented no-allocation fast path) |
| 10,000 registrations of DISTINCT addresses | **1,625,680** → **~163 bytes each** |
| first call, including one-time warmup | 7,664 (not a per-op figure) |

So the price is **~163 bytes per distinct pin, and effectively nothing per repeat.** The steady state
is free because `Register` returns early when the pair is already remembered — a fast path written
for `fmt`'s `%p` and equally load-bearing here.

**What that does NOT settle** is how many DISTINCT pins a real run mints. A syscall wrapper pinning a
fresh buffer per call is a new address each time, and 163 bytes/pin on such a path is a real cost
rather than a rounding error. §5 makes that a measured gate, not an argument.

## 5. Gates

1. **The distinct-pin census, measured before the mechanism lands**: instrument the forward
   conversions, run the behavioral suite and one sweep row, and report DISTINCT vs REPEAT pin counts.
   This is the number that decides whether registration is free or needs a cheaper record.
2. **The `unsafe.Slice` liveness audit** (§3): does any live call site reach the native arm with a
   pinned-managed pointer? Stated as unknown here; a gate, not a footnote.
3. **The floor, re-attempted on provenance** — the three classes of §1 must all pass, and the two
   measured regimes must still panic. The withdrawn floor's GolibTests are the ready-made assertions.
4. **The standing envelope**: GolibTests, full behavioral suite (the instrument that caught the
   floor), CNR, Windows and Linux stdlib builds, and the perf trio with a named control row per G's
   correction.

## 6. Open questions

* **⟨OQ-P1⟩ — is a table the right record, or should the box carry it?** A registered address costs a
  lookup at every fork. The alternative is a bit on `ж<T>` itself (`m_nativeAddr` plus
  `m_addressIsPinnedManaged`), which is free to read but only knowable when the BOX was created —
  and the reverse conversion creates the box from a bare number, which is precisely when it does not
  know. *Recommendation:* **the table**, because the number is all the reverse conversion has.
* **⟨OQ-P2⟩ — what is the lifetime of a pinned-address registration?** The order-token table uses
  `WeakReference` + a `ConditionalWeakTable` tie so entries die with their box. A pinned address is
  only valid while pinned, so the same tie is right — but an address can be REUSED by a later pin
  after the first box dies, and a stale entry would then answer for the wrong object.
  *Recommendation:* key the entry by (address, type) and validate the recovered box's address on
  read; **this is the question I am least sure of** and it is where a wrong answer becomes a silent
  aliasing bug rather than a panic.
* **⟨OQ-P3⟩ — does the slice fix ride this increment or its own?** §3's hazard is in LANDED code.
  *Recommendation:* **its own increment, immediately after**, so the provenance mechanism is gated on
  its own before a second consumer depends on it — but sequenced next, not queued behind the array
  emission work.
* **⟨OQ-P4⟩ — what if the census (§5.1) says distinct pins are hot?** Then registration is too
  expensive as designed and the record has to shrink (an address-range set rather than a per-pin
  entry, say). *Recommendation:* run the census FIRST and let it choose; do not build the table and
  then discover the bill.

---

*Prepared by lane R (`claude/provenance-amendment`), 2026-08-23, against master `8f7cf67cc`. For
ratification: §4's registration-at-the-pin shape, §3's slice finding and its consequence for a
landed mode, §5.1's census-before-mechanism ordering, and the four open questions.*
