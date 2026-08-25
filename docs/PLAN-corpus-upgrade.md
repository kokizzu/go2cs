# PLAN — the corpus upgrade ladder (Go 1.23.1 → current)

> **Status: ADOPTED — drafted 2026-08-13 as a PROPOSAL; §8's nineteen open questions RULED by the
> coordinator, 2026-08-13.** The ruling frame in §0 is coordinator-decided and treated as fixed. Where
> CLAUDE.md already settles a mechanic this document cites it rather than re-deciding it; where it did
> not, the item was an **open question** in §8, marked ⟨OQ-n⟩ at the point it arises — every ⟨OQ-n⟩ mark
> now reads as a **pointer to that question's ruling** in §8's *Ruling (coordinator, 2026-08-13)* column,
> never as an unresolved item. The prose at each ⟨OQ-n⟩ site is left exactly as drafted, so where that
> prose merely *proposes* an answer, §8's ruling is what governs.
>
> Measured-bill-first: every number below is either a measurement with its date and machine, or is
> explicitly named as unmeasured. Two whole classes of number — the hand-own census and the per-hop
> stdlib delta — are **re-measured at the hop**, never carried; the figures quoted are last-banked
> readings shown so the reader knows the order of magnitude, not values to assert against.

---

## 0. The ruling frame (fixed — amended 2026-08-13, user + coordinator)

1. **75 % (162 / 215) is Go 1.23.1's TERMINAL marker, not a waypoint.** The economics force this:
   every roster row re-derives from the new release's test sources at a hop (H10), so validation
   spent on `.1` past the credibility milestone is spent twice. `.1` remains the campaign's
   historical starting point; the validation campaign *continues* on the hop target, where the
   remaining walls are solved once and banked once.
2. Then the hop to the final 1.23.x patch (**1.23.12**) — same language, same package set — which is
   both the ladder's machinery rehearsal **and the 1.23 story's living corpus**: the push toward the
   1.23 terminal goal (every testable package validated or explicitly, permanently disclosed)
   happens there, on the release users would actually choose. The adjacent **.NET 10 hop precedes
   it** (separate hop, own gates, one variable at a time), so the ladder's differential baselines
   are derived once on the runtime they will live on.
3. Then **1.24.final** — the FIPS-140 reorg hop, which delivers the issue-#37 packages
   (`crypto/sha3`, `crypto/mlkem`, `crypto/hkdf`, `crypto/pbkdf2`, `crypto/fips140`, `weak`).
4. Then an **evidence-based decision** whether 1.25 and 1.26 are separate hops or one.
5. **EOL'd minors target their final patch; the current minor pins its latest patch at conversion time.**
6. **Branch shape.** Tag + branch `release/go1.23` at completion. Each hop lives on a long-lived version
   branch that merges master periodically. Master cuts over only at a **parity gate**: 100 % compile,
   roster % ≥ prior, full behavioral suite green, release ritual rehearsed.

This document supplies the ladder's research, the per-hop step inventory, the gate definitions, the risk
register, and the sequencing — in gate-cycles, not wall-clock.

**.NET 10 hop scope additions (2026-08-16, from the concluded bflat floor exploration —
`PLAN-bflat-perf-exploration.md`):** the hop's evaluation owns the **deployment-shape decision**
with two payoffs priced together: (1) **golib trim-safety** — stock-SDK `TrimMode=full` matches
the best measurable AOT floor (single-digit-MB binaries, ~3x-faster startup, working set near
Go's) but is gated on the trim diagnostics named by file in the perf README's Exploration
section; (2) the **self-contained single-file test host** that retires the `host-limit`
disclosure class (Roadmap, "declared host limits and their retirement path"). Disposition on
`SuppressTrimAnalysisWarnings`: it stays in the perf tree's defaults (perf publishes should not
fail on trim noise), and the trim-safety work runs its own audit build with warnings visible —
the suppression must never be the reason the diagnostics go unread. The hop's CPU measurement
should also expect real codegen gains (the exploration's one anomalous CPU row ran under
.NET-10-preview codegen and halved; unattributable there, decidable here).

**Disclosure-class retirement schedule (ruled 2026-08-19, user + coordinator):** `host-limit`
retires BY the .NET 10 hop (single-file host publish is hop scope — its retirement is a hop
deliverable). `chan-direction` was scheduled for the EARLY 1.23.12 era as the opening item of the
reflection-parity arc, on the reasoning that it buys no rows toward the 1.23.1 terminal — and it
**RETIRED EARLY, on 2026-08-20** (lane `claude/cargo-recv`), because that premise expired twice: the
class turned out to GATE the `text/template` and `html/template` rows, and its remedy was the
prerequisite for the `reflect.Value.Recv` bridge rather than a later improvement on it. The plumbing
prediction held exactly — direction is carried the way array dims are, at the same finite set of
positions. Both classes are hop-stable and self-retiring, so early landings force themselves out
loudly rather than lingering, which is what happened here.

---

## 1. The version ladder — research

### 1.1 The versions, measured

