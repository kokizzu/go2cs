# PLAN — the corpus upgrade ladder (Go 1.23.1 → current)

> **Status: ADOPTED — drafted 2026-08-13 as a PROPOSAL; §8's nineteen open questions RULED by the
> coordinator, 2026-08-13.** The ruling frame in §0 is coordinator-decided and treated as fixed. Where
> CLAUDE.md already settles a mechanic this document cites it rather than re-deciding it; where it did
> not, the item was an **open question** in §8, marked ⟨OQ-n⟩ at the point it arises — every ⟨OQ-n⟩ mark
> now reads as a **pointer to that question's ruling** in §8's *Ruling (coordinator, 2026-08-13)* column,
> never as an unresolved item. The prose at each ⟨OQ-n⟩ site is left exactly as drafted, so where that
> prose merely *proposes* an answer, §8's ruling is what governs.
>
> **Amended 2026-08-24 — the RUNBOOK now leads.** §2 (the step inventory), §3 (the hand-own re-audit)
> and §4 (the parity gates) were generalized into
> [`GoCorpusMigration.md`](GoCorpusMigration.md), which is the maintained procedure and is amended
> in-stage as it is executed; those three sections here are **pointer shells**, kept for their
> anchors and their ruling context. §0, §1, §5, §6, §7 and §8 remain this document's living content
> and govern hops B–D. **§8's rulings remain the citable record** — a runbook *"(ruled)"* resolves
> here, and a runbook edit never reopens one.
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

> **Pointer, 2026-09-02 — the MEASURED half of §1.2/§1.3 now exists, in
> [`phase4/RECON-go1.24-hop.md`](phase4/RECON-go1.24-hop.md).** That record is lane C2's empirical
> capture of the 1.24 hop (342/342 packages convert, the compile ladder, the hand-own and roster
> bills), and its closing **DELTA block, dated 2026-09-02**, was written against *this* survey: it
> re-derives §1.1's 1.24 row from a second source, and it sizes ⟨OQ-3⟩'s test-host item — the gap
> is on the `TB` **interface** (`Chdir`, `Context`), not just on `T`/`B`, and `B.Loop` reaches **115
> of 207** roster packages, where a rewritten benchmark body is a BUILD failure rather than a
> divergence. It names **two items this survey does not carry**: the **Swiss-table `map` and
> spinbit-mutex experiments as NEW `GOEXPERIMENT` flags** (§1.2 names `sync.Map`'s hash trie but
> neither the *builtin* map nor the runtime-internal mutex, and all four 1.24 flags are new rather
> than flipped); and a **hand-own category the H6 re-audit's gone/changed/identical classification
> is structurally blind to** — a principal that still EXISTS but is **DESELECTED** by a
> newly-baselined experiment, which is what `spinbitmutex` does to four hand-owned runtime lock
> files. **No ruling above is reopened.** ⟨OQ-2⟩, ⟨OQ-3⟩ and ⟨OQ-4⟩ stand as written, and §9 of
> that record supports holding the hop until 100% on 1.23.12: the delta changes the hop's CONTENT,
> not its SCHEDULE.

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

> **GENERALIZED — the maintained inventory is [`GoCorpusMigration.md`](GoCorpusMigration.md) §2.**
> This section drafted the canonical H0–H12 ladder. The runbook now carries it, amended in-stage as
> its first execution taught it — and it has since grown **H4a**, the opening deliberate-regen slot,
> which never existed here. What survives below is one status note per step, kept deliberately:
> the H-numbers are a **citation namespace** (shipped source cites `H2` and `H1.4` by name), and
> several steps carry the ⟨OQ-n⟩ marks that point into §8's rulings, which the runbook's terse
> *"(ruled)"* annotations resolve against. **Where a note below and the runbook disagree about
> procedure, the runbook is right and this is stale.**

Every hop runs H0 → H12 in order. Steps marked **GATE** are pass/fail and block the next step. Steps
marked ⟲ are re-measured at each hop and never carried forward.

