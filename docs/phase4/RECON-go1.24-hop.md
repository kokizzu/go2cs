# RECON — the Go 1.24 hop, baseline capture

> Lane `C2`, 2026-09-02. **MEASUREMENT ONLY.** Nothing outside this record was committed, no
> converter/golib/gen/corpus change is proposed here, and **the repository's `src/version.props` was
> never touched** — only a scratch root's pin moved, as [`../GoCorpusMigration.md`](../GoCorpusMigration.md)
> H2 requires.
>
> Shaped as H0 (baseline capture) + H3 (package census), read against H1 (toolchain provisioning) and
> H6 (the hand-own re-audit). **The runbook leads**; where this record and it disagree on procedure,
> the runbook wins.
>
> H3's own bar: *a patch-level migration should produce an empty census; a non-empty one is a
> finding.* **1.23.12 → 1.24 is a MINOR hop, so a non-empty census is expected — the SIZE and the
> SHAPE are what this record is for.**

## 0. Headline

**The converter, rebuilt on go1.24.13, converts the entire Go 1.24.13 standard library — 342 of 342
packages, zero failures, zero "did not fully type-check", zero visit errors.** The front end is not
the hop's problem.

The **compile** picture is a ladder, and two rungs were measured. Wall 1 is a **file-selection flip**
(`aliastypeparams` entered Go 1.24's baseline experiment set, so a different build-tag variant is
selected and the superseded file collides). Wall 2 is **one converter defect, three occurrences, one
identifier**: a C#-keyword escape applied to a name COMPONENT instead of the whole identifier
(`vgetrandomInit_@params`), in a runtime file that is new in 1.24.

The **corpus** bill is a 31-file deletion set, the **hand-own** bill is 4 vanished principals and 39
changed, the **registration table survives intact** (0 of 242 lost), and the **roster** bill is 10
rows whose Go package no longer exists (2,321 banked verdicts, of which `crypto/internal/nistec`
alone is 2,195) plus 99 rows with changed test sources.

**Recommendation: hold the hop until 100% on 1.23.12, as the owner ordered.** §9 gives the reasoning
and the cost.

---

## 1. Method, and the controls that make the numbers trustworthy

**Target release: `go1.24.13`** — the last 1.24.x available. Enumerated from the module proxy's
`golang.org/toolchain/@v/list` because `go.dev` is blocked from this container.

**Toolchain provisioning (H1), and a substitution worth recording for any future cloud lane.**
`go.dev`, `dl.google.com` and `storage.googleapis.com/golang/*` are all denied by this container's
egress policy. `proxy.golang.org` is not, and **Go publishes its toolchains there as ordinary
modules**, so `GOTOOLCHAIN=go1.24.13 go version` fetched the release in 8 s. Per H1 the tree was then
copied OUT of the read-only module cache into a real `GOROOT` and the read-only attribute stripped
recursively (verified: **0 files lacking `u+w`**), precisely so it cannot travel into copied fixtures.
Both toolchains verified three ways — `go version`, `go env GOROOT`, `go env GOTOOLCHAIN` — with
`GOTOOLCHAIN=local` pinning the selector.

**The trial converter (H1.2, H1.3), built in a SCRATCH clone.** H1.2 applied (the converter module's
`go` directive → `1.24.13`). **H1.3 turned out NOT to be required to build**: the converter compiles
against the current `golang.org/x/tools v0.36.0` / `x/mod v0.27.0` under go1.24.13, in 24 s.
(Available: v0.49.0 / v0.40.0.) This is a finding for the runbook rather than an assumption carried:
H1.3 remains worth doing for the hop proper, but it is not a precondition of measuring.

**Route #4's own mechanism used as the check**: `go version <exe>` reads the toolchain back out of a
built binary, so both binaries were verified to exist **at the exact `-o` path they were then invoked
from**, and to carry the intended toolchain — `go2cs-ctrl` → `go1.23.12`, `go2cs-trial` → `go1.24.13`
— with distinct sizes (17,749,709 vs 18,701,572 bytes).

**Two seeded emissions, diffed against each other — never against the committed tree**, which carries
known unlevelled drift. Each root seeded with `src/core` (build output excluded), `src/gen`,
`src/version.props` and `docs/validation`, **and the seeded `.cs` COUNT asserted (3,664 = 3,664) on
both** before either conversion ran, because the seed copy can die halfway and carry on. Emitted-vs-
seeded classified by a **sentinel mtime**, never by content. The trial root's `version.props` was
bumped to `1.24.13` (H2, without which `checkCorpusToolchainPin` refuses the run); the repository's
was not.