Read 2026-08-13 from [go.dev/doc/devel/release](https://go.dev/doc/devel/release) and cross-checked
against [endoflife.date/go](https://endoflife.date/go).

**As-of 2026-08-13, and stale by construction.** The table below is a dated reading, not a set of
targets to convert against — the section's own closing instruction (*"Re-read this table at the hop"*,
⟨OQ-1⟩) is the operative rule, and hops C and D re-read before they start. Hops A and B are the two
whose patch series are **closed**, so only their rows can be trusted to have held.

| Minor | Released | Final / latest patch | Patch date | Status | Ladder role |
|:--|:--|:--|:--|:--|:--|
| **1.23** | 2024-08-13 | **1.23.12** | 2025-08-06 | **EOL** 2025-08-12 (at 1.25.0) | current pin is `1.23.1`; rehearsal target is `1.23.12` |
| **1.24** | 2025-02-11 | **1.24.13** | 2026-02-04 | **EOL** 2026-02-10 (at 1.26.0) | hop 2 — the FIPS-140 reorg |
| **1.25** | 2025-08-12 | **1.25.12** | 2026-07-07 | supported | hop 3 (or 3a) |
| **1.26** | 2026-02-10 | **1.26.5** | 2026-07-07 | supported — **current** | hop 3b (or folded into hop 3) |

Go's policy: *"Each major Go release is supported until there are two newer major releases"*
([release history](https://go.dev/doc/devel/release)). That is what makes rule 5 mechanically safe for
1.23 and 1.24 — their patch series are **closed** and their targets can never move under the plan. It is
equally what makes 1.25 and 1.26 **moving targets**: 1.25.12 and 1.26.5 are today's readings, both dated
2026-07-07, and the monthly-ish cadence means both will have advanced before those hops start. **Re-read
this table at the hop; do not convert against the number written here.** ⟨OQ-1⟩

⚠ One rehearsal-relevant consequence of rule 5: the rehearsal hop is `1.23.1 → 1.23.12`, an
**eleven-patch** jump, not a one-patch jump. It is still the cheapest hop available (no language change,
no package added or removed) but it is not a null hop — eleven patch releases of security and runtime
fixes touched real source, and the goldens will move somewhere.

### 1.2 Per-minor standard-library delta

Sources: [Go 1.24 release notes](https://go.dev/doc/go1.24), [Go 1.25](https://go.dev/doc/go1.25),
[Go 1.26](https://go.dev/doc/go1.26).

#### Go 1.24 — the big one

**Packages ADDED (visible by default):** `weak` · `crypto/mlkem` · `crypto/hkdf` · `crypto/pbkdf2` ·
`crypto/sha3` · `crypto/fips140`. These are exactly the six that
[`FINDING-toolchain-goroot-divergence.md`](phase4/FINDING-toolchain-goroot-divergence.md) §3
identifies as the reporter's NU1101 wall — `go.crypto.sha3` 404s on nuget.org today because no 1.23.1
conversion ever contained it. Note `weak` is the **promotion** of the existing `internal/weak`, not a new
implementation; the corpus already hand-owns `internal/weak/pointer.cs` (joined the marker census at
r43e), so this package arrives with a hand-own attached to its ancestor.

**Package added behind an experiment:** `testing/synctest` (`GOEXPERIMENT=synctest`) — not in the default
package set, therefore **not** in a default `-stdlib` queue. ⟨OQ-2⟩

**The unmeasured cost centre — the FIPS-140 internal reorg.** The release ships "Go Cryptographic Module
v1.0.0" and moves the crypto primitives under a new `crypto/internal/fips140/...` tree. The release notes
do not enumerate that tree, and **this plan does not guess its size**: the number is produced by the hop's
own census (§3, step H3), and it is the single largest unknown on the ladder. Expect the 302-package
corpus to grow by materially more than the six named packages, concentrated entirely in `crypto`.

**Removed / deprecated that will move test suites:** `crypto/tls` drops `X25519Kyber768Draft00`; the
`x509sha1` GODEBUG is removed and `x509.Certificate.Verify` no longer accepts SHA-1 signatures;
`runtime.GOROOT()` and `crypto/cipher.NewOFB`/`NewCFBEncrypter`/`NewCFBDecrypter` are deprecated (still
present). `crypto/aes.NewCipher` stops implementing its undocumented `NewCTR`/`NewGCM`/`NewCBC*` methods —
**a shape change the converted `crypto/aes` suite validates today** (13 rows, banked).

**API changes with the widest blast radius across the banked roster:**
- `encoding.TextAppender` / `encoding.BinaryAppender` — new interfaces, implemented across `hash/*`,
  `crypto/*`, `log/slog`, `math/big`, `net`, `net/netip`, `net/url`, `regexp`, `time`. Every one of those
  gains method surface, and several are banked packages.
- `os.Root` / `os.OpenRoot` — a new type on the most platform-entangled package in the corpus.
- `runtime.AddCleanup` — new finalization mechanism.
- `testing.B.Loop`, `T.Context`, `B.Context`, `T.Chdir`, `B.Chdir` — **these land in the hand-owned
  `src/core/testing` host**, which is not converted and therefore does not follow the corpus
  automatically. This is a mandatory hand-own work item, not a re-audit finding. ⟨OQ-3⟩
- `sync.Map` reimplemented on a hash-trie map (`GOEXPERIMENT=nosynchashtriemap` reverts) — the corpus
  already hand-owns `internal/concurrent/hashtriemap.cs` (joined at r39d), so the 1.24 `sync` is closer to
  what the corpus already carries, not further.
- `strings`/`bytes` iterator families (`Lines`, `SplitSeq`, `SplitAfterSeq`, `FieldsSeq`,
  `FieldsFuncSeq`) — both are banked packages with large suites (`bytes` 82, `strings` 68 at bank time).
- `encoding/json` `omitzero`; `math/rand` top-level `Seed` becomes a no-op; `crypto/rand.Read` is
  guaranteed not to fail (**it crashes the program instead** — a behavioral change with an obvious managed
  analogue question).

**Toolchain surface:** `go.mod` `tool` directives and `go tool`; `go build -json`; `GOAUTH`; cgo
`#cgo noescape`/`nocallback`; `go:wasmexport`. None of these are converter inputs today, but the `tool`
directive is a new `go.mod` verb — and `modfile.ParseLax` silently drops unknown verbs (the trap recorded
in FINDING-toolchain-goroot-divergence §6), so the converter's go.mod readers should be re-checked against
it rather than assumed safe.

#### Go 1.25 — small package delta, one nasty behavioral change

**Packages added:** `testing/synctest` **graduates** to the default package set.
`encoding/json/v2` and `encoding/json/jsontext` exist only under `GOEXPERIMENT=jsonv2` — **not** default.

**Language changes: none.** The spec dropped the "core types" notion in favour of prose; no program
changes meaning.

**The behavioral change that matters most to a differential harness:** a compiler bug present since Go
1.21, which incorrectly *delayed* nil-pointer checks, is **fixed**. Programs that relied on it now panic
where they previously did not. Any converted test whose Go-side baseline changed panic behaviour between
1.24 and 1.25 re-derives its verdict — and, worse, the C# side has never had the bug, so a differential
row that *matched* under 1.24 by both sides being wrong in the same direction could newly diverge, or a
previously-disclosed divergence could silently close. Both directions must be classified, not celebrated.

**Deprecations that touch converted packages:** `go/ast.FilterPackage`, `PackageExports`,
`MergePackageFiles`, `MergeMode`; `go/parser.ParseDir`. `crypto/elliptic` **removes** the undocumented
`Inverse` and `CombinedMult`.

**Other API adds across banked/near-banked packages:** `hash.XOF`, `hash.Cloner` (every stdlib `Hash` now
implements `Cloner`); `io/fs.ReadLinkFS`; `os.Root` gains eleven methods; `reflect.TypeAssert`;
`sync.WaitGroup.Go` (**the hand-owned `sync` again**); `testing.T/B/F.Attr` and `.Output` (**the hand-owned
test host again**); `testing/fstest.MapFS` implements `ReadLinkFS` and `TestFS` stops following symlinks;
`unicode` gains `CategoryAliases`, `Cn`, `LC` and `C` now includes `Cn` — a **table regeneration**, and
`unicode` is a banked package whose suite compares tables.

**Runtime defaults that change measurement, not correctness:** container-aware `GOMAXPROCS` (Linux cgroup
limits, periodic re-read); DWARF 5 by default; `GOEXPERIMENT=greenteagc` available.
`testing.AllocsPerRun` now **panics** if parallel tests are running — directly relevant to the
`alloc-profile` disclosure class.

#### Go 1.26 — two language changes, and they are converter work

**Language changes (both are converter work, see §1.3):**
- `new()` accepts an **expression**: `new(yearsSince(born))`.
- **Self-referential generic type constraints**: `type Adder[A Adder[A]] interface { Add(A) A }`.

**Packages added:** `crypto/hpke` · `crypto/mlkem/mlkemtest` · `testing/cryptotest`. Behind experiments:
`simd/archsimd` (`GOEXPERIMENT=simd`) and `runtime/secret` (`GOEXPERIMENT=runtimesecret`) — neither
default.

**Removals:** `cmd/doc` / `go tool doc`; the **`windows/arm` port**; the historical `go fix` fixers. The
old `testing/synctest` API is removed (it was kept alongside the graduated one through 1.25).

**Deprecations of a distinctive shape:** across `crypto/dsa`, `crypto/ecdh`, `crypto/ecdsa`,
`crypto/ed25519`, `crypto/rand.Prime` and `crypto/rsa`, the **`io.Reader` randomness parameter is now
IGNORED**; determinism for tests moves to `testing/cryptotest.SetGlobalRandom`. `crypto/dsa` is a banked
package (and one of the four carrying a long-timeout floor in `run-validated-sweep.ps1`) — its suite's
determinism mechanism changes wholesale.

**Behavioral changes that will move goldens or verdicts:** `image/jpeg` gets a new encoder/decoder that
"may differ in bit-for-bit output"; `net/url.Parse` rejects malformed colon-in-host URLs
(`urlstrictcolons=0` reverts); `net/http`'s `ServeMux` trailing-slash redirect becomes **307**, and its
`Client` scopes cookies to `Request.Host`; `errors.AsType[E]`, `bytes.Buffer.Peek`, `log/slog.MultiHandler`,
`reflect` iterator methods (`Type.Fields/Methods/Ins/Outs`, `Value.Fields/Methods`) are added.
`io.ReadAll` is ~2× faster and allocates ~50 % less — an **allocation-shape change in a package the
`alloc-profile` disclosure class lives next to**.

**Runtime:** Green Tea GC **on by default** (`nogreenteagc` reverts, removal expected 1.27); heap base
address randomization on 64-bit.

### 1.3 Language changes the converter must absorb

| Release | Change | Converter impact |
|:--|:--|:--|
| 1.24 | **Generic type aliases** — a type alias may be parameterized | Real work. `go/types` must *materialize* `Alias` and expose `Alias.TypeParams` / `Alias.TypeArgs`; the emitter must map a parameterized alias onto C#. C# **has** generic `using` aliases only in narrow forms, so the likely mapping is the existing type-alias machinery (`ImplicitConvGenerator`) extended, or a generic `struct`/`record` shim. **Unscoped here** — it wants its own DESIGN. ⟨OQ-4⟩ |
| 1.24 | `GOEXPERIMENT=noaliastypeparams` escape hatch | **Removed in 1.25** — so there is no "convert 1.24 with generic aliases disabled" fallback beyond the 1.24 hop itself. |
| 1.25 | *(none)* | The 1.25 hop is a pure library/behavior hop for the converter. |
| 1.26 | `new(expr)` | `convCallExpr` / the `new` builtin path must accept an expression operand and emit an initialized box rather than a zero value. Small, but it is a **builtin semantics change**, not a syntax nicety. |
| 1.26 | Self-referential generic constraints (`type Adder[A Adder[A]] interface`) | Touches `constraintOperations.go` — the pass that already had a subset-of-operator-sets bug fixed in Phase 2. A self-referential constraint is a cycle the C# generic constraint system expresses natively (`where A : IAdder<A>`), so the mapping is plausible; the **cycle detection** in the existing pass is the suspect. |

The 1.23 → 1.23.12 rehearsal hop has **zero** language delta. That is the point of it.

### 1.4 The toolchain question — what go2cs itself must build with

This is the section with the most load-bearing finding on the page.

**Measured facts (2026-08-13, this checkout).**
- `src/go2cs/go.mod`: `module go2cs`, `go 1.23.1`, `golang.org/x/mod v0.24.0`, `golang.org/x/tools v0.31.0`.
- Host toolchain: `go version go1.23.1 windows/amd64`.
- `src/version.props`: `<GoStdLibVersion>1.23.1</GoStdLibVersion>`, `<GoBuildNumber>6</GoBuildNumber>`
  → published `1.23.1.6`.
- `conversionDriver.go:88-89` loads with **`packages.LoadAllSyntax`**.

**Consequence — the converter's own `go/types` is the type-checker.** `LoadAllSyntax` type-checks the
whole closure **from source**, using the `go/ast` + `go/parser` + `go/types` that were **compiled into
`go2cs.exe`**. So the rule is hard, not preferential:

> **To convert Go 1.N sources, `go2cs.exe` must be BUILT with a Go toolchain ≥ 1.N.**

A 1.23.1-built converter cannot parse a 1.24 generic type alias — `go/parser` rejects it — and cannot
represent it if it did. This is why the toolchain bump is step H1 of every hop and not a housekeeping
item.

**The export-data policy is a second, weaker constraint.** `golang.org/x/tools/go/gcexportdata`
[documents](https://pkg.go.dev/golang.org/x/tools/go/gcexportdata) that `Read` supports export data
produced by *"only the last two Go releases plus tip."* Because go2cs type-checks from source, this is
**not** on the `-stdlib` critical path — but it *is* on the path for `go/internal/gcimporter`'s converted
test suite, and it bounds how far x/tools may lag the toolchain. `x/tools v0.31.0` is a Go-1.24-era
release; it will not carry the ladder to 1.26. **x/tools and x/mod bump with each hop**, and that bump is
itself a converter change owing a CNR. ⟨OQ-5⟩

**Release tags are a silent emission channel.** `directiveOperations.go`'s `releaseTagsForVersion`
expands `go env GOVERSION` into the `go1.1 … go1.N` build-tag set. Hopping the toolchain therefore
changes **build-constraint evaluation** — which files each stdlib package includes — with no converter
edit at all. Every `//go:build go1.24` guard in the Go tree flips on at the 1.24 hop. This is the
mechanism by which a hop moves emitted C# even where the Go source did not change.

#### 1.4.1 The L8 pin guard — how the hop interacts with it (read this before scheduling H1/H2)

L8 landed (`3e440711f`). Read from `src/go2cs/toolchainResolution.go`:

- **`checkCorpusToolchainPin(mode, convertingVersion, pinnedRelease)`** refuses `-stdlib` and `-tests`
  when the Go tree being read is not the release `version.props` pins. The comparison is the **FULL
  release, patch included**, and it is **exact equality, not a floor** — the source comment is explicit
  that *"a pin is not a floor, and a corpus converted by a NEWER toolchain is no more measurable than one
  converted by an older one."* The converting release is read from **GOROOT's own `VERSION` file**, with
  `go env GOVERSION` as fallback.
- The pinned release comes from `corpusPinnedRelease(root)` — `<GoStdLibVersion>` read from **the resolved
  root's** `version.props`, not from a fixed path. An absent file returns `""` and never refuses.
- **`checkNuGetStdLibCompatibility`** is the sibling guard for `-recurse=nuget` and deliberately compares
  **`version.Lang` only** (`1.23.1` ≡ `1.23.5`), because the floating NuGet revision already spans patches.
  Its "published release" is `publishedStdLibRelease()` = **`runtime.Version()` of the go2cs binary**.

Three things follow, and they are the plan's tightest sequencing constraints.

1. **The rehearsal hop DOES trip the guard.** `1.23.1 → 1.23.12` is a patch move and
   `checkCorpusToolchainPin` compares patches. Good: the rehearsal exercises the guard for real instead of
   sliding under it.
2. **The guard's own error message prescribes the hop's opening move** — verbatim: *"if the corpus is
   deliberately moving to X, bump `<GoStdLibVersion>` to X first."* So **H2 (bump `version.props`)
   precedes H3 (reconvert)**, and that ordering is sanctioned by the code, not invented here.
3. **The A/B needed by the hand-own re-audit (§3) is still possible**, because the pin is read from *the
   output root's* `version.props`. Seed the OLD-Go staging root with a `version.props` pinned to the old
   release and the NEW-Go staging root with one pinned to the new release; each run passes its own guard.
   Do **not** try to run both against the repository's single `version.props`.

#### 1.4.2 ⚠ NEW FALSE-GREEN ROUTE — a toolchain hop does not invalidate `go2cs.exe`

CLAUDE.md catalogues three false-green routes. **A toolchain hop opens a fourth instance of route #1, and
the existing mitigation does not cover it.**

Every harness (`BehavioralTestBase`, `BehavioralRunner`, `PerformanceRunner`) rebuilds `go2cs.exe` when
**any converter `*.go` is newer than the binary**, and CNR re-transpiles unconditionally with whatever
binary it finds. A toolchain hop **touches no converter `.go` file**. So after installing Go 1.24 and
bumping `version.props`, every up-to-date predicate says "current" and every gate keeps running a binary
that embeds **Go 1.23.1's `go/parser` and `go/types`** — i.e. the old standard library's front end — while
`go list` hands it 1.24 sources. The symptom is not a clean failure: the old parser will reject or
mis-parse the new constructs and the run degrades into the converter's *best-effort* "did not fully
type-check" path, which CNR reports as **NOT MEASURED** (good) but which the runners do not.

**Remedy (H1.4) — CLOSED (2026-08-24, per ⟨OQ-6⟩, in an equivalent form).** The stamp turned out to be
unnecessary: every Go binary **already** embeds the release that built it, so nothing needed stamping.
`src/tests/ConverterBuildInputs.cs` reads it back and compares it against the live `go env GOVERSION` in
the ONE shared helper all three rebuild predicates delegate to, failing stale-wards (an unreadable stamp
or an unanswerable `GOVERSION` forces the rebuild). A toolchain hop now invalidates `go2cs.exe`
automatically, no explicit `go build` is owed, and no gate can run against a stale converter. The same
helper closed route #5 (the `//go:embed` assets) with it. Full statement:
[`GoCorpusMigration.md`](GoCorpusMigration.md) §1.2. ⟨OQ-6⟩

---

## 2. What one hop is — the step inventory

Every hop runs H0 → H12 in order. Steps marked **GATE** are pass/fail and block the next step. Steps
marked ⟲ are re-measured at each hop and never carried forward.

### H0 — Branch and baseline capture
Cut `upgrade/go1.NN` from master. Capture, on the **outgoing** toolchain and the **new** converter build,
the artifacts the hop will diff against: the `.cs.auto` baseline (§3), the package census, the roster
snapshot, the disclosure manifests. ⟲

⚠ **`.cs.auto` staleness is a prerequisite, not a finding.** The `-stdlib` overlay rule excludes
`*.cs.auto` in order to protect the hand-owned `.cs` beside it, so the tracked `.cs.auto` siblings are
frozen on their own schedule — 11 of 16 were stale at r40; r49a's control measured 12 content differences
and classified all 12 as exactly this (CleanupBacklog item 18). **A stale baseline poisons the first
hop's differential.** Either level item 18 immediately before H0, or generate the baseline fresh in H0
from a seeded old-Go regen rather than reading the committed siblings. The second is strictly safer and
is what this plan proposes. ⟨OQ-7⟩

### H1 — Toolchain provisioning **GATE**
1. Install the target Go release side-by-side; confirm the target **executes** — `bin/go version`
   OUTPUT, per [`GoCorpusMigration.md`](GoCorpusMigration.md) H1 (the `VERSION` file is **not** a
   verification; a `GOTOOLCHAIN` pin redirects silently — measured 2026-08-24, hop-A provisioning,
   [`phase4/STAGE0-provisioning.md`](phase4/STAGE0-provisioning.md)).
2. Bump `src/go2cs/go.mod`'s `go` directive ⟨OQ-8⟩ and the `golang.org/x/tools` / `golang.org/x/mod`
   requirements to releases contemporary with the target (§1.4's export-data policy bounds the lag).
3. `go build` the converter **on the new toolchain**; `go test ./...` green (200 s solo / 332 s loaded on
   the i7-5820K at the current tree — budget 3–4× any i9 figure).
4. **Close the route-#4 hole** (§1.4.2) before any harness runs.

**Gate:** converter unit tests green **and** `go2cs.exe` demonstrably built by the new toolchain.

### H2 — The pin bump **GATE**
Bump `<GoStdLibVersion>` in `src/version.props` to the exact target release. Decide `<GoBuildNumber>`
policy at the same moment ⟨OQ-9⟩. Nothing else in the repo changes in this commit — it is deliberately a
one-line, reviewable, revertible move, and it is what unblocks `-stdlib`/`-tests` under
`checkCorpusToolchainPin`.

**Gate:** a single-package `-stdlib` smoke conversion no longer refuses.

### H3 — Package census ⟲
Run the conversion queue and diff the package set against the outgoing corpus: **added**, **removed**,
**renamed/promoted** (e.g. `internal/weak` → `weak`), **experiment-gated and therefore absent**. This is
the hop's size estimate and the input to every subsequent step. For the 1.24 hop this is where the
`crypto/internal/fips140/...` tree gets its real number — §1.2 deliberately does not guess it.

Deliverable: a `CENSUS-go1.NN-packages.md` under `docs/phase4/`, in the shape of the existing census docs.

### H4 — Converter feature work **GATE**
Whatever §1.3 names for this hop, plus whatever H3's census surfaces. Each item follows the standing
repo discipline: root-cause against emitted `.cs`, land a behavioral regression test, update
`ConversionStrategies-Reference.md` (and the summary only if the headline mapping moved), prove
`check-no-regression` clean **on the outgoing corpus** where the change is supposed to be neutral.

**Gate:** CNR byte-identical over the full behavioral corpus (≈580 packages at the last measure), zero
NOT MEASURED. Budget 1,505 s solo / ~3,190 s with two sibling lanes on the i7-5820K.

### H5 — Seeded full reconvert **GATE**
Exactly CLAUDE.md's reconvert ritual, unchanged:
seed `<tmp>/src/core` + `<tmp>/src/version.props` + `<tmp>/docs/validation` mirroring the `src/` layout →
`go2cs -stdlib -comments -go2cspath <tmp>/src` → path-precise marker gate → overlay `.cs`/`.csproj`/
`README.md` excluding `*.cs.auto`.

Non-negotiables carried verbatim from CLAUDE.md, because a hop is the *most* likely moment to skip one:
- **Seed first.** An unseeded root clobbers every whole-file hand-own with an auto conversion that
  compiles and is operationally broken, and — since L3 — also breaks layout adoption.
- **Never convert twice into one temp root**; delete and re-seed per run; confirm no `go2cs.exe` is alive.
- **Marker gate is PATH-PRECISE, line-anchored, and re-measured.** ⟲ Measured on this checkout
  **2026-08-13**, and it has already moved off the last banked reading:

  | Quantity | r59 banked (2026-08-11) | Measured 2026-08-13 |
  |:--|--:|--:|
  | Line-anchored `[module: GoManualConversion]` files | 49 | **53** |
  | `*_impl.cs` companions | 41 | **42** |
  | Tracked `*.cs.auto` review siblings | 16 (r40) | **19** |
  | Converted package `.csproj` (excl. `*.tests.csproj`) | — | **306** |
  | Banked `*.tests.csproj` | — | **130** |

  Two things worth carrying. **First, the doctrine is vindicated in two days:** the marker census moved
  49 → 53 between a Saturday bank and a Monday reading, which is precisely why CLAUDE.md says re-measure
  and never assert last session's number. **Second, a free corroboration:** the 130 banked
  `*.tests.csproj` exactly equals the 130 roster rows in `ValidatedTestPackages.md`, which is the
  committed-evidence half of the green-badge rule (a badge is green only when the committed
  `.tests.csproj` and the proof page agree). If a hop ever ends with those two numbers unequal, the
  badge census will miscount — loudly, by design — and that is a cheap arithmetic check to run at H10.
  *(The "302 converted packages" figure in CLAUDE.md's prose is older than the 306 package `.csproj` on
  disk; H3's census re-derives the package set anyway, so treat neither number as the ladder's input.)*
- The three **hand-owned-by-consequence** packages (`internal/concurrent`, `internal/godebug`,
  `internal/weak`) never re-emit their `.csproj`/`package_info.cs`/`README.md`. A hop that *adds* a
  package to this class must notice.

**Gate:** overlay completes with the marker gate at zero violations and every diff classified (§7 risk R1).

### H6 — The hand-own re-audit ⟲ **GATE** — *see §3, this is the mandatory step*

### H7 — Compile parity **GATE**
Full `go2cs-stdlib.slnx` build, `-p:UseSharedCompilation=false`, zero errors, skipped-dependents checked
(a dependent of a failed project is skipped, not errored — count them). Last measured 92–188 s on the i9;
budget 3–4× on the i7-5820K class.

**Gate:** **100 % of the hop's package set compiles.** Not "as many as before" — 100 %, per the frame.

### H8 — Multi-platform L3 re-emission **GATE**
`go2cs -stdlib -comments -platforms windows/amd64,linux/amd64,darwin/amd64 -go2cspath <repo>\src`
(~545–560 s measured at r50a/r51b for three targets). A hop changes the platform axis in two ways at
once: new packages may be platform-varying, and existing ones may stop being so. Re-run
`-platform-census` and diff the manifest against the outgoing one; the **37 L3 packages** figure is a
measurement, not a constant. ⟲

The **1.26 hop has a named L3 consequence**: `windows/arm` is removed. That is a GOARCH the corpus does
not currently target, so the expected impact is nil — but it is the first hop where a *port* disappears,
and the census should say so explicitly rather than be silent.

**Gate:** the platform manifest's marker gate is zero per target; `-p:GoTargetOS=windows` reproduces the
default build byte-for-byte (the control r48a established).

### H9 — Behavioral golden rebank **GATE**
Behavioral goldens are conversions of go2cs's *own* Go programs, so a stdlib hop reaches them by three
channels — and all three are real:
1. **Imported type aliases.** A test's emitted `.cs` reads each imported package's `package_info.cs` to
   mint its `<ImportedTypeAliases>` block. A hopped `fmt`/`strings`/`time` moves that block.
2. **Release tags** (§1.4) change which files a referenced stdlib package includes.
3. **Go-side stdout** in `OutputComparisonTests` — 1.25's nil-check fix and 1.26's `image/jpeg` rewrite are
   the two known candidates.

Procedure: re-transpile everything (CNR or a runner pass — `UpdateTestTargets --createTargetFiles` copies
on-disk `.cs`, it does **not** re-transpile), then `UpdateTestTargets --createTargetFiles`, then classify
**every** moved golden before banking. A hop is not a licence to rebank unexamined diffs.

**Gate:** full `run-behavioral.ps1` (4 phases) green — transpile + compile + target + output. Budget from
the top of the range and note `BehavioralRunner`'s own internal budgets are independent of the caller's
(`--build-timeout` 2400 s default, etc.); a slow host reports `NOT MEASURED`, which fails the run and must
never be read as a corpus regression.

### H10 — Roster / proof-page / disclosure migration ⟲ **GATE** — *the hop's largest step*
**Every banked `_test.go` suite changes between minors, so every roster row re-validates from scratch.**
There is no "carry the row forward" path: the row's numerator, denominator and disclosure set are all
derived from the new release's test sources.

Per banked package:
1. Re-run `go2cs -tests -test-action all -test-timeout <n>` against the new GOROOT package.
2. Re-derive the verdict count. **The denominator moves** — tests are added and removed every release.
3. Re-derive the disclosure manifest. Disclosures are **pinned by exact failure signature**, so a
   renamed or reworded test invalidates its pin and the manifest must be re-signed rather than edited.
   The 1.26 crypto "randomness parameter is ignored" change (§1.2) rewrites `crypto/dsa`'s determinism
   mechanism outright.
4. Regenerate the proof page under `docs/validation/current/<dot-id>.md`, and let the README Tests badge
   recompose from it.
5. Re-check the four long-timeout floors in `run-validated-sweep.ps1` (`hash/maphash`,
   `index/suffixarray`, `crypto/dsa`, `archive/zip`) — a hop can change a suite's cost.

**Gate:** roster % **≥ prior hop's %**. ⟨OQ-10⟩ — the *denominator* (215 testable at 1.23.1) is itself
re-derived at each hop from the new release's `func Test…` census, so "≥ prior" needs a ruling on whether
it compares percentages or absolute package counts. Percentages can fall while absolute counts rise (a
release that adds testable packages faster than the ladder validates them); absolute counts can fall
legitimately when upstream deletes a package.

Budget: the full sweep measured 3,138 s at 109 packages / 13,611 verdicts on the i9; the roster is now
**130 / 215 (60.5 %, 14,764 matching verdicts, 47 disclosed, updated 2026-08-13)**. At the completion bar
(162) on the i7-5820K class this is plausibly a **multi-hour, background, coordinator-owned** run — and
per CLAUDE.md a lane that parks a detached sweep and ends its turn gets it killed.

### H11 — NuGet versioning + compat guards **GATE**
- `version.props` is already bumped (H2). The published version becomes
  `<GoStdLibVersion>.<GoBuildNumber>` = e.g. `1.24.13.1`.
- **Ordering claim to verify, not assume:** NuGet compares version components numerically, so
  `1.23.12.1 > 1.23.1.6` (Patch 12 > 1) and `1.24.13.1 > 1.23.12.n`. Monotonicity therefore holds across
  the whole ladder *if* the four-part form is preserved. Verify with `NuGetVersion.Compare` before the
  first hop publishes; a non-monotonic sequence on a public feed is not correctable. ⟨OQ-11⟩
- **`checkNuGetStdLibCompatibility` moves with the hop for free**, because
  `publishedStdLibRelease()` reads `runtime.Version()` of the converter binary — which H1 rebuilt. But
  that coupling has a sharp edge: between H1 and H2 the binary claims the new release while
  `version.props` still names the old one, so a `-recurse=nuget` run in that window refuses legitimate
  old-pin modules and accepts new-pin ones for a corpus that does not exist yet. **H1 → H2 must be a
  single reviewable pair, not two independently-mergeable commits.**
- The design note flagged in FINDING-toolchain-goroot-divergence §5B is now *more* pressing, not less:
  `publishedStdLibRelease()` is blind to what is actually on nuget.org, so during a hop the guard will
  happily approve a release that has been built but never published. ⟨OQ-12⟩
- New packages need new package IDs (`go.crypto.sha3`, `go.weak`, …). Removed packages need a
  **deprecation/unlisting policy** — a consumer pinned to `go.crypto.elliptic`'s removed methods is not
  served by silence. ⟨OQ-13⟩

### H12 — Docs, badges, READMEs **GATE**
- The **Docs** and **Source·Go** badges read the toolchain (`go env GOVERSION`, GOROOT's
  `src/vendor/modules.txt`) and follow the hop automatically. The **Tests** and **Source·C#** badges read
  `version.props` and follow H2. So a hop moves **all four badges on ~305 READMEs** as a matter of course
  — the diff is enormous and entirely expected, and its expected size should be stated *before* the
  overlay so it is not mistaken for drift.
- The **eight hand-owned READMEs** (`internal/godebug`, `internal/concurrent`, `internal/weak`, `unsafe`,
  `testing`, `crypto/x509/internal/macos`, `internal/runtime/syscall`,
  `vendor/golang.org/x/net/route`) do **not** follow. They are hand-edited, and per refinement 14 the
  edits are *derived and proved against the converter's own output*, never typed. Re-run that derivation
  as a control at each hop.
- The **19 GOROOT-vendored `golang.org/x/*` packages** re-pin from the new GOROOT's `modules.txt`.
- `docs/README.md`, `docs/Roadmap.md`, `docs/ValidatedTestPackages.md`, `CLAUDE.md`'s architecture row and
  the NEWS entry all carry the Go version in prose.
- **Release ritual rehearsal:** `push-nuget.ps1 -WhatIf`-equivalent dry run, exercising the pre-pack
  signed `nuget-<version>` tag, the write-once `docs/validation/<version>/` snapshot, the badge retarget
  (both halves — Tests proof link **and** Source·C# tag+message), and the recomputed re-verification pass.
  The frame requires the ritual **rehearsed** at the parity gate; whether the hop also **publishes** is
  ⟨OQ-14⟩.

---

## 3. H6 — The hand-own re-audit, driven by `.cs.auto` (mandatory, per user steer)

This is the step that distinguishes a corpus *upgrade* from a corpus *regeneration*, and it is the one a
hop is most likely to skip because everything compiles without it.

### 3.1 The failure mode being defended against

A hand-owned file is frozen at the semantics of the Go release it was written against. When upstream
**adds** code inside that file — a new branch, a new field, a new call, a hardening fix — the hand-own
does not receive it. Nothing fails: the file is excluded from the convert set by
`containsManualConversionMarker`, the corpus compiles clean, the behavioral suite is green, and the
package's own test suite may not cover the added path. **The defect is silent and operational, and it
surfaces later as an inexplicable divergence in a package nobody was working on.**

Newly-added upstream code is the dangerous class. A *changed* line often shows up as a behavioral
divergence; an *added* branch shows up as nothing at all.

### 3.2 The instrument

`.cs.auto` is exactly the right instrument because it is the converter's answer to the question *"what
would the automatic conversion of this file be, today, from this Go tree?"* — emitted beside the hand-own
whenever a seeded root holds the marked `.cs` at that path.

**The diff is `.auto`(old Go) vs `.auto`(new Go), per hand-own — never `.auto` vs the hand-owned `.cs`.**
The latter diff is dominated by the hand-own's *intended* divergence and is unreadable. The former
isolates the upstream delta.

**Both `.auto` files must be produced by the SAME converter binary.** Otherwise converter drift
contaminates the Go-version axis and the classification is worthless. Concretely:

```
run A: go2cs.exe(NEW build) -stdlib -comments -go2cspath <tmpA>/src   with GOROOT = old release
run B: go2cs.exe(NEW build) -stdlib -comments -go2cspath <tmpB>/src   with GOROOT = new release
```

both roots seeded per the ritual, each seeded `version.props` carrying **its own** matching
`<GoStdLibVersion>` so `checkCorpusToolchainPin` passes on both sides (§1.4.1 point 3). Run A is the
baseline that H0 captures; run B is H5's own staging root, so the marginal cost of the instrument is
**one extra full reconvert** (~200–240 s on the i9, 3–4× on the i7 class) plus the diff.

*Named blind spot:* if the new converter build cannot parse the OLD tree cleanly — unlikely, since Go's
parser is backward-compatible — run A degrades and the baseline is suspect. Assert run A's package count
and marker gate match the outgoing corpus's before trusting it.

### 3.3 The classification — every delta, explicitly, one of three

For each hand-own with a non-empty `.auto`↔`.auto` diff, every hunk is classified:

| Class | Meaning | Required record |
|:--|:--|:--|
| **(a) ABSORBED** | The upstream change is real and has been carried into the hand-own | The commit that carried it; a test or gate that observes it |
| **(b) N/A** | The upstream change does not apply to the managed implementation | **The reason, written out.** "Go's sleeping semaphore has no managed analogue; the `SemaphoreSlim` rewrite is unaffected by a change to the futex fast path" — not "n/a" |
| **(c) REWRITE OWED** | The hand-own must change and has not yet | A named work item, gating the hop or explicitly deferred with the deferral's owner and reason |

A hand-own with an **empty** diff still gets a record — `unchanged`, with the two `.auto` hashes. A
hand-own the run emitted **no** `.auto` for gets a record too, and that record is a **defect in the
audit**, not a pass: it means either the seed did not take at that path or the marker predicate could not
see the marker. *(That second case is not hypothetical — the r47d census found `runtime/runtime2_impl.cs`
carrying a marker the predicate cannot see, because an earlier `//` comment contains `*g/*p` and the `/*`
opens a phantom block comment the file never closes. Inert today; exactly the shape that would silently
clobber a whole-file hand-own.)*

### 3.4 Where it is recorded

Proposed: **`docs/phase4/AUDIT-handowns-go1.NN.md`**, one file per hop, committed with the hop's corpus.
Table shape:

| Hand-own path | Principal | `.auto` old ↔ new | Hunks | (a) | (b) | (c) | Verdict |
|:--|:--|:--:|--:|--:|--:|--:|:--|

plus a prose subsection per non-empty diff carrying the (b) reasons and (c) work items in full. One file
rather than per-package notes, because the completeness gate below has to be checkable in one place.

⟨OQ-15⟩ — alternative homes considered and not chosen: a `docs/validation/`-style per-package sheet
(spreads the completeness question across 59 files), or an in-source comment beside each marker (invisible
to a gate, and the marker file is the thing being audited).

### 3.5 The completeness gate

> **No hop's corpus is adopted until every hand-own in the re-measured census has a classified delta
> record in that hop's audit file, and every (c) is either closed or explicitly deferred with an owner.**

Mechanically: a script — proposed `src/check-handown-audit.ps1`, sibling to
`check-solution-integrity.ps1`, and run as an **H5 overlay preflight** the way solution-integrity runs as
CNR's preflight — asserts

1. the line-anchored marker census over `src/core` (re-measured, not asserted against a constant);
2. every marked path appears exactly once in the hop's audit file;
3. every audit row's class is one of `unchanged` / `a` / `b` / `c`;
4. every `b` row carries a non-empty reason;
5. every `c` row carries a work-item reference;
6. **zero** rows in the "no `.auto` emitted" state.

Exit 1 on any violation. This is deliberately the same shape as the repo's existing preflights: cheap,
by-path, and impossible to pass vacuously.

*[Landed 2026-08-24 as **[`src/handown-census.ps1`](../src/handown-census.ps1)** — the proposed name's
intent rather than its spelling. It is the differential **census** half of the gate: read-only,
self-verifying (its classes must sum to the re-measured marker census), and it classifies every marked
file `untouched` / `touched-trivial` / `touched-substantive` / `no-upstream-counterpart` across
`-FromGoRoot`/`-ToGoRoot`, so the audit starts from a list instead of from everything. It decides where
H6 looks, never what H6 concludes — the classification below stays a human reading. Procedure:
[`GoCorpusMigration.md`](GoCorpusMigration.md) H6.]*

The one **known** marker-visibility hazard the gate would have inherited — `runtime/runtime2_impl.cs`'s
header comment spelling `*g/*p/*m`, whose `/*` opens a phantom block comment for any comment-aware
scanner (§3.3) — **is fixed as of this commit series**: the comment now reads `*g / *p / *m` and the
line-anchored marker is visible to a scanner that honors block comments.

⚠ **Scale.** Measured 2026-08-13: the audit has **53 marked files / 42 `*_impl.cs` companions** to cover
(59 distinct hand-owns at the r59 bank, itself now two days stale — re-measure at the hop), of which the
`.cs.auto`-producing subset is much smaller — **19 tracked `.cs.auto` siblings** today against 15 of 41 at
r44a; the rest are `*_impl.cs` companions and hand-owned packages the converter never re-emits at that
path. **Those two populations are
different and the gate must not conflate them**: the *audit* covers all hand-owns; the *`.auto`
differential* only reaches the ones the converter re-emits. A `*_impl.cs` companion has no Go counterpart
and therefore no `.auto` — its record is legitimately "no automatic counterpart; audited against its
principal's `.auto`". Getting this distinction wrong in either direction is how the gate becomes either a
false alarm or a rubber stamp. ⟨OQ-16⟩

---

## 4. Parity-gate definitions

A hop merges to master only when **all five** hold. Each is stated so it can be checked, not felt.

| # | Gate | Definition | Instrument |
|:--|:--|:--|:--|
| **P1** | **Compile parity** | 100 % of the hop's package set compiles under the default `$(GoTargetOS)`, zero errors; skipped-dependents enumerated and zero | `dotnet build src/go2cs-stdlib.slnx -c Debug -p:UseSharedCompilation=false -clp:ErrorsOnly` |
| **P2** | **Roster parity** | Roster ≥ prior hop, per the ⟨OQ-10⟩ ruling on % vs absolute; every row backed by a regenerated proof page and a re-derived disclosure manifest | `run-validated-sweep.ps1` full roster, coordinator-owned, backgrounded |
| **P3** | **Behavioral parity** | Full 4-phase suite green; every moved golden classified before rebank; **zero** `NOT MEASURED` | `run-behavioral.ps1` (full) + CNR |
| **P4** | **Hand-own audit complete** | §3.5's gate passes | `check-handown-audit.ps1` (proposed) |
| **P5** | **Release ritual rehearsed** | Tag mint, write-once snapshot, both badge retargets, recomputed re-verification — all exercised on the hop's tree | `push-nuget.ps1` dry run |

**Master cuts over only at P1–P5.** The version branch may carry a red P2 for a long time — that is what
the branch is *for*.

**A note on what is NOT a parity gate.** Performance is not one: a Native-AOT publish now ILC-compiles the
full converted-stdlib closure per benchmark (~25 min each on the i7-5820K), so a full perf run is hours
and must run solo. Propose measuring perf **once per ladder**, not once per hop. ⟨OQ-17⟩

---

## 5. Risk register

| ID | Risk | Why it is real here | Proposed mitigation |
|:--|:--|:--|:--|
| **R1** | **Hand-own drift — added upstream code silently absent** | The whole reason §3 exists. Compiles clean, gates green, fails operationally, surfaces months later in an unrelated package | §3's `.auto` differential + P4. This is the mitigation; there is no second line of defence |
| **R2** | **Test-suite churn invalidates every differential baseline** | Disclosures are pinned by **exact failure signature**; a reworded test breaks the pin. Denominators move. 1.26 rewrites `crypto/dsa`'s determinism mechanism outright | H10 treats every row as re-derived from zero. Never carry a disclosure across a hop; re-sign each |
| **R3** | **The `go/types` checker wall meets newer `go/types` source** | Already open: the converted `go/types` fails to type-check generic source (92 bogus type-parameter errors, 91 nil-panics through `check.cs:430`'s re-panic arm), and `go/internal/gcimporter`'s 184 rows are downstream of it. **1.24 adds materialized generic aliases with `Alias.TypeParams`/`TypeArgs`; 1.26 adds self-referential constraints.** The wall gets taller at exactly the two hops that matter | Do **not** make `go/types` a hop blocker. Treat it as a standing board item whose row moves independently. But **do** measure it per hop, because a hop that makes the wall *worse* is information the frame's "roster % ≥ prior" gate would otherwise hide inside an aggregate |
| **R4** ✅ **CLOSED 2026-08-24** | **Stale `go2cs.exe` after a toolchain hop** (§1.4.2) | No harness's up-to-date predicate observed the toolchain; a hop touches no `.go` | **Closed by H1.4** — the three rebuild predicates delegate to one shared helper (`src/tests/ConverterBuildInputs.cs`) that compares the binary's embedded release against the live `go env GOVERSION` and fails stale-wards. No stamping was needed; route #5 (embedded assets) closed with it |
| **R5** | **Unseeded or double-converted staging root** | The single most expensive recorded mistake in the repo's history (14 hand-owns clobbered; a false operational-break alarm; and separately, one corrupted `runtime/arena.cs` with nine `«DYNTYPE»` markers from two overlapping conversions). A hop runs *more* conversions than normal work does | Mechanical: delete + re-seed per run, confirm no live `go2cs.exe`, wrap converter calls in `$ErrorActionPreference = 'Continue'` |
| **R6** | **`.cs.auto` baseline stale at H0** | 11 of 16 stale at r40; the overlay rule freezes them by design | Generate the baseline from a seeded old-Go regen, never from committed siblings (§H0) |
| **R7** | **Corpus growth outruns validation** — roster % falls while absolute rises | 1.24's FIPS-140 reorg is expected to add substantially more than its six named packages, and every added testable package enlarges the denominator | ⟨OQ-10⟩'s ruling. Whichever way it goes, report **both** numbers every hop |
| **R8** | **The three-pin window (H1↔H2)** | Between the converter rebuild and the `version.props` bump, `-recurse=nuget` refuses legitimate old-pin modules and approves new-pin ones for a corpus that does not exist | H1 and H2 land as one reviewable pair |
| **R9** | **x/tools lag** | v0.31.0 is Go-1.24-era; `gcexportdata` supports only "the last two Go releases plus tip" | Bump x/tools + x/mod in H1 and treat it as a converter change owing its own CNR |
| **R10** | **Badge/README diff mistaken for drift** | All four badges move on ~305 READMEs at every hop | State the expected diff size **before** the overlay; the eight hand-owned READMEs re-derived as a control |
| **R11** | **1.25's nil-check fix flips differential rows in both directions** | A row that matched because both sides were wrong the same way can newly diverge; a disclosed divergence can silently close | Classify closures as carefully as breaks. A disclosure that closes must be **retired with evidence**, not dropped |
| **R12** | **The hand-owned `testing` host falls behind** | 1.24 adds `B.Loop`, `T.Context`, `T.Chdir`; 1.25 adds `T.Attr`, `T.Output` and changes `AllocsPerRun`. `src/core/testing` is skip-listed and never converted, so it follows **nothing** automatically | Make the test host an explicit named work item in H4 of every hop, not a re-audit finding |
| **R13** | **Experiment-gated packages create a false "missing package" report** | `testing/synctest` (1.24), `encoding/json/v2`+`jsontext` (1.25), `simd/archsimd`+`runtime/secret` (1.26) are all absent from a default queue | H3's census names them explicitly as *deliberately absent*, so a later reader does not re-diagnose it |

---

## 6. Sequencing — in gate-cycles

A **gate-cycle** is one full pass of the expensive instruments: CNR + full behavioral suite + full
stdlib build. On the current coordinator machine (i7-5820K, 6C/12T) that is roughly CNR 1,505 s +
behavioral 2,820–4,131 s + stdlib build ~500 s ≈ **1.5–2 hours of machine time, solo**, before any
validation sweep. The full roster sweep is a separate, larger unit and is counted on its own.

| Phase | Gate-cycles | Sweep-runs | Notes |
|:--|:--:|:--:|:--|
| ~~**Complete 1.23.1** (130 → 162 rows)~~ ✅ **CROSSED 2026-08-22** | — | many | Not part of this plan's step inventory; it is the frame's precondition. 32 rows to go from the 2026-08-13 reading — **all 32 landed**: the roster reads 162 / 215 (75.3 %) |
| ~~**Tag + branch `release/go1.23`**~~ ✅ **DONE** | 1 | 1 | The full P1–P5 set, run once, as the ladder's zero point — tag `stdlib-tests-75pct-2026-08-22`, branch `release/go1.23`, published `nuget-1.23.1.7` ([`phase4/MILESTONE-75pct-prep.md`](phase4/MILESTONE-75pct-prep.md)) |
| **Hop A — 1.23.12 (REHEARSAL)** | 2–3 | 1 full | H4 should be ~empty (no language delta). The cycles buy the *machinery*: the pin bump, the `.auto` differential's first real exercise, the badge churn, the ritual rehearsal |
| **Hop B — 1.24.13** | 5–8 | 2 full | The big one. Generic type aliases (H4, likely its own DESIGN), the FIPS-140 tree (H3/H7), six new packages with new NuGet IDs, the `testing` host, and the largest expected `.auto` differential |
| **Hop C — 1.25.x** | 3–5 | 2 full | No language delta; cost is behavioral (nil-check fix, `unicode` tables, `AllocsPerRun`) and roster re-derivation |
| **Hop D — 1.26.x** | 4–6 | 2 full | `new(expr)` + self-referential constraints (H4), `crypto/hpke`, the crypto-randomness deprecation class, `image/jpeg` |

**Why the frame's "evidence-based decision" on C+D is the right shape.** Folding 1.25 and 1.26 into one
hop saves roughly one gate-cycle set and one full sweep — real money. It costs the ability to attribute a
regression to a release: a combined hop's `.auto` differential shows the union of two releases' upstream
changes, and R11's both-directions problem doubles. **Proposed decision rule, for coordinator ruling:**
fold C and D **only if** hop B's `.auto` differential classified cleanly with **zero (c) rewrites owed**
and hop B's roster % did not fall. Two clean hops in a row is evidence the machinery attributes correctly;
one is not. ⟨OQ-18⟩

**Ordering within a hop is not negotiable at three points:** H1→H2 are one pair (R8); H2 precedes H5 (the
pin guard refuses otherwise, and its own message says so); H0's baseline precedes H1's toolchain install
(you cannot capture the old tree's `.auto` after replacing the old tree — unless side-by-side installs are
used, which they should be, in which case this relaxes to "before H5"). Everything else can be reordered
by the executing lane.

---

## 7. What this plan does **not** answer

Named honestly, in the house habit, so the gaps are visible rather than discovered:

1. **How a generic type alias maps to C#.** §1.3 says it is real work and stops there. It wants a DESIGN
   before hop B's H4, not a paragraph here.
2. **The size of the FIPS-140 reorg.** Deliberately unguessed; H3 measures it.
3. **Whether the `go/types` wall is one root cause or two** — already unresolved on the board, and this
   plan does not resolve it.
4. **Whether the ladder ever stops.** After 1.26 the corpus is on the current minor and the question
   becomes a *cadence* question (every release? every other? only EOL'd?), which is a different document.
   ⟨OQ-19⟩
5. **Multi-platform beyond Windows.** H8 keeps the three-target emission honest, but the Linux corpus does
   not yet build (`docs/PLAN-linux-operation.md`, DESIGN-multiplatform-corpus §12), and this plan does not
   change that. A hop neither helps nor hurts it.
   *[Overtaken 2026-08-14: the compile wall FELL — the Linux flavor last measured **307/307, 0 errors**
   (`phase4/CENSUS-linux-compile-wall.md` §10; carried as `PLAN-hop-campaign.md` §4.1's H7 row). What
   remains beyond Windows is operational, not a compile wall; the sentence above is kept as the
   period-accurate reading it was.]*
6. **Whether any hop publishes to nuget.org, or only rehearses.** ⟨OQ-14⟩.

---

## 8. Open questions — RULED

Every question below carries its coordinator ruling. A ⟨OQ-n⟩ mark anywhere above is a pointer into this
table; where the prose at that mark proposes an answer, the ruling here is what governs.

| # | Question | Why it needed a ruling | Ruling (coordinator, 2026-08-13) |
|:--|:--|:--|:--|
| **OQ-1** | Do hops C and D pin **the latest patch at conversion time**, or a patch **chosen and frozen at hop start**? | 1.25/1.26 patches land monthly; a hop that spans two patch releases would otherwise have a moving input mid-flight | **Frozen at hop start.** A patch released mid-hop does not move the target; adopting it is an explicit **restart decision**, taken and recorded as one |
| **OQ-2** | Are GOEXPERIMENT-gated packages ever converted? | Decides whether `testing/synctest` (1.24), `encoding/json/v2` (1.25), `simd/archsimd`/`runtime/secret` (1.26) are in scope or permanently out | **Experiment-gated stays OUT until it graduates to the default package set.** `testing/synctest` therefore enters the corpus at the **1.25 hop**, not the 1.24 one |
| **OQ-3** | Who owns the hand-owned `testing` host's per-hop update — the hop, or a standing chip? | It follows nothing automatically and gains API at 1.24 and 1.25 | **The hop owns it**, as a **named H4 work item** — not a standing chip and not a re-audit finding |
| **OQ-4** | Does generic-type-alias support get its own DESIGN doc before hop B? | Proposed: yes. It is the only genuinely open converter design on the ladder | **Yes — a generic-alias DESIGN lands before hop B**, and gets **adversarial-panel treatment** like the other arc designs |
| **OQ-5** | Is the x/tools/x/mod bump a separate commit with its own CNR, or folded into H1? | Proposed: separate, because a dependency bump that moves emitted bytes must be visible on its own | **Separate commit, with its own CNR** |
| **OQ-6** | Accept the toolchain-stamp remedy for R4, or the interim force-rebuild? | Proposed: stamp. The interim relies on remembering | **The stamp remedy** — and it **lands BEFORE hop A**, not inside it |
| **OQ-7** | Level CleanupBacklog item 18 (`.cs.auto` staleness) before hop A, or generate baselines fresh in H0? | Proposed: fresh in H0 (strictly safer), and level item 18 separately on its own merits | **Fresh baselines in H0.** CleanupBacklog item 18 levels **separately**, on its own merits |
| **OQ-8** | Does `src/go2cs/go.mod`'s `go` directive move to the target release each hop, or stay at a floor? | Moving it lets converter source use new language features; staying keeps the converter buildable by older toolchains. These pull opposite ways | **`go.mod` moves to the target each hop** |
| **OQ-9** | Does `<GoBuildNumber>` **reset** at a hop or keep climbing? | `1.24.13.1` vs `1.24.13.7`. Resetting reads better; climbing is a strictly monotonic global counter. Interacts with OQ-11 | **`<GoBuildNumber>` resets per release** |
| **OQ-10** | "Roster % ≥ prior" — **percentage** or **absolute row count**? | The denominator is re-derived each hop from the new release's `func Test…` census; the two criteria can disagree. R7 | **Gate on the ABSOLUTE row count**, with rows lost to an **upstream-deleted package** admitted as **recorded exceptions**. **Both** numbers — absolute and percentage — are reported every hop |
| **OQ-11** | Confirm NuGet version monotonicity across the ladder before the first publish | `1.23.1.6` → `1.23.12.1` → `1.24.13.1`. Believed monotonic by numeric component comparison; **not verified** | **A scripted `NuGetVersion.Compare` assertion is added to H11**, and runs **before the first publish** |
| **OQ-12** | Close the `publishedStdLibRelease()` gap (built-but-not-published) with an embedded `version.props` value or a feed query? | Inherited open question from FINDING-toolchain-goroot-divergence §5; a hop makes the window wider | **Embedded publish-stamp**, per the standing **L5 ruling**. A feed query is **advisory** only, never the gate |
| **OQ-13** | Policy for **removed** upstream packages on the NuGet feed — unlist, deprecate, or leave? | First arises at hop D (`cmd/doc`; the `windows/arm` port). No precedent exists | **Deprecate, with a pointer to the last release that carried it. Never unlist** |
| **OQ-14** | Does each hop **publish**, or only **rehearse** the ritual until the ladder completes? | The frame says "rehearsed" at the parity gate; publishing every hop quadruples the public surface and the OQ-13 exposure | **Every hop publishes** — AMENDED 2026-08-13 with the frame: 1.23.12 is the 1.23 story's living corpus and supersedes `.1` on the feed, so hop A publishes too (original ruling had A rehearse-only; superseded by the frame amendment). *Scoped 2026-08-22 by ⟨OQ-H3⟩ ([`PLAN-hop-campaign.md`](PLAN-hop-campaign.md) §7): this ruling was framed over **Go-version** hops, which is the only kind in view here — a **.NET runtime** hop rehearses the ritual without publishing, because the version scheme carries no runtime signal* |
| **OQ-15** | Confirm `docs/phase4/AUDIT-handowns-go1.NN.md` as the audit's home | §3.4. Alternatives considered and rejected there | **Confirmed** |
| **OQ-16** | Confirm the audit covers **all** hand-owns while the `.auto` differential covers only re-emitted ones | §3.5's sharpest edge; getting it wrong makes the gate either a false alarm or a rubber stamp | **Confirmed** — plus: an `*_impl.cs` companion is audited **against its principal's `.auto` diff**; a hand-owned **package** is audited by **manual upstream diff**; and **every record names its evidence class** |
| **OQ-17** | Perf suite: once per ladder, or once per hop? | Hours per run, must be solo. Proposed: once per ladder | **Once per ladder, plus coordinator discretion** |
| **OQ-18** | Accept the proposed decision rule for folding hops C+D (fold only if B classified clean with zero (c) owed **and** B's roster % held)? | The frame defers this to evidence; this is a proposed definition of what evidence means | **Ratified as proposed** |
| **OQ-19** | After 1.26 — what cadence? | Out of scope here, but the answer changes whether the ladder's machinery is a one-off or a standing instrument worth more investment | **Deferred to a post-ladder cadence document** |

---

## Sources

- [Go Release History](https://go.dev/doc/devel/release) — patch versions, dates, support policy
- [endoflife.date — Go](https://endoflife.date/go) — release and EOL dates
- [Go 1.24 Release Notes](https://go.dev/doc/go1.24)
- [Go 1.25 Release Notes](https://go.dev/doc/go1.25)
- [Go 1.26 Release Notes](https://go.dev/doc/go1.26)
- [What's in an (Alias) Name?](https://go.dev/blog/alias-names) — `go/types` Alias materialization history
- [`golang.org/x/tools/go/gcexportdata`](https://pkg.go.dev/golang.org/x/tools/go/gcexportdata) — the
  "last two Go releases plus tip" export-data policy
- Repository, read 2026-08-13: `CLAUDE.md`; `src/version.props`; `src/go2cs/go.mod`;
  `src/go2cs/toolchainResolution.go`; `src/go2cs/conversionDriver.go`; `docs/ValidatedTestPackages.md`;
  `docs/phase4/FINDING-toolchain-goroot-divergence.md`; `docs/phase4/LANES.md` (L8);
  `docs/phase4/DESIGN-multiplatform-corpus.md` §12; `docs/phase4/DESIGN-validation-proof-pages.md`;
  `docs/phase4/PLAN-corpus-rebank.md`; `docs/phase4/BOARD-next-validation-candidates.md`;
  `docs/PLAN-linux-operation.md` §A4
