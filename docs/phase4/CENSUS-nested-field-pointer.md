# CENSUS — nested-field pointer copy-boxing (`Ꮡ(x.field).of(…)`) — DRAFT for coordinator review

> READ-ONLY census over `C:\Projects\go2cs` (branch `master`, HEAD `43e024d38`), 2026-08-23.
> Scope: production emission under `src/core` (3,275 `.cs` files; `bin`/`obj`/`Generated` excluded)
> and the behavioral tree `src/tests/Behavioral` (`.cs` + `.cs.target`). Instrument: Python
> balanced-paren parser over every `Ꮡ(` site (the address-of glyph is **U+13D1**, not U+13E1 as
> briefed — a plain U+13E1 grep returns zero and reads as a false all-clear; the first regex pass
> also under-counted by missing inners containing parens like `Ꮡ((~pr).Inst, 0)` — both corrected,
> final numbers are from the paren-parser). Every classified site below was read in context, not
> bulk-labeled; the counts are small enough that per-site classification is exhaustive, not sampled.

## Headline

| Bucket | src/core | Behavioral |
|---|--:|--:|
| Total `Ꮡ(…)` call-form sites (all shapes) | 5,565 | 1,051 |
| … followed by `.of(` | **20** | 20 |
| **HAZARD shape: one-arg `Ꮡ(dotted-field-path).of(`** | **4** | **0** |
| One-arg `Ꮡ(plain-local).of(` (copy-box, no field path inside) | 6 | 0 |
| Two-arg `Ꮡ(collection, i).of(` (ALIASING form — benign) | 10 | 20 |
| Chained `Ꮡroot.of(A.Ꮡb).of(B.Ꮡc)` (the CORRECT nested form, for scale) | 309 (49 files) | — |

**Hazard verdict: 4 write-context sites, one function, one file, one package — and that package is
the exact one lane R rooted.** There is no second shipped instance of the shape anywhere in the
production corpus.

## The 4 hazard sites — all write-context

`src/core/vendor/golang.org/x/net/dns/dnsmessage/message.cs`, all inside
`incrementSectionCount(this ref Builder b)`:

| Line | Emission |
|--:|---|
| 1349 | `count = Ꮡ(b.header).of(dnsmessage_package.Δheader.Ꮡquestions);` |
| 1353 | `count = Ꮡ(b.header).of(…Ꮡanswers);` |
| 1357 | `count = Ꮡ(b.header).of(…Ꮡauthorities);` |
| 1361 | `count = Ꮡ(b.header).of(…Ꮡadditionals);` |

Write context is certain: `count.Value++` at :1368 (plus a guarded read at :1365). golib's
`Ꮡ<T>(in T)` (builtin.cs:1613) heap-boxes a COPY by its own doc-comment, so the increment lands in
the copy and `b.header`'s section counts stay 0 — every DNS message packed through `Builder`
wire-encodes zero questions/answers/authorities/additionals. Silent wrong value, no fault.
**Blast radius beyond the package:** 13 `.cs` files under `src/core/net` reference `dnsmessage`
(the Go-resolver query-packing path), so `net`'s future validation walks straight into this.

Why this one function: the receiver is the `this ref Builder b` `[GoRecv]` form — no `Ꮡb` box in
scope — so the converter boxed the intermediate field value. Where the receiver IS a box
(`this ж<Conn> Ꮡc`), the converter already emits the correct chained projection routinely —
`Ꮡc.of(Conn.Ꮡout).of(halfConn.ᏑMutex).Lock()` (crypto/tls/conn.cs) is one of **309 chained
`.of(…).of(` sites in 49 files**. The fix direction lane R proved (chain from the receiver) is
therefore the emission the corpus already exercises at scale; the gap is only reaching a root
address from the ref-receiver form.

## The benign and ambiguous lookalikes (classified per-site)

**Two-arg `Ꮡ(collection, index).of(…)` — 10 sites, all BENIGN.** golib's `Ꮡ<T>(IArray<T>, index)`
returns an element-aliasing pointer (backing `T[]` is shared by the slice/array header copy), and
the `SliceElementFieldAddress` behavioral test WRITES through exactly this chain and passes — it is
the proven-correct form, not a hazard. Sites: `encoding/xml/xml.cs:328`;
`net/{windows,linux,darwin}/ipsock_posix.cs:77`; `regexp/onepass.cs:262,263,280,281` (the live
original of the already-fixed element-field arc, now on the correct form);
`runtime/mgcsweep.cs:97`; `runtime/sema.cs:61`.

