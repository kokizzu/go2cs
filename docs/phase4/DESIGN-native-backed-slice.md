# DESIGN — the native-backed `slice<T>`: aliasing native memory instead of snapshotting it (W1b's commission)

**STATUS: PROPOSED** — design-only, per the 2026-08-22 commission. No implementation, no golib
changes ship with this document. Template per `DESIGN-readmemstats-surface.md` /
`DESIGN-zh-box-b-prime.md` / `DESIGN-linux-exec.md`: the measured bill first, the design second,
the adversarial pass third, open questions with recommendations last. Inputs: R's mmap probe
(the sockaddr lane's board entry), golib's slice model (`slice.cs`), the ж-box family
(`FINDING-managed-box-uintptr-lifetime`, the row-#159 native-slot precedent), and `unsafe.cs`'s
own documented limitation at the exact line this design retires.

---

## 1. The measured bill

**The probe (R, converted program, isolated clone):** `syscall.Mmap(0, 0, 3·pagesize,
PROT_READ|PROT_WRITE, MAP_ANONYMOUS|MAP_PRIVATE)` returns a 12,288-byte `[]byte` with `err nil` —
and it is not the mapping. `Mprotect(b[:pagesize])` → `EINVAL`; `Munmap(b)` → `EINVAL`; writes
"succeed" into a copy the kernel never sees. Mechanism, from source rather than inference:
`unsafe.Slice` over a NATIVE pointer takes the `ptr.IsNative` arm in `unsafe.cs` and
**SNAPSHOTS** — `new slice<T>(new ReadOnlySpan<T>((void*)ptr.NativeAddress, n))` — a limitation
the code itself documents as "sufficient for reading a block a syscall returned … writes through
the resulting slice do not reach the native memory."

**The two rows carrying it (W1b):** `crypto/sha1` (`TestOutOfBoundsRead`, `panic: invalid
argument` — an mmap'd guard page the test deliberately reads up to) and `bytes` (the
page-boundary family, `TestIndexByteNearPageBoundary` / `TestIndexNearPageBoundary`). Both FAIL
on Linux today and are held honestly at that residual.

**The wider reach, so the design is sized by the class and not the rows:** every
`unsafe.Slice(nativePtr, n)` site — `syscall.Environ`'s environ block today, any future mmap'd
I/O, and every syscall that returns a kernel-owned buffer the caller is meant to WRITE. The
class has already shown its teeth once on the MANAGED side: `crypto/subtle`'s `xorBytes` rebuilt
`dst` from `&dst[0]` and the old snapshot swallowed every write — the whole test matrix compared
`dst` against its untouched fill. That defect forced `TryGetElementWindow` (aliasing) for
managed storage; this design is the SAME correction for the native arm.

**What the bill does NOT include:** managed-storage aliasing (done, above), `SliceData`
round-trips over managed backings (done — the interior-pointer repair with its three recorded
wrongs), and any change to Go-visible semantics. Go's spec is the contract; the design's job is
to make the existing contract TRUE for native memory, not to add one.

## 2. The design

### 2.1 The shape: `slice<T>` grows a native backing the way `ж<T>` grew one

The precedent is already banked. `ж<T>` carries `m_nativeAddr` beside its managed modes; a
native-backed box round-trips `(uintptr)`/`(void*)` to the EXACT address it aliases, and row
#159 settled the doctrine that the native slot holds the pointer VALUE. `slice<T>` mirrors it:

```
internal readonly T[]  m_array;      // managed backing (empty when native)
private  readonly nint m_low;        // window start — INDEX for managed, ELEMENT OFFSET for native
private  readonly nint m_length;
private  readonly nint m_capacity;
private  readonly nuint m_nativeBase; // NEW: 0 = managed; else the mapping's base address
```

One added word on golib's hottest type. `m_nativeBase == 0` is the discriminant; every existing
managed-path operation is unchanged except for the branch, and §5 prices that branch as a
MEASURED gate, not an assumption. A native-backed slice's element `i` lives at
`m_nativeBase + (m_low + i) * sizeof(T)`.

**Only unmanaged `T` may be native-backed**, enforced at every creation site with a named panic
(`RuntimeHelpers.IsReferenceOrContainsReferences<T>()`): a managed reference has no meaning in
kernel memory — this is the SiginfoChild corruption class stated as a constructor precondition
instead of discovered as heap corruption.

### 2.2 Creation sites — deliberately few

1. **`unsafe.Slice(nativePtr, n)`** — the `ptr.IsNative` arm stops snapshotting and constructs
   the native-backed slice over `(base: NativeAddress, low: 0, len: n, cap: n)`. `cap == len` is
   Go's own answer for `unsafe.Slice`.
2. **Nothing else in v1.** `syscall.Mmap`'s converted body already builds its result through
   `unsafe.Slice`, so the two rows need no second door. Future golib constructors (an mmap'd
   file reader, a native ring) go through the same one, on purpose: one creation site is what
   keeps the unmanaged-`T` precondition and the lifetime story (§2.5) auditable.

### 2.3 Operation semantics, the whole table

| Operation | Native-backed behavior | Why it is Go's |
|---|---|---|
| `s[i]` read/write | `Unsafe.Read/Write<T>` at the computed address | the mapping IS the storage; writes reach the kernel's page — the probe's failing case |
| `s[lo:hi]`, `s[lo:hi:max]` | same struct arithmetic on `m_low`/`m_length`/`m_capacity`, base unchanged | reslicing never reallocates in Go |
| `len`/`cap`/`== nil` | unchanged field reads | backing-agnostic already |
| `Ꮡ(s, i)` (`&s[i]`) | a **native-backed `ж<T>`** at the element address — the existing `ж(nuint)` constructor, no new machinery | `(uintptr)Ꮡ(s,i)` and `unsafe.Pointer` conversions then yield the REAL address: `Mprotect(b[:pagesize])` hands the kernel the mapping, which is the acceptance case verbatim |
| `unsafe.SliceData(s)` | native-backed `ж<T>` at `base + low·size` | the interior-pointer identity the managed repair established, now exact for native too |
| `append(s, …)` within cap | in-place native writes past `m_length` | Go: same backing while capacity holds |
| `append(s, …)` growing past cap | allocate a MANAGED `T[]`, copy, return a managed-backed slice | **Go's spec is the answer the commission asked for**: append that exceeds capacity returns a slice over a NEW array, and writes through it stop aliasing the old one. The native mapping simply plays the role of "the old array". No hidden native allocator, no surprise |
| `copy(dst, src)` | `Buffer.MemoryCopy`/span copy across any backing pair | byte-exact both directions |
| `(@string)(s)`, `ToArray()` | copy out, as they already do for managed | conversions snapshot BY CONTRACT in Go |
| range / enumerator | the indexer path | covered by it |
| equality | Go slices compare only to nil — no identity surface exists to preserve | unchanged |

### 2.4 The internal unification: `Span<T>` once, then flat

The commission named `MemoryManager<T>`; the design examined it and recommends the smaller tool.
What golib's helpers actually need is `Span<T>`:

```
internal Span<T> AsSpan() =>
    m_nativeBase != 0
        ? new Span<T>((void*)(m_nativeBase + (nuint)m_low * size), (int)m_length)
        : new Span<T>(m_array, (int)m_low, (int)m_length);
```

Every hot bulk path — `copy`, `bytealg`'s IndexByte family, the string conversions — goes
span-first and pays the discriminant ONCE per operation instead of per element.
`MemoryManager<T>` exists to mint `Memory<T>` — a heap object per mapping, for async escapees
golib does not have; it adds allocation and lifetime coupling for a capability nothing in the
bill uses. OQ-2 records it as the door to open IF `Memory<T>` interop is ever demanded; v1 does
not open it.

### 2.5 Lifetime, pinning, GC — deliberately nothing

A native mapping is not managed memory: there is nothing for the GC to move, so there is
**no pin**, and nothing for the GC to free, so there is **no ownership object**. `Munmap` is the
lifetime, exactly as in Go, and use-after-munmap faults exactly as in Go — parity includes the
hazards. The one lifetime rule the design adds is the one ж's native mode already lives by: a
native-backed slice never keeps anything alive, and nothing keeps IT valid except the program's
own mmap discipline. (An OWNED native buffer type with a finalizer is a different, future
design; conflating it here would give mmap semantics Go does not have.)

## 3. Rejected alternatives, with the reasons on the record

- **A separate `NativeSlice<T>` type.** The converter emits `slice<T>` at every Go `[]T` site;
  a second type either forks the emitted surface (converter + corpus churn across everything) or
  hides behind an interface (boxing golib's hottest type). The polymorphism must live INSIDE the
  struct, where one word and one predictable branch carry it.
- **Write-back snapshots** (copy out, copy back at… when?). There is no "when" — the kernel
  observes the memory continuously (`Mprotect` checks the address, a guard page faults on
  touch). The probe is the refutation.
- **A `Memory<T>`/`MemoryManager<T>` re-architecture of `slice<T>`.** Strictly more machinery
  for strictly less control on the hot path (`Span` via `Memory.Span` pays an extra indirection),
  plus a per-mapping heap object. Span-internal unification (§2.4) captures all the value.

## 4. Adversarial pass

- **"One extra word and a branch on the hottest type in the corpus."** The claim that it is
  cheap is NOT accepted on argument — §5 makes it a measured gate (the Perf trio + a GolibTests
  microbench bound) with numbers required before merge. The design notes only that the branch is
  perfectly predicted on the managed path (the discriminant is a struct field already in cache)
  and that `readonly` fields keep every existing JIT enregistration.
- **"append detaching from the mapping will surprise someone."** It is Go's own spec, and the
  surprise exists in Go identically (`append` over an mmap'd slice at capacity allocates heap).
  The doc states it; the GolibTests family asserts it (growth returns managed, old slice still
  aliases the mapping).
- **"Reinterpret casts (`Reinterpret<TFrom,TTo>`) over a native slice."** In-scope members whose
  math is `sizeof`-scaled keep working (the base is a byte address); any member that reaches for
  `m_array` directly must take the span path or panic NAMED — the implementation census (§5)
  enumerates every `m_array` touch in `slice.cs` precisely so none is missed.
- **"32-bit."** The corpus is amd64-only per flavor; `nuint` is the pointer width either way.
- **"The unmanaged-T check costs on every construction."** `IsReferenceOrContainsReferences<T>`
  is a JIT-time constant; the check compiles away for every managed-backed construction and
  folds to a constant branch on the one native creation site.

## 5. Acceptance and gates (spec'd here, run by the implementation lane)

1. **The two rows**: `crypto/sha1` and `bytes` on Linux at the lane tip — W1b's residual is the
   bill; their flip (or honest sub-residual) is the measurement.
2. **A GolibTests family** (`NativeBackedSliceTests`): index read/write reaching real memory
   (assert via a second native view), reslice arithmetic, `Ꮡ(s,i)`/`SliceData` address
   EXACTNESS (`(uintptr)` equality against computed addresses), append-within-cap in place,
   append-past-cap detaching (managed result, mapping unchanged), copy both directions, the
   unmanaged-T named panic, and `unsafe.Slice(&s[i], n)` managed aliasing UNCHANGED (the
   crypto/subtle regression guard).
3. **The perf gate, measured not asserted**: `PerfSieve`/`PerfString`/`PerfStringView` A/B at
   the merge tip within noise (the trio leans hardest on the indexer and bulk paths), plus a
   microbench bound in GolibTests for the managed-path indexer.
4. **The standing envelope**: GolibTests full, behavioral suite full (the slice is under every
   test in the corpus), CNR (no converter change expected — OQ-4 verifies), Windows stdlib
   build, and the i9 control per the usual dispatch.
5. **The `m_array` touch census**: every member of `slice.cs` (and every golib helper reading
   `m_array` via `internal`) enumerated and dispositioned span-path / arithmetic-safe /
   named-panic BEFORE the first commit — the SiginfoChild lesson applied preventively.

## 6. Open questions, each with a recommendation

- **OQ-1 — span-unification depth in v1.** Full sweep of every bulk helper vs the minimal set
  (`copy`, `bytealg`, string conversions). **Recommend: minimal set in v1** — each additional
  adoption is its own small perf win with its own small risk; land the model first.
- **OQ-2 — `MemoryManager<T>`/`Memory<T>`.** **Recommend: defer** (§2.4); reopen only when a
  consumer demands `Memory<T>` over a mapping.
- **OQ-3 — should `syscall.Environ` move to the aliasing arm?** It works today BECAUSE the
  snapshot is read-only-sufficient. **Recommend: yes, but as a no-behavior-change ride-along**
  (it reads once and never writes) — one fewer special case, and the snapshot arm can then be
  DELETED rather than maintained beside its replacement.
- **OQ-4 — converter involvement.** None expected: the emitted surface (`slice<T>`, `Ꮡ`,
  `unsafe.Slice`) is unchanged. **Recommend: CNR as the verifier, not a converter edit.**
- **OQ-5 — who implements.** A golib lane with the full envelope (this is the hottest-type
  change of the campaign); the two W1b rows re-measured on Linux and the i9 sweeping the
  Windows control. **Recommend: its own lane, sized like the backoff arc, with §5's census as
  the first commit.**

---

*Prepared by lane G (`claude/native-slice-design`), 2026-08-22, against master `662b1595f`'s
successors. For ratification: §2's shape (the ж-precedent dual backing, one creation door,
Go's append answer), §2.4's Span-over-MemoryManager recommendation, and §5's gate ladder.*