`CGO_ENABLED=0` on both conversion command lines only, per the corpus's emission-state convention.
Host: 4-core cloud Linux container, .NET SDK **10.0.111 (Ubuntu source-built, not Microsoft's)**,
stated because a wall-clock or SDK-shaped reading here is provisional until a Microsoft-build host
agrees.

**One near-miss recorded, because it would have been a catastrophic false finding.** The first bucket
pass reported "140 ERROR" in the trial log. Case-sensitive `ERROR` is **0**; all 140 were the word
*error* inside Go type names (`(…, error)` in the fips140 signatures a warning quotes). Caught before
it was reported. A case-insensitive grep for a marker whose case is load-bearing is not a census.

---

## 2. The package census (H3)

`go list std`, `CGO_ENABLED=1`, amd64:

| GOOS | 1.23.12 | 1.24.13 | Δ |
|:--|--:|--:|--:|
| linux | 305 | **345** | +40 |
| windows | 307 | **347** | +40 |
| darwin | 306 | **346** | +40 |

**54 added, 14 removed — but the shape matters more than the count.**

**38 of the 54 additions are `crypto/internal/fips140*`**, and **7 of the 14 removals are the old
`crypto/internal/{alias,bigmod,edwards25519,edwards25519/field,mlkem768,nistec,nistec/fiat}` moving
underneath it.** That is one reorganization wearing an add-and-remove costume. The rest:

**Genuinely new public API:** `crypto/fips140`, `crypto/hkdf`, `crypto/mlkem`, `crypto/pbkdf2`,
`crypto/sha3`, `weak` (promoted from `internal/weak`), plus `internal/synctest`, `internal/sync`,
`internal/runtime/{maps,math,sys}`, `internal/exportdata`, `internal/syslist`, `internal/impl`,
`crypto/internal/sysrand`.

**Genuinely removed (not moved into fips140):** `go/internal/typeparams`, `internal/concurrent`
(→ `internal/sync`), `internal/weak` (→ public `weak`), `runtime/internal/{math,sys}`
(→ `internal/runtime/*`), `vendor/golang.org/x/crypto/{hkdf,sha3}` (→ real `crypto/*` packages).

**Of the 291 packages present in BOTH: 157 are file- and line-stable, 134 changed.** The heaviest
(non-test `.go` line delta): `runtime` +3,185, `os` +1,494, `net/http` +930, `syscall` −813,
`crypto/x509` +649, `crypto/tls` +641, `go/types` +523, `reflect` +396.

---

## 3. The language change — measured, and the answer is counter-intuitive

**Generic type aliases are ON by default in Go 1.24.13.** Not off, as a reading of
`internal/goexperiment/flags.go` alone suggests. The toolchain's own file selection is the authority
and it is unambiguous:

```
go1.23.12  internal/goexperiment GoFiles: ... exp_aliastypeparams_OFF.go ...
go1.24.13  internal/goexperiment GoFiles: ... exp_aliastypeparams_ON.go ...
```

`aliastypeparams` entered 1.24's **baseline** experiment set. (Also newly baselined: `spinbitmutex`,
`swissmap`, `synchashtriemap` — all `_on` — plus `synctest`, `_off`.)

**And yet the converter owes it nothing today: there are ZERO generic type aliases in the 1.24.13
stdlib's production code.** All 21 `type X[T ...] = ...` matches live in `internal/types/testdata/`
— type-checker fixtures, never compiled as stdlib. (1.23.12: 13 matches, likewise all fixtures, 0
production.)

**So the headline 1.24 language feature costs the corpus hop nothing, and could cost the converter on
the first converted end-user app that uses it.** Those are different bills and this record does not
merge them.

Front-end surface deltas, for scale: `go/types` +9 exported declarations, `go/parser` +2,
`go/ast` −3, `go/token` 0.

---

## 4. The front-end trial

| | control (1.23.12) | trial (1.24.13) |
|:--|--:|--:|
| packages converted | **302 / 302 (100%)** | **342 / 342 (100%)** |
| failed | 0 | **0** |
| "did not fully type-check" | 0 | **0** |
| visit file errors | 0 | **0** |
| wall | ~3m30s | **3m42s** |
| converter WARNINGs | 21 | 51 |
| emitted `.cs` (newer than sentinel) | 1,725 | 1,895 |

