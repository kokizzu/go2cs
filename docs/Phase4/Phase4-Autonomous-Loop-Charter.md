# Phase 4 autonomous campaign — charter

> Operating charter for the Phase-4 standard-library test-validation campaign. **Cleaned up
> 2026-07-21** to serve as the durable baseline for the autonomous campaign (the earlier verbatim
> loop-prompt is folded into §1–§6 below). Authoritative alongside `H:\Projects\go2cs\CLAUDE.md` and
> the docs it points to. The running per-package ledger, blockers, and hard-won detail live in the
> **memory index** (`[[go2cs-phase4-operational]]` and the other `go2cs-*` notes) — consult it every
> session and keep it current.
>
> **Companion records in `docs/Phase4/`:**
> [`DESIGN-reflection-bridge.md`](DESIGN-reflection-bridge.md) — the reflection bridge's design;
> Phases 1–2 shipped, Phase 3 is the chip-class arc of §6.1.
> [`StringsBytes-BlockerMap.md`](StringsBytes-BlockerMap.md) — **CLOSED ledger** for packages #3–4:
> every row B1–B10 / R1–R14 resolved, both packages validated 2026-07-18. Read it as the worked
> example of a full package arc (scout → build blockers → runtime blockers → differential →
> disclosed-divergence ruling → bank); its open spin-offs are carried in §3/§6.1/§9 here, not there.

---

## 1. Mission

Advance Phase 4: convert and **RUN** Go's own `_test.go` suites for the standard-library packages in
`src/core`, validating each against `go test -json` through the pipeline

```
src/go2cs/bin/go2cs.exe -tests -test-action all -test-timeout 10m "<GOROOT>/src/<pkg>" src/core/<pkg>
```

(converts → builds the .NET test host → runs it → diffs vs `go test -json`), until every viable
package's **Test functions** pass in C#.

**State (2026-07-31): 62 packages validate — 28.8%** (see
[`docs/ValidatedTestPackages.md`](../ValidatedTestPackages.md), which maintains its own progress line
per §4.6). The authoritative roster is that table; `./src/run-validated-sweep.ps1` re-validates it
end to end and fails on any count drift — derive state from those two, never a remembered count.

**Completion shape.** A package is DONE when its `func Test*` set matches `go test`. `Example` and
`Benchmark` declarations are **uniformly Phase-4D-deferred** (excluded corpus-wide) — never chase
them, and never let a package's Example/Benchmark count make it look "incomplete."

---

## 2. Prime directive — CORRECT FIRST, ALWAYS

The mission is **not** to finish this loop; it is to advance go2cs's long-term vision correctly.
Completion is the goal, but *correct* is the mission — **do it the right way even when it takes
longer**, in alignment with the project's defined goals (the two end-user use cases, the
nothing-throwaway principle, "reads like Go / runs like Go"). Weigh every decision against those
goals, never against reaching the end of the loop.

- **Fix at the RIGHT layer and at the ROOT.** A converter / golib / go2cs-gen change over a one-off
  hand-patch; a real root cause over a workaround; the reproducible-from-repo result over a
  deploy-only hack.
- **If the correct fix is a REWORK, a REFACTOR, or a LONGER path — take it.** A shorter path that
  leaves a latent defect, a hack, or a narrow special-case is the WRONG path even when it "works"
  today. Rework now beats refactor-under-duress later.
- **A package that "validates" via a shortcut is a FAILURE, not a win** — faking or trimming output,
  silently skipping a real test, hand-editing generated code, or a disclosed-divergence used to
  paper over a genuine bug. Do not count it.
- **Never trade a project goal for loop progress.** If a fix would compromise the architecture or a
  stated goal, don't take it: find the right fix, or surface the decision (§10).

---

## 3. Work order — eat the frog, then highest-value, then the slog

Do the **hardest, most-foundational items first**, then the items that unblock the most tests, then
the long tail. The foundational frogs are *also* the highest-leverage work — one shared-machinery
capability unblocks whole classes of packages, so paying that cost early avoids re-deriving it 40
times in the slog. **Verify branch/blocker state fresh each session** — remembered branch names and
"still-owed" claims go stale (all 10 "pending" branches once turned out already superseded).

### Tier 0 — foundational capability frogs (hardest × highest-leverage; do these first)

