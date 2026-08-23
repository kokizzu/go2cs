# DESIGN — the native-backed `array<T>`: closing the raw-metal fork that fabricates a managed reference

**STATUS: RATIFIED (coordinator, 2026-08-23).** Rulings: **(1)-as-symmetry RATIFIED** — the slice design’s rejection of a second type transfers verbatim, and the m_array census (one hot path, one unsatisfiable escape hatch, one rider) makes array<T> the family’s cheapest conversion; (2) withdrawn by its own proposer on that evidence; (3) stays the constant-index peephole. **The §4 SAFETY FLOOR is ratified on its own merits and lands FIRST** — a named panic at the raw fork ends the next multi-document misattribution in one line; it parks under the freeze like everything but leads the arc’s increments. **⟨OQ-6⟩: panic by name** — a silent copy is the snapshot class’s foothold, the slice arc’s founding lesson; the array-typed subset census is owed before it can fire, as stated. **⟨OQ-4⟩ ANSWERED EARLY, from layout (R, mailbox, 2026-08-23), superseding the scheduled run**: `internal/poll` declares 116 bytes to the kernel over a 40-byte managed `RawSockaddrAny` of reference fields — a completed Windows UDP read overflows the managed object by 76 bytes into the GC heap, and `acceptOne` carries the identical shape. **§4.8 proceeds as ratified — its staging is what recv needs — and this design does NOT unblock recv**: fixing only the byte-view decode would read plausible values off corrupted heap, turning a loud failure silent. The two arcs are independent and parallel. ⟨OQ-G⟩’s send staging stands regardless on the documented contract. **⟨OQ-2⟩: the one-lifetime-story route is ratified WITH an escape-audit obligation** — the native door covers the short-lived in-helper views the corpus exhibits; any site whose view ESCAPES its function is named at implementation as its own question rather than silently pinned indefinitely (pinned-object-heap hostility is the hazard the audit guards). G corrects the record if the slice numbers were leaned on further than they reach; silence by G’s next entry is concurrence.** Originally:** PROPOSED (lane R, 2026-08-23).** Commissioned by the coordinator (mailbox, 2026-08-23)
after this lane measured `(ж<array<T>>)(uintptr)` producing a fabricated managed reference:
*"the DESIGN is yours, the fix waits for it… weighing your (2) against (1)-as-symmetry — the
RATIFIED native-backed `slice<T>` dual-mode is this family's precedent and already MEASURED the
hot-path branch cost that is (1)'s objection, so pull that arc's evidence in rather than
re-arguing it."* Sibling and precedent:
[`DESIGN-native-backed-slice.md`](DESIGN-native-backed-slice.md) (RATIFIED 2026-08-22, lane G).
§7 collects the open questions.

---

## 1. The measured bill

### 1.1 The defect, measured in both regimes

`array<T>` is a MANAGED struct whose first field is a `T[]` reference (`array.cs:46`,
`Backing => m_array ?? []`), and a native-backed `ж<T>` materialises its value with
`Unsafe.AsRef<T>((void*)m_nativeAddr)` (`ж.cs:250`). Composed, `(ж<array<byte>>)(uintptr)(…)`
**reinterprets whatever bytes live at that address as a managed array reference and dereferences
it.** Measured in GolibTests against golib directly — no kernel, no socket, no async:

| memory at the address | result | reading |
|:--|:--|:--|
| zeroed | `Length=0` — the reference reads null, `?? []` yields the empty array | a SILENT WRONG ANSWER |
| filled `0xAB` | `Length=-1414812757` — i.e. `0xABABABAB` | it **fabricated a managed reference out of the data bytes and dereferenced it**, returning a number instead of faulting BY LUCK |

The second row is the finding. This is a type-safety hole, not a wrong result: any real data — a
filled sockaddr, a `siginfo`, a register block — takes that path, and the CLR is handed a pointer
the program invented.

### 1.2 It is a KNOWN fork, and that is the useful part

golib says so itself, in `array<T>.AliasPointer`'s remarks:

> *"A pointer with no managed element storage behind it — a heap box, a struct field, a native
> address — keeps the raw-address route: no `T[]` exists to window, and an `array<T>` can neither
> view native memory nor be fabricated from a scalar's bytes. **That is the raw-metal fork,
> unchanged here.**"*

And the converter says so, in `arrayPointerAliasEmission`'s header, naming the exact mechanism:

> *"the address route… builds a native-backed `ж<array<T>>` whose deref reads an `array<T>` STRUCT
> (a backing reference plus bounds) out of the pointed-at DATA, i.e. **a fabricated managed
> reference**"*

