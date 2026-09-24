# DATA — the deferred alloc pins for `crypto/rand` and `sync`: prepared, and the one thing that blocks them

**Point-in-time record, 2026-09-22, lane C1.** Seat `claude/c1-alloc-pins-rand-sync` off `c6fdbe73c3`.
No manifest is written by this commit, and the reason is stated rather than worked around. Two findings
below would have produced a WRONG pin if the entries had been authored from the brief as given.

## Finding 1 — `TestMapClearOneAllocation` is a RENAME with a RELAXED want, and the corpus still has the old one

| release | declaration | assertion | failure text |
|:--|:--|:--|:--|
| go1.23.12 | `TestMapClearNoAllocations` (`sync/map_test.go:350`) | `allocs > 0` | `AllocsPerRun of m.Clear = %v; want 0` |
| go1.24.13 | **`TestMapClearOneAllocation`** (`:361`) | **`allocs > 1`** | `AllocsPerRun of m.Clear = %v; want **1**` |

`TestMapClearNoAllocations` does not exist at 1.24.13 (zero declarations). So this is a rename **with
an assertion change**, the same shape the roster's rename map already handles for
`edwards25519.TestAllocations`. Two consequences:

1. **The pin's `want` is "at most ONE allocation per run", not zero.** An entry authored from the
   sibling test's shape, or from the 1.23.12 text, would state the wrong want.
2. **The committed emission is the 1.23.12 conversion.** `src/core/sync/map_test.cs:418` still declares
   `TestMapClearNoAllocations` and prints `want 0`. Verified by reading the enclosing function rather
   than by grepping the message — the message text alone appears at `:425` and would have been
   mis-attributed to the 1.24 name.

`TestMapRangeNoAllocations` is unchanged across the two releases (same name, `allocs > 0`, `want 0`),
so it carries no rename.

**A rename-map entry is therefore OWED at the re-bank:** `sync.TestMapClearNoAllocations` →
`sync.TestMapClearOneAllocation`, a rename with a body change, which a byte-identity test correctly
refuses to pair.

## Finding 2 — the signature must be COUNT-FREE and WANT-FREE

The in-tree convention takes the invariant part of the failure text (`log`'s pin is
`" allocs, want at most 1"`). Here the want itself moves, so a want-bearing signature would go stale
the moment `sync` is reconverted at 1.24.13 — and a disclosure that cannot match is not inert, it is a
new red. The signatures to use are the distinctive count-free, want-free prefixes:

| test | signature |
|:--|:--|
| `sync.TestMapClearOneAllocation` | `AllocsPerRun of m.Clear = ` |
| `sync.TestMapRangeNoAllocations` | `AllocsPerRun of m.Range = ` |
| `crypto/rand.TestAllocations` | `allocs = ` |

`crypto/rand`'s text is `t.Errorf("allocs = %d, want 0", n)` (`rand_test.go:163`), over
`AllocsPerRun(10, …)` of `make([]byte, 32)` + `Read(buf)`.

## What blocks the three entries: the UNIT NOTE, not the count

