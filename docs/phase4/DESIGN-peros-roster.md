# DESIGN — per-OS roster mechanics: three honest platform records in one roster

> **STATUS: RATIFIED WITH AMENDMENTS** (coordinator, 2026-08-26). An adversarial review lane
> prototyped this note's grammar against the real parser and re-derived its denominators; **the
> shape decision (S-A, the L3 pattern) survived every attack** and the seven amendments A1–A7 are
> all mechanics. They are applied throughout, marked **[A*n*]** where they land. The eight open
> questions are now **ruled** — §13 records the rulings and what remains genuinely open.
>
> **DESIGN ONLY — this note still changes no roster row, no parser, no guard and no sweep.**
> Commissioned at the JOB-024 fold (`7bc998da1`) as the design half of the owner's
> **three-platform parity** ruling of the same day: *every platform reaches 100% of its own
> implementable denominator, with its own disclosure ledger*, reported "without averaging".
>
> **Re-rooted at master `782bb1c99`** — R's per-OS annotation rider (`547ee6c35`) landed between
> this note's first draft and its ratification and repaired the D1 defect in the roster, so §2.1
> reads as dated history rather than as a live defect **[A7]**.
>
> **Every item below is grounded in a measured instance**, from five places: R's JOB-024 Linux
> ledger (all 175 roster rows at pinned master `59af260e0`, go1.23.12 / .NET 10.0.400, WSL2
> Ubuntu-22.04, 4.3 h wall, 15,343 row-seconds — mailbox `2026-08-26 05:05` and its three shards);
> the review lane's prototype-and-re-derivation; the roster at `782bb1c99` (175 rows, 7 linux
> annotations, 7 excluded — `check-roster-format.ps1` reports **256 checks pass**); the harness
> sources; and Go 1.23.12's own sources under `GOROOT` for every build-tag fact.
>
> **Prior art, read first:** the per-OS verdict-arithmetic ruling (BOARD, coordinator 2026-08-22)
> and its landed harness half (`249b47b74`); [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md)
> (layout L3 — one tree, per-GOOS folders, `windows` as the empty-value default), whose shape this
> note deliberately mirrors; [`DESIGN-validation-proof-pages.md`](DESIGN-validation-proof-pages.md);
> and [`GoCorpusMigration.md`](../GoCorpusMigration.md) §H10, whose obligations §11.1 extends.

---

## 1. What exists today, exactly

Five pieces carry the OS dimension between them. Naming them precisely matters, because most of
what follows is *extension of a working mechanism*, not new machinery.

| Piece | Where | What it already does about OS |
|:--|:--|:--|
| Roster columns | `docs/ValidatedTestPackages.md`, `Tests` / `Disclosed` | The **Windows record**, authoritative, never blended |
| Per-OS annotation | the same row's *What it exercises* cell, last middle-dot segment | `linux: N` or `linux: N + D`; `windows` refused **by name**; both anchors load-bearing |
| Parser | `src/_roster.ps1` | `$RosterOsKeys = @('linux','darwin')`, `Get-SweepTargetGoos`, `Get-RosterRowExpectation`, `Get-SweepRowClassification` |
| Guard | `src/check-roster-format.ps1` | §1 parser fixtures, §1b classification rule, §1c ledger parser, §2 derived arithmetic, §3 rendered-column integrity |
| Sweep | `src/run-validated-sweep.ps1` | Reads the roster, resolves the expectation in force for `Get-SweepTargetGoos`, reports five classes: `pass`, `host-conditional`, `unbanked-count` (CVAC), `disclosed-moved`, `count` |

The header carries **two denominators** — 175/215 naive (81.4%) and 175/208 against the
implementable set (84.1%) — plus one Linux line: *"Linux: 7 of 175 rows validated at their Linux
counts — 1,259 matching verdicts · 8 disclosed."* Every one of those numbers is **derived from the
table by the guard**; none is hand-maintained. That is the doctrine this design must not weaken:
**no number without a guard.**

The seven annotated rows are the whole per-OS dataset in existence (post-rider):

| Row | Windows columns | linux annotation |
|:--|:--:|:--:|
| `bytes` | 82 + 6 | `linux: 86 + 6` |
| `crypto/rand` | 298 | `linux: 302` |
| `crypto/sha1` | 12 + 1 | `linux: 13 + 1` |
| `debug/buildinfo` | 197 | `linux: 204` |
| `go/internal/gcimporter` | 583 | `linux: 582` |
| `mime` | 17 + 1 | `linux: 18 + 1` |
| `path/filepath` | 61 | `linux: 54` |

---

## 2. The measured defect set

Six defects, each with the instance that produced it. Nothing here is hypothetical.

| # | Defect | Measured instance | Source |
|:--|:--|:--|:--|
| D1 | A bare annotation on a disclosing row false-reds | `bytes` reported `DISC 86, disclosed 6 vs the linux expectation 0`; `crypto/sha1` the same at 13/1 — both **functionally PASS with Windows' own disclosure sets**. *Repaired in the roster by `547ee6c35`; the grammar hole it came through is still open* | JOB-024 shard 1 + FINAL |
| D2 | Platform-exclusive rows are attempted and fail | `internal/syscall/windows/registry`: *"build constraints exclude all Go files"* on linux; counted among 29 FAILs | JOB-024 shard 3 |
| D3 | The exclusion ledger is Windows-shaped | Four packages rejoin off Windows, not three: `log/syslog`, `internal/runtime/syscall`, `internal/syscall/unix` and **`net/internal/socktest`**, whose `main_test.go` is `//go:build !js && !plan9 && !wasip1 && !windows` and declares `TestSwitch`/`TestSocket` **[A3]** | ledger + `GOROOT` file evidence |
| D4 | Disclosure sets are per-OS but manifests are not | `runtime/debug`: Windows sees **9 eligible** (4 matched + 5 disclosed), Go runs **10** on Linux, because `panic_test.go` is tagged `aix \|\| darwin \|\| dragonfly \|\| freebsd \|\| linux \|\| netbsd \|\| openbsd` ("TODO: test on Windows?"). `TestPanicOnFault` then **kills the host** with a fatal `System.AccessViolationException` — the Linux CLR cannot recover a hardware fault (no SEH) | JOB-024 FINAL + `GOROOT/src/runtime/debug/panic_test.go` |
| D5 | Deadline floors are Windows-shaped | `time`'s 40m floor was sized from its re-derived 169-test 1.23.12 suite measuring **1,146 s** on laptop-class Windows; R's Linux row measured **2,488 s** and came back deadline-partial with ~49 timer-family verdicts absent | `$longTimeouts` + JOB-024 FINAL |
| D6 | Counts diverge legitimately by OS, and a non-Windows-first bank has no home | `crypto/rand` 298/302, `path/filepath` 61/54, `debug/buildinfo` 197/204, `go/internal/gcimporter` 583/582; `net/smtp` 19, `net/http/httptest` 55, `net/http/httputil` 53, `net/rpc` 15 were **Linux-proven before Windows** and had to wait | 2026-08-22 ruling + 2026-08-25 fold |