### H0 — Branch and baseline capture

Cut the version branch; capture, on the **outgoing** toolchain and the **new** converter build, the
artifacts the hop diffs against — the `.cs.auto` baseline (§3), the package census, the roster
snapshot, the disclosure manifests. ⟲ Ruled: generate the baseline fresh from a seeded old-release
regen, never from the committed `.cs.auto` siblings, which the overlay rule freezes by design.
⟨OQ-7⟩ · Procedure: runbook H0.

### H1 — Toolchain provisioning **GATE**

Install the target side-by-side and confirm it **executes** (the correction at the head of §1.4.2's
neighbourhood applies: `bin/go version` OUTPUT, never `GOROOT/VERSION`); move the converter module's
`go` directive ⟨OQ-8⟩; bump `golang.org/x/tools` and `golang.org/x/mod` as a **separate commit with
its own CNR** ⟨OQ-5⟩; rebuild the converter on the new toolchain and green its `go test ./...`.
Procedure: runbook H1.

### H2 — The pin bump **GATE**

Bump `<GoStdLibVersion>` in `src/version.props` to the exact target; `<GoBuildNumber>` **resets** per
release ⟨OQ-9⟩. Lands as **one reviewable pair with H1** (R8), and **precedes** the reconvert, which
`checkCorpusToolchainPin` enforces in its own error text. Procedure and instrument (`migrate-gorelease.ps1`):
runbook H2.

### H3 — Package census ⟲

Diff the conversion queue's package set against the outgoing corpus: added, removed, renamed or
promoted, and experiment-gated-and-therefore-absent — that last category named explicitly, because
experiment-gated packages stay out until they graduate ⟨OQ-2⟩. Deliverable: a census document under
`docs/phase4/`. Procedure: runbook H3.

### H4 — Converter feature work **GATE**

Whatever §1.3 names for this hop plus whatever H3 surfaces, each item under the standing repository
discipline. Two work items belong here by ruling rather than as audit findings: the hand-owned
`src/core/testing` host, which follows nothing automatically ⟨OQ-3⟩, and the `go.mod` readers.
Procedure: runbook H4.

*The runbook also carries **H4a**, the opening deliberate-regen slot that levels the queued-leveling
bundle before H5 so H5's overlay diff is readable. It postdates this plan and has no note here.*

### H5 — Seeded full reconvert **GATE**

CLAUDE.md's reconvert ritual, unabridged — seed first, never convert twice into one staging root,
wrap the converter call so its stderr does not abort the wrapper, path-precise line-anchored marker
gate ⟲, classify emitted-vs-seeded by sentinel mtime, overlay `.cs`/`.csproj`/`README.md` excluding
`*.cs.auto`. The three **hand-owned-by-consequence** packages never re-emit their project files, and
a hop that adds a package to that class must notice. Procedure and every trap: runbook H5.

**Retained reading — not procedure.** Measured on this checkout **2026-08-13**, kept because it is a
dated measurement with no other home, and because the two-day movement in it is the argument for the
re-measure-never-carry rule:

  | Quantity | r59 banked (2026-08-11) | Measured 2026-08-13 |
  |:--|--:|--:|
  | Line-anchored `[module: GoManualConversion]` files | 49 | **53** |
  | `*_impl.cs` companions | 41 | **42** |
  | Tracked `*.cs.auto` review siblings | 16 (r40) | **19** |
  | Converted package `.csproj` (excl. `*.tests.csproj`) | — | **306** |
  | Banked `*.tests.csproj` | — | **130** |

  The marker census moved 49 → 53 between a Saturday bank and a Monday reading. And the free
  corroboration: the banked `*.tests.csproj` count exactly equalled the roster's row count, which is
  the committed-evidence half of the green-badge rule — a hop that ends with those two unequal makes
  the badge census miscount, loudly, by design. (Both readings have moved again since; the runbook's
  H10 carries the arithmetic check, and every number here is re-measured at the hop.)

### H6 — The hand-own re-audit ⟲ **GATE** — *see §3, this is the mandatory step*

