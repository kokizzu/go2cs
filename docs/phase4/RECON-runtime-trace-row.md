# RECON — the `runtime/trace` row (2026-09-04)

Point-in-time record, amended only by dated blocks. **Recon, not a fix**: nothing in this document
changes a line of the corpus, and the row's disposition is a SUGGEST list at the end for the
coordinator to rule on.

## What was run

Toolchain pinned and checked before the run: bare `go version` reads
`go version go1.23.12 windows/amd64`, and the value passed as `GOROOT` matched what `go env GOROOT`
prints, character for character. *The pin is proven by that version line alone, which carries no
path; this record quotes the pattern it censused and never a value matching one.*

The standard converted-test pipeline, all actions, at the Release default with a ten-minute package
deadline, over the corpus package `src/core/runtime/trace`.

## Prediction, posted before the run

Two verdicts; both tests reach one and both **fail cleanly** on a named tracing-not-supported error;
neither dies, neither infrastructure-errors, and the results tail carries no timeout event; the
`Example` is compile-only and the two benchmarks do not run at the default. Net 2 verdicts, 0
matching, 2 mismatches, 0 empty. Falsifiers named in advance: a death, an infrastructure error, an
empty verdict, or a timeout event in the tail.

## Measurement

**The prediction held on every point.** No falsifier fired.

| | |
|:--|:--|
| verdicts | 2 — `TestTraceStartStop`, `TestTraceDoubleStart` |
| C# | both `fail` |
| Go | both `pass` |
| matching | 0 · mismatching 2 · empty 0 |
| results tail | the normal package-`fail` event; **no `"action":"timeout"`**, so no deadline kill |
| records | comparison 2,229 B, results 1,185 B, XML 701 B — all three carrying this run's own mtime, preserved to distinct paths before any restore |

Both rows carry the same output, verbatim:

```
failed to start tracing: runtime error: tracing is not supported: the go2cs managed runtime has no execution tracer
```

## What stands behind the row

The package is two production files and three test files declaring exactly **two `func Test`**, one
`Example` with no `// Output:` comment (so it is compile-only and yields no verdict) and two
benchmarks (which do not run at the default). Both tests route through the runtime execution tracer:
`trace.Start` → `runtime.StartTrace`, the reader goroutine → `runtime.ReadTrace`, and `trace.Stop` →
`runtime.StopTrace`.

**Two of those three primitives are already hand-owned** (the per-GOOS `runtime/*/trace_impl.cs`
companions, registered `goosWindowsLinux`):

- **`StartTrace`** answers a named error rather than throwing. Go's tracer stops the world through
  `semacquire`, whose first step is `getg` — a per-thread runtime object the CLR does not have — so
  the auto body's first act was an unimplemented-intrinsic throw and every `trace.Start` surfaced as
  an infrastructure error. A capability the host cannot provide is honestly an error, not a crash.
- **`StopTrace`** is a no-op hand-own added 2026-09-02, precisely because `TestTraceDoubleStart`'s
  first statement is a bare `Stop()` before any `Start`, which previously threw through
  `traceAdvance` → `semacquire` → `getg`.
- **`ReadTrace`** stays auto and is unreachable: it is reached only from the goroutine `Start`
  spawns after succeeding, which on this host it never does.

So the row's two verdicts are not a gap waiting for a body — they are the measured, deliberate
consequence of a capability decision already taken and documented at both hand-own sites.

## What is NOT behind the row

The `internal/trace` parser is converted, but **this suite never imports it**. The test files' import
set is `benchmark`, `bytes`, `context`, `flag`, `fmt`, `log`, `os`, `runtime/trace`, `testing`,
`time`. The parser's own state therefore does not gate this row, and work there would not move it.

## Sizing

**This is a capability question, not a hand-own-scope question, and no floor-of-N estimate is
honest for it.** Per test class:

- **`TestTraceStartStop`** asserts the trace buffer is NON-EMPTY after a start/stop pair, and that
  nothing is written after the stop. Passing it requires a tracer that actually emits Go's event
  stream — not a stub that returns success.
- **`TestTraceDoubleStart`** asserts the second `Start` FAILS while the first succeeds. Passing it
  requires the tracer's enabled-state machine, which is meaningful only if starting the tracer means
  something.

Both therefore require the execution tracer itself, which is a serialization of the Go scheduler:
per-P buffers, stop-the-world, and `getg`. That is the entry cost, and it is not paid inside this
package.

## SUGGEST list

1. **Classify the row `untestable` by CAPABILITY**, with the per-test-class reason above recorded on
   the untestable ledger rather than left as a bare count. Its standing `0 of 2` then reads as a
   disposition instead of an open gap.
2. **Pin both rows by disclosure signature on the shared error text**, so a later change to that
   message is caught rather than silently re-shaping two verdicts.
3. **Record that the two hand-owns are load-bearing beyond this row.** Their own headers name the
   measured consumers elsewhere; retiring the error or changing its text moves those rows too, so
   the signature in (2) should be shared rather than duplicated per row.
4. **Do not spend on `internal/trace` for this row's sake** — the suite does not import it. If that
   package is wanted, it is wanted on its own merits.
5. **Consider one capability ruling covering the whole frontier.** The roster already places
   `runtime/pprof` and `net/http/pprof` behind the same wall; a single ruling with three named
   reasons is cheaper to hold than three rows drifting separately.

## Falsifiability

Every number above comes from the two preserved records, and the prediction that they score was
posted before the run. If a future host does provide a scheduler-level tracer, the falsifier is
simple: both rows go green without either hand-own changing, and this record is superseded by a
dated block rather than edited.
