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
- **2026-08-02:** **badge emitter + release linkage LANDED** — the follow-up arc above, items 1 and 2.
  Every converted stdlib README now carries a standalone validation badge; the release scripts freeze
  and retarget the proof it links; validated packages pack that proof as `VALIDATION.md`. Detail and
  the places reality corrected the plan are in the next section.
- **2026-08-08:** **the Docs badge joins it** (user ruling) — a second badge beside Tests on the same
  line, linking the official Go documentation for the sources each package was converted from, pinned
  to the version that produced them. It closes the round trip the READMEs were missing: the Tests
  badge proves the conversion behaves like the Go, and the Docs badge is the route to the Go it
  behaves like. Spec below; the vendored-module case and the import-path source are refinements 6–8.
- **2026-08-08:** **the two Source badges join them** (user ruling, r51c) — the Go sources and the
  converted C#, one click apart, for the exact version the reader holds. Docs reaches the *rendered
  documentation*; these reach the *sources themselves*, which is what a reader of a transpiler's
  output actually wants to compare. Landing them moved the release tag: the C# badge links
  `nuget-<version>`, so that tag now has to exist before the packages are packed. Spec below;
  refinements 10–14.

## The badges (Tests 2026-08-02, Docs 2026-08-08, Source pair 2026-08-08)

One badge-only line per package README, its own paragraph between the attribution blockquote and the
godoc body. No emoji, no trailing text link — **each badge IS its link**. It holds four badges, in
this order, separated by a single space so a narrow renderer wraps between them:

1. the **Tests** badge — this package's validation state, linking its proof;
2. the **Docs** badge — the official Go documentation for the sources it was converted from;
3. the **Source·Go** badge — those Go sources themselves, in the Go repository;
4. the **Source·C#** badge — the converted C# beside this README, in the go2cs repository.

**The order is the decision, not an accident.** Tests and Docs come first because they state what the
package *is* — validated, and what it mirrors. The Source pair comes **last** because it answers the
reader's *second* question: having been told this C# mirrors that Go, go read both. The pair is
deliberately adjacent and deliberately in convert-**from** → convert-**to** order, so the line reads
in the direction the conversion runs.

Tests' label was reduced from `Go_tests` to `Tests` by user ruling 2026-08-03: the `?logo=go` gopher
already says Go, so the word in the label was redundant.

| Badge | State | Message | Links |
|:--|:--|:--|:--|
| Tests | validated | `Tests-<m>%2F<t>_validated-brightgreen` | `go2cs.net/validation/<version>/<dot-id>.html` |
| Tests | has tests, not yet validated | `Tests-not_yet_validated-orange` | `go2cs.net/ValidatedTestPackages.html` |
| Tests | no tests | `Tests-none_to_validate-lightgrey` | `go2cs.net/ValidatedTestPackages.html` |
| Docs | standard package (`internal/…` included) | `Docs-@<goversion>-00ADD8` | `pkg.go.dev/<import-path>@go<goversion>` |
| Docs | GOROOT-vendored (`vendor/golang.org/x/…`) | `Docs-@<pin>-00ADD8` | `pkg.go.dev/<module>@<pin>/<subpath>` |
| Source·Go | standard package | `Source-Go_@<goversion>-00ADD8` | `github.com/golang/go/tree/go<goversion>/src/<import-path>` |
| Source·Go | GOROOT-vendored | `Source-Go_@<pin>-00ADD8` | `github.com/golang/go/tree/go<goversion>/src/vendor/<module>/<subpath>` |
| Source·C# | every package | `Source-C%23_@<version>-512BD4` | `github.com/ritchiecarroll/go2cs/tree/nuget-<version>/src/core/<pkg-dir>` |

`<m>` is matched and `<t>` is matched + disclosed, both read off the package's living proof page, so
the denominator counts every test the suite ran (io: `59%2F61`). `<version>` is
`<GoStdLibVersion>.<GoBuildNumber>` from `src/version.props`; `<dot-id>` is the import path with `/`
replaced by `.`, the same flat name the proof pages already use.

`<goversion>` is the toolchain's own `go env GOVERSION` without the `go` prefix — the same value the
README attribution's `> Go version: 1.23.1` line carries, never a literal. `<version>` is the
published four-part `<GoStdLibVersion>.<GoBuildNumber>`, and `<pkg-dir>` is the package's directory
beneath `src/core`. `00ADD8` is the Go project's own blue, so the three Go-facing badges read as the
Go they point at rather than as go2cs status lights; `512BD4` is .NET's purple, and Source·C# is the
one badge on the line that is not Go-blue — which is exactly the distinction it carries. The three Go
badges use `?logo=go`, Source·C# uses `?logo=dotnet`. The rendered line, for `bufio`:

```markdown
[![Tests](https://img.shields.io/badge/Tests-80%2F81_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.4/bufio.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/bufio@go1.23.1) [![Source](https://img.shields.io/badge/Source-Go_@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/bufio) [![Source](https://img.shields.io/badge/Source-C%23_@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/bufio)
```

