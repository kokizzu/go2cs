# PLAN — the rebank wave (Go 1.23.12 leveling, drafted 2026-08-31)

> **Instance campaign plan** (doc types: [`docs/Glossary.md`](Glossary.md)). Procedure authority
> stays with the runbooks; this plan holds the wave's inventory, sequencing, owners and gates.
> Every count below marked *(verify at wave head)* is re-measured before execution — carried
> numbers are hooks, not facts. The wave is the campaign's critical path: it gates runtime's
> semantic bill, reflect's IVT grant, and the drift families every sweep has been hand-restoring.

## Why one wave, not piecemeal

Each item below changes corpus emission or corpus-wide project files. Landed separately, each
would force its own full-gate battery and its own sweep-dirt reclassification; landed together,
ONE seeded multi-target reconvert levels everything, ONE classifier amendment covers the csproj
family, and ONE full-roster sweep proves the union. That is the same economics as r40.

## Inventory, in execution order

### Stage A — converter/gen arcs that must land BEFORE the regen
1. **Δ-rename coordinated fix** (the runtime last-five). Spec: variant-scoped `nameCollisions`
   renders `ΔPallocBits` in the internal variant while the external variant's fresh map and the
   go2cs-gen generated files (`PageCache.g.cs`, `AddrRange.g.cs`) compute names independently —
   TWO measured failed remedies prove reference-side fixes desync the generator (5→71 both
   times; `claude/g-runtime-zero` arc record, 2026-08-31). The fix is coordinated
   converter+generator or declaration-side. **Owner: fresh full-context lane (i9 candidate — the
   owner nudges it for wave execution). The arc OPENS with a two-seeded-emissions SIZING pass —
   the blast radius is unmeasured (both discards died at runtime's own build, never reaching a
   corpus reconvert), and the number reports to the coordinator BEFORE the cut so a
   generator-sized arc is discovered while re-scoping is cheap (G's review amendment,
   2026-08-31). Gate: runtime -tests build reaches ZERO; then the SEMANTIC BILL pipeline run
   prints — the wave's headline deliverable.**
