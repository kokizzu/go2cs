# DRAFT — C# 14 span-overload exposure census: the corpus is protected by identity, not by luck (read-only, 2026-08-24)

> **DRAFT for coordinator review.** Read-only census of `C:\Projects\go2cs` at `2e9d5f549` (master).
> Nothing in the repo was modified. Scopes the item `docs/phase4/RECON-dotnet10.md` §3 row 1 names as
> "the highest-attention item of the hop" and §7 signal 1 names as the one that would DELAY it:
> C# 14 first-class span conversions changing overload resolution against `src/core/golib/`.
> All paths repo-relative; line numbers against the cited tip.
>
> ⚠ **No .NET 10 SDK is installed on this machine** (`dotnet --list-sdks` → 2.1.x, 5.0.100, **9.0.317**),
> so nothing below was compiled. Every verdict is a reading of the conversion/overload rules against
> measured repo shapes. §6 names exactly what settles the two rows that reading cannot close.

---

> ## ⚠ AMENDMENT (2026-08-24, jsval-rootcause lane) — the headline is FALSIFIED, by row 3
>
> Compiled, on the .NET 10 SDK this census could not reach. The mechanism §1(b) describes is **real
> and confirmed** (`slice<T>` → `T[]` → `Span<T>` is an implicit conversion under C# 14 and does not
> exist under C# 13 — measured both ways, same TFM and same runtime, LangVersion the only variable).
> What is wrong is the *assessment* attached to it. Row 3 calls the new conversions "**widening —
> cannot break existing code**"; **they broke `html/template`'s `TestJSValEscaper`**, silently and
> without a compile error, and that was the one FAIL of the hop's 161/1 validated sweep.
>
> **The missed shape: normal-vs-expanded form within a SINGLE `params` candidate.** This census
> searched for overload SETS whose *winner* could change, and correctly found none — every emitted
> argument shape has an identity match. But a `params` method has **two applicable forms**, and C#
> prefers the normal form whenever the argument converts to the collection type. A new conversion
> therefore does not need a second candidate to change a call's meaning: it only needs to make the
> normal form applicable where previously only the expanded form was. `jsValEscaper(a)` with
> `a` a `slice<any>` against `params ꓸꓸꓸany` (== `params Span<any>`) bound the expanded form under
> C# 13 (one element, matching Go) and the normal form under C# 14 (the slice spread), losing exactly
> one level of nesting on every row of the test table.
>
> Two specific claims to read as retracted:
> - **§5's "`variadic.cs` … a single-candidate surface, structurally immune."** Single-candidate is
>   precisely *not* immune — it is the surface that broke. Every `params Span<T>` in the tree
>   (the `Actionꓸꓸꓸ`/`Funcꓸꓸꓸ` families and every converted `...T` signature) is exposed wherever a
>   call passes one argument of a type that C# 14 newly converts to `Span<T>`.
> - **§4.3's detector claim.** A flipped pick was predicted to surface only as an *allocation*
>   regression in the disclosure arithmetic. This one surfaced as an ordinary behavioral divergence
>   in an ordinary test assert. Both detectors matter; the second is not the only one.
>
> Everything else in the census stands as written, including the finding that identity protects every
> multi-candidate set (§3, §4) — no CS0121 and no changed winner has been observed. The fix is in the
> **converter**, not golib: a slice/array of the variadic element type passed as the sole argument of
> a non-spread variadic slot is now cast to the element type, exactly as the untyped-`nil` variadic
> slot already was. See `docs/ConversionStrategies-Reference.md` (§ "An untyped constant boxed as
> `any` boxes at Go's DEFAULT TYPE") and the `VariadicSlotInterfaces` behavioral guard.

---

## Headline

**The corpus is very likely to sail through Stage 1 untouched, and for a structural reason rather than
a lucky one: at every argument shape the converter actually emits, the winning overload is an *identity*
match, and no C# 14 rule outranks identity.** The new span conversions add candidates; they do not
displace exact ones.

That is the whole finding, and it survives the three separate places it could have failed: the
`append` family (4,812 spread sites), the u8-literal operator families (79,010 literal sites), and the
generated named-type templates (7,161 conversion operators across the corpus). What C# 14 *does* change
is real and worth recording — it silently creates a family of new, **allocating** implicit conversions
out of `slice<T>`, `array<T>` and `@string` into `Span<T>`/`ReadOnlySpan<T>` — but that is a widening,
and a widening cannot break code that already compiles. It becomes a hazard only for code written
*after* the hop, and for two shapes that are not in this tree today.

---

## 1. The mechanism, stated precisely (so the rest can be read against it)

Three C# 14 changes matter here. The first two are the engine; the third is the documented headline.

**(a) Array→span and string→span become *standard* implicit conversions.** In C# 13, `T[]` →
`Span<T>`/`ReadOnlySpan<T>` was a *user-defined* operator (`op_Implicit` on the span types) and
`string` → `ReadOnlySpan<char>` did not exist implicitly at all. In C# 14 they are built-in
conversions in the same class as implicit reference conversions.

**(b) Consequence — user-defined conversions can now CHAIN through them.** A user-defined implicit
conversion permits one *standard* conversion before the operator and one after. So any golib type
carrying `implicit operator T[]` gains an implicit conversion to `Span<T>`/`ReadOnlySpan<T>` that it
did not have in C# 13 (where the second hop would have been a second user-defined conversion, which
C# never composes). Measured instances:

| Type | Operator that makes it reachable | New in C# 14 | Cost of the new route |
|:--|:--|:--|:--|
| `slice<T>` | `slice.cs:737` `implicit operator T[](slice<T>)` → `value.ToArray()` | `slice<T>` → `Span<T>`, `ReadOnlySpan<T>` | **full array copy** |
| `array<T>` | `array.cs:528` `implicit operator T[](array<T>)` | `array<T>` → `Span<T>`, `ReadOnlySpan<T>` | **full array copy** |
| `@string` | `string.cs:528` `implicit operator byte[](@string)` → `AllocationCounter.CopyOf(value.Bytes)` | `@string` → `Span<byte>`, `ReadOnlySpan<byte>` | **charged copy** |
| `@string` | `string.cs:444` `implicit operator string(@string)` | `@string` → `ReadOnlySpan<char>` | `ToString()` |
| `sstring` | `sstring.cs:164` `implicit operator string(sstring)` | `sstring` → `ReadOnlySpan<char>` | — |

The second-order effect is the one that can actually break something: `Span<T>` → `slice<T>` already
existed (`slice.cs:717`), and `ReadOnlySpan<T>` → `slice<T>` too (`slice.cs:722`). With the reverse
direction now existing as well, **`slice<T>` and `Span<T>` become mutually convertible, which destroys
the "better conversion target" ordering between them.** Same for `array<T>`↔span and `@string`↔`ROS<byte>`
(`string.cs:449`). Any overload set offering both, reached by an argument that matches *neither*
exactly, goes from "span wins" to CS0121. That is the new-ambiguity engine, and §3 is the search for a
set it can fire in.

**(c) Extension-method receivers and generic inference.** The documented .NET break
([csharp-overload-resolution](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution))
centres on span overloads becoming applicable where an `IEnumerable<T>`/interface overload previously
took the call — classically `MemoryExtensions` displacing `Enumerable` on an array receiver. Censused
in §4; the shape is essentially absent from this tree.

**Load-bearing assumption, stated so it can be falsified:** (b) depends on implicit span conversions
being members of the *standard implicit conversion* set (which is what lets them serve as the pre/post
hop of a user-defined conversion). If C# 14 instead excluded them from that set, rows 1–2 of the risk
table evaporate and this census gets *safer*, not riskier. The verdict direction does not depend on it.

---

## 2. Inventory — golib's user-defined conversion operators

`grep -E "(implicit|explicit) operator"` over `src/core/golib/**` returns 200 lines, of which **198 are
declarations** (2 are comments at `string.cs:639` and `sstring.cs:339`). Only the array/span/Memory-adjacent
ones can participate; the numeric families (`UntypedInt`, `UntypedFloat`, `UntypedComplex`, `complex64`,
`uintptr` — 118 of the 198) and the `NilType` operators are inert here.

| Type | File:line | Conversions **from** | Conversions **to** |
|:--|:--|:--|:--|
| `array<T>` | array.cs:503–528, 586 | `T[]`, `Span<T>`, `ROS<T>`, `Memory<T>`, `ROMemory<T>`, `NilType` | `T[]` |
| `slice<T>` | slice.cs:712–748, 823 | `T[]`, `Span<T>`, `ROS<T>`, `Memory<T>`, `ROMemory<T>`, `array<T>`, `NilType` | `T[]`, `array<T>` |
| `@string` | string.cs:439–601 | `string`, `ROS<byte>`, `slice<byte>`, `slice<rune>`, `rune`, `slice<char>`, `byte[]`, `rune[]`, `ROS<rune>`, `char[]`, `NilType` | `string`, `slice<byte>`, `slice<rune>`, `slice<char>`, `byte[]`, `rune[]`, `ROS<rune>`, `rune`(exp), `char[]`(exp) |
| `sstring` | sstring.cs:159–218 | `string`, `@string`, `in slice<byte>`, `byte[]`, `ROS<byte>`(exp), `NilType` | `string`, `@string`, `slice<byte>`, `ROS<byte>`(exp), `byte[]`(exp) |
| `sslice<T>` | sslice.cs:170–180 | `Span<T>` | `Span<T>`, `slice<T>` |
| `PinnedBuffer` | PinnedBuffer.cs:179–194 | `byte[]`, `@string` | `Span<byte>`, `byte*` |
| `map`/`channel`/`ж`/`error`/`EmptyStruct` | map.cs:415–484, channel.cs:1654, ж.cs:1228–1315, error.cs:186–191, EmptyStruct.cs:37 | `Dictionary`, `NilType`, `uintptr`, `void*`, `ж<T>`, `T` | `Dictionary`, `uintptr`, `void*` | 

**Corpus side (generated, non-golib): 7,161 conversion-operator declarations**, essentially all minted by
two `go2cs-gen` templates and therefore uniform:

- **Named-string wrappers** (`InheritedTypeTemplate.cs:207,209,251,260–264`): `W ↔ @string`, plus
  `implicit operator W(ReadOnlySpan<byte>)` and the four `==`/`!=` operators against `ROS<byte>` (the u8
  bridge). **No array-typed operator**, so the new span conversion has no competitor to be ambiguous with.
- **Named-slice / other named wrappers** (`InheritedTypeTemplate.cs:207,209`): `W ↔ slice<T>` only.
  `W → Span<T>` would need `W→slice<T>` (UD) then `slice<T>→Span<T>` (UD) — **two user-defined
  conversions, which C# still never composes.** The wrapper layer is insulated by construction.

---

## 3. Overload sets at risk — the nine that mix a span parameter with an array-ish one

A mechanical scan of every declaration in `src/core/golib/**` grouped by member name, keeping sets that
contain at least one `Span`/`ReadOnlySpan` parameter **and** at least one `T[]`/`slice`/`array`/`@string`/
`string`/`Memory`/interface parameter, yields exactly **nine sets**. Each is assessed against the argument
shapes the converter actually emits.

### 3.1 `builtin.append` — 7 overloads · **UNAFFECTED**

`builtin.cs:292` `(in slice<T>, params T[])` · `:314` `(ISlice, params T[])` · `:336` `(slice<T>, params T[])` ·
`:358` `(slice<T>, params Span<T>)` · `:379` `(slice<byte>, params ROS<rune>)` · `:1134`
`(S, params Span<T>)` · `:1149` `(S, params ROS<T>)`.

Two argument shapes dominate the corpus, and both land on an identity match:

- **Spread** — `append(s, other.ꓸꓸꓸ)`, **4,812 sites**. `ꓸꓸꓸ` is `Span<T>` (`slice.cs:391`,
  `array.cs:273`), so `:358` matches in normal form by identity. C# 14 makes `Span<T>`→`ROS<T>` standard
  rather than user-defined, promoting `:1149`'s conversion — but it remains a conversion against an
  identity, and identity is unbeatable.
- **u8 literal** — `append(bs, "…"u8)`. The argument is `ReadOnlySpan<byte>`; `:1149` matches by identity
  (S=`slice<byte>`, T=`byte`). Nothing converts `ROS<byte>` to `Span<byte>` or to `byte[]` in either
  language version, so no candidate is added or removed.

The `params T[]` overloads (`:292/:314/:336`) cannot be reached in *expanded* form by a span argument in
either version — the type argument would be a ref struct (CS9244), which the file's own comment at
`builtin.cs:1128–1133` already records as the reason `:1134` exists.

### 3.2 `slice<T>.Append` — `params ReadOnlySpan<T>` beside `params T[]` · **UNAFFECTED**

`slice.cs:1052` and `slice.cs:1108`. This is the textbook new-ambiguity shape and it is already
dispositioned in-tree: the C4 item of `docs/phase4/CENSUS-span-unification.md` proposed *adding* a ROS
overload beside the Span one, and the implementing lane **narrowed instead of adding** — the comment at
`slice.cs:1046–1051` states it plainly ("two params-span overloads would put an ambiguity (CS0121) in
front of every collection-expression call site"). The surviving pair is one span overload plus one array
overload: a `T[]` argument matches `:1108` by identity in normal form, and in expanded form the C# 12
collection-expression betterness that orders span above array is untouched by C# 14. Nothing to do.

### 3.3 `@string` constructors — 13 overloads · **UNAFFECTED AS EMITTED**

`string.cs:71,78,85,92,101,103,105,131,133,137,142,152,162`. This is the widest set and the one where
the new mutual convertibility could fire, because it offers `in ReadOnlySpan<byte>` (`:92`) *and*
`in slice<byte>` (`:105`) in the same position. Assessment by argument type:

| Argument | C# 13 winner | C# 14 |
|:--|:--|:--|
| `@string` | `:162` identity | unchanged |
| `slice<byte>` / `slice<rune>` / `slice<char>` | `:105` / `:133` / `:131` identity | unchanged |
| `byte[]` / `char[]` / `string` | `:85` / `:101` / `:152` identity | unchanged |
| `ReadOnlySpan<byte>` / `ReadOnlySpan<rune>` | `:92` / `:103` identity | unchanged |
| **`Span<byte>`** | `:92`, because `ROS<byte>` was the better target (`ROS<byte>`→`slice<byte>` existed, the reverse did not) | 🔴 **would become CS0121** — the reverse now exists, so neither target is better |

The `Span<byte>` row is the one genuine predicted break in the whole census, and **no call site of that
shape exists**: `grep "new @string(" src/core` outside golib returns 145 sites, **none** with a
`ToSpan()`/`AsSpan()`/`Span<`-typed argument. Inside golib the two constructors that would hit it dodge
it for independent reasons — `string.cs:133` passes `value.ToSpan()` through a `: this(...)` initializer,
which excludes the containing constructor from the candidate set, and `string.cs:137` already writes an
**explicit** `(ReadOnlySpan<byte>)` cast on `value.ToSpan()`. (Read that explicit cast as prior art: the
ambiguity it disambiguates is the same one C# 14 would generalize.)

### 3.4 `slice<T>` constructors — 14 overloads · **UNAFFECTED**

`slice.cs:88,114,120,136,144,152,160,168,227,259,290,320,324,348,382`. Every emitted shape has an
identity ctor (`T[]`→`:120`, `Span<T>`→`:136`, `ROS<T>`→`:144`, `array<T>`→`:168`, `Memory`→`:152/:160`).
`:120` vs `:290` (which adds two optional parameters) is resolved by the pre-existing "fewer omitted
optionals" tie-break, unchanged by C# 14. The new `slice<T>`→`Span<T>` conversion adds candidates behind
identities and never wins.

### 3.5 `array<T>` constructors / `array()` extensions — 19 declarations · **UNAFFECTED**

`array.cs:58–245` (ctors) and `array.cs:683,699,713,722,728` (the `.array()` extension family over
`T[]`, `IEnumerable<T>`, `Span<T>` receivers). Constructors: identity everywhere. Extensions: extension
*receiver* conversions never admit user-defined conversions in either version, so `slice<T>`/`array<T>`/
`@string` receivers can only reach their own identity overloads; and every array receiver has `:683/:699`
as an identity match, which outranks any newly-admissible span receiver.

### 3.6 `slice()` extension family — 6 receivers · **UNAFFECTED**

`slice.cs:1155` (`this in slice<T>`), `:1162` (`this T[]`), `:1180` (`this Span<T>`), `:1186`
(`this IEnumerable<T>`), `:1201` (`this array<T>`), `:1220` (`this @string`). This is the closest thing
in the tree to the documented `Enumerable`-vs-`MemoryExtensions` break: an interface receiver sitting
beside a span receiver. It is safe because **every receiver type the converter emits has its own identity
overload in the family** — there is no receiver that must fall through to `IEnumerable<T>`. A BCL `string`
receiver would fall through to `:1186` with `T=char`, but `string`→`Span<char>` does not exist (only
`ReadOnlySpan<char>`, and the family has no `this ReadOnlySpan<T>` overload), so even that lane is closed.

### 3.7 `builtin.len` — 20 overloads · **UNAFFECTED**

`builtin.cs:1404–1610`. Offers `in ReadOnlySpan<byte>` (`:1525`) alongside `T[]` (`:1414`), `in slice<T>`
(`:1444`), `in array<T>` (`:1404`), `@string` (`:1505`), `in sstring` (`:1515`), `string` (`:1550`),
`ISlice` (`:1464`), `IArray` (`:1424`). Identity is present for every one of those argument types, so the
new conversions land strictly behind exact matches.

### 3.8 `sstring` constructors — 6 overloads · **UNAFFECTED AS EMITTED**

`sstring.cs:52,58,63,70,74,79`. Same structure and same single soft spot as §3.3: `ROS<byte>` (`:58`)
beside `in slice<byte>` (`:70`). A `Span<byte>` argument resolved to `:58` in C# 13 by target betterness
and would tie in C# 14. No such call site found. Note `sstring`'s span operators (`:185`, `:190`) are
**explicit**, which keeps it out of the implicit chaining in §1(b) entirely.

### 3.9 `CopyTo` / `Enumerator` · **NOT REAL SETS**

`array.cs:342`, `slice.cs:529`, `sslice.cs:133,138`, `SparseArray.cs:57` — these are members of
*different declaring types*, so they never compete in one overload resolution. Listed for completeness
because the name-grouped scan surfaces them.

---

## 4. Call sites in the corpus — what would actually exercise a changed pick

Since the corpus is generated, a changed pick shows up as behavior, not as a compile error, so the
traffic matters as much as the shapes. Measured counts over `src/core` excluding golib:

| Emitted shape | Count | Binds | C# 14 |
|:--|--:|:--|:--|
| u8 literals `"…"u8` (→ `ReadOnlySpan<byte>`) | **79,010** | exact-match operator families | unaffected — see below |
| spread `.ꓸꓸꓸ` (→ `Span<T>`) | **4,812** | `append` `:358` / `:1134` by identity | unaffected (§3.1) |
| `copy(` call sites | **777** | `builtin.cs:735–1055`, no span parameter in any position | unaffected |
| `append(…, IDENT)` | 872 | `:336`/`:358` per argument type | unaffected |
| `new @string(` | 145 | identity ctor per argument type; **zero** span-typed arguments | unaffected (§3.3) |
| collection expressions `= [ …` | 81 | see §5 | low |
| `.SequenceEqual(` / `.Reverse(` on any receiver | 2 | — | see §4.2 |

### 4.1 The u8-literal operator families are safe by exact match

`string.cs:647–733` declares `==`, `!=`, `<`, `<=`, `>`, `>=` and `+` for `(@string, ReadOnlySpan<byte>)`
in **both operand orders**; `sstring.cs:278–459` mirrors it; the generated named-string wrappers get the
same four comparison operators (`InheritedTypeTemplate.cs:260–264`). For `s == "…"u8` the operator
`==(@string, ROS<byte>)` is exact on both parameters. C# 14 does add a candidate here — the reversed
`==(ROS<byte>, @string)` becomes applicable, because `@string`→`ROS<byte>` now exists per §1(b) — but it
requires a user-defined conversion on *both* parameters and loses to the doubly-exact one. The single
highest-traffic C# 14-adjacent surface in the repo is therefore untouched.

This also means the comments at `string.cs:642–643` and `InheritedTypeTemplate.cs:257–258` ("exact match
beats the user-defined conversion") remain true statements after the hop, not stale ones.

### 4.2 The documented `Enumerable`-vs-`MemoryExtensions` break has no purchase here

Every `SequenceEqual` call in golib is already made on a receiver that *is* a span — `@string.Bytes`
(`string.cs:66`, a `ReadOnlySpan<byte>`), `sstring.m_value`, or `ToSpan()`: `string.cs:286,649,654,659,664`,
`sstring.cs:133,280,285,290,295,347,352,357,362`. Every `Contains` is either an explicit interface-qualified
call (`array.cs:652`, `slice.cs:913`, `map.cs:529,544`) or on a `List`/`string` receiver where an instance
method wins regardless (`channel.cs:979`, `GoReflect.FieldAccess.cs:352`). Across the whole generated
corpus the two names appear **twice**. In the 59 hand-owned `*_impl.cs` files, the only instances are
`reflect/deepequal_impl.cs:105` (`b1.ToSpan().SequenceEqual(b2.ToSpan())` — already spans) and
`:177` (`m2.Contains(entry.Key)` on a Go map view).

golib spells its spans; it does not lean on implicit array-receiver extension binding. That habit is what
retires this row.

### 4.3 Where a flipped pick *would* show up, if one existed

Worth carrying into the Stage-1 record, because the detector is not the obvious one: every new span
route created by §1(b) runs through an **allocating** operator (`ToArray()` / `AllocationCounter.CopyOf`).
So a silently-flipped pick would be an *allocation* regression, not an output divergence — invisible to
the behavioral suite's byte goldens and stdout comparisons, but visible to the signature-pinned
alloc-count disclosure manifests (`go2cs_test_disclosures.json`) that `run-validated-sweep.ps1` already
arithmetics. RECON §6 prediction 3 already tells the coordinator to expect disclosure movement in the
*favorable* direction from .NET 10 escape analysis; an **unfavorable** move on a bytes/strings-class entry
would be this census's row 2 firing, and should be triaged as an overload-resolution finding rather than
a runtime one.

---

## 5. `params` arrays and collection expressions

Both `params` families that could carry the classic new-ambiguity shape are already resolved in-tree:

- `slice<T>.Append` — one span overload beside one array overload, deliberately (§3.2, `slice.cs:1046–1051`).
- `builtin.append` — the `params T[]` legacy overloads and the `params Span<T>`/`params ReadOnlySpan<T>`
  overloads coexist, and the comment at `builtin.cs:1128–1133` documents the exact betterness reasoning
  that keeps them apart (CS9244 forecloses the ref-struct expansion of the array candidates).
- Remaining `params T[]` in golib are `object[]`/`string[]`/`long[]`/`SelectOp[]` attribute and
  diagnostic surfaces with no span sibling (`builtin.cs:569,588,1880,1894,1903`, `GoArrayDimsAttribute.cs:64`,
  `GoMapKeyDimsAttribute.cs:31`, `GoValueCloneAttribute.cs:29`, `GoInterfaceShellAttribute.cs:56`,
  `runtime/HashCode.cs:25`). Inert.
- `variadic.cs` declares 18 delegate families with `params Span<TArg>` tails and **no array twin at all**
  — a single-candidate surface, structurally immune.

Collection expressions: 81 sites in the generated corpus, ~16 in golib. The one that targets an
at-risk set is `string.cs:493` — `new @string([value])` inside `implicit operator @string(rune)`. It
binds `:103` `@string(in ReadOnlySpan<rune>)`, since the element type `rune` has no implicit conversion
to `byte` or `char` and the `slice<…>` constructors are not valid collection-expression targets. C# 14
does not change collection-expression conversions or their betterness rules, so this is stable; noted
because it is the only collection expression in the tree pointed at a span-vs-array constructor set.

---

## 6. What I cannot settle without compiling, and exactly what would settle it

Two rows are readings, not measurements:

1. **Whether implicit span conversions are members of the *standard implicit conversion* set** — the
   assumption §1(b) rests on. If yes, `slice<T>`→`Span<T>` etc. exist and §3.3/§3.8's `Span<byte>` rows
   are genuine (but unreached) ambiguities. If no, those rows disappear.
2. **Whether C# 14 added a betterness tie-break that ranks an implicit span conversion above a
   user-defined one.** This decides what happens in a set that offers both a span parameter and a
   `slice`/`array`/`@string` parameter and is reached by an argument matching neither exactly. I found
   **no such call site**, so it is unreached either way — but it governs whether such a site, if one is
   hiding behind a generic instantiation I did not enumerate, becomes CS0121 or silently flips.

**Cheapest thing that settles both, and it is already scheduled:** Stage 1 itself. golib pins
`<LangVersion>latest</LangVersion>` (`golib.csproj:37`) exactly as the converted projects do, so
`dotnet build src/core/golib/golib.csproj` under the .NET 10 SDK compiles golib as C# 14 in ~seconds
and is a complete test of every set in §3 that golib's own code exercises. If a targeted answer is
wanted *before* the corpus build, a ~20-line probe against the real golib assembly closes both questions:

```csharp
// probe.cs — build against golib with LangVersion=latest under SDK 10
using go;
static class Probe {
    static void ConversionExists(slice<byte> s, array<byte> a, @string t) {
        Span<byte>         s1 = s;   // C#13: error. C#14: OK ⇒ question 1 answered "yes"
        ReadOnlySpan<byte> s2 = t;   // C#13: error. C#14: OK ⇒ @string→ROS<byte> exists
        ReadOnlySpan<byte> s3 = a;   // C#13: error. C#14: OK
        _ = (s1.Length, s2.Length, s3.Length);
    }
    static void M(slice<byte> x) { }            // the two-candidate set §1(b) creates
    static void M(ReadOnlySpan<byte> x) { }
    static void Betterness(byte[] b, array<byte> a) {
        M(b);                        // C#13 picked ROS. C#14: same, or CS0121 ⇒ question 2 answered
        M(a);                        // C#13 picked slice<byte>. C#14: same, flip, or CS0121
    }
    static void Ctor(Span<byte> sp) {
        _ = new @string(sp);         // §3.3's predicted CS0121, in isolation
    }
}
```

Not censused, and honestly out of scope for a golib census: **BCL overload sets** reached from golib and
from the 59 hand-owned `*_impl.cs` files. The BCL is full of `M(T[])`/`M(ReadOnlySpan<T>)` pairs, and
identity protects every one of them that is called with an array or a string — but I did not enumerate
calls that pass a *golib* type (which now converts to a span) into a BCL set. The compile settles it, and
it is cheap: 8 of the 59 impl files touch spans at all (`internal/chacha8rand`, `math/rand`, `math/rand/v2`,
`net/dnsclient`, `os/tempfile`, `reflect/value`, `runtime/managed`, `syscall/linux/sockaddr_linux`).

---

## 7. Risk-ranked table

| # | Site / shape | Evidence | Assessment |
|:--|:--|:--|:--|
| 1 | `new @string(<Span<byte>>)` — `ROS<byte>` ctor beside `slice<byte>` ctor | string.cs:92 vs :105 | 🟠 **newly-ambiguous IF reached — 0 call sites found** (145 `new @string(` sites, none span-typed; golib's two near-misses dodge via `: this(…)` exclusion and an explicit cast at string.cs:137) |
| 2 | `new sstring(<Span<byte>>)` — same shape | sstring.cs:58 vs :70 | 🟠 same, **0 call sites found** |
| 3 | New allocating conversions `slice<T>`/`array<T>`/`@string` → `Span`/`ROS` | slice.cs:737, array.cs:528, string.cs:528/444 | 🔴 **FIRED — see the amendment above.** Assessed here as "widening — cannot break existing code"; that is false for a `params` parameter, where a new conversion flips the call from the EXPANDED form to the preferred NORMAL form with no second candidate and no compile error. Broke `html/template` `TestJSValEscaper`; fixed in the converter (`variadicArgBindsParamsCollection`). Still also a future-hazard footgun — every route copies |
| 4 | `builtin.append` 7-overload family | builtin.cs:292–379, 1134, 1149 | 🟢 **unaffected** — identity at `:358` for 4,812 spread sites, at `:1149` for u8 sites |
| 5 | u8-literal operator families (`@string`, `sstring`, named-string wrappers) | string.cs:647–733; sstring.cs:278–459; template :260–264 | 🟢 **unaffected** — doubly-exact operators outrank the one new candidate; 79,010 literal sites |
| 6 | `slice<T>.Append` `params ROS<T>` beside `params T[]` | slice.cs:1052, :1108 | 🟢 **unaffected** — the C4 disposition already chose the one-span-overload shape for this reason |
| 7 | `slice<T>` / `array<T>` / `len` / `slice()` / `array()` sets | slice.cs:114–382, 1155–1220; array.cs:58–245, 683–728; builtin.cs:1404–1610 | 🟢 **unaffected** — identity candidate present for every emitted argument type |
| 8 | Generated named-string wrappers (`W ← ROS<byte>`) | InheritedTypeTemplate.cs:251 | 🟢 **widening only** — single route, no competing array operator |
| 9 | Generated named-slice wrappers | InheritedTypeTemplate.cs:207,209 | 🟢 **unaffected** — double-user-defined barrier is not lifted by C# 14 |
| 10 | Extension-receiver flip (`Enumerable` → `MemoryExtensions`) | golib receivers already spans; 2 corpus sites; 2 impl sites | 🟢 **unaffected** — the shape is essentially absent |
| 11 | Collection-expression target `new @string([value])` | string.cs:493 | 🟢 low — binds `ROS<rune>` ctor; C# 14 leaves collection-expression rules alone |
| 12 | BCL overload sets reached with golib types | not enumerated | ⚪ **UNKNOWN — the Stage-1 compile settles it** (§6) |
| 13 | Hand-owned `*_impl.cs` (59 files, 8 span-touching) | listed §6 | ⚪ **UNKNOWN, low volume** — the compile settles it |

---

## 8. Bottom line

**The corpus should sail through Stage 1 on this axis, and the reason is structural rather than
fortunate.** C# 14's first-class span conversions add candidates to overload sets; they never outrank an
identity match; and golib is written such that every argument shape the converter emits — the 4,812
`Span<T>` spreads, the 79,010 `ReadOnlySpan<byte>` u8 literals, the 777 `copy` sites, the 145 `@string`
constructions — has an exactly-matching overload waiting for it. I found **zero** call sites that would
newly go ambiguous and **zero** where the pick would silently change. The two shapes that *would* break
(`new @string(<Span<byte>>)`, `new sstring(<Span<byte>>)`) are real predictions with real line numbers
and no callers; golib's own two near-misses already carry the disambiguation, one by a `: this(…)`
exclusion and one by an explicit cast a previous author evidently had to write.

Three caveats keep this from being a clean bill of health. First, **it is a reading, not a compile** —
no .NET 10 SDK exists on this machine, and §6 names the two rule questions the reading rests on plus a
20-line probe that closes both in seconds on the build machine. Second, **BCL overload sets reached with
golib types were not enumerated**; that is the one flank a golib-scoped census structurally cannot cover,
and it is also the flank the Stage-1 build covers for free. Third, and worth putting in the Stage-1
record rather than discovering later: C# 14 quietly creates a family of new implicit conversions out of
`slice<T>`, `array<T>` and `@string` into spans, **every one of which allocates a copy**. Nothing binds
them today. If something ever does, it will not show up as a failing golden or a diverging stdout — it
will show up as an allocation-count disclosure moving the wrong way, which makes the sweep's disclosure
arithmetic the second detector for this class and worth reading with that in mind.

If Stage 1 comes back clean on span overload resolution, that is the predicted result and it can be
recorded as such rather than as luck. If it comes back with a CS0121, the first two places to look are
`string.cs:92` vs `:105` and `sstring.cs:58` vs `:70`, and the fix in both cases is one explicit cast at
the call site — not a golib redesign.

*Census by read-only analysis lane, 2026-08-24. No files in the repo were modified. Compiled: nothing —
see §6.*