**So nothing here is newly discovered except the SEVERITY.** The fork was documented as
*unsupported*; the measurement shows it is *unsafe* — it does not decline, it invents a reference.
That distinction is this design's whole reason to exist, and it is why the safety floor (§4) is
separable from the representation fix (§3).

### 1.3 Half the class is already solved, and by the shape this design should copy

`arrayPointerAliasEmission` already handles `(*[N]T)(unsafe.Pointer(p))` **when `p`'s element type
equals `T`**, emitting `array<T>.AliasPointer(p, N)` — carrying the length, windowing real managed
storage, and falling back at RUNTIME when the pointer has no element storage. The gap is precisely
the **differently-typed** conversion, where the target array's element type is not the source
pointer's:

```go
p := (*[2]byte)(unsafe.Pointer(&pp.Port))   // Port is uint16; target elem is byte
```

`types.Identical(srcPtr.Elem(), targetArr.Elem())` is false, the alias arm declines, and the raw
route runs. **The length is NOT the blocker** — I said it was in my first report and that was
wrong: Go's `*[2]byte` carries N in the type and the converter already has it (`csNintLiteral`
exists to render exactly this). The blocker is that `array<T>` has nowhere to put an ADDRESS.

### 1.4 Census: 61 sites

`(ж<array<…>>)(uintptr)` across `src/core`:

| package | sites |
|:--|--:|
| `runtime` | 17 |
| `runtime/linux` | 10 |
| `syscall/darwin` | 8 |
| `syscall/linux` | 6 |
| `runtime/darwin` | 5 |
| `internal/poll/windows` | 4 |
| `runtime/windows` | 3 |
| `reflect` | 2 |
| `net/darwin` | 2 |
| `internal/syscall/windows/registry/windows` | 2 |
| `vendor/…/route/darwin`, `vendor/…/sha3` | 1 each |

### 1.5 The liveness audit — latent, with a live trigger

The raw count reads alarmingly (35 in `runtime`); the audit says otherwise, and the honest framing
matters more than the number.

The `runtime` sites sit in `mapaccess1_faststr`, `mapaccess2_faststr`, `c64hash`, `c128hash`,
`memequal128`, `readUnaligned32/64`, `initAlgAES`, `cheaprand`, `runfinq`, `printArgs`, `pkgPath` —
**the hottest paths in Go's runtime. If they were live and broken, nothing would run at all.** They
are converted-but-inert: golib implements maps (`map<K,V>`) and hashing natively, so `map_faststr.cs`
and most of `alg.cs` are faithful conversions nothing calls. Reachability spot-check: `memequal128`
**0** call sites, `mapaccess1_faststr` **1** (inside `runtime` itself), `readUnaligned64` **2**.

**The empirical argument is the strong one.** A zeroed-memory hit panics immediately and visibly —
exactly how the netpoll recv presented — and the roster is green at 146 packages / 18,569 verdicts.
No live path on today's roster reaches any of these sites.

So the class's true shape is **latent, with a live trigger**:

- 61 sites are wrong-by-construction and dormant because nothing reaches them.
- A site goes live the moment a new code path is reached — which is what the Phase-4 campaign does,
  continuously and by design.
- **The netpoll recv is arrival number one**, and it arrived because this lane's own §4.7 send fix
  un-hid it — the `LocalTimeZone` pattern one level down.
- Each future arrival presents as a plausible panic several layers from the cause. That
  re-diagnosis cost, not today's blast radius, is the argument for fixing rather than routing
  around.

`cheaprand` (`rand.cs:219`, 32 in-`runtime` callers) is the one site I would not certify by
inspection alone; that the corpus runs is the evidence it is unreached.

---

## 2. What the precedent already settles

`DESIGN-native-backed-slice.md` is the same problem one container over, and it is RATIFIED. Three of
its rulings transfer without re-argument:

1. **The dual-mode shape.** `ж<T>` carries `m_nativeAddr` beside its managed modes; `slice<T>` gained
   `m_nativeBase` with `0` as the discriminant. `array<T>` is the third member of the family and the
   only one still forked.
2. **A separate native type was REJECTED, for reasons that apply verbatim here.** §3 of that design:
   *"The converter emits `slice<T>` at every Go `[]T` site; a second type either forks the emitted
   surface (converter + corpus churn across everything) or hides behind an interface (boxing golib's
   hottest type). The polymorphism must live INSIDE the struct."* Substitute `array<T>`/`[N]T` and
   the sentence is unchanged. **This overturns my own first recommendation** — I proposed a distinct
   `NativeArray<T>` before reading the sibling arc, and I withdraw it (§6).