**The +30 warning delta is ONE new class, 29 sites, and it names its own packages:**

```
WARNING: @getGenericDefinition - approximate/union/method-carrying pointer constraint
`crypto/internal/fips140/ecdh.Point[P]` on `crypto/internal/fips140/ecdh.Curve[P …]`
is not erased; emission may not compile in "ecdh.go"
```

**23 in `crypto/internal/fips140/ecdsa`, 6 in `crypto/internal/fips140/ecdh`** — both packages that
do not exist in 1.23.12. **Zero in the control.** The remaining delta is one extra `unsafe.Sizeof`
const warning (13 → 14).

Note what the warning *claims*: "emission may not compile". **Neither package reached the compiler**
in either build below — both sit behind `runtime` — so **whether that prediction is right is
UNMEASURED**, and this record does not assert it either way.

---

## 5. The compile ladder

`dotnet build go2cs-stdlib.slnx -c Debug -m --no-incremental -p:GoTargetOS=linux`, 357 projects.

**A census reports the FIRST wall.** Dependents of a failed project are skipped, not errored, so
every error count below is a **floor**, and the metric is packages-compiling.

### Rung 1 — 2 errors, `internal/goexperiment`, 186/357 assemblies

```
internal/goexperiment/exp_aliastypeparams_on.cs(7,19): error CS0102: The type
'goexperiment_package' already contains a definition for 'AliasTypeParams'
```

**Root, classified by sentinel mtime rather than guessed:** `exp_aliastypeparams_off.cs` is
**SEEDED**; `exp_aliastypeparams_on.cs` is **EMITTED THIS RUN**. The control emitted only `_off.cs`,
correctly. So §3's baseline flip changed which variant is selected, and the seed merely explains why
the superseded file was still on disk to collide with.

**This is the ritual's rule earning its keep: a seeded reconvert can NEVER reveal a file the
converter has STOPPED emitting.** Only an emitted-vs-seeded classification can, and without it this
would read as a converter defect.

### The deletion set — 100 non-test seeded files, split by cause

| cause | files | |
|:--|--:|:--|
| hand-owns — never re-emitted at that path by design | **69** | not deletions |
| **Go principal GONE in 1.24** | **28** | the crypto reorg: `crypto/aes/{block,cipher,cipher_generic,const,modes}.cs`, `crypto/sha256/sha256block*.cs`, `crypto/sha512/*`, `crypto/rand/linux/*`, `crypto/ecdsa/ecdsa_noasm.cs`, `crypto/rsa/pss.cs`, `crypto/tls/notboring.cs`, … |
| **build-tag selection FLIP** | **2** | `internal/goexperiment/exp_aliastypeparams_off.cs` and **`sync/map.cs`** (the `synchashtriemap` baseline) |
| other | 1 | `crypto/ecdh/package_init.cs` |

**The hop's corpus-side deletion bill is 31 files.** All 31 were removed and the build re-run.

### Rung 2 — 10 errors / 6 distinct, ALL in `runtime`, 188/357 assemblies

And they are **syntax** errors, which resolve to **one root with three occurrences**:

```
runtime/vgetrandom_linux.cs(27,56):  error CS1513/CS1514
runtime/vgetrandom_linux.cs(27,64):  error CS1519: Invalid token '{' in a member declaration
runtime/linux/package_info.cs(612):  the same identifier in the witness list
```

The emitted line:

```csharp
[GoType("dyn")] internal partial struct vgetrandomInit_@params {
```

**`@` is legal only as the FIRST character of a C# identifier.** The Go source
(`runtime/vgetrandom_linux.go:30`, **a file that does not exist in 1.23.12**) declares a
function-local anonymous struct `var params struct { … }`. The converter lifts it to a type named
`<function>_<variable>` and applies the C#-keyword escape to the **variable-name component** rather
than to the composed identifier — which, being `vgetrandomInit_params`, needs no escape at all.

The emission is *internally* correct about the variable itself — `ref var @params = ref heap(new
vgetrandomInit_@params(), out var Ꮡparams)` escapes the local correctly, because `params` really is
the keyword there. Only the composed TYPE name is wrong.

**Census: exactly 3 occurrences of one identifier across the whole 1.24 emission; ZERO `_@`-composed
identifiers in the 1.23.12 control.** A single-root, single-site defect, and small — but it is a
CONVERTER change, so this record states it and proposes nothing.