**One-arg `Ꮡ(plain-local).of(…)` — 6 sites, zero lost writes today:**

| Site | Context | Classification |
|---|---|---|
| `archive/tar/writer_test.cs:509` `Ꮡ(tfΔ1).of(…Ꮡhdr)` | `tw.WriteHeader(…)` consumes the header, no write-back | read — value-correct (and its validated suite passes, a live positive control) |
| `net/netip/netip.cs:385, 391` `Ꮡ(ip).of(…Ꮡaddr)` | immediate `.halves()[…]` read | read — value-correct |
| `net/rpc/debug.cs:99` `Ꮡ(server).of(…ᏑserviceMap)` | `.Range(…)` over the map | read — map internals shared even through the copy |
| `runtime/symtab.cs:1102, 1146` `Ꮡ(f).of(…Ꮡnfuncdata)` | raw `uintptr` address arithmetic (funcdata walking) | **ambiguous** — address-of-copy is wrong for identity purposes, but the path is raw-metal funcInfo territory, inert on the managed runtime |

This matches the board's own earlier census of the bare-local shape (BOARD ~11395: "6 sites in 4
files … only xml's is a WRITE") — xml's write has since been FIXED (its type-switch binding is now
heap-boxed: `ref var t1 = ref heap(t1ᴛ2, out var Ꮡt1)` then `Ꮡt1.of(…)`; encoding/xml validated
386), and the tar site is a test file the board's production census didn't count.

**Adjacent family, same mechanism, no `.of(` projection** — one-arg `Ꮡ(dotted)` without `.of(`:
**719 sites**, sub-bucketed: **691 call-results** (`Ꮡ(pkg.Func(…))` — benign by construction:
boxing a fresh rvalue IS Go's `&T{…}`/escape semantics); **10 deref-root paths** (9 ×
`internal/abi/type.cs` uncommonType metadata reads via `Reinterpret`, 1 × `runtime/linux/signal_…`
— read/raw-metal, inert); and **18 pure lvalue paths, every one a cross-package package-var
address-of**, worth naming:

- `Ꮡ(syscall.ForkLock).RLock()/RUnlock()` — **9 sites, write-context** (mutex state mutates a fresh
  copy per call; the lock is never really held): `internal/poll/{linux,darwin}/fd_unixjs.cs:36,37`,
  `net/darwin/sys_cloexec.cs:23,28`, `os/darwin/pipe_unix.cs:19,22,27`. All per-GOOS unix files —
  never compiled on the Windows lane, so zero effect on today's validated corpus, but they go live
  with the Linux-operation campaign.
- `Ꮡ(build.Default)` — 4 sites (`go/importer/importer.cs:69` + srcimporter/go-types test files) —
  read-context (the importer reads the context).
- `flag.BoolVar(Ꮡ(@internal.IgnorePC), "nopc", false, …)` — `log/slog/internal/benchmarks
  /benchmarks_test.cs:22` — write-context in principle (flag machinery writes through the pointer)
  but inert in the validated suite: the default written equals the initial value and the flag is
  never set.
- `Ꮡ(channel<T>.SendOnly/RecvOnly)` statics — 4 sites in `internal/reflectlite` tests — read-only
  reflect type probes.

## Behavioral coverage — nothing exercises the hazard emission

Zero one-arg `Ꮡ(field-path).of(` sites exist anywhere in the behavioral tree (goldens included).
The 20 behavioral `.of(`-after-`Ꮡ(…)` sites are all the **two-arg aliasing form**:
`SliceElementFieldAddress` (18 = 9 sites × `.cs`/`.cs.target`) and `NamedArrayAnonElement` (2).
`SliceElementFieldAddress` is the closest guard and its own header comment names the
`Ꮡ(s[i]).of(T.Ꮡf)` copy-box fallback as the regression it locks out — but its root is a slice
ELEMENT, not a struct-typed FIELD under a `ref` receiver, so **no behavioral test contradicts lane
R's finding and none guards the coming fix**. The only behavioral one-arg-dotted uses at all are
`ReflectChanDirection`'s 4 read-only channel-type probes. A new guard (the receiver-nested-field
mirror of `SliceElementFieldAddress`, with a slice-element `&s[0].h.n` two-level case — lane R's
second reproduction, which has **zero** shipped corpus instances today) is part of the arc's price.

