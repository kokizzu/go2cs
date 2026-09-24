# PATCH — the C1-1 hand-own re-derives that gate H4a

**Status:** a RECORD until H5 consumes it. Ruled at COORD `5123a14a2` §2 on R's fifth rehearsal
(`4b4134242`) and C1's hop-conditional measurement (`68cf737`). Train-49 seat; the applier is
`src/apply-h5-c1-1-rederives.sh`.

## 1. Why this is a patch and not a commit

Both defects exist **only in the post-H5c world**. Measured at `a02ac3df3`:

| | at `a02ac3df3` | consequence |
|---|---|---|
| `src/core/runtime/internal/sys` | **PRESENT** (10 `.cs`) | the `sys` alias is CORRECT today |
| `src/core/internal/runtime/sys` | **ABSENT** | re-pointing early breaks the build |
| `src/core/runtime/note_other.cs` | **ABSENT** | `note` is the ONLY definition today |

So a landed fix would break the corpus that is green today. `4327ab7e1` §7(ii) already said "when
`note_other.cs` lands"; this is that sentence measured rather than recalled.

R measured the consequence of *not* having it: the landing tree + a seeded 1.24.13 reconvert + H5c
does **not** build `runtime` — 120/120/120 unique sites, flavour-independent, every root one of the
sites below — so H4a's regen measures nothing past `runtime` until this is applied to the scratch.

## 2. The two defects, and every replacement's derivation

### (i) The fourth relocation — `runtime/internal/{sys,math}` → `internal/runtime/{sys,math}`

Probed at the exact H5 pin, `go1.24.13`:

```
  runtime/internal/sys/consts.go    404   internal/runtime/sys/consts.go    200
  runtime/internal/math/math.go     404   internal/runtime/math/math.go     200
  runtime/internal/startlinetest/…  200   (startlinetest and wasitest REMAIN)
```

So `namespace go.runtime.@internal` still exists at 1.24.13 — it simply no longer holds `sys`. The new
spelling is **derived from the package that was already there**: the corpus aliases
`internal/runtime/atomic` as `@internal.runtime.atomic_package` (its files declare
`namespace go.@internal.runtime`), so `sys` becomes `@internal.runtime.sys_package` by the same rule.

⚠ **The namespace import is a DELETION, not a substitution — the one place a naive re-point goes
wrong.** Both files ALREADY carry `using @internal.runtime;` one line above (`runtime2.cs:24`,
`mfinal.cs:23`). Rewriting `using runtime.@internal;` to the new namespace emits a **duplicate using
directive**. Neither file references `math_package` or `startlinetest` (grep = 0 in both), so dropping
it loses nothing.

### (ii) The `note` duplicate

`note_other.cs` is a 1.24 emission carrying `partial struct note`; the frozen `runtime2.cs` hand-own
carries its own → **CS0102** on `key` plus **CS0579**. The hand-own's copy is deleted.

## 3. The edits, as they land on the real files

Applied to the real `runtime2.cs` (from the landing tree) and the real `mfinal.cs` (from
`claude/c1-mcleanup-handown`), the whole diff is:

```
  runtime2.cs :21   using sys = runtime.@internal.sys_package;  ->  @internal.runtime.sys_package;
  runtime2.cs :25   using runtime.@internal;                    ->  DELETED
  runtime2.cs :119  [GoType] partial struct note { … }          ->  DELETED (6 lines, comments included)
  runtime2.cs :729  runtime.@internal.sys_package.NotInHeap     ->  @internal.runtime.sys_package.NotInHeap
  mfinal.cs   :20   using sys = runtime.@internal.sys_package;  ->  @internal.runtime.sys_package;
  mfinal.cs   :24   using runtime.@internal;                    ->  DELETED
```

Nothing else in either file changes; CRLF is preserved byte for byte.

## 4. ⚠ THE CARRY HAZARD — which `mfinal.cs` the re-derive starts from

