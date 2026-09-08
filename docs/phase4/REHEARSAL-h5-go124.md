# REHEARSAL — H5/H7 against Go 1.24.13

**Lane R, 2026-09-07, on R-LAPTOP.** A point-in-time **record**: what a full `-stdlib` conversion of
Go 1.24.13 emits, and how far the emitted corpus compiles. Amended with dated blocks, never rewritten,
never executed from.

```
toolchain (preflight, 3 arms ASSERTED)   tree=go1.24.13   bin=go1.24.13
converter built by                        go1.24.13
corpus baseline being measured against    307 projects, 307/307 compiling at 1.23.12
scratch version.props                     GoStdLibVersion 1.24.13   (the repo's stays 1.23.12)
```

**Scope.** No overlay into the corpus, no cuts, no `x/tools` bump, no H2 pin. Everything below was
produced in a scratch root outside any repository, seeded per the reconvert ritual and deleted when
this record committed. **Predictions were posted BEFORE the run** (`b2856687f`) and are scored in §9
against what happened, including the two that were wrong.

---

## 1. The conversion — the converter's own failure surface

This is the first table, and it precedes any build.

```
  CONVERSION exit 0 after 334s
  Total packages 344      Successfully converted 344 (100.0%)      Failed 0 (0.0%)
  'did not fully type-check' lines                0
  export-data / x-tools refusal                   0
  WARNING lines                                  50
  solution generated                            358 projects
```

**The dominant risk did not fire.** The pre-registered worry was `go/packages` refusing 1.24 export
data on the pinned `x/tools v0.36` — a total refusal in which every package fails identically and the
run measures nothing. Zero occurrences. **H1.3 is therefore not on H5's critical path**, which is a
result about sequencing, not just a number.

The 50 WARNINGs are four classes and none is a failure:

| n | class | where |
|---:|:--|:--|
| 29 | `@getGenericDefinition` — approximate/union/method-carrying pointer constraint | only `crypto/internal/fips140/{bigmod,ecdh,ecdsa}` |
| 15 | Go const via `unsafe.Sizeof` — may not match run-time value | scattered |
| 3 | expression did not resolve to a constant — run-time form emitted | scattered |
| 3 | Go code via `unsafe.Sizeof` — may not produce the same value | scattered |

## 2. Emission census, and the 22 ghosts in the generated solution

```
  packages the converter FOUND                  346      (go list std @1.24.13 = 346)
  packages with >=1 .cs emitted this run        335
  distinct core packages in the generated slnx  357
  ghosts (in the slnx, not emitted)              22
```

**The generated solution carries 22 projects this conversion did not write.** They split cleanly, and
the split matters because building the slnx naively would count 1.23.12 leftovers as "compiling":

- **15 not in `std` at 1.24.13** — `golib` (not a Go package) plus **14 genuine H3 removals**:
  `crypto/internal/{alias,bigmod,edwards25519,edwards25519/field,mlkem768,nistec,nistec/fiat}`,
  `go/internal/typeparams`, `internal/concurrent`, `internal/weak`, `runtime/internal/{math,sys}`,
  and `vendor/golang.org/x/crypto/{hkdf,sha3}`. **The 14 closes against the pre-run `go list`
  arithmetic (+54 −14 = +40), and the "+2" that post could not name is now named: the two vendored
  `x/crypto` packages.**
- **7 live at 1.24.13 but not emitted by a windows-target run** — `testing` and `unsafe` (skip-listed),
  `internal/godebug` and `crypto/internal/boring/bcache` (hand-owned-by-consequence),
  `crypto/x509/internal/macos` and `vendor/golang.org/x/net/route` (other-platform), and
  `internal/runtime/syscall`.

⚠ **Method note.** A first pass classified this by csproj mtime and produced a 129-file list containing
`cmp`, `errors` and `slices` — live packages. The converter does not rewrite an unchanged `.csproj`,
so that measured content-stability, not emission. The classification above is anchored on `.cs`
emission with package directories resolved from csproj locations, and `go list std` unioned over
windows/linux/darwin (a windows-only `go list` cannot see darwin-exclusive packages).