The 2026-09-05 ladder decides the label by the METER, and its FIRST arm is an incomparable unit →
`alloc-count-semantics`, which is not a deferred pin at all. The documented discriminator is **the
run's own unit note**, not a source read: the `os` exemplar states it outright — *"DEFERRED rather than
alloc-count-semantics, and the discriminator is the run's own unit note … the host reports `counted 400
go2cs-runtime object allocations (42,400 bytes) over 100 run(s)`, so the counter SAW the allocations"*.

The lines supplied carry the test's own `Errorf` text (`allocs = 2, want 0`) but **not** the go2cs unit
note. A nonzero value does not discriminate: the shim reports a byte-derived figure precisely when its
counter saw NONE, and that figure is deliberately at least 1. So "2" is consistent with both arms.

**Three unit notes close all three entries** — the `go2cs: testing.AllocsPerRun counted …` line that
accompanied `TestXAESAllocations`' reading. Until then the ladder's fourth outcome applies (none of the
three classes established → a reading owed, and no label at all), so no manifest is written here.
Marking the reading "OWED" inside a `deferred` entry would be authoring the label the reading is
supposed to decide.

## The entries, prepared

`crypto/rand` has **no** manifest (`src/core/crypto/rand/go2cs_test_disclosures.json` absent; the
directory exists). It is minted in `sync`'s shape (`schemaVersion: 1`, `disclosures: [...]`, keys
`name`/`class`/`signature`/`reason`, plus `want`/`reading`/`plan` for a deferred entry per the `os`
exemplar).

- `crypto/rand.TestAllocations` — want: **0 allocations per run** over a 32-byte `make` plus
  `rand.Read`. reading: **2 per run** (unit note owed). plan: the want-zero arc.
- `sync.TestMapClearOneAllocation` — want: **at most 1 allocation per run** for `sync.Map.Clear`.
  reading: owed. plan: the want-zero/at-most-one arc.
- `sync.TestMapRangeNoAllocations` — want: **0 allocations per run** for `sync.Map.Range` over a
  `func(key, value any) bool` callback. reading: owed. plan: the same arc.

## The roster arithmetic, stated in full

`sync`'s manifest carries **3** pins, not 4 — `TestOnceXGC/OnceFunc`, `/OnceValue`, `/OnceValues`, all
`codegen-liveness`. The parent `TestOnceXGC` has **no entry** and is counted through the
disclosed-parent aggregation (`src/go2cs/testConversion.go:3620-21`), which is how 3 pins read as
**Disclosed 4**.

- **`sync`: 3 pins → 5, Disclosed 4 → 6.** The parent rule adds nothing here: both new entries are
  top-level declarations with no subtests. It is already accounted for once, in the existing 4.
- **`crypto/rand`: 0 pins → 1, Disclosed blank → 1.** Tests 298 unchanged.
- `Tests` unchanged on both rows.

## One observation, not a change

The batch-2 reading recorded `TestOnceXGC` and its three subtests as failing on **both** sides —
matched, not diverged — while the manifest pins all three subtests as disclosures. A disclosure pins a
Go=pass/C#=fail divergence, so either that run's Go side failed them for its own reason or the three
entries are legacy. Flagged for COORD; nothing here touches them.

---

# AMENDMENT 2026-09-22 — the three unit notes arrived; the entries are WRITTEN, in TWO classes

The blocker above is closed. The notes from the i7's s2 run at `c6fdbe73c3` decide the label per entry,
and they do not all land in the same arm — which is why the label had to wait for them.

| entry | unit note, in brief | ladder arm | class |
|:--|:--|:--|:--|
| `crypto/rand.TestAllocations` | *"counted 20 go2cs-runtime object allocations (4,424 bytes) over 10 run(s) … an allocation COUNT per run … the structural mirror of runtime.MemStats.Mallocs"* | same meter, named mechanism | **`deferred`** |
| `sync.TestMapClearOneAllocation` | *"measured 8,960 allocated BYTES over 10 run(s) … BYTES PER RUN, not an allocation count. The go2cs runtime allocation counter charged none of it"* | **incomparable unit** | **`alloc-count-semantics`** |
| `sync.TestMapRangeNoAllocations` | *"measured 1,520 allocated BYTES over 10 run(s) … BYTES PER RUN, not an allocation count …"* | **incomparable unit** | **`alloc-count-semantics`** |

**The 20-vs-2 relation, since it was flagged:** it is TOTAL against PER RUN, not two measurements. 20
counted over 10 runs is the 2 that `allocs = 2, want 0` reports. Nothing to reconcile.

**Why the two `sync` entries carry no `want`/`reading`/`plan`.** The incomparable-unit arm has *nothing
to retire*: the entry does not claim the path allocates too much, it records that the instrument cannot
answer the question the test asks. The keys follow `context`'s `TestAllocs` exemplar exactly
(`name`/`class`/`signature`/`reason`).

**And why the shim did not simply report zero**, which is the part worth keeping: its counter charged
NOTHING on both `sync` paths, so every object there was allocated outside golib — a compiler-emitted
closure in converted code, or a BCL internal. Reporting that zero would have been a **false pass**
against wants of one and zero, so the shim reports bytes instead and the row fails honestly. These two
are therefore not evidence that `sync.Map.Clear`/`Range` allocate too much; they are evidence that the
meter cannot see the path.

**`crypto/rand` is `deferred`, not `structural`, and the discriminator is a named mechanism rather than
the size of the number.** Two per run, over a `make([]byte, 32)` that does not escape plus the fill
path: nothing in the CLR's object model requires an allocation there, so the excess is reducible bridge
work and a floor claim would be unfalsifiable. Contrast
`crypto/internal/fips140test.TestXAESAllocations`, classed **structural** the same day on a 199-per-run
LOWER BOUND over a whole AES-GCM seal/open round trip with five live locals. Both are want-zero
`AllocsPerRun` asserts; what separates them is whether a mechanism that REMOVES the allocations can be
named, not how large the reading is. Stated here so the two do not read as arbitrary.

## Written by this commit

- `src/core/crypto/rand/go2cs_test_disclosures.json` — **MINTED** (the directory existed, the file did
  not), 1 entry, `deferred`, in `sync`'s shape.
- `src/core/sync/go2cs_test_disclosures.json` — **3 → 5** entries (`codegen-liveness` 3,
  `alloc-count-semantics` 2).
- `docs/ValidatedTestPackages.md` — `crypto/rand` Disclosed blank → **1**; `sync` **4 → 6**; `Tests`
  unchanged on both (298, 47); 203 rows unchanged; one sentence added to each description naming the
  new disclosures and their class, inserted before the row's first `·` marker so it follows the prose.

**Asserted by my own read, not by the guard** (`check-roster-format.ps1` is COORD's to run, pwsh): both
manifests parse as JSON; entry counts are 1 and 5; every entry has a non-empty `signature`; every
`deferred` entry has `want`, `reading` and `plan`. The signatures are want-free and count-free as
Finding 2 requires.

**STILL OWED, unchanged by this commit:** the rename-map entry at `sync`'s re-bank,
`sync.TestMapClearNoAllocations` → `sync.TestMapClearOneAllocation`, a rename with a body change.
