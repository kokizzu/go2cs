# c2-token-door-cost — §4.2 of `DESIGN-token-value-tag-refusal.md` (point-in-time record)

The **A-vs-B cost bench** the refusal record owes before the golib increment: what the value-tag
door costs at the trampoline, and whether the high-bit variant (**outcome B**, record §C) benches
worse than the 16-bit split (**outcome A**, record §B). COORD's criterion makes that the whole
question — §C is the default and §B is taken *only against a measured regression*.

**A record, not a gate.** Numbers frozen in the design note's §4.2 amendment.

## The arms, and why each one is here

| arm | test | why |
|:--|:--|:--|
| `none` | — | the trampoline as it is today; the baseline the door's absolute cost is measured against |
| `A` | `(arg >> 48) == 0x8000` | record §B, outcome A |
| `A2` | *byte-identical body to `A`* | **the noise floor.** Two methods, one predicate — whatever they differ by is what this harness cannot resolve |
| `B` | `(arg & 0x8000_8000_0000_0000) == 0x8000_0000_0000_0000` | record §C, outcome B |
| `B2` | `(long)arg < 0 && ((arg >> 47) & 1) == 0` | **the same predicate as `B`, spelled without a 64-bit immediate** — the obvious "why would B cost more" hypothesis, put on the bench instead of asserted |
| `CONTROL` | a `Dictionary.ContainsKey` probe | a deliberately expensive door that **must** separate, or a null A-vs-B result means only that the harness cannot resolve a difference |
| `ANCHOR` | one P/Invoke to a trivial native function | the managed→native transition the trampoline pays **anyway** — a LOWER BOUND on the guarded call, not the syscall cost |

`A`, `B` and `B2` assert they refuse the **same set**; a disagreement is a finding, not a timing
result. The argument mix is realistic rather than uniform — canonical addresses, small handles,
lengths, `-1` (`INVALID_HANDLE_VALUE`, which must **not** be refused) and ~5 % tagged tokens — so
the branch is not perfectly predicted and the refused count is provably nonzero.

## Running it

```
DOTNET_TieredCompilation=0 dotnet run -c Release              # the configuration of record
DOTNET_TieredCompilation=1 dotnet run -c Release              # tiering on
DOTNET_TieredCompilation=0 dotnet run -c Release -- --reverse # arms back-to-front
```

`--reverse` is not a convenience: arm **order** is a confound this bench cannot otherwise control,
and the reading below is what it exposed.

## What it read on linux-x64

Outputs of record in `output-linux.txt` (4 vCPU Xeon @ 2.80 GHz container, CoreCLR 10.0.111,
2026-09-08; 21 processes across four bench compositions × two orders × two tiering modes).

- **Door cost over no door: `A` 0.38–0.54 ns, `B` 0.39–0.70 ns per test.**
- **`B − A`, tiering OFF — 13 readings: −0.015 … +0.204 ns, twelve positive, mean +0.097.**
- **`B − A`, tiering ON — 8 readings: −0.034 … +0.012 ns, five NEGATIVE, mean −0.011.**
- **`B2 − A` — 12 readings: +0.656 … +0.763 ns, mean +0.704, in every configuration.**
- **Noise floor `|A2 − A|`: 0.0001 … 0.091 ns.** Control fired 21/21 at 6.3–8.4×.

**`B2` is the calibration, and it is what makes the `B − A` reading legible.** A genuine per-test
cost difference in this harness looks like `B2`: stable to ±7 %, same sign and magnitude in every
order, both tiering modes, every composition. `B − A` is none of those — its sign flips with
tiering, its magnitude moves when unrelated arms are *added to the bench*, and it lands inside the
noise floor in 9 of 21 runs. So the honest reading is that **B's excess over A is at or below what
this instrument can resolve**, with a weak positive lean at tiering-off.

`B2` also **falsifies the mechanism this lane was about to assert** — that B costs more because
x86-64 cannot encode its two 64-bit constants as immediates. Spelling the identical predicate to
avoid both is decisively worse, and the literal `B` spelling is the one to cut.

⚠ **CORRECTED 2026-09-08 (design record §F.1): this paragraph originally said "4–5× worse than
either door", and that multiplier used the wrong denominator.** As a **door-cost ratio** B2 is
~2.5× on the linux figures above and ~2.2–2.75× on the windows reproduction. The 4–5× came from
dividing B2's EXCESS over A by B's excess over A — a different quantity, which the sentence did not
say. The falsification itself is unaffected: B2 is stably the worst door in all 12 linux and all 24
windows runs.

## The materiality arithmetic, at the right granularity

The door sits on `SyscallN(uintptr trap, params ꓸꓸꓸuintptr argsʗp)` and runs **once per
argument** — arities to 18 exist — so a per-test number quoted as a per-call cost understates it by
the arity. The bench prints the per-call rows itself. Taking the **worst** `B − A` reading
(+0.204 ns) at the **worst** arity (18) against the anchor's lower bound: **3.7 ns on a guarded
call of ≥ 105 ns, i.e. ≤ 3.5 %** — and a real syscall is strictly larger than the anchor, so that
share is an overstatement. At realistic arities it is under 1 %.

⚠ **THOSE PERCENTAGES ARE LINUX-ONLY, AND THE WINDOWS ANCHOR IS NOT A SYSCALL AT ALL** (design
record §F.1(2)). On Windows the anchor arm calls `GetCurrentProcessId`, which is a **user-mode PEB
read at 6–9 ns**, not a kernel transition — a ~15× weaker lower bound than linux's `getppid` at
105–111 ns. So a "% of the lower bound" figure computed from a windows run is an **anchor artifact
and must not be quoted**, and **cross-host percentages from this probe are invalid**. The real
per-syscall row, through a genuine kernel transition, is specified in record §F.2 and is owed on
Windows.

## What this probe does NOT establish

1. **The mint side.** The record's prediction has a second clause — one extra shift-and-or per token
   on a path measured at 4,532 events — and this bench does not touch it.
2. **The per-syscall half of §4.2.** The record owes "one syscall-dominated roster row measured with
   and without". The trampoline is Windows; this reading is linux. That half is the binding host's.
3. **Anything about a real Windows syscall's cost.** The anchor is a P/Invoke transition, deliberately
   labelled a lower bound.