## 3. The would-be-deletions class — and it BLOCKED the build

A seeded root cannot reveal a file the converter has **stopped** emitting. At a release hop that stops
being theoretical.

```
  seeded production .cs not rewritten by this run            1292
    minus files in non-target linux/ darwin/ per-GOOS dirs   -500   (other targets' files)
    minus test-host artifacts (192 package_test_info.cs
          + 192 go2cs_test_host.cs, both <Compile Remove>d)  -384
    minus *_impl.cs hand-own companions                       -11
  = would-be deletions                                         25   (25 of 25 confirmed)
```

Each of the 25 was verified against **Go's own selected file set** (`go list -f '{{.GoFiles}}'` at
1.24.13), not against mere file existence, with a negative control proving the instrument can also
answer "keep" (`fmt/print.cs`, `sync/mutex.cs`, `runtime/chan.cs` all read SELECTED).

**The through-line is three GOEXPERIMENT flips that all default ON at 1.24.13** — `aliastypeparams`,
`swissmap`, `synchashtriemap` — plus the FIPS reorganization:

| stale file(s) | why |
|:--|:--|
| `internal/goexperiment/exp_aliastypeparams_off.cs` | `aliastypeparams` ON, so Go selects `_on.go` |
| `runtime/map.cs`, `map_fast32/64/faststr.cs`, `internal/abi/map.cs` | `swissmap` ON, so maps moved to `internal/runtime/maps` |
| `sync/map.cs` | `synchashtriemap` ON, so `sync.Map` is `hashtriemap.go` |
| `crypto/aes/{block,cipher,cipher_generic,const,modes}.cs`, `crypto/sha256/sha256block*.cs`, `crypto/sha512/sha512block*.cs`, `crypto/ecdsa/ecdsa_noasm.cs`, `crypto/rsa/pss.cs`, `crypto/ecdh/package_init.cs`, `crypto/rand/windows/rand_windows.cs`, `crypto/{tls,x509}/notboring.cs` | FIPS reorganization into `crypto/internal/fips140/*` |
| `go/build/syslist.cs`, `go/internal/gcimporter/{exportdata,iimport}.cs` | moved to `internal/syslist`, `internal/exportdata` |

⚠ **The FIRST build died on exactly this**, in 116 s, with two errors and nothing measured:
`exp_aliastypeparams_off.cs` (seeded) and `exp_aliastypeparams_on.cs` (emitted) both declare
`AliasTypeParams`; the package csproj globs, so both compile — CS0102 x2 in a leaf almost everything
depends on. **The hop owes a deletion pass. Nothing in the overlay ritual performs deletions today.**

## 4. The windows build — one package gates the corpus

After the deletion pass, with the corpus's own hand-owns in place:

```
  BUILD exit 1 after 150s        120 errors, 0 MSB/NETSDK
  failing packages                 1   -- runtime, and only runtime
  projects whose OWN assembly built  70 of 357
```

Errors by code: CS0246 x100, CS0111 x28, CS0102 x22, CS9348 x18, CS0715 x14, CS0563 x12, CS0057 x12,
CS0708 x8, then a tail. **Every one of them is in `runtime`.** Because `runtime` sits under essentially
the whole corpus, its failure does not produce 300 failing packages — it produces one failing package
and ~287 skipped ones. That shape is the single most important thing in this record: **packages-compiling
is not a spectrum here, it is a gate.**

## 5. The root — two hand-owns frozen at 1.23.12

The 120 errors resolve to two production files, both carrying `[module: GoManualConversion]`, both
seeded, both correctly left alone by the converter:

- **`runtime/runtime2.cs`** — 12 errors
- **`runtime/mfinal.cs`** — 4 errors

…and ~104 cascade errors in `go2cs-gen` TypeGenerator output referencing types those files break.