3. **The unmanaged-`T` precondition.** `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` is a
   JIT-time constant, so the guard compiles away on every managed construction and folds to a
   constant on the native door. It states the SiginfoChild class as a precondition instead of
   discovering it as heap corruption — and here it is doubly apt, since fabricating a reference is
   the exact failure being closed.

What does NOT transfer: the slice arc's OQ-4 (*"converter involvement: none expected"*). This
design **requires** a converter emission change, because the native door needs a length the current
raw-cast emission drops (§3.2).

---

## 3. The design

### 3.1 `array<T>` grows a native backing, symmetric with its two siblings

```
internal readonly T[]  m_array;       // managed backing (empty when native)
private  readonly int  m_low;         // window start — INDEX for managed, ELEMENT OFFSET for native
private  readonly int  m_length;
private  readonly nuint m_nativeBase; // NEW: 0 = managed; else the base address
```

One added word. `m_nativeBase == 0` is the discriminant; element `i` lives at
`m_nativeBase + (m_low + i) * sizeof(T)`. The existing window fields already exist for exactly this
family of aliasing conversions (`array.cs:48–55`), so the native mode reuses them rather than adding
a second addressing scheme.

**Only unmanaged `T`**, enforced at the creation door with a named panic, per §2(3).

### 3.2 The creation door, and the converter change it needs

One door, mirroring `AliasPointer`:

```csharp
// Go: (*[N]T)(unsafe.Pointer(p)) where p's element type is NOT T.
public static ж<array<T>> ViewPointer(ж<TSrc>? p, nint length);
```

`arrayPointerAliasEmission` currently declines when
`!types.Identical(srcPtr.Elem(), targetArr.Elem())` and lets the raw cast run. It should instead
emit the view form, with the length it already computes (`csNintLiteral` is written for this). The
same-element-type arm is unchanged: `AliasPointer` stays the better answer there because it can
window real managed storage.

**Runtime disposition inside the door**, in this order:

1. The pointer addresses **managed element storage** of the same type → window it, as `AliasPointer`
   does today.
2. The pointer addresses **managed storage of a different type** (our case: a `uint16` field viewed
   as `[2]byte`) → this is the interesting one, and §7's ⟨OQ-2⟩ asks how far to go. The bytes are
   real and managed; a `Span<byte>` over the pinned field is faithful, and the pin must outlive the
   view.
3. The pointer is **genuinely native** → native-backed `array<T>` over the address.
4. `T` is managed, or the length is negative → **named panic**, never a fabricated reference.

### 3.3 Operation semantics

The slice design's table applies with one container's worth of translation; the short version:
indexer reads/writes go through the computed address, `len` is `m_length`, `Ꮡ(a, i)` yields a
native-backed `ж<T>` at the element address (the existing `ж(nuint)` ctor), bulk paths go
span-first via an `AsSpan()` that pays the discriminant once per operation rather than per element
(§2.4 of the sibling design), and `Clone()`/conversions **copy out** — as they already do — because
Go's array assignment is a copy by contract.

`Reinterpret`-style members whose math is `sizeof`-scaled keep working over a byte base. Any member
reaching `m_array` directly must take the span path or panic named; §6 makes that census a gate,
exactly as the sibling arc did.

---

## 4. The safety floor — separable, and landable before the representation

**Whatever is ruled for §3, the raw-address route must stop fabricating references.** Today it
silently invents one; a named panic converts a memory-safety hole into a diagnostic that names its
own cause, at the cost of turning latent-silent into latent-loud.

This is worth stating as its own tier because:

- It is **small and self-contained** — a guard where the native-backed `ж<array<T>>` materialises.
- It is **strictly safer** than today under every reading, and it cannot regress a working path:
  by §1.5 no live path reaches these sites, so nothing on the roster can start panicking.
- It makes every FUTURE arrival cheap to diagnose. The netpoll recv cost this lane a full
  misattribution — through a design, a ratification and four documents — because the failure
  presented as a plausible panic in unrelated code. A named panic at the fork would have ended that
  in one line.
- It is honest about what the corpus supports. The project's own doctrine is that *a stub that
  compiles is an acceptable milestone solution*; a fork that panics by name is the same principle
  applied to a hazard.

Recommended message shape: name the conversion, the element type and the fact that the pointer has
no managed element storage — enough that the reader reaches this document rather than the panic
site.

