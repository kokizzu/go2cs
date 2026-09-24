# c2-mnz-census -- which AllocsPerRun asserts reach bytealg.MakeNoZero (point-in-time record)

Input to [`DESIGN-allocation-counting.md`](../../DESIGN-allocation-counting.md) §9.1 (C2, 2026-09-24,
REC-F (iii)). A **dynamic** census. It runs Go's own tests on a patched copy of the go1.24.13 GOROOT, so
the population is the call paths Go really executes, with no call-graph approximation.

**The instrument ([`goroot-go1.24.13.patch`](goroot-go1.24.13.patch), three files):**
- `internal/bytealg` gains a window flag and two counters.
- `runtime.bytealg_MakeNoZero`, the implementation, counts calls and size-class bytes while the flag
  is set.
- `testing.AllocsPerRun` sets the flag around its measured runs only (the warm-up call is excluded). It
  prints one `MNZCENSUS` line per call: caller `file:line`, `runs`, Go's own `mallocs`, and the
  MakeNoZero calls and bytes.

**Positive control, before any zero was read:** a closure doing `Grow(18)` read 1 call and 24 B per run,
which is Go's size class.

```
cp -r "$(go env GOROOT)" <root> && chmod -R u+w <root> && (cd <root> && patch -p1 < goroot-go1.24.13.patch)
GOROOT=<root> PATH=<root>/bin:$PATH GOTOOLCHAIN=local go test -json -count=1 <packages> 2> err > run.json
# again with GOMAXPROCS=1 for the packages whose alloc tests skip on "GOMAXPROCS>1"
```

**Packages:** every std package whose tests contain `AllocsPerRun`, 46 of them, cmd excluded and
`syscall/js` unbuildable here. The run was on linux/amd64, a 4-core VM. Two failures, neither the
instrument's:
- net `TestLookupCNAME` (network);
- encoding/gob `TestCountDecodeMallocs` under `GOMAXPROCS=1`, which fails identically (5 vs 3) on the
  UNPATCHED go1.24.13 on this box.

[`census-linux-amd64.tsv`](census-linux-amd64.tsv): one row per `AllocsPerRun` call. The `pass` column
names the GOMAXPROCS the call ran under.

Two TSV rows name an IP-literal subtest of net/netip. Their subtest names are masked as
`<IP-prefix subtest>`, the convention of the H10 relabel record. Neither reaches MakeNoZero.