**The mechanism, measured against Go's own sources:**

```
  type note struct   1.23.12 -> runtime2.go        1.24.13 -> note_other.go, note_js.go
  note_other.go      1.23.12 -> ABSENT             1.24.13 -> PRESENT
  runtime2.go declares note   1.23.12 = 1          1.24.13 = 0
```

Go **relocated** `note` out of `runtime2.go` into a new file. The converter emitted the new
`note_other.cs`; the hand-owned `runtime2.cs`, frozen at 1.23.12, still declares `note` — so the type
is declared twice and the package cannot compile. The converter behaved correctly throughout: it
protected both hand-owns and dropped `.cs.auto` review siblings for each, which is exactly the input
the reconciliation needs.

`finblock` is **byte-identical** between the two releases, so the `mfinal.cs` failure is not a shape
change either — it is the same collision class.

## 6. ⚠ THE H6 CENSUS MISSED BOTH FILES — and the failure is the population, not the classifier

`runtime/runtime2.cs` and `runtime/mfinal.cs` **carry the hand-own marker at master and appear nowhere
in `CENSUS-h6-handown-go124.md`**, whose population is 149. They are the entire critical path for this
build. That is a defect in my own census and it is recorded here rather than quietly fixed.

**It is not a classifier blind spot.** The classifier keys on the principal's member diff, and `note`
*is* removed from `runtime2.go` (1 to 0) while the hand-own declares it — so had the file been in the
population it would have classified MEMBERS-REMOVED and disposed **RE-WRITE**, correctly and
automatically. **The population derivation is what failed.**

Sizing the gap against the authoritative marked-file set at master `f4ced674d`
(`git grep` line-anchored over `src/core/**/*.cs`):

```
  marked files                    142
    *_impl.cs companions           98
    whole-file rewrites            44
```

Of the 44 whole-file rewrites, **26 are absent from the H6 census**. Some are legitimately out of
scope (the four host-infrastructure files COORD already ruled out, their `testing/` siblings, the
hand-owned-by-consequence class, `unsafe`, two `_test.cs`). The remainder are not, and the pattern is
telling: `sync/mutex.cs` and `sync/once.cs` are in the population while
`sync/{pool,poolqueue,rwmutex,waitgroup,oncefunc}.cs` are not — **partial coverage inside a single
package, which is the signature of an incomplete enumeration rather than a deliberate scope.**

**H6 owes a re-derivation of its population from the marker census, not a re-run of its classifier.**

## 7. The unmasking measurement — what is behind the gate

To measure the residual rather than stop at the gate, the two hand-owns were replaced by the
converter's own `.cs.auto` emissions and the build repeated. ⚠ **This is a COMPILE census only.** The
hand-owns exist for operational reasons (`mfinal.cs` carries the .NET finalizer bridge); the `.auto`
versions compile but are operationally wrong. Nothing here says the auto conversion is acceptable.

```
  BUILD exit 1 after 192s          7 errors  (down from 120)
  failing packages                 1  -- still runtime, and only runtime
  projects whose OWN assembly built 70 of 357
```

**Two of the seven are artifacts of the experiment itself** — `managed_impl.cs` references
`GoFinalizerQueue`, which is defined 6x in the hand-own that was swapped out and 0x in the `.auto`.
Verified, and excluded. **The genuine residual is 5 errors in 3 files:**

| file | n | detail |
|:--|--:|:--|
| `runtime/lock_spinbit.cs` | 3 | **new file at 1.24.13** (absent at 1.23.12): CS0246 on `Ꮡsched`; CS0029 x2 `ж<array<byte>>` to `ж<byte>` |
| `runtime/type.cs` | 1 | CS9135 — a constant of type `ж<byte>` expected |
| generated `m.g.cs` | 1 | CS1061 — `m` has no `Δtrace` |

⚠ The obvious explanation for the last one is refuted: **`m` carries a `trace` field at BOTH releases**,
so it is not a struct-shape change and its cause is not established here.

