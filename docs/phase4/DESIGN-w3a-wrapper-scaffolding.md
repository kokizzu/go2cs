# W3a-residual — the wrapper's own scaffolding vs. its wrapped type

**Status:** DESIGN ONLY — no code from this document is committed. Two experiments below were run
and reverted; neither is banked.
**Lane:** runtime walls campaign, i9 fleet lane
**Date:** 2026-08-30
**Spec:** [`docs/phase4/CENSUS-runtime-first-contact.md`](docs/phase4/CENSUS-runtime-first-contact.md) §W3;
[`docs/phase4/MAILBOX.md`](docs/phase4/MAILBOX.md) 2026-08-30 i9→COORD (the 39-error report) and the
COORD ruling that asked for this document
**Tree read:** `claude/i9-w3-accessibility` @ `2b08b924f`; toolchain go1.23.12 / .NET SDK 10.0.400

---

## 0. Headline

> **None of the census's occupants are Go type aliases, so the alias route this document was asked
> to weigh is not incomplete — it is wrong for every one of them.** All nine wrapper types
> `runtime`'s `export_test.go` declares over an unexported production struct use Go's **defined-type**
> syntax (`type MSpan mspan`, no `=`), never the alias syntax (`type MSpan = mspan`). A defined type
> is a distinct Go type: conversion to and from its underlying is **explicit**, and it can carry its
> own methods. A `global using MSpan = go.runtime_package.mspan;` would make `MSpan`/`mspan`
> **silently interchangeable** in C# — strictly more permissive than Go's own type system — for
> every one of them. The wrapper struct is the correct representation; the wall is a pure
> accessibility bug in the wrapper's OWN scaffolding, not an architectural mismatch, and it is
> narrower than the earlier cascade made it look: **downgrading the whole wrapper cascades (32→70,
> measured); downgrading only its constructor and `.Value` does not (39→12, measured, zero new
> errors). The remaining 12 are a distinct, C#-language-forced case — `implicit operator` cannot be
> non-public at all — with its own clean answer: omit it, don't weaken it.**

---

## 1. The census — every occupant, and why "pure alias" does not apply

`grep -nE "^type [A-Z][a-zA-Z0-9]* [a-z]"` over `runtime/export_test.go` (the complete set of
capitalized defined-types-over-a-lowercase-name pattern) plus a per-type method count:

| Wrapper | Underlying (production, unexported) | Methods on the wrapper | Go declaration |
|---|---|---:|---|
| `LockRank` | `lockRank` | 1 | `type LockRank lockRank` |
| `ProfBuf` | `profBuf` | 3 | `type ProfBuf profBuf` |
| `PallocSum` | `pallocSum` | 3 | `type PallocSum pallocSum` |
| `PallocBits` | `pallocBits` | 5 | `type PallocBits pallocBits` |
| `PallocData` | `pallocData` | 5 | `type PallocData pallocData` |
| `PageCache` | `pageCache` | 6 | `type PageCache pageCache` |
| `ChunkIdx` | `chunkIdx` | 0 | `type ChunkIdx chunkIdx` |
| `PageAlloc` | `pageAlloc` | 7 | `type PageAlloc pageAlloc` |
| `MSpan` | `mspan` | 0 | `type MSpan mspan` |
| `TimeHistogram` | `timeHistogram` | 2 | `type TimeHistogram timeHistogram` |

**Every row is a defined type. Zero are aliases.** (`AddrRanges`, `ScavengeIndex` and others the W3a
report also named are fresh `struct{...}` declarations with no wrapped production type at all — no
accessibility question, out of scope here.) Only `ChunkIdx` and `MSpan` carry zero of their own
methods; the other eight exist specifically to host real method sets Go declares directly on the
defined type — exactly the capability a C# `global using` (a pure name substitution, no new type, no
room for a method) cannot provide. The wrapper struct — `[GoType(...)] partial struct MSpan;` plus
`InheritedTypeTemplate`'s generated body — is architecturally the right choice for all ten; **option
(a) from the ruling (a true alias emission) is retired as a candidate, not merely narrowed.**

