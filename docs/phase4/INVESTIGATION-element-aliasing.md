# Element-aliasing investigation — the bcache concurrent-Put entry loss

**Lane:** `claude/local-element-aliasing-probe` (investigation only — no fix banked)
**Machine:** i7-5820K coordinator, Windows 11, .NET 10.0.400, go1.23.12
**Tree:** detached at `2de8d7394` (master tip; carries `e3005df96`, the bcache `registerCache` wiring)
**Date:** 2026-08-30

---

## 0. Verdict

**Rooted, measured, and reproduced in isolation.** The defect is **not** in the CAS, not in
`ElemRefBox` canonicalization, not in GC relocation, and not GC-correlated at all. It is an
**unsynchronized lazy-initialization lost update** in `ж<T>.at<Telem>()`:

```csharp
// src/core/golib/ж.cs
private void ensureArrayBacking(ref T value)
{
    if (typeof(T).IsValueType && value is IArray boxedView)   // (1) BOX A COPY of the shared struct
    {
        _ = boxedView.Source;                                 // (2) materialize the lazy backing ON THE COPY
        value = (T)(object)boxedView;                         // (3) WRITE THE COPY BACK over shared storage
    }
}

public ж<Telem> at<Telem>(nint index)
{
    ensureArrayBacking(ref Value);
    if (Value is not IArray<Telem> array) throw …;
    return new ElemRefBox<Telem>(array, (int)index);
}
```

Steps (1)–(3) are a read-modify-write of shared mutable state with no synchronization. Two threads
that both reach (1) while the backing is still unmaterialized each allocate **their own** backing
array at (2), and the second write-back at (3) **silently discards the first** — together with every
element already written into it. The element pointers already handed out keep pointing at the
orphaned array, so their writes land somewhere nothing will ever read again.

The lazy backing this races over is emitted by go2cs-gen for every **named fixed-size Go array type**
(`InheritedTypeTemplate.ValueGetter`, `TypeClass == "Array"`):

```csharp
// Generated/go2cs-gen/go2cs.TypeGenerator/…cacheTable_K, V_.g.cs
private array<sync.atomic_package.Pointer<cacheEntry<K, V>>>? m_value;
public  array<sync.atomic_package.Pointer<cacheEntry<K, V>>>  Value => m_value ??= new array<…>(1021);
public  sync.atomic_package.Pointer<cacheEntry<K, V>>[]       Source => Value;
```

**This is the concurrency residue of a defect that was already found and fixed once, single-threaded.**
Commit `47ddd5a50` ("Converter+golib: pointer-to-named-array access family") says so in its own words:

> golib (the real find): `ж.at<Telem>`'s `val is IArray<Telem>` pattern COPIES the wrapper before its
> lazy Array-class backing materializes — allocation landed on the copy and the real storage stayed
> virgin, silently dropping every write through the element box (the pallocBits lesson at the
> box-element seam).

`ensureArrayBacking` **is** that fix: it added the write-back at (3) so the shared storage stops
staying virgin. What it did not do is make the sequence atomic. Single-threaded it is correct;
with two threads it loses writes.

**A second, strictly worse instance of the same class is still unfixed and needs no concurrency at
all** — see §5.2 (`runtime/sema.cs`).

---

## 1. Reproduction on this tree

`src\go2cs\bin\go2cs.exe -tests -test-action all|compare -test-timeout 15m -go2cspath <wt>\src <GOROOT>\src\crypto\internal\boring\bcache <wt>\src\core\…\bcache`

### 1.1 The arithmetic

| Batch | golib | Environment | Runs | Pass | Fail |
|---|---|---|---|---|---|
| A | unmodified | default | 11 (1 `all` + 10 `compare`) | 9 | 2 |
| B | unmodified | `DOTNET_GCgen0size=0x40000000` | 10 `compare` | 8 | 2 |
| D | unmodified (post-revert) | default | 15 (1 `all` + 14 `compare`) | 9 | 6 |
| **A+B+D** | **unmodified** | — | **36** | **26 (72.2 %)** | **10** |
| C | `ensureArrayBacking` serialized | default | 16 (1 `all` + 15 `compare`) | **16 (100 %)** | **0** |