## 8. The linux flavour — NOT a readiness measurement, and why

```
  BUILD exit 1 after 170s        188 errors     failing packages 1 (runtime)
  dominated by CS0234 x250       'sys_package' / 'maps_package' not in namespace go.internal.runtime
```

**This number does not mean linux is 181 errors worse than windows.** `runtime.csproj` was re-emitted
at 1.24.13 with per-GOOS `ProjectReference` blocks, and only the target that actually ran was
recomputed:

```
  windows block   internal/runtime/{maps,math,sys}      <- the 1.24.13 packages
  linux   block   internal/runtime/syscall,
                  runtime/internal/{math,sys}           <- the 1.23.12 packages, carried from the seed
  darwin  block   runtime/internal/{math,sys}           <- likewise
```

**A single-target conversion recomputes only its own target's reference block.** At an ordinary regen
that is harmless because references do not move; **at a release hop where packages MOVE
(`runtime/internal/sys` to `internal/runtime/sys`, and the new `internal/runtime/maps`), it means the
other flavours reference packages the new release does not have.**

⚠ **H5 therefore requires the multi-platform emission** (`-platforms windows/amd64,linux/amd64,darwin/amd64`
with `-platform-stage`), not a single-target `-stdlib` run. **The linux and darwin flavours cannot be
measured at all until that runs**, and this record does not claim to have measured them.

## 9. Predictions, scored — two held, two missed

| # | predicted | measured | verdict |
|:--|:--|:--|:--|
| 1 | 0–8 converter failures; export-data refusal will NOT fire | 0 failures, 0 refusals | **HELD** |
| 2 | packages-compiling windows **300–325** of ~346 | **70** | **MISSED, badly** |
| 3 | linux **3–10 fewer** than windows | unanswerable as posed | **VOID** |
| 4 | the "neither" set **15–35** | **1** | **MISSED, badly** |

**Why 2 and 4 were wrong is one reason, and it is structural.** Both predictions assumed failures would
be *distributed* — many packages each failing on their own 1.24.13 material. Instead a single leaf
(`runtime`) fails and everything above it is skipped, so packages-compiling collapses to 70 and the
failing set has exactly one member. **The "neither" set — failing packages that are neither H6 RE-WRITE
rows nor H3 removals — is `{runtime}`.** It is one package, not 15–35, because the denominator is the
*failing* set and the gate made that set a singleton.

**The honest headline is not a count.** It is: *the H4 bill nobody has named is not a long list of
packages — it is `runtime`, and it is gated on two hand-owned files that the H6 census did not
enumerate.* That is a smaller bill than predicted and a sharper one.

## 10. The `%!` census over this emission

Scoped to production `.cs` **emitted by this run**, one pattern used for both selection and extraction:

```
  emitted production .cs carrying a %!VERB( artifact      1   (fmt/doc.cs)
  of which ALSO present in the originating Go source      1   -> faithful
  converter-produced %! artifacts                          0
```

**Zero converter-produced artifacts, agreeing with G's measured zero at 1.23.12.** ⚠ A first pass
reported 5 files; that pass grepped seeded `_test.cs` outside its stated scope and extracted with a
narrower pattern than it selected with, producing three empty rows misread as artifacts. The
quiet member G named — `100%%` collapsing to `100%` — **cannot be found by this census by
construction**, since its output is well-formed; 216 emitted comment lines carry a bare `N%` and are
the population a different instrument would have to check.

## 11. What the hop owes, from this rehearsal

1. **A deletion pass.** 25 files, enumerated in §3, derived from Go's selected file set. Without it the
   corpus does not build at all — the first casualty is `internal/goexperiment`, and it is fatal.
2. **A re-derived H6 population** (§6) from the marker census. The classifier is fine; the enumeration
   was not. The two files that gate this build were missing from it.
