# DESIGN — intrinsic call granularity at the emission's package boundary

**Status: SIZING DRAFT. No cut is proposed here and none should be taken from it.** The ruling
follows the draft; this document exists so that ruling is made against measured numbers and stated
blast radii rather than against an argument.

Author: G, 2026-09-02. Measurements: `BOARD-next-validation-candidates.md`, the `math/bits` two-null
block and the `addMulVVW` block that follows it.

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
