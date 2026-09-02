# DESIGN — intrinsic call granularity at the emission's package boundary

**Status: SIZING DRAFT. No cut is proposed here and none should be taken from it.** The ruling
follows the draft; this document exists so that ruling is made against measured numbers and stated
blast radii rather than against an argument.

Author: G, 2026-09-02. Measurements: `BOARD-next-validation-candidates.md`, the `math/bits` two-null
block and the `addMulVVW` block that follows it.

> ⚠ **READ §10 FIRST.** Sections 2 and 5 were written before two falsifiers were run and they are
> **SUPERSEDED**: they attribute the cost to the cross-assembly boundary, which §10 measures at
> **~1.0×**. The real mechanisms are an `UntypedInt` struct comparison evaluated per call (2.72×) and
> IL size over the JIT's inlining budget (1.32–1.42×), both in the emitted body. §10 also replaces the
> first candidate. The superseded sections are kept per this directory's amend-never-rewrite rule, so
> the wrong attribution and its cause stay visible — but nothing in §2 or §5 should be quoted without
> §10 beside it.

---

## 1. What this buys the objective

**The success metric is a row, not a benchmark.**

`net/http`'s `TestWriteDeadlineEnforcedPerStream/h2` and `TestWriteDeadlineExtendedOnNewRequest/h2`
fail at Debug on hosts whose converted TLS handshake exceeds `Server.WriteTimeout`, and **pass at
Release on the same host**. At Release the handshake's residual over Go is **79% one RSA-2048
CertificateVerify signature** (44.5–64.6 ms converted against Go's 0.834 ms, depending on host).

So the question this design answers is: *how much of that signature can the emission give back?* Any
candidate below is judged by what it does to that row and that signature — never by its ratio on the
micro-benchmark that found it.

⚠ **The row already passes at Release.** This design does not unblock it; the pipeline-configuration
ruling did. What it addresses is the residual underneath — the reason a converted handshake is 22×
Go's even when built the way users ship.

---

## 2. The seam, as measured

The `math/bits` intrinsic cut (withdrawn, `claude/g-mathbits-intrinsics`) established what the seam is
**not**: replacing the emulated primitives with `Math.BigMul`/`BitOperations` made them 1.6–1.9×
faster and moved three workloads by 0.0%, −3.7% and nothing. **The arithmetic is ~2% of a signature.**

The `addMulVVW` micro-benchmark then apportioned the loop that *is* hot (Montgomery multiplication's
inner loop, i.e. every RSA private-key operation), Release + `DOTNET_TieredCompilation=0`:

```
full emitted shape                                    24.2 ns/word    13.1x raw
cross-assembly boundary          A / G       4.16 - 4.17x   <- DOMINANT
AggressiveInlining, cross-asm    A / E-CROSS 2.00 - 2.01x
AggressiveInlining, same-asm     G / E       1.32 - 1.34x
golib slice vs Span              D / B       1.47 - 1.50x
Word generated-struct wrapper    F / A       1.08 - 1.09x   <- nearly free
```

**The seam is that leaf primitives live in a different assembly from every caller.** Go has no
equivalent cost: `cmd/compile` intrinsifies `math/bits.Mul64` to a single `MULQ` **at the call site**
(`ssagen/ssa.go:5022`), and additionally aliases `math/big`'s own `mulWW` to that intrinsic
(`ssa.go:5113`). There is no call, no tuple, and no module boundary to cross.

---

## 3. What a candidate must carry

Per the coordinator's standing shape, each candidate below states:

1. **blast radius** — which packages' emission changes, to be measured by the **two-seeded diff's HUNK count**, never its file set (the 2026-09-02 rule);
2. **its guard** — what turns a regression into a red gate;
3. **the number it predicts** for the RSA-2048 signature and the handshake.

### How the predicted signature numbers below are derived

Stated so they can be checked rather than believed. An RSA-2048 CRT signature performs two 1024-bit
modexps; each is ~1280 modular operations (1024 squarings plus a windowed multiply chain), and each
modular operation over 16 64-bit words costs ~2·16·16 word-multiplies. That is **~1.3 × 10⁶
word-multiplies per signature**.

At the measured **24.2 ns/word** that is **~31.7 ms**, against a measured **64.6 ms** signature on this
host — i.e. **the emitted `addMulVVW` loop is roughly half the signature.** Go's assembly loop at
~0.4 ns/word is ~0.5 ms of its 0.834 ms signature, which is the consistency check that the estimate is
in the right region.

⚠ **These are ESTIMATES with a stated derivation, not measurements.** Every candidate's real number is
owed as a before/after on the RSA probe at Release+TC0 before any ruling. If the measured movement
disagrees with the estimate, the estimate is wrong and the arithmetic above is where to look.

---

## 4. Candidate A — `AggressiveInlining` on the leaf primitives

**Measured: 2.00–2.01× on the loop** (22.35 → 11.1 ns/word, cross-assembly, attribute on all four
levels of `Mul`→`Mul64` and `Add`→`Add64`).

⚠ **This candidate has NO CARRIER at master.** The framing assumed the attribute would ride the
registry's hand-owned leaves — but `manualConversionFuncs` has **zero `math/bits` registrations** at
`3c745e0d9`, because that cut was withdrawn for measuring three nulls. So Candidate A is really two
sub-candidates, and they cost very differently:

* **A1 — re-introduce the withdrawn hand-own solely to carry the attribute.** Sixteen hand-owned functions plus a registry entry, whose *own* value was measured at zero; the attribute would be the entire justification. Blast radius: the two files the withdrawn cut's two-seeded diff already measured (`bits.cs` 33/195, `package_info.cs` 1/1). Guard: `math/bits` 26/26 and `math/big` 224/224, both already proven unmoved by that cut.
* **A2 — the converter emits the attribute on small leaf functions generally.** No hand-own, but a corpus-wide emission change whose blast radius is **unmeasured** and must be a two-seeded HUNK count before it is sized. Guard: full CNR plus the converter suite; the emission change is mechanical, so byte-identity outside the intended hunks is the real assertion.

**Predicted signature:** the loop at 12.1 ns/word ⇒ ~15.9 ms of loop cost, i.e. **64.6 → ~48.8 ms
(−24%)**. Handshake ~57.9 → ~42 ms. **Neither closes the row's margin, which is already closed.**

---

## 5. Candidate B — a converter-side intrinsic table, emitting the BCL call at the site

The direct analogue of `cmd/compile`'s `ssa.go:5113`: the converter recognises a fixed set of
`math/bits` calls and emits `Math.BigMul` / `BitOperations.*` **inline at the call site** instead of a
cross-assembly call.

**Measured proxy: 5.51–5.58× on the loop** (variant E, same-assembly and inlined, is what
emit-at-the-site produces: 22.35 → 4.03 ns/word).

* **Blast radius:** every package that calls the tabled functions — the alias-resolved census gives `crypto/internal/nistec/fiat` 3,061 sites, `crypto/internal/edwards25519` 240, then a long tail; `math/big` reaches them through the word-sized `bits.Mul`/`bits.Add` wrappers, which the table must therefore cover too. **HUNK count unmeasured and owed** before this is sized; the file count is not the number.
* **Guard:** `math/bits` and `math/big` rows (the semantics are Go's own and must not move), the crypto canaries (`crypto/rsa`, `crypto/x509`, `crypto/tls`), full CNR, and a converter-suite guard asserting the table emits at the site for a known call and does NOT for an untabled one — control-first, RED before the change.
* **Predicted signature:** loop at 4.4 ns/word ⇒ ~5.8 ms ⇒ **64.6 → ~38.7 ms (−40%)**. Handshake ~57.9 → ~32 ms.

**This is the candidate with the numbers behind it**, and it is also the one that reproduces what Go
actually does rather than approximating it.

---

## 6. Candidate C — nothing

Retained as a real option, because two of tonight's three cuts were withdrawn on measurement and this
document exists to make that outcome available rather than embarrassing.

The case for C: **the row already passes at Release.** The residual is a performance property of the
emission, not a correctness gap; no roster row is currently blocked by it; and both A and B spend
converter or hand-own surface against an estimate that no measurement has yet confirmed at the
signature level. C costs nothing and loses nothing that is currently owed.

The case against C: the residual is ~40% recoverable by B on the estimate above, it compounds across
every crypto package in the corpus, and the arithmetic-heavy rows carrying `$longTimeouts` floors are
plausibly paying it too — though **that last point is a pattern, not a measurement**, and it should not
be used as an argument until someone measures one of those rows.

---

## 7. What is explicitly NOT proposed

* **The `Word` generated-struct wrapper.** Measured at **1.08–1.09×** — the JIT promotes the single-field `readonly struct` almost completely. It is not a design concern and should not be reopened without a new measurement.
* **`golib`'s `slice<T>` indexing.** 1.47–1.50×, real but second-order against the boundary's 4.17×. Any work here belongs after B, judged on its own measurement.
* **`math/big`'s `addMulVVW` as an assembly-shaped hand-own.** The earlier ruling held this in reserve as a "second cut" behind `math/bits`. Tonight's apportionment retires it: it is another arithmetic-level fix to a cost that is not arithmetic, and B subsumes what it would have bought.

---

## 8. What would falsify the sizing

Stated so the ruling can be made conditional rather than final:

1. **The signature estimate.** If a Candidate-B prototype moves the RSA probe by materially less than the predicted ~40%, the ~1.3 × 10⁶ word-multiply derivation in §3 is wrong and the loop is a smaller share of the signature than estimated. **Measure before ruling.**
2. **The boundary attribution.** A/G was measured with a *hand-transcribed* same-assembly copy. If a real emit-at-the-site prototype does not reproduce ~4× on the same loop, the transcription differs from the emission in some way the benchmark did not model.
3. **Whether the JIT's behaviour is version-stable.** Every number here is .NET 10.0.11 with tiering off. A future runtime that inlines across the boundary more aggressively would shrink the seam without any change on our side.

---

## 9. Recommendation

**Prototype Candidate B far enough to measure the RSA probe, then rule.** Not to cut it — to replace
§3's estimate with a number, because that estimate is the only thing standing between this design and
a decision, and tonight has twice shown what happens when an op-count argument substitutes for a
measurement.

If B's measured movement is near the predicted 40%, it is worth its converter change. If it is near
A's 24%, the simpler A2 is worth comparing. If it is small, **C** is the answer and this document is
the record of why.

---

## 10. RULING and the falsifiers' answers (2026-09-02, appended after §1–9)

**The coordinator HELD this document at DRAFT** rather than authorising a prototype of Candidate B,
because §5's central number carried a tell the draft did not explain: variant **E** (same-assembly
local copy, attribute) read **4.0 ns/word** while **E-CROSS** (the same code in a scratch `bits`
assembly, the same attribute on the same four methods) read **11.1**. *A JIT that inlines identical IL
emits identical machine code whichever assembly the IL came from* — so a 2.75× gap between those arms
could not be "the boundary's cost", and the `A/G` pair that §5 rested on inherited the same question.

**The hold was correct and the draft's §5 was wrong.** Two falsifiers settled it.

### Falsifier 1 — optimization state of every referenced assembly, read in-process: REFUTED

`DebuggableAttribute` on `typeof(bits_package).Assembly`, golib and each console, printed from inside
the probe: **`IsJITOptimizerDisabled=False` everywhere**, both consoles. No Debug callee reached a
Release console, so the table was not void on that ground.

### Falsifier 2 — the JIT's own report

`DOTNET_JitPrintInlinedMethods` requires a checked JIT and produced nothing; **`DOTNET_JitDisasmSummary=1`
works on the release runtime** and answers by which methods are compiled at all.

*Arm A (corpus `bits`, no attribute) — NOT inlined:*

```
29: JIT compiled go.math.bits_package:Mul(nuint,nuint)        [FullOpts, IL size=83, code size=141]
32: JIT compiled go.math.bits_package:Add(nuint,nuint,nuint)  [FullOpts, IL size=87, code size=152]
```

*Arm E-CROSS (scratch `bits`, attribute) — INLINED:* `bits_package` appears **zero** times in that
arm's entire compile list; the timed lambda reads `[FullOpts, IL size=63, code size=800]`.

**So the assembly boundary never prevented inlining.** The attribute overrides the size budget across
it, and the default JIT declines on **IL size (83/87 bytes)**, not on provenance.

### Variant H — the difference the benchmark did not model

`H` = `E` with the emitted `UintSize` restored (`bits.cs:21` emits `public static UntypedInt UintSize
=> 64;`, a property returning the generated struct) in place of the `const int` my transcription used.

```
E  same-asm, attr, const        4.02 - 4.05 ns/word
H  same-asm, attr, UntypedInt  10.95 - 11.06        H / E = 2.72 - 2.73x
E-CROSS  cross-asm, attr, UntypedInt  10.96 - 12.31  == H, within noise
```

**`H` reproduces `E-CROSS`.** The assembly boundary is **~1.0×**; §5's "4.17×" was an artifact of the
hand-written copy.

### The seam, restated

1. **`UintSize == 32` is a struct comparison evaluated per call — 2.72×.** `UntypedInt.operator ==` is `left.Equals(right)` over a private `Compare` the JIT compiles standalone at **IL 141** and never inlines. Go folds this branch at compile time. **This is a property of emitted UNTYPED CONSTANTS, not of `math/bits`.**
2. **IL size over the inlining budget — 1.32–1.42×**, from the two-level chain, tuples and conversions.

⚠ **~1.5× of `A` is unapportioned** (`4.02 × 2.72 × 1.38 = 15.1` vs a measured 22.4) and is named, not
absorbed; the likely reading is compounding, and it is **not** claimed as boundary cost.

### Candidate map, revised

* **A1-as-attribute-carrier: REJECTED** (coordinator, on the draft's own argument). **A2: HELD.** **C: stays available.**
* **NEW FIRST MOVE — the one-level word-size hand-own.** Replace `Mul`/`Add`/`Sub` with a single BCL call each: this removes the nested level, the inter-level tuples **and** the `UintSize` branch together — the 2.72× and the 1.38× in one cut, with no converter change. ⚠ It is exactly the level the **withdrawn** `math/bits` cut did not register (it took `Mul64`/`Add64`/`Sub64`), which explains that cut's 0.0% far better than "intrinsics do not help".
* **Candidate B is DEMOTED.** Most of the recoverable cost is reachable without a converter intrinsic table, and B is not clean for `Add64`/`Sub64` in any case — no single BCL call exists, so site emission would be the carry formula or a golib helper, i.e. a cross-assembly call again.

### The question this opens, flagged and NOT sized here

Whether `UntypedInt`-versus-literal comparisons should fold in the emission **generally**. The 2.72×
was measured in `math/bits` but the mechanism is not specific to it: any emitted `UntypedInt` compared
against a literal in a hot path pays a non-inlined struct equality call for what Go treats as a
compile-time constant. That is a converter design question with a corpus-wide radius, larger than this
document, and it is raised here rather than answered.

### Status

**Still a DRAFT, and now with a different first candidate than §5 proposed.** Nothing is cut. The next
measurement owed is the one-level word-size hand-own, before/after on the RSA probe at Release+TC0.