3. **A multi-platform emission** (§8). Single-target `-stdlib` cannot produce a buildable
   multi-flavour corpus across a hop that moves packages.
4. **Reconciliation of `runtime2.cs` and `mfinal.cs`** against their `.cs.auto` siblings — the whole
   critical path, and the converter has already produced the review inputs.
5. **Five genuine residual errors** (§7), three of them in `lock_spinbit.cs`, a file that did not exist
   at 1.23.12.

## 12. Instrument corrections made during this run

Recorded because each produced a plausible, well-formed, wrong number that a gate would not have caught:

- A `sed` alternation whose `|` delimiter collided with its own alternation printed **"0 packages
  emitted"** against 4,027 files on disk. Caught by impossibility.
- Backslash-bearing `sed`/`tr` expressions written through a heredoc lost a backslash level and
  **failed open** — "0 failing packages" on a 120-error build, "0 packages compiling". Caught by
  impossibility.
- The pre-post security census's **UNC arm had never been made to fire**; two attempts to plant the
  pattern were silently de-escaped before it was proven on the third.
- A hand-own marker probe used `grep -cl`, which returns a filename, so its tag could never print.
- An unanchored `map.go` matched `hashtriemap.go` and nearly kept a stale file.
- The emitted-vs-seeded time window excluded the build's own generator output, briefly labelling
  `.g.cs` files "SEEDED 1.23.12".

-- R

---

## 2026-09-07 — §10. THE THREE-TARGET REHEARSAL (ruling 3) — appended; §1–§9 unchanged

**Why this section exists.** §8 of this record could not measure linux or darwin: a single-target
conversion recomputes only its own target's per-GOOS `ProjectReference` block, so the other flavours
carried the SEED's references to packages 1.24.13 no longer has, and the linux reading was an
artifact. COORD ruled H5 runs the three-target emission. This is that run.

```
  converter   master b6746ab18, built BY go1.24.13, embedded stamp verified
  preflight   3 arms ASSERTED (GOROOT EXPORTED, so arms 2 and 3 carry the weight)
  emission    -platforms windows/amd64,linux/amd64,darwin/amd64  -platform-stage
  seed        3,756 .cs   142 marked hand-owns   scratch version.props 1.24.13, repo untouched
  predictions posted BEFORE the conversion (mailbox e3a4b5466)
```

### 10.1 The conversion — clean on all three targets

```
  exit 0 after 1006s
  Failed: 0 (0.0%)   x3        did not fully type-check: 0        export-data refusals: 0
  staging   windows 4014 .cs    linux 4020    darwin 4019
  merged corpus 4031 .cs (seeded 3756)   emitted this run 1632   WARNING lines 147
```

**PREDICTION 1 HELD** — zero converter failures on every target, converted counts exactly as
predicted (windows 344 of 346 `go list`, the two skip-listed packages being `unsafe` and `testing`).

### 10.2 ⚠ THE DELETION INSTRUMENT WOULD HAVE DELETED `golib` — reported, not applied

Ruling 2's `reconvert-deletions.ps1`, first real run, dry:

```
  DELETE-ABSENT 201   DELETE-DESELECTED 4   UNRESOLVED 43   PROTECTED 151   KEEP-SELECTED 693
  of the 201, rows whose principal is under golib/ or go2cs/            117
  on disk: golib/*.cs 116  +  go2cs/Symbols.cs 1                      = 117   (closes exactly)
```

`golib` is classified `DELETE-ABSENT` with reason **"package not in std at target"** — true and
irrelevant, since `golib` was never a Go package. `PROTECTED` covers `[module: GoManualConversion]`
markers and `*_impl.cs` companions; **`golib` carries neither, correctly, because nothing converts
into it — so the one directory that needs no marker is the one the instrument does not protect.**
`-Apply` does not refuse either: `Remove-Item` sits inside the apply branch while the UNRESOLVED
`exit 2` runs AFTER the deletion loop, so a run exiting 2 has already deleted.

