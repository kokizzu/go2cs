# parsedebugvars at start: footprint PREDICTION

Written 2026-09-27, BEFORE any emission from the cut converter exists. Base: 1dae85e093. Cut:
c692b06bc1 (+ this file). The converter change is ONE registry row:
`manualConversionFuncs["runtime"]["enableWER"] = goosWindows`.

## Mechanism

A DISPLACEMENT that removes emitted code, on windows only. The converted body of `enableWER`
(signal_windows.go) is replaced by the converter's one placeholder line
(`// go2cs generated this placeholder — func enableWER is hand-converted ...`). The body lives in
windows/signal_windows_impl.cs, which is hand-owned and not emitted.

## Per target

| Target | Files | What changes |
|---|---|---|
| windows | 2 | runtime/windows/signal_windows.cs (the body → the placeholder); runtime/windows/package_info.cs (signal_windows.go's GoPositionMap line RE-ENCODED, because a removal moves positions) |
| linux | 0 | enableWER there is Go's own empty stub (nonwindows_stub.go), outside the goosWindows scope |
| darwin | 0 | same |

Line kinds:
- signal_windows.cs: the `internal static void enableWER() {` line, its body and its closing brace
  go (−7 at most). The placeholder comes in (+1).
- package_info.cs: exactly 1 − / 1 + on the signal_windows.go map line. There are 0 other lines.

Falsifiers:
- any linux or darwin file;
- any windows file other than these two;
- a map line REMOVED rather than re-encoded (signal_windows.cs keeps other mapped content, so the
  record must survive);
- any line in package_info.cs other than that one map line.

## Ranked uncertainties

1. Comments. The body's inner comment (`// re-enable Windows Error Reporting`) may be HOISTED and
   re-emitted standalone beside the placeholder, as the converter does for displaced bodies. The doc
   comment above the func may stay or go. This moves the signal_windows.cs COUNT (between −5/+1 and
   −8/+3) but not the file set or the line kinds. A count inside that band is MET.
2. The runtime1.cs callers of enableWER (all three targets) are unchanged: a call site does not
   depend on where the body lives.
