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
python3 apply.py                   <out>/src/core/runtime/mfinal_test.cs
go2cs -tests -test-action build    ...        # then run / compare — NEVER `all`
python3 apply.py --verify          <out>/src/core/runtime/mfinal_test.cs
```

`--verify` reporting **0 markers means the reading is VOID** — the pipeline re-converted over it.
Gate the row single-test (`-test-filter`), which makes it diagnostic by construction and never
bankable, as i9's pre-cut legs already are.

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