**Escalated (mailbox `1f5e8f276`); COORD confirmed both defects and owns the fix. Nothing was
applied.**

**What was applied instead — a hand-built SAFE subset**, the instrument's own classification minus
its defect, with the extraction proven by matching the instrument's header counts (201 and 4) exactly
and guarded to refuse any `golib/`, `go2cs/` or marker-carrying path:

```
  union of DELETE-ABSENT + DELETE-DESELECTED     205
    excluded golib/ + go2cs/                    -117
  = SAFE deletion set                             88     guard violations 0, all 88 present on disk
      files of the 14 removed Go packages         50
      live-package per-file deletions             38     contains 24 of §3's 25
  controls  golib 116 .cs intact   Symbols.cs intact   fmt/print.cs intact
```

**⚠ PREDICTION 2 MISSED, and the reason is the finding.** I predicted "exactly 25 on windows"; the
instrument reads 205. It counts **whole REMOVED PACKAGES and non-Go directories**, which §3's
population deliberately excluded by scoping to packages the converter emitted into. **Its question is
the right one for a deletion pass; mine was the right one for a live-package census.** Two questions,
and I predicted against the wrong one. §3's enumeration is intact inside its live-package class.

**The deletion pass works**: `AliasTypeParams` errors after it — **0**. The blocker that killed §3's
first build in 116 seconds is gone.

### 10.3 The per-flavour builds — the gate is FLAVOUR-INDEPENDENT

Each flavour `--no-incremental`, `bin`/`obj`/`Generated` purged between (783–786 dirs each), normal
verbosity, strict `error (CS|MSB|NETSDK)[0-9]+` split in two:

| flavour | CS lines | MSB/NETSDK | failing packages | assemblies built | wall |
|:--|--:|--:|:--|--:|--:|
| windows | 240 | 0 | `runtime` only | 70 | 152 s |
| linux | 240 | 0 | `runtime` only | 67 | 176 s |
| darwin | 240 | 0 | `runtime` only | 67 | 169 s |

Identical error-code distributions on all three (CS0246 ×100, CS0111 ×28, CS0102 ×22, CS9348 ×18,
CS0715 ×14, CS0563 ×12, CS0057 ×12, CS0708 ×8).

**PREDICTION 3 HELD** — one leaf gates every flavour, and packages-compiling lands within **3** of
itself across the three (band predicted: ±10). The `note` collision is platform-neutral because
`note_other.go` is selected on all three targets, and the deletion pass correctly does not touch a
marker-protected hand-own.

**PREDICTION 4 HELD, decisively** — **CS0234 on `sys_package`/`maps_package`: 250 → 0.** §8's
188-error linux reading was entirely the single-target artifact, and linux is not merely *within 3×*
of windows but **identical to it**. §8's refusal to report that number as a measurement was correct.

### 10.4 The residual behind the gate — reproducible, and unchanged from §7

Unmasking arm (runtime's two hand-owns swapped for their `.cs.auto`; **a COMPILE census only** — the
`.auto` are operationally wrong): **120 errors → 7**, of which **2 are artifacts of the swap itself**
(`managed_impl.cs` wants `GoFinalizerQueue`, defined 6× in the hand-own and 0× in the `.auto`).

```
  runtime/windows/lock_spinbit.cs  x3   CS0246 on Ꮡsched; CS0029 x2  ж<array<byte>> -> ж<byte>
  runtime/type.cs                  x1   CS9135  a constant of type ж<byte> is expected
  generated m.g.cs                 x1   CS1061  m has no Δtrace
```

**Byte-for-byte the same five genuine errors §7 measured single-target**, with one difference that is
itself a confirmation: `lock_spinbit.cs` now sits in `runtime/windows/`, the per-GOOS folder the L3
merge correctly routed it to.

### 10.5 ⚠ PREDICTION 5 IS UNSCORED — unreachable, NOT refuted

