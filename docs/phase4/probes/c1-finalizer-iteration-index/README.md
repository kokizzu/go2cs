# c1-finalizer-iteration-index

The iteration-index instrument for `runtime`'s `TestFinalizerType`, ruled as C1's commit 2(b)
(COORD `72f9db8e`). **It is a patch, not a committed edit, and the reason is structural:** `runtime`
is UNBANKED, so its test emission is git-ignored and not in the tree — there is nothing to edit, and
committing one would bank an unbanked row's test sources.

## What it measures

`TestFinalizerType` loops six `(object, finalizer)` shapes. Each iteration registers a finalizer,
calls `runtime.GC()`, then **blocks on a receive**. So the row does not fail — it **stops**: the
first shape whose finalizer never runs holds the receive forever and the package deadline kills the
host with **zero converted verdicts** (i9 measured exactly that solo, `6b2a96a1d`: 5 m 14 s, no
names at all). **Which iteration never delivers names that shape.**

The six, from the pinned source (i9's enumeration, `f7bfc68`):

| # | object | finalizer parameter |
|---|---|---|
| 1 | `x` (`*int`) | `func(v *int)` — matching |
| 2 | `Tintptr(x)` | `func(v Tintptr)` — matching |
| 3 | `Tintptr(x)` | `func(v *int)` — named type → underlying |
| 4 | `(*Tint)(x)` | `func(v *Tint)` — matching |
| 5 | `(*Tint)(x)` | `func(v Tinter)` — interface parameter |
| 6 | `x` (`*int`) | `func(v any) [4]int64` — `any` param **and a return value** |

## ⚠ Prediction, on record before the run

**Iteration 3 is where it stops.**

Reasoning, from the emission rather than from the Go source. The converter emits
`[GoType("ж<nint>")] partial class Tintptr;` — a **distinct wrapper class**, and **no named-pointer
wrapper anywhere in the corpus derives from `ж<T>`** (`git grep "partial class X : ж<"` is empty;
`TypeGenerator` speaks of an `m_value` wrapper field, not a base). Shape 3 therefore hands a
`Tintptr` instance to a delegate whose parameter is `ж<nint>`, and the relationship between them is
a **user-defined conversion**, which `Delegate.DynamicInvoke` does **not** perform.

⚠ **This contradicts the elimination in `f7bfc68`, and deliberately.** Arm 6 varied *looser by
REFERENCE CONVERSION* (`Action<object>`), which binds; shape 3 is *named wrapper → underlying*,
which does not. **Arm 6's green does not reach shape 3.**

⚠ **The weakest link in that prediction, named so the run scores it rather than me:** I have not
seen `go2cs-gen`'s *generated* partial for `Tintptr`. If the generator emits `partial class Tintptr :
ж<nint>`, a reference conversion exists, shape 3 binds, and the wall is 5 or 6 instead. **Falsifier:
the run stops at 1, 2, or reaches 4 or later.**

## How to run it — ⚠ order matters

`-test-action all` and `-test-action compare` **re-convert every non-marked corpus file before
building**, which wipes this patch and makes the probe read nothing while looking healthy.

```
go2cs -tests -test-action convert  <goroot>/src/runtime  <out>/src/core/runtime
python  apply.py                   <out>/src/core/runtime/mfinal_test.cs
go2cs -tests -test-action build    <goroot>/src/runtime  <out>/src/core/runtime
<published host> --json -run '^TestFinalizerType$' -timeout 5m  2> run.log
python  apply.py --verify          <out>/src/core/runtime/mfinal_test.cs   # the PATCH survived
python  apply.py --ran             run.log                                 # the TEST actually RAN
```

⚠ **THE HOST IS DRIVEN DIRECTLY, AND THAT REPLACED `-test-action run` ON 2026-09-08.** `run` IGNORES
`-test-filter` — it passes only `--json` and `-timeout` to the host, and `testFilter` is consumed on
the compare path alone — so the runbook's earlier instruction to use `run` **and** gate with
`-test-filter` could not both hold. i9 measured it: an unfiltered `run` died at 25 s on exit status 2
long before reaching `TestFinalizerType`. **The published host accepts `-run` itself**, which keeps
the patch (no converter involved at all) and gates the row. **`GOROOT` must be set or the host
refuses before it parses its arguments.**

⚠ **AN EARLIER VERSION OF THIS SEQUENCE said "then run / compare — NEVER `all`", contradicting the
warning at the top of this section.** i9 caught it before it cost a void reading, and settled it at the
converter source rather than by reading the labels — `testConversion.go:6103-6115`, whose branch
BODIES are the answer: `case "run"` calls `publishTestHost` and then executes the host with **no
convert anywhere**, while `case "compare", "all"` share one path into `compareGoAndConvertedTests`,
which is the re-converting one. **`compare` would have wiped the patch exactly as the warning above
describes.** Verified here independently at the same lines before this correction was written.

⚠ **Neither `-test-action run` nor the direct host produces a comparison record, and that is correct
for this probe** — its answer is the `println` on stderr naming the iteration, not a verdict pair.

⚠ **Read `--verify` AFTER the run, not only after the build.** A re-convert can only happen at an
action that converts, so the build is not where the patch dies.

⚠ **On a Windows box `python3` can be a Store alias** that prints an install advert and exits 0
rather than an interpreter, while `python` is real (i9, 3.12). Following `python3 apply.py`
literally there gives no patch, no recognisable error, and then a `--verify` of 0 that looks exactly
like the re-convert trap — **two different causes, one symptom.** Check the interpreter first.

⚠ **`--verify` AND `--ran` ANSWER DIFFERENT QUESTIONS, AND THE SECOND ONE IS WHY IT EXISTS.**
`--verify` reads the FILE: 0 markers means a re-convert wiped the patch and the reading is VOID. It
**cannot see a run that never reached the test** — over i9's 25-second death it reported *"patch
intact"* on a reading that measured NOTHING, so the runbook's stated validity gate PASSED on a void
result. `--ran` reads the RUN OUTPUT and refuses when no marker line appears.

**Both, every time.** A patch can survive a run that never happened, and a run can happen over a
patch that was wiped; neither check sees the other's failure.

Gating is by the host's own `-run`, which makes the row diagnostic by construction and never
bankable, as i9's pre-cut legs already are.

⚠ **THE MARKER TEXT CARRIES NO FORMAT VERB, AND IT USED TO.** The converted `println` does **not**
substitute a format — it prints its arguments separated by spaces, exactly as Go's `println` does —
so the first version printed `%d` LITERALLY with the number trailing the whole sentence. The reading
was unambiguous that time and one edit away from not being (i9, 2026-09-08).

⚠ **AND IT PRINTS BOTH NUMBERS**, `idx <i>` and `shape <i+1>`, because the loop index is 0-based
while the shapes in this document are numbered from 1. **Index 2 IS shape 3.** This file's own
falsifier was written in shape numbers against a probe that printed only the index — a reader
scoring from the output alone could have read a HIT as a MISS.

## Why `println` and not `t.Logf`

The failure mode being measured is a **hang killed by the package deadline**, and the host buffers
`t.Logf`. `println` goes to stderr unbuffered and survives the kill. Call shape verified against
golib (`builtin.cs:2279`, `println(params object[])`) and against real `runtime` call sites.

## Instrument notes

- **No glyph literals.** Every anchor is matched by SHAPE. The first draft retyped the loop's value
  identifier by hand with the wrong superscript character and matched zero times.
- **Scoped to `TestFinalizerType`'s body.** The `done`/`GC`/`ch` shape occurs **twice** in
  `mfinal_test.cs`; a file-wide anchor is ambiguous and the script refused on exactly that. That was
  the guard working — the fix is to scope, never to loosen.
- **Controls, both run against the real 1.23.12 emission:** applies cleanly (2 anchors, 3 markers,
  CRLF uniform, 8-space indent matching its neighbours); refuses on a file without the anchors;
  `--verify` calls an unpatched file VOID. **It has never been compiled — this container has no
  .NET.**
