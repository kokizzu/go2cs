# DRAFT — Span-unification census: the harvest is smaller and sharper than the hypothesis (read-only census, 2026-08-22)

> **DRAFT for coordinator review.** Read-only census of `src/core/golib/` on master (`d03f086c1`),
> C:\Projects\go2cs. Scopes the FUTURE HARVEST item banked at
> `docs/phase4/BOARD-next-validation-candidates.md:17920` ("Span-unification sweep beyond the
> native-slice v1 minimal set"). All paths repo-relative; line numbers against the cited tip.

## Headline

The hypothesis ("hot paths added piecemeal, unifying over spans would simplify and speed up in
many small wins") is **half right**. golib is already substantially span-clean: `slice<T>.ToSpan()`
is the design's ratified discriminant-once point (slice.cs:537–550), `@string.Bytes` is a
`ReadOnlySpan<byte>` window that every comparison/hash/concat routes through, `sstring`/`sslice`
are span-native by construction, and the native-slice v1 minimal set (`copy` same-type arms,
bytealg, string conversions) landed as designed. What remains is **not a sweep — it is nine
discrete adoptions**, of which one is a genuine hot-path allocation bug-shape (`copy` from
`@string` allocates a full copy of the string per call, and `strings.Reader.Read` sits on it),
one is a latent correctness defect found in passing (double-offset in the heterogeneous `copy`
fallback), and the rest are the "individually small, collectively the point" wins the harvest
item predicted. A tenth family (indexer double bounds-check, the sstring precedent) is real but
higher-risk and belongs in its own measured arc.

## (a) Existing span fast paths — where the code already is span-clean

| Fast path | Where | Trigger |
|---|---|---|
| `slice<T>.ToSpan()` — pays the managed/native discriminant ONCE, then flat | slice.cs:537–550 | every bulk op (`CopyTo` 527, `Append` 1094, `clear` builtin.cs:1098, bytealg) |
| `slice<T>.Append` — span copies both arms, single-element store fast path, native within-cap writes the mapping via span | slice.cs:1044–1098 | every `append` |
| `@string.Bytes` — `ReadOnlySpan<byte>` window; O(1) slicing (the r57c archive/zip quadratic fix) | string.cs:66, 204–219 | all reads |
| `@string` equality/compare/hash — `SequenceEqual`/`SequenceCompareTo` (SIMD)/`AddBytes` | string.cs:286–314 | `s == t`, `s < t` |
| `@string`/`sstring` u8-literal operator families — literal compared/concatenated in place, zero alloc | string.cs:627–724; sstring.cs:278–459 | converter emits `s == "…"u8`, `s + "…"u8` |
| `sstring` — whole type is a `ReadOnlySpan<byte>` view; indexer bypasses the span's redundant bounds check via `Unsafe.Add` (measured on AOT) | sstring.cs:46–96 | converter escape analysis for non-escaping `string([]byte)` (sstringHoistOperations.go) |
| `sslice<T>` — `Span<T>`-backed stack slice, `TryAppend` in place | sslice.cs (whole file) | converter stack-slice emission |
| `tmpstring` / `TransientAliasOf` — zero-copy map-read keys (Go's `slicebytetostringtmp`) | string.cs:121–129; builtin.cs:2412–2415 | map-index READ keys only |
| `builtin.copy` same-type arms — `ToSpan()[..min].CopyTo(...)` (memmove semantics, overlap-safe) | builtin.cs:813, 956 | `copy(dst, src)` via `ISlice` views |
| `builtin.clear` — vectorized `span.Clear()` when zero is default | builtin.cs:1106–1119 | `clear(s)` |
| `ByteSeqExtensions.ToSlice/ToGoString` — one `ꓸꓸꓸ` span read + `CopyOf` | IByteSeq.cs:90–113 | `[]byte(s)` / `string(s)` on `string\|[]byte` constraints |
| `ToUTF8Bytes` / `ToRunes` / `DecodeRunes` — stackalloc + span walk | builtin.cs:390–417; string.cs:392–434 | rune/string conversions |
| bytealg hand-own — `ToSpan().IndexOf` / `SequenceCompareTo` (vectorized BCL) | src/core/internal/bytealg/bytealg_impl.cs:35, 51, 104 | IndexByte/Index/Compare |
| variadic delegates — `params Span<T>` tails | variadic.cs (whole file) | every variadic func value |
| `array<T>` bulk ops — `ToSpan`-routed `CopyTo`/`ToArray`/`Clone` | array.cs:342–362 | array copies |
| `AllocationCounter.CopyOf(scoped ReadOnlySpan<T>)` — the one copy primitive, span-typed | AllocationCounter.cs:207–213 | all golib copies |

`map.cs`, `channel.cs`, and the reflection surfaces hold no element-copy hot loops; SparseArray and
PinnedBuffer's enumerator are cold paths. This much of the census is genuinely done — the piecemeal
arcs converged rather than diverged.

## (b) + (c) Candidates — non-span hot paths, and native-fork merges

Numbered for the ranking below. "Fork" = a `m_nativeBase != 0` arm a span abstraction would merge.

**C1 — `copy` from `@string` allocates a full copy of the string per call.**
builtin.cs:932–936 (`slice<byte>` dst), 975–978 (`ISlice<byte>` dst), 993–997 (`ж<slice<byte>>` dst).
Current shape: `slice<byte> bytes = src;` binds `implicit operator slice<byte>(@string)`
(string.cs:469–472), which materializes `AllocationCounter.CopyOf(value.Bytes)` — a charged,
string-length heap array — then copies AGAIN into dst. Go's `copy(b, s)` is one copy, zero
allocations. Corpus witness that this is hot: `strings/reader.cs:49` — `strings.Reader.Read`
binds this overload once per Read with the string's remaining tail, so every `strings.NewReader`
consumer (io, bufio, encoding, tests) pays a full-tail allocation per read. Proposed:
`src.Bytes.Slice(0, (int)min).CopyTo(dst.ToSpan())` — `Bytes` is internal-visible to builtin, and
`dst.ToSpan()` already serves the native-backed arm. Effect: deletes one allocation + one full copy
per call; also removes a golib-inflicted allocation charge (see the disclosure note at the end).
Risk: near-zero — immutable source, memmove-semantics destination, both backings served by the
existing span surfaces.

**C2 — `copy(slice<T1>, slice<T2>)` same-type path: managed/native fork, native arm element-by-element.**
builtin.cs:840–859. Current: managed → `Array.Copy(src.m_array, src.Low, dst.m_array, dst.Low, min)`
(858); native → per-element `Unsafe.As` loop over spans, two bounds checks per element (850–854).
Proposed: a `typeof(T1) == typeof(T2)` fast path — the ONLY case converted Go emits, since Go's
`copy` requires identical element types — doing `src.ToSpan()[..(int)min].CopyTo(Unsafe.As<...>(dst.ToSpan()))`
(or plain `CopyTo` after the type test folds), serving BOTH backings; keep `Array.Copy` for the
assignable-but-different managed case and the `Unsafe.As` loop for assignable-but-different native.
Effect: merges the fork for the dominant case, vectorizes native-to/from-native copies, and the
same-type test is a JIT-time constant so the managed path compiles to exactly the span copy.
Risk: low — `Span.CopyTo` has the same overlap (memmove) semantics `Array.Copy` provides, which
Go's `copy` requires; the type-test fold is the pattern `ByteSeqExtensions` already uses.

**C2b — LATENT BUG found in passing (correctness, not perf): double-offset in the heterogeneous
`copy` fallback.** builtin.cs:863–864: `dst[dst.Low + i] = …src[src.Low + i]…` — but both `slice<T>`
indexers are already window-relative (they add `m_low` internally and bounds-check against
`m_length`; slice.cs:436–451), so any nonzero-`Low` operand reads/writes the wrong elements or
panics. The comment on the sibling overload (builtin.cs:807–810) states the rule this line breaks.
Reachable only when `T1 != T2` needs an `IConvertible` conversion — converted Go can't emit that
(Go's `copy` is same-type), so hand-written/interop code only, which is why no gate has caught it.
Fix is deleting both `+ …Low` terms; a GolibTests case with offset windows locks it. Should ride
with C2 since the same function is open.

**C3 — `IByteSeq` copy constructors loop element-by-element through an interface indexer.**
slice.cs:259–276 (`slice(IByteSeq<T> seq)`: N interface dispatches + N bounds checks) and
string.cs:142–152 (`@string(IByteSeq<byte> value)`: same shape). Both interfaces already expose the
span (`IByteSeq<T>.ꓸꓸꓸ`, IByteSeq.cs:39). Proposed: `AllocationCounter.CopyOf(seq.ꓸꓸꓸ)` — one
interface call, one vectorized copy (exactly what `ByteSeqExtensions.ToSlice` at IByteSeq.cs:99
already does for the constrained-generic route; these ctors are the boxed-interface route).
Effect: N virtual calls → 1; allocation count unchanged (still exactly one charged copy).
Risk: near-zero — `ꓸꓸꓸ` is window-correct on both golib implementers; a foreign implementer's
span is its own contract, same as its indexer was.

**C4 — constrained `append` over `ReadOnlySpan` materializes an array first.**
builtin.cs:1087–1094: `go.slice<T>.Append(new slice<T>(s), items.ToArray())` — one array allocation
+ copy per call purely because `slice<T>.Append` (slice.cs:1044) takes `params Span<T>` and a ROS
can't feed it. Proposed: add `slice<T>.Append(in slice<T>, params ReadOnlySpan<T>)` — the existing
body only READS `elems`, so it is the same code with the parameter re-typed — and have the `Span`
form delegate to it. Effect: deletes an allocation per constrained append (`S ~[]E` generic bodies).
Risk: low, but carries an overload-resolution check (Span converts implicitly to ROS; the corpus
must not grow a CS0121) — a CNR run plus the behavioral suite is the gate, matching the file's own
history of betterness traps (see the comment at builtin.cs:1066–1071).

**C5 — `@string → slice<rune>` goes through LINQ over an iterator; `(rune)s` allocates a whole
rune array for one rune.** string.cs:479–484: `((IEnumerable<rune>)value).ToArray()` — the
enumerator itself calls `ToRunes()` (a full decode + array) then yields it element-wise into
LINQ's growth buffers; so `[]rune(s)` pays the decode array + iterator + ToArray growth.
Proposed: `new slice<rune>(value.ToRunes())` — the freshly-decoded array is exclusively owned, so
wrapping it directly is safe. Sibling: string.cs:491–494 (`explicit operator rune`) does
`ToRunes().FirstOrDefault()`; decoding just the first rune from `Bytes` via `Rune.DecodeFromUtf8`
(the exact loop body at string.cs:415) makes the cast allocation-free. Effect: `[]rune(s)` drops
two of three allocations; `rune(s)` drops all. Risk: low — the decode logic is already centralized
and Go-exact (single-byte U+FFFD advance).

**C6 — `array<T>` range enumeration allocates; `slice<T>` already got the struct-enumerator arc.**
array.cs:425–431: `GetEnumerator()` is an iterator method returning `IEnumerator<(nint, T)>` —
every `for i, v := range arr` over a fixed array allocates the compiler state machine.
slice.cs:562–649 documents the exact same defect being fixed for `slice<T>` (two allocations per
loop → zero) with the pattern-binding rationale. Proposed: mirror it — a public struct
`Enumerator` over `(Backing, m_low, m_length)`, interface consumers keep the boxing path.
Effect: zero-alloc range over Go arrays, matching Go's cost model. Risk: low-medium — same shape as
the proven slice arc, but the named-array generated wrappers' enumeration surface needs a check
(go2cs-gen `TypeGenerator`), which may widen the A/B footprint beyond golib. Not strictly a SPAN
adoption — listed because it is the same hot-loop family and the precedent is measured.

**C7 — structural equality boxes every element; `slice<T>.Equals` copies both operands first.**
array.cs:438–498: `Equals`/`GetHashCode` route through `IStructuralEquatable` on `WindowArray` —
per-element BOXING of both sides, plus a full window copy for alias windows; a
`map<array<byte>, V>` key lookup pays 2N boxes today. slice.cs:677–705 is worse in shape:
every `Equals` overload materializes `Source` — `CopyOf(ToSpan())`, a full charged copy — for
EACH operand, per comparison. Proposed: span walk with `EqualityComparer<T>.Default` (devirtualized
for value types; nested-array recursion preserved because the element comparer lands on the
overridden `Equals(object)`), and the structural hash rewritten over the same walk. Effect: zero
allocation, no boxing, native arm unified via `ToSpan`. Risk: MODERATE — array equality is real Go
semantics (comparable values, map keys) with a live behavioral surface; hash VALUES change (legal,
but anything accidentally hash-order-sensitive would surface); and this is where the alloc-count
disclosure note below bites hardest. Second tranche, own gate.

**C8 — indexer double bounds-check: the sstring precedent applied to `@string` (and measured-only
to `slice<T>`).** string.cs:173–195 and slice.cs:411–451: explicit Go-panic bounds check, then the
CLR's array bounds check on `m_value[m_offset + index]` — the JIT cannot elide the second (the
window invariant is invisible to it). sstring.cs:83–96 solved exactly this with
`Unsafe.Add(ref MemoryMarshal.GetReference(...), index)` after the explicit check, with a measured
Native-AOT justification in the comment. Proposed: same form via `GetArrayDataReference` after the
explicit check. Effect: one bounds check per byte read — material in decode loops (utf8 walks,
strconv) and on AOT. Risk: HIGHER — removes the CLR safety net, making the window invariants
(constructor-enforced `m_low + m_length ≤ array.Length`) load-bearing for memory safety; and
slice.cs:418–427 records a measured −30% PerfSieve regression from fattening that exact indexer
body, so any change here must re-run that measurement. Own arc, perf-gated, after C1–C5.

**C9 — `Append` within-capacity fork could merge over a capacity-span helper.** slice.cs:1063–1084:
the native arm builds its span inline in an `unsafe` block; the managed arm has the single-element
store + span copy. An internal `CapacitySpan(start, len)` (the capacity window beyond `m_length`,
both backings) would merge the arms and keep the single-element fast path. Effect: purely
structural — deletes a fork, no measurable perf delta expected. Fold into whichever arc next opens
`Append`; not worth its own commit.

**Remaining native forks, dispositioned honest-no:** the per-element indexer fork
(slice.cs:423–426/446–449) is DESIGNED — the measured comment explains the NoInlining split and the
+30% cost of doing it any other way; the enumerator `Current` forks (slice.cs:617–626, 985–993) are
one perfectly-predicted branch per element on a class/struct that cannot hold a span (a class field
can't; the struct implements `IEnumerator` so it can't go `ref struct`); `Reslice` (493),
`GetHashCode` (660), `operator==` (764), `buffer` (394), `TransientAliasOf` (string.cs:125),
`AliasOfElement` (slice.cs:211) are header arithmetic or cold, with nothing for a span to merge.
These stay.

**Also dispositioned honest-no:** `@string(in slice<char>)` double-hop (string.cs:101, 131 — Go
has no char slices; interop-only), `slice<char>`/`char[]`/`IEnumerable<char>` conversions
(string.cs:496–557 — same), `PinnedBuffer.GetEnumerator` full-copy (PinnedBuffer.cs:160–167 —
cold), `widen`'s per-element loop (builtin.cs:2347–2361 — the conversion IS per-element),
`sslice.IndexOf`/`slice<T>.IndexOf` comparer loops (generic `T` without `IEquatable` can't bind
the vectorized `MemoryExtensions.IndexOf`; `Array.IndexOf` on the managed arm already
special-cases), `clear`'s GoZero arm (builtin.cs:1117 — element construction is per-element by
definition), `ToUTF8Bytes`' rune loop (encode is per-rune).

## Ranking and first tranche (estimated win × low risk)

| # | Candidate | Win | Risk | Verdict |
|---|---|---|---|---|
| 1 | **C1** copy-from-`@string` de-allocation | HIGH (per-Read alloc on `strings.Reader` et al.) | near-zero | tranche 1 |
| 2 | **C2 (+C2b)** same-type `copy` unification + the double-offset fix | MED (fork deleted, native vectorized) + correctness | low | tranche 1 |
| 3 | **C3** IByteSeq ctors → `ꓸꓸꓸ` span copy | SMALL-MED (N dispatches → 1) | near-zero | tranche 1 |
| 4 | **C4** ROS `Append` overload | SMALL (1 alloc/constrained append) | low (CS0121 check) | tranche 1 |
| 5 | **C5** `[]rune(s)` / `rune(s)` direct | SMALL-MED (2–3 allocs/conversion) | low | tranche 1 |
| 6 | C6 array struct enumerator | MED | low-med (gen surface) | tranche 2 |
| 7 | C7 structural-equality de-boxing | MED-HIGH where hit | moderate (map keys, alloc manifests) | tranche 2, own gate |
| 8 | C8 indexer double bounds-check | MED (AOT, decode loops) | higher (safety-net removal, PerfSieve lesson) | own arc |
| 9 | C9 Append capacity-span merge | ~0 (structural) | low | ride-along only |

**Suggested first tranche: C1–C5.** Five small commits, each with the harvest item's own micro-gate
(the `Perf*` row it touches — PerfString/PerfStringView for C1/C3/C5, PerfSieve control for C2 —
plus GolibTests and no regression on the native-slice branch gate). C2b lands inside C2 with a
failing-first GolibTests case. Everything in the tranche is golib-only — no converter change, so
CNR is the verifier, per the design's OQ-4 pattern.

## Process note the implementing lane must carry

Several candidates DELETE charged allocations (`AllocationCounter` sites: C1's `CopyOf` in the
implicit operator path, C4's `ToArray`, C5's iterator materializations). The Phase-4 validated
packages pin alloc-count expectations through signature-pinned disclosure manifests
(`go2cs_test_disclosures.json` — the bytes/strings precedent), so an allocation-REDUCING golib
change can invalidate a pinned disclosure in the right direction. The sweep after the tranche
should expect and re-baseline those, not read them as drift.

*Census by read-only analysis lane, 2026-08-22. No files in the repo were modified.*

---

## Tranche 1 (C1–C5) — landed, and what the perf micro-gate could and could not say

*Implementing lane G, 2026-08-23, branch `claude/span-tranche-c1c5`.*

All five items landed as separate commits, plus C2b riding inside C2 as the census directed.

**Gates:** GolibTests **276/276** (42 new guards) · CNR **byte-identical ×633** (which is what
proves the tranche really was golib-only) · behavioral suite **PASS 606** (Output 580 pass / 26
skip, 0 fail).

**Two items were bigger than the census scoped them, both discovered by writing the guard:**

- **C2b was two defects.** The double-offset was the banked finding. Writing its guard exposed a
  second one in the same arm: `TypeExtensions.ConvertToType` answers the Go representation of what
  a value *already is* — its own header says so — so the `(T1)` unbox threw `InvalidCastException`
  for every genuinely different element pair. `int` → `long` crashed rather than converting. The
  two are **inseparable for testing**: no plain-primitive pair both reaches that arm and survives
  the cast, so a guard for the offset alone would have had to be built on a wrapper type chosen to
  dodge the crash — a test written to pass. Both fixed together; all three `copy` fallbacks now
  route through one `ConvertElement<T>`.
- **C4 was solved by *narrowing*, not by adding.** The census proposed adding a `ReadOnlySpan`
  overload beside the `Span` one, flagging "the corpus must not grow a CS0121". Adding it is what
  would have *caused* that — two params-span candidates put an ambiguity in front of every
  collection-expression call site. The existing overload was **widened** instead: its body only
  ever read `elems`, a `Span` argument converts implicitly, and one span overload is strictly less
  ambiguous than two.

**The perf micro-gate: NO REGRESSION, and no demonstrable win either — the noise floor beat the
effect size.** Measured as a paired same-session A/B (tip vs `39b651997`, back to back, `--no-aot`,
5 runs), because on this laptop class an unpaired number is not evidence:

| Benchmark | base | tip | Δ |
|---|---:|---:|---:|
| String | 1,209.6 | 1,225.9 | +1.3% |
| StringView | 20.7 / 20.8 | 21.3 / 21.0 | +1.4% / +1.0% |
| StringMatch | 984.6 | 991.8 | +0.7% |
| **Sieve (control)** | 120.1 | 126.1 | **+5.0%** |

**Read the control row and the Go column before reading anything else.** Sieve touches no string or
byte-sequence path and moved *more* than every string row; and the **Go binaries moved too** —
identical Go source, `String` +6.0% and `Sieve` +17% between legs. That is host drift, and it is
several times the effect being looked for. The honest verdict is therefore: **no regression is
detectable on the nearest rows, and no improvement is demonstrable from them.**

That is not a disappointing result so much as a **mis-scoped instrument**, worth recording for
whoever gates tranche 2. These items delete *allocations and passes*, not instructions in these
benchmarks' hot loops, and the corpus witness the census itself named for C1 — `strings.Reader.Read`
paying a full-tail allocation per read — is exercised by **none** of `PerfString`,
`PerfStringView`, `PerfStringMatch` or `PerfSieve`. Where a win is claimable it is claimed by
*counting*, not timing: C3's guard asserts the conversion costs **exactly one** charged allocation,
and C1/C5 delete charged allocations outright.

**For tranche 2:** either gate allocation-reducing items by allocation COUNT (the
`AllocationCounter` pattern `ByteSeqAllocationTests` already uses, which is deterministic and
host-independent), or add a benchmark that actually walks the changed path — a `strings.NewReader`
read loop would cover C1 honestly. Timing these rows on a laptop mostly measures the laptop.

**Also carried forward, for the next Phase-4 sweep:** C1 and C5 both delete charged allocations, so
a signature-pinned alloc-count disclosure may re-baseline in the FAVORABLE direction. Expected, per
this census's own process note — not drift.
