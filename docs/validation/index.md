# Validation proofs

One page per validated package — the full per-test differential behind its row in
[Validated Test Packages](../ValidatedTestPackages.md): every eligible `Test` function, `go test`'s
verdict and go2cs's, side by side. The converter writes these pages itself, from the same
comparison record that decides whether a package validates at all.

Pages under `current/` are living proof — regenerated only when a package's verdicts change.
Versioned sibling directories are frozen publication snapshots: written once at release and never
rewritten, so the proof link for a published package stays the proof as of that binary.

| Package | Proof | Converted package |
|:--|:--|:--|
| `cmp` | [`cmp.md`](current/cmp.md) | [`src/core/cmp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/cmp) |
| `io` | [`io.md`](current/io.md) | [`src/core/io`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/io) |
