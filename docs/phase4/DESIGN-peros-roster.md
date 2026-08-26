# DESIGN — per-OS roster mechanics: three honest platform records in one roster

> **STATUS: DESIGN ONLY — nothing here is implemented, and this note changes no roster row, no
> parser, no guard and no sweep.** Commissioned by the coordinator at the JOB-024 fold
> (`7bc998da1`, 2026-08-26) as the design half of the owner's **three-platform parity** ruling of
> the same day: *every platform reaches 100% of its own implementable denominator, with its own
> disclosure ledger*, reported "without averaging".
>
> **Every item below is grounded in a measured instance**, and the measurements come from four
> places: R's JOB-024 Linux ledger (all 175 roster rows at pinned master `59af260e0`, go1.23.12 /
> .NET 10.0.400, WSL2 Ubuntu-22.04, 4.3 h wall, 15,343 row-seconds — mailbox `2026-08-26 05:05`
> and its three shards); the roster itself at this branch's base (175 rows, 7 linux annotations,
> 7 excluded — `check-roster-format.ps1` reports **256 checks pass**); the harness sources
> (`src/_roster.ps1`, `src/check-roster-format.ps1`, `src/run-validated-sweep.ps1`,
> `src/go2cs/testConversion.go`, `src/go2cs/validationProofPages.go`); and Go's own 1.23.12
> sources under `GOROOT` for the build-tag facts.
>
> **Prior art, read first:** the per-OS verdict-arithmetic ruling (BOARD, coordinator 2026-08-22)
> and its landed harness half (`249b47b74`, four rows encoded, Windows byte-identical);
> [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) (layout L3 — one tree,
> per-GOOS folders, `windows` as the empty-value default), whose shape this note deliberately
> mirrors; [`DESIGN-validation-proof-pages.md`](DESIGN-validation-proof-pages.md) (the proof pages
> and badges the roster's numbers publish through).

---

## 1. What exists today, exactly

Five pieces carry the OS dimension between them. Naming them precisely matters, because most of
what follows is *extension of a working mechanism*, not new machinery.

| Piece | Where | What it already does about OS |
|:--|:--|:--|
| Roster columns | `docs/ValidatedTestPackages.md`, `Tests` / `Disclosed` | The **Windows record**, authoritative, never blended |
| Per-OS annotation | the same row's *What it exercises* cell, last middle-dot segment | `linux: N` or `linux: N + D`; `windows` refused **by name**; both anchors load-bearing |
| Parser | `src/_roster.ps1` | `$RosterOsKeys = @('linux','darwin')`, `Get-SweepTargetGoos`, `Get-RosterRowExpectation`, `Get-SweepRowClassification` |
| Guard | `src/check-roster-format.ps1` | §1 parser fixtures, §1b classification rule, §1c ledger parser, §2 derived arithmetic (incl. the Linux progress line), §3 rendered-column integrity |
| Sweep | `src/run-validated-sweep.ps1` | Reads the roster, resolves the expectation in force for `Get-SweepTargetGoos`, reports five classes: `pass`, `host-conditional`, `unbanked-count` (CVAC), `disclosed-moved`, `count` |

The roster's header carries **two denominators** — 175/215 naive (81.4%) and 175/208 against the
implementable set (84.1%) — plus one Linux line: *"Linux: 7 of 175 rows validated at their Linux
counts — 1,259 matching verdicts · 1 disclosed."* Every one of those numbers is **derived from the
table by the guard**, none is hand-maintained. That is the doctrine this design must not weaken:
**no number without a guard.**

The seven annotated rows, verbatim, are the whole per-OS dataset in existence:

| Row | Windows columns | linux annotation |
|:--|:--:|:--:|
| `bytes` | 82 + 6 | `linux: 86` |
| `crypto/rand` | 298 | `linux: 302` |
| `crypto/sha1` | 12 + 1 | `linux: 13` |
| `debug/buildinfo` | 197 | `linux: 204` |
| `go/internal/gcimporter` | 583 | `linux: 582` |
| `mime` | 17 + 1 | `linux: 18 + 1` |
| `path/filepath` | 61 | `linux: 54` |

---

## 2. The measured defect set

Six defects, each with the instance that produced it. Nothing here is hypothetical.

| # | Defect | Measured instance | Source |
|:--|:--|:--|:--|
| D1 | A bare annotation on a disclosing row false-reds | `bytes` reported `DISC 86, disclosed 6 vs the linux expectation 0`; `crypto/sha1` the same at 13/1 — both **functionally PASS with Windows' own disclosure sets** | JOB-024 shard 1 + FINAL |
| D2 | Platform-exclusive rows are attempted and fail | `internal/syscall/windows/registry`: *"build constraints exclude all Go files"* on linux; counted among 29 FAILs | JOB-024 shard 3 |
| D3 | The exclusion ledger is Windows-shaped | `log/syslog` is E1 *on windows* and carries `syslog_test.go`; `internal/runtime/syscall` carries `syscall_linux_test.go`; `internal/syscall/unix` carries `siginfo_linux_test.go` — three of the seven exclusions **rejoin on Linux** | ledger + `GOROOT` file evidence |
| D4 | Disclosure sets are per-OS but manifests are not | `runtime/debug`: Windows sees **9 eligible** (4 matched + 5 disclosed), Go runs **10** on Linux, because `panic_test.go` is tagged `aix \|\| darwin \|\| dragonfly \|\| freebsd \|\| linux \|\| netbsd \|\| openbsd` ("TODO: test on Windows?"). `TestPanicOnFault` then **kills the host** with a fatal `System.AccessViolationException` — the Linux CLR cannot recover a hardware fault (no SEH) | JOB-024 FINAL + `GOROOT/src/runtime/debug/panic_test.go` |
| D5 | Deadline floors are Windows-shaped | `time`'s 40m floor was sized from its re-derived 169-test 1.23.12 suite measuring **1,146 s** on laptop-class Windows; R's Linux row measured **2,488 s** and came back deadline-partial with ~49 timer-family verdicts absent; a 90m re-run was in flight at the fold | `run-validated-sweep.ps1` `$longTimeouts` + JOB-024 FINAL |
| D6 | A row's counts diverge legitimately by OS, and a non-Windows-first bank has no home | `crypto/rand` 298/302, `path/filepath` 61/54, `debug/buildinfo` 197/204, `go/internal/gcimporter` 583/582 (annotated); `net/smtp` 19, `net/http/httptest` 55, `net/http/httputil` 53, `net/rpc` 15 were **Linux-proven before Windows** and had to wait for a Windows bank to become rows at all | 2026-08-22 ruling + 2026-08-25 fold |

### 2.1 D1 is worse than a false red — it silently understates a published number

The header's Linux line reads **"1,259 matching verdicts · 1 disclosed"**, and the guard proves
that line against the annotations (`check-roster-format.ps1` §2). The verdict sum is right
(582+204+302+54+18+86+13 = 1,259). The **disclosed** sum is derived from an incomplete input:
`mime`'s `+ 1` is the only term recorded, while `bytes` and `crypto/sha1` demonstrably disclose 6
and 1 more on Linux. The published Linux disclosure total is therefore **1 where the measurement
says 8** — and every guard passes, because a derivation cannot rescue an omitted term.

That is the design lesson D1 actually teaches: **the omission must become impossible to write, not
merely wrong.** It also relocates detection — today the defect surfaces after 4.3 hours of sweep as
an ambiguous `DISC`; it should surface in the seconds-long standalone guard as a parse error.

### 2.2 What R's 83.4% is, and is not

R's honest Linux figure is **146/175** — 144 PASS + the 2 DISC rows that validate underneath. It is
an honest and valuable number, and it is *Linux behavior measured against the **Windows** roster*.
It is **not** Linux's campaign percentage, because its denominator is a Windows artifact: it counts
`internal/syscall/windows/registry` (which cannot exist on Linux) and omits `log/syslog` (which can
only exist there). Under three-platform parity these two quantities need distinct names, and this
note uses:

- **roster-parity rate** — rows of the (Windows) roster that also validate on OS *X*. R's 146/175.
- **validation rate for OS *X*** — rows banked *at their X counts* over X's own implementable set.
  Today, for Linux, that is **7 / (unmeasured)**.

The second is the one three-platform parity is defined over, and §5 is how it becomes derivable.

---

## 3. The roster shape: three alternatives, priced

The decision is where per-OS facts live. All three shapes can be made correct; they differ in
migration cost, guard cost and what a visitor sees.

### S-A — extend the annotations (one table, per-OS segments)

Keep the Windows columns, keep the middle-dot `goos: N + D` segment, extend the *grammar* and push
the non-count per-OS facts (exclusions, floors, disclosure scope) to the artifacts that already own
those facts.

- **Migration:** ~6 character-level edits to existing rows (§4), one new ledger column, one new
  optional manifest field. Every existing row keeps parsing; the gate-bearing `$RosterRowPattern`
  is untouched except for the §8 sentinel.
- **Guard:** extends the five existing guard sections; each new number is derived like its
  siblings. No new instrument.
- **Readability:** the flagship table stays exactly what a visitor knows — a Windows record with
  per-row annotations. **Cost:** the *What it exercises* cell keeps accumulating machine data at
  its tail (`host-conditional (…): …` then `linux: N + D` then `darwin: N + D`); at three platforms
  a fully-annotated row's prose ends in three segments of arithmetic. This is real and it is the
  honest price of S-A.

### S-B — OS-keyed columns (`| Package | Tests | Disclosed | Linux | Darwin | What it exercises |`)

- **Migration:** touches **all 175 rows** and changes the rendered column count 4 → 6. The
  document's own machine-parsed comment warns that "reflowing, reordering, or adding columns breaks
  the sweep's roster parser"; `$RosterRowPattern` and §3's five-pipe render assertion both move.
  This is a whole-table migration of a gate-bearing artifact for a property that today applies to
  7 rows of 175.
- **Guard:** cleaner in steady state — numbers live in columns, prose in prose, and the arithmetic
  reads columns uniformly.
- **Readability:** best at a glance for comparison; worst for the 168 rows that would carry two
  permanently empty cells, and each new platform is another column on a table already too wide for
  a phone.

### S-C — per-OS roster files (`ValidatedTestPackages-linux.md`, `-darwin.md`)

- **Migration:** a new document per platform, a roster-path parameter through `_roster.ps1` and the
  sweep, and per-file header arithmetic.
- **Guard:** most expensive — three package sets must be cross-checked against each other, and
  three copies of each row's *What it exercises* prose (the campaign's single largest human asset)
  must be kept from diverging. That duplication is precisely the hazard CLAUDE.md's
  concurrent-lane rule names: *two lanes solving the same problem produce a silent duplication, not
  a conflict.*
