# DESIGN — untyped-constant emission: the `UintSize` shape, censused, and three sized candidates

**Status: SIZING DRAFT — no cut. The ruling follows this draft (coordinator dispatch 2026-09-02,
the Fable queue's item 3). Census instruments and raw outputs referenced below live in the lane
scratchpad; every count in this document is reproducible from the committed predicate description
plus the pinned toolchain.**

## 1. The construct, and why it costs

A Go package-level **untyped** constant has no legal C# `const` form when the converter models its
adaptability with a wrapper struct: `visitValueSpec.go` emits it as a **get-only property** of a
golib wrapper type —

```csharp
// src/core/math/bits/bits.cs:21 (the shape's canonical instance)
public static UntypedInt UintSize => /* uintSize */ 64;
```

The property form was chosen deliberately over `static readonly` to kill the class-textual-order
initialization trap (the compress/flate `huffmanNumChunks` story recorded at the emission site),
and its comment claims *"the JIT folds the literal at every use — the Go semantics exactly."*

**The math/bits word-size arc falsified that claim for the comparison path** (measured 2026-09-02,
Release + `DOTNET_TieredCompilation=0`): `UintSize == 32` is a **struct comparison per call** —
`operator ==` → `Equals` → the private `Compare`, which the JIT compiles standalone at IL 141 and
never inlines (`DOTNET_JitDisasmSummary` evidence in `bits_impl.cs`'s header). Measured **2.72×**
on `addMulVVW`'s inner loop; the branch Go folds at compile time is evaluated on every call. The
mixed signed/unsigned payload handling that makes `Compare` correct (the strconv 2^63 regression
recorded in `UntypedInt.cs`) is also what makes it big.

Arithmetic operators are thin by contrast (`left.m_value + right.m_value` plus a ctor); the
comparison operators are the expensive family, and comparisons against wrapper-typed operands are
the expensive class.

## 2. Census — two derivations, both positive-controlled

### 2.1 Use side (derivation B): `go/types` over the pinned GOROOT, object-resolved

Instrument: a `go/packages` walk of **`std` with `Tests: true`** at **go1.23.12**
(`C:\Users\Admin\sdk\go1.23.12`, printed and guarded in-run — see §2.3). A site is a
`BinaryExpr` (comparison / arithmetic / shift) with at least one operand **resolving through
`go/types` objects** — spelling-independent by construction, which is what "alias-resolved"
requires — to a `*types.Const` that:

- sits at **package scope** (`Parent() == pkg.Scope()`; function-local untyped consts take the
  existing tightening arm and are excluded),
- has an **untyped basic** declared type (`UntypedInt`/`UntypedRune`/`UntypedFloat`/
  `UntypedComplex`; untyped string/bool take other emission arms),
- has a value **fitting the wrapper's int64/uint64 payload** (else the `GoBigConst` arm).

This mirrors `visitValueSpec.go`'s decision, i.e. the census predicate is the emission predicate.

**Positive control** (ordered by the dispatch: on `bits.cs:21`): the census must find math/bits'
`UintSize` sites. Independently derived expectation (grep of the pin's `bits.go`): 11
`UintSize == 32` comparisons plus the `UintSize - Len(x)` family. Found: all 11, at the exact
lines, classified `comparison/foldable`, plus the arith sites — 53 math/bits sites total.

**Negative controls, one per axis the predicate reads** (the vary-each-axis rule):

| axis | control | result |
|---|---|---|
| named-type filter | `time.Minute` / `syscall.EINVAL` sites must be absent | 0 found ✓ (fires: these exist as comparisons in std) |
| untyped filter | neuter it (drop `IsUntyped` **and** widen the kind switch — the first neuter alone is masked by the upstream filter) | 6,109 → **6,696**; restore → set-identical to baseline ✓ |
| toolchain | GOROOT guard (§2.3) | refuses unset and wrong-root, allows the pin ✓ |

### 2.2 The numbers

**6,109 sites** across 306 std packages (4,910 production / 1,199 `_test.go` — the `-tests`
dimension included by rule):

| op class | live | foldable (whole expr is a Go constant) |
|---|--:|--:|
| comparison | **1,854** | **132** |
| arithmetic | 1,552 | 2,334 |
| shift | 78 | 159 |
| **total** | **3,484** | **2,625** |

Other-operand classes: 3,484 typed-expr (mixed — the wrapper implicit-conversion path), 1,678
untyped-literal + 947 untyped-const (wrapper-vs-wrapper — the `Compare` path; note every
wrapper-vs-constant comparison is necessarily foldable, which is why the 132s coincide).

Roster split: **3,950 sites in roster-row packages / 2,159 not**. Top packages by sites (prod in
parens, R = roster row): runtime 1,795 (1,442) —, unicode 436 (417) R, math 313 (285) R, math/big
236 (104) R, syscall 227 (221) R, compress/flate 181 (158) R, **crypto/tls 181 (152) R**, time
169 (132) R, image/jpeg 120 (116) R, unicode/utf8 115 (101) R, net 93 (66) R, reflect 90 (50) —.

*Granularity disclosure:* "sits on a path a roster row measures" is approximated at package
granularity (site-in-a-roster-package). Path-level attribution (does a measured test actually
execute the site) is not claimed anywhere in this document.

### 2.3 Instrument integrity (what it took to make these numbers bankable)

The census initially produced **three invalid readings in a row**, each caught by a control, each
a known trap through a new door: (a) a `go build | head` pipeline whose `&&` chained off `head`'s
exit code reported BUILD-OK over a compile error, and the **stale binary** then answered a
control; (b) a per-command `PATH=` prefix bound only to `go build`, so the census itself ran
against the **ambient go1.23.1** — the wrong-release trap's fourth firing this arc, detected as a
phantom 4-site "nondeterminism" whose diff showed two different GOROOT paths; (c) a first neuter
control that could not fire because the axis it varied was masked by an upstream filter. The
instrument now carries a **GOROOT guard**: `GCENSUS_GOROOT` must be set and every loaded source
file must live under it (build-cache intermediates exempt), else it refuses to run. Both guard
directions are proven, the pinned baseline reproduces set-identically, and the restore after the
neuter is set-identical to the baseline.

### 2.4 Declaration side (derivation A): the corpus emission census

`git grep` over `src/core` for the emission shape (`git grep`, not bare `rg`, per the
`.gitignore` census rule): **6,812 wrapper-property declarations** in **193 packages** — 6,667
`UntypedInt`, 145 `UntypedFloat`, 0 `UntypedComplex`. Control: `bits.cs:21` matched.

## 3. The three candidates

Costs are stated per the two-seeded ritual's units. **No candidate has been cut, so every hunk
count below is DERIVED from census arithmetic, not measured by a diff** — stated per the
honest-sizing rule; a real two-seeded diff is each candidate's first gate if pursued.

### 3.1 Candidate 1 — integral untyped constants that fit `long` emit as C# `const` with an inferable type

The deep fix: a real compile-time constant, so the JIT sees a literal everywhere and every
downstream effect (folding, dead branches, no ctor, no `Compare`) follows for free. It also
retires the declaration-order concern more thoroughly than the property does.

- **Reach:** all 6,667 UntypedInt declarations (float needs its own analysis; complex is empty).
- **The load-bearing subtlety:** C#'s implicit *constant* conversions are **`int`-sourced only**
  (an in-range `int` constant converts to `sbyte`…`ulong`; a `long` constant does **not**
  implicitly narrow — CS0266). So the inferable type must be chosen per constant (`const int`
  when the value fits, `const long`/`const ulong` above that), and every use site whose context
  needs a type outside the chosen type's implicit reach takes an explicit cast the converter must
  emit. That is a **use-site rewrite whose true extent this census does not measure** — §2's
  6,109 counts binary-expr sites only; call arguments, array lengths, case labels and assignment
  contexts are additional consumers. **A use-context census extension is candidate 1's first
  gate, before any hunk count is believed.**
- **Derived hunk count:** ≥ 6,667 declaration lines + an unmeasured use-site fraction; the
  largest footprint of the three by an order of magnitude.
- **Guard:** a behavioral golden pinning the `const` emission and a program consuming one
  constant in int-, long-, uint64- and float-typed contexts (the conversion-reach matrix).
- **Predicted effect:** RSA probe **≈ 0** — the post-word-size hot path no longer reads
  `UintSize` (stated so the win is not double-counted). Handshake: crypto/tls holds 152
  production sites; direction favorable, magnitude unpredicted (path-level attribution absent).

### 3.2 Candidate 2 — fold constant expressions at the converter

`go/types` already computes every constant expression's value (`info.Types[expr].Value`); the
converter emits the folded literal with the original spelled in the comment form the corpus
already uses (`/* 32 << (^uint(0) >> 63) */ 64`). Visual fidelity is preserved by the comment;
Go itself folds these at compile time, so the emitted semantics are exactly Go's.

- **Reach:** the **2,625 foldable sites** (132 comparisons — the entire measured `Compare`-shape
  class, `UintSize == 32` included — plus 2,334 arith and 159 shifts).
- **Derived hunk count:** ≤ 2,625 corpus lines (multi-site lines collapse); converter change is
  one early-out in `convBinaryExpr` where the whole expression is constant and a wrapper-typed
  operand is present. Two-seeded diff expectation: those lines and nothing else.
- **One design question to rule on, not decide silently:** a folded branch condition emits
  `if (/* UintSize == 32 */ false)`, which C# compiles with a CS0162 unreachable-code warning in
  the corpus's normal warning spread. Alternatives (suppress at the site / fold only
  sub-expressions and leave lone conditions) trade fidelity differently.
- **Guard:** a behavioral golden pinning the folded form at a `UintSize == 32`-shaped site and a
  mask-arithmetic site (`m0 & m1` shape).
- **Predicted effect:** RSA probe ≈ 0 (same reason as 3.1). Handshake: the foldable fraction of
  crypto/tls's sites dies at compile time. The 1,854 live comparisons are untouched — this
  candidate removes the measured class, not the whole family.

### 3.3 Candidate 3 — make the wrapper's operators inlineable

`[MethodImpl(AggressiveInlining)]` on `UntypedInt.Compare` (IL 141 — over the default budget,
which is precisely why the attribute and not hope) and the comparison operators; the arithmetic
operators are already thin. The word-size arc measured this mechanism directly: the same
attribute took `Add` from compiled-standalone (IL 59, two calls per word, 6.50–6.93 ns/word) to
inlined (2.76–2.89), and once inlined the JIT constant-folds through `readonly struct` ops — so a
foldable comparison likely dies at JIT even without candidate 2.

- **Reach:** every live wrapper operation corpus-wide, at runtime, with **zero emission change**
  — the two-seeded corpus diff is empty by construction; the footprint is ~10 attribute lines in
  `src/core/golib/UntypedInt.cs` (+ siblings if ruled).
- **Gates (route #7 applies — golib compiles into everything):** full behavioral COMPILE, a
  cross-assembly consumer, the converter suite, **and the cost canary** — golib operator inlining
  is a corpus-wide code-size change, so `crypto/internal/nistec` wall-time before/after per the
  split canary rule, plus `DOTNET_JitDisasmSummary` evidence that `Compare` actually left the
  compile list (the claim dies if it is still there).
- **Predicted effect:** RSA ≈ 0 (as above). Handshake and corpus-wide: removes the call cost of
  the 1,854 live comparisons AND (via constant-prop) plausibly the 132 foldable ones; magnitude
  unpredicted, measured by the probe if pursued.

### 3.4 Composition

3 is the cheapest and composes with either; 2 targets exactly the measured class and keeps the
JIT out of the reasoning; 1 is the deep fix gated on a use-context census this document does not
contain. Nothing here rules an order — that is the ruling this draft exists to inform.

## 4. Falsifiers (the intrinsic design's section, kept)

- **"The property form is expensive"** dies wherever `DOTNET_JitDisasmSummary` shows the
  comparison inlined and folded already — the claim is measured only for the `Compare` path at
  IL 141 on net10.0's JIT; a future runtime inlining it unaided retires candidate 3 (the
  attribute becomes redundant rather than wrong, per the `bits_impl.cs` precedent).
- **"Folding is safe"** dies on any site where the emitted literal's C# type binds a different
  overload than the wrapper did — the candidate-2 guard exists to catch exactly this, and a
  two-seeded diff hunk outside the predicted set is a stop.
- **"The handshake will move"** is a direction, not a number; if a cut lands and the handshake
  probe moves <5%, the family was not on that path and the census's package-granularity column
  was the wrong proxy — record the null like the addMulVVW nulls were recorded.
- **"6,109/6,812" are toolchain-pinned counts** (go1.23.12); they die at the next corpus hop and
  must be re-derived, not carried (the re-measure-never-carry rule).

## 5. What this draft does not do

No converter change, no golib change, no corpus regen. The RSA-probe predictions are all ≈ 0 and
stated so nobody re-spends the word-size win; the interesting probe for any pursued candidate is
the TLS handshake and a wrapper-comparison micro-benchmark, neither of which exists yet as a
standing instrument.
