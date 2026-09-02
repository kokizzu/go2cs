# DESIGN — per-OS roster bubbles

> Lane `C2`, 2026-09-02. **Design record. Nothing here is applied**, and no darwin annotation lands
> on the roster until the coordinator rules on it — that gate is the reason this document exists
> before any darwin sweep is banked.
>
> Serves the owner's banked "three bubbles per roster row" idea. Companion to
> [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) (layout L3, which is what makes
> a darwin flavor exist), [`../CIMatrix.md`](../CIMatrix.md) (the only darwin hardware the fleet
> reaches) and [`DESIGN-darwin-run-layer.md`](DESIGN-darwin-run-layer.md) (why a darwin *validation*
> is not imminent even though a darwin *compile* is green).

## 0. The finding that shortens this document by most of its length

**The roster's row form is already per-OS, already generic, and already guarded.** Before proposing
anything I read `src/_roster.ps1` and `src/check-roster-format.ps1`, and found:

```powershell
# src/_roster.ps1
$RosterOsKeys = @('linux', 'darwin')
```

- `$RosterOsKeys` **already contains `darwin`.**
- `$RosterOsPattern` captures **any** lowercase key — it is not spelled `linux` anywhere.
- The parser returns `OS` as a **hashtable of goos → @{ Expected; Disclosed; Applicable }** — it was
  never a Linux field.
- `check-roster-format.ps1` **already carries a fixture asserting a `darwin:` annotation parses**:
  `Assert-Equal 'annotation: darwin is a valid key' 7 $byName['dar/pkg'].OS['darwin'].Expected`.
- `windows:` is **refused by name**, with the right reason recorded: columns 2 and 3 *are* the
  Windows expectation, so a row carrying both would hold two Windows answers with no rule for which
  wins. A `windows: n/a` back door is refused in the same breath.
- The `n/a` form already parses as `Applicable = $false` with `Expected = $null` — permanently
  inapplicable, distinct from not-yet-measured.
- `run-validated-sweep.ps1` already prints its banking instruction **parameterized on the target**:
  `record a row's measured count as a '${targetGoos}: N + D' annotation`.

**So this design proposes no new annotation syntax and no parser change.** What it proposes is the
three things the per-OS story is genuinely missing, and one correction.

---

## 1. What is actually missing

### 1.1 The header's completion metrics are Linux-shaped

The roster header carries four derived numbers, and `check-roster-format.ps1` recomputes each from
the table so none can be hand-set:

```
> ### Phase 4 progress: **201 / 215 testable packages validated — 93.5%**
> **27,734 matching test verdicts · 154 disclosed**
> **Against the implementable set (215 − 6 excluded = 209): 201 / 209 — 96.2%.**
> **Linux: 178 of 199 applicable rows validated at their Linux counts** — 21,807 matching
>   verdicts · 90 disclosed · 2 rows platform-exclusive (`linux: n/a`).
```

The guard derives that last line with **Linux hardcoded four times**:

```powershell
$linuxRows   = @($rows | Where-Object { $_.OS.ContainsKey('linux') -and $_.OS['linux'].Applicable })
$linuxNaRows = @($rows | Where-Object { $_.OS.ContainsKey('linux') -and -not $_.OS['linux'].Applicable })
...
Assert-Equal 'linux header: annotated row count' ... (Get-HeaderNumber $lines 'Linux:' 'Linux:\s*\*{0,2}(\d+)\s+of\s+(\d+)\s+applicable rows')
```

**Proposal: loop the existing derivation over `$RosterOsKeys`.** The label is the key with its first
letter capitalized (`linux` → `Linux:`, `darwin` → `Darwin:`), the four assertions are unchanged in
substance, and the Linux line's arithmetic is bit-for-bit what it is today — which is the property
that makes this safe to land before any darwin row exists.

**A key with zero annotated rows must produce NO header line, and the guard must assert that too.**
A `Darwin: 0 of 199 applicable rows` line before any darwin sweep exists is a fresh way to be
dishonest: it implies 199 rows were considered and 199 failed. **Absence is the honest rendering of
"not yet measured"; zero is a claim.** So the loop emits a line only when `$osRows.Count -gt 0`, and
the guard asserts both directions — a key with rows *has* its line, a key without rows *has none*.

### 1.2 The three completion metrics, derived and never hand-set

Per OS key with at least one annotated row, exactly the four the Linux line already carries:

| metric | derivation |
|:--|:--|
| **validated rows** | count of rows with an applicable annotation for that key |
| **applicable denominator** | *whole table* − rows marked `<key>: n/a` |
| **matching verdicts** | Σ `Expected` over those rows |
| **disclosed** | Σ `Disclosed` over those rows |

**The denominator rule is load-bearing and is inherited, not invented.** The guard's own comment says
it: *"a denominator silently containing rows no Linux can ever measure makes 100% unreachable and the
line quietly dishonest against the parity goal."* Darwin will have its own `n/a` set — every
Windows-exclusive row, at minimum — and the same rule handles it with no new logic.

**Windows is deliberately NOT a per-OS key and must never become one.** Columns 2 and 3 are the
Windows expectation. The Windows completion metrics are the header's first three lines, which
already exist. **"Three bubbles" is a rendering of three data sources, not three annotations** — and
that asymmetry is the design's most important single point, because the obvious "just add `windows:`
for symmetry" is precisely what `_roster.ps1` refuses by name and for a reason.

### 1.3 The per-row bubbles

The owner's idea, rendered as a **derived** column, never a hand-set one. Per row, three states from
three sources:

| bubble | source | states |
|:--|:--|:--|
| **W** | columns 2/3 | validated (every row in the table is Windows-validated by construction) |
| **L** | `linux:` annotation | validated · `n/a` · pending (no annotation) |
| **D** | `darwin:` annotation | validated · `n/a` · pending |

The states are exactly the three the guard already distinguishes for Linux — *validated*,
*permanently inapplicable*, *pending* — so the bubbles introduce no new vocabulary.

**Recommendation: render the bubbles in the header's per-OS lines and in the exclusion ledger, NOT as
a fourth table column, at least at first.** Reasons, in order of weight:

1. **A hand-maintained fourth column is a hand-set metric wearing a derived costume.** The roster is
   a hand-edited markdown table; a column that must agree with an annotation elsewhere in the same
   row is a second place for the truth to live, and the fleet has already paid for that shape (two
   branches writing the same wrong header number and auto-merging cleanly).
2. **The annotation IS the bubble.** `· linux: 80 + 1 ·` in the row already says "L: validated, 80
   matching, 1 disclosed", in less space and with the count attached.
3. If a visual column is wanted anyway, it should be **generated** into the table by an instrument
   that reads the annotations and rewrites the column — at which point the guard asserts the column
   against the annotations, and the column can never drift. That is a real option; it is just not
   the *first* thing to build, and it should not gate a darwin annotation.

### 1.4 `Get-SweepTargetGoos` — a third stale darwin line

```powershell
# ... and on macOS because darwin's corpus does not build yet and keeps the status-quo
# default until its own lane earns one.
function Get-SweepTargetGoos {
    if (-not [string]::IsNullOrWhiteSpace($env:GoTargetOS)) { return $env:GoTargetOS.Trim().ToLowerInvariant() }
    if ($IsLinux) { return 'linux' }
    return 'windows'
}
```

**The condition in that comment is now false.** Darwin's corpus builds — census run 32649840220
(2026-08-23) and again run 33578337083 (2026-09-02, **306/306 assemblies, 0 errors, both mac legs**)
after the CS0266 fix. This is the third stale darwin line found in one evening; the other two were
`CLAUDE.md:1047` (corrected) and the arc's own prompt.

**Proposal: `if ($IsMacOS) { return 'darwin' }`, and rewrite the comment to say what is now true.**
The change is one line and it is *not* urgent, because no fleet machine is a Mac and every darwin run
goes through the matrix, where `GoTargetOS` is set explicitly by the workflow's `env:` block and
therefore wins anyway. It should land with the header loop so the file stops asserting something
false, not as an emergency.

---

## 2. How a `darwin:` annotation gets banked — the part with no precedent

This is the genuinely new question, and it is new because **the fleet owns no Mac.** Every prior
annotation was banked from a sweep on a machine the fleet controls.

**First, the mechanic that surprised me and that this design must respect: `run-validated-sweep.ps1`
never writes the roster.** It reads it, compares, and *prints an instruction* — `record a row's
measured count as a '${targetGoos}: N + D' annotation`. **Annotations have always been hand-written
from a sweep's output.** So "how does the sweep bank a darwin annotation from a matrix artifact" has
a simpler answer than it appears: **the sweep does not bank anything anywhere; the question is how
the EVIDENCE travels from a runner to the lane that writes the line.**

Three routes, and the recommendation is the third:

1. **The uploaded artifact** — per-row evidence under `rows/`, plus the full log. Complete, and
   **unreachable from a restricted-egress host** (Azure blob storage answers 403 on CONNECT there),
   which is precisely the gap the annotation route was cut to close.
2. **The job summary** — same blob-storage problem.
3. **The annotation route** (`.github/annotate-summary.ps1`, landed 2026-09-02) — the sweep stage's
   summary comes back from `GET /check-runs/{id}/annotations` as JSON from `api.github.com` itself.
   **Readable by any lane, on any host, restricted egress or not.**

**Recommendation: route 3 is the primary evidence channel, with route 1 as the archive.** The
annotation carries the row's arithmetic — package, matched count, disclosed count, bucket — which is
exactly what an annotation line needs. The artifact remains the deeper record for anyone who can
reach it, and its 7-day retention is a reason to read the summary promptly rather than to rely on it.

**And the bar for banking must be HIGHER than for Linux, for reasons that are about the runner and
not about darwin.** `CIMatrix.md` says it plainly: *"a hosted runner is an unanchored machine; a
number measured here is provisional until a fleet machine agrees or the runner is itself
calibrated."* A Linux annotation is banked from a machine with a measured budget and a known
toolchain provenance. A darwin annotation would be banked from a runner nobody has calibrated.

**Proposed bar — all four, and this is the piece I most want ruled rather than assumed:**

1. **Both mac legs agree**, arm64 and x64, on the same commit. They are different compilations of the
   same flavor; a divergence is a finding about the corpus, not a number to average.
2. **The row's proof page records the OS dimension.** This is already ruled and PARKED — *"manifest
   entries and proof pages gain their OS condition together, i9 + coordinator on the schema"* (v3.4's
   ruling). **A darwin annotation should not land before that schema does**, or the proof page will
   claim a platform it does not name.
3. **Two runs, not one**, until the mac runners have a calibration record the way the fleet's
   machines do. The second run is what turns "the runner said so" into "the runner says so
   reproducibly", and the first darwin dispatches are precisely what builds that record.
4. **The provenance is stated in the bank record**, exactly as this lane states its Ubuntu-built .NET
   SDK: *banked from GitHub-hosted `macos-15` / `macos-15-intel`, run <id>, both legs agreeing*.

---

## 3. Sequencing — and the honest answer about when any of this pays

**None of it is urgent, and the reason is worth stating plainly rather than discovered later.**

**A darwin `sweep-shard` cannot produce a validated row today.** Every converted program dies in a
module initializer on an unimplemented libc trampoline
([`FINDING-darwin-run-layer.md`](FINDING-darwin-run-layer.md)); a sweep would report every row as a
failure, uniformly, for one known reason. **So there is no darwin annotation to bank until the run
layer exists**, which is [`DESIGN-darwin-run-layer.md`](DESIGN-darwin-run-layer.md)'s subject and is
design-with-user territory.

That gives a natural order:

| step | when | cost |
|:--|:--|:--|
| **1. The header loop over `$RosterOsKeys`** + the no-rows-no-line rule and its guard | now, safely — it changes no number today | small; the Linux line's arithmetic is unchanged bit-for-bit |
| **2. `Get-SweepTargetGoos` returns `darwin` on macOS** + the comment corrected | with step 1 | one line |
| **3. The proof page's OS dimension** | when i9 + coordinator open the parked schema | not mine |
| **4. The first darwin annotation** | after the run layer, both legs, twice, provenance stated | gated on step 3 |
| **5. A rendered bubble COLUMN, generated and guarded** | only if wanted after 1–4 | deliberately last |

**Steps 1 and 2 are worth doing now anyway**, independent of darwin: step 1 removes a hardcoded OS
from a guard whose whole job is to derive rather than assume, and step 2 stops a shipped file
asserting something measurably false.

---

## 4. What I did not decide

- **Whether the bubbles get a table column at all.** §1.3 recommends against it *first*, and gives
  the shape it should take if the owner wants it: generated and guarded, never hand-set.
- **The proof page's OS schema** — parked, and not this lane's.
- **Whether a hosted-runner annotation is bankable at all**, or only ever "provisional pending a
  calibrated machine". §2's four-part bar is a proposal; `CIMatrix.md`'s "not a place that writes to
  the repository" could equally be read as *no* hosted number is bankable, in which case the darwin
  column would wait for Apple hardware in the fleet. **That is a ruling, not a measurement**, and it
  is the one question in this document that a lane cannot settle by measuring anything.