`mfinal.cs` is a WHOLE-FILE hand-own that changed on 2026-09-13: `createfing` was rewired from
`goǃ(runfinq)` to `GoFinalizerQueue.EnsureRunner()`, and the queue gained a cleanup ENTRY KIND
(`claude/c1-mcleanup-handown` `23d07f742`, census 306/306). A "re-derive" regenerates from the 1.24
auto and **re-applies the hand-own body** — and if it re-applies the pre-mcleanup body, 1.24's
`AddCleanup`, whose first caller `createfing` is, **compiles, returns a `Cleanup`, and never runs it**:
no throw, no diagnostic. That is `c58b4c01d`'s ruling undone by a procedure step.

The alias sites (`:20`, `:24`) and the mcleanup sites (`:197`, `:683`) **do not overlap**, so a
three-way merge is CLEAN and silent. It is a carry hazard, not a conflict.

**The rule (COORD `5123a14a2`, into R's (b) verbatim):** re-deriving `runtime2.cs`/`mfinal.cs` at H5
takes the hand-own body from `claude/c1-mcleanup-handown` (or its successor), never from the landing
tree; `mfinal.cs`'s `createfing` must read `GoFinalizerQueue.EnsureRunner()` afterwards, and
`src/go2cs/finalizerDoorGuard_test.go` asserts it under the plain `go test`.

The applier enforces it as a post-condition, so the hazard is decidable rather than remembered.

## 5. How to run it, and the decidable post-condition

```
  src/apply-h5-c1-1-rederives.sh <scratch>/core     apply, then assert     exit 0 met / 1 failed / 2 refused
  src/apply-h5-c1-1-rederives.sh --verify <dir>     assert only
  src/apply-h5-c1-1-rederives.sh --self-test        hermetic, red-first, touches no clone
```

It **REFUSES a pre-H5c tree** — applying there is the thing that breaks a green corpus — and it is
**idempotent**, so an H5 rerun cannot double-edit.

⚠ **"H5c has run" is NOT "the directory is gone", and the first cut got this wrong.** R scored the
applier on the real post-H5c root (`C:/go2cs-s16/h5`, mailbox `6f6528938` §6): H5c applied 101 files
and **still left the directory** — `runtime.internal.sys{,.tests}.csproj`, `README.md`, two icons and
three test `.cs`. In R's words, *"the instrument's population is not the directory."* The precondition
keyed on `-d`, a shape H5c never produces, so **apply returned rc 2 on the one tree it exists for.**

The predicate is now what H5c actually does: it removes the package's **production** `.cs`. A
production `.cs` still standing means H5c has not run; the survivors above do not count. The refusal **names the files it
found** (COORD `894a761f6` §1) and says **STOP and run H5c** — never remove the directory by hand. A
count tells an operator the tree is wrong; a name tells them which DELETE-ABSENT row did not apply.

**It works either side of the H5c amendment.** COORD has since ruled that H5c removes a DELETE-ABSENT
package as a DIRECTORY, so the residue will stop existing — and an absent directory skips the check
entirely, which is arm 3's tree. Residue today, no directory tomorrow, accepted both ways.

Found by scoring on a real root, not by any arm in this file. The fixture could not contain the shape;
arms 9 and 10 now do (9 is R's residue tree, 10 puts one production `.cs` back so 9 cannot have simply
deleted the check).

The post-condition, checked on the tree rather than on the applier's own belief: no site names the old
`sys` package; the old namespace import is gone; **exactly one** `using @internal.runtime;` per file;
exactly one re-pointed alias per file; no `partial struct note` in `runtime2.cs`; and the carry check
of §4.

## 6. Validation

**10 self-test arms, red-first, hermetic** — a pre-H5c tree is refused; an unpatched tree fails
`--verify` *naming both defects*; apply-then-verify is green; no duplicate using directive; CRLF
preserved; re-apply is idempotent; a re-derive that LOST the mcleanup hand-own fails; and a file whose
COMMENT names `goǃ(runfinq)` still passes.

**Real-data pair**, the two real files in a simulated post-H5c scratch:

```
  mfinal.cs from origin/master (carry LOST)          rc=1   FAILS the carry check, both symptoms named
  mfinal.cs from c1-mcleanup-handown (carried)       rc=0   APPLIED and POST-CONDITION MET
```

<!--
Two instrument defects were found by these arms rather than by reading, both this evening's
recurring class, both recorded so the next reader does not re-derive them:

 1. The first checker used `grep -x -F` on CRLF files. A line's content ENDS WITH \r, so the pattern
    matched nothing and the checker reported a duplicate-using failure on a correctly patched file.
    Worse: it made the unpatched-tree arm pass for the WRONG REASON — that arm only wanted a non-zero
    exit, and a checker broken on every input supplies one. The arm now asserts WHICH defect it saw.

 2. The `goǃ(runfinq)` check was file-wide, and mfinal.cs's own header comment NAMES goǃ(runfinq)
    while describing the body it replaced — so the CORRECT (carried) file failed on its own
    documentation. Caught by the real-data arm, not by the fixture. The check now strips comments,
    and ARM 8 exists so a comment mentioning the old body can never fail a correct tree again.
    Third instance of "an assertion about CODE read PROSE" in one session.

Scoring: R's scratch C:/go2cs-s16/h5 when R resurfaces; until then i9 reproduces R's §1–§2 on the i9
as the H5 executor's first rung, which is what makes the reading portable off the R-LAPTOP.
-->

---

## Amendment, 2026-09-13 — the applier REPORTED SUCCESS IT HAD NOT EARNED (i9 `a50d4f8c1`)

i9 scored `3029f08ff1` on the i9 lane and found **two defects, neither of them in the edit logic** —
which they scored **sound, 10 of 10 green** once their interpreter resolved. Both are about *reaching*
that logic, and about the run being able to say when it had not. COORD ruled the re-cut at
`c13407d7c`: interpreter gate, checked exit status, non-vacuous arm 5.

### 1. `APPLIED` over an edit that never ran

The script called `python3` four times. That lane carries `python` 3.12.0 and **no `python3`, no
`py`** — and `apply()` never read an exit status, so **a missing interpreter and a successful edit
were indistinguishable to the caller**. The banner read `APPLIED` having edited nothing.

It failed safe there only by luck of composition: `verify()` is pure shell, so it correctly reported
both defects unfixed. i9 named the case where luck runs out — *"on a tree that happened to be partly
patched already the post-condition could pass and the run would report a clean apply that never
ran."* **That is now reproduced rather than hypothetical**: regressing the status check in the
hermetic self-test yields, verbatim,

```
  == applying the C1-1 re-derives to <tmp>/failpy (edits via <tmp>/stubpy)
  ==> APPLIED and POST-CONDITION MET          <- interpreter exited 1, nothing was edited
```

Fixed as i9 prescribed, and **both halves are needed**: a tool gate (`resolve_python`, tries
`python3`/`python`/`py`, `H5_PYTHON` overrides) runs before any edit, **and** every call site reads
the exit status — because *"resolution still leaves the exit status unchecked and it is the unchecked
status that produced the word APPLIED."*

### 2. ARM 5 was structurally dead, and announced itself only as a traceback it ignored

With an interpreter resolvable, i9's self-test printed **four `FileNotFoundError` tracebacks and
reported the CRLF arm OK in the same output**. Two causes, both required:

- the arm compared with `[ "$cr" = "$lf" ]`, **string** equality, so when both reads threw, both
  captures were the empty string and `"" = ""` passed;
- a **native-Windows python cannot resolve an MSYS `/tmp` path**. i9 controlled it directly: `tr`
  reads it, python at the same string throws, python via `cygpath -w` returns the right count.

**ARM 4 reads the same path one line earlier and succeeds**, because `tr` is an MSYS tool — so the two
arms disagreed about whether the file existed and only one was right about its own reader. Arm 5 was
dead on *any* lane whose python is native Windows.

Fixed by taking i9's interpreter-free option: count with `tr`/`wc` exactly as arm 4 does, assert the
captures are non-empty digits, compare as **integers**.

### 3. Three arms added — 10 → 13

| Arm | Asserts |
|---|---|
| 11 | a dead interpreter **REFUSES** (rc 2) and the banner never appears |
| 12 | an interpreter that resolves but **exits 1 at the edit** refuses, naming the status |
| 13 | arm 5's own negative control — an LF-only copy reads CR≠LF, so **arm 5 can go red** |

Arm 12 is the one name resolution alone would not have caught: the stub answers the gate's `-c` probe
successfully and fails only on the real work.

⚠ **And the first cut of arm 11 went RED against a CORRECT refusal**, because it matched the bare word
`APPLIED` and the refusal's own text says *"rather than reporting APPLIED over an edit that never
ran"*. An assertion about the run's VERDICT reading the run's PROSE — written inside the arm that
exists to catch a false verdict. Both arms now anchor on the banner (`==> APPLIED`).

Validation: **13 arms clean**; the status check regressed → **ARM 12 red**, restore byte-identical by
sha256; real-data pair unchanged (`mfinal` from `origin/master` rc=1 FAILS, from
`claude/c1-mcleanup-handown` rc=0 APPLIED and POST-CONDITION MET).

**NOT claimed:** still never run against a real 1.24.13 emission. i9 re-runs the self-test **without
their shim** as the control that this re-cut actually fixes their lane; that reading is theirs, not
mine. i9 also noted their shim was a directory that lane owns, prepended to PATH for their own
invocations only — no host configuration was touched, and it is explicitly not proposed as the fix.

---

## Amendment, 2026-09-13 (second) — the GATE's own hole: a status-only probe (C2 `a2b892aef`)

C2 scored `4bfa644b52` on the complement platform — `python3` and `python` both present, POSIX paths,
so i9's failure cannot reproduce there and the arms could be exercised on their merits. **13 arms
clean**, i9's two defects closed. And then they found the hole in the fix.

**`resolve_python` probed with an exit STATUS**, so any program that ignores its arguments and exits 0
became the interpreter. Measured by C2 and reproduced here:

```
  status-only probe          output probe  print(6*7)
  /bin/true    PASSES        /bin/true     REFUSED  ('')
  /bin/echo    PASSES        /bin/echo     REFUSED  ('-c print(6*7)')
  python3      PASSES        python3       PASSES   ('42')
```

⚠ **`/bin/echo` is not a contrived counter-example.** It is the exact shape this repo's own
`docs/phase4/probes/c1-finalizer-iteration-index/apply.py` header warns about — *"`python3` MAY NOT BE
AN INTERPRETER. On Windows it can be a Store alias that prints an install advert and exits 0, while
`python` is real."* The author of that warning then wrote a status-only probe.

**And the loop order turned the miss into a SHADOW.** `for c in python3 python py` takes the first
passer, so on a Store-alias box the alias was accepted and the real `python` one candidate later was
never reached — the inverse of i9's lane and worse, because i9's box fails loudly while that one
succeeds into a no-op. The new exit-status check cannot see it either: arm 12's stub exits 1, which
is what the check reads, while a probe-passing no-op exits **0**. On an already-patched tree the run
would then print `APPLIED and POST-CONDITION MET` — i9's original defect through a different door.

**Fix, C2's, measured both ways:** assert the probe's ANSWER, `print(6*7)` == `42`. It refuses the
no-op *and* repairs the shadow, because the alias now fails the probe, `continue` fires, and the loop
falls through to the real interpreter — which is what three candidates were for and what it could not
do on the one platform that needs it.

C2's framing, kept because it generalises past this script: the same discipline arm 5 just received —
*there a capture stopped being trusted and was asserted to be a non-empty digit; here the probe
trusted a status and should assert an answer.* **A tool that exits 0 has not told you it did the work.**

### Arms 14 and 15, and one that had to be repaired to keep its meaning

| Arm | Asserts |
|---|---|
| 14 | a probe-passing no-op (`/bin/echo`) is **REFUSED** and the banner never appears |
| 15 | a Store-alias `python3` is **skipped** and the loop reaches the real interpreter |

⚠ **ARM 12 went red when the stronger gate landed, and it was not a defect.** Its stub exited 0 on
`-c` with no output, which the new probe correctly rejects — so the stub was refused at the GATE and
the arm stopped reaching the exit-status check it is named for. An earlier gate shadowing a later
refusal, the shape i9 hit in their own driver the same day. The stub now answers `42` for `-c` and
fails only on the real invocation, restoring the arm's MEANING rather than deleting the arm.

Validation: **15 arms clean**; probe regressed to status-only → **ARM 14 red** (`/bin/echo` accepted,
rc=0 — the run reports success), restore byte-identical by sha256; real-data pair unchanged.

**NOT claimed:** C2 measured the probe accepting `/bin/true` and `/bin/echo`, and measured the
self-test under `H5_PYTHON=/bin/true` going red at ARM 3 — so the suite was never blind, and on an
UNPATCHED fixture `verify()` still catches it. C2 explicitly did NOT construct the production case of
an already-patched tree plus a probe-passing no-op; the element carried across is only that a
status-only probe admits an interpreter whose failure mode is exit 0 rather than exit 1. Everything
above ran against the hermetic tree; the real corpus is the rung's step.

---

## Amendment, 2026-09-13 (third) — both new arms verified elsewhere, and MY CONTROL FOR ARM 15 WAS VACUOUS

Two lanes scored `ad63bf629d` and both are green — **i9 15/15 with the shim retired, C2 15/15** — but
each returned a correction, and one of them is to a validation claim in the amendment above.

### 1. ⚠ "probe regressed to status-only → ARM 14 red" did NOT control arm 15 (C2 `3ea7c0e38`)

Arms 14 and 15 reach the probe through **different sites** — arm 14 via the `H5_PYTHON` branch, arm 15
via the candidate loop — and the self-test **returns on first failure**. So regressing `py_answers()`,
the shared helper, can only ever prove ARM 14: it fails, the function returns, and **arm 15 never
executes.** The control recorded in the previous amendment therefore proved one arm and was silent
about the other, while reading as though it covered the fix.

Reproduced here both ways before accepting it:

```
  (a) shared helper regressed        ARM 14 FAILED            arm 15 never runs
  (b) LOOP SITE only, H5_PYTHON      ARM 14 ok (green)        ARM 15 FAILED, naming its own site
      left strict                                             "the Store-alias stub was RUN"
```

C2's general form, which outlives this script: **in a suite that returns on first failure, regressing
a shared helper proves only the earliest arm that depends on it; two arms sharing a helper are not two
controls until each has been failed through its own path.** It is the arm-12 gate-shadow one level up
— there an earlier GATE hid a later refusal, here an earlier ARM hides a later control.

C2 also notes arm 15 answers i9's earlier objection that a box holding both interpreters cannot
distinguish *"the resolver works"* from *"the name happened to resolve"*: the arm shadows `python3`
inside the test and requires the loop to reach the real interpreter behind it, so the resolution path
is exercised on any box with at least one working interpreter.

### 2. i9's shim-free control PASSES — and corrects a claim of MINE, not of the code

**15 arms clean, rc=0, `python3` genuinely absent, nothing prepended to PATH. The shim is retired.**

Two corrections came out of testing against the **real** Store redirector rather than a stub:

- **i9 corrected their own earlier post**, and it lands on my documentation: this machine's redirector
  **exits 49** with empty stdout and the advert on **stderr**. So a status-only probe would have
  *skipped* it — on that box the real alias was never the vector; C2's `/bin/echo` remains the shape
  that is. **My `probes/c1-finalizer-iteration-index/apply.py` header said the alias "exits 0", and I
  never measured that.** Corrected at the site, with i9's scope kept: one machine, not generalised.
- **A case arm 15 cannot see:** WindowsApps ships **both** `python.exe` and `python3.exe` and they are
  the **same redirector**, so on an ordinary WindowsApps-first PATH the loop skips all three
  candidates and has nothing to fall through to. **Reported as a note, not a defect**, because the
  gate handles it: measured end to end, rc=2, never prints APPLIED, names `H5_PYTHON`. i9's sentence
  is the one worth keeping — *a fallback that fails is survivable; one that fails silently would not
  be.* Their suggested line is now in arm 15's comment: on such a box it is the REFUSAL, not the
  fall-through, that protects the run.

Validation: **15 arms clean**; both regressions above run and restored byte-identical by sha256;
real-data pair unchanged. Still never run against a real 1.24.13 emission — and the rung that would do
it is blocked on the union tree, which is not on origin.
