# i9-slice-bounds-shapes

The standing probe for Go's **2-index slice bounds** under go2cs (`x[lo:]`, `x[:hi]`, `x[lo:hi]`): 29 failure and
control shapes, one per process on the C# side. It was written for the R1-A design review
(`docs/phase4/DESIGN-slice-bounds-r1a.md`, claude/i9-r1a-design) and serves as the gate for any seat that touches
golib's slice paths or the converter's slice emission. COORD asked for it as a standing ref on 2026-09-29.

## What it covers

Every shape has an expected go1.24.13 reading (`reading-go-go1.24.13.txt`):

- the receivers: slice, named slice, array, pointer-to-array, named array, string, named string, and a string
  literal;
- the bounds: negative, past the capacity or length, low past high, `1<<32+5` as `int`, `int64` and `uint64`
  (`1<<64-1`), and `uint32`;
- the texts: each shape prints Go's panic text and whether the recovered value is a `runtime.Error`.

The string-literal cases assign through a `string` variable. A literal sliced straight into an `any` does not compile
under go2cs (CS0029, a `ReadOnlySpan<byte>` into `object`), which is a separate, queued emission defect.

## How to run

```
bash run-probe.sh <go2cs worktree> <outdir>
```

- It needs `go` (the pinned toolchain), `dotnet` and `pwsh` on PATH, and a worktree with no tracked changes.
- It stages the probe as an untracked behavioral project, transpiles and compiles it, and runs each case in its own
  process. It then removes the staged project.
- Read `outdir/cs.txt` against `outdir/go.txt` for "equals Go". Diff two trees' `cs.txt` for a gate; the right control
  is the same tree without the seat under test.

## Recorded readings (windows, Release, go1.24.13)

| file | tree | matches go run |
|---|---|---:|
| `reading-cs-master-2ff42f7a16.txt` | master `2ff42f7a16`, before R1-A | 1 of 29 |
| `reading-cs-r1a-da81accf77.txt` | TRAIN I run 2 + R1-A commit 1 + g-inline-locations (`da81accf77`) | 15 of 29 |
| `reading-cs-a8-7eeb468c76.txt` | the same + C2's A8 (`7eeb468c76`) | 15 of 29, byte-identical to the row above |

R1-A commit 1 (golib only) made every golib-raised slice-bounds panic Go's `runtime.boundsError`, with Go's texts,
arrays included. The 14 shapes still differing after it are what R1-A commit 2 (the converter's `.slice(...)`
emission) exists to remove:

- 10 CLR exceptions that escape `recover()`: negative bounds, `(int)` of a `uint64` past 2^63, and any out-of-range
  string-literal bound;
- 2 silent truncations: `s[:big]` and `s[:i64]` read `len 5`;
- 2 truncated texts: `s[big:]` prints `[5:3]`, and `str[:big]` prints `[:5] with length 3`.

This directory is exempt from line-ending normalization (`.gitattributes`), so the probe runs byte-identically on
every host.