The unmodified 72.2 % matches the ~70 % the wiring lane reported. Batch C is the causal A/B (§4).
Batch D was run *after* reverting the A/B edit, byte-identical to `HEAD` (`git diff` on
`src/core/golib/*.cs` → 0 lines) — the flake returned, so the green in C could go red and was a
measurement, not an artifact.

### 1.2 The tell nobody had looked at: *which* entries are lost

Every one of the 10 failures reports only keys in **`22001`–`22031`**:

```
lost 2 : 22005 22001
lost 2 : 22001 22009
lost 1 : 22001
lost 1 : 22001
lost 4 : 22013 22003 22001 22005
lost 5 : 22007 22003 22009 22001 22004
lost 1 : 22001
lost 10: 22021 22005 22011 22001 22007 22003 22017 22009 22028 22029
lost 2 : 22001 22003
lost 1 : 22005
```

`cache_test.go`'s `seq` counter reaches exactly **22000** at the end of the single-threaded work
(10 000 iterations × 2 `next` calls = 20 000, then 2 000 overwrite `next` calls = 22 000; the
registered-cache section allocates nothing). **`22001` is therefore the very first key created in the
concurrent section**, and it appears in 8 of the 10 failures.

So the losses are confined to roughly the first **15 of 102 100** entries — 0.015 % of the section,
all of it at `t = 0`. That is the fingerprint of a bounded start-up window, and it is what a lazy
first-materialization race looks like. It is not what GC-during-steady-state, a broken CAS, or a
broken hash slot would look like — any of those would scatter losses across the whole run.

---

## 2. The minimal isolated case

`ElemAliasProbe` — a standalone console app referencing the built `golib.dll` and `sync.atomic.dll`,
containing **no converter output**. `Node`, `Cache.table/Put/TryGet` are hand transcriptions of the
emitted `src/core/crypto/internal/boring/bcache/cache.cs`; `Tbl` is a verbatim transcription of the
generated `cacheTable<K,V>` wrapper (the nullable `m_value` + null-coalescing `Value` getter). The
real `atomic.Pointer<T>` `Load`/`CompareAndSwap` extension methods are used unchanged.

Instrumentation is one line: after each `at()`, record `head.PinnableStorage` — for this shape that
resolves through `ElemRefBox.CanonicalPair()`'s `default:` arm to the wrapper's `Source`, i.e. the
actual `Pointer<Node>[]`. Counting *distinct* storages tells you directly how many arrays one table
box handed out.

### 2.1 Results

```
ElemAliasProbe  arm=all  gcServer=False  procs=12

arm0  materialization race over one shared box
      24 threads x 300 trials
      trials whose ONE table box handed out pointers into >1 backing array: 257/300 (85.7%)
      worst distinct backings in a single trial: 14
      distinct-backing histogram:  1x43  2x156  3x84  4x14  5x1  6x1  14x1

arm1  cold table, no pre-materialization
      100 threads x 1021 Puts = 102100 entries
      lost entries: 2
      distinct backing arrays handed out by the ONE table box: 2
        backing #00bd264e  head-pointer derivations: 2          <-- the orphan
        backing #00e8af39  head-pointer derivations: 204198     <-- the winner

arm2  table pre-materialized single-threaded before launch
      lost entries: 0        distinct backings: 1   (204200 derivations)

arm3  cold table + GC pressure thread (forced gen0 collections throughout)
      lost entries: 0        distinct backings: 1

arm4  head pointers derived ONCE then reused - .at() removed from the hot path
      lost entries: 0
```

**`arm1` reproduces the real failure's arithmetic exactly**: two backings, the orphan receiving
exactly **2** head derivations, and exactly **2** lost entries. The loss count *is* the number of
operations that resolved against the orphan before it was replaced. That closes the loop between the
isolated case and the pipeline.

Repeated (30 trials each, one process, threads created fresh per trial):

