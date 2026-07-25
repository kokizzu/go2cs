# PLAN — the whole-corpus rebank (Go 1.23.1, one deliberate regen)

> Produced 2026-07-25 from the rebank root-cause probe. **The feared "fresh regen is operationally
> broken" alarm was FALSE** — it was the measurement's own unseeded overlay clobbering 14 hand-owned
> files (see CLAUDE.md's Phase-3 mechanics, the SEED-FIRST rule). A correctly-seeded fresh corpus
> compiles 0-errors and validates all banked packages. This plan is executable whenever the
> coordinator schedules it; only Stage 0 gates it.

**Key facts (probe-measured):**
- Drift: ~335 production `.cs` + 5 `.csproj` across 112 of 302 packages, in ~24 classes — every
  class inventoried as NEUTRAL / IMPROVING / intended-BEHAVIORAL (the probe report carries the
  class → origin-commit → verdict table). 41 further files are CRLF-only phantoms (skip).
- Banked test sources are regenerated on every validation run — they are review artifacts, never
  validation inputs. A rebank therefore only changes a banked package's *dependencies'* code, and
  the serial re-validation sweep measures exactly that.
- The 5 csproj changes flip `AllowUnsafeBlocks` true→false (consequence of the managed-reinterpret
  model) — converter-consistent, compiles clean.

**Stages:**

0. **Decide S1** — the `Reinterpret<X, array<Y>>` fabrication class (~42 sites, live Windows paths:
   reparse points, registry values, net lookups, poll). Build the small behavioral probe/guard
   (fixed-array element address reinterpreted through the box) or record an explicit acceptance.
   *The one item the rebank must not cross undecided.*
1. **Capture the regen — SEEDED.** `cp -r src/go-src-converted <tmp>/core` FIRST, then
   `go2cs -stdlib -comments -go2cspath <tmp>`. **Hard gate:** `.cs.auto` count in the temp root
   must equal the committed tree's `[module: GoManualConversion]` count (14 as of 2026-07-25);
   0 means unseeded — abort. Every marked file's fresh `.cs` must be identical to the committed one.
2. **Overlay.** `*.cs` excluding `*.cs.auto`; `*.csproj` excluding `*.tests.csproj` with the
   `core\` → `go-src-converted\` rewrite, excepting `core\golib` and the exact
   `core\testing\testing.csproj`. Refresh the tracked `.cs.auto` review siblings (13 → 14;
   `syscall/exec_windows.cs.auto` is new).
3. **Compile gate.** Full `go-src-converted.slnx` build, 0 non-lock errors, skipped-dependents
   checked.
4. **Operational gate.** All banked packages via the `-tests` pipeline, SERIALLY (~45 min measured
   at 43). This regenerates each banked package's test artifacts in place — paired with the exact
   production tree they validated against.
5. **Bank in two reviewable commits:** (1) the production regen (content-changed files only — skip
   CRLF phantoms); (2) the banked test-artifact refresh (note: `strings.tests.csproj` legitimately
   gains a `runtime/internal/math` reference from B5). Then delete pipeline leftovers
   (git-ignored `.go` staging + manifests).

No CNR / behavioral-suite gate applies to the rebank itself (no shared machinery changes); it must
simply start from a master whose converter already passed those gates.