2. **Init-hook relocation** (owner-sourced; G's scout is the spec, 2026-08-31 mailbox): the
   `initᴛᴛimportꓸ*` hooks move to package_info.cs's machinery area, csproj emission gains the
   explicit-first `<Compile>` item that pins module-initializer order (measured enabler; the
   naive move reproduces log/slog's nil-deref). May retire sweep-dirt class 2's `initᴛᴛtests`
   shape — measure the retirement, don't assume it. *(ANSWERED 2026-09-01, G's sizing census,
   mailbox `52607b382`: NO — different file, construct and trigger; three controls; class 2's
   fourth shape survives the wave and Stage C's classifier amendment is written expecting it.
   The census also sized the cut — 2,125 production hooks / 684 files / 314 packages. Blocker
   status CORRECTED by G's own retraction (mailbox `f04c507f9`): the bcache CS0111 claim was WRONG —
   bcache is FULLY hand-owned, its `package_info.cs` is never re-emitted, no duplicate can arise;
   only the runtime/metrics dead-name hook stands, riding the relocation commit. Same census: **14
   forced-init hooks are MISSING corpus-wide** from the hand-own fence — 5 fixable by the
   relocation, 8 inside the frozen hand-owned-by-consequence class (now FOUR members, bcache the
   fourth) fixable ONLY by the frozen-README option (a) below, 1 the dead-name blocker. Zero graph
   risk, W1-checked: every such import is already a ProjectReference. This moves option (a) from a
   README-freshness call to a corpus-correctness one.)*
3. **g-synthesis-ivt** (parked branch `3f2e02bc0`, validated 2026-08-30): the IVT-to-synthesis
   grant line joins every generated csproj. Merges as a Stage-A converter change; its corpus
   effect arrives with the Stage-B regen. *(Verify branch still merge-clean at wave head.)*

4. **Hand-own scope-routing fix** (found 2026-09-01 by A2 step 3's seeded three-target gate;
   PRE-EXISTING at master, A/B-proven byte-identical red — same file/line/column, both binaries,
   one seed): `platformHandOwnDestinations` routes a hand-own by its PRINCIPAL's emitter set and
   ignores the registration's GOOS SCOPE, so `runtime/linux/trace_impl.cs` (scope `goosLinux`)
   lands FLAT and windows compiles it beside the undisplaced generated body — CS0111. The corpus
   guard's principal-in-all-folders `continue` is the matching hole; nothing had run a
   three-target merge since the scope was introduced. Exactly 1 of 74 hand-owns moves.
   **STAGE-B BLOCKER — the regen IS a three-target merge.** Owner: G (ruled 2026-09-01; they hold
   both staging roots and the standing A/B). The rule: route by the targets whose emission
   actually DISPLACED a member the hand-own defines — evidence the merge already holds (the
   placeholder witness) — which subsumes the unscoped case; narrow the guard's `continue` to
   match, red captured FIRST per the registry-guard pattern. Discriminating gate: the merged
   staging build goes 1 error → 0 on windows with the file landing linux/-routed, byte-identical
   to the committed corpus.

### Stage B — the seeded multi-target regen (coordinator-executed)
One seeded reconvert per target (windows/linux/darwin), full ritual (seed corpus +
version.props + docs/validation; marker gate path-precise; never twice into one root), then ONE
classified overlay. Families expected, all previously measured *(verify extents at wave head —
some may have leveled silently; my 2026-08-31 curated emission saw six, not seven)*:
- len(fixedArray) folding (runtime rand/string/windows-mheap/windows-proc; the 13-file corpus
  extent from the tracker's item 7)
- NilSafeDelegateConversion (runtime metrics.cs)
- comment-column alignment (the Δ-rename alignment churn; select.cs and siblings)
- position-map line tables (funcLits drift, corpus-wide extent unmeasured — this regen IS the
  measurement)
- the IVT grant line in every csproj (from A3)
- the init-hook relocation (from A2)
- **ref-primary arc, linux/darwin residue** — Q35 applied the per-GOOS footprint I3 (`6a7688c88`) and
  I1 (`0571e71cb`) each measured single-target and never emitted: ten files, +22 −22, hunks at full
  context, nothing routed. What REMAINS for the wave is the other arcs' staleness in six of those
  files (22–52 lines each against the arc's 4–6) plus B's own four routed files
  (`log/syslog/{linux,darwin}/syslog.cs`, `net/{linux,darwin}/pipe.cs`), whose `defer`→`finally`
  hunks would not land at any context because those files carry the chan-direction arc as well.
- runtime production two-arcs-stale regen; go/doc/comment files; five-package test-info
  staleness; `lookup_windows.cs.auto` and the whole `.cs.auto` set re-measured per CleanupBacklog 18
- **six frozen READMEs**: decision INSIDE the wave — option (a) emit README/csproj/package_info
  for fully-hand-owned packages IF it provably does not clobber internal/concurrent's
  hand-maintained `<TypeAccessibility>` block, else option (b) the set-version.ps1 refresh.
  Scout first, half-day, any lane.

### Stage C — classifier and doctrine amendments (ride the same commit train)
- Sweep-dirt classifier: the csproj grant line becomes the ONE intended production-csproj
  exception, then the rule re-tightens.
- CLAUDE.md class-2/class-5 notes updated for whichever shapes Stage B retires (initᴛᴛtests,
  alignment churn) — remove retired classes, don't let them linger as phantom restores.
- UTF-16 stderr trap + NUL-byte tell (fleet-confirmed 2026-08-31) joins the instrument notes.

### Stage D — the union proof
- Full battery (converter suite, CNR, GolibTests, behavioral 4-phase, both slnx, GoTargetOS=linux
  stdlib) at the merged head.
- **The owed FULL-ROSTER sweep** (coordinator-backgrounded, solo) — the wave's banked-row proof.
  Host-exception ledger applies per host; net/http's C#-side rides its direct-run precedent if
  the i7 Go-oracle wall persists.
- runtime's semantic bill re-printed at the BANKED head (comparator: G's throwaway-branch
  measurement if one landed earlier, else first print here).
- Tracker + board batch: the wave's findings ledger entries land with the bank.

## Sequencing constraints
- A1 blocks the bill; A2/A3 are independent of A1 and of each other.
- Stage B cannot start until every Stage-A converter change is MERGED (one regen, one truth).
- The gcount decision (R's arc, in flight) lands BEFORE Stage B if it changes emission
  (hand-owning `gcount()` adds a marker + placeholder the regen must see) — R's httputil
  vacuous-pass finding means its filtered sweep rides Stage D regardless.
- Mid-wave, the mid-battery source freeze applies fleet-wide during every Stage-D leg.

## Open items feeding the wave but not gating it
- **crypto/tls third-host-state sweep encoding** (owner-forwarded chip, 2026-09-01): teach
  `run-validated-sweep.ps1`/`_roster.ps1` the BoGo host state the acceptance machinery does not
  encode — runner PRESENT but the managed shim provably unable to clear the BoGo child's fixed
  10-minute wall on this host class (the committed TestBogoSuite host-limit disclosure carries the
  measurements) — accepting the go-pass/C#-collapse ONLY when the shortfall is exactly the bogo
  sub-verdict set and nothing else (anti-waving-through preserved; follow `Test-HostConditionalDelta`).
  Should land BEFORE Stage D so the full-roster sweep on the coordinator host reads crypto/tls
  honestly instead of refusing on count.
- reflect descriptor pass (G, in flight) — banks on its own train when ready; if it merges before
  Stage B, its emission (if any) joins the regen truth.
- testing Option-1 implementation — post-wave, first increment the -tests-on-testing F15b guard.
- os seam row; unique disclosure classification; os/user bank decision (denominator grows) —
  bankable independently, sequenced around the wave at coordinator discretion.