I predicted `sync/mutex.cs :: fatal` (the second H6 collision) would surface in the unmasked arm.
**It did not, and the reason is not that it is absent.** The unmasked arm still leaves 7 errors in
`runtime`, `sync.csproj` carries a `ProjectReference` to `runtime`, and a dependent of a failed
project is **skipped, not compiled** — `sync` produced no assembly and no errors. It cannot be built
standalone for the same reason.

**The source-level evidence stands and was re-verified in this run's own emitted corpus:**

```
  sync/mutex.cs    marker-protected (1 marker line), declares fatal
  sync/runtime.cs  EMITTED at 1.24.13, declares fatal
```

Both files declare it; nothing displaces it. **But a build has not confirmed it, and I am not
recording a prediction as held on source reading alone.** It scores when `runtime` compiles.

### 10.6 What the hop owes — updated from §11

1. **Fix `reconvert-deletions.ps1`** (COORD owns it): exclude non-conversion-target directories, and
   move the UNRESOLVED refusal ahead of the deletion loop. Then re-run the dry pass — the scratch
   from this rehearsal reproduces it in minutes.
2. **Reconcile `runtime2.cs` and `mfinal.cs`** — still the whole critical path, on every flavour.
3. **Five residual errors**, three in a file new at 1.24.13.
4. **`sync/mutex.cs :: fatal`** — predicted, unscored, next in line once `runtime` compiles.
5. The **43 UNRESOLVED** rows the instrument correctly refuses to delete still need a human.

### 10.7 Instrument corrections in this run

- Four extraction attempts on the dry-run table returned zeros from wrong section anchors and wrong
  indent widths — including once because a *previous* command's `sed 's/^/  /'` had added the very
  indent I then measured. **Fixed by asserting the extraction against the instrument's own header
  counts (201, 4) before using it** — the check that should have been first.
- A gate-host census read **1** and it was the querying shell matching its own pattern. Re-run from a
  script that excludes its own PID and any command line carrying the pattern: **0**.
- The first seed of §1–§9 (3,769 `.cs`) was larger than this one (3,756) with the corpus `.cs`
  unchanged: that seed came from a worktree holding **untracked `reflect` test emission**. A seed
  inherits its source tree's dirt; this run seeded from a clean detached checkout.

---

## 2026-09-07 — §11. THE 43 UNRESOLVED ROWS, DISPOSED — and a correction to §3's own count (appended; §1–§10 unchanged)

The deletion instrument classifies 43 seeded files `UNRESOLVED`, refuses to delete them, and exits
non-zero **specifically so a human disposes of each before the overlay**. This is that disposition.
Nobody else had run the instrument, so the human is me.

### What they are

All 43 are converter-generated metadata — `package_info.cs` and `package_init.cs` — which by
construction have **no Go principal**. `go list` cannot answer for them, and the instrument is right
to decline rather than guess.

### The disposition, by the SAME discriminator the marker case needs

A package's metadata belongs to its package. So the question is not "does this file have a Go
principal" but **"does its PACKAGE exist at the target"** — the identical rule §10's dossier addition
proposes for marker-protected files:

```
  DELETE  package absent at 1.24.13   15
  KEEP    package live                28
                                     ---
                                      43
```

The 15 are **15 files across exactly 14 packages** — `crypto/internal/edwards25519` contributes both
its `package_info.cs` and its `package_init.cs` — and those 14 are **precisely the H3 removals §2
named**, with no residue on either side:

```
  crypto/internal/{alias,bigmod,edwards25519,edwards25519/field,mlkem768,nistec,nistec/fiat}
  go/internal/typeparams   internal/concurrent   internal/weak
  runtime/internal/{math,sys}   vendor/golang.org/x/crypto/{hkdf,sha3}
```

The 28 KEEP rows are live packages whose metadata simply did not change between the releases: the
converter's `needToWriteFile` skips a write whose bytes are identical, so unchanged metadata reads as
*seeded* and must be kept. **That is exactly the caveat the instrument's own header documents, met in
practice.**