### Rung 3 — 120 errors / 49 distinct, ALL in `runtime`, 188/357 assemblies — and it is a GENERATOR wall

Measured 2026-09-02 by patching the `_@` defect in the **scratch converter clone only** (marked as a
measurement patch in its own source, never committed; `git status src/go2cs` empty before and after),
seeding a **fresh** root, reconverting — 342/342 again, `_@` identifiers **3 → 0** — re-deriving the
deletion set independently for that root (**31 again**) and rebuilding.

**The errors are not in converter emission at all.** Read verbatim, 85 of the 120 are in **go2cs-gen's
generated output** (`Generated/go2cs-gen/go2cs.TypeGenerator/go.runtime_package.{_func,finblock,note.1,…}.g.cs`),
plus 6 in `runtime2.cs` and 2 in `mfinal.cs`. That is **route #7's territory — a class no `-stdlib`
diff can see**, because CNR is transpile-only and generator output never enters its verdict.

**Root, with its control.** The generated type files carry, at line 15:

| | the generated using |
|:--|:--|
| **1.23.12 (control)** | `using global::go.runtime.@internal;` — correctly qualified |
| **1.24.13 (trial)** | `using runtime.@internal;` — **`global::go.` gone** |

Occurrences of the unqualified form: **0** in the 1.23.12 generated output, **29** in the 1.24.13
generated output. Without `global::`, `runtime` does not resolve from inside `namespace go;` and every
generated type in the package fails — `CS0246: 'runtime' could not be found` — cascading into
CS0111 / CS0102 / CS0116 / CS0715 as the partial declarations lose their context.

**What triggers it is 1.24's namespace graph**: the release moves `runtime/internal/{math,sys}` to
`internal/runtime/{math,sys}` and adds `internal/runtime/maps` (the `swissmap` baseline — which is
also why the deletion set removes exactly `runtime/map{,_fast32,_fast64,_faststr}.cs`), so `runtime`
acquires imports it did not have. **Why the qualifier is dropped is NOT established** — the
generator's qualification decision was not read.

> **Two wrong readings, kept on the record because the control is what caught the second.** The first
> bucketing of this rung reported "all 120 in `runtime/runtime.cs`" — **there is no such file**; the
> regex `runtime/[a-z_0-9/]*\.cs` matched a suffix of the real generated paths. And the first root
> offered was "the generator composes `internal/runtime/*` backwards", which is **wrong**: the
> namespace is correct in both releases and only the `global::` qualifier is missing. Reasoning from
> one emission would have banked the second; running the control is what separated a guess from a
> measurement.

**Rungs 4+ remain UNMEASURED.** `runtime` sits under nearly everything, so 169 of 357 projects were
still never reached. The ladder is real and the darwin precedent (19 → 10 → 9 → 0 across four
censuses) is the shape to expect.

---

## 6. Hand-own exposure (H6)

Marker census re-measured at the trial's base: **98 marked files / 75 `*_impl.cs` companions / 108
distinct hand-own files.** Each mapped to its Go principal and compared across releases:

| | files |
|:--|--:|
| **PRINCIPAL GONE in 1.24** | **4** |
| **CHANGED** | **39** |
| identical | 38 |
| no direct Go principal (companions to generated/asm surface) | 27 |

**The four whose principal vanished — the silent-subtraction hazard in its purest form:**

| hand-own | vanished principal |
|:--|:--|
| `src/core/internal/concurrent/hashtriemap.cs` | `internal/concurrent/hashtriemap.go` (408 lines) — package deleted → `internal/sync` |
| `src/core/internal/weak/pointer.cs` | `internal/weak/pointer.go` (83 lines) — package deleted → public `weak` |
| `src/core/crypto/subtle/xor_generic.cs` | `crypto/subtle/xor_generic.go` (64 lines) |
| `src/core/vendor/golang.org/x/crypto/sha3/xor.cs` | vendored package deleted → real `crypto/sha3` |

⚠ **Two of those four are hand-owned-by-consequence packages** — `internal/concurrent` and
`internal/weak` are two of the four packages whose every non-test Go file is hand-owned, so their
`.csproj`, `package_info.cs` and `README.md` are hand-owned by consequence and never re-emitted.
**Their Go packages cease to exist in 1.24.** They are not "a hand-own to refresh"; they are a
package-identity question the hop has to answer deliberately.

