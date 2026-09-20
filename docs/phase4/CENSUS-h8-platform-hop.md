# CENSUS — H8's platform axis across the 1.23.12 → 1.24.13 hop

**Record, 2026-09-20.** Point-in-time; amended with dated blocks, never rewritten, never executed from.
The procedure is [`docs/GoCorpusMigration.md`](../GoCorpusMigration.md) §H8 and its amendments; the gate
rulings are COORD's on the mailbox. This file is the measurement.

| | |
|---|---|
| incoming release | **go1.24.13**, census base `46307b4704f6b7b1b608c8be0cf6f59e1d67ff26` (the version branch), `version.props` 1.24.13 |
| outgoing release | **go1.23.12**, census base `7105c846849d234743a5adee7a36255b55790e65` (master), `version.props` 1.23.12 |
| converter | one binary for both, sha256 prefix `093a6328b71883bb`, built at the incoming base |
| targets | `windows/amd64`, `linux/amd64`, `darwin/amd64`, sequential, each into its own seeded root |
| flags | `-stdlib -comments -platform-census`, build tags `purego`, `math_big_pure_go` |
| `CGO_ENABLED` | **0** on both — measured from the emission: `runtime/cgo` emits 0 `.cs` on every target and is absent from both seeds |
| walls | incoming **802 s**, outgoing **1116 s** |

**The axis.** The two runs differ in exactly two coupled things — `GOROOT` and the tree whose
`version.props` names the release. Everything else is held: the same converter binary by sha256, the same
flags, the same seeding shape, never-reused output roots, one conversion at a time. The pin is asserted
from `go version` **OUTPUT**, with the other SDK as a control proving that assertion can fail. The
converter refuses a tree whose `version.props` disagrees with the pin, so a run that *completes* against a
tree is itself the assertion that the tree is the release it claims.

---

## 1. Class counts, both releases

The four classes are `platformCensus.go`'s partition: a name emitted by every target is `identical` or
`variant` by content; by exactly two, `partial`; by exactly one, `exclusive`.

| class | outgoing 1.23.12 | incoming 1.24.13 | Δ |
|---|--:|--:|--:|
| **shared** — `identicalOnAllTargets` | 1476 | 1631 | **+155** |
| **variant** — `sameNameDifferentContent` | 79 | 83 | **+4** |
| **partial** — `emittedBySomeButNotAll` | 87 | 93 | **+6** |
| **exclusive** — `platformExclusive` | 277 | 283 | **+6** |
| union — `unionEmittedCs` | 1919 | 2090 | +171 |
| packages emitting | 303 | 340 | +37 |
| packages with any delta | 37 | 37 | 0 |
| variant by kind — source / `package_info` / `package_init` | 47 / 28 / 4 | 51 / 28 / 4 | +4 / 0 / 0 |
| `packagesWithDifferingCsproj` | 5 | 4 | −1 |
| emitted `.cs` per target — W / L / D | 1659 / 1727 / 1730 | 1823 / 1892 / 1896 | +164 / +165 / +166 |
| `packagesQueued` per target — W / L / D | 304 / 302 / 303 | 344 / 342 / 343 | +40 / +40 / +40 |

**Partition control, both sides.** 1476 + 79 + 87 + 277 = 1919; 1631 + 83 + 93 + 283 = 2090. The classes
are disjoint and complete, so nothing is double-counted or lost.

**Derived twice, by different code.** The rows are the converter's own `platform-manifest.json`. Per-file
manifests put through `h8-comparand.sh classify` reproduce **both** sides exactly, and its `compare`
reproduces the Δ column independently.

**Pairwise.**

| pair | emitted by both | identical | differ | packages touched |
|---|--:|--:|--:|--:|
| W ↔ L, outgoing → incoming | 1560 → 1720 | 1487 → 1643 | 73 → 77 | 34 → 34 |
| W ↔ D | 1563 → 1723 | 1488 → 1643 | 75 → 80 | 35 → 36 |
| **L ↔ D** | **1629 → 1792** | **1585 → 1746** | **44 → 46** | **20 → 20** |

Linux and darwin remain far closer to each other than either is to windows, across the hop — the property
the third platform's cheapness rests on.

**L3 pricing.** Outgoing: flat 1476 + per-GOOS 688 = 2164 against a single-platform tree of 1659, **+30.4 %**.
Incoming: flat 1631 + per-GOOS 718 = 2349 against 1823, **+28.9 %**. The growth ratio *fell* slightly
across a hop that added 171 artifacts.

---

## 2. The gate

**Clause 1 — the marker gate is zero per target, on both releases.** `markerGateViolations` is empty on
all six target-runs. Re-derived from the definition rather than read off the array, with the population
predicted from the **rule** (the marker matches with or without the `go.` qualifier and with or without the
`Attribute` suffix) and not from one spelling:

| | outgoing | incoming |
|---|--:|--:|
| seed hand-owns by the rule | **147** = 124 `go.`-prefixed + 23 unprefixed | **153** = 131 + 22 |
| disjointness control | 124 + 23 = 147 ✓ | 131 + 22 = 153 ✓ |
| marker lost at that path, W / L / D | 0 / 0 / 0 | 0 / 0 / 0 |
| seed path absent from the emission | 0 / 0 / 0 | 0 / 0 / 0 |
| `seedManualConversionFiles` in the manifest | 147 | 153 |
| negative control — a marker that does not exist | 0 files | 0 files |

The predicate was **made to fail**: a real hand-own copied out and its marker line stripped is admitted
clean and refused stripped. The hand-own population itself moves **+6** across the hop (124 → 131
prefixed, 23 → 22 unprefixed), which is the hop's hand-own work and not a gate movement.

<!-- The first run of this instrument keyed the emission paths one directory level too high (the roots nest
     the corpus under a further `core` segment). It printed "marker LOST 0" — a clean-looking zero — while
     "path absent from emission" read 153: it had compared nothing and the violation counter was
     structurally dead. The zero was void and the OTHER counter is what exposed it. A single-counter
     instrument here would have banked a false green. -->

**Clause 2 — the default flavour reproduces the single-target build byte-for-byte. PASS on all three
flavours**, on real E1/E2 pairs.

| flavour | E1 artifacts | E2 artifacts | only in E1 | only in E2 | content differs | verdict |
|---|--:|--:|--:|--:|--:|---|
| windows/amd64 | 3342 | 3342 | 0 | 0 | 0 | **PASS** |
| linux/amd64 | 3397 | 3397 | 0 | 0 | 0 | **PASS** |
| darwin/amd64 | 3395 | 3395 | 0 | 0 | 0 | **PASS** |

Tree hashes equal on each; both sides non-empty on each, which the instrument asserts.

**E2 is ONE merged L3 corpus viewed at three hosts** — a single three-target emission (rc 0, 1057 s)
re-used for all three flavours. This is sharper than three independent pairs: a host-biased merge shows up
precisely under three views of one corpus, whereas three separate merges could each be self-consistently
wrong. **E1 is a fresh single-target emission per flavour** from the same base and binary — windows 387 s,
linux 425 s, darwin 309 s.

**The controls that stop a PASS being vacuous.** Three host views of the *same* corpus must disagree:

```
  windows vs linux    rc 1   only-in-windows 227   only-in-linux  282   content-differs 0
  windows vs darwin   rc 1   only-in-windows 227   only-in-darwin 280   content-differs 0
  linux   vs darwin   rc 1   only-in-linux   282   only-in-darwin 280   content-differs 0
```

All three fire. Positive control: `identity` of a view against itself returns rc 0.

**A property the controls hand over free:** `content-differs` is **0** in all three cross-flavour
comparisons. Where two flavours share a path the bytes are identical; the entire difference between
flavours is *which files are present*. That is layout L3 behaving as designed — platform-varying content
lives at per-GOOS paths and never collides in a flavour view — observed independently of the census.

<!-- The first cross-flavour control loop packed each comparison into a colon-delimited string and split on
     `:`. These are Windows paths, so `C:` shattered every field; the loop compared wreckage, grep failed on
     a filename assembled from the pieces, and it printed "CONTROL FIRED" anyway because the message was
     gated on a meaningless rc rather than on anything it had checked. The ARMS were never affected — they
     take explicit arguments — but the controls were void while reading green. Re-written without packed
     fields and re-run; the numbers above are the corrected instrument's. -->

---

## 3. The predictions, scored as worded

### 3.1 The package delta — MET exactly

C2's `pkgdelta` predicted net **+40 packages on every target**. Measured on the converter's own census, on
a different host and instrument: **+40 on all three** (304 → 344, 302 → 342, 303 → 343), no residual. The
absolute counts sit 2 below C2's 306 / 304 / 305 because the census queues a slightly different set than a
raw `std` listing; the Δ, which is what was predicted, is identical.

**This matters for reading the misses below.** The package axis is *correct*. P1 and P3 are derived from
it and extrapolated to file-level classes, and both miss for one shared reason: **Go's own per-file
build-tag selection changed inside packages that exist on all three targets and were never in the added or
removed sets at all.** A package-membership derivation is structurally blind to that, and the blind spot is
now measured: **+20 source artifacts on P3, +6 partial on P1.**

### 3.2 P1 — `Δ partial = 0`. **REFUTED**, measured +6

Seven arrived, one departed:

