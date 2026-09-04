# The `os` want-zero seam — three capabilities, one contract, one boundary

> **Status: design record, not a cut (COORD ruling 2026-09-03, mailbox `8e17989b2`; scope restated in
> the gate handoff `b01402107` §3).** This is the ж-box arc's record for the `os` want-zero row
> (`TestWriteStringAlloc`), written so the next lane starts from the MEASURED per-box bill and the
> per-capability arithmetic rather than from the 704 B headline. Every number below was measured on
> G-LAPTOP at master `fd2e618b9`, go1.23.12 windows/amd64, .NET 10, **Release + tiered compilation
> off**, 100 runs, by a per-construction-site instrument that was reverted after each reading
> (mailbox `5e0000301`, `3b677073a`); every code claim names the file and line it was read from at
> `dc24a21c3`. The record measures nothing new and cuts nothing. Companion records:
> [`DESIGN-zh-box-reduction.md`](DESIGN-zh-box-reduction.md) (Phase A, the ref-lowering pass),
> [`DESIGN-zh-box-b-prime.md`](DESIGN-zh-box-b-prime.md) (B′, the dual receiver emission),
> [`DESIGN-phase-c-element-aliasing.md`](DESIGN-phase-c-element-aliasing.md) (the measured wall on
> edwards25519 that Phase-C's aliasing field pointer is sized against).

## 1. The bill, priced per site

`os`'s only remaining divergence is `TestWriteStringAlloc`: `AllocsPerRun` bounded at ZERO on
`f.WriteString(s)`. Ruling #1 (owner, 2026-08-02; restated `043665204`) governs it: a want-zero
allocation assert is satisfiable in principle and is never a disclosure, so the row banks at **zero
bytes** and the arcs that reach it are priced, not argued. The complete measure is the byte count
(`AllocsPerRun` returns 0 only when zero bytes were allocated — `core/testing/testing.cs`); golib's
`AllocationCounter` reads a LOWER BOUND (golib sites only).

| Arc | Site | allocs/run | bytes/run |
|:--|:--|--:|--:|
| **1 — the aliasing `FieldRefBox`** | the TYPED `of()` overload, `golib/ж.cs:171` | **11.00** | **704.0** |
| 2 — owning box (escape analysis) | `Ꮡ(in T)` → `StandardBox`, `builtin.cs:1930` | 1.00 | 88.0 |
| 2 — element box | `Ꮡ(buf, i)` → `ElemRefBox`, `builtin.cs:1970` | 2.00 | 128.0 |
| non-box golib (incl. pinnable slots; an UPPER bound) | by subtraction | 3.00 | — |
| NONE bucket — defer/`GoFunc` frame + the dead `unsafe.Pointer` + BCL | not a golib site | — | 537.8 |
| **total** | | **17.00** | **1,457.8** |

Two facts the priced census settled (`5e0000301`): arc 1 is the largest item in BOTH units and it is
ONE call site (the untyped `of()` and every other `FieldRefBox` site fired zero; the per-call wrapper
is memoized, so 64 B is the box itself); and the NONE bucket — 37 % of the bytes — cannot be split by a
golib-site instrument, so arc 3 and the dead-`Pointer` peephole stay UNPRICED until a byte probe inside
converted `os`/`syscall` frames exists. (The board's 1,320 B/run is the same row at Debug/tiered; the
two differ on the configuration axis, not on any cut.)

### 1.1 The 704 B seam, attributed per field

The `of()` attribution (`3b677073a`, reproducing 11.00 / 704.0 exactly) split the eleven boxes by the
field they alias. This record adds four columns the attribution did not carry — the package boundary
each box crosses, who owns the callee's declaration, which capability removes it, and WHERE each row's
shape and ownership were read (the mailbox hash for a measurement, the file and line for a code read at
`dc24a21c3`) — so every row is re-derivable on its own rather than trusted as part of a whole. That
last column exists because two rows of this very table were carried wrong until they were re-read
today (§2.1, §2.3).

| Field aliased | /run | B/run | Call shape (site) | Boundary | Callee ownership | Removed by | Measured by |
|:--|--:|--:|:--|:--|:--|:--|:--|
| `file.Ꮡpfd` | 1 | 64 | receiver, direct — `Ꮡf.of(File.Ꮡpfd).Write(b)` | **os → internal/poll** | converted (`FD.Write` is generated and carries its own `ref var fd` entry alias) | **cap 1** + the contract | count `3b677073a`; site `os/file_posix.cs:58`; callee `internal/poll/windows/fd_windows.cs:722-725` |
| `FD.Ꮡfdmu` | 2 | 128 | receiver, direct — `.rwlock(true)` / `.rwunlock(true)` | same package | converted | **cap 1** | count `3b677073a`; sites `internal/poll/fd_mutex.cs:245/255` |
| `FD.Ꮡl` (direct) | 1 | 64 | receiver, direct — `Ꮡfd.of(FD.Ꮡl).Lock()` | **internal/poll → sync** | **hand-own**, storage-keyed (§2.1) | **cap 1** + a hand-declared primary + the contract | count `3b677073a`; site `fd_windows.cs:734`; callee `sync/mutex.cs` (`Lock` / `gateOf`, §2.1) |
| `FD.Ꮡl` (deferred) | 1 | 64 | receiver, defer-captured — `defer(Ꮡfd.of(FD.Ꮡl).Unlock, ref ᒐ)` | internal/poll → sync | the same hand-own | **cap 4** + that primary + the contract | count `3b677073a` (the Lock-vs-deferred-Unlock sub-prediction, confirmed to the box); site `fd_windows.cs:735`; `defer`'s signature `golib/builtin.DeferRegistrations.cs:55` |
| `fdMutex.Ꮡstate` | 4 | 256 | pointer ARGUMENT — `atomic.LoadUint64(Ꮡmu.of(fdMutex.Ꮡstate))`, `CompareAndSwapUint64(…)` | **internal/poll → sync/atomic** | **hand-own**, storage-only leaf (§2.2) | **cap 3** + the caller's ref + the contract | count `3b677073a`; sites `fd_mutex.cs:69/77/87/98`; callee `sync/atomic/doc_impl.cs:81/222`; scope `14171d280` |
| `fdMutex.Ꮡwsema` | 2 | 128 | pointer ARGUMENT — `runtime_Semrelease(Ꮡmu.of(fdMutex.Ꮡwsema))`; stored to a box-typed local, consumed later | internal/poll's own bodyless declaration, linknamed to `runtime` | **hand-own**, IDENTITY-keyed (§2.3) | **nothing in this record — the boundary (§6)** | count `3b677073a`; sites `fd_mutex.cs:107` (direct), `:145` (store), `:170` (use); callee `internal/poll/runtime_sema_impl.cs:46-48`; boundary `14171d280` |
| | **11** | **704** | | | | | |

Six of the eleven are pointer ARGUMENTS satisfying a `ж<T>` parameter, not receivers; that is the
finding that turned a two-capability sizing into this record, and it is why the parameter half
(§3.3) carries the majority of the seam's bill.

## 2. Three reads that decide the arithmetic

### 2.1 `sync.Mutex` is hand-owned AND storage-keyed

`src/core/sync/mutex.cs` is a whole-file hand-own (`[module: go.GoManualConversion]`, the native
`SemaphoreSlim` rewrite of 2026-07-11). Its `Lock` is `public static void Lock(this ж<Mutex> Ꮡm) =>
gateOf(Ꮡm).Wait()`, and `gateOf` takes `ref var m = ref Ꮡm.Value` and then reads and CASes the struct
FIELD `m.gate` (`Volatile.Read(ref m.gate)`, `Interlocked.CompareExchange(ref m.gate, created, null)`).
The gate therefore lives in the STORAGE, not on the box — the file's own comment ("the box holds the
single shared gate") misdescribes it; the field is what holds it. **That is a hand-own header that
lies, recorded here as such** (a scope header that misdescribes its own mechanism reads as the census
to the next lane), and it is corrected IN CODE with I1's cut — the same commit that gives the file its
hand-declared `ref` primary, because a scope header is corrected in the commit that changes the scope
— not in this docs seat, which stays docs-only. Consequence: a `Lock(this ref Mutex m)` primary is
expressible, `gateOf(ref Mutex m)` reading the same field. But a hand-own does not dual-emit (B′
§4.1's XM-1 veto: hand-owned declarations are frozen at the form their author chose), so the primary
exists only if the hand-own DECLARES it, by hand, beside the ж twin — a curated change of the same
class as the atomics' signature change in §3.3.

### 2.2 The atomics are storage-only leaves

`sync/atomic/doc_impl.cs` (34 `Interlocked` sites) and `internal/runtime/atomic/atomic_impl.cs` (28)
take a box and unwrap it immediately: `CompareAndSwapUint64(ж<uint64> addr, …)` is
`Interlocked.CompareExchange(ref addr.Value, …)`, `LoadUint64` is a `Volatile.Read(ref addr.Value)`.
No site uses the box's identity. So the caller's 64 B box is a round trip the callee already undoes,
and a `ref uint64` signature removes a detour rather than adding a capability. Go declares these
functions BODYLESS (assembly), which is the converter's `X5-bodiless` veto
(`refLoweringAnalysisOperations.go:425`): "lowering the emitted partial declaration would orphan its
hand-written `*_impl.cs` companion". The veto's own comment names the remedy's shape — the hand-own
signature CHANGES (a mismatch is a hard C# error, loud), it does not gain an overload.

### 2.3 The semaphore leaf is identity-keyed

`internal/poll/runtime_sema_impl.cs:46-48`: `ConcurrentDictionary<ж<uint32>, SemaBucket> semaTable`
and `bucketFor(ж<uint32> sema) => semaTable.GetOrAdd(sema, …)`. Go keys its semaphore table on the
ADDRESS of the `uint32`; the managed port keys it on the BOX OBJECT, because a `ref uint32` cannot be
a dictionary key and managed storage has no stable address. A ref rewrite of any shape leaves this
callee with nothing to key on. Hence the split the spike banked (`14171d280`): bodyless leaves are
either **storage-only** (§2.2 — ref-lowerable in principle) or **identity** (this one — not), and the
eight `runtime_Sem*` sites leave the parameter half's population (127 → 119).

### 2.4 Phase-A lowering is same-package by design, and the caller's rendering matters too

`refLoweringEmissionOperations.go:44-48`: `refLoweredCalleePositions` returns nil "when the callee
is not a same-package lowered function (imported callees cannot lower in Phase A: scope is
unexported functions, whose callers are all within the declaring package — §10.1)", enforced twice
(the callee must be a bare `*ast.Ident`, so a qualified `atomic.LoadUint64` is a `*ast.SelectorExpr`
and returns nil; and `obj.Pkg().Path()` must equal the converting package's). Every cross-package box
in §1.1 — seven of the nine removable ones — is out of Phase A's reach for a stated safety reason:
an unexported function's callers are ALL visible to the declaring package's analysis, which is the
whole justification for lowering without a global fixpoint; an exported callee has no such closure.

And on the caller's side (`c411667e9`): `refLoweredAddressArg`'s nullable-root branch ends in
`refSplitRenderedChain` (`refLoweringEmissionOperations.go:368-371`), which requires the rendered
chain to begin with the root's BARE VALUE NAME — "any other rendering (a box form, a captured rename,
a deref wrapper) fails the split and the caller falls back" — and the fallback is
`ref (<boxed render>).DerefOrNull()`, which STILL allocates the box. `FD.Write` carries a `ref var fd
= ref Ꮡfd.DerefOrNull()` entry alias (`fd_windows.cs:725`); `incref(this ж<fdMutex> Ꮡmu)`
(`fd_mutex.cs:67`) carries none, so `&mu.state` renders as `Ꮡmu.of(fdMutex.Ꮡstate)` and the split
fails. A parameter-half increment is therefore coupled to the CALLER holding a ref — an entry alias
or a ref-receiver primary — not only to the callee's signature.

## 3. The capabilities

Each section states the mechanism, what it reaches on this row, what it needs, its cost in the
direction the cost cuts, and the prediction that stands on record before any increment is cut.

### 3.1 Capability 1 — receiver aliasing (the Phase-C aliasing field pointer)

**Mechanism.** An `Ꮡ(v.field)` taken from a `ref` receiver that ALIASES the receiver's managed
storage — an interior pointer into the struct — instead of boxing a copy of the field. This is the
capability [`DESIGN-phase-c-element-aliasing.md`](DESIGN-phase-c-element-aliasing.md) §3 sizes
against edwards25519, extended from slice elements (the element-aliasing publish gate) to struct
fields reached from a ref receiver. On the converter side it is the INVERSE of the eligibility filter
`ae444cc48` (`bodyTakesReceiverFieldAddress` + its implicit twin): that fix excludes
field-address-taking methods from ref-receiver promotion precisely because no aliasing mechanism
existed; with one, the exclusion relaxes. The filter and the relaxation are ONE design, not a revert.

**Reaches on `os`:** `file.Ꮡpfd` (1), `FD.Ꮡfdmu` (2), `FD.Ꮡl` direct (1) — **4 boxes / 256 B**.

**Needs, per box:** `FD.Ꮡfdmu` is same-package and needs nothing beyond the capability; `file.Ꮡpfd`
crosses os → internal/poll and needs the contract (§3.2) to know `FD.Write` has a ref primary;
`FD.Ꮡl` crosses internal/poll → sync AND its callee is the hand-own of §2.1, so it needs the contract
plus a hand-declared `Lock(this ref Mutex m)` / `Unlock` primary.

**Cost, in the direction it cuts.** A golib change on the `ж<T>` path: the corpus-wide byte-cost rule
applies (CLAUDE.md — a per-box instance field is +8 B on EVERY box, proportional to boxes allocated
per path; the element-aliasing publish gate is the precedent, and its unfavorable direction once
shipped unmeasured and burned an attribution run). Any cut measures the reduction it buys AND the
per-box cost it adds, on the same row, and states the direction. Plus a corpus regen (the r40
pattern) when the relaxed eligibility changes emission.

**Predictions.** I1 (same-package only, no contract): **2 boxes / 128 B on `os`** (`FD.Ꮡfdmu`) — the
attribution instrument re-run reads it directly; edwards25519 98 → ≤ 10 objects/run is Phase-C's own
acceptance, per its record. I3 (with the contract and sync's hand primary): **4 / 256 B**. A measured
I1 above 2 on `os` means the relaxation reached a box this table does not attribute to it — a per-site
re-run answers which, before any rule is widened.

### 3.2 Capability 2 — the cross-package lowering CONTRACT

**Why it is its own capability.** Read per box, receiver aliasing faces the same question the
parameter half does: how does a caller in one package know that a callee in another has a ref
primary (or a lowered parameter)? Phase A answered it by SCOPE — same-package, unexported, all callers
visible (§2.4). Seven of the nine removable boxes cross a package boundary, so the record answers it
here, before increment 1 is predicted, rather than discovering it in a second spike.

**Mechanism.** The declaring package PUBLISHES its verdicts — which methods carry a `ref` primary,
which parameter positions lowered — in its `package_info.cs` as assembly records, the shape the
`[assembly: GoImplement<T, Iface>]` records already have; a consuming package reads them at import
through the imported-alias reader (`importOperations.go:1018-1060`, `loadPackageImplementLines`)
exactly as it reads foreign implement pairs today, and binds the ref form at the call site when the
verdict says one exists. Two existing mechanisms carry it the rest of the way: `-recurse=nuget`
conversions have no dependency source on disk and read the embedded `stdlib-metadata.txt` instead
(`importOperations.go:883-897`), whose generator (`internal/stdlibmeta/generate.go:56,245,247`)
keeps exactly two line kinds by prefix today — `GoTypeAlias` and `GoImplement` — so a verdict record
is a THIRD prefix case there and rides the standing `go generate` + `TestStdLibMetadataInSync` gate;
and under layout L3 the verdict is per target (`FD.Write`'s body is per-GOOS), which the reduction
design's §3.5 already requires of every ref-lowering classification.

**The stale check, and why it is asymmetric.** The design §3.7 sketch of A′ priced this on "a
converter and its matching corpus always agree on signatures"; the contract makes that agreement
CHECKED rather than assumed, and the check has two unequal halves. A stale verdict that claims a
primary the callee no longer has fails at the CONSUMER by C#'s own overload rules — `this ref T`
cannot bind what is not declared (CS1501/CS1503), the same property B′ §4.2 proved for selection: no
silent wrong binding, the failure mode is a build error. A MISSING verdict (a primary exists, the
record does not say so) leaves the consumer boxing — safe, and SILENT. That asymmetry is the REASON
the loud check lives where it lives: on the DECLARING side, where the silent half can be made loud.

**The guard, stated as the converter-suite test it will be.** `TestPublishedRefVerdictsMatchEmitted`
(in the tier every lane already pays for, the plain `go test ./...` from `src/go2cs`, beside
`projitemsIntegrity_test` and the displacement witness): convert a fixture package carrying at least
one `ref` primary and one lowered parameter position through the `-stdlib`-shaped driver, parse the
verdict records the emitted `package_info.cs` publishes, and assert record-set EQUALITY against the
set of primaries and lowered positions the same conversion actually emitted — both directions, so a
record without an emission and an emission without a record each fail by name. Positive control: the
test neuters ONE published record (drops it from the emitted set) and must go red naming that record,
then restores byte-identically. Because the contract's silent failure is the MISSING verdict, the
guard's negative arm (an emission with no record) is the one that carries the weight, and it is the
arm a lane re-checks whenever the construct it greps for legitimately relocates (route #8). Hand-owns
publish by hand — the inverse of the `X5-hand-owned` veto, curated exactly as the linkname registries
are, because a hand-own's C# is invisible to a per-package Go scan — and the same guard covers them
through a second fixture whose primary is hand-declared, so a hand-own that gains a primary without
publishing it is caught at the converter rather than by a consumer that stays boxed.

**Under `-recurse=nuget`, stated so the design cannot be read as local-refs-only.** A published
`go.<pkg>` dependency has no `package_info.cs` on disk, and the converter reads the embedded
`stdlib-metadata.txt` for it (`importOperations.go:883-897`). The verdict records ride that file as its
THIRD line kind (the generator at `internal/stdlibmeta/generate.go:56,245,247` keeps `GoTypeAlias` and
`GoImplement` today and gains one prefix case), regenerated by the standing `go generate .` and gated
by `TestStdLibMetadataInSync` — so a corpus regen that moves a verdict without the regenerate leaves
the converter suite red at master, exactly as a moved `GoImplement` record does today, and a NuGet
consumer binds the same ref forms a source-referencing one does. The published corpus already pairs
converter and stdlib versions (`GoStdLibVersion`), so a verdict can never describe a package version
the consumer does not link.

**Governs:** `file.Ꮡpfd`, `FD.Ꮡl` ×2, `fdMutex.Ꮡstate` ×4 — seven of the nine removable boxes.

**Cost, in the direction it cuts.** Converter emit + read (a record kind, its parser, the binding
site), the metadata generator's third case, ONE corpus regen when the first verdicts publish (every
consumer of a published primary rebinds), and the guard above. Nothing on the golib path.

**Prediction: ZERO reduction on its own.** The contract removes no box; it is what lets capabilities
1, 3 and 4 reach a cross-package one. It is therefore a guard-only increment and is never scored as a
reduction — a lane that reports bytes moved by the contract alone has measured something else.

### 3.3 Capability 3 — the parameter half, restricted to storage-only leaf callees

**Mechanism.** B′-S1's parameter half (`-dual-recv-params`, `commandLineOptions.go:45`) restricted
to the population where it is known to buy something: the storage-only bodyless leaves of §2.2. The
`X5-bodiless` veto relaxes for a CURATED leaf set, the hand-own signatures move to `ref` (a mismatch
is a hard C# error, so the set cannot drift silently), the contract carries the verdicts across the
package boundary (every one of these callees is cross-package from its callers), and the caller holds
a ref — an entry alias or a ref-receiver primary — so that `refLoweredAddressArg` renders
`ref mu.state` rather than falling back to the allocating `DerefOrNull` form (§2.4).

**The measured null this section starts from.** Increment 2 as an INDEPENDENT cut removes **0 of 6**
(`14171d280`): the spike relaxed the veto for the atomics, converted `sync/atomic` + `internal/poll`,
read the sites, and found the DECLARATION had not lowered either — Phase A's same-package scope, not
the veto, is the wall — and its first null was an instrument artifact caught by checking the
declaration before interpreting the call sites. That increment is withdrawn and is not re-walked
here; its population census stands: **119 sites in 24 production files** (10 per-GOOS, 14 flat; 15 of
the 24 in `runtime`), three hand-owns gaining `ref` signatures; the ADJACENT 148 sites passing a plain
`Ꮡlocal` to the same APIs are deliberately out of scope (identical mechanism, its own increment).

**Reaches on `os`:** `fdMutex.Ꮡstate` ×4 — **256 B**. Not the two `Ꮡwsema` boxes (§6).

**Cost, in the direction it cuts.** A corpus-wide emission change (119 sites, 24 files, `runtime`
carrying most of them) for a row-local 256 B — an unfavorable ratio, stated as such; three hand-own
signature changes; and the B′-S1 machinery it rides has a measured null of its own on edwards25519
(the Phase-C record §6: zero reduction, then an invalid mixed shape), which is why this record
restricts it to leaves and does not claim the general parameter half.

**Prediction (with the contract and the caller-side ref): 4 of 6 boxes / 256 B.** Falsifier: if the
four `state` boxes do not fall once the callee is `ref`, the caller renders a ref, and the verdict is
published, then the leaf/fixpoint reading of §2.2 is wrong and the parameter half needs re-sizing
before anything else is cut on it.

### 3.4 Capability 4 — the ref-struct defer frame

**Mechanism.** golib's `defer` is `defer(Action action, ref GoFrame frame)`
(`builtin.DeferRegistrations.cs:55`), and a byref receiver cannot be bound into an `Action` — so every
DEFERRED receiver-bound call forces a heap receiver however good capability 1 is (`c411667e9`).
`GoFrame` is already a `ref struct` (`GoFrame.cs:54`) with four inline `Action?` slots (`m_d0`–`m_d3`)
and a `List<Action>` only on overflow (`:63-64`), so the frame is not the cost; the DELEGATE over the
box is. The capability is an EMISSION change: the deferred call emitted as a local function taking
`ref` to the frame's state, so a ref receiver is deferred without a delegate over a box — r39's item 3
("chip-class; do not attempt it as a golib-local edit").

**Reaches on `os`:** `FD.Ꮡl` deferred — **1 box / 64 B** on the seam — plus whatever share of the
537.8 B NONE bucket is the defer/`GoFunc` frame itself, which is **UNPRICED**: the golib-site
instrument cannot see it (§1), and pricing it needs the byte probe inside converted frames. The
record states the seam number and refuses to guess the bucket's.

**Cost, in the direction it cuts.** A converter emission change on every `defer` site in the corpus
(a regen), with the readability claim of the converted code to defend at each shape; no golib
per-box cost.

**Prediction: 1 box / 64 B on the seam.** The bucket's movement is reported as measured when the
probe exists, against no prediction, because none can honestly be made from this record's inputs.

## 4. Arithmetic against the bank condition

Ruling #1 makes zero BYTES the bar, so the arithmetic is stated against it and nothing is softened:

| | boxes | bytes |
|:--|--:|--:|
| the seam (arc 1, the typed `of()`) | 11 | 704 |
| reached by capabilities 1 + 3 + 4 WITH the contract | **9** | **576** |
| the identity-keyed boundary (§6) | 2 | 128 |
| the rest of the row, outside this record — arc 2's owning + element boxes (escape analysis), 3 non-box golib allocs, the 537.8 B NONE bucket | 6 (+ NONE) | 216 + 537.8 |

**The record's end** is its four capabilities landed and each MEASURED at the prediction its section
carries. **The arc's end** is `os` at zero bytes, which additionally needs arc 2 (escape analysis for
the owning boxes), the dead-`unsafe.Pointer` peephole and the rest of the NONE bucket, and a boundary
redesign (§6). Neither is an increment's acceptance: an increment is accepted when its own prediction
reads, and the bank condition is the arc's, never an increment's.

## 5. Increments, in the order this record recommends

Each is cut on its own branch with its prediction posted BEFORE the measurement, its footprint measured
by the two-seeded diff and applied by hunks, and CNR at the union — the standing rules, restated so the
next lane does not re-derive them.

| # | Increment | Predicted on `os` (Release + TC0) | Measured by | Cost class |
|:--|:--|:--|:--|:--|
| C0 | **The contract** — record kind, parser, binding site, metadata generator case, the declaring-side guard | **0 B** (guard-only; never a reduction) | the converter suite (guard red on a removed record) | converter + one regen |
| I1 | Receiver aliasing, same-package only | **−2 boxes / −128 B** (`FD.Ꮡfdmu`), plus the +8 B/box cost if a base-class field is added, measured on the same row | the attribution instrument, per field; edwards25519 `TestAllocations` 98 → ≤ 10 | golib `ж` path + regen |
| I3 | Receiver aliasing across the contract, incl. sync's hand-declared primary | **−2 more / −128 B** (`file.Ꮡpfd`, `FD.Ꮡl` direct) | the same instrument | hand-own + regen |
| I4 | The storage-only leaf parameter half | **−4 / −256 B** (`fdMutex.Ꮡstate`) | the same instrument; the 119-site census re-run | 3 hand-owns + corpus-wide emission |
| I5 | The ref-struct defer frame | **−1 / −64 B** on the seam; the NONE share reported, unpredicted | the instrument + the byte probe | converter emission + regen |

C0 goes first because everything after I1 depends on it and it must be scored as what it is — a
guard, not a reduction. I1 goes second because it is measurable on two rows independently (`os` and
edwards25519) without the contract, which makes it the cheapest falsification of capability 1's
premise. The order of I3–I5 is effort, not partial credit: the row does not bank until the arc's end,
and no increment is optional.

## 6. The boundary — identity-keyed leaves

`runtime_Semacquire` / `runtime_Semrelease` key on the box (§2.3). Under any ref rewrite the caller
would hold `ref uint32` and the callee would have nothing to key on, so `os`'s two `fdMutex.Ꮡwsema`
boxes (128 B) are NOT removable by capabilities 1–4, and the eight `runtime_Sem*` sites leave the
parameter half's population (127 → 119). If the row is ever to reach zero, this needs a DIFFERENT
design — an address-keyed semaphore, which is its own question about what an address means for
managed storage. Two candidates are named here so the next lane does not start from nothing, and
neither is designed: (a) a stable per-field TOKEN derived from the storage's (source object,
accessor) pair — which is the `FieldRefBox`'s own identity, i.e. the box under another name, so it
removes the allocation only if the token is a value type; (b) an inline `object` slot in the converted
`fdMutex` struct holding the bucket — a layout change to a CONVERTED type, priced against every
`fdMutex` in the corpus and against Go's `unsafe.Sizeof` expectations. Each carries a cost the record
does not pretend to know; the boundary is stated so it is not mistaken for a missing increment.

## 7. Nothing-throwaway

The arc's realized products so far are the priced census and the attribution (the instrument, its
positive control, and its two scored predictions — one HIT at 4 of 11, one FALSIFIED at 5 of 11
against a predicted 11), the increment-2 spike with its two falsified hypotheses and its population
split (storage-only vs identity), and this record. The next lane building any of the four
capabilities starts from a per-box table with the boundary each box crosses and the owner of each
callee, a measured null for the one increment that looked independent and was not, a contract whose
stale check is placed on the side where it can be loud, an arithmetic that names what the record can
and cannot reach, and predictions on record for every increment — not a headline.

## AMENDED 2026-09-04 (SUB-Q5's per-frame byte probe; coordinator dispositions `a8f4525f4`) — the row has a FLOOR of 1,320.00 B/run, golib's share is 1,032 not 920, the three unplaced objects are located, and §1's configuration-axis sentence is withdrawn

**Nothing above is rewritten.** Every figure in §1–§7 stands as what it measured — a golib-SITE
instrument reading a lower bound, sampled at 100 runs. What follows is what a byte probe inside the
CONVERTED `os`/`syscall` frames (the instrument §1 said did not yet exist) measured at
`26ff0c45b`, Release + `DOTNET_TieredCompilation=0`, **1,000,000 runs**, with segment sums closing
exactly in both units, `probe_own_bytes` = 0, a positive control that moved one segment alone, and a
non-perturbation arm identical to the un-instrumented tree. Prediction posted before the run
(`00e8128cd`), measurement `2f77a03d0`, full decomposition and the four-cell table on the BOARD under
*2026-09-04 — the `os` want-zero row has a FLOOR*.

**1. The floor, and §1's parenthetical is withdrawn.** §1 closes with "*(The board's 1,320 B/run is
the same row at Debug/tiered; the two differ on the configuration axis, not on any cut.)*" That
reading is **wrong, and the correction is the more useful fact**: at the floor (40 reps of 100 runs
per cell, minimum taken) Release/TC0, Debug/tiered and Debug/TC0 are **all 1,320.00 B/run at 17.00
objects**. The row never moved on the configuration axis. This record's **1,457.8**, and the 1,510.8
carried elsewhere, are 100-run draws ABOVE that floor — the same slop the probe saw in its own first
three samples (1,470.96 / 1,479.60 / 1,489.20). **A 100-run `AllocsPerRun` sample cannot resolve a
change under ~150 B/run**; quote the floor or a high-`runs` figure, and say which. The one cell that
genuinely differs is Release/**tiered** at 1,256.00 — see point 4.

**2. golib's share is 1,032 B, not 920.** §1's golib rows sum to 920 (704 + 88 + 128). Measured in
isolation, each **element**-box site costs **120 B / 2 objects**, not 64 B / 1 — so §1's
"element box | 2.00 | 128.0" row is **240.00 B across the two sites**, and the golib-attributable
total is **1,032.00 B / 17.00 objects**. Consequently the NONE bucket is **288.00 B/run (21.8 %)**,
not the 537.8 B / 37 % §1 and §4 carry.

**3. The three unplaced objects are LOCATED.** §1's "non-box golib (incl. pinnable slots; an UPPER
bound) | by subtraction | 3.00 | —" row no longer needs subtraction: they are **one pinnable slot at
`heap(new uint32())`** (segment 14, which is 88.00 B / 2 objects in total — matching this record's
88.0) **plus one companion object at each of the two element-box sites** (segments 1 and 32). Nothing
on this row is unattributed. The eleven `of()` field boxes measure **704.00 B exactly**, reproducing
§1.1's 704.0 **to the byte from an independent instrument** — the second derivation that makes both
readings believable, and the reason the coordinator did not require a third (`a8f4525f4`).

**4. The `of()` box unit at the floor is 64 B, and the COUNT is charged at the `new`.** The 89.7 B/box
rate realised by (b′) and banked in doctrine is corrected to **64 B/box** at the floor; the (b′) delta
is **6 × 64 = 384 B** (§1.1's segments 6 and 23 decompose to exactly 4 `of()` boxes each). Release +
**tiered** is 64 B cheaper for a mechanical reason the probe localizes: `of(FD.Ꮡl)` #1 — the box
feeding the **direct** `Lock()` — falls to 0.28 B/run because tier-1 escape analysis stack-allocates
it, while its twin feeding the **deferred** `Unlock` stays at 64.00 because the `defer` delegate
captures it. That is an independent, mechanical confirmation of §3.1/§3.4's coupling at exactly the
box pair §1.1 names — and it means **capability 1's byte payoff on this row is 4 boxes / 256 B at
Release+TC0 but 3 / 192 B at Release+tiered**, so §3.1 must not be credited twice. Critically,
`AllocationCounter` reported **1.00 object for the box that cost 0.28 B**: the count is charged at the
`new`, so **no JIT improvement can reach ruling #1's bank condition** (§4's bar) — only not
constructing the boxes, which is what capabilities 1/3/4 do. Every reduction claim on this family
therefore names its UNIT and its CONFIGURATION.

**5. §5's I5 row gets its measured number, and one item §4 does not carry at all.** I5's prediction
reads "−1 / −64 B on the seam; **the NONE share reported, unpredicted**" — that share is now measured:
the two instance-bound `Action` method-group conversions are **128.00 B/run**, and `GoFrame` itself,
its four inline slots, `frame.Run()`'s dispatch and the whole try/catch/finally are **0.00**. I5's
full value on this row is **192 B** (its 128 plus the coupled segment-10 box). Two further NONE-bucket
readings bear on §4's "the rest of the row": the dead-`unsafe.Pointer` peephole is **0.00 B/run here**
(every site is behind a false `race`/`msan`/`asan` guard and the branch folds — its value is IL and
code size, and it must not be sized against this row's byte bill), and the P/Invoke boundary is
**0.00** (the `params ...uintptr` collection materializes no heap array). What replaces them is an
item no section of this record anticipated: the **address-take PIN, 160.00 B/run (12.1 %)** —
`(uintptr)box` → `EnsureStableAddress()` → `PinnedBuffer.PinOnly(storage)` mints a fresh
`PinnedBuffer` per box per syscall (56 B element, 104 B owning, 0 for the nil `Overlapped`). It is
larger than I5's direct share, and it was given an owner the same day as a sizing after the syscall
buffer-pin cut lands.

**6. §4's arithmetic restated at the floor**, replacing nothing above:

| step | B/run | removed |
|:--|--:|:--|
| the floor, today | **1,320** | — |
| after (b′) | **936** | 6 `of()` boxes in the `rwlock`/`rwunlock` segments (384 B) |
| after I3 | **680** | `FD.Ꮡfdmu` ×2, `file.Ꮡpfd`, `FD.Ꮡl` direct (256 B) |
| after capability 4 / I5 | **488** | the two `Action` delegates (128 B) + the coupled `FD.Ꮡl` deferred box (64 B) |
| after the address-take PIN | **328** | the two `PinnedBuffer`s (160 B) |
| the arc's end | **0** | the element boxes (240 B) and the owning box (88 B) at the syscall seam |

**Two readings of that ladder against §5 and §6, both DERIVED from the segment decomposition rather
than separately measured, so they are stated with their arithmetic shown.** First, the ladder's "I3"
row is the coordinator's bundle and is **§5's I1 remainder plus §5's I3**: (b′) lands before either,
so I1's `FD.Ꮡfdmu` ×2 (§5: −2 / −128 B) is still outstanding when I3 runs, and 128 + 128 = the 256 B
above. §5's per-increment predictions are unchanged; only their ORDER against (b′) makes them read as
one step. Second, segments 6 and 23 are 8 `of()` boxes / 512 B, and §1.1 accounts for exactly those
eight as `FD.Ꮡfdmu` ×2 (128) + `fdMutex.Ꮡstate` ×4 (256) + `fdMutex.Ꮡwsema` ×2 (128) — so (b′)
removing 6 of the 8 and leaving `FD.Ꮡfdmu` ×2 means the six it removes are `state` ×4 **and `wsema`
×2**. That is §6's identity-keyed boundary: those two boxes are **not removable by capabilities 1–4**,
which stands, and (b′) is a different mechanism (the dual receiver emission, which stops reaching the
call rather than re-keying it). §6 is therefore not contradicted, but the row's path to zero no longer
runs through it — worth stating so a future lane does not re-open the address-keyed-semaphore question
believing 128 B still hangs on it. Both readings are arithmetic over two records; a lane that needs
either as a load-bearing fact measures it.

**OPEN, and not settled by this amendment:** the post-(b′) BASELINE. 936 is floor-derived
(1,320 − 384); G's (b′) acceptance run measured **972.4 B/run at 11 allocations** on a 100-run draw
and reads the excess as a real non-per-box component (the semaphore side table's `SemaBucket`/`Queue`
no longer materialising per `fdMutex`) rather than as slop. Both sides agree on the COUNT (11 → 7) and
name the same four boxes; I3's own run settles the byte unit, with G's prediction on record at
972.4 → 716.4 and its falsifier stated (fewer than four boxes moving, or any box that is not one of
the four).