### ⚠ CORRECTION TO §3 — my enumeration was 24, not 25

`crypto/ecdh/package_init.cs` appears in §3's 25 and in the instrument's UNRESOLVED set, and it is the
one row where the two disagreed. **The instrument is right and §3 is wrong.**

§3 mapped each seeded `.cs` to a same-named `.go` and deleted it when that principal was absent at the
target. For `package_init.cs` there is **no `package_init.go` at either release** — it is go2cs-generated
metadata that never had a Go principal — so the rule read "principal gone" and counted it a deletion.
`crypto/ecdh` is LIVE at 1.24.13 and its metadata stays.

**§3's would-be-deletions count is therefore 24, not 25**, and the 24 are the rows the instrument's
live-package class also carries. Everything §3 concludes from the class is unaffected; only the count
moves. The lesson is §10.7's, one file over: **a rule that maps an artifact to a principal must first
ask whether the artifact HAS one.**

### The hop's deletion bill, stated as SETS

Three populations, deliberately not summed, because they overlap:

```
  88  the safe subset applied in this rehearsal (50 removed-package .cs + 38 live-package)
  15  UNRESOLVED metadata belonging to removed packages          <- this section
  24  corpus files under internal/concurrent + internal/weak that every instrument path declines
```

`internal/concurrent/package_info.cs` and `internal/weak/package_info.cs` are members of **both** the
15 and the 24. A single total would double-count them, so the record carries the sets.

**All three become one rule once the instrument is fixed:** *a package absent from `go list std` at
the target takes every file under it — sources, metadata, marker-carrying hand-owns, `.csproj`,
README — and a package that is present keeps its metadata whatever its timestamp says.*

---

## 2026-09-07 — §12. THE KEEP SIDE, MEASURED — completing the deletion instrument's ledger (appended; §1–§11 unchanged)

§10 called the instrument "otherwise sound and its `go list` decider right". **That was an inference,
not a measurement**: I had audited the DELETE side (found the `golib` defect) and the PROTECTED side
(found the removed hand-own-by-consequence packages) and never the KEEP side. Every seam check carries
both sides of the ledger, so here is the third.

### Why it needed an independent derivation

`KEEP-SELECTED` prints **only its count** (693) — the rows are not enumerated — so the class cannot be
audited from the report at all. It has to be re-derived.

### The derivation, and one confound caught in it

For every seeded production `.cs` still on disk, excluding non-conversion-target directories, marked
hand-owns and `*_impl.cs` companions: is its principal in Go's SELECTED set at 1.24.13?

⚠ **The first pass reported ~40 FALSE KEEPS and every one was my own confound** — files in
`runtime/linux/`, `runtime/pprof/darwin/`, `syscall/darwin/` checked against the **windows** selected
set. A file in a per-GOOS folder must be checked against **its own flavour**. This is the same
per-GOOS trap §10.7 records for the H6 collision census, walked into a second time in the same
evening, which is why it is written down twice.

Corrected — each file checked against the flavour of the folder it sits in:

```
  checked against Go's own selected set   692
  FALSE KEEPS                               0
```

**The KEEP side is SOUND.** Every file the instrument keeps has a principal Go still selects on that
file's own flavour.

### What this bounds — and it is the useful part for the fix

The instrument's ledger now reads, all three sides measured:

| side | verdict |
|:--|:--|
| **DELETE** | **defective** — 117 `golib`/`go2cs` rows, files that were never Go packages |
| **PROTECTED** | **defective in the mirror direction** — keeps files of REMOVED packages (`internal/concurrent`, `internal/weak`) |
| **KEEP** | **SOUND** — 692 of 692 |

**Both defects sit at exactly one boundary: "is this a Go package at the target".** Neither is in the
selected-file logic, which is correct on every row measured. **So the fix is an added classification
in front of the existing predicate, not a change to it** — which is what §10's proposed rule and
§11's converge on, now with the KEEP side measured rather than assumed.