### 2.1 D1 was worse than a false red — the dated history **[A7]**

At the time of R's ledger the header's Linux line read **"1,259 matching verdicts · 1 disclosed"**,
and the guard proved that line against the annotations. The verdict sum was right
(582+204+302+54+18+86+13 = 1,259); the **disclosed** sum was derived from an incomplete input —
`mime`'s `+ 1` was the only term recorded, while `bytes` and `crypto/sha1` demonstrably disclosed 6
and 1 more on Linux. The published total therefore read **1 where the measurement said 8**, with
every guard passing, because **a derivation cannot rescue an omitted term**.

R's rider (`547ee6c35`, merged `782bb1c99`) added both terms and the header re-sums to 8 — proven
by a deliberate half-edit that made the guard say *"expected 8, got 1"* first. **The data is
repaired; the grammar that allowed it is not.** That is what §4.1 closes, and the history is what
justifies closing it: the defect cost a 4.3-hour sweep to surface as an ambiguous `DISC` when a
seconds-long parse error was available.

### 2.2 What R's 83.4% is, and is not

R's honest Linux figure is **146/175** — 144 PASS + the 2 DISC rows that validate underneath. It is
honest and valuable, and it is *Linux behavior measured against the **Windows** roster*: its
denominator counts `internal/syscall/windows/registry` (which cannot exist on Linux) and omits
`log/syslog` (which can only exist there). Under three-platform parity the two quantities need
distinct names, and this note uses:

- **roster-parity rate** — rows of the (Windows) roster that also validate on OS *X*. R's 146/175.
- **validation rate for OS *X*** — rows banked *at their X counts* over X's own implementable set.
  Today, for Linux, **7 / 211** (§5.3).

The second is what three-platform parity is defined over.

---

## 3. The roster shape: three alternatives, priced

The decision is where per-OS facts live. All three can be made correct; they differ in migration
cost, guard cost and what a visitor sees. **The review lane attacked this decision directly and it
stands.**

### S-A — extend the annotations (one table, per-OS segments)

Keep the Windows columns, keep the middle-dot `goos: N + D` segment, extend the *grammar*, and push
the non-count per-OS facts (exclusions, floors, disclosure scope) to the artifacts that already own
those facts.

- **Migration:** four row edits plus three authority texts and the guard fixtures (§4.1), one new
  ledger column, one new optional manifest field. Every existing row keeps parsing; the
  gate-bearing `$RosterRowPattern` moves only for §8's sentinel.
- **Guard:** extends the five existing sections; each new number derived like its siblings.
- **Readability:** the flagship table stays what a visitor knows. **Cost:** the *What it exercises*
  cell keeps accumulating machine data at its tail; at three platforms a fully-annotated row's
  prose ends in three segments of arithmetic. Real, and the honest price of S-A.

### S-B — OS-keyed columns

- **Migration:** touches **all 175 rows** and changes the rendered column count 4 → 6. The
  document's own machine-parsed comment warns that "reflowing, reordering, or adding columns breaks
  the sweep's roster parser"; `$RosterRowPattern` and §3's five-pipe render assertion both move — a
  whole-table migration of a gate-bearing artifact for a property that today applies to 7 rows.
- **Guard:** cleaner in steady state.
- **Readability:** best at a glance; worst for the 168 rows carrying permanently empty cells, and
  each new platform is another column.

### S-C — per-OS roster files

- **Migration:** a document per platform, a roster-path parameter through `_roster.ps1` and the
  sweep, per-file header arithmetic.