---

## 2. The mechanism, stated generally

A test-file-declared defined type over an unexported production struct is, correctly, emitted
**public** — its Go name is exported, exactly as `export_test.go`'s author intended for external test
consumption (every consumer is a sibling file in the same test assembly under the whitebox-reference
model, so `internal` would be just as reachable, but the CONVERTER's existing name-cased rule has no
reason to know that here and this document does not ask it to learn it — see §3).

The wrapper's **generated body**, though, is not one thing — it is several members, and each names the
WRAPPED type (the unexported production struct) with a different degree of necessity:

1. **The constructor** `public MSpan(mspan value) => m_value = value;` and **`.Value`**
   `public mspan Value => m_value;` — the wrapper's own storage accessors. Needed by the wrapper
   ITSELF (nothing external requires them to be public — every actual consumer reaches `MSpan`
   through some OTHER exported function's own return, e.g. `AllocMSpan() -> MSpan`, never by
   constructing one from a raw `mspan` directly).
2. **The forwarded field/property accessors** (`ForwardedMembers`) — already fixed in the banked W3a
   commit, per-field, using the identical technique this document extends one level up.
3. **The paired `implicit operator` conversions** to and from the wrapped type — C#'s own conversion
   surface for the two types, and the one member kind that **cannot** be downgraded at all (§4).

None of these three needs the WRAPPER STRUCT's own declared scope to change. Each is a narrower
question: does THIS member's OWN signature touch something the wrapper's public declaration does not
already cover?

---

## 3. The options, costed — with measured evidence, not estimate

| # | Option | Measured result | Verdict |
|---|---|---|---|
| **(a)** | True `global using` alias, dissolving the wrapper for pure aliases | N/A — zero occupants are aliases (§1) | **Retired.** Not incomplete; wrong for this census. |
| **(b1)** | Downgrade the WRAPPER STRUCT'S OWN declared `Scope` to internal when its underlying is unexported | **Cascades**: every OTHER test-declared function/method naming the wrapper as a parameter or return type computes ITS OWN accessibility independently (name-cased), so it stays "public" while the type it names just became internal — 32 errors → **70** (measured, reverted). `AllocMSpan`, `FreeMSpan`, `MSpanCountAlloc` and every sibling consumer breaks. | **Rejected.** Fixing it this way requires propagating the downgrade through every consumer's own signature too — a corpus-wide ripple this task was not scoped to chase, and probably wrong anyway: nothing about `AllocMSpan`'s OWN exported-ness should depend on what `MSpan` happens to wrap. |
| **(b2)** | Keep the wrapper struct's own `Scope` untouched; downgrade only the constructor and `.Value` (the members that name the wrapped type DIRECTLY) when the wrapped type is not itself effectively public | **39 → 12 errors, zero new errors of any kind** (measured, reverted — see §5 for why it is not yet banked). The narrowest-wins rule `ForwardedMembers` already applies per forwarded field, one level up: to the wrapper's OWN storage accessors. | **RECOMMENDED**, combined with (c) below for the residual 12. |
| **(c)** | For the paired `implicit operator` conversions specifically: **omit them** (not weaken them) under the same condition | Required, not optional — see §4. C# refuses a non-public conversion operator outright (CS0558); "internal" is not an available modifier for these two members at all. | **RECOMMENDED**, folded into (b2)'s gate. |

**(b2)+(c) is the recommendation.** It costs nothing Go promised (§4) and, per the (b1) experiment, the
blast radius is genuinely local — nothing outside the wrapper's own four scaffolding members (ctor,
`.Value`, two operators) ever references the wrapped type directly; everything else names the WRAPPER,
which never changes scope.

---

## 4. Why omitting the operators is the Go-faithful choice, not a compromise

