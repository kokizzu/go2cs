# DESIGN — the PC read-back half (synthetic-PC increment 2)

> **Status: SIZING, not ruled.** Written by C1 on COORD's 2026-09-03 instruction to name Stage B's
> needs before the seam is cut. Companion to `DESIGN-synthetic-pc-registry.md`, which this does not
> restate. Measurements are taken at master `6fa031d08`, reproducible from the commands beside them.
> **No code exists for this increment.**

## 1. What increment 2 turned out NOT to be

The plan called it "the symbolizer". Reading the tree first says otherwise, and this is the whole
reason the note exists rather than a cut:

**A PC→name symbolizer already exists, and it already works.** `runtime/managed_impl.cs` carries the
entire traceback surface — `Callers`, `callers`, `captureCallers`, `Frames.Next`, `FuncForPC`,
`Func.Name`/`Entry`/`FileLine` — over its own token table, with the `[GoPositionMap]` records decoded
by `goFramePosition`/`readGoPositionMaps` in the same file. Writing a second one is the throwaway the
repo forbids, and it was one afternoon away from happening.

So increment 2 is a **reconciliation**: making one resolver answer for two token spaces that were
minted independently and have never met.

## 2. The finding that shapes it — there are THREE token spaces, not one

| space | minted by | value | measured at |
|:--|:--|:--|:--|
| **caller frames** | `captureCallers` interning a walked `StackFrame` | `s_callerRecords.Count` — i.e. `1, 2, 3, …` | `managed_impl.cs:1375` |
| **managed pointers** | `ManagedPointerTokens` for `reflect.Value.Pointer()` | `(nuint)(uint)RuntimeHelpers.GetHashCode(o)` — 32-bit | `ж.Contracts.cs:198` |
| **synthetic PCs** | `GoSyntheticPC.Of` for `FuncPCABI*` | `0xFFFF_8000_0000_0000 + (index << 12)` | `GoSyntheticPC.cs` |

**On 64-bit they are disjoint**, and not by luck in one direction: the high-half base was chosen so a
dereference faults, and that choice also puts it above everything the other two can produce.

### 2.1 A latent defect in my own 32-bit arm, found by this census

`GoSyntheticPC`'s 32-bit base is `0xF000_0000`. A managed pointer token is
`(uint)RuntimeHelpers.GetHashCode(o)`, which is unconstrained across the full 32-bit range and
therefore **can and will exceed `0xF000_0000`**. On a 32-bit runtime the two spaces collide, and a
resolver that tries both would answer a pointer token as a function.

It is unreachable today — the corpus targets 64-bit — which is exactly why it is worth writing down
now rather than leaving it latent for whoever first builds 32-bit. Two honest remedies, neither
chosen here: make the 32-bit arm THROW at mint time (the corpus cannot reach it, so a loud refusal
costs nothing and cannot rot), or give the resolver a discriminator that does not depend on range.
The registry's own guard should gain whichever is ruled.

## 3. The seam, stated as the minimal change

`Frames.Next` and `FuncForPC` resolve a pc through `callerFrameRecord(token)`, which returns `null`
for anything outside `1..s_callerRecords.Count` (`managed_impl.cs:1387`). A synthetic PC is outside
it by construction, so it already takes the null path and renders as `0x0` — which is precisely what
`TestEmptyCallStack` printed.

The change is therefore **additive and cannot disturb the working path**: where `callerFrameRecord`
returns null, consult `GoSyntheticPC.Resolve` before giving up. Banked rows depend on the caller-token
path; nothing about it moves.

### 3.1 The open question the seam cannot answer by itself — file and line

`goFramePosition(method, frame)` derives a Go position from a **live `StackFrame`**: the frame gives a
C# line, and the `[GoPositionMap]` record maps that line to a Go line. A synthetic PC has no frame and
therefore no C# line, so the existing reader cannot be pointed at it unchanged.

Go answers `lostProfileEvent` with a real file and line out of the pclntab. Three candidates, and the
design does not pick one before it is measured against what the consumers actually assert:

* **(a) name only** — file `""`, line 0. `TestEmptyCallStack` asserts only `strings.Contains(got,
  "lostProfileEvent")`, so this passes it. Cheapest, and honest about what a token knows.
* **(b) the function's declaring position** — the first `[GoPositionMap]` record for the method's own
  file. Needs a method→C#-line source that reflection alone does not provide (a PDB read, or a new
  per-method record the converter emits). Costs more than the increment.
