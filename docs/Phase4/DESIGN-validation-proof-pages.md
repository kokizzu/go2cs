# DESIGN — per-package validation proof pages (`docs/validation/`)

> Recovered 2026-08-02. This design was worked out in a session that died before writing it to
> disk; the conversation survived and is restored here as the durable record. One correction
> against reality: the machine-readable differential is `src/core/<pkg>/go2cs_test_comparison.json`
> (fields: `package, status, go, csharp, matched, skipped, disclosed, excluded, errors`), with
> per-test event detail in `go2cs_test_results.json` — not a `go2cs_test_comparison/results.json`
> subdirectory as the original discussion assumed. Both are gitignored pipeline artifacts today;
> this design turns the first one into the product.

## The idea

Every `-test-action compare` run already produces the complete per-test differential — every test
name, Go's verdict, C#'s verdict, and each disclosed divergence. Today that proof is thrown away
as pipeline scratch. A small deterministic renderer turns it into a committed, human-readable
**proof sheet per validated package**, published through the existing Jekyll site so every
validated package's claim is one click from its evidence:

```
docs/validation/
  current/io.md            <- living proof, updated by each banking commit
  current/path.filepath.md    (dot-form names mirror the NuGet ids, keep Jekyll paths flat)
  1.23.1.3/io.md           <- frozen snapshot, created at publication, never rewritten
  index.md                 <- generated roster, links both
```

- `https://go2cs.net/validation/current/io.html` — living proof (banking updates it).
- `https://go2cs.net/validation/1.23.1.3/io.html` — frozen at publication; the README badge
  target. A versioned directory is written once and never touched, so the link is immutable by
  convention: the page shown for `go.io 1.23.1.3` is forever the proof as of that binary.

## The proof sheet

- **Header (provenance):** package, verdict totals (`59 matched · 2 disclosed`), validation date,
  converter commit, Go version (1.23.1), platform (`windows/amd64`) — what makes it proof rather
  than claim.
- **The table:** every test, Go verdict vs C# verdict, side by side, sorted. Disclosed rows carry
  their pinned reason inline, read from the package's committed `go2cs_test_disclosures.json`
  ("exact allocation-count assert; the CLR counts bytes where Go counts mallocs — see the
  disclosure manifest").
- **Deliberately omits per-test wall-times** — that is what keeps the committed form byte-stable
  across re-validations.

**Content-stability rule (load-bearing):** the renderer must NOT churn `current/` on routine
sweeps. It compares the content-stable portion (totals + table) of an existing page before
writing; if only provenance (date / converter commit) would change, it leaves the file alone.
Provenance therefore updates only when the verdict content itself changes — i.e., at banking.
This keeps the 71 proof files out of every sweep's drift report by construction, rather than by
membership in the restore-unless-rebanked class.

## Publication linkage (the follow-up arc, deliberately separate)

1. **README badges:** the converter's per-package README emitter gains a validation badge —
   green + tag-pinned proof link for validated packages, honest orange for unvalidated ones. The
   badge links the **versioned** go2cs.net URL composed from `version.props`; the plain-text line
   can additionally link the living `ValidatedTestPackages.md` row. Requires a corpus README
   regen — its own gated arc.
2. **`release-nuget` gains two scripted steps:** copy `current/` → `<version>/` and tag the
   publication commit (`nuget-<version>`). The in-nupkg copy rides along at pack time from the
   versioned snapshot, so anyone who extracts the package holds the audit sheet offline.
3. **`ValidatedTestPackages.md` rows** link their `current/` proof page — the table a visitor
   already lands on becomes the proof index. ⚠ The sweep's roster parser regex anchors the first
   three columns of each row; the link must be appended INSIDE the existing "What it exercises"
   cell, never as a new column.

## Automation — every piece already runs

1. Compare oracle emits `go2cs_test_comparison.json` ✅ (exists)
2. Renderer → proof page (small, deterministic; hooked at the end of a successful
   `compare`/`all` action, locating the tree's `docs/` by the same upward walk the pipeline
   already uses for `$(go2csPath)`)
3. Banking commits it ✅ (existing §4.6 flow, one more file)
4. README emitter writes badge + tag-pinned link (follow-up arc)
5. `release-nuget` snapshots + tags (follow-up arc)

Future Go-version campaigns then produce trust automatically: validated packages ship green
badges with clickable proof, unvalidated ones ship the honest orange, and
`go2cs.net/validation/` becomes the most convincing page the project has — 71 green proof
sheets, each one `go test` agreeing with the C#, test by test.

## Status

- **2026-08-02:** design recovered and recorded.
- **2026-08-02:** **renderer LANDED** — `src/go2cs/validationProofPages.go`, hooked at the end of a
  successful `compare` (and therefore `all`) in `compareGoAndConvertedTests`, gated on
  `status == "validated"`. Unit-tested against a committed fixture differential
  (`src/go2cs/testdata/validationproof/`), determinism-tested, and stability-tested. First live
  pages: `current/cmp.md` and `current/io.md`, at their banked counts (4 / 59 + 2 disclosed).
  Badge emitter + release-flow snapshot/tag: still queued follow-up, needs the corpus README regen
  and a `release-nuget` change.

### Where the implementation refines the spec above

Recorded rather than silently diverged from:

1. **The stability compare is "whole page minus the provenance LINE"**, not "everything from the
   totals line down". Provenance is one italic line (`*Validated <date> · converter <sha>*`) and the
   writer strips exactly that line from both the rendered text and the file on disk before
   comparing. Strictly stronger than the line-position rule: a change to the title, the intro, or
   the Go version also rewrites, while the date/commit alone never does.
2. **Go version and platform are CONTENT, not provenance**, and therefore live *below* the
   provenance line, on the totals line. A Go-version bump is a different claim and must rewrite the
   page even if every verdict repeats.
3. **Disclosed tests get their own section, not an inline reason column.** The verdict table stays
   the three columns the roster reader expects (`Test | go test | go2cs`); a disclosed row's C# cell
   reads `fail ([disclosed](#disclosed-divergences))` and the section below carries class + pinned
   reason from the package's committed `go2cs_test_disclosures.json`.
4. **The disclosed set is derived from disagreeing verdicts**, not parsed back out of the
   comparison's formatted `disclosed` strings. On a validated comparison every non-agreeing pair is
   disclosed by construction (anything else is a mismatch and never validates), and deriving it this
   way also catches a disclosed *ancestor* rolled up from disclosed subtests — which carries no
   manifest entry of its own and renders as class `aggregate`.
5. **An "Excluded declarations" section was added** (not in the original spec). Without it the page
   overclaims: `Benchmark`/`Fuzz`/`Example` declarations and capability-blocked tests are filtered
   from *both* sides of the oracle, and a proof sheet has to say what it is not claiming.
6. **`index.md` regenerates on every validated run**, not only when a page changed — it is
   stability-gated too, so an unchanged roster writes nothing, and a missing or stale index heals
   itself.
7. **Pages are written CRLF**, matching the docs tree on disk and the converter's other generated
   text (`README.md`); the stability compare additionally ignores CRs, so `core.autocrlf` can never
   turn a proof page into permanent churn.
