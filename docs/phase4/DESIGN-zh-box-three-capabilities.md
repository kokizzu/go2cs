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

## 8. AMENDMENT 2026-09-04 — I1 RETIRED on measurement, §4's arithmetic corrected 9/2 → 6/5, and the boundary's case has arrived

> Added, not rewritten (the doc-type rule). Every reading below was measured on G-LAPTOP at master
> `d188e89ed` on 2026-09-04, before any cut existed; ruled by the coordinator the same night
> (mailbox `d55ec8f9e`). Nothing in §1–§7 above is deleted: what changes is which increment goes
> first, and what §4 says is reachable.

### 8.1 The capability step this record silently assumed

§3.1 spoke of "receiver aliasing" as though a method taking a receiver-field address only needed
the eligibility filter relaxed. It needs one more thing first, and the stdlib-wide `-ref-census`
(analysis only, 13 s, corpus verifiably unchanged) is what showed it: **`fdMutex.rwlock` carries NO
declaration-level veto at all** — none of B′ §4.1's XM-1..XM-5 fires on it. Its exclusion happens at
the SELECTION stage, where arm (a) admits only methods that RETURN their receiver (the R3 ruling,
`DESIGN-zh-box-b-prime.md` §10), and `rwlock` returns `bool`. So before any field-address relaxation
can matter, primaries must extend beyond the fluent arm to non-fluent methods. That step was never
named here, and naming it is half of why I1 read as cheaper than it is.

### 8.2 Why I1 — "same-package receiver aliasing" — has no reachable case

Three readings, each independently sufficient:

1. **The `os` chain is pinned by the IDENTITY boundary, not by the package boundary.** `readLock`
   emits `Ꮡfd.of(FD.Ꮡfdmu).rwlock(true)` while already holding `ref var fd`, so the box exists only
   to satisfy `rwlock`'s `ж<fdMutex>` receiver — and `rwlock` forms three receiver-field addresses:
   `&mu.state` (cross-package atomics, capability 3) and `&mu.rsema` / `&mu.wsema`, which feed
   `runtime_Semacquire`/`Semrelease`. A `ref` receiver has no object to anchor a `ж<uint32>` on, and
   `FieldRefBox` requires one by construction (`object m_source`; its own comment: "a field in a heap
   allocated struct"). So `rwlock` cannot take a `ref` receiver at all while the semaphore keys on
   the box.
2. **The selection fixpoint cascades that upward, by its own demotion rule** (a selected method that
   calls a direct-ж method on its receiver which is not itself selected is demoted): `FD.Write` calls
   `Ꮡfd.writeLock()`, `writeLock` calls `rwlock`, `rwlock` is never selected — so `FD.Write` is never
   selected, and `os`'s `Ꮡf.of(File.Ꮡpfd).Write(b)` box sits behind the same wall.
3. **The acceptance case is cross-package too.** `edwards25519`'s point-level methods take their
   aliasing field addresses to the field ops in `crypto/internal/edwards25519/field` — a different
   package (`using field = go.crypto.@internal.edwards25519.field_package` at the head of the
   emission), so a same-package increment cannot move that chain either.

**Prediction left on record rather than erased:** I1 as scoped removes ZERO boxes on `os` and ZERO on
`edwards25519`. Its falsifier is any box removed on either row, which would mean one of the three
readings is wrong. I1 is RETIRED as a scoping — not because it is hard, but because it has no
measured case; it re-enters only if I3's or the boundary's own measurements surface one.

### 8.3 §4's arithmetic, corrected

| | boxes | bytes | why |
|:--|--:|--:|:--|
| reachable by capabilities 1 + 3 + 4 WITH the contract | **6** | **384** | `fdMutex.Ꮡstate` ×4 (capability 3, plus the caller-side entry alias `incref` lacks) and `FD.Ꮡl` ×2 (capability 1 through `sync`'s hand-declared `ref` primary — these need only the caller's existing `ref var fd` entry alias, NOT `FD.Write`'s own promotion, so they are inside the wall) |
| behind the IDENTITY boundary | **5** | **320** | `fdMutex.Ꮡwsema` ×2 (the boundary itself), `FD.Ꮡfdmu` ×2 (callee `rwlock`/`rwunlock` unpromotable), `file.Ꮡpfd` ×1 (cascaded through `FD.Write` → `writeLock` → `rwlock`) |

§4 read 9 reachable / 2 at the boundary; measured, it is **6 / 5**. The three rows that moved are
`FD.Ꮡfdmu` ×2 and `file.Ꮡpfd`, and they moved for the same reason: their callees cannot be promoted
while the semaphore keys on a box.

### 8.4 The boundary's case is the row itself

§6 filed the identity-keyed leaves as "a boundary awaiting a case". The case has arrived and it is
`os`'s own bank condition: ruling #1 makes the want-zero assert satisfiable in principle and
therefore never a disclosure, so `os` banks at ZERO bytes or not at all — and 5 of the seam's 11
boxes are now behind this boundary. **`os` cannot bank until the identity-keyed semaphore is
redesigned**, which promotes §6 from a stated boundary to the arc's objective-critical item. The two
candidate redesigns §6 names (a stable per-field token; an inline slot in the converted struct) are
sized against this measured population next; neither is built here.

The order this record recommended in §5 changes with it: the contract (C0) first as it always was,
then **I3** — cross-package receiver aliasing, whose measured reach on `os` is the `FD.Ꮡl` pair,
2 boxes / 128 B — and the boundary sized beside it rather than deferred behind the rest.
