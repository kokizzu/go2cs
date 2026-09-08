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