| Arm | Trials losing ≥1 entry | Loss histogram | Backing histogram |
|---|---|---|---|
| `arm1r` cold, gen0 default | **1/30** | `lost0 x29`, `lost53 x1` | `1 x29`, `37 x1` |
| `arm1r` cold, gen0 = 1 GB | **1/30** | `lost0 x29`, `lost99 x1` | `1 x29`, `43 x1` |
| `arm2r` pre-materialized | **0/30** | `lost0 x30` | `1 x30` |

(In-process repetition only races on the *first* trial — once the JIT is warm, thread *n*'s
materialization completes before thread *n+1* starts. That is why a single cold trial, which is what
the real test is, races far more often than a warm loop.)

`arm2r` is the positive control: same code, same CAS, same boxes, same GC — one variable removed
(concurrent first materialization) — and the backing count is pinned at 1 and the loss at 0 in every
trial, while `arm1r` can and does go red.

---

## 3. Hypotheses refuted (each with the measurement that kills it)

| # | Hypothesis | Refutation |
|---|---|---|
| 1 | GC relocation invalidates the `Interlocked` target mid-operation | The CAS target is `ref x.v` reached from `m_backing[m_index]` — a GC-*tracked* managed ref, safe by construction. `arm4` performs the identical CAS on the identical array under the identical GC and loses nothing. |
| 2 | `atomic.Pointer<T>.CompareAndSwap` / `nilCanon` is wrong | `arm2`/`arm4` use those exact extension methods over that exact `Pointer<Node>[]`, 204 200 operations per trial, 30 trials, zero loss. |
| 3 | Tiered compilation artifact | Already refuted by the wiring lane (`TieredCompilation=0` was **worse**, 4/10). Consistent with a start-up-timing race: less warm-up ⇒ tighter thread clustering ⇒ more racing. Do not route this to a tier-0 A/B. |
| 4 | **GC-correlated** (`DOTNET_GCgen0size=1GB` → 10/10) | **Does not replicate.** Real pipeline: **8/10** with `DOTNET_GCgen0size=0x40000000`, statistically identical to 8/10 default. `arm1r`: 1/30 in both. And `arm0` shows a huge gen0 makes the race *more* likely (98.7 % vs 85.7 %), because threads run without pauses and cluster harder. **The reported 10/10 was n=10 sampling noise.** There is no GC dependence to explain. |
| 5 | The `registerCache` wiring / gen2 sentinel | Exonerated by the wiring lane (reproduced disarmed, 3/10), and structurally: the failing section uses a **fresh, unregistered** `Cache`, so no clearing path is reachable. The probe contains no sentinel and reproduces. |
| 6 | `ElemRefBox` canonicalization mints two boxes for one element | Backwards. Canonicalization is correct: the two boxes name two different *arrays*, and they do so because the wrapper materialized twice. `arm2` proves canonicalization is sound once the backing is singular. |
| 7 | Hash slot `(uintptr)Ꮡk` moves under GC, so `Get` looks in the wrong bucket | `StandardBox.PointerOrderToken` is `AllocationBase(RuntimeHelpers.GetHashCode(this))` — identity-hash based and GC-stable, never an address. `arm2`/`arm4` compute the slot identically with zero loss. |

---

## 4. The causal A/B (batch C)

`ensureArrayBacking` was temporarily serialized per box — the whole read/box/materialize/write-back
sequence inside `lock (this)`, boxing the copy *inside* the lock. Nothing else changed.

* **16/16 PASS** (1 `all` + 15 `compare`), against **26/36** unmodified.
* Fisher exact, 10 failures in 36 vs 0 in 16: **p ≈ 0.02**.
* The edit was then reverted; `git diff` on `src/core/golib/*.cs` reports **0 changed lines**, and the
  flake returned immediately (batch D, 9/15).

**The lock is a probe, not a proposal** — see §6.

---

## 5. Blast radius

### 5.1 The trigger, stated precisely

> A **named fixed-size Go array type** (`type T [N]E`) whose **zero value** is first touched through
> **more than one thread**, or through a **by-value copy** (§5.2).