---

## 5. Rejected alternatives, on the record

- **A distinct `NativeArray<T>` view type (my own first proposal).** Withdrawn on the sibling
  design's §3 reasoning: it forks the emitted surface or boxes golib's hot type. Recorded so it is
  not re-proposed.
- **Pointer arithmetic per site (my option 3).** Only covers sites indexing with constants, and
  several of the 61 pass the view onward. **Rejected as primary, retained as a peephole**: under
  either §3 or §4, a constant-index site could still emit direct offset reads and skip the view
  entirely. Cheapest possible answer for the common `p[0]`/`p[1]` shape.
- **Write-back snapshots.** The sibling design's refutation applies unchanged: there is no "when" to
  write back, and the kernel observes the memory continuously.
- **Do nothing.** Today's blast radius is genuinely NONE (§1.5), so this is defensible on today's
  evidence — and it is what the codebase has done, deliberately, with the fork documented. It stops
  being defensible the moment a second arrival lands, and arrivals are what Phase 4 manufactures.

---

## 6. Gates (spec'd here, run by the implementation lane)

1. **The `m_array` touch census** — every member of `array.cs` and every golib helper reading
   `m_array` via `internal`, enumerated and dispositioned span-path / arithmetic-safe / named-panic
   **before the first commit**. The sibling arc made this its first commit; the SiginfoChild lesson
   applied preventively.

   **Run already, because it sizes the whole proposal** (2026-08-23): **31 `m_array` touches in
   `array.cs`**, and the blast radius is much smaller than the slice arc's — the great majority are
   CONSTRUCTORS (`m_array = …`, `m_length = m_array.Length`), which are the managed path and change
   not at all. Only four places need a real disposition:

   | member | line | disposition |
   |:--|--:|:--|
   | `private T[] Backing => m_array ?? []` | 271 | **THE hot read path** — every indexer, enumerator and bulk helper funnels through it. Becomes the span/discriminant seam; this is where §3.3's "pay the discriminant once per operation" lives. |
   | `public T[] Source => m_array` | 257 | ⚠ **the one member whose contract cannot be honored natively** — it hands out the backing `T[]`, and a native-backed array has none. See ⟨OQ-6⟩. |
   | `Array IArray.Source => m_array` | 604 | the interface form of the same problem, same ruling. |
   | `builtin.GoZero` fill | 419 | writes a fresh managed array but READS `Backing[m_low + i]`, so it rides on `Backing`'s disposition. |

   `Alias(slice<T>, length)` (166) reads `source.m_array` and stays managed-only by construction.
   That is the entire census: **one hot path, one escape hatch (twice), one rider.** For comparison
   the sibling arc's equivalent census drove its whole first commit — `array<T>` is the cheaper
   member of the family to convert, which is itself an argument for (1)-as-symmetry over any new
   type.
2. **A GolibTests family** (`NativeArrayViewTests`): both regimes from §1.1 as explicit assertions
   (zeroed → correct length and contents, not `0`; `0xAB`-filled → real bytes, not a fabricated
   reference); index read/write reaching real memory verified through a second view; the
   differently-typed managed case (`uint16` field viewed as `[2]byte`) reading and WRITING the
   field's real bytes; `Ꮡ(a, i)` address exactness; the unmanaged-`T` named panic; and the
   same-element-type `AliasPointer` path UNCHANGED (regression guard for the half already working).
3. **The §4 floor has its own test** even if §3 lands with it: a pointer with no managed element
   storage panics by name rather than returning anything.
4. **Perf, measured not asserted** — the sibling arc's objection applies to `array<T>` too.
   `PerfSieve`/`PerfString`/`PerfMatMul` A/B at the merge tip within noise, plus a GolibTests
   microbench bound on the managed-path indexer.
   **Precision, from the slice arc's own author (G, mailbox concurrence, 2026-08-23), amending
   what the cited 30% means:** (a) the figure measured an INLINE-BUDGET failure, not a branch
   budget — the native branch and its `unsafe` block placed INLINE cost PerfSieve +30% because
   the indexer stopped being inlinable; the remedy was `[MethodImpl(NoInlining)]` on the
   out-of-line slow path. **The transferable lesson is the SHAPE, not the affordability** — if
   `array<T>`'s indexer takes the branch inline, expect the same 30% and this gate will find
   it. (b) "Within noise" requires a NAMED CONTROL ROW reported beside the treatment rows (the
   control-row-first doctrine): this laptop class measured a +5.0% control / +17% same-binary
   drift floor on 2026-08-23, and an indexer change reaches EVERY benchmark, so the control
   must be a row the change provably does not touch. (c) The 30% was measured on the dead
   perf-canon i9 — it is a RATIO ANCHOR, never a threshold for another host; re-measure paired,
   same session. (d) Where the claim is "no allocation was added", gate by COUNT
   (`AllocationCounter`, deterministic) — a count cannot be swallowed by a 17% noise floor.
