# DESIGN — the `object-lifetime` disclosure class: when a test asserts WHEN the collector acts

*Lane G, 2026-08-23. Commissioned by the coordinator (mailbox, queue-deepening directive; reassigned
G 2026-08-23). Docs-only; merges under any freeze. Status: **PROPOSED** — §7 collects the OQs.*

The managed object-lifetime divergence recurs across rows — `crypto/tls` `TestCertCache`,
`runtime/debug` `TestFreeOSMemory`, the finalizer-dependent family — and this document gives it the
formalization `runtime-capability` got (board ruling, 2026-08-20): a class definition with an
admission test, the disclosure shape including per-OS scoping, the constituency censused from the
roster, and open questions for ruling. The model is deliberately that ruling's: *a disclosure names
what is provably unsatisfiable, never what is unimplemented*, and every candidate row below is that
bar applied once.

---

## 1. What the class is — and the boundary that keeps it small

Go's runtime makes two promises this project's replacement runtime cannot make together:

1. **Per-safepoint liveness.** Go's GC liveness maps drop a local at its *last use*, so a test can
   nil its own local mid-frame and observe the object become unreachable *while the frame still
   runs*. The CLR's GC info reports an address-exposed slot live for the *whole method* — measured
   in fully optimized Release code, not a JIT-tier artifact (`TestCertCache`, `TestOnceXGC`,
   `TestFreeOSMemory` disclosures, each with its own A/B).
2. **Prompt, observable finalization.** Go's `runtime.GC()` + finalizer contract lets a test drive
   an unreachable object's finalizer to *completion within the test's own patience window* and
   observe its side effects. The CLR promises eventual finalization on an unspecified thread at an
   unspecified time; go2cs narrows it (`runtime.GC()` = `Collect` → `WaitForPendingFinalizers` →
   `Collect`, and `SetFinalizer` is the hand-owned `ConditionalWeakTable`-and-sentinel bridge), but
   *narrowed* is not *promised*.

**The class named here covers assertions that depend on promise 2 — the WHEN.** Assertions that
fail because of promise 1 — the object *cannot become unreachable at all* while the asserting frame
runs — are already named, mechanically and correctly, by **`codegen-liveness`**, and this design
does **not** absorb them (§7 ⟨OQ-L1⟩ puts the sibling-vs-umbrella question to ruling, with a
recommendation).

The boundary matters because the corpus proves the finalizer bridge **works**:

* `sync`'s banked `TestPoolGC` measures **98 of 100** objects finalizing on the first
  `runtime.GC()`, the test's own retry loop absorbing the rest.
* `io`'s `TestMultiReaderFinalizer`-family row (`multi_test.go`'s `SetFinalizer` on a `*bytes.Reader`)
  **passes** on the banked roster.
* `sync`'s `map_test.go` finalizer-driven cleanup **passes**.
* The board's netpoll instrumentation observed **21 finalizer-driven `close` calls per three suite
  runs** behaving correctly.

A class whose admission test is loose enough to admit those rows would be laundering passing
behavior into disclosures. The admission test below is written to refuse them.

## 2. The admission test

The commissioning directive posed it as *"does the assertion depend on deterministic
finalization/GC timing the CLR cannot promise?"* — the right question, refined here into the form
the manifests can apply, one clause per word that does work:

> **A row is admitted to `object-lifetime` iff its assertion fails — or cannot complete — unless
> collection or finalization of a specific object is OBSERVABLE at a specific point the test
> chooses, AND the object is genuinely unreachable at that point, AND no retry/patience window the
> test itself provides is sufficient under the bridge's real behavior.**

Clause by clause:

* **"observable at a specific point the test chooses"** — the divergence is about *when*, not
  *whether*. A test that merely *uses* `SetFinalizer` for cleanup, GC pressure, or leak detection
  (the `time` timer-stress loops, the compress benchmarks' `StartTimer` hygiene, `os/exec`'s
  `execwait` leak probes) is refused: nothing in its assertion depends on the collector's schedule.