### H7 — Compile parity **GATE**

Full `go2cs-stdlib.slnx` build with shared compilation disabled: **zero errors and zero
skipped-dependents**, at **100 %** of the hop's package set — not "as many as before". Every
buildable target-OS flavor, purging between switches. Procedure: runbook H7.

### H8 — Multi-platform L3 re-emission **GATE** ⟲

Re-run the multi-target emission and the platform census; diff the manifest against the outgoing one.
A hop moves the platform axis in **both** directions at once, and the per-GOOS package count is a
measurement, not a constant. Procedure and gate: runbook H8.

*Hop-specific research with no other home: the **1.26 hop removes the `windows/arm` port**. That is a
GOARCH the corpus does not target, so the expected impact is nil — but it is the first hop where a
port disappears, and the census should say so explicitly rather than be silent.*

### H9 — Behavioral golden rebank **GATE**

Behavioral goldens move through §1.1's three channels, and which are live is knowable in advance —
**predict the diff's size before running the rebank**, because a diff that materially exceeds the
prediction is a finding rather than a rebank. Re-transpile **first**; the golden-update utility copies
on-disk `.cs` and does not run the converter. Procedure: runbook H9.

### H10 — Roster / proof-page / disclosure migration ⟲ **GATE** — *the hop's largest step*

Every banked suite re-validates from scratch — numerator, denominator and disclosure set alike, with
no carry-forward path. Disclosures are **re-signed, never edited**. The gate is on the **absolute row
count** ≥ prior, with upstream-deleted-package losses admitted as recorded exceptions, and **both**
numbers reported every hop ⟨OQ-10⟩. Procedure, the per-package steps and the pre-staging technique:
runbook H10; the shardable-campaign procedure is runbook §3.

### H11 — NuGet versioning + compat guards **GATE**

The published version is the pinned release plus the build counter, already set at H2. **Verify
monotonicity with a scripted comparison before the first publish** ⟨OQ-11⟩ — a non-monotonic public
sequence is not correctable. `checkNuGetStdLibCompatibility` follows the hop for free because it
reads the converter binary's own runtime version, which is exactly the H1↔H2 window R8 names. The
published-release stamp is a repository-recorded fact; a feed query is advisory only ⟨OQ-12⟩. New
packages need new IDs; removed packages are **deprecated with a pointer, never unlisted** ⟨OQ-13⟩.
Procedure: runbook H11.

### H12 — Docs, badges, READMEs **GATE**

All four README badges move as a matter of course — two read the toolchain and follow H1, two read
`version.props` and follow H2 — so **state the expected diff size before the overlay**. The
hand-owned READMEs do **not** follow, and their derivation is re-run as a control. The
GOROOT-vendored `golang.org/x/*` packages re-pin from the new GOROOT's own vendor manifest. The frame
requires the release ritual **rehearsed** at the parity gate; whether a hop also **publishes** is
⟨OQ-14⟩. Procedure, and the ritual's five defined elements: runbook H12.

---

## 3. H6 — The hand-own re-audit, driven by `.cs.auto` (mandatory, per user steer)

> **GENERALIZED — the maintained procedure is [`GoCorpusMigration.md`](GoCorpusMigration.md) H6.**
> This section specified the audit; the runbook carries it, and it has since gained the instrument
> this section only proposed. Kept here: the ruling context, and the ⟨OQ-n⟩ marks §8 resolves.

**The failure mode** (§3.1 as drafted): a hand-owned file is frozen at the semantics of the release it
was written against, so when upstream **adds** code inside it — a branch, a field, a hardening fix —
the hand-own does not receive it, and *nothing fails*. The file is excluded from the convert set, the
corpus compiles, the suites are green. **The defect is silent and operational**, and *newly-added*
upstream code is the dangerous class: a changed line often shows up as a divergence, an added branch
shows up as nothing.

