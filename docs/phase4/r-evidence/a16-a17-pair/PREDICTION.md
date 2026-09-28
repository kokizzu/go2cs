# A16 (a) + A17 (revised) pair — PREDICTION (written before any emission)

Seat: `claude/r-a16-a17-pair` on the TRAIN G union `e6fc210500`. COORD rulings 2026-09-28 (A16 (a); A17 revised,
with the identity condition and the NO-NEW-METADATA guard).

Commits before this prediction: `7491bf940c` (A16, golib only) and `8cb0f370b6` (A17: converter, go2cs-gen,
golib, arms). The corpus has NOT been emitted yet.

## Two-seeded `-stdlib` footprint (base `e6fc210500` converter vs this seat's), three targets

Only (1) of A17 changes emission (the arc admits NAMED zero-size fields); A16, the generator, the identity rule
and the guard are golib/gen and emit nothing.

| file | change | targets |
|---|---|---|
| `internal/runtime/atomic/types.cs` | `Int32`, `Int64`, `Uint8`, `Uint32`, `Uint64`, `Uintptr` each gain `[StructLayout(LayoutKind.Explicit, Size = N)]` (4, 8, 1, 4, 8, 8), a `[FieldOffset(k)]` on every field, and `readonly` on `noCopy` (and on `_` where present); a `using System.Runtime.InteropServices;` line if the file lacks it | all 3 |
| `internal/fuzz/pcg.cs` | `pcgRand` the same (Size 16; `noCopy` readonly at 0; `state` at 0, `inc` at 8), plus the using line if absent | all 3 |
| each changed file's `package_info.cs` | the `GoPositionMap` record RE-ENCODES only if a using line was added (W1's lesson: the map lives in package_info.cs) | all 3 |

UNCHANGED, stated: `UnsafePointer` (its value is a reference-bearing `@unsafe.Pointer`), the generic `Pointer<T>`
(`_ [0]*T` is an array, managed), `Bool` and `Float64` (no zero-size field of their own), `sync.Cond` (managed
`L`), and every struct that merely CONTAINS an atomic carrier (its fields are not zero-size).

Remainder: other NAMED zero-size carriers the census patterns cannot see (a named `struct{}`-typed field) may
appear; each must be exactly the three-token shape above on an unmanaged, embed-free struct. **Falsifiers:** any
hunk that is not attribute/readonly/using/position-map; any per-GOOS difference in these files; a managed struct
gaining a layout (a TypeLoadException shape).

## CNR

**Exactly one CHANGED:** `ReflectStructTagCopy` (its `layout` struct: named `pad empty` + three unmanaged fields),
gaining the layout. Its run output is predicted UNCHANGED (it prints offsets from reflect's own Go layout walk).
Every other behavioral package byte-identical.

## GolibTests / GenTests

GolibTests pinned both flavours: the three host symlink rows only. GenTests: green.

## The family re-probe (the gate)

With W1 (windows) and this pair: the page-allocator rows no longer die in `scavengeIndex.alloc` (the linux
AccessViolation, the windows out-of-reservation corruption): `atomicScavChunkData` is 8 bytes, so the stride
is Go's. **Falsifier:** that AV or "too many pages allocated in chunk?" reproducing. The NEXT gate is not
predicted; the probe reads it.

## Atomic A/B (owed before landing)

An atomic-heavy microbench (Load/Store/Add/CompareAndSwap on `internal/runtime/atomic.Uint64` and `Int32`, as
a heap field and as a local) at base vs this seat, base arms agreeing within 0.5 ns. Predicted: no loss for
the heap-field form (accessed by ref either way); the local form MAY lose promotion under explicit layout. Its
delta is reported, not assumed.

---

## FOOTPRINT SCORED 2026-09-28 (appended; the prediction above is left as written)

Two-seeded `-stdlib`, base `e6fc210500` converter vs cut `f2be4e3d7a`; seeds 4,205 `.cs` per root (one value);
both binaries fresh; 1,856 / 1,926 / 1,926 files written per arm. Every target: **5 files, +33 −30**, the same
content (the linux/darwin hunk lists differ from windows only in ORDER, the per-GOOS fuzz `package_info.cs`
sorting under its own path).

| file | predicted | measured |
|---|---|---|
| `internal/runtime/atomic/types.cs` | 6 carriers gain the layout | **MET**: Int32 (4), Int64 (8), Uint8 (1), Uint32 (4), Uint64 (8), Uintptr (8), `noCopy` and `_` readonly at 0, value at 0; + the using line |
| `internal/fuzz/pcg.cs` | `pcgRand` (16) | **MET**: `noCopy` readonly at 0, `state` at 0, `inc` at 8; + the using line |
| `internal/runtime/atomic/package_info.cs` | map re-encode (using added) | **MET** |
| `internal/fuzz/<goos>/package_info.cs` | map re-encode (using added) | **MET** (the per-GOOS file on each target) |
| `sync/waitgroup.cs.auto` | not predicted | **MISSED**: `WaitGroup` declares a NAMED `noCopy noCopy` beside `atomic.Uint64` and `uint32` in go1.24.13, so the widened arc admits it (Size 16; `state` at 0, `sema` at 8) |

**Count MISSED (4 predicted, 5 measured), mechanism MET.** The fifth is a carrier my census could not see: its
glob read `.cs` files and `WaitGroup`'s emission lives in the `.cs.auto` review sibling of the hand-owned
`sync/waitgroup.cs`. Every hunk is the predicted shape (attribute, offsets, readonly, using, map); no falsifier
fired (no per-GOOS difference, no managed struct gained a layout). The LIVE `WaitGroup` is the hand-own's
managed shape (`WaitGroupState? st`), so the `.auto` change is review-only and moves no behaviour.

Hunk rule: `git merge-file -p <committed> <base emission> <cut emission>` per file (shared files from the windows
target, each fuzz `package_info.cs` from its own target); applied delta equals emission delta file for file;
residual drift committed-vs-base = 0 and applied-vs-cut = 0 on every file and every target.