The lazy `m_value ??= new array<E>(N)` getter is emitted for exactly `TypeClass == "Array"` — a Go
*named array* type. golib's own `array<T>`/`slice<T>` are **not** affected: their `Source` is the raw
backing and never materializes, so `ensureArrayBacking`'s write-back rewrites identical bytes and is
benign.

`ж<T>.at()` is not the only door. The generated `Value` getter itself is a read-modify-write of
`m_value`, so any concurrent first touch of a shared zero-valued named array — indexing, `Length`,
`Source`, ranging — races the same way even without `at()`.

### 5.2 The unfixed sibling: `builtin.Ꮡ(IArray<T>, int)` — no concurrency required

`Ꮡ<T>(IArray<T> target, int index)` (`builtin.cs:1946`) takes its target **by value**, boxing the
caller's struct, and — unlike `ж<T>.at()` — **never runs `ensureArrayBacking`**. Over a still-lazy
named array, the backing materializes on that private boxing temp, which the `ElemRefBox` then
retains. The shared storage is never written. Measured (`arm5`):

```
arm5  by-value element pointer over a LAZY named-array wrapper (runtime/sema.cs shape)
      two consecutive `Ꮡ(t, 0)` calls name the SAME storage: False
      two consecutive `box.at(0)` calls name the SAME storage:  True
      `Ꮡ(t, 0)` and `box.at(0)` name the same storage:          False
      a Store through Ꮡ(t,0) is INVISIBLE to a second Ꮡ(t,0):   True
      a Store through Ꮡ(t,0) is INVISIBLE to box.at(0):         True
```

This is **unconditional write loss**, single-threaded — the *original* `47ddd5a50` defect, still live
at the `Ꮡ` overload because the fix was only applied at `ж<T>.at()`.

The corpus site is **`src/core/runtime/sema.cs`**:

```csharp
internal static ж<semTable> Ꮡsemtable = new StandardBox<semTable>(default(semTable));
internal static ref semTable semtable => ref Ꮡsemtable.Value;

[GoType("[251]semTableᴛ1")] partial struct semTable;

[GoRecv] internal static ж<semaRoot> rootFor(this ref semTable t, ж<uint32> Ꮡaddr) {
    return Ꮡ(t, (int)((((uintptr)Ꮡaddr >> (int)(3))) % (uintptr)semTabSize)).of(semTableᴛ1.Ꮡroot);
}
```

`rootFor` is the **only** access path to `semtable` (lines 153 and 200), so nothing ever materializes
the shared table: every call hands back a pointer into a brand-new private 251-entry array, i.e. a
fresh zero `semaRoot`. Every semaphore-queue mutation through it is lost. This is **latent today**
only because `sync`'s Mutex/RWMutex/WaitGroup are hand-owned on `SemaphoreSlim`/monitors and never
reach `runtime.semacquire`. It would bite the moment anything did.

**Candidate, not yet measured:** `src/core/runtime/mpallocbits.cs` uses the same by-value door —
`(Ꮡ((pageBits)(b))).setRange(…)` and six siblings — over `pageBits`/`pallocBits`, both
`[8]uint64` named arrays. `pallocBits` is literally the shape `47ddd5a50` named as "the pallocBits
lesson". Worth its own check.

### 5.3 Census

| Measure | Count |
|---|---|
| `[GoType("[N]…")]` fixed-size named array declarations, production sources | **50** in **23** packages |
| …including `*_test.cs` | 59 in 28 packages |
| Of those 23 packages, on the banked roster | **16** — `archive/tar`, `bytes`, `crypto/internal/edwards25519`, `crypto/internal/mlkem768`, `crypto/internal/nistec`, `crypto/x509`, `hash/crc32`, `hash/crc64`, `hash/fnv`, `image/jpeg`, `internal/abi`, `internal/trace`, `net/http`, `strings`, `syscall`, `unicode` |
| `.at<` call sites corpus-wide (upper bound on door A) | **311** in **64** files |
| Door A confirmed | `crypto/internal/boring/bcache` |
| Door B confirmed | `runtime/sema.cs` `rootFor` |