- **Guard:** most expensive — three package sets cross-checked, and three copies of each row's
  *What it exercises* prose (the campaign's largest human asset) kept from diverging. That is
  exactly CLAUDE.md's concurrent-lane hazard: *two lanes solving the same problem produce a silent
  duplication, not a conflict.*
- **Readability:** each platform's page is clean — the genuine attraction.
- **Precedent against it:** the project already ran the two-artifact experiment. The corpus was two
  trees for six weeks and the doctrine that survived was **one tree, per-GOOS folders, a default
  for the empty value**.

### Recommendation, ratified — **S-A**, explicitly the L3 shape

One artifact, per-OS segments, `windows` as what an unset key means. Smallest change that makes
every per-OS fact expressible; keeps the Windows record byte-stable; keeps every number derived by
the guard that already derives its siblings. S-B remains a mechanical transform *from* a
fully-annotated S-A roster and is revisited when annotated rows are the majority rather than 7 of
175 (§13, OQ-2).

---

## 4. The annotation grammar, formalized

Today's grammar, as implemented (`$RosterOsPattern`):

```
annotation   := SEP , goos , ":" , count , [ "+" , disclosed ] , (?= SEP | "|" | EOL )
SEP          := U+00B7 MIDDLE DOT
goos         := "linux" | "darwin"          (* $RosterOsKeys; "windows" refused by name *)
count        := digits                      (* > 0, guarded *)
disclosed    := digits                      (* today: omitted means zero *)
```

Three properties are already right and stay untouched: the **two anchors** (a separator before the
key; a separator, the row's pipe, or end-of-line after the number) so prose cannot false-parse;
**at most one segment per key**; **`windows:` refused by name**.

### 4.1 The disclosure term becomes non-omittable — by VALIDATION, never by regex **[A1, BLOCKING]**

**Rule: an OS annotation always carries its disclosure term. Zero is written `+ 0`.**

**The mechanism is a lenient pattern plus an explicit throw. `$RosterOsPattern` is NOT tightened.**
The review lane prototyped the tightened form against the real parser and measured why it must not
be used:

- **A tightened pattern silently drops 4 of the 7 annotations at master, with zero diagnostics.**
  The parser consumes annotations through
  `foreach ($match in [regex]::Matches($line, $RosterOsPattern))` — a `Matches` loop **does not
  throw**; a segment that stops matching simply ceases to exist. The four bare-but-correct rows
  (`crypto/rand`, `debug/buildinfo`, `go/internal/gcimporter`, `path/filepath`) would vanish from
  `$row.OS`, the Linux header would re-sum to a smaller number, and the sweep would silently
  reclassify those rows as `unbanked-count`. **A rule whose violation is invisible is the same
  failure mode D1 was.**
- **It also disarms three existing refusal guards.** The `windows:`-key, unknown-key and
  duplicate-key fixtures are all written with **bare counts** (`windows: 4`, `plan9: 4`,
  `linux: 4 · linux: 5`). Under a tightened pattern none of them matches, so none of them throws,
  and three `Assert-Throws` checks start failing with *"expected a throw naming …, nothing was
  thrown"* — or, worse in a future edit, passing for the wrong reason.

**Deliverable formulation:**

```
keep  $RosterOsPattern exactly as it is (the disclosed group stays optional)
add   a FOURTH named refusal inside the match loop, after the existing three:

      key is not 'windows'     -> (existing) windows-annotation refusal
      key is in $RosterOsKeys  -> (existing) unknown-key refusal
      key not already seen     -> (existing) duplicate-key refusal
      disclosed group MATCHED  -> NEW: throw naming the row and the rule --
                                  "carries a '<goos>' annotation with no disclosure term;
                                   write '+ 0' when the count is zero"
```

**The ordering is part of the deliverable, not an implementation detail.** Because the new refusal
runs *last*, the three existing refusal fixtures keep working **as written, bare counts and all**:
`windows: 4` throws the windows message, `plan9: 4` the unknown-key message, and
`linux: 4 · linux: 5` the duplicate message — each still naming its own rule rather than being
swallowed by the term check. Reversing the order would make every refusal fixture report the term
error and would erase three guards' meaning.

Why the rule at all, restated now that the data is repaired:

- **Detection moves to the cheap gate.** Absence becomes a *validation* failure the standalone
  guard reports in seconds, instead of a `DISC` after a 4.3-hour sweep that reads like corpus
  breakage (§2.1).
- **It kills the analogy that caused the defect.** Three authority texts today say the term is
  "omitted when zero, mirroring the blank Disclosed column". A blank *column* is a visible absence
  in a rendered table; a missing *text term* is invisible. The column keeps its blank convention;
  the annotation stops borrowing it.

**Re-priced migration for I1 [A7]** — larger than the first draft's "six character edits", because
the rule contradicts three texts of record:

| Kind | Sites |
|:--|:--|
| Roster rows | **4**: `crypto/rand`, `debug/buildinfo`, `go/internal/gcimporter`, `path/filepath` gain `+ 0` (`bytes`/`crypto/sha1` already repaired by the rider) |
| Authority texts | **3**: the roster's machine-parsed HTML comment (*"The + D half is omitted when the disclosed count is zero…"*, `ValidatedTestPackages.md`:135–136), the reader-facing paragraph (*"the matching count, then the disclosed count when there is one"*, :112–114), and `_roster.ps1`'s parser doc (*"the optional `+ <disclosed>` … is omitted when zero"*, :26–28) |
| Guard fixtures | **4** positive fixtures gain `+ 0` (`cond/pkg`, `ann/pkg`, `dar/pkg`, `tail/pkg`) **+ 1 new** failing-first refusal fixture. The **3** refusal fixtures stay unchanged *because of* the ordering rule above — which is why the count is 4+1 rather than the 3 the amendment estimated; the difference is exactly the fixtures the ordering rule saves |

### 4.2 Darwin joins as a key, not as a third term

The grammar is **already** `goos: count` pairs, `darwin` is already in `$RosterOsKeys`, and the
guard already fixtures it. No grammar change is owed for the third platform, and a positional third
term would be actively wrong: it would encode the platform in position (breaking both anchors and
the one-segment-per-key rule) and make the header's per-OS sums positional too. **Decision: pairs,
unchanged. Darwin joins by data.**

One mechanical prerequisite, so it is not discovered later: `Get-SweepTargetGoos` returns `windows`
on macOS **by design** (`_paths.ps1` scopes its pin to `$IsLinux`, because darwin's corpus does not
build yet). A `darwin:` annotation is therefore inert data until the darwin run layer lands
([`FINDING-darwin-run-layer.md`](FINDING-darwin-run-layer.md)) — honest, not a defect, but the
header's Darwin line must exist and read zero rather than be absent, so the arithmetic exists
before the data does.

### 4.3 What the grammar deliberately does **not** absorb

Platform-*exclusive* facts do **not** become an annotation status token such as
`linux: excluded E1`. They are exclusion facts, the exclusion ledger already owns exclusion facts,
and two grammars for one fact is how two records drift apart. §5 scopes the ledger instead, and the
sweep gains one call to the ledger reader `_roster.ps1` already exports.

---

## 5. Per-OS honest denominators and an OS-scoped exclusion ledger

### 5.1 The naive denominator is GOOS-BLIND — one rooted number, 215, for every platform **[A2, BLOCKING]**

The first draft commissioned a per-OS re-derivation of the naive testable count. **That measurement
does not exist and must not be invented**, because the naive denominator has never been a per-OS
quantity:

- **The rule that produces it evaluates no build constraints.** `packageDeclaresGoTests`
  (`src/go2cs/readmeValidationBadge.go`, ~:529) reads a GOROOT package directory, parses every
  `*_test.go` file, and reports whether any `func Test…` is **declared**. It parses rather than
  scans (a doc comment mentioning a test must not count), and it never consults `GOOS`.
- **The board's own reconciliation says the same.** The scout-batch derivation records **216**
  package directories with a `func Test` in their GOROOT sources, *minus* hand-owned `testing`,
  reconciling to the header's **215 exactly** — with no platform anywhere in the arithmetic.

So:

```
naive(os)         = 215                                for every os   (one rooted number)
eligible(os)      = 215 - |E1 rows scoped to os|       (packages Go itself gives tests on)
implementable(os) = 215 - |ALL ledger rows scoped to os|
```

The review lane's re-derivation closes on all three platforms, and its Windows figure lands on the
board's own: **windows 215 − 5 = 210**, **linux 215 − 3 = 212**, **darwin 215 − 5 = 210**. Windows'
implementable then falls out as 210 − `os/user` (E2) − `internal/unsafeheader` (E3) = **208**,
which is exactly the number published today — the reconciliation that makes this formulation
trustworthy rather than merely tidy.

**Consequence for OQ-7:** the commissioned measurement dissolves. What is derived per OS is not a
denominator but the **ineligible SET**, as reviewable ledger rows — and a set of named packages is
auditable in a way a bare count never is. Those sets are still derived **twice, independently,
under a parse-count control** (the 2026-08-25 frontier precedent); what changes is that the
artifact of the derivation is rows a reader can check, not a number a reader must trust.

### 5.2 The ledger gains a `Scope` column, and the rejoin set is FOUR **[A3]**

```
| Package | Verdicts | Class | Scope | Mechanism | Rooting |
```

`Scope` is a comma-separated GOOS list or `all`, and it states *where the exclusion holds*. The
ledger must carry **every platform-ineligible package**, which adds two rows that have never been
in it, because until now the ledger only had to answer for Windows:

| Package | Class | Scope | Evidence |
|:--|:--:|:--|:--|
| `internal/runtime/syscall` | E1 | windows, darwin | only `syscall_linux_test.go` exists |
| `internal/syscall/unix` | E1 | windows, darwin | `siginfo_linux_test.go`, `kernel_version_solaris_test.go` |
| `log/syslog` | E1 | windows | `syslog_test.go` is Unix-tagged — testable on linux/darwin |
| `net/internal/socktest` | E1 | **windows** | `main_test.go` is `//go:build !js && !plan9 && !wasip1 && !windows` and declares `TestSwitch`/`TestSocket` — **it rejoins off Windows; the previous "no test entry points anywhere" reading was wrong** |
| `runtime/race` | E1 | all | no `-race` build exists in the converted corpus on any platform |
| `os/user` | E2 | windows | the broken oracle is host-specific; another OS must re-measure before it is scoped there |
| `internal/unsafeheader` | E3 | all | structural: a managed slice is not the `{Data,Len,Cap}` triple |
| *(new)* `internal/syscall/windows` | E1 | linux, darwin | `exec_windows_test.go`, `version_windows_test.go` declare `TestRunAtLowIntegrity`/`TestSupportUnixSocket`; nothing eligible off Windows |
| *(new)* `internal/syscall/windows/registry` | E1 | linux, darwin | *"build constraints exclude all Go files"* (D2) — the banked-and-excluded case |

So the **rejoin set off Windows is four** — `log/syslog`, `internal/runtime/syscall`,
`internal/syscall/unix`, `net/internal/socktest` — and the linux-ineligible set is the three the
amendment names (`internal/syscall/windows`, `internal/syscall/windows/registry`, `runtime/race`),
with darwin adding `internal/runtime/syscall` and `internal/syscall/unix`.

**The `Verdicts` cell becomes per-OS for a scoped row, and must say which OS's number it carries.**
Today the column is the naive count a suite *would* contribute — `0` where the platform yields no
eligible test, `—` where no baseline exists. For a row scoped to more than one OS those need not be
one number, and for `internal/syscall/windows/registry` they emphatically are not: it contributes 0
on linux and darwin while being a **banked 6** on Windows. The cell therefore reads as the value
**for the scoped platforms**, a row scoped `all` carries the one number that holds everywhere, and
a `0` in a scoped row is never a statement about an unscoped platform.

**The reviewer's note, recorded rather than acted on:** a `Scope = all` on an **E1** row is the tell
that the class conflates a *build-constraint* fact with a *corpus* fact. `runtime/race` is
ineligible everywhere not because Go's constraints exclude it on every platform, but because the
converted corpus has no `-race` build at all — a property of the conversion, not of the target.
Recorded as an observation; **no new class is minted** and no ruling is asked for.

### 5.3 The header states every platform, and the naive line stays **[A4]**

The three per-OS lines **supplement** the naive line; they do not replace it. The owner's
both-numbers ruling is binding — *"The line above measures against every package that defines a
`Test` function; this one against the packages a faithful managed conversion can honestly validate
at all"* — and adding platforms does not retire it. The resulting header block:

```
> ### Phase 4 progress: **175 / 215 testable packages validated — 81.4%**
>
> **18,979 matching test verdicts · 87 disclosed** … Denominator: the 215 of 302 converted
> standard-library packages whose Go 1.23.12 sources declare `Test` functions — a GOOS-blind
> count, identical for every platform.
>
> **Against the implementable set (215 − 7 excluded on windows = 208): 175 / 208 — 84.1%.**
> Both numbers are always reported.
>
> **Windows: 175 / 208 — 84.1%** · 18,979 matching verdicts · 87 disclosed
> **Linux: 7 / 211 — 3.3%** · 1,259 matching verdicts · 8 disclosed
> **Darwin: 0 / 209 — 0.0%** · no rows yet
```

(Linux's 211 = 215 − `internal/syscall/windows` − `…/registry` − `runtime/race` −
`internal/unsafeheader`; darwin's 209 subtracts `internal/runtime/syscall` and
`internal/syscall/unix` as well. Both move by one if `os/user`'s oracle is re-measured broken there
too — a measurement the Scope column is *designed* to make owed rather than assumed.)

**"The Windows block unchanged" is withdrawn.** The first draft claimed the Windows arithmetic
would not move; the review lane showed five named sites that must, and I2 owns all of them:

| Site | What changes |
|:--|:--|
| `check-roster-format.ps1`:289–290 | the excluded-vs-validated disjointness assertion becomes **OS-scoped** (a package may be a roster row *and* excluded on a different OS; never on the same one) |
| `check-roster-format.ps1`:297 | `$implementable = $testable - $ledger.Count` becomes `$testable -` the count of ledger rows **scoped to windows** |
| `check-roster-format.ps1`:301 | *"excluded count equals the ledger row count"* becomes the scoped count |
| `check-roster-format.ps1`:331–332 | the Linux line's denominator stops being *"the whole table"* and becomes `implementable(linux)` |
| `ValidatedTestPackages.md` header | *"The seven, each with its class…"* becomes seven-**on-windows** prose, and the ledger's intro (*"Seven of those cannot be validated at all"*) gains its scope |

Every one of those is still **derived**; what changes is what they are derived *from*.

### 5.4 What the sweep does with a scoped exclusion

A sixth classification, `platform-excluded`: the row is **skipped, reported by name, and counted in
neither the pass nor the fail column** — the same honesty shape `unbanked-count` (CVAC) already
has. It converts D2 from a FAIL needing a footnote into a line stating a fact.

---

## 6. Per-OS disclosure sets

### 6.1 The measured instance, in full

`runtime/debug` on Windows: **4 matched · 5 disclosed** over 9 eligible tests, the 5 spanning three
classes — `host-limit` (`TestStack`), `runtime-capability` (three `WriteHeapDump` tests),
`codegen-liveness` (`TestFreeOSMemory`). On Linux Go runs **10**: `panic_test.go` is tagged for the
Unix family and Windows is excluded by Go's own source. The extra test, `TestPanicOnFault`, **kills
the converted host** with a fatal `System.AccessViolationException`, because the Linux CLR cannot
recover a hardware fault. Same converted code, same runtime version, different platform contract.

Two facts ride along. First, `TestFreeOSMemory` is *already* disclosed on Windows under a **prefix**
pin (`"less than 16777216 released:"`) and R's Linux capture reads
`less than 16777216 released: 0 -> 1921024`, so the existing pin should match there too and the
row's Linux delta is probably `TestPanicOnFault` alone — to be settled by measurement, not inference
(§13, OQ-8). Second, R's shard-2 "per-OS disclosure-capture defect" (`crypto/ed25519`) was
**retracted by its own re-read**: the disclosure machinery is per-OS identical on the evidence.
This design therefore introduces **no per-OS capture behavior at all** — the only per-OS dimension
is *which entries apply*.

### 6.2 Manifest schema v2: scope an entry, gate the version **[A6]**

`go2cs_test_disclosures.json` is hand-owned, committed, and read by `loadTestDisclosures`
(`testConversion.go`) into `map[string]testDisclosure`; **24** manifests exist in the corpus. Two
changes, and the second is not optional:

**(a) An optional `goos` field on an entry.**

```
{ "name": "TestPanicOnFault", "class": "...", "signature": "...", "reason": "...",
  "goos": ["linux", "darwin"] }
```

Absent `goos` = every platform, so all 24 committed manifests keep working byte-for-byte. Filtering
happens at **load** time in `loadTestDisclosures` — one place — so `matchTerminalStatuses` and the
whole oracle are untouched, and the anti-laundering clauses (`platform-skip` as the sole key
admitting Go=pass/C#=skip; `runtime-capability` pinning its rows AS FAILING) keep being enforced
exactly where they are now. Validation stays loud per the file's own doctrine (*a broken disclosure
must not widen the oracle*): unknown GOOS, an empty array, or a duplicate `name` within one GOOS is
an error, never a silent no-op.

**(b) A version gate, because today there is none.** `testDisclosureManifest.SchemaVersion` is
parsed (`testConversion.go`:5049) and **never read** — `loadTestDisclosures` validates the entries
and ignores the version entirely. Without a gate, an **un-rebuilt converter** meeting a v2 manifest
would ignore the `goos` key it does not know and apply a Linux-only disclosure **on every
platform**: silent oracle-widening, precisely what the manifest's own doctrine forbids, and
structurally the stale-binary family CLAUDE.md catalogues as false-green routes #1/#4/#5. **The
loader refuses a schemaVersion it cannot honor**, naming the file and the version, and that refusal
is the first thing the v2 work lands.

**The filter key is `goosOfTarget(options.targetPlatform)`** — the GOOS half of the converter's own
target (`windows/amd64` → `windows`), never the ambient environment. Its **precondition must be
stated where the filter lives**: the converter's target GOOS and the MSBuild `$(GoTargetOS)` that
selects which per-GOOS corpus flavor is compiled must agree, or a run would scope disclosures for
one platform while measuring a corpus built for another. On the current instruments they do agree
(`_paths.ps1` pins `GoTargetOS=linux` on a Linux host, and the sweep runs the converter on that
same host), which is why this is a stated precondition rather than new mechanism — but it is
exactly the kind of agreement that holds until someone cross-builds, so it is written down.

**Why not one manifest per platform:** `runtime/debug` would duplicate 5 of 6 entries, each
carrying multi-paragraph campaign prose whose exactness is the point, and the copies would diverge
silently — the same argument that rejects S-C.

### 6.3 The class question, and the isolation PREREQUISITE **[A7]**

`TestPanicOnFault` cannot simply be classified, because **it produces no captured output to pin**:
the host dies. Every disclosure class in existence rests on a signature found in the converted
side's failure text. Worse, the damage is not one verdict: `TestPanicOnFault` sorts **second** of
`runtime/debug`'s ten tests, and the host reports in sorted order, so the crash costs the
**alphabetical tail — roughly 8 of 10 verdicts** — which is the exact signature CLAUDE.md teaches
must be read as a died-partway run rather than as divergence.

Three ways out, and the dependency between them is the correction:

1. **Platform-gate it in the converted test host** — barred outright. `platform-skip`'s admission
   test is binding: the skip must be *the upstream test's own skip statement*, never one the
   harness injects.
2. **A per-test, OS-scoped exclusion** — the ledger's E-classes applied at test granularity: the
   test named, its mechanism recorded, its verdict pinned as *not runnable on this OS*, accounted
   like a disclosure.
3. **Per-test process isolation on the affected OS** — a real harness change.

**(3) is the PREREQUISITE for (2), not an upgrade to it.** Without isolation there is no capturable
exit and no signature, so (2) would be an accounting entry resting on the absence of evidence — and
it would still leave ~8 verdicts unmeasured, which no accounting class can repair. With isolation
the fatal test costs one verdict, the crash becomes a capturable non-zero exit, the other nine
verdicts are measured, and (2) can be pinned the way every other class is. **Ruled: class (2), with
(3) landing first** (§13).

---

## 7. Per-OS floors

`$longTimeouts` is eight entries, all sized from Windows measurements, and it is a **floor, not an
override**. D5 is the first proven mis-sizing across an OS boundary: `time`'s 40m came from
**1,146 s** on laptop-class Windows; the same laptop class under WSL2 measured **2,488 s** and
returned a deadline-partial row with ~49 timer-family verdicts missing — the alphabetical-tail
signature again, which anyone without R's shard notes would have read as divergence.

**OS-keyed floors, not host-class-keyed:**

```
$longTimeouts = @{
    'time' = @{ default = '40m'; linux = '90m' }
    ...
}
```

`default` means "every OS without its own entry" (the L3 rule again); precedence is unchanged —
effective budget = max(entry-for-this-OS, `-TestTimeout`).

Not host-class, because the repository has **no host-class primitive**: `_paths.ps1` exposes
`$IsWindowsHost`, `$ExeSuffix` and `$IsLinux`, and nothing that distinguishes an i7-5820K from a
Ryzen 6850U. Introducing one would be a machine registry that goes stale exactly as the budget
table's i9 rows did. The existing doctrine already answers it: *a deadline is a safety net against
a hung run, never a performance assumption* — size to the slowest legitimate host of that OS; a
fast box pays only how long a rare genuine hang takes to be declared.

**The confound is stated in the entry, not hidden:** R's datum compares native Windows against
**WSL2** on one laptop, so it does not separate "Linux" from "WSL2 filesystem behavior". For a
floor that is fine — both are legitimate hosts of the Linux corpus and the floor covers the slower
— but the comment must say so, exactly as every existing entry carries its evidence. The 90m
re-run's result is the number that lands; until it posts the entry is unwritten.

---

## 8. Roster-row semantics for OS-divergent counts and non-Windows-first banks

**(a) A row banked on Windows that also validates elsewhere at a different count.** Solved today —
annotate; `crypto/rand` 298/302 and `path/filepath` 61/54 are the working precedent. Nothing new
beyond §4.1's mandatory term.

**(b) A row banked on Windows that is structurally impossible elsewhere.** `registry`, D2 — solved
by §5.2's ledger scope plus §5.4's `platform-excluded` classification. The row itself does not
change.

**(c) A row that can validate elsewhere and NOT on Windows.** `log/syslog` is the clean case: Go's
own constraints give it no Windows tests, so under today's shape it can never become a roster row
and its Linux verdicts have nowhere honest to live. This is the only case needing a schema change,
and the review re-priced it — **the em-dash sentinel touches four things, not one**:

| Site | Change |
|:--|:--|
| `$RosterRowPattern` (`_roster.ps1`) | the count captures admit `—` beside `\d+`; the parsed row gains `HasWindowsRecord = $false`, `Expected = $null` |
| Both header numerators | the Windows sums must skip sentinel rows (they contribute no numbers) and the per-OS numerator must count them — two derivations, not one |
| The render guard's predicate | §3's five-pipe assertion iterates rows *that parse*; a sentinel row must parse and still be pipe-checked |
| The sweep's null handling | `Get-RosterRowExpectation` has no columns fallback for such a row — asking for one is an **error**, never a silent zero — and `Get-SweepRowClassification` must never be handed a `$null` expectation |

Plus two invariants, guarded both ways: a sentinel row **must** carry at least one OS annotation,
and **must** have a ledger row whose Scope includes `windows`. `windows:` stays refused as a
*count*; a sentinel row's Windows status is the ledger's business, so "a row never holds two
Windows answers" stays literally true.

**A merely-pending row is still not admitted.** A package validated on Linux and simply not yet
validated on Windows keeps waiting, exactly as `net/smtp`, `net/http/httptest`, `net/http/httputil`
and `net/rpc` waited. Structural impossibility is a permanent fact and earns the sentinel; "not
yet" is a schedule (§13, OQ-3).

---

## 9. Guard extensions — every new surface derived and guarded on day one

Mapped onto `check-roster-format.ps1`'s existing sections, so nothing new needs an instrument.

**§1 — parser contract (fixtures).**
- A bare `linux: 86` **throws**, naming the row and the rule (§4.1's fourth refusal), proven
  failing-first.
- `linux: 86 + 0` parses with `Disclosed = 0`; the four positive fixtures gain `+ 0`.
- The three refusal fixtures stay **unchanged**, which is itself the assertion that the validation
  order (key → duplicate → term) is right.
- A sentinel row parses with `HasWindowsRecord` false; a sentinel row with no OS annotation is
  refused by name.

**§1b — classification rule.** Two new buckets as pure functions: `platform-excluded`, and the
sentinel row's expectation resolution (the annotation answers; asking for a columns fallback is an
error). The Windows rows stay first in the file and stay the proof that the reachable Windows set
is unchanged.

**§1c — ledger parser.** `Scope` parsed; every value in `$RosterOsKeys + 'windows'` or `all`; class
still one of E1/E2/E3; disjointness **OS-scoped** (§5.3's :289–290).

**§2 — arithmetic, per OS.** 215 asserted as the single GOOS-blind naive denominator with its
rooting present; `implementable(os)` derived from the scoped ledger count; each per-OS line's row
count, verdict sum, disclosed sum and percentage derived from the annotations; a platform with no
annotated rows still renders its line (§4.2).

**§3 — render integrity.** The pipe-count assertions extend to the ledger's new column (6 → 7) and
to sentinel rows (still 5 for the roster). This is the section that caught the `log` row's phantom
fifth column; it grows with every column added.

**§4 (new) — configuration integrity.** Every `$longTimeouts` key is a package on the roster (a
floor for a non-roster package is dead config); every per-OS floor key is a known GOOS.

**Converter side (Go, under the plain `go test ./...`).** The schemaVersion gate (§6.2b) and the
`goos` field's validation, guarded in the `disclosedParentAggregation_test.go` family: an
unhonorable version refuses; an unknown GOOS errors; an absent field applies everywhere; a scoped
entry is invisible to another target's oracle.

**Failing-first, for every one of them** — CLAUDE.md's rule and the 2026-08-22 harness lane's own
precedent (two failing-first proofs): each check lands with a deliberate regression, the
exact-message proof, and a byte-identical restore.

**Wiring.** The guard stays **standalone** per the coordinator's 2026-08-22 note, and is named
explicitly as a step in the banking ritual (§13, OQ-6).

---

## 10. The proof-page namespace moves NOW **[A5]**

`writeValidationProofPage` writes **flat** to `docs/validation/current/<dot-id>.md`, and
`proofPageProvenance.platform` is **CONTENT**, not provenance — "a Go-version or platform change is
a different claim and must rewrite the page". `emitValidationProofPage` fires on every run whose
comparison reaches `validated`, whatever the target GOOS. So **a Linux `-test-action all` run
rewrites the Windows page today**, and through the totals line the badge emitter reads back, the
package README's Tests badge with it.

The first draft deferred this to the anchor release and proposed a comment in the sweep's drift
note. **A comment detects nothing.** Both halves land now:

1. **Route non-windows targets to `current/<goos>/<dot-id>.md`; windows stays flat.** Roughly three
   lines in `writeValidationProofPage` (join the GOOS segment into `currentPath` when the target is
   not windows; `MkdirAll` covers it). **Zero published URLs move**, and there is **zero index
   churn**: `writeValidationIndex` already skips directory entries
   (`if entry.IsDir() … continue`), so a per-GOOS subdirectory is invisible to the generated roster
   page — the index stays the Windows roster until a per-OS index is wanted, which is a later and
   separate question.
2. **Widen the sweep's drift pathspec.** `run-validated-sweep.ps1`:618 runs
   `git … diff --numstat --ignore-cr-at-eol -- src/core`; `docs/validation` joins the pathspec so a
   page a run rewrote is *reported* rather than left to be noticed. Detection, not prose.

The badge's totals-line read must then name which OS's totals it took, or badges silently become
last-sweep-wins.

---

## 11. Increments (landing order, each independently bankable)

| # | Increment | Depends on | Size |
|:--|:--|:--|:--|
| I1 | §4.1 lenient regex + fourth refusal, ordering rule, 4 row edits, 3 authority texts, 4+1 fixtures (failing-first) | — | small |
| I2 | §5 ledger `Scope` column (9 rows incl. two new), OS-scoped disjointness, the five header/guard sites of §5.3, `platform-excluded` sweep class | I1 | medium |
| I3 | §5.1 per-OS **ineligible sets** derived twice under parse-count control; the three per-OS header lines | I2 | small — the derivation's artifact is reviewable rows, not a measurement campaign |
| I4 | §6.2 schemaVersion **gate** first, then the `goos` field, loader validation, Go guards, page scope rendering | — (independent) | small-medium |
| I5 | §10 proof-page per-GOOS routing + sweep drift pathspec | — (independent) | ~3 lines + 1 |
| I6 | §7 OS-keyed floors, `time` first, once the 90m re-run posts | — | small |
| I7 | §8(c) sentinel rows — all four sites | I2, I3 | medium; touches the gate-bearing regex |
| I8 | §6.3 per-test process isolation, then the per-test OS-scoped exclusion | ruling + harness work | largest |
| I9 | Darwin joins by data | I3, the darwin run layer | none in code |

I1 alone turns D1's whole class from a four-hour ambiguous failure into a seconds-long validation
error. I1+I2 change what R's next Linux sweep *says* without changing what it measures: `bytes` and
`crypto/sha1` already PASS post-rider, and `registry` reports `platform-excluded` instead of FAIL —
so the ledger reads **146 PASS of 174 comparable rows** rather than 146 of 175 with one row failing
for existing on the wrong platform. Same measurement, honest presentation, no corpus change.

### 11.1 What this adds to the hop runbook's H10 **[A7]**

[`GoCorpusMigration.md`](../GoCorpusMigration.md) §H10 (*Roster, proof-page and disclosure
re-derivation*, a ⟲ GATE) re-derives every row from the new release's own test sources. Per-OS
mechanics add four obligations and remove one:

1. **Scope is re-derived, never carried.** A ledger row's `Scope` is a fact about the *release's*
   build tags; a package can gain or lose a platform's tests between releases exactly as counts
   move. H10 re-derives the per-OS ineligible sets alongside the counts.
2. **Floors are re-checked per OS.** H10's step 5 already re-checks the deadline floors; with §7
   that is per-OS entries, and the OS with the slowest legitimate host owns the number.
3. **Disclosure re-signing gains a shard PLATFORM-AFFINITY rule.** H10's step 3 re-signs each
   manifest against the new release's failure text; a `goos`-scoped entry can only be re-signed by
   a shard **running on that platform**, because its signature comes from that platform's capture.
   The shard map (§3 of the runbook) must therefore assign a scoped row to a machine of the
   matching OS, the way it already derives its reserved floor set from the script.
4. **[A2] removes the six per-hop denominator re-derivations.** With `naive(os)` GOOS-blind there is
   one naive number per hop, not three, and no per-OS implementable denominator is measured at all —
   each is derived from the scoped ledger. Three platforms × (naive + implementable) = six
   re-derivations that simply do not exist.

---

## 12. Non-goals

- **No blending, ever.** No averaged counts, no "best of" per row, no footnote hiding a difference.
- **The Windows *record* does not move**, even though five Windows-facing *derivations* do (§5.3):
  the columns stay Windows-authoritative for the 1.23.x era and no banked number changes.
- **No new instrument.** Everything extends `_roster.ps1`, `check-roster-format.ps1`,
  `run-validated-sweep.ps1`, `validationProofPages.go` and the manifest loader.
- **No per-OS capture behavior** in the disclosure oracle (§6.1, R's retraction).
- **No new exclusion class** — §5.2's `Scope = all` observation is recorded, not acted on.
- **No corpus or converter-emission change.**

---

## 13. Rulings recorded, and what remains open

Ruled by the coordinator at ratification (2026-08-26):

| # | Question | Ruling |
|:--|:--|:--|
| OQ-1 | Mandatory disclosure term | **(a) always required, zero written `+ 0`** — via A1's mechanism: lenient regex, explicit throw, validation ordered key → duplicate → term |
| OQ-2 | S-A vs S-B | **S-A**, with the revisit trigger as written (reconsider when annotated rows are the majority, not 7 of 175) |
| OQ-3 | May a row enter the roster on a non-Windows bank? | **Structural impossibility only**, via the sentinel; "not yet" is a schedule. Mechanism re-priced: **four** sites (§8(c)) |
| OQ-4 | `TestPanicOnFault`'s accounting class | **Class (2)**, the per-test OS-scoped exclusion, **with (3) per-test process isolation as its PREREQUISITE** — the crash costs the alphabetical tail (~8 of 10 verdicts), and only isolation restores a capturable exit and a signature |
| OQ-5 | Proof-page namespace | **Per-OS paths, windows flat — brought FORWARD** (§10), not deferred to the anchor release, plus the drift-pathspec widening |
| OQ-6 | Guard wiring | **Standalone**, named explicitly in the banking ritual |
| OQ-7 | Who derives `naive(linux)` | **Reframed**: there is no per-OS naive denominator (A2). What is derived is the per-OS **ineligible set**, twice, under parse-count control, as reviewable ledger rows |
| OQ-8 | `runtime/debug`'s Linux arithmetic | **Settles after OQ-4's isolation exists** — until a run can survive `TestPanicOnFault` the row cannot report its other nine verdicts, so the annotation cannot be written honestly |

Genuinely open, and deliberately not ruled here:

- **`os/user`'s scope off Windows.** Its E2 exclusion is a *host* fact (Go's own `TestGroupIds`
  fails in the oracle); whether it holds on Linux and darwin is a measurement nobody has taken, and
  the Scope column is what makes that debt visible instead of assumed. Both per-OS implementable
  denominators in §5.3 move by one if it is broken there too.
- **A per-OS validation index page.** `docs/validation/index.md` is Windows-shaped by construction
  once §10 lands, since the generator skips directories. Deliberately left until a second platform
  has rows worth indexing.

---

## Appendix — the measured instances this design is built on

| Instance | Value | Where measured |
|:--|:--|:--|
| Linux full-roster ledger | 175 rows, 144 PASS · 2 DISC · 29 FAIL; honest 146/175 (83.4%); 4.3 h, 15,343 row-seconds | R, JOB-024 FINAL, pinned `59af260e0`, WSL2, go1.23.12 / .NET 10.0.400 |
| Bare-annotation false red | `bytes` 86 matched with Windows' 6 disclosures vs expectation 0; `crypto/sha1` 13/1 | JOB-024 shard 1 |
| Linux disclosed total, before the rider | header read 1; the annotations omitted 6 + 1 | §2.1; repaired by `547ee6c35`, merged `782bb1c99` |
| Tightened-regex prototype | silently drops **4 of 7** annotations at master, zero diagnostics; disarms **3** refusal fixtures | review lane, against the real parser |
| GOOS-blind naive denominator | `packageDeclaresGoTests` parses `_test.go` declarations and evaluates no build constraints (`readmeValidationBadge.go` ~:529); the board's **216 − 1 hand-owned `testing` = 215** reconciliation | source + BOARD scout-batch derivation |
| Per-OS eligible re-derivation | windows 215 − 5 = **210** (the board names the same five), linux 215 − 3 = **212**, darwin 215 − 5 = **210**; windows implementable 210 − 2 = **208**, matching the published number | review lane |
| Rejoin set off Windows | **four**: `log/syslog`, `internal/runtime/syscall`, `internal/syscall/unix`, `net/internal/socktest` (`main_test.go` `//go:build !js && !plan9 && !wasip1 && !windows`, declaring `TestSwitch`/`TestSocket`) | `GOROOT` sources |
| Second linux/darwin-ineligible row | `internal/syscall/windows` — `exec_windows_test.go`, `version_windows_test.go` declare `TestRunAtLowIntegrity`/`TestSupportUnixSocket` | `GOROOT` sources |
| Platform-exclusive row | `internal/syscall/windows/registry`: "build constraints exclude all Go files" on linux | JOB-024 shard 3 |
| Per-OS disclosure set | `runtime/debug`: 9 eligible on Windows (4 + 5 disclosed), 10 on Linux; `panic_test.go` tagged `aix\|darwin\|dragonfly\|freebsd\|linux\|netbsd\|openbsd`; `TestPanicOnFault` fatal `System.AccessViolationException`, sorting **second of ten** so the crash costs the tail | proof page + `GOROOT` + JOB-024 FINAL |
| Manifest surface | **24** committed `go2cs_test_disclosures.json`; `schemaVersion` parsed at `testConversion.go`:5049 and **never read** | corpus + source |
| Per-OS floor | `time` 40m from 1,146 s (Windows, laptop-class, 169 tests); 2,488 s deadline-partial on Linux, ~49 timer verdicts absent; 90m re-run in flight | `$longTimeouts` history + JOB-024 FINAL |
| OS-divergent counts | 298/302, 61/54, 197/204, 583/582, 17+1 / 18+1, 82+6 / 86+6, 12+1 / 13+1 | the seven annotated rows |
| Linux-first precedent | `net/smtp` 19, `net/http/httptest` 55, `net/http/httputil` 53, `net/rpc` 15 — Linux-proven, Windows-walled, held | 2026-08-22 S3 ledger; 2026-08-25 fold |
| Guard state at this note's base | 256 checks pass — 175 rows, 7 linux annotations, 7 excluded | `check-roster-format.ps1` at `782bb1c99` |
| Proof-page namespace | flat `current/<dot-id>.md`; `platform` is CONTENT; emitted on every validated run; `writeValidationIndex` skips directories | `validationProofPages.go` |
| Sweep drift pathspec | `git diff --numstat --ignore-cr-at-eol -- src/core` — `docs/validation` is outside it | `run-validated-sweep.ps1`:618 |