**The instrument** (§3.2/§3.3 as drafted): `.cs.auto`, diffed **`.auto`(old release) against
`.auto`(new release), per hand-own — never `.auto` against the hand-owned `.cs`** — with **both sides
produced by the SAME converter binary**, each staging root seeded and carrying its own matching
`version.props` so the pin guard passes on both. Every delta is classified **(a) ABSORBED /
(b) N/A with the reason written out / (c) REWRITE OWED with a named work item**; an empty diff still
gets a record, and a hand-own the run emitted **no** `.auto` for is a **defect in the audit, not a
pass**.

**Where it is recorded** (§3.4): `docs/phase4/AUDIT-handowns-go1.NN.md`, one file per hop, committed
with the hop's corpus — one file rather than per-package notes, because the completeness gate has to
be checkable in one place. Alternatives considered and rejected: a per-package sheet (spreads the
completeness question across dozens of files) and an in-source comment beside each marker (invisible
to a gate, and the marker file is the thing being audited). ⟨OQ-15⟩

**The completeness gate** (§3.5):

> **No hop's corpus is adopted until every hand-own in the re-measured census has a classified delta
> record in that hop's audit file, and every (c) is either closed or explicitly deferred with an
> owner.**

⚠ **Two populations, and conflating them makes the gate either a false alarm or a rubber stamp**: the
*audit* covers **all** hand-owns; the *`.auto` differential* reaches only the ones the converter
re-emits. A `*_impl.cs` companion has no Go counterpart and therefore no `.auto` — it is audited
against its principal's diff; a hand-owned **package** is audited by manual upstream diff; and every
record names its evidence class. ⟨OQ-16⟩

**The instrument landed** as **[`src/handown-census.ps1`](../src/handown-census.ps1)** (2026-08-24),
under the proposed `check-handown-audit.ps1`'s intent rather than its spelling: it is the differential
**census** half — read-only, self-verifying against the re-measured marker census, classifying each
marked file `untouched` / `touched-trivial` / `touched-substantive` / `no-upstream-counterpart` across
two GOROOTs, so the review starts from a list instead of from everything. **It decides where H6 looks,
never what H6 concludes.** The mechanical assertions this section specified, and the human
classification they gate, are runbook H6's.

*(The one known marker-visibility hazard this gate would have inherited — a header comment whose `/*`
opens a phantom block comment for any comment-aware scanner — was fixed at the source; the census is
line-anchored and sees the marker.)*

---

## 4. Parity-gate definitions

> **GENERALIZED — the maintained definitions are [`GoCorpusMigration.md`](GoCorpusMigration.md) §5.**
> The five gates and their arithmetic are stated there, in the form the runbook maintains. This table
> survives as the P-number namespace and its instrument column.

A hop merges to master only when **all five** hold. Each is stated so it can be checked, not felt.

| # | Gate | Definition | Instrument |
|:--|:--|:--|:--|
| **P1** | **Compile parity** | runbook §5 | `dotnet build src/go2cs-stdlib.slnx -c Debug -p:UseSharedCompilation=false -clp:ErrorsOnly` |
| **P2** | **Roster parity** | runbook §5, per the ⟨OQ-10⟩ ruling on % vs absolute | `run-validated-sweep.ps1` full roster, coordinator-owned, backgrounded |
| **P3** | **Behavioral parity** | runbook §5 | `run-behavioral.ps1` (full) + CNR |
| **P4** | **Hand-own audit complete** | §3.5's gate, procedure at runbook H6 | `src/handown-census.ps1` (the census half) + the audit file's own assertions |
| **P5** | **Release ritual rehearsed** | runbook §5, and its five elements at runbook H12 | `push-nuget.ps1` dry run |

**Master cuts over only at P1–P5.** The version branch may carry a red P2 for a long time — that is
what the branch is *for*.

**A note on what is NOT a parity gate.** Performance is not one: a Native-AOT publish ILC-compiles the
full converted-stdlib closure per benchmark, so a full perf run is hours and must run solo. Measure
perf **once per ladder**, not once per hop. ⟨OQ-17⟩

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