The Tests badge's three states **partition the corpus**, which is what makes it auditable rather than
decorative. Census at the landing regen (`src/version.props` = 1.23.1.2):

- **71 green** — exactly the roster of [`ValidatedTestPackages.md`](../ValidatedTestPackages.md),
  compared row for row, with every badge's counts equal to its proof page's totals line.
- **144 orange / 87 grey** across all 302 converted packages, reproducing the roster's own
  denominator (215 testable) from an independent predicate: the GOROOT package's `_test.go` files are
  PARSED and searched for a top-level `func Test…` (Go's own naming rule). Comments and string
  literals mentioning `func Test` are everywhere in the standard library's test sources, so a
  lexical scan would over-count.
- **On disk that is 71 / 143 / 86 = 300 badged READMEs**, the count at that regen. `testing` and
  `unsafe` are hand-owned and carry no converter-*emitted* README, so they were absent from that
  figure; their badge lines are maintained by hand (refinement 13). The corpus is at **305 badged
  READMEs of 308** as of the Source-pair regen — the other three carry no badge line at all: the
  root attribution README, `golib`'s own, and a verbatim Go `testdata` README.

### Where the badge implementation refines the spec

1. **Green requires BOTH signals.** The committed `<dot-id>.tests.csproj` beside the package says the
   pipeline ran and its results were banked; the proof page supplies the counts. A badge is green only
   when both agree, so it can never claim a number no committed evidence backs. A disagreement falls
   through to the honest has-tests classification and shows up as a census miscount, which is a loud
   failure rather than a wrong badge.
2. **No repository context ⇒ no badge line at all.** `version.props` and `docs/validation/current/`
   are located by the same upward walk the pipeline uses for `$(go2csPath)`. A conversion that finds
   neither (a bare temp `-go2cspath` root, a deployed GOPATH runtime root) emits the README exactly as
   it did before badges existed rather than a half-composed URL. The consequence is a SEED RITUAL: a
   reconvert that must reproduce the committed READMEs byte-identically has to seed `version.props`
   and `docs/validation` alongside `src/core`, and mirror the `src/` layout so `docs/` lands as the
   root's sibling. Recorded in CLAUDE.md's corpus-mechanics step 1a; proven by a two-root A/B whose
   entire `core` trees compared byte-identical.
3. **`internal/godebug` is badged BY HAND.** Its single Go file is fully hand-owned, so
   `unmarkedFileCount == 0` makes the conversion driver `continue` before `writeProjectFile` — its
   `.csproj`, `package_info.cs` and README have always been hand-owned by consequence. Its badge
   (orange) and pack block were applied by hand, byte-identical to what the converter emits for every
   other package. It is the one package where the badge is not converter-maintained.
4. **`VALIDATION.md` rides in the nupkg via a marker, not a template verb.** `csproj-template.xml`
   carries `>>MARKER:VALIDATION_PACK<<` on a line of its own, substituted after the printf verbs (like
   the friend-assembly grant, and for the same reason: a user-supplied `-csproj` template cannot know
   about the slot). A stdlib conversion expands it into an `Exists`-guarded `<None …
   PackagePath="VALIDATION.md" />` sourced from `$(go2csPath)..\docs\validation\$(GoStdLibVersion).$(GoBuildNumber)\<dot-id>.md`;
   every other conversion collapses the whole line to the blank line the template always had there, so
   behavioral and `-recurse` csproj output stays byte-identical (CNR is the proof). The block is
   emitted for EVERY stdlib package, not only validated ones — the `Exists` guard means a package that
   validates later starts shipping its sheet with no `.csproj` change.
5. **The release script's "re-emission is a no-op" check is computed, not converted.** After the
   snapshot and retarget, `push-nuget.ps1` re-derives each green badge from the frozen proof page and
   compares it to the README byte for byte. That is the same equality a converter re-emission would
   assert, without a Go toolchain or a 4-minute reconvert in the middle of a release.
6. **The Docs badge pins what it links, which makes GOROOT-vendored packages a separate case**
   (added 2026-08-08). An ordinary package — `internal/…` included, pkg.go.dev serves those like any
   other std package — links `pkg.go.dev/<import-path>@go<goversion>`, so the reader lands on the
   documentation for the exact Go release the C# beside it was converted from. A GOROOT-vendored
   package is not a Go release artifact at all: it is a snapshot of a third-party module, and its
   `vendor/`-prefixed path exists only inside GOROOT (`pkg.go.dev/vendor/golang.org/…` is not a page).
   Those resolve through **GOROOT's own `src/vendor/modules.txt`** — the file `go mod vendor` writes
   and the only place the real version survives, since the vendored tree carries no `go.mod` — to
   `pkg.go.dev/<module>@<pin>/<subpath>`. The badge's MESSAGE states that pin rather than the Go
   version, because the badge names the documentation it actually links: `@1.23.1` over a link to
   `x/crypto@v0.23.1-0.20240603234054-0b431c7de36a` would name documentation that does not exist. A
   pin that cannot be resolved emits **no Docs badge**, on the same reasoning as the Tests badge's
   fallback — an unpinned docs link resolves to whatever is current rather than to the sources this
   package holds, which is the one thing the badge promises.
7. **The import path comes from the source DIRECTORY, not from the loader's `PkgPath`.** `PkgPath` is
   not stable across load configurations for exactly the two shapes that matter here: the same
   vendored package reports `golang.org/x/crypto/chacha20` under one configuration and
   `vendor/golang.org/x/crypto/chacha20` under another, and `internal/abi` can come back as
   `std/internal/abi`. `stdLibImportPath` strips the `GOROOT/src` prefix with the same
   case-insensitive `pathReplace` `getProjectName` uses, so the Docs badge's import path and the
   project's dotted name are guaranteed by construction to name the same package.
8. **Pairing the two badges on one line is safe for `push-nuget.ps1` because of a tightening made for
   an unrelated reason.** Its retarget pattern's segment class `[^/\s)]+` excludes whitespace (added
   when a prose link in `testing`'s hand-owned README was eaten by a looser `[^/]+`), so the space
   between the badges terminates the segment and a retarget cannot run past the proof link into the
   `pkg.go.dev` link beside it. The verification pass is anchored on `badge/Tests-`, which a
   `badge/Docs-` badge cannot satisfy. Both were checked against a green and a vendored README before
   the badge landed; the note lives beside the pattern in the script.
9. **An interim snapshot `docs/validation/1.23.1.2/` was created by hand** (a copy of `current/`, with
   a README saying so) so the badge links resolve from the day they landed. Every later versioned
   directory is written by `push-nuget.ps1` at publication.
10. **Source·Go keeps its badge where Docs must drop it** (added 2026-08-08). Both resolve a vendored
    package's pin through the same `modules.txt` reader, but they need it for different things: the
    Docs link's *address* is composed from the pin (`pkg.go.dev/<module>@<pin>/…`), so an
    unresolvable one has nowhere honest to point and the badge goes silent; the Source·Go link is the
    GOROOT tree at `go<goversion>` and is fully pinned by the Go release alone, so the module version
    only sharpens the badge's TEXT. An unresolvable pin therefore degrades Source·Go's text to the Go
    release and keeps the link. A badge whose target is perfectly good should not disappear because
    its label could have been more precise.
11. **Source·C# links a TAG, and that moved the release flow.** A `master` link would drift away from
    the package the reader is holding with the very next commit, so the badge pins
    `tree/nuget-<version>/`. That made the tag a *pre-pack* artifact: `push-nuget.ps1` now mints it
    (signed, idempotent by check-then-skip) at snapshot time, before anything is built, instead of
    the flow suggesting `git tag` as a post-push Phase-3 instruction. Tagging afterwards meant every
    README baked into every package linked a tag that did not exist yet. It sits *before* the
    write-once proof snapshot so a signing failure costs nothing, and it is gated on the build-number
    bump so a pack-only inspection run cannot mint a release tag. The tag names the tree the release
    was built FROM — HEAD there differs from the release commit only by `version.props`, the snapshot
    and the retargeted README links, and no converted C# moves between them.
12. **The release retarget had to grow a second half.** Source·C# is version-pinned TWICE (its message
    and its tag), so a release that moved only the Tests badge's proof link would publish READMEs
    pointing at the *previous* release's C#. `push-nuget.ps1` retargets both, in its **own block
    outside the proof-snapshot branch** — this badge is on every package README, validated or not,
    and gating it on `docs/validation/current/` existing would silently ship stale source links on
    the one run where the proof pages had gone missing. A verification pass then asserts both pins
    name the version being published, the same consistency-by-construction the green badges get.
13. **The hand-owned-README class has EIGHT members, not the one refinement 3 recorded.** A package
    whose README the converter never re-emits gets its badge line by hand, and the corpus regen found
    that this is true of `internal/godebug`, `internal/concurrent` and `internal/weak` (zero unmarked
    files, so the driver `continue`s before `writeProjectFile`), `unsafe` and `testing` (skip-listed,
    never converted), and `crypto/x509/internal/macos`, `internal/runtime/syscall` and
    `vendor/golang.org/x/net/route` (nothing eligible emits on this platform). All eight ship a
    `.csproj` and therefore a NuGet package, so all eight carry the badges.
14. **Those hand edits were derived, then proved, not typed.** Each package's two Source badges are
    composed from data already in its own README — the attribution line's Go version and the Docs
    badge's already-escaped pin, which came from the same `modules.txt` resolution — plus the
    package's directory. The derivation was run as a CONTROL against the 297 converter-emitted
    READMEs first and reproduced all 297 byte for byte before being applied to the 8; re-run
    afterwards it reproduces all 305. "Byte-identical to what the converter emits" is a claim worth
    making only when something checked it.

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