1. **Channels / goroutine rendezvous — CLOSED (landed ~2026-07-24/25; ground-truthed 2026-08-02).**
   All four gaps are fixed on master: real unbuffered rendezvous, correct cap/len, single-fire
   selectgo, uniform-random ready-case choice (`DESIGN-channels.md`; master commits `76aefaead` et
   seq.; guards `ChannelRendezvous`/`ChannelCapLen`/`NestedSelectRecvTarget`/`SelectOperandOnceEval`/
   `SelectOperandSourceOrder`/`DeepSelectRecursion`, all green). ⚠ The 2026-07-31 note that
   previously stood here — "the gaps are still real" — was written from a stale memory AFTER the
   landing and was wrong; base32/base64/bufio/os/signal/sync validated ON the real semantics, not
   around gaps. §9's trap, caught by ground-truthing the code. **Consequence: `time` and `context`
   are unblocked** (their other prerequisites landed with r32); `net`'s next wall is its init gap
   (`sync.OnceFunc` nil panic), not channels.

2. **Reflection completeness — Phase 3 of the bridge** *(golib `GoReflect` + the hand-owned
   `internal/abi`/`reflect`/`internal/reflectlite` `*_impl.cs` entry points).* **Read
   `docs/Phase4/DESIGN-reflection-bridge.md` first — Phases 1 and 2 are SHIPPED, not proposals.**
   Already landed: the Kind classifier, `TypeOf`/`ValueOf`/`unpackEface`, the ~21 `Value` readers +
   `MapIter`, the `rtype` name/field methods, canonical (interned) `reflect.Type`, `deepValueEqual`,
   the `reflectlite` mini-bridge (`Len`/`Swapper`), and the `synthType.Equal` comparability signal
   (csv, 2026-07-21). **Remaining = Phase 3, the write-back & call half:** `Value.Set*` /
   addressability, `Value.Call` / `MakeFunc` dynamic invocation (testing/quick), `MakeSlice`/`MakeMap`
   round-trips (encoding/binary, encoding/gob), the `getcallersp` stub (a `PartialStubGenerator`
   `NotImplementedException` in `runtime`; errors TestAs → `reflect.mustBeAssignableSlow`). ~~and the
   adapter-type follow-up flagged by R10~~ — **that half is CLOSED and this text was stale**: the
   unwrap landed 2026-07-24 in `94b5a1790` (R10, increment 1's own commit), so `KindOf` unwraps both
   adapter shapes and `ElementType` the pointer-sourced one. All that remains of it is a narrow
   CONSISTENCY defect — `ElementType` does not unwrap a *value-sourced* ᴠ-adapter, so a ᴠ-adapter
   over a named container (`color_PaletteᴠModel`) classifies as `Slice` yet answers a null element
   type — latent, since descriptors never carry an adapter class (`abi.TypeOf` unwraps via
   `GoDynamicTypeOf` first). Detail + the correction notice: BOARD-next-validation-candidates.md.
   **Blocks: encoding/binary, encoding/gob·json·xml, testing/quick, errors,
   math/big (#4 below), and any reflect-driven package.** Deep + architectural + multi-session →
   this one does **not** run as an inline sub-agent: it is a **coordinator-spawned independent chip,
   per §6.1.**

3. **os / filesystem** *(golib `os` operational).* `os.Open` + file reads (testdata). **Blocks: strconv
   (TestFp reads testdata), errors (synth `*fs.PathError`), and every testdata-reading test.** (os/exec
   child-spawning already works on master.) High value, moderate depth.

4. **math/big** *(189 Test funcs; heavy deps: encoding/gob·json·xml, testing/quick, os/exec,
   crypto/rand).* The single largest arc — multi-session, gates `go/constant` and "all math library
   tests." Treat as its own campaign; per the operating model it may warrant a **chip (user-owned)**
   rather than a sub-agent. Depends on Tier-0 #2 (reflect) landing first.

### Tier 1 — high-value single-blocker unblockers *(2026-07-31: this tier is CLEARED)*

Every package originally listed here — errors, unicode (whose "Change C" grew into today's
three-model test-project family: `reference`, `whitebox-reference`, `recompile`-as-fallback),
text/tabwriter, encoding/base32/base64, mime, hash/crc32, and the whole compile-blocked cluster —
**validated and banked**. The current per-package board with rooted diagnostics is
[`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md); treat that file as
this tier's living replacement and keep it rooted (a first diagnostic is a starting point, not a
diagnosis — its own header says why).

### Tier 2 — the campaign slog (the long tail, ~170 packages)

Per iteration: **scout with the isolation pipeline sweep** (the cheapest scout — running `-tests` on a
candidate IS its characterization: validates immediately vs a specific blocker). Prefer packages whose
dependencies already validate. Fix the tractable ones at the right layer; for one needing a Tier-0
capability, record the blocker in the memory ledger and move on — don't spin.

---

## 4. Per-iteration workflow

1. **Choose** the next unvalidated package (deps-already-validate first); consult `docs/Roadmap.md`
   (Phase 4), `docs/Phase4/*`, and the memory ledger for ordering/known blockers.
2. **Run** the `-tests -test-action all` pipeline (§1).
3. **Root-cause each failure against the REAL emitted `.cs` and runtime behavior — never assume.**
   Determine the correct layer (converter emission / golib runtime / go2cs-gen / hand-owned `_impl` /
   a genuinely-unsatisfiable-in-managed-runtime case warranting a **signature-pinned** disclosed-
   divergence). Fix it there.
4. **Lock every fix in** with a behavioral guard test (CLAUDE.md's regression-test steps) and update
   `docs/ConversionStrategies(-Reference).md` in the same change.
5. **GATE before landing (§5).** Then RE-VALIDATE the target on the post-change tree, and confirm
   **every already-validated package still validates.**
6. On a clean validation, follow the validated-package commit policy: commit the converted C# test
   sources into `src/core/<pkg>` (Go headers intact), **and add the package's row to
   [`docs/ValidatedTestPackages.md`](../ValidatedTestPackages.md) in the same banking commit**
   (alphabetical order; update the header's progress line — validated count, the /215 percentage,
   verdict and disclosed totals — user ruling 2026-07-25: this table maintains its own progress,
   per-package; the 215 denominator = converted packages with Go 1.23.1 `Test` functions, fixed
   until a Go version bump). Commit gpg-signed to master
   (solo-project convention). **Batch NEWS/README/Milestone** — do NOT touch those per package; wait
   for a notable cross-section (`[[go2cs-doc-update-cadence]]`).
7. Move to the next package.

**Milestone tags — the remaining cadence (user ruling, 2026-07-26).** Validation milestones are
tagged sparingly from here. The 25% crossing is tagged `stdlib-tests-25pct-2026-07-26`
(`c253370d1`); the next tags are **50%**, then **75% only if it coincides with something genuinely
hard landing** (`log/slog` is the named example — its four-root conversion is on record), and
**100% unconditionally**. Everything between them is banked by the table's own progress line, which
is per-package and needs no ceremony. Tags are annotated and gpg-signed, named
`stdlib-tests-<pct>pct-<date>`, and their annotation carries the figures, what was banked in the
arc, and the correctness classes the arc flushed out — the record a future reader wants.

---

## 5. Prove it — the mandatory gates (ALL SHIPS RISE)

A fix to shared machinery (converter, golib, go2cs-gen, the test host) must improve **all** converted
code, never just the package under test. A fix that greens one package but regresses or degrades
others is **rejected** — rework it until it lifts everything. **Compiling ≠ correct.** For each change
class, the gate is non-negotiable:

| Change | Gate |
|---|---|
| **Converter** (`src/go2cs/*.go`) | `check-no-regression.ps1` **byte-identical** (except intended, individually-justified golden re-baselines) — the authoritative drift instrument; it re-transpiles unconditionally. Then reconvert + build the **302-package corpus** (`.Value`→`.ValueSlot`-style type-safe swaps aside, a converter change *can* break corpus compile). |
| **golib** (`src/core/golib`) | full behavioral suite (compile + run, `run-behavioral.ps1`) — golib is linked by everything. |
| **go2cs-gen** | full behavioral suite **and** the 302-corpus build. |
| **Any of the above** | **Operational re-validation of every already-validated package** on the post-change tree — the real all-ships-rise proof. The instrument is **`./src/run-validated-sweep.ps1`**: it parses the roster and expected counts from `ValidatedTestPackages.md`, fails on any count drift, and reports post-sweep corpus CONTENT drift — the only gate that sees banked *test-source* staleness (CNR covers behavioral projects; the isolation-reconvert-diff covers production `.cs`; neither sees test emission). The isolation-reconvert-diff remains the cheap pre-filter to *predict* which packages a change touches; the sweep is the proof. |

Disclosed-divergence is **only** for asserts the managed CLR *provably cannot* satisfy (alloc counts),
signature-pinned per test. A real bug or an unimplemented feature is **never** a disclosure candidate.

---

## 6. Operating model — delegate widely, gate centrally

The coordinator runs as **Fable 5 Ultracode**. Run the campaign as a **coordinator with parallel
sub-agents** in **isolated worktrees** (or, for extremely delicate/complex work, user-owned chips).
The coordinator's OWN work is deliberately narrow: **gating, integration, re-validation, and landing**,
plus the *most difficult, complex, and delicate* tasks that genuinely need Fable-5-caliber reasoning.
**Everything else is delegated.** Sub-agents branch-only; the coordinator gates + lands everything
gpg-signed; **never land ungated or unverified work.**

- **Resource strategy — delegate ALL that's feasible; match model + effort to the task.** Fable 5
  credits are the scarce resource — spend them on the hard/delicate frogs and the coordinator's own
  judgment calls, NOT on routine work. Delegate as parallel as feasible, and choose each sub-agent's
  **`model` + `effort`** (the Workflow/Agent options) to fit the task, using cheaper tiers **liberally**
  wherever the work is efficiently suited to them:
  - **Cheap / mechanical → a light model at low effort** (e.g. sonnet / opus, low–medium): the
    isolation-pipeline-sweep scout, banking a validated package's test sources, re-validation runs,
    characterizing a blocker, doc/ledger updates, any well-specified single-file mechanical fix.
  - **Moderate, well-scoped fixes → a mid model at medium–high effort** (e.g. sonnet / opus, high): a
    contained converter/golib fix with a clear root cause and a bounded blast radius.
  - **Hardest / most delicate → the top tier at high–max effort, or the coordinator itself**: the
    Tier-0 capability frogs (channels, reflect, os, math/big), architecture/design decisions,
    adversarial design review (§7), and the final all-ships-rise integration judgment. Reserve Fable 5's
    own reasoning for exactly these.
  - When unsure whether a cheaper model suffices, **start cheap** (a scout or an attempt) and escalate
    only on evidence it's under-powered — don't pre-emptively burn the top tier, and don't under-power a
    genuinely delicate task (a wrong cheap fix that has to be reworked costs more than doing it right). If it turns out that starting cheap most always requires escalation anyway, adjust strategy as needed: start at a higher tier to begin with on future tasks for better work cadence.

- **Parallelism is now cheap and unconstrained.** Workspaces target the **C:\ NVMe** (fastest drive);
  the earlier H:\ dev-drive disk bottleneck is **gone**. The 32-core machine easily handles **multiple
  concurrent sessions / sub-agents / chips** — as long as **each works in its own isolated workspace**
  (worktree or checkout). There is **no concurrency cap**; fan out as the work needs. (Keep the NuGet
  cache on H:\ ReFS — that config is deliberate and read-mostly once warm.)
- **Serialize only where builds genuinely collide:** two `-tests` runs that rebuild a *shared*
  dependency race on its `.dll` (CS2012); serialize those or pre-build the dep. Isolated worktrees
  don't collide.
- **The coordinator's job is merging + validation**, not doing every fix — integrate sub-agent
  branches (cherry-pick + re-gate), keep the memory ledger current, and own the landing.

### 6.1 Special operation — CHIP-CLASS arcs (the reflection bridge; math/big)

A few items on this campaign are **not sub-agent work at any model tier**. They are deep,
architectural, multi-session arcs whose blast radius is *every* converted package, and they need
design-WITH-the-user (§10) and adversarial design review (§7) before a line is written. These run as
**independent chips** — a separate, user-owned session spun off from a background-task chip
(`spawn_task`) — **spawned by the coordinator at the right moment**, not inline, not in a worktree
sub-agent, and not up front.

**The chip-class list (updated 2026-07-31):**

| Arc | Scope | Spawn trigger / state |
|---|---|---|
| **Reflection bridge — Phase 3** (Tier-0 #2) | Design doc: `docs/Phase4/DESIGN-reflection-bridge.md`. **Substantially landed via chip increments**: `Value.Set*`, `Value.Convert`, type-relation mirrors, `MakeSlice`/`MakeMap` round-trips, canonical `reflect.Type` — enough that errors, encoding/binary, testing/quick and go/token all validate, and gob's engines RUN. Increment 4 (2026-07-31) answered `runtime.Callers`/`Frames.Next` natively, retiring the `getcallersp` row; increment 5 (2026-08-02) hand-owned `reflectlite`'s `rtype.String`, taking **`context` to 37/38** — one alloc-count disclosure from banking. **Remaining surface**: `MakeFunc`, gob's residues, and `rtype.Name` (the next descriptor-name gap of increment 5's shape, unfixed for want of a consumer). ~~the adapter-type `Kind`/`Elem` unwrap~~ — **CLOSED; that row was stale from 2026-07-24** (it landed in R10, `94b5a1790`), leaving only a latent `ElementType`-vs-`KindOf` disagreement on value-sourced ᴠ-adapters; see the correction notice in BOARD-next-validation-candidates.md. Same chip owns the remainder; spawn its next increment on the next demonstrated consumer. |
| **math/big** (Tier-0 #4) | 189 Test funcs; its own campaign. **PREREQUISITE (user, 2026-07-31): rename the `GoUntyped` C# alias first.** `GoUntyped` (= `System.Numerics.BigInteger`, `golib.csproj` `<Using Alias>`) is the ORIGINAL name from before golib's untyped numeric wrapper structs (`UntypedInt`/`UntypedFloat`/…) existed, and now reads as if it were one of them; it is actually the arbitrary-precision carrier for untyped constants exceeding native width. Pick a better name WITH the user at arc start (candidates to seed the discussion: `GoBigConst`, `UntypedBig`, `GoBigValue` — distinct from math/big's own `big.Int` mapping, which this arc adds). Footprint measured 2026-07-31: converter emits it from 5+ `.go` files; **685 corpus files** reference it → converter + golib + one deliberate whole-corpus regen, gated as usual. Doing it BEFORE math/big avoids renaming under a much larger `BigInteger` surface. | After the reflection remainder lands (depends on it through gob/json/xml + testing/quick). |

**Coordinator protocol for spawning one:**

1. **Don't pre-spawn.** Keep working the packages that don't need it. Spawn on the *first demonstrated
   consumer*, so the chip is designed against a real failing differential rather than a guess.
2. **Write a self-contained chip prompt.** The chip is a fresh session with none of this context. It
   must carry: this charter's path, the design doc's path, the **exact deferred surface** the arc owns,
   the concrete consumer package + its failing differential, the §5 gate table that applies, and the
   §10 design-WITH-user requirement. A chip prompt that says "continue the reflection work" is a
   defect.
3. **Declare an ownership lock, and honor it.** While a chip is live the coordinator and its
   sub-agents do **not** edit that arc's files — for the reflection chip: `reflect/*_impl.cs`,
   `internal/reflectlite/*_impl.cs`, `internal/abi/type_impl.cs`, `golib/GoReflect.cs`, and the
   `manualConversionFuncs` entries for those packages. Record the lock in the memory ledger when the
   chip is spawned and clear it when the chip lands. Concurrent edits to a hand-owned bridge file are
   how a split-brain lands.
4. **Never block on it.** The coordinator keeps validating packages that don't touch the arc; every
   package that *does* gets its blocker recorded in the ledger against the chip and is skipped — no
   spinning, no partial workarounds that the chip will have to unwind (a package "validated" around a
   missing bridge capability is a §2 failure, not a win).
5. **The chip owns its own gates and its own landing.** It is a full session under user control: it
   runs the §5 gate for its change class (golib → full behavioral suite; go2cs-gen → suite + the
   302-corpus; **plus operational re-validation of every already-validated package**, isolation-
   reconvert-diff to skip byte-identical ones) and lands gpg-signed per §4.6. If it lands on a branch
   instead, the coordinator re-runs the all-ships-rise gate before ff-merging — **never ff-merge a
   chip's branch on the chip's say-so alone.**
6. **One chip per arc, not per package.** The arc's follow-ups (e.g. the adapter-type `Kind`/`Elem`
   row) belong to the same chip, not to a new one.
7. **Update this table** when an arc lands or a new chip-class arc is identified — it is the durable
   record of what is deliberately *not* being done inline.

---

## 7. Adversarial review for delicate / complex work

For anything delicate, architectural, or high-blast-radius (Tier-0 frogs, shared-machinery changes,
new mechanisms, anything touching escape analysis / reflection / channels / the test-project model),
**invest in adversarial review up front** — it is far cheaper than the rework/refactor a wrong design
forces later. Concretely:

- **Independent verification of a fix**: spawn skeptic sub-agents prompted to *refute* the fix (find
  the input that breaks it, the regression it hides, the case it doesn't generalize to). Prefer
  **diverse lenses** (correctness / a different failing package / does-it-reproduce / does-it-hold-
  under-the-corpus) over N identical reviewers.
- **Design panels for real design decisions**: generate a few independent approaches, judge them
  against the long-term goals, synthesize from the strongest.
- **Gate the design, not just the diff**: for a Tier-0 capability, get the *design* adversarially
  reviewed (and user-blessed) before writing the implementation.

The goal is to catch "plausible-but-wrong" before it lands and metastasizes into every downstream
package.

---

## 8. Abstract & general — build for many use cases

Whenever possible, make each fix **abstract and general** — it should apply to as many **current and
future** use cases as possible, always with an eye to the long-term project goals. A converter/golib
capability that a package needs is almost never truly package-specific; find the general rule and
implement *that*. Examples from this campaign: the deref-alias fixes generalized to *every*
`*error`/`*[]T`/nilable-out-param; the reflect comparability fix corrected *all* synthetic types; the
unicode "Change C" is a general black-box-test-project model, not a unicode patch. A narrow special-
case that greens today's package but not the next ten like it is the wrong altitude — lift it.

---

## 9. Lessons learned — traps to NOT repeat

Hard-won during this campaign. Read these before touching the relevant area.

**Converter / gating**
- **Bank only artifacts the FINAL binary emitted — regenerate the whole roster before any bank.**
  A development session's working tree accumulates artifacts from every intermediate converter build
  along the way; the 2026-07-31 whitebox review found a staged corpus emitted by **four different
  binaries** (only one package matched the final source), carrying uncompilable fingerprints of
  superseded emission strategies. mtime forensics (artifact vs converter-source vs exe) is the cheap
  detector; the remedy is always the same — discard ALL of it, `go build -o bin/go2cs.exe`, and let
  one full sweep regenerate every banked artifact from the single final binary. Never classify or
  hand-repair mixed-vintage output.
- **Wire new analysis passes into ALL THREE conversion drivers.** The analysis phase runs in three
  places — normal (`main.go` ~1151), `-tests` (`testConversion.go` ~584), and hand-owned-sibling
  (`autoSiblingOperations.go`). A collector wired into only one **silently no-ops** for the others
  (the nil-arg fix didn't fire on `-tests` until wired everywhere). Follow `collectAddressedGlobals`.
- **Reconvert to measure a converter change.** The committed corpus can be stale (predates
  recent fixes); building it measures *old* output. To see a change's corpus impact, reconvert.
- **Isolation-reconvert-diff** narrows which validated packages a converter change actually touched:
  reconvert the candidate set to a **C:\ temp**, CRLF-normalize, diff vs committed; re-validate only
  the changed ones (most are byte-identical → skip). Isolate the change from pre-existing drift by
  diffing wave-converter vs master-converter reconverts when needed.
- **RESTORE re-validated packages; do NOT partial-rebank.** A converter change drifts many packages'
  production `.cs`. Rebanking a scattered subset = split-brain corpus. Restore them (re-validation is
  the *gate*, not a rebank trigger) and defer the whole-corpus re-baseline to **ONE deliberate clean
  regen** (kept at Go 1.23.1 — no version bump, no rug-pull). Newly-*validated* packages DO bank their
  fresh test sources.
- **False-green traps.** (a) A stale `go2cs.exe` runs old logic — force `go build -o bin/go2cs.exe`.
  (b) Runner *UpToDate* skips were fixed 2026-07-20 (both runners + MSTest now require `.cs` newer than
  the exe); `check-no-regression.ps1` re-transpiles **unconditionally** = the authoritative instrument.
  (c) `UpdateTestTargets --createTargetFiles` copies current `.cs`→`.cs.target` **without** re-
  transpiling — re-transpile first or it re-baselines stale output.
- **False-ALARM traps — the mirror of false-green.** (a) An A/B (stash the change, rebuild, compare)
  proves only that an error is **not attributable to your change**. It does NOT establish the error is a
  real pre-existing bug — it may be an artifact of an invalid check (see the standalone-`.tests.csproj`
  trap below). Root-cause it before reporting it as a finding. (b) **Verify your verification**: a scan
  whose `grep -P` dies on the locale, or whose paths fail to resolve, returns an empty result set that
  reads exactly like a clean PASS. Give every corpus scan a **positive control** — a case it MUST find —
  and confirm the control fires before trusting a zero-hit result.
- **A capability-EXCLUDED test still COMPILES.** Exclusion gates the *run registry*, not emission, so
  a broken emission inside an `AllocsPerRun`-excluded (or otherwise unsupported) test still fails the
  whole package build — bytes was blocked for a wave by one such site. Never dismiss a build error
  because "that test doesn't run anyway"; and conversely, a census that shows N excluded declarations
  tells you nothing about how much C# had to be emitted correctly.
- **Slow ≠ hung — a short `-test-timeout` FAKES a failing tail.** When the host is killed mid-run,
  every test after the cut reports `C#=""` and the differential reads as a mass infrastructure-error
  wall that looks like a real blocker class. strings' `TestCompareStrings` legitimately runs ~109 s in
  the C# runtime (the `unsafeString`→`@string` copy cost — a real, still-open performance gap), and the
  2 m default truncated the whole suite behind it. Hence the `-test-timeout 10m` in §1's command. Before
  root-causing a `C#=""` cluster, confirm the host ran to completion.
- **Root-cause LAYERING — one row masks, triggers, or moves another.** Expect the failure you are
  looking at to be the top of a stack: R8's null array-backing masked R5's DeepEqual (fixing R8 *moved*
  TestFinderCreation's error site rather than greening it); R9's pointer-print crash was *triggered by*
  R11's wrong comparison (fixing R11 stopped the trigger while R9 stayed latent); and Roslyn skips
  method-body binding while declaration errors exist, so a whole wave of CS1503s was invisible until
  the CS0246s cleared. Re-measure after every fix; never assume the wall you mapped is the wall that
  remains, and never count a row "fixed" because its symptom moved.
- **Adding a supported test capability can change BANKED packages.** Widening
  `supportedTestCapabilities` moves previously excluded-unsupported tests into the RUN set, so a
  package validated under the old list can shift. Before landing one, scan every validated package's
  `_test.go` for the newly-supported call (with a positive control, per above) and confirm zero hits —
  otherwise re-validate the affected packages. (Worked example: `testing.Benchmark` / `B.N` /
  `BenchmarkResult.NsPerOp`, 2026-07-21 — zero hits across 32 packages / 97 test files, control
  `unicode/letter_test.go`.)

**Build / git mechanics**
- **CS2012 during a corpus build = file-LOCKS, not compile errors** — an orphaned/concurrent build
  holds `.dll` locks. `dotnet build-server shutdown`, rebuild the locked packages; never leak a
  `&`-backgrounded build. Grep for `error CS` *excluding* CS2012 to see real errors.
- **A committed `<pkg>.tests.csproj` does NOT build standalone — that is not a valid check.** Its inputs
  are pipeline-staged, in two independent ways. (a) The `*.go` differential-baseline copies are
  git-ignored, so a clean tree has none and the build dies with `MSB3030` copy errors that mean nothing.
  (b) It compiles against the production `.cs` the `-tests` run **regenerates**, which can legitimately
  differ from the committed production emission. Worked example: `math/rand/v2`'s committed `pcg.cs`
  emits bare `using go.math;` — correct, because the production closure contains no `go/*` package —
  while the `-tests` build regenerates it as `global::go.math`, because `regress_test.go` imports
  `go/format`, whose `namespace go.go` shadows the root and would otherwise bind `go.math` → `go.go.math`
  (CS0234). Both emissions are right for their own closure; only the pipeline pairs them correctly
  (`globalQualifyRooted` / `rootNamespaceShadowed`, guarded by `rootShadowQualification_test.go`).
  **To check a package, run the pipeline** — `go2cs -tests -test-action all <goroot-pkg> <converted-pkg>`
  — never a bare `dotnet build <pkg>.tests.csproj`.
- **autocrlf-only "drift":** a re-validated / agent-banked test source often shows `git status`
  modified but the **content diff is empty** (CRLF↔LF). Confirm with `git diff` before chasing.
- **`.slnx` edits are byte-exact CRLF:** `sed`/`awk` strip CRLF — use `perl -0777` with explicit
  `\r\n`, or the Edit tool. Register every behavioral `.csproj` (`check-solution-integrity.ps1`); the
  harness builds by path, so a missing registration passes the suite but breaks VS.
- **`[GoTestMatchingConsoleOutput]`** goes in `package_info.cs` via the **Edit tool** (CRLF-safe), never
  `sed` (which corrupts the CRLF the transpiler's marker-parser needs).
- **Hand-owned preservation on overlay:** reconvert emits `.cs.auto` for `[module: GoManualConversion]`
  files; overlay `*.cs` **excluding `.cs.auto`** to preserve hand-owned `.cs`; also except `core\golib`
  and `core\testing` refs.
- **GPG "Timeout" = a stuck agent, not a missing key.** Do NOT kill the agent (that clears the cached
  passphrase); a bounded standalone `--clearsign` re-warms it. Launch daemons via **Gpg4win's**
  `gpgconf` (`C:\Program Files (x86)\GnuPG\bin`), not the MSYS one. Never bypass signing.

**Sub-agent / Workflow orchestration**
- **Give agents the WORKTREE path** for the `-tests` output dir, not the absolute MAIN checkout path —
  else their runs pollute the main tree. Clean stray `-tests` leftovers (git-ignored `.go` copies +
  manifests accumulate; `git clean -fdq` skips ignored files — `find … -name '*.go' -delete`).
- **Workflow worktree branches persist** (linked worktrees). Cherry-pick the agent's `claude/wf-<slug>`
  branch, re-gate, ff master. Windows long-path can block `git worktree remove` → `rm -rf` the dir +
  `git worktree prune`.
- **Don't trust a stale "still-owed" memory.** Assess stale branches with `git cherry` (patch-id) THEN
  content/ground-truth — a fix may already be on master via a divergent commit. Verify branch existence
  with `git branch` (the memory's branch claims drift out of date).

**Campaign shape**
- **The reflect `.Clone` mis-parenthesization** still blocks a fully-clean whole-corpus rebank — a
  known separate campaign; don't let it stall package work.
- **Prefer the guard test that reproduces the bug and would catch its regression** — a package's
  banked test suite is its own guard; a shared-machinery fix also needs a minimal behavioral guard.

---

## 10. Decisions, honesty, cadence

- **Decisions.** On a genuine design decision or trade-off (a new mechanism, a promotion question, a
  semantic divergence — anything shaping shared architecture), do NOT pick a shortcut to keep moving.
  If the durable choice is clear from the project goals, make it and document it. If it is a real
  judgment call that could affect the architecture, the user should own, **STOP, write up the options + your recommendation, surface it**,
  and continue with other packages while it's pending. **Design WITH the user on anything that would cause a long-term architectural change.** Judgments on internal semantics, you own, just keep code clean and clear, reduce duplication, refactor for clarity and optimize for conversion speed. 
- **Honesty.** Report real numbers at every checkpoint — what validated, what's blocked and precisely
  why, what decisions were made or pending. State partial results plainly ("N/M agree; remainder is
  <class>, owned/pending"). Compiling ≠ correct; validating-via-hack ≠ validated. Never claim a
  package validates when it does not.
- **Cadence.** Keep a fast pace using parallel options for continuous progress. Don't spin on a genuinely
  blocked package — record the blocker, surface any needed ruling, move to the next viable one.
- **Push policy (amended by user 2026-07-24).** The coordinator is authorized to `git push origin master`
  at **gated clean points**: master states the coordinator judges safe and improved over the existing
  stdlib corpus for running both the real-world example (main README.md) and the Tour of go2cs — i.e.
  the full §5 gates for every change class landed, no known regression to those two consumer paths.
  Rationale: with frequent machine reboots, code should end in the cloud more often than not. Landing
  stays gpg-signed to local master first; push follows once the point qualifies. **The user's ritual
  moves to the README.md update**: main README.md is updated only for notable/major corpus milestones,
  and that update remains user-owned per [[go2cs-doc-update-cadence]]. Pause/end only when a major
  cross-section of viable packages validates or when blocked solely on user input.
