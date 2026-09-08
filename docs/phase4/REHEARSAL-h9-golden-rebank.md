# H9 PREP — the behavioral golden rebank, predicted and triaged before it runs (2026-09-07)

**Preparation, not the rebank.** No goldens are re-baselined here and nothing is banked. H9 runs after
the hop; this record exists so its diff is *predicted* rather than read, which is what H9's own text
asks for: *"Predict the diff's size before running the rebank; a diff that materially exceeds the
prediction is a finding, not a rebank."*

| | |
|---|---|
| Corpus | `bef7a6dbd` (`claude/g-hop-h1`, = master `f4ced674d` + H1.2 + H1.3) |
| Outgoing / incoming | Go **1.23.12** → **1.24.13** |
| Host | i7-5820K (6C/12T Haswell-E), Windows 11, `windows/amd64` |
| Measurement | the one-axis GOROOT isolation of [`CENSUS-go124-package-delta.md`](CENSUS-go124-package-delta.md) §8 |

## 1. The prediction, on record before the rebank

**EIGHT goldens move; all eight are mechanical; ZERO require a named judgement.**

| | predicted |
|:--|:--|
| goldens moved | **8**, named in §3 |
| changed line-pairs | **35** |
| per-file shape | `added == removed` on every file |
| distinct mechanisms | **1** |
| §4 class | mechanical, one attributed upstream cause |
| **T5 · UNATTRIBUTED** | **0** |

**Falsifiers.** A ninth golden moves · any hunk that is not the alias rename · any file where
`added != removed` · any golden whose cause is not the §4 mechanism below. **Any of these makes the
rebank a finding rather than a rebank**, which is exactly the line H9 draws.

⚠ **This prediction is measured, not estimated** — §8 of the census ran the isolation with the build
axis proven constant by byte-identical binaries. It is stated as a prediction anyway because the
rebank runs against a **different tree** (post-H4, post-H5, post-H6), and a measurement taken here
does not transfer to there by assumption. **If the rebank sees eight, the prediction held; if it sees
more, the intervening steps moved something and that is the finding.**

## 2. Which of §1.1's three channels are live for H9 — measured, not assumed

§1.1 names three ways emitted C# moves when the Go source did not, and asks that liveness be verified
per migration rather than assumed either way.

| channel | live for H9? | evidence |
|:--|:--|:--|
| **1 · release-tag expansion** | **live upstream, ZERO behavioral reach** | 1.23 → 1.24 is a *minor* bump and `releaseTagsForVersion` is minor-keyed, so the tag set does gain `go1.24` — but **0** behavioral `.go` files carry a `//go:build … go1.N` constraint, so no behavioral file selection can change |
| **2 · imported type aliases** | **LIVE — this is the whole of H9's drift** | §4 |
| **3 · upstream source** | **dead** | go2cs owns the behavioral Go sources; the migration does not touch them |

**So H9's drift is single-channel**, which is why one mechanism explains all eight files.

## 3. The eight, with their measured shape

| golden | +/− |
|:--|:--:|
| `FuncForPCName` | 2/2 |
| `FuncLiteralCallerNames` | 3/3 |
| `GoexitDefers` | 2/2 |
| `GoroutineWaitState` | 3/3 |
| `IterPullRendezvous` | 2/2 |
| `RuntimeCallerFrames` | 15/15 |
| `SetFinalizerBridge` | 6/6 |
| `SyscallKeystonePulls` | 2/2 |
| **total** | **35/35** |

⚠ **It was FIVE until it was measured on one axis.** Three of the eight were missing from the count
the fleet carried, and they are not new projects (added 2026-08-07, 2026-08-29, 2026-09-05 — two of
them older than members of the carried five). **A rebank sized at five would have left three goldens
unaccounted for.** See the census §8.1 for how the wrong number was reached.

⚠ **The shape is `added == removed`, not "2/2".** `2/2` was the smallest instance mistaken for the
rule; a file with more `runtime.` call sites simply renames more lines.

## 4. The mechanism, established end to end

**Not "the alias set changed" — the specific collision, and why it stops occurring.**

`importAliasOperations.go` mints a `Δ` rename when a C# `using` alias declared inside `namespace go`
would collide with a **child namespace** visible from the reference closure — `using runtime =
runtime_package;` collides with `go.runtime` and produces **CS0576 at every use**. The source comment
names the culprit exactly: *"runtime.csproj itself references runtime/internal/math|sys"*.

**Measured at both releases — `go list -deps runtime`, members under `runtime/`:**

```
go1.23.12   runtime/internal/math, runtime/internal/sys      count 2   -> go.runtime EXISTS -> Δruntime
go1.24.13   (none)                                           count 0   -> no collision   -> runtime
```