## Roster cross-reference

| Package | Hazard-write `.of(` sites | On validated roster? |
|---|--:|---|
| `vendor/golang.org/x/net/dns/dnsmessage` | **4** | **NO** (not on roster; vendored — surfaced via `net`, also not yet validated) |

No validated package carries an active hazard-write site in the `.of(` family. Validated packages
touching the wider family are all benign/fixed forms: encoding/xml 386 (two-arg + its former write
fixed), regexp 45 (fixed two-arg form), sync/atomic 108 (the board's 43-divergence copy-box root,
since remedied by heap-boxing the local — committed `atomic_test.cs` now reads
`ref var x = ref heap(…, out var Ꮡx)`), archive/tar 97 / go/importer / go/types /
go/internal/srcimporter / internal/reflectlite / log/slog/internal/benchmarks (read-context or
inert sites only). The task's "validated package with an untested hazard site" scenario does not
occur — the nearest real thing is the **unix-only `ForkLock` write sites** waiting on the Linux
lane, and `runtime/symtab`'s two address-arithmetic sites (runtime is unvalidated).

## Relation to existing arcs (docs/phase4, per their own text)

The board already tracks this as a serialized FAMILY — "the address-of-copy-boxing shape, one base
shape per fix" (BOARD-next-validation-candidates.md:2791, calling gob's reinterpret-as-value the
*fifth sighting*). Prior sightings and their states, per the board: element-field-address
(`&s[i].f`) — FIXED via the two-arg aliasing form, guarded by `SliceElementFieldAddress`
(ConversionStrategies-Reference.md:10703), regexp's onePassCopy the live original; type-switch
binding (`&t1.Name`, encoding/xml) — FIXED via heap-boxing the binding, +12 verdicts measured;
local-whose-field-address-is-taken-through-a-method (sync/atomic's 43 divergences, BOARD ~11577) —
the board named an escape-analysis remedy and the committed corpus shows it landed; gob's
reinterpret-as-value — recorded, chip-owned, not fixed. **Lane R's receiver-nested-field shape is
the family's next sighting, and no phase4 doc covers it**: there is no `embedded-pointer-promotion`
document, and the ж-box arc (`DESIGN-zh-box-reduction.md`, signed off 2026-08-10, plus
CENSUS-zh-box-a1 / DESIGN-zh-box-b-prime / EXEMPLARS-a2-ref-lowering) is the PERFORMANCE axis over
the same emission neighborhood — its text treats `Ꮡe.of(…)` field-ref boxes as an allocation COST
to lower (A2's `ref T` lowering), never as a correctness question, and nothing in it reaches the
`Ꮡ(b.field)` copy-box. The two arcs are complementary, not overlapping: this fix changes WHICH
storage the pointer aliases; ж-box changes how much the pointer costs.

## Pricing

**This is a handful, not hundreds — but the handful is load-bearing.** The shipped exposure is 4
write-context sites in one function of one unvalidated vendored package, plus two ambiguous
raw-metal reads in `runtime/symtab` and 9 unix-only `ForkLock` adjacent-family sites that activate
with the Linux campaign. Corpus churn from the fix itself is correspondingly tiny (one file, four
lines today, assuming the emission change doesn't reshape the 309 already-correct chained sites —
CNR will prove that either way). The converter-side work is the real price: making the
`this ref` `[GoRecv]` receiver form reach a root address so the emission can chain
`.of(…).of(…)` instead of boxing the intermediate field — the sixth entry in a family where each
prior fix (element aliasing, binding heap-boxing, local heap-boxing) was a bounded, single-shape
converter change with a behavioral guard, and this one looks the same size. Budget: one converter
fix + one new behavioral guard (receiver-nested-field mirror of `SliceElementFieldAddress`,
including the slice-element two-level case) + a dnsmessage regen + a full CNR pass. What the small
count does NOT license is deferral-by-rarity: the shape is exactly what any Go code with a
struct-typed field under a pointer receiver produces, `net`'s DNS path sits on it, and the corpus
count will grow with every future stdlib upgrade — the same reason the family's five earlier
sightings were each fixed at the converter, not hand-patched.