The named array types are: `IntArgRegBitmap`, `SockaddrGen`, `Table`, `asciiSet`, `block`,
`buckhashArray`, `byteReplacer`, `cacheTable<K,V>`, `d`, `fiatScalar{,Non}MontgomeryDomainFieldElement`,
`header{V7,GNU,STAR,USTAR}`, `nttElement`, `p{224,256,384,521}{,Non}MontgomeryDomainFieldElement`,
`p{224,256,384,521}Table`, `pageBits`, `ringElement`, `semTable`, `sigset`, `slicing8Table`,
`sse42Table`, `statDepSet`, `sum128`, `sum128a`, `sum224`, `timedEventArgs`, `tmpBuf`, `ΔcgoCallers`.

### 5.4 What this means for banked rows

Containing a named array is **not** the same as being exposed. Most of the 50 are per-value locals or
built-once tables — tar's four header views, fnv's `sum128`, nistec's point tables (`Select` reads
through the `ref` receiver and takes no element pointer), the crc tables. The realistic exposure is
narrow.

But the failure mode is a **silent lost write**, never a crash or an exception, and it is confined to
a start-up window measured in microseconds. **A green row is therefore not evidence of absence.**
Any banked row that materializes a shared, still-zero-valued named fixed-size array from more than one
thread is a candidate explanation for a rare, non-reproducible flake — and door B (§5.2) needs no
concurrency at all, so its sites should be enumerated and checked individually rather than assumed
clean.

---

## 6. Remedy direction — and the design decision this needs

**Not implemented. No golib change is banked on this branch.** Per the brief, the root turned out to
demand a design decision, so it is escalated rather than settled.

The tension: the materialization must become **atomic and singular**, but `ж<T>` is deliberately
**unconstrained** in `T`, so golib cannot call an interface member on `ref Value` without boxing a
copy — and the non-boxing route was already tried and reverted: `47ddd5a50` used "a non-boxing
constrained interface call (delegate built once per T)", and `d5c0c9c10` had to tear it out because
"every Native AOT binary was dead at type-init". Any proposal must survive that.

Three candidate directions, in increasing scope:

1. **Gate the materialization per box (golib only).** `ж<T>` is a *class*, so it can hold a
   `volatile int m_arrayBackingReady` and do the double-checked `lock (this)` once. Race-free, one
   volatile read on the hot path afterwards, no converter change, no AOT exposure. Closes door A
   (the bcache class) completely. Does **not** close door B, which never touches a box.
   *This is the batch-C probe, minus the per-call lock cost.*

2. **Route `ref`-receiver named-array element addresses through the box (converter).** Emit
   `Ꮡt.at<TElem>(i)` instead of `builtin.Ꮡ(t, i)` when the receiver is a `ref` to a named array type —
   exactly the arm `47ddd5a50`'s `convSelectorExpr` already added for
   `bh.at<atomic.UnsafePointer>(i)`, which `rootFor` was missed by. Closes door B. Being an emission
   change it owes CNR plus a behavioral guard of its own.

3. **Publish the wrapper's backing with an interlocked CAS (go2cs-gen).** Hold the backing as a plain
   `E[]? m_backing` and materialize via `Interlocked.CompareExchange(ref m_backing, new E[N], null)`,
   with `Value` re-reading the winner. Since a named fixed-size array is *always* a full window
   (`Low = 0`, `Length = N`), `new array<E>(m_backing)` reproduces present semantics exactly. This
   makes two *direct-field* materializations converge — but it cannot help a materialization that
   happens on a **boxed copy**, so it is a complement to (1) and (2), not a replacement.

**The decision to be taken:** whether the durable answer is (1)+(2) — keep the laziness, make every
door either atomic or box-mediated — or a fourth option that removes the laziness at its source
(e.g. a generated parameterless constructor that allocates, so `new(T)` and `= new()` field
initializers are born materialized). Option 4 is attractive but **incomplete on its own**: `default(T)`
skips constructors, so a named array reached as a zeroed struct field, a slice element, or
`Ꮡ(default(T))` would still be lazy. Recommendation is **(1) first** — it is small, self-contained in
golib, closes the measured live defect, and is independently gateable — then **(2)** as its own
converter increment with `runtime/sema.cs` as the proof site.