* **(c) file without line** — the record's `goFile`, line 0. Available today: the map record is keyed
  by C# FILE, and the method's declaring type identifies it.

My reading is (c) is the honest maximum for a token, and (a) is what the acceptance requires. Ruling
wanted before code.

## 4. The two Go-branch delegations that ride this increment

Both are pprof linkname destinations, both currently throwing stubs, both the same shape as
consumer (1) — Go's own branch, not an approximation:

* `runtime_FrameSymbolName` — Go's body is `if !f.funcInfo.valid() { return f.Function }` before it
  inline-unwinds. `funcInfo` is never valid here, so **`return f.Function`** is the branch that fires.
* `runtime_FrameStartLine` — Go's body is unconditionally `return f.startLine`. A delegation.

Both are worthless until §3 lands, because both read `Frame` fields that are empty today. That is why
they ride this increment rather than landing as a trivial pair now: a pair that returns `""` is not a
fix, it is the same silent zero one layer along.

## 5. Stage B's needs — named, and with a question that may loosen the dependency

Stage B of the `runtime.Stack` arc (captured park stacks) is recorded as holding behind this
increment. Naming its needs is this note's assignment, and doing so surfaces something worth
measuring before Stage B is designed:

**A park-time capture that records its stack through `captureCallers` produces caller tokens, which
`Frames.Next` ALREADY resolves.** If Stage A's `Goroutine.Park(reason)` captures that way, Stage B's
frames never enter my registry at all, and the dependency is not "Stage B needs the synthetic-PC
symbolizer" but "Stage B and this increment must render frames through ONE path". Those are different
obligations and only the second is real.

What Stage B genuinely needs from this increment, on that reading:

1. **One renderer, two sources.** Whatever resolves a pc for `Frames.Next` must be the same code that
   renders a park-time frame, or the corpus grows the second symbolizer by a different door.
2. **A pc slice captured at park time must stay resolvable after the goroutine parks** — which the
   caller-token table already guarantees ("resolved at intern time so a later Frames walk needs no
   live StackFrame", `managed_impl.cs:1180`). This increment must not weaken that.
3. **Synthetic PCs must survive the same walk**, because a park stack can contain a `FuncPCABI*`-
   sourced frame even when most of it is interned caller frames.

**The measurement Stage A's owner and I owe jointly, before Stage B is cut:** does the park capture
use `captureCallers`? One read of Stage A's cut answers it, and the answer decides whether Stage B
waits on this increment at all.

## 6. The guard, and why the obvious one is wrong

R's descriptor-cargo §8.5 warning applies here almost word for word, one arc over: *the gate must
assert what the change alters, not the symptom that led you to it.*

This increment changes **PC→function RESOLUTION**. Its obvious acceptance —
`TestEmptyCallStack`'s `strings.Contains(got, "lostProfileEvent")` — is a **name** check, and a
resolver that returned the right name for the WRONG token would pass it. So:

* **The guard asserts resolution** (GolibTests, over the registry directly): a known token resolves to
  its own function; a neighbouring token resolves to a different one; a caller-space token
  (small integer) still resolves through the caller table and NOT through the registry; a pc in
  neither space still answers null.
* **The name assertion is the consumer-level confirmation, not the proof.**
* **A negative control** that neuters the fallback must make exactly the resolution guard go red, and
  the restore must be byte-identical — the standard increment 1 met.

## 7. Acceptance

* `TestEmptyCallStack` — `Go=pass`, `C#=pass` (the name reaches the textual profile).
* `TestConvertCPUProfile` — reaches a verdict at last, its second stub (`runtime_FrameSymbolName`)
  being §4's first bullet. Which verdict is not predicted here.
* `internal/abi` reproduces `1 + 1` — the increment must not disturb the registry's own row.
* GolibTests count-reconciled against the compile set; the stdlib build on both targets read out of
  the generator's output; `runtime/pprof`'s filtered set with the prediction recorded first.

## 8. What is NOT proposed

* **No pclntab, no inline trees.** Unchanged from the registry's §8; §4's first bullet depends on it.
* **No change to the caller-token path.** It is banked-load-bearing and the fallback is additive.
* **No unification of the three token spaces.** They are disjoint on 64-bit (§2) and unifying them is
  a larger cut with no consumer asking for it. The 32-bit collision (§2.1) is named, not fixed here.
* **No `getg()` change**, and no park accounting — that is Stage A, and it is somebody else's cut.

-- C1