```
  + internal/poll/sendfile_unix.cs            + os/root_nonwindows.cs
  + internal/syscall/unix/eaccess.cs          + os/root_unix.cs
  + os/eloop_other.cs                         + runtime/vgetrandom_unsupported.cs
  + vendor/golang.org/x/sys/cpu/cpu_other_x86.cs
  - crypto/rand/rand_unix.cs
```

`os/root_unix.cs` and `os/root_nonwindows.cs` are 1.24's `os.Root` work landing in a package that was
already emitted everywhere — invisible to a package-delta derivation.

### 3.3 P2 — `Δ exclusive = +3`, the three being `crypto/internal/sysrand`'s `rand_*`. **SPLIT**

The **three named artifacts arrived exactly as predicted**, one per target. **The count is refuted: Δ is
+6**, from 15 arrivals and 9 departures. The named three are matched by three departures from the package
they came from:

```
  ARRIVED (crypto/internal/sysrand)      DEPARTED (crypto/rand)
  + rand_windows.cs                      - rand_windows.cs
  + rand_getrandom.cs                    - rand_getrandom.cs
  + rand_arc4random.cs                   - rand_darwin.cs
```

That is a **relocation**, so its net contribution to `exclusive` is **zero**, not +3: the derivation
counted the artifacts entering the added package and not the matching three leaving the one they came
from. The real +6 is the other 12 arrivals against 6 departures.

### 3.4 P3 — `Δ (identical + variant)` = +97 source and +40 `package_info`. **REFUTED on both**

| kind | outgoing | incoming | Δ measured | Δ predicted |
|---|--:|--:|--:|--:|
| source | 1233 | 1350 | **+117** | +97 |
| `package_info.cs` | 297 | 334 | **+37** | +40 |
| `package_init.cs` | 25 | 30 | **+5** | not predicted |

Total +159, reconciling with (1631 + 83) − (1476 + 79).

`package_info.cs` by class reads 6 exclusive + 269 identical + 28 variant = **303** outgoing and
6 + 306 + 28 = **340** incoming — exactly `packagesEmitted` on each side, i.e. **one per emitting package**.
So Δ `package_info` is Δ *emitting* packages (+37), while the predicted +40 is the net *queued* package
delta. Three net-new queued packages emit nothing.

<!-- An earlier explanation for this gap — that three net-new packages are platform-exclusive so their
     package_info.cs scores `exclusive` and never enters identical+variant — was REFUTED by measurement:
     package_info.cs in the exclusive class reads 6 on BOTH sides, with zero arrivals and zero departures.
     Recorded because it is the plausible wrong answer and someone will reach for it again. -->

### 3.5 P4 — the identical/variant split of the added artifacts is content-dependent. **HONOURED**

Stated in advance as a reading rather than a prediction. Measured: +155 identical against +4 variant.
Recorded as a reading; it was not foreseen and is not presented as a hit.

### 3.6 P5 — per-target symmetry. **RULED not a defect** (COORD)

| target | arrived | departed | net |
|---|--:|--:|--:|
| windows/amd64 | 254 | 90 | **+164** |
| linux/amd64 | 260 | 95 | **+165** |
| darwin/amd64 | 259 | 93 | **+166** |

P2's relocation contributes +1 to each and creates no asymmetry. The residual spread is **2 artifacts
across three targets**, from per-GOOS file selection inside the exclusive and partial classes — per-target
by nature, and not from package membership, whose added and removed sets are identical on all three.
COORD ruled the prediction's tolerance was written too tight and the spread is the expected shape.

---

## 4. The variant-source set, decomposed on one axis

`DESIGN-multiplatform-corpus.md` §4.3 names its 38 variant source files in full, measured at a 1.23.x
corpus with a converter many weeks older. Scoring the incoming census against it directly gives 13
arrivals and 0 departures — but that reading mixes the release hop with converter drift. **The outgoing
census separates them**, being the same release as the design's table and the same converter as the
incoming one:

| step | set | arrived | departed | what the step is |
|---|--:|--:|--:|---|
| design's 38 → outgoing 47 | 38 → 47 | **+9** | **0** | **converter drift** — same release, weeks of converter change |
| outgoing 47 → incoming 51 | 47 → 51 | **+4** | **0** | **the release** — same converter, one axis |
| design's 38 → incoming 51 | 38 → 51 | +13 | 0 | the mixed reading, superseded by the two above |

**38 + 9 + 4 = 51.**

- **Converter's nine:** `internal/filepathlite/path.cs`, `net/file.cs`, `net/interface.cs`,
  `net/tcpsock.cs`, `os/exec_posix.cs`, `os/types.cs`, `runtime/cgocall.cs`, `runtime/runtime1.cs`,
  `runtime/sigqueue.cs`.
- **The release's four:** `internal/syscall/unix/syscall.cs`, `os/error_errno.cs`, `os/root_openat.cs`,
  `runtime/lock_spinbit.cs` — 1.24's `os.Root` and spinbit-mutex work.

