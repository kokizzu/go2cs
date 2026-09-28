# Slice-bounds escape: sizing (2026-09-28). Probes and census at master ec23ad9f6b.

## Executed readings (single-project probes at -m:2, WHEA 0 on every run)
| route | shape | C# at master | Go |
|---|---|---|---|
| R1a | 2-index bound 1<<32+5 (slice or string) | SILENT `len 5`: `s[..(int)(wide)]` | slice bounds out of range [:4294967301] with capacity/length 10 |
| R1b | negative 2-index bound (slice or string) | CLR ArgumentOutOfRangeException (System.Index.op_Implicit); unrecoverable, process dies | [-1:] |
| R1c | string sliced past its end | CLR ArgumentException from golib @string.this[Range]; process dies | [:5] with length 3 |
| R1d | slice past its cap | correct (golib Reslice) | same |
| R2 | index on a string LITERAL | CLR IndexOutOfRangeException (`"abc"u8[(int)(i)]`); process dies | index out of range [5] with length 3 |
| R3a | &a[i], i uint64 or int64 = 1<<32+5 | SILENT WRONG ADDRESS: `Ꮡ(a, (int)(wide))` writes a[5] | index out of range [4294967301] |
| R3b | 3-index bounds | SILENT `len 5 cap 5`: `.slice(0, (int)w, (int)w)` | [::4294967301] with capacity 10 |
| R3c | type-parameter DIRECT index | correct: `(nint)(ConvertToUInt64<I>(i))` | same |
| R4-local | same-package pointer method on an element | correct: `a[(nint)(wide)].Add()` (ref indexer, `this ref` extension) | same |
| R4-foreign | foreign pointer method on a slice element (atomic.Uint64) | SILENT WRONG ADDRESS: `Ꮡ(a, (int)(wide)).Add(1)` increments a[5] | index out of range [4294967301] |
| R4-at | the same through a pointer-to-field (`Ꮡh.at(hist.Ꮡcounts, (nint)(wide))`) | index RIGHT, but golib ж.at throws a CLR IndexOutOfRangeException | index out of range [4294967301] |
Side defect: `var h hist` declared INSIDE a closure, with `h.counts[i].Add` there, emits the nonexistent
`Ꮡh` (CS0103).