- **Readability:** each platform's page is clean and complete — the genuine attraction.
- **Precedent against it:** the project already ran the two-artifact experiment. The corpus was two
  trees for six weeks and the doctrine that survived was **one tree, per-GOOS folders, a default
  for the empty value** (layout L3). The roster is the same problem one dimension smaller.

### Recommendation — **S-A**, explicitly as the L3 shape

One artifact, per-OS segments, `windows` as what an unset key means. It is the smallest change that
makes every per-OS fact expressible, it keeps the Windows record byte-stable (the 2026-08-22 ruling
demanded exactly that and its harness proved it three ways), and it keeps every number derived by
the guard that already derives its siblings. S-B's win is real but it is a whole-table migration
bought for a readability improvement; if it is ever wanted, it is a mechanical transform *from* a
fully-annotated S-A roster and can be reconsidered when annotated rows are the majority rather than
7 of 175 — recorded as **OQ-2**.

---

## 4. The annotation grammar, formalized

Today's grammar, as implemented (`$RosterOsPattern`):

```
annotation   := SEP , goos , ":" , count , [ "+" , disclosed ] , (?= SEP | "|" | EOL )
SEP          := U+00B7 MIDDLE DOT
goos         := "linux" | "darwin"          (* $RosterOsKeys; "windows" refused by name *)
count        := digits                      (* > 0, guarded *)
disclosed    := digits                      (* omitted means zero *)
```