5. **The standing envelope**: GolibTests full, behavioral suite full, CNR (a converter emission
   change IS expected here — CNR measures its footprint), Windows stdlib build, `-p:GoTargetOS=linux`
   build, and the i9 control per the usual dispatch.
6. **The §4.8-fate measurement, which this design owes** (see §7 ⟨OQ-4⟩).

---

## 7. Open questions, each with a recommendation

* **⟨OQ-1⟩ — one tier or two?** Land §4's named panic first as its own small change, or only as part
  of §3? *Recommendation:* **two tiers, floor first.** It is small, strictly safer, cannot regress a
  live path, and every day it is in place is a future arrival diagnosed in one line instead of
  through a misattribution. It also de-risks §3 by making the fork loud while the representation is
  being built.
* **⟨OQ-2⟩ — how far does the differently-typed MANAGED case go?** §3.2(2): a `uint16` field viewed
  as `[2]byte` is managed storage the view must both read and WRITE, with a pin outliving the view.
  Options: pin-and-view (faithful, but golib's pinning story for interior fields is the one `ж.cs`
  calls *"a transient address"*), or route it through the native door by taking the pinned address
  (simpler, and the pin becomes the door's problem). *Recommendation:* **the native door**, because
  it gives ONE path to audit for lifetime instead of two — but this is the question I am least
  certain of, and it is where the netpoll recv actually lives.
* **⟨OQ-3⟩ — scope of the first landing.** All 61 sites are affected by the representation, but only
  the emission change decides which SPELL differently. *Recommendation:* **land the representation
  plus the emission change wholesale** (they are one semantic unit) and let CNR measure the corpus
  footprint; do not stage by package.
* **⟨OQ-4⟩ — the §4.8 fate measurement, which this design owes.** With the byte-view fixed, whether
  Windows recv needs §4.8's staging seam is a ONE-RUN answer: re-run `UdpLoopbackRoundTrip` on
  Windows and read whether the decoded sockaddr is correct. Correct ⇒ the kernel does fill a managed
  `ж<RawSockaddrAny>` and §4.8's seam is unnecessary for recv (⟨OQ-G⟩'s send staging stands
  regardless, on the documented contract). Wrong-but-not-crashing ⇒ the struct-passing class is real
  here after all and §4.8 proceeds as ratified. *Recommendation:* **run it in this arc's gate pass
  and report the answer to the §4.8 owner** — it is one run and it retires or confirms a ratified
  design.
* **⟨OQ-6⟩ — what does `Source` do on a native-backed array?** The census (§6(1)) makes this the one
  member whose contract is unsatisfiable: it returns the backing `T[]`, and there is none. Options:
  **materialize a copy** (safe, silent, and wrong for any caller expecting aliasing — the exact
  snapshot bug the slice arc existed to kill), or **panic by name** (loud, honest, and it may fire
  on a path that only wanted to read). *Recommendation:* **panic by name**, on the same reasoning as
  §4 — a native array reaching a `T[]`-shaped consumer is a real mismatch, and silently copying it
  is how the snapshot class got its foothold in the first place. The unfiltered `.Source` reference
  count outside golib is large (582, receiver-type unseparated), so the implementation census owes
  the array-typed subset before this fires.
* **⟨OQ-5⟩ — who implements.** *Recommendation:* **a golib lane with the full envelope**, sized like
  the sibling arc, with §6(1)'s census as the first commit. This lane (R) holds the netpoll context
  and the measurements; either is defensible, and I have no preference beyond not splitting the
  §4.8 fate question away from whoever runs ⟨OQ-4⟩.

---

*Prepared by lane R (`claude/native-array-view-design`), 2026-08-23. For ratification: §3's shape
(the dual-mode array symmetric with `ж<T>`/`slice<T>`, one creation door, the converter emission
change), §4's separable safety floor, and §6's gate ladder. Evidence pulled from
`DESIGN-native-backed-slice.md` per the commission rather than re-argued; lane G cc'd as that
design's owner for the measured branch-cost numbers.*