Also worth recording as doctrine, independent of which option wins:

> **A lazy-initialization fix is not finished until the publish is atomic.** `47ddd5a50` correctly
> diagnosed "the allocation landed on the copy and the real storage stayed virgin" and added the
> write-back — which is right single-threaded and lossy with two threads. The single-threaded repair
> of a lost-write defect is exactly the shape that leaves a concurrency residue behind.

---

## 6a. Amendment 2026-08-30 — door 2 is closed (option 3, with a correction)

Lane `claude/local-door2-fix`. Doors 1 and 3 merged as `a6b951a55`; **door 2 — the generated `Value`
getter's own `??=` — is now closed too**, by option 3 above (the interlocked CAS in go2cs-gen), with
one correction to how §6 described it and one consequence §6 did not anticipate.

* **Measured, arm7, 24 threads × 300 trials × 3 batches:** `872/900` racing trials before
  (98.0 % / 97.0 % / 95.7 %), **`0/900`** after. Arms 0–5b, 6 and 8 unchanged; `arm8` still reports
  EXPOSED, as designed — a publish on a by-value COPY is a different door.
* **Correction to option 3's sketch.** It proposed holding the backing as a plain `E[]?`. That is not
  value-preserving: a constructor-supplied `array<E>` may be an **alias window** (`array<E>.Alias`,
  Go's `(*[N]E)(s)`) whose `Source` is wider than the array, so flattening to the backing would widen
  the named array and shift its origin. The slot is a `StrongBox<array<E>>` — still one CAS-able
  machine word, but carrying the whole value. (`object` also carries it and needs no golib companion,
  but costs an `unbox.any` helper call on every warm read: `1.97 → 4.13 ns/op` on the element path
  against `1.97 → 2.45` for the typed holder.)
* **The consequence §6 did not anticipate.** Changing the slot from a struct to a reference silently
  moved `ValueType.Equals` and `ValueType.GetHashCode`, which a C# struct inherits and which both read
  that field. Two distinct wrappers over equal content stopped comparing equal and stopped hashing
  alike — so a Go named array used as a **map key** or compared by `reflect.DeepEqual` began missing
  itself. The `==` operator hid it entirely (it binds the wrapper's own structural
  `Equals(IArray<E>)` at compile time), which is why it had to be measured rather than reasoned:
  probe `arm10` checks the compile-time and the runtime overload separately. The Array kind now emits
  both overrides, delegating to `array<E>`'s element-wise pair.
* **Not a golib-free change after all:** `GoReflect.TryUnwrapWrapperValue` reads `m_value` by
  reflection, so it unwraps the holder's extra level.
* **New guard:** `NamedArrayWrapper` gains a map-key probe (two separately built equal keys, plus the
  virgin zero array), proven red at the emission without the overrides.
* **New probe arms:** `arm9` (warm-getter cost, both emissions in one process against a `TblLazy`
  transcription of the `??=` shape) and `arm10` (hash/equality parity).

The §6 doctrine line stands and gains a second clause: *a lazy-initialization fix is not finished
until the publish is atomic* — **and making a publish atomic moves the value out of the struct, so
every inherited member that read that field has to be re-measured, not re-reasoned.**

## 6b. Amendment 2026-08-30 — the mpallocbits operator-copy door is closed

Lane `claude/i9-mpallocbits`. §5.2's "candidate, not yet measured" is measured and fixed. Unlike
doors 1/2/3, this one is not a materialization RACE at all — no concurrency needed to trigger it,
single-threaded is sufficient — which is why it was tracked separately from the start.

**Root cause, confirmed (arm8, unchanged as the historical measurement).** Go's
`(*pageBits)(b).setRange(…)` (`b *pallocBits`, both `[N]uint64` named arrays — literally
`type pallocBits pageBits`) converted through the general path as a VALUE conversion:
`Ꮡ((pageBits)(b))`. The generated conversion operator for an Array-kind wrapper takes its argument
BY VALUE (`implicit operator pageBits(pallocBits value) => value.view`), so the struct COPY is
made before anything materializes — a still-lazy `b` then materializes on the OPERATOR'S OWN
PARAMETER COPY, and the caller's storage is never written. Once something else has already
materialized `b` (warm), every copy already shares the one backing array and the bug is invisible —
exactly the "exposed on first touch only" signature §5.2 named it by.

**Fix, at the converter (`pointerReinterpretManagedSource`, `convCallExpr.go`).** The function
already routes every OTHER `(*U)(p)` pointer reinterpret whose source is a managed box through
golib's `Reinterpret<T,TDst>()` — which ALIASES the source's storage rather than copying through a
value conversion — but excluded EVERY pointer-to-array target as a blanket rule, because the
general array case (a numeric or other non-array pointee reinterpreting to an array pointee) fails
`Reinterpret`'s own safety gate (`array<E>` is a backing-store REFERENCE, too wide to alias a
narrower slot) and would silently fall back to exactly the address route the function exists to
avoid. `pallocBits`/`pageBits` are the ONE sub-case that exclusion swept up wrongly: BOTH sides are
named types over the IDENTICAL array shape, and go2cs-gen's `InheritedTypeTemplate` gives every
Array-kind wrapper the same one-field shape (a `StrongBox<array<E>>` slot) regardless of its Go
name — so golib's `ReinterpretAliasesStorage<T,TDst>` (checking layout compatibility, not Go
identity) correctly recognizes them as alias-safe. Narrowed the exclusion to require the source's
own pointee to ALSO be an array-underlying type identical to the target's, which is exactly and
only the sibling-names-over-one-shape case.