**On the isolated axis the variant-source set gains 4 and loses 0.** REHEARSAL's H8 wording — *"a migration
moves the platform axis in both directions"* — is therefore **true at `exclusive`** (15 in, 9 out) and
**false at `variant`** (4 in, 0 out) and **at `partial`** (7 in, 1 out). It is right about one class of
three and wrong about the largest.

---

## 5. Project files — a number that is not a corpus property

| | design, recorded | outgoing | incoming |
|---|--:|--:|--:|
| `.csproj` rewritten, W / L / D | 0 / 21 / 22 | **169 / 170 / 171** | **1 / 3 / 4** |
| `packagesWithDifferingCsproj` | (27, a different predicate) | **5** | **4** |

A *rewrite count* is a property of **how far the seed sits from what the converter emits**, not of the
corpus: the incoming tree was already converted by this converter so it rewrites almost nothing; master's
was not, so it rewrites nearly everything; the design's figure is a third seed age. **The three numbers
answer different questions and none is a comparand for another.**

The seed-independent number barely moves: 5 → 4. Outgoing is `crypto/x509/internal/macos`,
`internal/runtime/syscall`, `internal/syscall/windows/registry`, `log/syslog`,
`vendor/golang.org/x/net/route`; incoming is the same **minus `internal/syscall/windows/registry`**.

<!-- The incoming 1/3/4 against the design's 0/21/22 was written up here as a possible collapse, with a
     mechanism attached (the conditioned <ProjectReference> work having landed). The outgoing census
     refuted it before it was posted. Recorded because the mechanism was plausible and would have explained
     a fact that was not there. -->

---

## 6. Seed-versus-emission, named

Per target, `.cs` differing from the seed reads 4 / 2 / 2 and line-ending-only 49 / 49 / 48 at the incoming
census. The population:

- **All three targets:** `crypto/internal/fips140/subtle/package_info.cs` — the seed carries a
  `GoPositionMap` row for the hand-owned `xor_generic.cs` that the converter no longer emits; and
  `internal/godebug/package_info.cs` — `typeof(sync_package)` in the seed against `typeof(go.sync_package)`
  in the emission.
- **Windows additionally:** `os/windows/file_windows.cs` — the seed carries two comment lines the emission
  drops — and its `package_info.cs`, whose position map moves with them.

Reading: the tree is one converter-generation behind on four files, the hop-stale class treated once
already by the `crypto/ecdh` stale `package_init.cs` seat.

---

## 7. Instrument notes carried forward

- **A manifest keyed on the raw relative path scores the L3 TREE, not the artifact classes.** Measured:
  raw keying reads `identical 1631 / variant 0 / partial 0 / exclusive 718`, union 2349 — which is the
  manifest's own `l3UnionTreeTotal`, sums to its union, passes the partition check and clears the seed
  tell. The only unaided tell is `variant 0`. `platformCensus.go` keys an artifact by its **flat
  package-relative path**; strip the layout folder and the four classes reproduce exactly.
- **Stripping must use the structural discriminator**, never the GOOS names: measured on the incoming
  emission, each target holds **100 GOOS-named directories, 99 layout folders and one real package**
  (`internal/syscall/windows`, which carries its own `.csproj`). A name filter deletes that package from
  two of three views. A collision check must accompany the strip — it read 0 duplicate keys on all three.
- **A census target root is seeded, so a whole-root walk is not the emitted set.** The emitted subset is
  identified by the sentinel mtime `2000-01-01T00:00:00Z` (`platformCensus.go`), off which a file's mtime
  has moved iff that run emitted it; verified against the manifest's own `emittedCs` at 1823 / 1892 / 1896.
- **A GOOS-named *file suffix* is the seed tell**: in a true per-target emission census a `*_windows.*`
  artifact cannot appear in a foreign target's manifest.

<!-- Both instrument findings were closed inside h8-comparand.sh during this hop, each with its own control
     arm rather than a documented convention: `classify` refuses an unstamped manifest and asserts the WRONG
     answer as well as the right one, and `manifest` gained an emitted-set restriction. The selftest grew
     20 -> 27 -> 32 arms. -->

---

## 8. What this record does not contain

- **No build and no test.** Every number here is an emission property. Nothing states that any flavour of
  the converted corpus compiles; that is H7's fact and the corpus builds that follow an apply.
- **No rung declaration.** The gate rulings are COORD's, on the mailbox.
- The outgoing 1.23.12 manifest is **produced**, not recovered: no platform manifest has ever been tracked
  on any ref, and the preserved staging artifacts are either the wrong release or the wrong artifact kind.
  It is the comparand of record for this hop by COORD's ruling.