`type MSpan mspan` is a **defined type**, so Go itself requires an **explicit** conversion between
`MSpan` and `mspan` (`MSpan(m)` / `mspan(ms)`) — never an implicit one. The converter's existing
`UnderlyingConversionOperators` already emits C# `implicit operator` for this pair as an ergonomic
liberality beyond what Go grants. Losing that liberality specifically for the cases where it cannot be
made accessibility-consistent anyway (a non-public conversion operator is not legal C#, full stop)
costs nothing the Go source promised: the EXPLICIT path — the constructor and `.Value`, now correctly
`internal` — remains fully available to every consumer, all of which are sibling files in the same
test assembly. No Go program that relies on `MSpan`↔`mspan` conversion stops working; it simply stops
being spelled with an implicit cast, which Go never allowed in the first place.

---

## 5. Why the (b2) experiment is not banked as-is — the scoping finding

The same experiment that measured 39→12 also measured a **second, unscoped regression**: wiring the
"is the wrapped type public" check into `TypeGenerator`'s one shared construction site for
defined-type-over-struct wrappers — the path EVERY such wrapper in the corpus takes, test-declared or
production — downgraded ordinary PRODUCTION wrappers too. `unsafe_package.ArbitraryType`,
`internal/goarch`'s `ArchFamilyType`, and others newly failed CS0558 on their (previously fine)
conversion operators, because their own underlying structs read as "not public" under the SAME check
with no test-file gate to stop it.

This is the same lesson `testMethodAccessDowngrade` (W3a, banked) already encodes for methods: **the
downgrade must be gated to a TEST-file-declared wrapper specifically**, mirroring
`v.isTestFileDecl(...)` on the Go side and an equivalent syntax-position check on the go2cs-gen side
(the wrapper's own `[GoType(...)]` declaration site is known to the converter at emission time — the
gate belongs there, stamped into the emitted declaration for the generator to read, the same
`GetExplicitAccessModifier`-reads-what-the-converter-decided pattern `RecvGenerator` already uses).
Implementing that gate correctly is real, scoped work — not large, but real — which is why this stays
a design, not a same-turn cut.

---

## 6. Gate plan

| # | Gate | What it proves |
|---|---|---|
| **G1** | Converter `go test ./...` and a new go2cs-gen guard: the downgrade fires ONLY for a test-file-declared wrapper (a synthetic fixture mirroring `unsafe_package.ArbitraryType` — ordinary production wrapper, unexported underlying — must stay untouched) | The §5 scoping finding, red-proven before it can regress silently again |
| **G2** | Full corpus build (`go2cs-stdlib.slnx`, 307 projects) | Load-bearing exactly as it was for the banked W3a commit: go2cs-gen changes are compile-time and invisible to CNR's pure-emission diff. Canary: `unsafe_package.ArbitraryType`/`internal/goarch.ArchFamilyType` compile unchanged — the two sites the unscoped attempt broke |
| **G3** | `runtime -tests -test-action build`, byte-compared error set | 39 → 12 (ctor/`.Value`) → 0 (operators omitted), each step re-measured fresh against current HEAD, not carried from this document's numbers |
| **G4** | CNR (683 behavioral packages) | Byte-identical — the change is gated to test-declared wrappers only |
| **G5** | Blast radius: two seeded `-tests` emissions of `runtime` diffed against each other (the method this session used throughout) | Isolates the change's true footprint the same way the banked W3a commit's was isolated |
| **G6** | A synthetic behavioral or golib-test fixture pinning the omitted-operator behavior | So a future converter change cannot silently re-add a non-public `implicit operator` and reintroduce CS0558 |

---

## 7. What is deliberately not proposed

* **No change to the wrapper struct's own `Scope` computation** (`generatedTypeScope`/
  `GetExplicitAccessModifier ?? GetScope`) — (b1) is retired, not folded in.
* **No corpus-wide default change** to `InheritedTypeTemplate` for non-test wrappers — the gate is
  test-file-only, by design, per §5.
* **No attempt to make the conversion operators internal** — not possible in C#; §4 is the complete
  answer, not a placeholder for a cleverer one.
* **No re-litigating W3a/W3b's already-banked rule** — this document is additive to
  `testMethodAccessDowngrade`/`packageScopeClassName`, not a replacement for either.