Emission changed from:

```csharp
internal static void allocRange(this ж<pallocBits> Ꮡb, nuint i, nuint n) {
    ref var b = ref Ꮡb.DerefOrNull();
    (Ꮡ((pageBits)(b))).setRange(i, n);
}
```

to:

```csharp
internal static void allocRange(this ж<pallocBits> Ꮡb, nuint i, nuint n) {
    (Ꮡb.Reinterpret<pallocBits, pageBits>()).setRange(i, n);
}
```

— routed entirely through machinery that already existed for every other managed-source pointer
reinterpret; no new golib code, no new converter concept, one exclusion narrowed. The
`ref var b = ref Ꮡb.DerefOrNull();` prologue drops out too: `Reinterpret` operates on the box
directly, so the deref-and-rebind this shape used only to feed the doomed value conversion is no
longer needed.

**Verification.**
* `ElemAliasProbe` arm8 stays RED, unchanged, and now documents explicitly that it measures the
  GENERAL hazard (any hand-written `(Dst)(src)` conversion over an Array-kind wrapper), not the
  corpus's own call site. A new arm8b measures the ACTUAL fixed emission
  (`Ꮡb.Reinterpret<pallocBits, pageBits>()`) directly: **not exposed, cold or warm.**
* Every other arm (0, 1-4, 5, 5b, 6, 7, 9, 10) re-run and unchanged from its established result —
  the narrowed exclusion touches nothing outside the one sub-case.
* Full corpus reconvert-and-build (seeded, isolated blast radius exactly 2 files —
  `runtime/mpallocbits.cs` and its `runtime/windows/package_info.cs` position-map companion): 307
  projects, 0 errors.
* CNR and the full behavioral suite: see the mailbox report for this lane's numbers at commit time.

Not a design escalation — the shape generalized cleanly through machinery already in the file,
exactly the outcome the dispatching ruling scoped as the non-escalation branch.

## 7. Artifacts

* `element-aliasing-investigation.md` — this report.
* `src/tests/ElemAliasProbe/` — the isolation probe (`arm0`…`arm10`, plus `arm1r`/`arm2r` repeat
  harnesses). Standalone; references the built `golib`/`sync.atomic` assemblies, is registered in no
  solution and enumerated by no gate. `dotnet run --project src/tests/ElemAliasProbe -c Release -- all`.

Scratch logs: `<scratchpad>/elem-alias-*.log`.