* **"genuinely unreachable at that point"** — if the object is still rooted by the asserting
  frame's own address-exposed slot, the row is `codegen-liveness`, not this class: the failure is
  structural (the object *cannot* be collected), not temporal (it *wasn't yet*). This clause is
  the sibling boundary, and it is why the three currently-disclosed families stay where they are.
* **"no retry window the test provides is sufficient"** — `TestPoolGC`'s 98-of-100-plus-retries is
  the measured proof that a patience window usually suffices. A row is admitted only with an A/B
  showing the bridge cannot satisfy it *within the window the test itself allows* — the same
  measured-not-asserted bar every `codegen-liveness` reason already meets.

**Anti-laundering clause, binding as `runtime-capability`'s:** widening a test's own retry loop,
lengthening its timeout, or inserting extra `runtime.GC()` calls into converted test code to make
a row pass — or to make it fail more legibly — is forbidden by this class's text. The conversion
runs the test Go wrote; the manifest discloses what that test cannot see.

## 3. The constituency, censused from the roster

Census method: the 162-row roster (`docs/ValidatedTestPackages.md`) cross-referenced against the
GOROOT test sources for `SetFinalizer` / `weak.` / `KeepAlive` (the lifetime-asserting
vocabulary) and `runtime.GC()` (the wider net), then each hit classified against §2. Numbers are
this census's; re-derive at consumption per the inventory discipline.

### 3a. Currently disclosed, correctly, as `codegen-liveness` — NOT this class's members

| Row | Package | Why it stays |
|---|---|---|
| `TestOnceXGC/{OnceFunc,OnceValue,OnceValues}` | `sync` | by-value slice header → address-exposed caller temp, live whole frame |
| `TestCertCache` | `crypto/tls` | two-result call materializes an address-exposed temp; fails at the FIRST check in optimized Release |
| `TestFreeOSMemory` | `runtime/debug` | 32 MB rooted by the asserting frame; three-way control measured (returned-call form releases to the byte) |

Every one fails §2's *"genuinely unreachable"* clause — the object cannot be collected while the
frame runs, deterministically. Reclassifying them would trade a mechanism-precise name for a vaguer
one and lose the A/B evidence each reason carries.

### 3b. Refused by §2 — lifetime-adjacent rows that PASS, listed so the class cannot creep

| Package | What its GC-touching tests do | §2 clause that refuses |
|---|---|---|
| `sync` (`TestPoolGC`, map finalizer) | finalization with the test's own retry window — **passes** | retry window sufficient |
| `io` (multi-reader finalizer) | `SetFinalizer` observed via the test's completion — **passes** | retry window sufficient |
| `time` | `runtime.GC()` as background pressure in timer-stress loops | nothing asserts the schedule |
| `compress/flate`, `compress/lzw` | benchmark hygiene between `StopTimer`/`StartTimer` | nothing asserts the schedule |
| `image/gif` | `MemStats` around decode — alloc territory (`alloc-profile` if it ever diverges) | nothing asserts the schedule |
| `os/exec` | `runtime.GC()` to drive `execwait` leak-probe finalizers | leak probe, not a schedule assert |
| `sync/atomic`, `internal/reflectlite` | GC hygiene / liveness-shaking inside helpers | nothing asserts the schedule |
| `crypto/rsa`, `crypto/tls` (`KeepAlive` uses) | `KeepAlive` *extends* lifetime — the direction the CLR honors trivially | nothing asserts collection |

### 3c. The open seat, and the mechanism gap that keeps it open

**`internal/weak` — off-roster, and the reason is a disclosure-MECHANISM gap, not a divergence
question.** Its first-ever run measured 1 of 3: `TestPointerEquality` — the canonicalization
clause, the hardest — **passes** end to end. `TestPointer`/`TestPointerFinalizer` fail on frame
rooting, but `TestPointerFinalizer` does not *fail an assertion*: it **blocks forever** on
`<-done`, awaiting a finalizer a still-rooted object can never queue. The disclosure oracle
reclassifies a *failure whose captured output contains the pinned signature*; a hang has no
output to pin and surfaces as a package timeout. **No hang-shaped divergence can be disclosed
today, whatever class it belongs to.** §7 ⟨OQ-L3⟩ puts the remedy to ruling; until it rules,
`internal/weak` cannot bank regardless of how this class is drawn — which is precisely the kind of
fact a constituency census exists to surface.

### 3d. Expected future members

The class is minted with **zero immediately-admitted rows** (⟨OQ-L1⟩'s recommendation keeps 3a
where it is), and that is a feature: the same was true of `runtime-capability` beyond its founding
member. Future members arrive from two directions: (1) roster growth into `runtime`'s own suite
and the `weak`/`unique` packages, where §2-shaped asserts are dense; (2) any 3a row whose
`codegen-liveness` premise *dissolves* — if a future CLR tracks address-exposed-slot lifetime, the
structural pin retires and the row re-measures against the *timing* promise instead, landing here
if it still diverges.

## 4. The disclosure shape

### 4a. The entry, unchanged where it can be

The existing five-field shape (`name`, `class`, `signature`, `reason`, optional
`hostConditional`) carries `object-lifetime` rows without modification, and the field disciplines
transfer verbatim: the signature pins the exact divergence so any *other* failure of the same test
stays a regression; the reason must carry the A/B (§2's third clause makes one mandatory — "no
sufficient retry window" is a measurement, not an opinion).

### 4b. Per-OS scoping — the schema addition, and why this class forces it

The commissioning directive requires per-OS manifests, and this class is where the need is
structural rather than hypothetical: collection and page-release behavior legitimately differ by
platform (workstation vs server GC defaults, Linux `MADV_FREE`-style decommit vs Windows working-set
trim — the very surface `TestFreeOSMemory`'s passing first assert measures), so a row can diverge
on one GOOS and pass on another. Today the schema cannot say so: a manifest is one file per
package, consulted identically by every platform's sweep, and a row disclosed for a linux-only
failure would *silently widen the oracle on windows*, where the same test passing strictly is the
verdict we want recorded.

**Proposed: a `goos` field on the entry, not a per-OS file.**

```json
{
  "name": "TestX",
  "class": "object-lifetime",
  "goos": ["linux"],
  "signature": "…",
  "reason": "… (measured on linux: …; windows passes strictly, measured …)"
}
```

* Absent `goos` = all platforms — every existing manifest keeps its exact meaning, no migration.
* A scoped entry is consulted only when the run's `TargetGOOS` (already in the run manifest) is
  listed; elsewhere the test compares **strictly**, so a windows pass stays a real pass and a
  windows failure is a real failure — the oracle never widens where the divergence was not shown.
* One file per package, not `go2cs_test_disclosures_linux.json` siblings: the per-OS-files
  alternative forks the reason prose per platform, invites drift between copies of the same row,
  and breaks the existing loader/validator/proof-page pipeline, which is one-file-shaped
  throughout. The field is ~20 lines in `loadTestDisclosures` + the oracle's consult; the sibling
  files are a pipeline change. (⟨OQ-L4⟩ takes the counterargument seriously.)
* Validation rule: a `goos` value outside the three swept platforms is an error, same
  fail-loudly posture as the loader's existing required-field checks.

### 4c. Proof-page rendering

A scoped row renders with its scope (`disclosed (linux)`), and the per-OS verdict lines the roster
already carries (`bytes` row: "linux: 86") absorb the arithmetic naturally. No new page machinery.

## 5. What this class is NOT — the refusals, stated as the ruling's were

* **Not a home for `codegen-liveness` rows** (§3a; ⟨OQ-L1⟩). Different promise broken, different
  evidence shape, and the existing name is mechanism-precise.
* **Not a home for hangs** — not because they don't belong conceptually (`TestPointerFinalizer` is
  §2-shaped to its core) but because the *mechanism* cannot express them yet (⟨OQ-L3⟩). Minting a
  class member the oracle cannot enforce would be a disclosure in prose only.
* **Not a patience knob.** The anti-laundering clause forbids widening any test's own window. If
  the bridge can satisfy a row by being *better* (a `WaitForPendingFinalizers` placement fix, a
  collection-mode change), that is an arc with a price, never a disclosure — `runtime-capability`'s
  own boundary, applied here.
* **Not `hostConditional`'s territory.** That field tolerates a non-deterministic *Go* side; this
  class discloses a deterministic *C#* inability. A row needing both carries both, and they answer
  different auditors.

## 6. Consumers and cost

Docs-only at this stage: minting the class costs nothing until a row is admitted (the class
vocabulary lives in the manifests, not in code — the loader validates fields, not class names).
The one code change this design *proposes* (not lands) is §4b's `goos` field: loader + oracle
consult + proof-page annotation, CNR-invisible, gated by its own unit tests when a lane takes it.
⟨OQ-L3⟩'s hang remedy is priced separately by whichever design answers it.

## 7. Open questions for ruling

* **⟨OQ-L1⟩ — sibling or umbrella?** Recommendation: **sibling**. `codegen-liveness` keeps its
  three families; `object-lifetime` admits only §2-shaped rows. The umbrella alternative (rename
  `codegen-liveness` rows under a broader class with a `mechanism` sub-field) buys one fewer class
  name at the cost of re-touching five banked manifests and blurring the structural/temporal
  boundary §2 leans on. If ruled umbrella, the migration is mechanical and this document's §1–§2
  become the umbrella's text.
* **⟨OQ-L2⟩ — is the class minted empty?** Recommendation: **yes** (§3d). The alternative —
  waiting for the first admitted row — leaves the admission test unratified exactly when the
  roster reaches the packages where it will be needed under pressure.
* **⟨OQ-L3⟩ — the hang shape.** A `TestPointerFinalizer`-shaped divergence cannot be disclosed:
  no output, no signature. Candidate remedies, for pricing not ruling here: (a) a `shape: "hang"`
  entry the oracle matches against a *timeout* verdict for that specific test, with the
  package-deadline machinery attributing the timeout to the named test; (b) a converted-side
  test-host watchdog that converts a per-test deadline into a failure with synthesizable output,
  which the ordinary signature then pins. (b) is more honest (the row FAILS, visibly, with text)
  but touches the test host; (a) is smaller but pins on absence-of-output, which is a weaker
  integrity guard. Recommendation: **(b)**, priced as its own small arc — it also benefits every
  future hang, disclosure-bound or not.
* **⟨OQ-L4⟩ — `goos` field vs per-OS manifest files.** Recommendation: **field** (§4b). The
  strongest counterargument is that a per-OS file keeps each platform's oracle input
  byte-auditable in isolation; the answer is that the field is *more* auditable — one row, one
  reason, its scope explicit — and the loader's validation makes an out-of-vocabulary scope loud.
* **⟨OQ-L5⟩ — does `TestFreeOSMemory` eventually migrate?** Its pin is `codegen-liveness` (frame
  rooting, measured), but its *second* assert also depends on decommit behavior that is per-OS in
  exactly §4b's way. If the frame-rooting premise ever dissolves (⟨OQ-L1⟩'s re-measure path), the
  row is this design's first live test of the `goos` field. Nothing to rule now; recorded so the
  re-measure isn't a surprise.

---

*Census sources: `docs/ValidatedTestPackages.md` (162 rows), GOROOT test sources at go1.23.1,
the five committed `codegen-liveness` disclosure reasons (each carrying its own A/B), board
entries for `internal/weak`'s first run and the netpoll finalizer instrumentation. Re-derive
counts at consumption; the classifications are the durable part.*
