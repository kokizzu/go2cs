# INVENTORY — intended corpus drift that is NOT yet banked

*Lane G, 2026-08-23. Docs-only; no corpus file is touched by this document.*

**What this is for.** Several converter arcs landed without a corpus regen. Their emission changes
are real and intended, but the committed `src/core` still holds the *previous* output — so the next
lane that runs a three-target regen sees a diff it did not cause and cannot easily attribute. Wall
#3 hit exactly that, restored the lot rather than smuggling it into an unrelated commit, and named
the hazard: **the next regen lane inherits them blind.** This is the worksheet that removes the
blindness. It becomes the leveling lane's checklist when the merge train drains.

**How to read a row.** *Evidence* is what can be checked against the committed tree today, without
a regen. *Disposition* is what the leveling lane should do. **Restore** means "a regen will rewrite
this and it is NOT yours to bank" — that was wall #3's discipline, correct for a lane whose subject
is something else. **Bank** means the leveling lane should keep the regen's version.

---

## Correction to the figure this inventory was commissioned from

My wall-#3 board entry and mailbox signal said "**~24** `linux/package_info.cs` files gaining a
position-map block". **That number is a truncation artifact of my own listing** — I read the
regen's delta through `git diff --numstat … | head -25`, so 24 was where my terminal stopped, not
where the family ended. The static census below puts the population at **56**. The regen's delta
is still the authoritative statement of what a regen *does*; my summary of it was not.

---

## Family 1 — per-GOOS `package_info.cs` missing `<GoSourcePositionMaps>`

**Population, censused on the committed tree:**

| GOOS folder | per-GOOS `package_info.cs` | carrying a position-map block |
|---|---:|---:|
| `windows` | 30 | **30** |
| `linux` | 28 | **1** (`syscall/linux`, from the Linux exec arc) |
| `darwin` | 29 | **0** |

**56 files** (27 linux + 29 darwin) lack a block their windows twins all carry. The wall-#3 regen
was observed adding it to the linux ones; darwin is inferred from the same asymmetry and is the
row most worth re-deriving rather than trusting.

* **Originating arc:** the position-map emission (`GoPositionMap` per converted file). The windows
  flavor was regenerated after it landed; the linux and darwin flavors predate it.
* **Evidence:** `git grep -l GoSourcePositionMaps -- 'src/core/**/<goos>/package_info.cs'`.
* **Why it matters beyond tidiness:** position maps are what let `runtime.Caller` and the
  tracebacks over it name the GO file and line a frame came from. A package whose per-GOOS
  metadata lacks them reports its emitted C# position instead — on linux and darwin only.
* **Disposition: BANK** at the leveling regen. This is pure catch-up; nothing decides it.

## Family 2 — `runtime/windows/package_info.cs` implicit-conv records a regen REMOVES

Three records that a current regen deletes:

```
[assembly: GoImplicitConv<nameOff, Δhex>(Inverted = true, ValueType = "int32")]
[assembly: GoImplicitConv<textOff, Δhex>(Inverted = true, ValueType = "int32")]
[assembly: GoImplicitConv<typeOff, Δhex>(Inverted = true, ValueType = "int32")]
```

* **Originating arc:** an implicit-conv record-emission narrowing (not mine; landed without a regen).
* **Evidence:** the three lines are present in the committed windows file; the wall-#3 regen's diff
  was `0 +/ 3 −` on it.
* **⚠ Do not assume symmetry.** The `linux` and `darwin` twins each carry **4** `Δhex>` lines, and
  they were **not** part of that run's measured delta. Whether the same three retire there, or a
  different set does, must be **re-derived per target** at the leveling regen — this is precisely
  the mistake wall #3 made in the other direction (concluding an artifact was shared after
  comparing two of three flavors, when the third was the one that differed).
* **Disposition: BANK the removal, per target, after re-deriving each target's own set.**

## Family 3 — the pointer-comparison spelling `Δp.Value != v.Value` → `Δp != v`

* **Sites (4), censused:** `runtime/linux/mem_linux.cs` lines 87, 103, 191; `runtime/darwin/mem_darwin.cs` line 75.
* **Originating arc:** a converter change to how `unsafe.Pointer` comparisons are emitted.
* **Evidence:** the old spelling is present at those lines today.
* **Census note worth carrying:** a corpus-wide `git grep` for this shape returned **only unrelated
  test sources and golib's own `uintptr` operator** and missed all four sites; a plain `grep` on the
  file found them immediately. Census this family with **both** tools, or it reads as already-clean.
* **Disposition: BANK.** Behavior is identical; the new spelling is the current emission.

## Family 4 — `.cs.auto` review siblings the regen refreshes

**23** `*.cs.auto` files are tracked. The wall-#3 regen refreshed **five**: `runtime/mfinal.cs.auto`,
`sync/atomic/type.cs.auto`, `time/tick.cs.auto`, `crypto/subtle/xor_generic.cs.auto`,
`internal/weak/pointer.cs.auto`. One more appeared as untracked and was removed rather than added:
`internal/syscall/unix/linux/siginfo_linux.cs.auto` — a **new** hand-own's first review sibling,
which a leveling regen should ADD rather than delete.

* **Standing cause, not an arc:** the `-stdlib` overlay rule excludes `*.cs.auto` in order to protect
  the hand-owned `.cs` beside it, so these freeze on their own schedule. Recorded as CleanupBacklog
  item 18 and unchanged by this inventory.
* **Disposition: BANK all refreshed siblings together, in their own commit**, and add the new
  `siginfo_linux.cs.auto`. This is the `.cs.auto` half of the leveling lane, accepted as a
  post-release item.

## Family 5 — F1's regen: **nothing owed**

The F1 fix (`e44bed59f`) reported **zero corpus diff** with CNR clean, so it contributes no
unbanked drift. Recorded explicitly so the leveling lane does not go looking for it.

---

## What is NOT in this inventory, deliberately

* **The six parked branches' own corpus changes.** Wall #3's four staged files, the span tranche
  (golib-only), and F2 (generator-only) are banked on their branches and merge with them. They are
  not drift.
* **CRLF phantoms.** A regen shows ~52 files modified with an empty numstat; that is the documented
  phantom class, not drift, and it must be restored every time rather than banked.
* **The root attribution files.** Six files (`src/core/README.md` and siblings) are re-copied
  verbatim by every conversion and show as modified with an empty diff. Restore.

## The one procedural point the leveling lane should inherit

Every family above was found the same way: a regen produced a diff, and each differing file was
**classified before anything was staged**. That is what kept wall #3's four-file commit honest
while ~90 other files were moving in the same working tree. The leveling lane's job is the mirror
image — it banks what wall #3 restored — but the classification step is identical, and it is the
step that makes the difference between a leveling commit and an accident.
