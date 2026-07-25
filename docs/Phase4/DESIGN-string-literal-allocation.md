# DESIGN — retiring per-evaluation `@string` literal allocation (Tiers A / B / C)

> **Status: DRAFT for user review — 2026-07-25.** Scope blessed in principle (user: "the always-allocating
> `@string` is THE performance bottleneck in the system"; hoisting proposed by user with the
> "close to usage" placement requirement). Implementation is **one session, three independently-gated,
> independently-revertible landings** in the order A → B → C. Companion evidence: the
> `u8bench` experiment (scratchpad; `Mockup.cs` compiles the before/after forms against live golib).

## 1. Problem

Go string literals live in RODATA: `return "true"`, `s == "true"`, `HasPrefix(line, "//go:build")`
allocate **nothing** in a Go binary. The converted C# pays a heap allocation — and usually a
UTF-16→UTF-8 transcode — at **every evaluation** of the same sites, because each literal→`@string`
conversion materializes a fresh backing `byte[]`:

| Site shape (real corpus code) | Today | Cost per evaluation |
|:--|:--|:--|
| `exprᴛ1 == "true"u8` (strconv `ParseBool`, 12 operands) | span → `@string.ToArray()` per operand | 408 B / call |
| `return "true"u8` (strconv `FormatBool`) | span → `ToArray()` | 32 B / call |
| `HasPrefix(line, "//go:build"u8)` (go/build scan loop) | span → `ToArray()` | 40 B / line scanned |
| `Ꮡt.Fatal((@string)"…")` (any-target diagnostics) | UTF-16 string → `Encoding.UTF8.GetBytes` + box | 88 B / call |

Measured (u8bench, Release, net9.0, best-of-5 × 10M):

| Shape | current | fixed | speedup |
|:--|:--:|:--:|:--:|
| ParseBool("False") — 12 literal compares | 36.8 ns, 408 B | 7.7 ns, 0 B | **4.8×** |
| FormatBool value return (hoisted) | 7.0 ns, 32 B | 5.3 ns, 0 B | 1.3× |
| t.Fatal any-target (hoisted, pre-boxed) | 20.8 ns, 88 B | 6.5 ns, 0 B | **3.2×** |
| 16-line HasPrefix scan (hoisted prefix) | 185 ns, 640 B | 158 ns, 0 B | 1.2× + GC pressure |

The end state after all three tiers: **a literal costs at most one allocation per program run**
(Tier C), comparisons cost zero ever (Tier A) — i.e. Go's own cost model, restored.

### Safety precondition (verified)

Sharing one `@string` across evaluations is sound because nothing may mutate its backing array:

- `[]byte(s)` **copies** — `implicit operator byte[](@string)` (string.cs) copies precisely because
  the wrapping form once let utf8's TestDecodeRune corrupt the package's `utf8map` string table
  (incident recorded in the operator's comment). `slice<byte>(s)` routes through it.
- Go itself forbids string mutation; converted code has no legal write path to a string's bytes.
- Package-level Go string constants/vars **already** live as long-lived shared `@string` statics
  corpus-wide — Tier C adds no new exposure class.
- Residual writable aliases (`ToSpan()`, `ꓸꓸꓸ` spread) are read-only by every converter-emitted
  consumer (`append` copies in). They pre-date this design and are unchanged by it.

---

## 2. Tier A — golib span comparison operators (zero visual change)

**Change (golib only, ~40 lines):** `@string` gains the full comparison operator set against
`ReadOnlySpan<byte>`, both operand orders — `==`, `!=`, `<`, `<=`, `>`, `>=` — implemented on the
null-safe `Bytes` view via `SequenceEqual` / `SequenceCompareTo` (the same byte-ordinal primitives
`CompareTo(@string)` already uses).

**Effect:** every `x == "…"u8` / `switch string(y)` lowered chain / relational compare against a
literal becomes allocation-free **with the emitted source text byte-identical** — the u8 span now
binds the span overload by exact match instead of converting. No converter change, no goldens churn,
no readability cost. This is the largest single win (4.8× on ParseBool) at the lowest cost, which is
why it lands first.

**Legality:** user-defined operators may take `ReadOnlySpan<byte>` parameters — compile-proven in the
experiment (`gostrProof` struct in `Mockup.cs`), and precedented by `sstring`'s existing 12-operator
span/`@string` set (increment A of the sstring arc).

**Ambiguity audit (the one real risk):** `sstring` already defines span and `@string` comparison
operators, and `sstring` ⇄ `@string` convert implicitly both ways. The new operators must not
re-open the CS0034 class the sstring arc closed. Analysis: for `sstring == "…"u8` both operands
exactly match sstring's own span operator (no conversion) — unchanged; for `@string == "…"u8` the
new operator is the unique exact match; `@string == sstring` binds sstring's existing mixed
operators as today. The **full-corpus build is the oracle** (exactly how the math/big CS0034 was
caught in increment C); any ambiguity found gets resolved the same way — by adding the exact-match
overload, never by removing capability.

**Gates:** golib class → full behavioral suite (Output 0-fail); full `go-src-converted.slnx` build
0 errors (ambiguity oracle); banked canaries at counts. CNR expected byte-identical (no converter
change). PerfStringView re-run to confirm no regression on the sstring paths.

---

## 3. Tier B — the combined `(@string)"…"u8` rendering

**Change (converter):** `convBasicLit` renders the cast (`castToGoString`) and the u8 suffix
(`u8StringOK`) from independent flags; roughly ten emission sites force `u8StringOK = false` where
only the *bare* span's lack of an object-conversion motivated it, conflating "needs a cast" with
"can't be u8". Flip u8 back on at those sites so interface/`any`-target literals render
`(@string)"…"u8` — routing through the `ReadOnlySpan<byte>` constructor (memcpy) instead of
`Encoding.UTF8.GetBytes` (transcode). Measured 2.1–2.4× (ASCII) / 4.2× (non-ASCII), allocation
unchanged.

Site-by-site (each keeps or rewrites its rationale comment):

| Site | Action |
|:--|:--|
| convKeyValueExpr.go:25 (any-target map/struct values, object keys) | flip on |
| visitAssignStmt.go:118 / 1124 / 1387 (interface-target assignment forms) | flip on |
| visitSendStmt.go:72 (send into `channel<any>`) | flip on |
| visitReturnStmt.go:238 / 335 (interface-typed returns, incl. element form) | flip on |
| visitValueSpec.go:170 (`var v any = "x"`) | flip on |
| convCallExpr.go:1691 + convExprList.go:95 (any-arg / generic-arg contexts) | flip on — the cast gives the expression a real type, so generic inference and `params object[]` both work |
| **visitStructType.go:162 (struct tags)** | **stays off** — tags land in attribute arguments, which must be compile-time constants; a span cannot be one |
| convBinaryExpr.go:957 (concat suppression, `(@string)"a" + "b"`) | verify separately — expected to resolve via `operator+`; the math/big panic-concat CS0034 history lives here, so it gets its own A/B check before flipping |

**Relationship to Tier C (important for review):** Tier C replaces most of these very sites with
hoisted field references, so Tier B's *standalone* footprint shrinks to the contexts C deliberately
excludes — package-level / one-time initializers, where per-evaluation cost doesn't exist and the
inline literal is the more readable form. Tier B is still a required landing, for two reasons:
its combined rendering is exactly what **Tier C's field initializers** emit
(`… = (@string)"…"u8;` for pre-boxed fields), and it independently banks the 2–4× on every
any-target site should C be rejected or deferred in review. The intermediate golden churn between
B and C is contained to one session and the user reviews the final state.

**Gates:** converter go test; CNR **classified** (every changed site is this one class); behavioral
goldens re-baselined via UpdateTestTargets after inspection; full suite Output 0-fail (values are
byte-identical — compile-time UTF-8 of a valid literal equals runtime `GetBytes`); seeded corpus
reconvert + overlay + 0-err build. Literals with `\xHH` raw-byte escapes are already diverted to the
byte-array-backed path before this rendering (convBasicLit.go:644), so every affected literal is
u8-expressible by construction.

---

## 4. Tier C — hoisting literals to `static readonly` fields "close to usage"

### 4.1 The form

One field per unique literal per package, placed **immediately above the first consuming function**,
initialized with the Tier-B rendering; call sites reference the field:

```csharp
// ── emitted form ─────────────────────────────────────────────
// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string trueˢ = "true"u8;
private static readonly @string falseˢ = "false"u8;

// FormatBool returns "true" or "false" according to the value of b.
public static @string FormatBool(bool b) {
    if (b) {
        return trueˢ;
    }
    return falseˢ;
}
```

For a literal whose **every** use in the package is an `any`/interface target (the typical unique
diagnostic message), the field is emitted **pre-boxed** so call sites allocate nothing at all:

```csharp
private static readonly object bytesBufferReadFromˢ = (@string)"bytes.Buffer.ReadFrom didn't panic"u8;
…
    Ꮡt.Fatal(bytesBufferReadFromˢ);
```

Mixed-use literals get one `@string` field; any-target uses of it box per call (24 B — still
removes the dominant byte[] + transcode cost). Never two fields for one literal.

### 4.2 Hoist set — exactly the value-materializing contexts

| Context | Hoist? | Why |
|:--|:--:|:--|
| `return "…"u8`, `@string` local/param assignment | ✅ | value materializes per evaluation |
| argument to an `@string` parameter (incl. fmt format strings) | ✅ | ditto — the broadest hot footprint |
| `any`/interface targets (args, returns, sends, assignments, KeyValue) | ✅ | pre-boxed when exclusively any-target |
| map index keys, composite-literal elements *inside function bodies* | ✅ | rebuilt per evaluation |
| comparison operands (`==`, `!=`, relational, lowered switch chains) | ❌ | Tier A makes them zero-alloc with the literal inline — strictly better readability |
| concat operands (`x + "…"u8`) | ❌ | `operator+(@string, ReadOnlySpan<byte>)` already consumes the span without materializing |
| `[]byte("…")` / `[]rune("…")` conversion sources | ❌ | result must be freshly mutable (Go copy semantics); `slice<byte>("…"u8)` is already the optimal single mandatory alloc |
| sstring-elided sites | ❌ | already zero-alloc spans |
| struct tags | ❌ | attribute arguments (compile-time constants) |
| package-level var/const initializers, package-level composite literals | ❌ | one-time by nature; Go-named constants are already "hoisted by the source"; inline literal is the more readable form |
| literals already diverted to byte-array-backed `@string` (`\xHH`) | ❌ (v1) | rare, cold; revisit only if a hot site appears |

### 4.3 Naming

- **Marker:** new symbols.json entry `HoistedLiteralMarker = "ˢ"` (U+02E2 MODIFIER LETTER SMALL S,
  category Lm — valid identifier char), suffix position: `trueˢ`, `goBuildˢ`. Regenerate both
  projections via gensymbols; never hardcode the glyph (per the standing Symbols rule). The
  marker-shaped-user-identifier exposure is the same documented, accepted one as ᴛ/ʗ/Δ/ᴠ.
- **Slug:** identifier-safe words of the literal, camelCase-joined, truncated at a **word boundary
  ≤ 24 chars**: `"true"` → `trueˢ`; `"//go:build"` → `goBuildˢ`;
  `"bytes.Buffer.ReadFrom didn't panic"` → `bytesBufferReadFromˢ`.
- **Fallback:** empty/digit-leading/non-ASCII-only slugs → `strˢN` (per-package ordinal in
  first-occurrence order). The literal remains visible in the field initializer one hop above.
- **Collisions:** distinct literals with the same slug → deterministic ordinal suffix in
  package-wide first-occurrence order (`msgˢ`, `msgˢ2`). Collisions against Go identifiers go
  through the existing name-collision analysis like any other emitted name.

### 4.4 Placement, dedupe scope, and determinism

- **Dedupe is package-scoped, placement is first-use.** All files of a package emit into one
  partial `<pkg>_package` class, so per-file fields would collide (CS0102) whenever two files use
  the same literal. The converter keeps a per-package registry: the field is emitted above the
  first consuming function of the first consuming file (deterministic file order); later files
  reference it. Locality is exact for the common case (a literal used in one function) and
  one-partial-class-hop otherwise (IDE go-to-def resolves it).
- **Receiver methods and func literals** hoist to the same package class, above the containing
  top-level declaration.
- **Test conversions (`-tests`):** the test emission pass seeds its registry with the production
  pass's field names — production output stays byte-identical whether or not tests are converted,
  and internal-test files (same package class) cannot collide with production fields.
- **Determinism:** names and placement derive only from literal content + source order; the
  converter stays byte-deterministic.

### 4.5 What Tier C does *not* do (v1)

- No hotness heuristics, thresholds, or caps — the rule is uniform and deterministic. If the corpus
  A/B shows a pathological field-count file, that's a review finding, not a pre-added knob.
- No golib intern cache for 1-byte strings (candidate future refinement; noted, not built).
- No change to sstring elision, `slice<byte>` literal conversions, or the byte-array-backed path.

### 4.6 Gates (heaviest of the three — this is a corpus-wide re-baseline)

Converter go test → CNR **classified** (hoist sites + field blocks only) → behavioral goldens
re-baselined (UpdateTestTargets after inspection) → full suite with **Output 0-fail** (behavior must
be byte-identical) → seeded corpus reconvert + overlay + 0-err build → **full banked-package
operational sweep** (all validated packages at their counts — the strongest available proof that
shared statics change nothing observable) → performance suite re-run (StringView unchanged;
PerfString expected flat-to-better). New behavioral guard project **`StringLiteralHoisting`**:
value return, @string arg, any-target pre-boxed, mixed-use single-field, map key, in-function
composite, comparison NOT hoisted, concat NOT hoisted, package-level init NOT hoisted, `[]byte`
source NOT hoisted, slug collision ordinals, digit-start fallback, cross-file dedupe (two files, one
field). Docs in the same landing: ConversionStrategies.md headline section + Reference subsection
with real corpus before/after; Symbols docs pick up ˢ.

---

## 4.7 Measurement instrument — `PerfStringMatch` (committed ahead of the arc)

A new performance benchmark, **`src/Tests/Performance/PerfStringMatch`**, exercises exactly the
tier-target shapes in one hot loop (20M iterations): switch-on-string dispatch (Tier A comparison
chains), `strings.HasPrefix(line, "//go:build")` literal arguments, literal returns, and literal
map-key counters (Tier C value shapes) — 4–9 per-iteration `@string` materializations that the Go
original never pays. Verified byte-identical Go↔C# output through the runner's Verify phase.
Indicative pre-arc numbers (machine under load, provisional): Go ≈ 147 ms, C# JIT ≈ 1.69 s —
**≈ 11× — the worst ratio in the suite**, i.e. the headroom this design targets.

**Measurement protocol for the implementation session** (all timed runs on a quiet machine — no
concurrent agent builds):
1. **Baseline before Tier A:** full suite `./run-performance.ps1 --update-readme` — the pre-arc
   table lands in the README History section per the established .NET-version-comparison pattern.
2. **After each tier:** `--filter StringMatch` (seconds) to attribute the improvement per tier;
   record the three points in the landing commits.
3. **After Tier C:** full suite `--update-readme` — every other benchmark doubles as the
   no-regression oracle (StringView must stay flat; String/Map must not regress).

## 5. Sequencing (one session) and rollback

| Order | Landing | Risk | Revert story |
|:--:|:--|:--|:--|
| 1 | **A** golib operators | CS0034 (audited; corpus build is the oracle) | revert one golib commit; nothing else references the operators by name |
| 2 | **B** combined rendering | none beyond golden churn | revert converter commit + golden re-baseline |
| 3 | **C** hoisting | emission complexity; corpus-wide churn | revert converter commit + re-baseline; A and B stand on their own |

Corpus (`go-src-converted`) is regenerated per-tier for gating but **committed once**, at session
end, as the post-C state (plus the banked-package test-artifact refresh) — the rebank precedent:
generated churn stays out of the converter-fix commits. The session runs **after the r10 wave
lands** so this rebaseline and the stages-4/5 record-set rebaseline never interleave in one diff.

## 6. Open decisions for review (recommendations inline)

1. **Marker glyph `ˢ` suffix** — alternatives: prefix position (`ˢtrue`), or a different Lm glyph.
   *Recommended: `ˢ` suffix — reads as a natural "string constant" annotation after the slug.*
2. **Pre-boxed `object` fields for exclusively-any literals** — costs one field-type irregularity,
   buys literally-zero-alloc diagnostics. *Recommended: yes (it's deterministic from the
   registry's own use-set; no heuristics involved).*
3. **Slug budget 24 chars** — shorter keeps call sites tight, longer keeps more of the message
   visible. *Recommended: 24, word-boundary.*
4. **In-function composite-literal elements** — uniform hoisting may emit many fields above a
   function that builds a large literal table per call. *Recommended: hoist uniformly in v1;
   inspect the corpus A/B for pathological cases before landing (data over speculation).*
5. **Tier B's transient churn** — an alternative is to land B's rendering capability without
   flipping the corpus sites (C then uses it only in initializers), avoiding one golden
   re-baseline at the cost of losing B's standalone banking. *Recommended: flip the sites — B must
   be independently complete and revertible.*
