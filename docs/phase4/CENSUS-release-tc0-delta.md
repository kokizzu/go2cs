# CENSUS — the roster at Release + tiering-off, against the Debug bank

> Measured 2026-09-02 by lane i9 on the fleet's bogo-capable Windows host, running
> `run-validated-sweep.ps1 -TestConfig Release` over **all 201 banked rows** in four shards.
> The committed proof pages under `docs/validation/current/` are the **Debug baseline** this delta
> is taken against.
>
> **Why it exists.** The owner ruled (2026-09-02) that the validation configuration of record becomes
> **Release with tiering off**, Debug remaining available by flag. That ruling needs to know what the
> roster actually does at the new default before the default moves — not whether one row moves, but
> whether the roster is the same roster. This census is that measurement.
>
> **Base commit: `ac385553e`** (lane branch `claude/i9-sweep-testconfig`, immediately before train 11
> landed). Every figure below is that tree, not master's current head — stated because a census whose
> layer is unnamed is the trap this repo's own doctrine names, and because train 11 landed mid-census.

## Verdict

**195 of 201 rows are unchanged at Release+TC0.** Six rows are flagged, and they do not all mean the
same thing: **four are findings about the configuration** (`net/http`'s two movers, the two TC0-only
residuals, and `crypto/tls`'s Release-only crash), **one is a configuration-INDEPENDENT regression**
the census merely surfaced (`errors`), and **one is arithmetic not yet attributed** (`sync`). **Two
rows** need a per-row opt-out for the flip to be safe, and both are measured rather than inferred.

| shard | rows | wall | log |
|---|---|---|---|
| 1/4 | 51 | 1,233 s | `i9-sweep-shard1of4-attempt3.log` |
| 2/4 | 51 | 1,601 s | `i9-sweep-shard2of4-attempt3.log` |
| 3/4 | 51 | 2,234 s | `i9-sweep-shard3of4-attempt3.log` |
| 4/4 | 48 | 1,622 s | `i9-sweep-shard4of4-attempt3.log` |

Sharded with a ten-minute cooldown between shards because this host's own thermal limit reboots it
under a continuous multi-hour sweep — the first attempt died that way at 13 minutes. Each shard ran
detached (`Start-Process -WindowStyle Hidden`, PID-polled positively); the corpus and
`docs/validation/current` were restored to HEAD after every shard.

## 1. Movers — verdicts that changed direction

### `net/http` — one favourable, one unfavourable

**Favourable, and it retires a disclosure.** `TestWriteDeadlineExtendedOnNewRequest` and both its
subtests (`/h1`, `/h2`) report `pass` on **both** sides at Release+TC0, and the row's `disclosed` list
is **empty**. At Debug this row carries a `performance-margin` disclosure — the founding row of that
class. At the new default it does not need one: the handshake fits the deadline when the code is
optimized and the JIT is not re-tiering underneath it.

This was predicted by name and in shape before the run ("*a genuine PASS at Release+TC0, not merely
disclosed under a different label*"), which is the only reason it is worth stating as a prediction
rather than a result.

**Unfavourable, and unpredicted.** `TestRegisterErr` — and its subtest
`TestRegisterErr//a:&http.handler{i:0}` — move `Go="pass" C#="fail"`. Not chased here; the record is
preserved. It is the one result that argues the flip is not free.

`TestTransportGCRequest` remains **excluded** (`requires unsupported …`) at both configurations, as
expected: a gate is about whether the host can run the declaration at all, not about timing.

## 2. TC0-only residuals — the flip's opt-out list

Two rows fail **only** because tiering is off, and both recover with it on. One variable, same host,
same converter, same `go1.23.12` oracle on both arms:

| row | Release + TC0 | Release + `-TestTiered` |
|---|---|---|
| `internal/godebug` | **FAIL** — `TestCmdBisect` `Go="pass" C#="fail"` | **PASS 5/5** (51 s) |
| `log/slog` | **FAIL** — `TestCallDepth` `Go="pass" C#="fail"` | **PASS 194/194** (38 s) |

Both are PC/line-attribution assertions, which is precisely what tiering's presence supplies. These are
the mirror of `internal/weak`'s existing `execution: release-tc0` annotation — that row opts INTO
tiering-off because its `codegen-liveness` assertions need it; these two opt OUT because theirs need
tiering. One vocabulary, two directions, each per-row and each measured.

**Both were predicted in prose by this repo's own doctrine** long before this census — the
`internal/weak` roster entry names "internal/godebug's line attribution" and "log/slog's pc=0" as the
reason `release-tc0` was made per-row opt-in rather than global. The census's contribution is that it
covers every banked row rather than the two anyone happened to look at, and after 201 rows those two
are still the only TC0-sensitive residuals.

## 3. Build regressions — surfaced, not caused

### `errors` — a banked row whose test assembly no longer builds

`CS0122: 'errors_package.is_typeᴛ1' is inaccessible due to its protection level`, at
`join_test.cs:49`. **Configuration-independent** — accessibility is a compile-time property and
`-test-config` never touches conversion, only publish and run.

The mechanism is the lift dedup crossing an assembly boundary: the banked `join_test.cs` declares its
own file-local `[GoType("dyn")] partial interface TestJoin_typeᴛ1`, while a fresh conversion deletes
that and rebinds the cast to the production assembly's same-shape `is_typeᴛ1`, which is `internal` —
invisible to the separate test assembly. That is the documented "deduplicated same-shape anonymous
structs" class **reappearing** after it was leveled, which the doctrine says is news rather than a
phantom to restore.

Found by this census only because a roster-wide `-tests` reconversion is the only instrument that
walks every banked row's test emission: CNR is transpile-only, and the stdlib solution compiles
production assemblies.

## 4. Unmeasured — host deaths, not verdicts

### `crypto/tls` — access violation in the bogo shim

The largest row on the roster produced no verdict. Its stream ends with a bare
`{"test":"","action":"fail","elapsed":332.46}` — no `timeout` event, no results files — and the
comparison record carries the cause:

```
use of closed network connection, child error 'exit status 0xc0000005', stdout: (empty),
stderr: flag provided but not defined: -on-resume-verify-fail
Usage of [...crypto.tls.tests.exe -port 64975 -shim-id 1747 -ipv6 -bogo-mode -resume-count 1 ...]
```

`0xc0000005` is an access violation. Two findings sit here, and the **first gates the flip**: the same
shim accepts `-on-resume-verify-fail` at Debug on this very host (the row banks 3,643 verdicts there
with the bogo suite live), so at Release a flag **registration** that exists at Debug is missing. The
leading hypothesis — Go's package-level `var x = flag.Bool(...)` registrations converting to static
field initializers on a `beforefieldinit` type, which an optimizing JIT may run lazily at first
static-field *access* rather than at package init — is corpus-wide if true. The second finding is that
an unrecognized flag produces an access violation where Go's shim exits 2.

Root not yet established; it is the next item after this census.

## 5. Infrastructure — settled, not counted

`internal/types/errors` failed shard 3 with `MSBUILD : error MSB4166: Child node "2" exited
prematurely` and zero CS diagnostics — the shape this repo classifies as build infrastructure rather
than a package root. `MSBUILDDISABLENODEREUSE=1` was already set, so the documented mitigation did not
prevent it. Re-run in isolation at `-TestConfig Release`: **PASS 155/155, 71 s.**

Not a finding, and not counted — which is why shard 3 reads 49/2 in the record above and 48/3 in its
own log.

## 6. Open arithmetic

`sync` reports **47 matching verdicts against 44 banked** with a completely clean comparison
(`"status": "validated"`, `"matched": true`, zero divergence entries). The surplus of three is not yet
attributed.

One reading was considered and **rejected on measurement**: that the row's committed `TestOnceXGC`
disclosures had become unnecessary. They have not. At Release+TC0 the C# side still reports
`TestOnceXGC`, `/OnceFunc`, `/OnceValue` and `/OnceValues` as `fail` against Go's `pass`, absorbed by
those disclosures exactly as banked. The disclosure's own explicitly testable claim — that the failure
*"holds in fully optimized code, not just under the non-optimizing JIT"* — is **confirmed** by this
census.

## What this census does and does not license

It licenses the flip's **shape**: Release+TC0 as the default with a per-row `release-tiered` opt-out,
because the opt-out list is two rows and both are measured.

It does **not** license the flip **yet**, on its own evidence: `crypto/tls`'s Release-only crash is a
missing flag registration on the roster's largest row, and its root lands first.

Records preserved for every flagged row (`crypto.tls`, `internal.godebug`, `log.slog`, `net.http`,
`sync`) before each restore.
