# WriteDeadlineBudget — does the h2 write-deadline row pass at ANY budget?

Mirrors `testWriteDeadlineEnforcedPerStream` (GOROOT `net/http/serve_test.go:1008`) in h2 mode —
`WriteTimeout = timeout/2`, first request must succeed, second must error — but at budgets the real
test cannot reach. Go's `tryTimeouts` (`serve_test.go:980`) is hardcoded `{250ms, 500ms, 1s}`, so the
largest `WriteTimeout` it will ever set is 500 ms.

The discrimination: **passes at some budget = performance gap; fails at every budget = the deadline
spans the handshake where Go's does not.**

Measured on GRETCHEN-LAPTOP (2026-08-29), three runs, identical each time:

| budget | `WriteTimeout` | Go | converted C# |
|---|---|---|---|
| 250 ms | 125 ms | PASS | FAIL |
| 1 s | 500 ms | PASS | FAIL |
| 4 s | 2 s | PASS | **PASS** |
| 16 s | 8 s | PASS | **PASS** |

Takes an optional argument: a single budget in milliseconds (e.g. `500`).

```
go run .            /  go run . 500
go2cs -go2cspath <repo>/src <this-dir>
dotnet build -c Debug -p:go2csPath=<repo>/src/ -p:UseSharedCompilation=false
./bin/Debug/net10.0/WriteDeadlineBudget.exe [ms]
```
