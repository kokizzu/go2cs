# net/http's close reading at go1.24.13 (a record)

Point-in-time record (2026-09-23). This is the H10 close seat's B3 reading of `net/http`, taken
immediately before the row moves to the candidates. It banks nothing; the row's re-emitted test
artifacts are committed beside it as candidate evidence.

- **Tree and configuration.** The close seat's tree at `47e088d3d7` (the batch-8g STAMP plus the
  docs-only close-precondition commit), through the tree's own `run-h10-recon.ps1` (blob
  `f17cc5b437`) with the row's roster pin applied: `execution: release-tiered`, which the wrapper
  maps to `-test-config Release -test-tiered`. Deadline 60m (floor 60m, asked 60m). windows/amd64,
  `CGO_ENABLED=0`, go1.24.13.
- **Reading.** DIVERGED: 1,387 verdicts on each side, 1,370 matching, 0 disclosed, 17 undisclosed
  divergences. Converter wall 376 s. The results file holds no deadline marker.
- **The divergences** are exactly the synctest class:
  - 11 `synctest.Run` infrastructure leaves, Go pass (one skip) against C# `infrastructure-error`:
    `TestNewClientServerTest/synctest/{h1,h2,https1}`, `TestServerShutdownStateNew/{h1,h2}`,
    `TestTransportIdleConnRacesRequest/{h1,h2unencrypted}` (Go skips `h2unencrypted`),
    `TestTransportRemovesConnsAfterBroken/{h1,h2}` and `TestTransportRemovesConnsAfterIdle/{h1,h2}`;
  - 6 aggregation parents, pass against fail: `TestNewClientServerTest`,
    `TestNewClientServerTest/synctest`, `TestServerShutdownStateNew`,
    `TestTransportIdleConnRacesRequest`, `TestTransportRemovesConnsAfterBroken` and
    `TestTransportRemovesConnsAfterIdle`.
- **TestRegisterErr.** It and all five of its subtests, `TestRegisterErr//a:&http.handler{i:0}`
  among them, read pass / pass under the pin.

## Files

- `rows.tsv` is the wrapper's row line, eleven columns, LF only.
- `results-tail.txt` is the results file's tail and headline facts, read before anything else.
- `go2cs_test_comparison.json` is the comparison record, reduced: `errors` and `stderr` are dropped
  because they carry the host's absolute paths. Every verdict field is kept: `go`, `csharp`,
  `skipped`, `disclosed`, `excluded`, `gated` and `environment`.