**The upstream cause is the Go 1.24 runtime-internals relocation** — `runtime/internal/{math,sys}` →
`internal/runtime/{math,sys}` — which the H3 census independently records as two of its fourteen
removals with successors at 8/8 and 2/2 file overlap. **The last `runtime/*` member leaves runtime's
closure, the child namespace disappears, the collision that forced the `Δ` disappears with it, and
every behavioral golden that qualifies a `runtime.` call re-emits under the plain alias.**

**The mechanism is confirmed by a transformation, not by inspection**: normalizing the `-` side of
every hunk (`Δruntime` → `runtime`) reproduces the `+` side **exactly** — 35 minus lines, 35 plus
lines, identical after normalization, **zero exceptions.**

## 5. §4 triage — and a gap in the triage table

Testing the eight against §4's classes **in order**:

| class | verdict |
|:--|:--|
| **T0 · known non-diff** | **No.** Real numstat (35/35), not a line-ending phantom |
| **T1 · upstream, attributed** | **Nearly.** The drift maps to a named upstream change — but T1's test reads *"the file maps to an upstream commit touching **its** Go source"*, and these files' own Go source is untouched |
| **T2 · test-closure re-emission** | **Its list names "an import alias" — and its disposition is RESTORE, which is WRONG here** |
| **T3 · born-stale** | No |
| **T4 · hand-own consequence** | No |
| **T5 · UNATTRIBUTED** | **No — and this is the load-bearing answer.** The cause is named, measured at both ends, and traced to a specific upstream relocation |

⚠ **T2 is the trap, and a mechanical triage would fall into it.** T2's shape list literally includes
*"an import alias"*, so a reader matching on the hunk's appearance lands there — and T2's disposition
is **Restore**, a *standing* restore. Restoring these eight would fight the new GOROOT on every
subsequent run, forever, because the alias is not a `-stdlib`-versus-`-tests` emission difference at
all. **T2 is about two emissions of the SAME sources differing; this is one emission mode reading
DIFFERENT sources.** The hunks look identical and the dispositions are opposite.

**Proposed, for the coordinator rather than applied here:** §4 gains a class for **§1.1 channel 2**
drift — *the golden's own Go source is unchanged and its emission moved because a DEPENDENCY moved* —
whose test is "the alias/namespace change traces to a package relocation in the upstream diff" and
whose disposition is **Bank, naming the relocation**. Today that case has to be argued into T1 by
reading "its Go source" loosely, and the honest reading of T1 as written excludes it.

## 6. Procedure notes for the run itself

⚠ **H9's procedure text carries a claim that is now false.** It reads: *"re-transpile everything
first (the golden-update utility copies on-disk `.cs`; it does **not** re-run the converter, so a copy
over stale output silently re-baselines it)"* — `docs/GoCorpusMigration.md:434`. **Both re-baseline
paths now re-transpile each project unconditionally immediately before the copy, and REFUSE by name
when that transpile fails, times out, or degrades** (landed 2026-09-04; `UpdateTestTargets/Program.cs`
carries the refusal set and the message *"a golden minted by a stale converter is the defect"*).

**The instruction survives; its stated reason does not.** Re-transpiling first is still right — you
want the emission examined before it becomes a record — but a reader following the parenthetical
would believe a hazard that is closed, and might also believe the utility is safe to point at a stale
tree, which the refusal now prevents. **Amendment proposed to the runbook's owner, not applied here.**

Two further notes for the run:

- **A whole-corpus re-baseline banks any `.cs`-vs-committed drift into the goldens silently**, so a
  **byte-identical CNR verdict is H9's precondition and runs FIRST**, never after.
- **`--only <Name>[,…]`** narrows the transpile-and-copy, which matters here: eight named projects
  make the refusal branch cheap to exercise rather than a ~25-minute control nobody runs.

## 7. Instrument notes

- **The build axis was proven constant, not assumed**: the converter built under each `GOROOT`
  produced **byte-identical binaries** (sha256 `a8cc7ae315b3eef4`, both stamped `go1.24.13`), and
  CNR's own unconditional rebuild reproduced that hash. Without that, "the GOROOT moved these files"
  would have been an inference across two binaries.
- **Zero NOT MEASURED** across 722 packages in the isolation arm — so no route-#4 degradation
  contaminated the reading. ⚠ It does **not** follow that a 1.23.12-built front end can read 1.24
  sources: both arms ran a converter *built by* 1.24.13, matched to its sources.
- **A glyph count is the wrong instrument for a rename** and produced 27 phantom "non-alias" lines
  before the transformation test replaced it — a renamed line's `+` side no longer carries the glyph
  it was renamed away from.

## 8. What this record does NOT establish

- **It is not the rebank**, and it is not a licence to bank the eight unexamined — H9 still classifies
  every moved golden at the tree it actually runs on.
- **It says nothing about the other three phases.** H9's gate is the full behavioral suite green
  across all four; this record predicts only the Target-phase diff.
- **It does not survey H4/H5/H6's own emission changes**, any of which can add goldens the prediction
  does not carry. That is precisely why §1 states the prediction as transferring by *measurement at
  the rebank*, not by assumption from here.
