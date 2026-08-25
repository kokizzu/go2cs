# Evidence — the AOT farm A/B session (raw data record)

> Lane G, 2026-08-25, the boundary session that closed the farm question YES. Summary and verdict
> live in the mailbox entry ("THE FARM QUESTION CLOSES YES") and the ratification that follows it;
> this file is the raw data record those entries cite. Design: alternating pairs (X,Y,X,Y,…) so
> slow thermal drift hits both sides of each pair equally; 1 discarded warmup per binary; medians
> of 5, matching the perf runner's own count protocol. One quiet session on the perf-canon host
> (Ryzen 5 PRO 6650U), session validity anchored by the Go control row.

## Binaries

| label | provenance | SHA-256 |
|:--|:--|:--|
| fib-pub1 | canon ladder publish #1 (11,862 s), preserved pre-overwrite | `9e8797df87c83976297da28452f94eb2ec53fe2b2b3c904ec84bee2fe61e9666` |
| fib-pub2 | canon ladder publish #2 (12,173 s), same tree/ILC/config, ~15 h later | `78a092bbe93ddc002fe07764be7cc67e12dd2495858c14190b5679d2afcd3f0c` |
| sieve-canon | canon ladder publish (12,869 s) | `13e1e1d23b8c1cf67f65412236acfcf89cfbb17832f1120e8a9f6ec39a1856a5` |
| sieve-i9 | i9 farm probe publish (6,157 s), shipped drop-dir, hash-verified before run | `b3f75657c35be585c003db76fd10a81e72cc7360c3ba52bb209a89b5b520a410` |

All four are 298,189,824 bytes. ILC 10.0.11 for all; SDK 10.0.400 both boxes.

## Raw runs (elapsed_ns, in-program workload time)

A/A null — Fib pub1 vs pub2 (known-equivalent binaries):

| run | fib-pub1 | fib-pub2 |
|--:|--:|--:|
| 1 | 175,696,200 | 172,924,400 |
| 2 | 171,413,000 | 171,866,500 |
| 3 | 171,746,700 | 175,241,600 |
| 4 | 190,598,500 | 173,765,300 |
| 5 | 178,086,200 | 173,631,500 |
| **median (ms)** | **175.70** | **173.63** |

A/B — Sieve canon-built vs i9-built:

| run | sieve-canon | sieve-i9 |
|--:|--:|--:|
| 1 | 232,501,600 | 234,367,300 |
| 2 | 235,832,800 | 233,071,600 |
| 3 | 231,482,300 | 232,077,600 |
| 4 | 235,837,000 | 237,558,000 |
| 5 | 231,219,400 | 231,741,200 |
| **median (ms)** | **232.50** | **233.07** |

Go control — Sieve (canon Go binary, same session): 68,016,000 / 66,214,700 / 62,106,800 /
61,607,400 / 66,829,400 → **median 66.21 ms**, inside the N4 quiet-box band (66.5/67.4).

## Verdict arithmetic

- A/A delta: |175.70 − 173.63| / 173.63 = **1.19 %** — the empirical null: what two
  known-equivalent binaries measure apart on this box, this protocol, this session.
- A/B delta: |232.50 − 233.07| / 232.50 = **0.25 %** — well inside the null → **YES,
  measurement-identical**. Mixed compile provenance is proven-acceptable for the canon table,
  recorded per-row in the README History note at bank time.

## Corroborating determinism observations (pipeline, not ILC-proper)

Byte-identity does NOT hold anywhere and proves nothing about measurement: Fib pub1 vs pub2
differ (same size to the byte); i9's IfaceShell re-publish re-hashed differently from its first
(their same-box A/A); sizes sit in a narrow band (298,184,192–298,638,336 across five rows) —
closure-dominated layout, embedded IDs varying inside a fixed frame (the C# rebuild mints fresh
MVIDs into the ILC's inputs each publish).