The 39 with a changed principal, heaviest first: `reflect/value.go` −323, `time/time.go` +245,
`sync/mutex.go` −195, `os/user/lookup_windows.go` +153, `runtime/lock_sema.go` −121 (×2 flavors),
`internal/syscall/windows/zsyscall_windows.go` +108, `runtime/lock_futex.go` −93.

**The registration table survives intact, and this is the good news.** All **242**
`manualConversionFuncs` registrations across 15 packages were checked for a surviving declaration in
both releases: **239 present in both, 3 outside the predicate's reach** (`internal/reflectlite`'s
`Field`, `TField`, `Zero` — linkname-forwarded, not declared as `func` in that package), **0 lost, 0
whose package vanished.**

The predicate was controlled in all three directions before that zero was believed: a known-present
name reads true in both; a fabricated name reads false in both; and a deliberately-vanished package
(`internal/weak.Make`, `internal/concurrent.NewHashTrieMap`) reads **true → package-gone**, so the
zero is a measurement rather than a blind spot.

---

## 7. Roster exposure — the rebank bill

201 rows, **27,734 banked matching verdicts**. Each row's `_test.go` set hashed in both releases:

| | rows | verdicts | share |
|:--|--:|--:|--:|
| **Go package GONE in 1.24** | **10** | **2,321** | 8.4% |
| **test sources CHANGED** | **99** | **13,146** | 47.4% |
| test sources BYTE-IDENTICAL | 92 | 12,267 | 44.2% |

**The 10 gone rows** — every one a crypto-reorg or internal-package casualty:
`crypto/internal/nistec` (**2,195** verdicts, 5 disclosed), `crypto/internal/edwards25519` (54, 1),
`crypto/internal/edwards25519/field` (16), `crypto/internal/bigmod` (14),
`crypto/internal/mlkem768` (12), `internal/concurrent` (20), `runtime/internal/sys` (4),
`internal/weak` (4), `crypto/internal/alias` (1), `runtime/internal/math` (1).

Nine of the ten have an obvious successor path (`crypto/internal/fips140/*`, `internal/sync`,
`internal/runtime/*`, public `weak`), so the likely disposition is **re-home, not retire** — but a
re-homed row is a **new** row that must earn its count from its own sweep, not inherit one. **2,195
of those verdicts are one package**, and `crypto/internal/nistec` is also the fleet's declared
**cost canary** for descriptor synthesis, so its disposition has a second consequence beyond the
roster arithmetic.

**Heaviest changed rows by test-line delta** (the rebank's weight): `net/http` +1,370 (1,343
verdicts), `crypto/tls` +830 (**3,643**), `crypto/x509` +805 (341), `crypto/cipher` +630,
`debug/elf` +489, `encoding/json` +355 (491), `time` +320 (169), `go/types` +253 (557),
`debug/buildinfo` +232 (197), `crypto/rsa` +208 (559).

**A byte-identical test source does NOT mean a row re-validates** — the production code under it
changed for 134 packages. It means the test-source regeneration is a no-op for that row, which is
the cheaper half of the bill.

---

## 8. What this record does NOT establish

1. ~~**Rung 3**~~ — **MEASURED, §5**: a `global::`-qualification loss in go2cs-gen's using emission,
   0 occurrences in the control vs 29 in the trial. **Rungs 4+ remain unmeasured**: 169 of 357
   projects were still never reached, including whether §4's 29-site generic-constraint warning is
   right that its emission does not compile. WHY the qualifier is dropped is also not established.
2. **Anything operational.** No test suite was converted, built or run at 1.24. The roster bill in §7
   is an exposure census, not a validation forecast.
3. **Windows and darwin flavors at 1.24.** Only `-p:GoTargetOS=linux` was built.
4. **Whether the two fully-vanished hand-owned-by-consequence packages have a mechanical successor**
   or need a design decision. §6 names the question; it does not answer it.
5. **SDK-shaped effects.** This host's .NET SDK is Canonical's source build. Any figure here that
   ever looks SDK-shaped needs a Microsoft-build host to confirm.
6. **`-tests` behaviour under 1.24** — not attempted; `-tests` is where the roster bill is actually
   paid and none of it was exercised.

---

## 9. Sequencing recommendation

**Hold the hop until the 1.23.12 objective is met.** This is the owner's stated order and the
measurements support it rather than merely complying with it:

**(a) The hop's cost lands almost entirely on the ROSTER, and the roster is what the current
objective is still moving.** 109 of 201 rows are exposed (10 gone + 99 changed) — **55.8% of banked
verdicts**. Every row validated on 1.23.12 between now and the hop is a row that must be re-derived
after it. Hopping first does not avoid that work; it *duplicates* it, because the remaining rows
would be validated once on 1.23.12's successor and again at whatever the corpus settles on.

**(b) The converter side is nearly free, which is exactly why it should not drive the schedule.**
342/342 converted with zero type-check failures, one new warning class in two new packages, and one
single-site keyword-escape defect. **The front end is not the constraint**, so there is no
"conversion gets harder if we wait" argument to weigh against (a).

**(c) The two package-identity questions want a decision, not a migration.** `internal/concurrent`
and `internal/weak` are hand-owned-by-consequence packages that cease to exist. Answering them under
schedule pressure, mid-hop, is how a package acquires a hand-own nobody can later explain.

### Estimated cost, stated as ranges because they are estimates

| | estimate | confidence |
|:--|:--|:--|
| converter arcs | **1 measured** (the `_@` composed-identifier escape, 3 sites, 1 root) **+ unknown from rungs 3+** | the 1 is measured; the rest is not |
| corpus deletions | **31 files**, mechanically classifiable by emitted-vs-seeded | high |
| hand-own refreshes | **39 changed principals** + **4 vanished**, of which 2 are package-identity decisions | high |
| registration table | **0 changes** | high, three-way controlled |
| roster rebank | **99 rows / 13,146 verdicts** re-derived; **10 rows / 2,321 verdicts** re-homed or retired | high |
| unmeasured | the compile ladder past `runtime`; all operational validation | — |

### Runbook amendments this capture suggests — proposed, not applied

1. **H1 gains the module-proxy route.** `GOTOOLCHAIN=<release> go version` fetches a toolchain from
   `proxy.golang.org` when `go.dev` is unreachable, and H1's read-only-cache warning is exactly the
   step that route requires. Worth stating as a first-class alternative rather than a workaround.
2. **H1.3 is not a build precondition.** Measured: the converter builds under go1.24.13 at the
   current x/tools and x/mod. H1.3 should say what it is *for* (front-end behaviour) rather than
   implying the build needs it.
3. **H3 should require the emitted-vs-seeded classification, not just a package diff.** §5's rung 1
   is invisible to a package census and to a reconvert diff alike; only the sentinel-mtime split
   finds a file the converter has stopped emitting. The deletion set is a first-class H3 output.
4. **A stage between H3 and the corpus regen: the build-tag/GOEXPERIMENT baseline delta.** Comparing
   `go list -f '{{.GoFiles}}'` for `internal/goexperiment` between releases is one command and it
   predicted rung 1 exactly. Any release can move an experiment into baseline, and the corpus
   silently keeps the superseded variant.
5. **H6 should split "hand-own whose principal changed" from "hand-own whose principal VANISHED",
   and again from "hand-owned-by-consequence PACKAGE that vanished".** They are three different
   bills; only the first is a refresh.

---

## Appendix — reproducing this

```
GOTOOLCHAIN=go1.24.13 go version                    # fetch via the module proxy
cp -r $GOMODCACHE/golang.org/toolchain@v0.0.1-go1.24.13.linux-amd64 $HOME/sdk/go1.24.13
chmod -R u+w $HOME/sdk/go1.24.13                    # H1: strip the read-only attribute

# scratch converter, H1.2 applied
sed -i 's/^go 1\.23\.12$/go 1.24.13/' <scratch>/go.mod
GOROOT=$HOME/sdk/go1.24.13 GOTOOLCHAIN=local go build -o <bin>/go2cs-trial .
go version <bin>/go2cs-trial                        # route #4: verify the embedded toolchain

# two seeded roots (src/core minus build output, src/gen, version.props, docs/validation),
# .cs count asserted equal on both, sentinel touched before each conversion
sed -i 's|1\.23\.12|1.24.13|' <trial>/src/version.props     # H2: the SCRATCH pin only
CGO_ENABLED=0 <bin>/go2cs-ctrl  -stdlib -comments -go2cspath <ctrl>/src
CGO_ENABLED=0 <bin>/go2cs-trial -stdlib -comments -go2cspath <trial>/src

dotnet build <trial>/src/go2cs-stdlib.slnx -c Debug -m --no-incremental \
  -p:GoTargetOS=linux -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Classify every emitted file by the sentinel mtime before reading any diff.
