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

0. ~~**Decide S1**~~ — **DONE 2026-07-25, and it was a REGRESSION, not a re-spelling.** The
   `Reinterpret<X, array<Y>>` class fabricates a managed reference out of the pointee's data: seven
   probed shapes, five of them a hard `AccessViolationException`. Crucially the *committed* tree was
   **correct** at 7 of those sites (a `slice<T>` over a `ReadOnlySpan<T>`) — the fresh regen lost that
   fusion, and the plan's "5 csproj `AllowUnsafeBlocks` true→false" line is its fingerprint, not
   benign converter-consistency. Confirmed live: converted `registry.GetStringValue` (the read behind
   `time.initLocalFromTZI` and `mime.initMimeWindows`) returns `Windows 10 Pro` committed and hard-faults
   fresh. **Fixed in the converter** (`6c31a59d2`, array-underlying targets keep their route; guard
   `PointerCastSliceReinterpret`), plus a pre-existing dropped-low-bound bug in the same fusion
   (`ce2d5a743`, fixing `os.Readlink` and `internal/abi.OutSlice`). CNR 491/491 byte-identical, suite
   491/491 PASS. Full record in
   [`FINDING-managed-box-uintptr-lifetime.md`](FINDING-managed-box-uintptr-lifetime.md) *S1 follow-up*.
   **Stage 0 clears the rebank — but only from a master that CONTAINS both commits**; regenerating from
   an earlier converter reintroduces the fault. Re-capture Stage 1 accordingly, and expect the
   `AllowUnsafeBlocks` flip to be absent from registry / internal/syscall/windows / os/user / net (it
   legitimately remains gone for `reflect`, whose loss had a different, benign cause).
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