Three properties are already right and this design keeps them unchanged: the **two anchors** (a
separator before the key; a separator, the row's pipe, or end-of-line after the number) so prose
cannot false-parse; **at most one segment per key**; and **`windows:` refused by name**, because
the columns are the Windows answer and a row must not hold two.

### 4.1 The disclosure term becomes non-omittable

**Rule (recommended): an OS annotation always carries its disclosure term. Zero is written `+ 0`.**

```
annotation   := SEP , goos , ":" , count , "+" , disclosed , (?= SEP | "|" | EOL )
```

Why this rather than the two weaker alternatives:

- **Detection moves to the cheap gate.** Absence becomes a *parse* failure the standalone guard
  reports in seconds, instead of a `DISC` line after a 4.3-hour sweep that reads like corpus
  breakage. D1 cost exactly that misreading.
- **It kills the analogy that caused the defect.** Today's convention says the term is "omitted
  when zero, mirroring the blank Disclosed column". A blank *column* is a visible absence in a
  rendered table; a missing *text term* is invisible. The column keeps its blank convention; the
  annotation stops borrowing it.
- **Migration is six character-level edits**: `bytes` → `linux: 86 + 6`, `crypto/sha1` →
  `linux: 13 + 1` (the two real fixes, already commissioned as R's rider), and `+ 0` appended to
  `crypto/rand`, `debug/buildinfo`, `go/internal/gcimporter`, `path/filepath`. `mime` is already
  legal.

The alternative — *the term is mandatory only when the row's Windows `Disclosed` column is
non-zero* — has lower text noise and, applied to today's table, fails on exactly the two defective
rows and no others. It is rejected as the primary rule because it cannot see a **Linux-only**
disclosure on a row that discloses nothing on Windows, which is precisely `runtime/debug`'s shape
(§6). Recorded as **OQ-1**.

### 4.2 Darwin joins as a key, not as a third term

The grammar is **already** `goos: count` pairs, `darwin` is already in `$RosterOsKeys`, and the
guard already fixtures it (`annotation: darwin is a valid key`). No grammar change is owed for the
third platform, and a positional third term would be actively wrong: it would encode the platform
in position (breaking both anchors and the one-segment-per-key rule) and it would make the header's
per-OS sums positional too. **Decision: pairs, unchanged. Darwin joins by data.**

One mechanical prerequisite is worth stating so it is not discovered later:
`Get-SweepTargetGoos` returns `windows` on macOS **by design** (`_paths.ps1` scopes its pin to
`$IsLinux`, because darwin's corpus does not build yet). A `darwin:` annotation is therefore inert
data until the darwin run layer lands ([`FINDING-darwin-run-layer.md`](FINDING-darwin-run-layer.md)).
That is honest, not a defect — but the header's Darwin line must then read as "0 rows" rather than
be absent, so the arithmetic exists before the data does (§9).

### 4.3 What the grammar deliberately does **not** absorb

Platform-*exclusive* facts ("this package has no eligible tests on this OS") do **not** become an
annotation status token such as `linux: excluded E1`. They are exclusion facts and the exclusion
ledger already owns exclusion facts; putting them in two grammars is how two records of one fact
drift apart. §5 scopes the ledger instead, and the sweep gains one call to the ledger reader that
`_roster.ps1` already exports.

---

## 5. Per-OS honest denominators and an OS-scoped exclusion ledger

### 5.1 The ledger gains a `Scope` column

```
| Package | Verdicts | Class | Scope | Mechanism | Rooting |
```

`Scope` is a comma-separated GOOS list or `all`, and it states *where the exclusion holds*. What
the current seven rows' evidence implies (each to be **confirmed by measurement on each OS**, in
the same way each was measured on Windows — the rejoin clause is binding and these are proposals,
not rulings):

| Package | Class | Implied scope | Evidence |
|:--|:--:|:--|:--|
| `internal/runtime/syscall` | E1 | windows, darwin | only `syscall_linux_test.go` exists |
| `internal/syscall/unix` | E1 | windows, darwin | `siginfo_linux_test.go`, `kernel_version_solaris_test.go` |
| `log/syslog` | E1 | windows | `syslog_test.go` is Unix-tagged — **testable on linux/darwin** |
| `net/internal/socktest` | E1 | all | declares no test entry points anywhere |
| `runtime/race` | E1 | all | no `-race` build exists in the converted corpus on any platform |
| `os/user` | E2 | windows | the broken oracle is host-specific; another OS must re-measure |
| `internal/unsafeheader` | E3 | all | structural: a managed slice is not the `{Data,Len,Cap}` triple |
| *(new)* `internal/syscall/windows/registry` | E1 | linux, darwin | *"build constraints exclude all Go files"* (D2) |

The last row is the one that closes D2: a package **banked on Windows** and excluded elsewhere. It
therefore appears in the roster *and* in the ledger — which today's guard forbids outright
("ledger: no excluded package is also a roster row"). That assertion is correct and merely
OS-blind; §9 makes it **OS-scoped**: a package may never be excluded and validated **on the same
OS**, which is the invariant that was actually meant.

### 5.2 What the sweep does with a scoped exclusion

A new sixth classification, `platform-excluded`: the row is **skipped, reported by name, and
counted in neither the pass nor the fail column** — the same honesty shape `unbanked-count` (CVAC)
already has. It converts D2 from a FAIL that needs a footnote into a line that states a fact.

### 5.3 The per-OS honest denominators

Each platform's honest line needs exactly **one measured input**, exactly as Windows does today:
its **naive testable count** — the packages whose Go 1.23.12 sources define a `Test` function
*under that GOOS's build constraints*. Windows' 215 is that number; it is stated in the header,
rooted on the board, and the guard consumes it as given while deriving everything else from it.
Linux's and Darwin's numbers do not exist yet and must be derived the same way the post-hop
frontier was: **twice, independently, under a parse-count control**, then rooted with a date.

Given one such number per OS, every other figure is derived:

```
implementable(os) = naive(os) - |ledger rows whose Scope includes os|
numerator(os)     = |roster rows carrying an expectation for os|
verdicts(os)      = sum of those rows' counts       (columns for windows, annotation otherwise)
disclosed(os)     = sum of those rows' disclosed counts
percentage(os)    = numerator(os) / implementable(os)
```

and the header states three lines with no blending anywhere:

```
Windows: 175 / 208 - 84.1%   18,979 matching verdicts - 87 disclosed
Linux:     7 / D_l -  x.x%    1,259 matching verdicts -  8 disclosed
Darwin:    0 / D_d -  0.0%        0 matching verdicts -  0 disclosed
```

The roster-parity rate (§2.2) is *not* part of this header. It is a campaign measurement about a
run, not a property of the table, and it belongs where R put it — on the board, with its pin.

---

## 6. Per-OS disclosure sets

### 6.1 The measured instance, in full

`runtime/debug` on Windows: **4 matched · 5 disclosed** over 9 eligible tests (proof page
`docs/validation/current/runtime.debug.md`), the 5 spanning three classes — `host-limit`
(`TestStack`), `runtime-capability` (three `WriteHeapDump` tests), `codegen-liveness`
(`TestFreeOSMemory`). On Linux Go runs **10**: `panic_test.go` is tagged for the Unix family and
Windows is excluded by Go's own source. The extra test, `TestPanicOnFault`, **kills the converted
host** with a fatal `System.AccessViolationException`, because the Linux CLR cannot recover a
hardware fault — there is no SEH equivalent. This is structurally per-OS: the same converted code,
the same runtime version, a different platform contract.

Two smaller facts ride along and must be settled by measurement rather than assumed. First,
`TestFreeOSMemory` is *already* disclosed on Windows under a **prefix** pin
(`"less than 16777216 released:"`), and R's Linux capture reads
`less than 16777216 released: 0 -> 1921024` — i.e. the existing pin should match on Linux too, so
the row's Linux delta is probably `TestPanicOnFault` alone. That single row's re-measure decides
whether `runtime/debug`'s Linux annotation is `4 + 6` or something else, and it is cheap. Second,
R's shard-2 "per-OS disclosure-capture defect" (`crypto/ed25519`) was **retracted by its own
re-read**: the disclosure machinery is per-OS identical on the evidence. This design must therefore
introduce **no per-OS capture behavior at all** — the only per-OS dimension is *which entries
apply*.

### 6.2 Manifest schema v2: scope an entry, never fork the file

`go2cs_test_disclosures.json` is hand-owned, committed, and read by `loadTestDisclosures`
(`testConversion.go`) into `map[string]testDisclosure`; 23 manifests exist. The change is one
optional field:

```
{ "name": "TestPanicOnFault", "class": "...", "signature": "...", "reason": "...",
  "goos": ["linux", "darwin"] }
```

- **Absent `goos` = every platform.** All 23 existing manifests keep working byte-for-byte; the
  migration cost is zero.
- **Loading filters by the run's target GOOS** in `loadTestDisclosures` — one place — so
  `matchTerminalStatuses` and the whole oracle are untouched, and the anti-laundering clauses
  (`platform-skip` being the sole key that admits Go=pass/C#=skip; `runtime-capability` pinning its
  rows AS FAILING) continue to be enforced exactly where they are now.
- **Validation is loud, per the file's existing doctrine** (*a broken disclosure must not widen the
  oracle*): an unknown GOOS value, an empty array, or a duplicate `name` within one GOOS is an
  error, never a silent no-op.
- **The proof page renders the scope.** A reader of the Windows page must not see an entry that
  cannot apply there, and a reader of a Linux page must see why an entry exists that Windows never
  had. This is the same page that gains an OS dimension at the anchor release (§10).

**Why not one manifest per platform:** `runtime/debug` would duplicate 5 of 6 entries, each
carrying multi-paragraph campaign prose whose exactness is the point, and the two copies would
diverge silently — the identical argument that rejects S-C in §3 and that CLAUDE.md's
concurrent-lane rule states in general.

### 6.3 The class question, and the mechanical problem behind it

`TestPanicOnFault` cannot simply be classified and forgotten, because **it produces no captured
output to pin**: the host dies. Every disclosure class in existence rests on a signature found in
the converted side's failure text. Three ways out, priced:

1. **Platform-gate it in the converted test host** — barred outright. `platform-skip`'s admission
   test is binding: the skip must be *the upstream test's own skip statement*, never a skip the
   harness injects.
2. **A per-test, OS-scoped exclusion** — the ledger's E-classes applied at test granularity: the
   test is named, its mechanism recorded, its verdict pinned as *not runnable on this OS*, and it
   is accounted like a disclosure but sourced from the run's **crash evidence** rather than from
   captured output. This is new vocabulary and needs its own owner ruling, because it is the first
   accounting class whose evidence is not a signature.
3. **Run the row test-per-process** on the affected OS so a fatal test costs one verdict rather
   than the package — a real harness change with a real cost, and it would turn the crash into a
   capturable non-zero exit.

**Recommendation: (2) as the design, with (3) named as the mechanism that would make (2)
signature-pinnable and therefore preferable if the host ever gains per-test isolation.** Whether
"the platform's CLR cannot recover a hardware fault" is a `host-limit` (a property of the
deployment shape) or a new per-OS sibling of `runtime-capability` is an owner call — **OQ-4**.

---

## 7. Per-OS floors

`$longTimeouts` is eight entries, all sized from Windows measurements, and it is a **floor, not an
override** (a larger `-TestTimeout` raises it; a smaller one loses). D5 is the first proven
mis-sizing across an OS boundary: `time`'s 40m was sized from **1,146 s** on laptop-class Windows;
the same laptop class under WSL2 measured **2,488 s** and returned a deadline-partial row with ~49
timer-family verdicts missing — which is the CLAUDE.md **alphabetical-tail** signature, not a
divergence, and would have been read as one by anyone without R's shard notes.

**Recommendation: OS-keyed floors, not host-class-keyed.**

```
$longTimeouts = @{
    'time' = @{ default = '40m'; linux = '90m' }
    ...
}
```

with `default` meaning "every OS without its own entry" (the L3 rule again), and the existing
precedence unchanged: effective budget = max(entry-for-this-OS, `-TestTimeout`).

Why not host-class keying: the repository has **no host-class primitive** — `_paths.ps1` exposes
`$IsWindowsHost`, `$ExeSuffix` and `$IsLinux`, and nothing that identifies an i7-5820K from a
Ryzen 6850U. Every attempt to introduce one would be a machine registry that goes stale exactly as
the budget table's i9 rows did. The budget doctrine already answers this: *a deadline is a safety
net against a hung run, never a performance assumption* — size to the slowest legitimate host of
that OS, and a fast box pays only how long a rare genuine hang takes to be declared.

**The confound must be stated in the entry, not hidden:** R's datum compares native Windows against
**WSL2** on the same laptop, so it does not separate "Linux" from "WSL2 filesystem behavior". That
is fine for a floor — both are legitimate hosts of the Linux corpus, and the floor covers the
slower — but the comment must say so, exactly as the existing entries carry their evidence. The
90m re-run's result is the number that lands, and until it posts the entry is unwritten.

---

## 8. Roster-row semantics for OS-divergent counts and non-Windows-first banks

Three cases, in increasing order of what they cost.

**(a) A row banked on Windows that also validates elsewhere at a different count.** Fully solved
today — annotate. `crypto/rand` 298/302 and `path/filepath` 61/54 are the working precedent, and
the sweep's `Get-RosterRowExpectation` puts the right expectation in force. **Nothing new is
owed** beyond §4.1's mandatory term.

**(b) A row banked on Windows that is structurally impossible on another OS.** `registry`, D2 —
solved by §5's ledger scope plus the `platform-excluded` classification. **Nothing about the row
itself changes.**

**(c) A row that can validate on another OS and NOT on Windows.** `log/syslog` is the clean case:
Go's own constraints give it no Windows tests at all, so under today's shape it can *never* become
a roster row, and its Linux verdicts have nowhere honest to live. This is the only case that needs
a schema change:

- The `Tests` column takes an **em-dash sentinel** (`—`), reusing the ledger's existing convention
  for "no baseline exists to count against", and `Disclosed` likewise.
- `$RosterRowPattern`'s count captures admit the sentinel; the parsed row gains
  `HasWindowsRecord = $false`, `Expected = $null`.
- A sentinel row **must** carry at least one OS annotation, and **must** have a ledger row whose
  Scope includes `windows`. Both directions guarded (§9).
- The Windows header sums skip sentinel rows by construction (they contribute no numbers), so the
  Windows record is unchanged to the digit — the property the 2026-08-22 ruling protects.
- `windows:` stays refused as a **count**. A sentinel row may carry no windows annotation either;
  its Windows status is the ledger's business. The invariant "a row never holds two Windows
  answers" stays literally true.

**What this design does *not* do: admit a merely-pending row.** A package validated on Linux and
simply not yet validated on Windows keeps waiting, exactly as `net/smtp`, `net/http/httptest`,
`net/http/httputil` and `net/rpc` waited before their Windows banks. Admitting them would make the
flagship table's core reading — *every row here is proven* — conditional on reading a sentinel, for
rows that are going to gain their Windows numbers anyway. Structural impossibility is a permanent
fact and earns the sentinel; "not yet" is a schedule. Recorded as **OQ-3**, since it is the shape
question the coordinator has been holding since those four rows.

---

## 9. Guard extensions — every new surface derived and guarded on day one

Mapped onto `check-roster-format.ps1`'s existing sections, so nothing new needs an instrument.

**§1 — parser contract (fixtures).**
- `linux: 86` (no term) **must not parse** once §4.1 lands; the throw names the row and the rule.
- `linux: 86 + 0` parses with `Disclosed = 0`.
- Sentinel row: `| [pkg](url) | — | — | … · linux: 12 + 0 · … |` parses with `HasWindowsRecord`
  false; a sentinel row with **no** OS annotation is refused by name.
- `windows: <count>` still refused; `darwin` still valid; duplicate keys still refused.

**§1b — classification rule.** Two new buckets exercised as pure functions: `platform-excluded`
(scoped-out row on this OS — neither pass nor fail) and the sentinel row's expectation resolution
(annotation answers; there is no columns fallback, and asking for one is an error rather than a
silent zero). The Windows rows stay first and stay the proof that the reachable Windows set is
unchanged.

**§1c — ledger parser.** `Scope` parsed; every value in `$RosterOsKeys + 'windows'` or `all`; every
class still one of E1/E2/E3; the disjointness assertion becomes **OS-scoped** (a package excluded
on OS *X* must not carry an expectation for OS *X*; it may be a roster row for any other OS).

**§2 — arithmetic, per OS.** The Windows block unchanged. For each key in `$RosterOsKeys`: the
row count, verdict sum and disclosed sum derived from the annotations; the implementable
denominator derived as `naive(os) − |ledger rows scoped to os|`; the percentage recomputed. The
**one measured input per OS** (`naive(os)`) is asserted to be *present and dated with a rooting
link* — the guard checks its existence and its use, never its value, exactly as it treats 215
today. A platform with no annotated rows must still render its line (Darwin's zero line, §4.2), so
the arithmetic exists before the data.

**§3 — render integrity.** The pipe-count assertions extend to the ledger's new column (6 → 7) and
to the sentinel rows (still 5 for the roster). This is the section that caught the `log` row's
phantom fifth column; it must grow with every column that is added.

**§4 (new) — configuration integrity.** Two checks that today have no home: every `$longTimeouts`
key is a package that is actually on the roster (a floor for a non-roster package is dead config),
and every per-OS floor key is a known GOOS. Cheap, pure text, and they close the class where a
rename silently disarms a floor.

**Converter side (Go, under the plain `go test ./...`).** The manifest's `goos` field is validated
where it is loaded, with the existing "loud, never silent" doctrine, and guarded by a test in the
`disclosedParentAggregation_test.go` family: an unknown GOOS errors; an absent field applies
everywhere; a scoped entry is invisible to another target's oracle.

**Failing-first, for every one of them.** CLAUDE.md's rule — *a gate that has never been made to
fail proves nothing* — and the 2026-08-22 harness lane's own precedent (two failing-first proofs)
apply: each new check lands with a deliberate regression, the exact-message proof, and a
byte-identical restore.

**Wiring.** The guard stays **standalone**, per the coordinator's 2026-08-22 note (hooking it into
the sweep preflight would add a new Windows failure mode). Recommendation: keep it standalone and
name it explicitly as a step in the banking ritual; revisit wiring at a quiet point — **OQ-6**.

---

## 10. The artifact namespace: proof pages and badges are single-OS today

Not commissioned, but discovered while pricing §6 and load-bearing for any dual-OS workflow:
`writeValidationProofPage` writes **flat** to `docs/validation/current/<dot-id>.md`, and
`proofPageProvenance.platform` is **CONTENT**, not provenance — "a Go-version or platform change is
a different claim and must rewrite the page". `emitValidationProofPage` fires on every run whose
comparison reaches `validated`, whatever the target GOOS. Therefore **a Linux `-test-action all`
run rewrites the Windows page** (and, through the totals line the badge emitter reads back, the
package README's Tests badge).

The 2026-08-22 ruling already decided the timing — *proof pages gain the OS column AT THE ANCHOR
RELEASE, so the pages move once* — and this note does not move them. What it adds is the shape to
move them into, and the interim rule:

- **Interim (now):** a non-Windows sweep is understood to dirty `docs/validation`; those files are
  **restored, never banked**, which puts them in the same handled class as the CRLF phantoms and
  the `-tests`-closure production files. It is worth one line in the sweep's own drift note.
- **At the release:** either per-OS page paths (`current/<goos>/<dot-id>.md`, with `windows`
  staying flat so 175 published URLs do not move) or one page per package carrying a per-OS
  section. The first keeps each page a single claim and each URL stable; the second keeps one page
  per package as the visitor expects. **OQ-5.**
- Either way the badge's totals-line read must name which OS's totals it took, or badges silently
  become last-sweep-wins.

---

## 11. Increments (landing order, each independently bankable)

| # | Increment | Depends on | Size |
|:--|:--|:--|:--|
| I1 | §4.1 mandatory disclosure term: 6 row edits + parser rule + guard fixtures (failing-first) | — | small; R's rider is its first half |
| I2 | §5 ledger `Scope` column, OS-scoped disjointness, `platform-excluded` sweep class | I1 | small |
| I3 | §5.3 per-OS denominators: Linux's naive count derived twice and rooted; header gains its per-OS lines; guard derives them | I2 + a measurement | medium (the measurement is the work) |
| I4 | §6 manifest schema v2 (`goos` scope) + loader validation + Go guards + page scope rendering | — (independent) | small-medium |
| I5 | §7 OS-keyed floors, `time` first, once the 90m re-run posts | — | small |
| I6 | §8(c) sentinel rows for structurally-Windows-impossible packages | I2, OQ-3 ruled | medium — touches the gate-bearing regex |
| I7 | Darwin joins by data | I3, the darwin run layer | none in code |
| I8 | §10 proof-page/badge OS dimension | anchor release, OQ-5 ruled | medium |

I1 alone converts D1 from a four-hour ambiguous failure into a seconds-long parse error and repairs
a published number. I1+I2 change what R's next Linux sweep *says* without changing what it
measures: `bytes` and `crypto/sha1` report PASS instead of DISC, and `registry` reports
`platform-excluded` instead of FAIL — so the ledger reads **146 PASS of 174 comparable rows**
rather than 144 PASS + 2 DISC + 1 FAIL out of 175, with three fewer lines that need a footnote to
be read correctly. Same measurement, honest presentation, no corpus change.

---

## 12. Non-goals

- **No blending, ever.** No averaged counts, no "best of" per row, no footnote that hides a
  difference. The 2026-08-22 ruling's first principle is untouched.
- **The Windows record does not move.** Columns stay Windows-authoritative for the 1.23.x era;
  every §9 guard extension keeps the Windows reachable set exactly what it is.
- **No new instrument.** Everything extends `_roster.ps1`, `check-roster-format.ps1`,
  `run-validated-sweep.ps1` and the manifest loader.
- **No per-OS capture behavior** in the disclosure oracle (§6.1, R's retraction).
- **No corpus, converter-emission or harness-timing change** is proposed here beyond the floor
  table's shape.

---

## 13. Open questions for coordinator ruling

- **OQ-1 — the disclosure term's mandatory rule.** (a) Always required, zero written `+ 0`
  (**recommended**, §4.1); (b) required only when the row's Windows `Disclosed` column is non-zero
  (lower text noise, blind to a Linux-only disclosure on a Windows-clean row).
- **OQ-2 — S-A vs S-B at scale.** Extend annotations now (**recommended**), and revisit OS-keyed
  columns when annotated rows are the majority rather than 7 of 175 — or rule S-B now and pay the
  whole-table migration once.
- **OQ-3 — may a row enter the roster on a non-Windows bank?** Recommended: **only** for structural
  impossibility on Windows (`log/syslog`), via §8(c)'s sentinel; a merely-pending row keeps waiting
  as the four Linux-proven rows did. The alternative admits pending rows and makes "every row here
  is proven" conditional on reading a sentinel.
- **OQ-4 — `TestPanicOnFault`'s accounting class.** A `host-limit` (deployment-shape property), a
  new per-OS sibling of `runtime-capability`, or the per-test OS-scoped exclusion §6.3(2) proposes?
  Recommended: the per-test exclusion, because the host dies and there is no signature to pin —
  which is a new kind of evidence and therefore an owner call.
- **OQ-5 — proof-page namespace at the anchor release.** Per-OS paths with `windows` flat
  (**recommended** — stable URLs, one claim per page) vs one page with per-OS sections.
- **OQ-6 — guard wiring.** Keep `check-roster-format.ps1` standalone and name it in the banking
  ritual (**recommended**, per the 2026-08-22 note) vs wiring it into the sweep preflight.
- **OQ-7 — who derives `naive(linux)`, and when.** The Linux honest denominator is a measurement,
  not a design decision; recommended to derive it twice independently under a parse-count control
  (the 2026-08-25 frontier precedent) as part of I3, before any Linux percentage is published.
- **OQ-8 — `runtime/debug`'s Linux arithmetic.** One cheap re-measure decides whether the Linux
  annotation is `4 + 6` (the existing `TestFreeOSMemory` prefix pin firing on Linux, plus
  `TestPanicOnFault`) or something else. Recommended: settle it before I4 encodes the scope, so the
  first scoped entry is written against a measurement rather than an inference.

---

## Appendix — the measured instances this design is built on

| Instance | Value | Where measured |
|:--|:--|:--|
| Linux full-roster ledger | 175 rows, 144 PASS · 2 DISC · 29 FAIL; honest 146/175 (83.4%); 4.3 h, 15,343 row-seconds | R, JOB-024 FINAL, pinned `59af260e0`, WSL2 Ubuntu-22.04, go1.23.12 / .NET 10.0.400 |
| Bare-annotation false red | `bytes` 86 matched with Windows' 6 disclosures vs expectation 0; `crypto/sha1` 13/1 | JOB-024 shard 1 |
| Published Linux disclosed total | header says 1; annotations omit 6 + 1 | roster header vs D1 |
| Platform-exclusive row | `internal/syscall/windows/registry`: "build constraints exclude all Go files" on linux | JOB-024 shard 3 |
| Inverse exclusions | `log/syslog` (`syslog_test.go`), `internal/runtime/syscall` (`syscall_linux_test.go`), `internal/syscall/unix` (`siginfo_linux_test.go`) | ledger + `GOROOT` file listing |
| Per-OS disclosure set | `runtime/debug`: 9 eligible on Windows (4 + 5 disclosed), 10 on Linux; `panic_test.go` tagged `aix\|darwin\|dragonfly\|freebsd\|linux\|netbsd\|openbsd`; `TestPanicOnFault` fatal `System.AccessViolationException` | proof page + `GOROOT` + JOB-024 FINAL |
| Per-OS floor | `time` 40m from 1,146 s (Windows, laptop-class, 169 tests); 2,488 s deadline-partial on Linux, ~49 timer verdicts absent; 90m re-run in flight | `$longTimeouts` history + JOB-024 FINAL |
| OS-divergent counts | 298/302, 61/54, 197/204, 583/582, 17+1 / 18+1 | the seven annotated rows |
| Linux-first precedent | `net/smtp` 19, `net/http/httptest` 55, `net/http/httputil` 53, `net/rpc` 15 — Linux-proven, Windows-walled, held | 2026-08-22 S3 ledger; 2026-08-25 fold |
| Guard state at this branch's base | 256 checks pass — 175 rows, 7 linux annotations, 7 excluded | `check-roster-format.ps1`, this lane |
| Disclosure manifests in the corpus | 23, `schemaVersion 1` | `src/core/**/go2cs_test_disclosures.json` |
| Proof-page namespace | flat `current/<dot-id>.md`; `platform` is CONTENT; emitted on every validated run | `validationProofPages.go` |