## Census (go/packages, the converter's predicates; the probes are the positive controls; windows / linux / darwin, prod + test)
- R1 non-constant 2-index bounds: slice 2994+820 / 3009+858 / 3052+825; string 1098+262 / 1091+256 /
  1087+256. Totals 5174 / 5214 / 5220. Constant bounds (in range by Go's rules): about 300 per target.
- R2: 6+2 on every target.
- R3a: 116+4 / 134+4 / 131+4. R3b: 8 / 4 / 4. R4-foreign: 12 prod on every target. R4-local: 35 (correct).

## Candidate shapes
### Golib-only (0 emission footprint)
- G1: @string.this[Range] throws RuntimeErrorPanic.SliceBoundsOutOfRange (Go's text) instead of
  ArgumentException/ArgumentOutOfRangeException. Fixes R1c only, since the negative case never reaches golib.
- G2: ж.at<T>(nint) throws RuntimeErrorPanic.IndexOutOfRange(index, length) instead of IndexOutOfRangeException.
  Fixes R4-at.
  Both are behaviour changes on a failure path only, so the risk is none.

### C1's pattern (emission plus golib overloads), for R3a / R3b / R4-foreign, and R2 by its own helper
- An int64 index or bound takes `(nint)` (binds the existing nint overloads). An unsigned one is emitted BARE onto
  new ulong overloads: Ꮡ<T>(IArray<T>, ulong), Ꮡ<T>(slice<T>, ulong), Ꮡ<T>(array<T>, ulong), and the 3-index
  `.slice` bound set. Each checks the full unsigned value, then addresses as the nint form does.
- CS-ambiguity risk: LOW. An nint argument binds the nint overload exactly. A uint64 argument converts
  implicitly only to ulong. Wherever both nint and ulong apply (a narrower unsigned type), C#'s
  signed-over-unsigned better-conversion rule picks nint, with no tie, and every such value fits in nint. C1 already shipped the same shape for the
  indexers (this[ulong]) with no CS fallout. A type-parameter index keeps ConvertToUInt64, but lands on the ulong
  overload bare instead of `(int)(...)`.
- R2: `"abc"u8[(int)(i)]` becomes a golib helper `builtin.index("abc"u8, i)` (ReadOnlySpan<byte>, with nint and
  ulong overloads) that checks with Go's text and then indexes the span. No allocation (a `(@string)` cast
  would copy the literal on every call, and image/jpeg's tables are hot). Low CS risk: a u8 literal binds
  ReadOnlySpan<byte> exactly.
- PREDICTED EMISSION FOOTPRINT (sites; files to be listed at the cut): R3a + R3b + R4-foreign + R2 is 148 / 162 /
  159 (windows / linux / darwin). Every one is an in-line replace of a cast (or of a u8 indexer by the helper).

### R1: needs a DESIGN REVIEW before a cut
A C# Range cannot carry a negative bound (System.Index throws in the conversion, before any golib code runs) or
a bound past int32 (the emitted `(int)` truncates first). So no golib-only shape reaches R1a or R1b.
- R1-A (recommended): a non-constant bound routes to a golib nint method with Go's checks:
  `s.slice(lo, hi)`, `s.slice(lo)` and `str.slice(lo, hi)`, over slice<T>, array<T>, @string, sslice and
  sstring, and the named wrappers via go2cs-gen. It has exact Go text, because the method knows the length and
  capacity. Constant bounds keep today's Range syntax (no churn on about 300 sites per target). Footprint
  about 5,200 sites per target: every non-constant slice expression in the corpus. That is mechanical, but it is
  the largest emission change of the migration.
- R1-B: keep the Range syntax and wrap each non-constant bound in `GoIndex(x)`, which throws a Go panic for a
  negative or beyond-int32 value. The same footprint as R1-A, but the text lacks the length/capacity ("with
  capacity 10"), so it is worse than R1-A at the same cost.
- R1-C: G1 alone. Fixes only a string past its end.
Recommendation: land G1 + G2 and C1's pattern (about 160 sites) as one seat now. Put R1-A to a design review
with its footprint named, since its measured failures (silent truncation past 2^31, and an unrecoverable crash
on a negative bound) are real but need values that Go code rarely produces outside panic tests.

## At the cut (the seat on C1's unsigned-index base 05c0699fb2)
The seat takes G1, C1's pattern for R3a / R3b / R4-foreign, and R2. G2 is ALREADY on the base: C1's 2c97dd9bce
makes ж.at<T>(nint) throw RuntimeErrorPanic.IndexOutOfRange. R1-A goes to its design review. Three things
differ from the sizing above:
- The red test found two more silent routes, and the golib fix covers both with no emission change. The
  existing element-address overloads Ꮡ<T>(IArray<T>|slice<T>|array<T>, int|nint) checked NOTHING:
  - a Go `int` index (C# nint) past 2^31 narrowed with `(int)` and addressed a small element;
  - an index past the slice's LENGTH but inside its capacity addressed a hidden element.
  Every Ꮡ overload now checks the index at its full value against the length first (goPanicIndex), and the new
  ulong overloads do the same (goPanicIndexU).
- A 3-index slice takes `(nint)` for every wide bound, not ulong overloads. Its three bounds can mix kinds
  (`s[i:j:k]`, i int, j uint64), and no single unsigned overload set binds a mixed call. `(nint)` is exact below
  2^63. An unsigned bound at or above 2^63 still panics, but with a signed bound in the text; this is a stated
  residual.
- R2's helper is `LiteralByteAt(ReadOnlySpan<byte>, nint|ulong)`, not `index`. A local named `index` is common
  in Go code and would shadow a `using static` method.

## Predicted footprint, as a named file list
It is derived from the census sites: each Go file:line mapped to its converted file in the base tree, per GOOS
folder where the package has one. The counts are sites (files). Windows 148 (45), linux 162 (44), darwin 159
(43). 21 files in the union carry a hand-ownership marker. In five of them, the whole file's `.cs.auto` sibling
moves in place of the `.cs` (C1's footprint precedent):
- crypto/internal/boring/bcache/cache.cs
- internal/sync/hashtriemap.cs
- runtime/mfinal.cs
- sync/pool.cs
- sync/poolqueue.cs
The rest are per-function hand-owns, where a site inside a hand-owned body does not move. The -stdlib footprint
settles these, and anything beyond the list is named site by site.

| target | file (sites) |
|---|---|
| windows | compress/flate/deflate.cs (3) |
| windows | crypto/internal/boring/bcache/cache.cs (2) |
| windows | debug/elf/file.cs (12) |
| windows | encoding/xml/xml.cs (1) |
| windows | image/jpeg/writer.cs (3) |
| windows | index/suffixarray/suffixarray_test.cs (1) |
| windows | internal/bisect/bisect.cs (3) |
| windows | internal/sync/hashtriemap.cs (9) |
| windows | internal/syscall/windows/windows/security_windows.cs (2) |
| windows | internal/trace/internal/oldtrace/parser.cs (2) |
| windows | internal/zstd/block.cs (3) |
| windows | internal/zstd/huff.cs (2) |
| windows | os/user/windows/lookup_windows.cs (2) |
| windows | reflect/all_test.cs (1) |
| windows | regexp/backtrack.cs (1) |
| windows | regexp/exec.cs (1) |
| windows | regexp/onepass.cs (6) |
| windows | regexp/syntax/compile.cs (6) |
| windows | regexp/syntax/prog.cs (4) |
| windows | runtime/heapdump.cs (2) |
| windows | runtime/histogram.cs (1) |
| windows | runtime/iface.cs (2) |
| windows | runtime/mcentral.cs (4) |
| windows | runtime/mfinal.cs (3) |
| windows | runtime/mgcmark.cs (2) |
| windows | runtime/mpagealloc.cs (3) |
| windows | runtime/mprof.cs (16) |
| windows | runtime/mstats.cs (2) |
| windows | runtime/mwbbuf.cs (1) |
| windows | runtime/plugin.cs (1) |
| windows | runtime/symtab.cs (6) |
| windows | runtime/tracecpu.cs (1) |
| windows | runtime/tracemap.cs (1) |
| windows | runtime/traceregion.cs (2) |
| windows | runtime/tracestatus.cs (4) |
| windows | runtime/windows/malloc.cs (2) |
| windows | runtime/windows/mcheckmark.cs (1) |
| windows | runtime/windows/mheap.cs (5) |
| windows | runtime/windows/proc.cs (1) |
| windows | runtime/windows/sigqueue.cs (9) |
| windows | runtime/windows/syscall_windows.cs (1) |
| windows | runtime/windows/trace.cs (6) |
| windows | sync/atomic/atomic_test.cs (4) |
| windows | sync/pool.cs (1) |
| windows | sync/poolqueue.cs (3) |
| linux | compress/flate/deflate.cs (3) |
| linux | crypto/internal/boring/bcache/cache.cs (2) |
| linux | debug/elf/file.cs (12) |
| linux | encoding/xml/xml.cs (1) |
| linux | image/jpeg/writer.cs (3) |
| linux | index/suffixarray/suffixarray_test.cs (1) |
| linux | internal/bisect/bisect.cs (3) |
| linux | internal/sync/hashtriemap.cs (9) |
| linux | internal/trace/internal/oldtrace/parser.cs (2) |
| linux | internal/zstd/block.cs (3) |
| linux | internal/zstd/huff.cs (2) |
| linux | reflect/all_test.cs (1) |
| linux | regexp/backtrack.cs (1) |
| linux | regexp/exec.cs (1) |
| linux | regexp/onepass.cs (6) |
| linux | regexp/syntax/compile.cs (6) |
| linux | regexp/syntax/prog.cs (4) |
| linux | runtime/heapdump.cs (2) |
| linux | runtime/histogram.cs (1) |
| linux | runtime/iface.cs (2) |
| linux | runtime/linux/malloc.cs (2) |
| linux | runtime/linux/mcheckmark.cs (1) |
| linux | runtime/linux/mheap.cs (5) |
| linux | runtime/linux/proc.cs (1) |
| linux | runtime/linux/signal_unix.cs (16) |
| linux | runtime/linux/sigqueue.cs (9) |
| linux | runtime/linux/trace.cs (6) |
| linux | runtime/linux/vdso_linux.cs (3) |
| linux | runtime/mcentral.cs (4) |
| linux | runtime/mfinal.cs (3) |
| linux | runtime/mgcmark.cs (2) |
| linux | runtime/mpagealloc.cs (3) |
| linux | runtime/mprof.cs (16) |
| linux | runtime/mstats.cs (2) |
| linux | runtime/mwbbuf.cs (1) |
| linux | runtime/plugin.cs (1) |
| linux | runtime/symtab.cs (6) |
| linux | runtime/tracecpu.cs (1) |
| linux | runtime/tracemap.cs (1) |
| linux | runtime/traceregion.cs (2) |
| linux | runtime/tracestatus.cs (4) |
| linux | sync/atomic/atomic_test.cs (4) |
| linux | sync/pool.cs (1) |
| linux | sync/poolqueue.cs (3) |
| darwin | compress/flate/deflate.cs (3) |
| darwin | crypto/internal/boring/bcache/cache.cs (2) |
| darwin | debug/elf/file.cs (12) |
| darwin | encoding/xml/xml.cs (1) |
| darwin | image/jpeg/writer.cs (3) |
| darwin | index/suffixarray/suffixarray_test.cs (1) |
| darwin | internal/bisect/bisect.cs (3) |
| darwin | internal/sync/hashtriemap.cs (9) |
| darwin | internal/trace/internal/oldtrace/parser.cs (2) |
| darwin | internal/zstd/block.cs (3) |
| darwin | internal/zstd/huff.cs (2) |
| darwin | reflect/all_test.cs (1) |
| darwin | regexp/backtrack.cs (1) |
| darwin | regexp/exec.cs (1) |
| darwin | regexp/onepass.cs (6) |
| darwin | regexp/syntax/compile.cs (6) |
| darwin | regexp/syntax/prog.cs (4) |
| darwin | runtime/darwin/malloc.cs (2) |
| darwin | runtime/darwin/mcheckmark.cs (1) |
| darwin | runtime/darwin/mheap.cs (5) |
| darwin | runtime/darwin/proc.cs (1) |
| darwin | runtime/darwin/signal_unix.cs (16) |
| darwin | runtime/darwin/sigqueue.cs (9) |
| darwin | runtime/darwin/trace.cs (6) |
| darwin | runtime/heapdump.cs (2) |
| darwin | runtime/histogram.cs (1) |
| darwin | runtime/iface.cs (2) |
| darwin | runtime/mcentral.cs (4) |
| darwin | runtime/mfinal.cs (3) |
| darwin | runtime/mgcmark.cs (2) |
| darwin | runtime/mpagealloc.cs (3) |
| darwin | runtime/mprof.cs (16) |
| darwin | runtime/mstats.cs (2) |
| darwin | runtime/mwbbuf.cs (1) |
| darwin | runtime/plugin.cs (1) |
| darwin | runtime/symtab.cs (6) |
| darwin | runtime/tracecpu.cs (1) |
| darwin | runtime/tracemap.cs (1) |
| darwin | runtime/traceregion.cs (2) |
| darwin | runtime/tracestatus.cs (4) |
| darwin | sync/atomic/atomic_test.cs (4) |
| darwin | sync/pool.cs (1) |
| darwin | sync/poolqueue.cs (3) |
