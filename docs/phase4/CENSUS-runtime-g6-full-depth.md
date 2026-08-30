# G6 — `runtime`, the first full-depth pipeline run

**Lane:** G6 measurement, i7-5820K coordinator machine (Windows 11, PowerShell 5.1)
**Date:** 2026-08-30
**Tree:** isolated worktree, detached at `ba3be3c67` (= `origin/master` at start **and** at finish; includes `df9e88c71`), clean at start, **pristine at finish**
**Toolchain:** .NET SDK 10.0.400, Go 1.23.12 (windows/amd64), converter built from `src\go2cs` at that commit (6.5 s)
**Scope:** MEASUREMENT ONLY. No fixes, no commits. One probe patch was applied to an *untracked, emitted* artifact to reach the next layer; it is itemised in §7 and was reverted with the tree.

---

## 0. Headline

> **The census called it exactly. All four numbers it staked came back to the digit.**
>
> `runtime` now **converts clean**, **restores clean**, and stops at the **build** layer with
> **154 errors** — `W3a 102 / W3b 49 / W3c 3` — against the census's predicted
> `102 / 49 / 3`. Zero new families. Zero escapes.
>
> Both walls the campaign brought down since the census are confirmed down *in this run*, not
> merely in their own lanes: **W1** (no `MSB4006`, the windows `ItemGroup` stays empty, the cycle
> is not re-minted) and **W2a** (zero `Unresolved dynamic struct type` warnings — all five sites
> now emit the production lift names the census's probe had to write by hand).
>
> The one thing that moved: **W2c is now the sole gate on W3.** Three errors in one file — a
> nested block comment — derail the parser and make all 154 W3 errors invisible. It is the
> smallest wall in the document and it currently hides the largest.
>
> **Reachable verdicts today: still 0 of 444.** The run layer was not reached and this run says
> nothing new about it. What it does say is that the *build* bill is fully known, unchanged after
> two days of merges, and one accessibility decision plus four small emission fixes away from the
> first `runtime` verdict in project history.

---

## 1. Prediction vs actual — the scorecard

The census staked numbers. This is the score.

| Layer / family | Census predicted | **Measured** | Verdict |
|---|---|---|---|
| **Conversion** — errors | 0 | **0** | ✅ exact |
| **Conversion** — `Unresolved dynamic struct type` | 3 (W2a, fatal downstream) | **0** | ✅ **W2a DOWN** |
| **Conversion** — const-`Sizeof` warnings | 12 | **12** | ✅ exact |
| **Conversion** — `Sizeof` run-time-form warning pairs | 4 (+4) | **4 (+4)** | ✅ exact |
| **Conversion** — emitted `*_test.cs` | 77 + 2 metadata | **79** | ✅ exact |
| **Conversion** — `ProjectReference`s in `runtime.tests.csproj` | 66 | **66** | ✅ exact |
| **Conversion** — test-surface set match | 444 / 444, 0 drift | **444 / 444, 0 drift** | ✅ exact (re-scored independently) |
| **Restore** — `MSB4006` cycle | fatal (pre-W1) | **none** | ✅ **W1 DOWN** |
| **Build** — production assembly | clean | **clean** | ✅ exact |
| **Build** — W2c | 3 errors / 1 site | **3 errors / 1 site** | ✅ exact |
| **Build** — W3a accessibility | **102** | **102** | ✅ exact |
| **Build** — W3b qualification | **49** | **49** | ✅ exact |
| **Build** — W3c metadata | **3** | **3** | ✅ exact |
| **Build** — total behind W2c | 154 | **154** | ✅ exact |
| **Build** — new families | (none predicted) | **none** | ✅ |
| **Run layer** | unreached | **unreached** | ✅ as predicted |

**Master's tip did not move during the run** (`ba3be3c67` at start and at finish), so the i9's W3
lane had not landed — the build stopping at W3 is the correct and expected outcome, not a stale-tree
artifact.

---

## 2. Layer 1 — CONVERSION: **passes, and W2a is down**

`go2cs -tests -test-action all -test-timeout 30m` — exit 0, **zero errors**, warnings only.

### 2.1 The warning taxonomy shrank by exactly the W2a family

| Warning | Census | **Measured** |
|---|---|---|
| `Go const converted to C# using 'unsafe.Sizeof' may not match run-time value` | 12 | **12** |
| `Go 'unsafe.Sizeof' did not resolve to a constant - emitting run-time form` | 4 | **4** |
| `Go code converted to C# using 'unsafe.Sizeof' may not produce same value as Go` | 4 | **4** |
| **`Unresolved dynamic struct type`** | **3** | **0** |
| **total** | 23 | **20** |

The 12 const-`Sizeof` sites are unchanged and remain the standing latent **run**-layer risk
(`sizeof_test.go` is the test that exists to catch them — still behind the walls).

### 2.2 W2a: the five sites now emit the production lift names

The census's §W2 probe substituted the production pass's own lifted names by hand and called that
"what a fixed converter emits". The fixed converter now emits exactly that, unaided:

```
export_test.cs:288 : public static Func<ifaceHash_i, uintptr, uintptr> IfaceHash;
export_test.cs:1211: Δp.of(global::go.runtime_package.pageAlloc.Ꮡscav).of(pageAlloc_scav.Ꮡindex).alloc(ci, pallocChunkPages);
export_test.cs:1212: Δp.of(global::go.runtime_package.pageAlloc.Ꮡscav).of(pageAlloc_scav.Ꮡindex).free(ci, 0, pallocChunkPages);
export_test.cs:1234: Δp.of(global::go.runtime_package.pageAlloc.Ꮡscav).of(pageAlloc_scav.Ꮡindex).alloc(ci, s.N);
export_test.cs:2069: ᏑgcDebugMarkDone.of(gcDebugMarkDoneᴛ1.ᏑspinAfterRaggedBarrier).Store(spin);
```

A scan of every emitted `*_test.cs` for the literal Go type syntax the census recorded
(`interface{F()}`, `struct{index runtime…`, `struct{spinAfterRaggedBarrier…`) returns **0 hits**.

**Corroboration that the census's probe was faithful:** the `IfaceHash` site did not disappear — it
*migrated families*. It is now `CS0052`, reading
`field type 'Func<runtime_package.ifaceHash_i, uintptr, uintptr>' is less accessible…`. The census's
probe had already substituted the same name, which is why its W3a total and this run's both land on
exactly 102 rather than 101 or 103.

### 2.3 Test-surface reproduction re-scored independently: still exact

The census compared the host against a parsed `go test -json` transcript. This run compared it
against `go test -list '.*' runtime`, a different instrument:

```
host Test names (case-sensitive): 444
oracle Test names:                444
HOST-only:   0
ORACLE-only: 0
```

**Zero drift, two independent methods, two weeks apart.** The conversion layer reproduces
`runtime`'s entire Windows-eligible test surface exactly.

---

## 3. Layer 2 — RESTORE: **passes. W1 is down.**

No `MSB4006`. Restore completed across the closure and `dotnet publish` reached the compiler.

**W1-M held — the cycle was not re-minted.** After the `-tests` run rewrote the production tree,
`src/core/runtime/runtime.csproj:187` still reads:

```xml
<ItemGroup Condition="'$(GoTargetOS)'=='windows'" />
```

— empty, exactly as at baseline. No `internal.syscall.windows` `ProjectReference` was added.

**W1-S confirmed in place at baseline** (the storage inversion, from the census's amendment):

```
src/core/runtime/windows/os_windows.cs:442  //go:linkname canUseLongPaths internal/syscall/windows.CanUseLongPaths
src/core/runtime/windows/os_windows.cs:443  public static bool canUseLongPaths;                      <- storage
src/core/internal/syscall/windows/syscall_windows.cs:22
    public static bool CanUseLongPaths { get => go.runtime_package.canUseLongPaths; set => …; }      <- forwarder
```

`os_windows.cs` was **not** among the files the `-tests` run modified — the inversion is stable
under both emission modes.

**Production assembly compiled clean:** `runtime.dll`, 2,446,336 bytes. As in every census probe,
production is not the problem.

---

## 4. Layer 3 — BUILD, first pass: **3 errors, all W2c**

The pipeline's `-test-action all` died at `dotnet publish` **217 s** in (11:25:15 → 11:28:52), with
exactly three errors:

```
arena_test.cs(60,138): error CS8124: Tuple must contain at least two elements.
arena_test.cs(60,139): error CS1519: Invalid token '8' in a member declaration
arena_test.cs(60,162): error CS1519: Invalid token '/' in a member declaration
```

Line 60 verbatim — the nested block comment:

```csharp
[GoType("[524289]ж<smallPointer>")] /* [runtime_internal_test_package.UserArenaChunkBytes / /* unsafe.Sizeof(&smallPointer{}) */ (uintptr)8 + 1]ж<smallPointer> */
```

**Site census.** A scan of all 81 emitted test/metadata files for lines carrying two or more `/*`
found 6 candidates. Five are **sequential**, well-formed comments on one line and compile fine:

```
export_test.cs:1735   internal channel/*<-*/<EmptyStruct> stop = channel/*<-*/<EmptyStruct>.SendOnly;
export_test.cs:1736   internal /*<-*/channel<EmptyStruct> done = /*<-*/channel<EmptyStruct>.RecvOnly;
mgcpacer_test.cs:380  …, /* DefaultHeapMinimum - 64<<10 */ 4.128768e+06D, /* DefaultHeapMinimum + 64<<10 */ 4.25984e+06D);
runtime_test.cs:354   if (/* unsafe.Sizeof(T2{}) */ (uintptr)16 != 8 + /* unsafe.Sizeof(uintptr(0)) */ (uintptr)8) {
runtime_test.cs:355   Ꮡt.Errorf("sizeof(%#v)==%d, want %d"u8, …, /* unsafe.Sizeof(T2{}) */ (uintptr)16, …);
```

Only `arena_test.cs:60` is genuinely **nested** — the one shape where the `Sizeof` run-time-form
comment is interpolated *inside* an enclosing `[GoType]` type comment. **The census's "1 site" is
exact**, and the discriminator is interpolation-inside-a-comment, not "a line with two comments".

### 4.1 The ordering fact that changed

The census wrote: *"W2 gates W3 — the parser derails before the binder runs."* That is still true,
but the composition changed. With W2a fixed, **W2c alone now gates W3.** Three errors in one file
hide 154. Every future measurement of W3 needs the same probe until W2c lands — which makes W2c,
at 1 site, the cheapest unblock in the entire wall stack and a prerequisite for *measuring* W3, not
just for passing it.

---

## 5. Layer 3 — BUILD, past the W2c probe: **154 errors, W3 exactly as censused**

With the nested comment un-nested (§7), the build advances to the binder in **~50 s** and stops at
W3. **154 error lines, 154 unique, 100 % in `runtime.tests.csproj`.** Production clean. **Zero
non-CS errors, zero MSB errors — no new families.**

| Family | Codes | Census | **Measured** | Δ |
|---|---|---|---|---|
| **W3a** accessibility | `CS0050` | 34 | **34** | 0 |
| | `CS0053` | 34 | **34** | 0 |
| | `CS0051` | 19 | **19** | 0 |
| | `CS0056` | 7 | **7** | 0 |
| | `CS0057` | 7 | **7** | 0 |
| | `CS0052` | 1 | **1** | 0 |
| | **subtotal** | **102** | **102** | **0** |
| **W3b** qualification | `CS0426` | 49 | **49** | 0 |
| **W3c** metadata | `CS0246` | 2 | **2** | 0 |
| | `CS0234` | 1 | **1** | 0 |
| | **subtotal** | **3** | **3** | **0** |
| **TOTAL** | | **154** | **154** | **0** |

### 5.1 File spread — 43 of 48 files are generated, which is the sizing result

48 distinct files carry the 154 errors. **43 are go2cs-gen generated adapters** (`*.g.cs`); only
**5 are hand-emitted**. The census's claim that W3a's price is "far below what 102 errors across 50
files suggests" is confirmed structurally: the generated files follow automatically from the
accessibility level the converter picks, so the fix surface is *the converter plus go2cs-gen*, not
48 files.

| File | errors | kind |
|---|---|---|
| `go.runtime_internal_test_package.MSpan.g.cs` | 29 | generated |
| `go.runtime_internal_test_package.PageAlloc.g.cs` | 23 | generated |
| `export_debuglog_test.cs` | 17 | emitted |
| `go.runtime_internal_test_package.ProfBuf.g.cs` | 13 | generated |
| `go.runtime_internal_test_package.ΔPallocData.g.cs` | 8 | generated |
| `go.runtime_internal_test_package.PageCache.g.cs` | 5 | generated |
| `go.runtime_internal_test_package.ΔPallocBits.g.cs` | 5 | generated |
| `go.runtime_internal_test_package.TimeHistogram.g.cs` | 5 | generated |
| `mpallocbits_test.cs` | 4 | emitted |
| `go.runtime_internal_test_package.AddrRanges.g.cs` | 3 | generated |
| `package_test_info.cs` | 3 | emitted |
| `export_test.cs` | 3 | emitted |
| (36 more) | 36 | 33 generated / 3 emitted |

### 5.2 One verbatim exemplar per code

```
--- CS0050 (W3a) ---
export_debuglog_test.cs(24,53): error CS0050: Inconsistent accessibility: return type
  'ж<runtime_package.dlogger>' is less accessible than method
  'runtime_internal_test_package.B(ж<runtime_package.dlogger>, bool)'

--- CS0051 (W3a) ---
export_debuglog_test.cs(20,20): error CS0051: Inconsistent accessibility: parameter type
  'ж<runtime_package.dlogger>' is less accessible than method
  'runtime_internal_test_package.End(ж<runtime_package.dlogger>)'

--- CS0052 (W3a) ---
export_test.cs(288,51): error CS0052: Inconsistent accessibility: field type
  'Func<runtime_package.ifaceHash_i, uintptr, uintptr>' is less accessible than field
  'runtime_internal_test_package.IfaceHash'

--- CS0053 (W3a) ---
Generated\…\go.runtime_internal_test_package.ProfBuf.g.cs(33,112): error CS0053: Inconsistent
  accessibility: property type 'runtime_package.profAtomic' is less accessible than property
  'runtime_internal_test_package.ProfBuf.r'

--- CS0056 (W3a) ---
Generated\…\go.runtime_internal_test_package.ProfBuf.g.cs(70,41): error CS0056: Inconsistent
  accessibility: return type 'runtime_package.profBuf' is less accessible than operator
  'runtime_internal_test_package.ProfBuf.implicit operator runtime_package.profBuf(…)'

--- CS0057 (W3a) ---
Generated\…\go.runtime_internal_test_package.ProfBuf.g.cs(68,41): error CS0057: Inconsistent
  accessibility: parameter type 'runtime_package.profBuf' is less accessible than operator
  'runtime_internal_test_package.ProfBuf.implicit operator runtime_internal_test_package.ProfBuf(…)'

--- CS0426 (W3b) ---
Generated\…\go.runtime_internal_test_package.InUse.global__go.runtime_internal_test_package.PageAlloc.g.cs(16,69):
  error CS0426: The type name 'AddrRange' does not exist in the type 'runtime_package'

--- CS0234 (W3c) ---
package_test_info.cs(124,57): error CS0234: The type or namespace name
  'runtime_internal_test_package' does not exist in the namespace 'go.go'

--- CS0246 (W3c) ---
package_test_info.cs(140,35): error CS0246: The type or namespace name 'unsafe' could not be found
```

### 5.3 The three W3 roots, confirmed at source

- **W3a** — `export_debuglog_test.cs:24` still emits
  `public static ж<global::go.runtime_package.dlogger> B(this ж<…> Ꮡl, bool x)`. Across emitted
  `*_test.cs` there are **13** `public static` members naming `runtime_package` in their signature
  against **10** `internal static` equivalents — i.e. the level choice, not the architecture, is the
  variable. `InternalsVisibleTo` remains emitted at `runtime.csproj:33`, so every error is
  *"less accessible"* and never *"inaccessible"*.
- **W3b** — `export_test.cs:23` still emits
  `using ꓸꓸꓸAddrRange = Span<runtime_package.AddrRange>;` while `AddrRange` is declared at
  `export_test.cs:1049` as `[GoType] public partial struct AddrRange` in the **test** package.
  **One** alias-minting site; all 49 errors are downstream `RecvGenerator` adapters.
- **W3c** — `package_test_info.cs:124` emits
  `global::go.go.runtime_internal_test_package.TestingT` (double-root escape) and `:140` emits
  `objWith<@unsafe.Pointer>`. *Note:* the `go.go.` prefix on lines 41–59 (`go.go.token_package`,
  `go.go.types_package`) is **legitimate** — those are the real `go/token`, `go/types` packages.
  Only line 124 is the escape. A naive `go.go.` grep over-reports this family 4×.

---

## 6. Production-emission finding: `runtime`'s corpus is **two converter arcs stale** (chip, not an escape)

The `-tests` run left **17 tracked production files** modified. Classified against the standing
sweep-dirt taxonomy:

| Class | files | status |
|---|---|---|
| **1. CRLF phantoms** (empty `--numstat`) | 3 — `mgcpacer.cs`, `mpagealloc.cs`, `windows/extern.cs` | documented |
| **2. Δ-alias family** (`using math` → `using Δmath`) | `chan.cs`, `map.cs`, `slice.cs`, `unsafe.cs`, `hash64.cs`, `rand.cs`(part) | documented |
| **2. `initᴛᴛtests` hook** (+7/−0) | `windows/package_init.cs` | documented |
| **NOT in the four families** | `string.cs`, `rand.cs`(part), `metrics.cs`, `windows/package_info.cs` | **see below** |

Three shapes are **not** any of the four documented `-tests`-closure families:

1. **`len()` of a pointer-to-array folded to a constant** — `string.cs`, `rand.cs`:
   ```diff
   -    ref var buf = ref Ꮡbuf.DerefOrNull();
   -    if (Ꮡbuf != nil && n <= len(buf.Value)){
   +    if (Ꮡbuf != nil && n <= 32){
   -            seed.Value[i % len(seed)] ^= (byte)(c);
   +            seed.Value[i % 32] ^= (byte)(c);
   ```
2. **Nil-safe delegate conversion** — `metrics.cs`:
   ```diff
   -    d.compute = (…) => new metricReader(read).compute(p1, p2);
   +    d.compute = (…) => NilSafeDelegateConversion<metricReader, Func<uint64>>(read).compute(p1, p2);
   ```
3. **`windows/package_info.cs` 139/139 `GoPositionMap` churn** — consequential to (1); folding
   shifts line positions, so the position maps re-encode.

**Attribution — these are corpus staleness, not a new `-tests` family:**

| Shape | Converter commit | Date | runtime corpus files it touched |
|---|---|---|---|
| `len`/`cap` of pointer-to-array is a constant | `41a0998a1` | **2026-08-28** | **0** |
| `NilSafeDelegateConversion` | `d1aa2b73f` | **2026-08-29** | **0** |

`runtime`'s last corpus regen is **`ae7ed9103` (2026-08-14)**, which predates both. Neither commit
carried the runtime regen it implied. `NilSafeDelegateConversion` appears in **zero** committed
production corpus files (only in golib's definition and four banked `net/http` *test* sources —
themselves emitted after the change).

**Confidence and its limit.** Both emission sites live in mode-agnostic converter code
(`convCallExpr.go:822` and the `convExpr`/`convCompositeLit` array-length path); neither is gated on
`-tests`. Combined with the date arithmetic that is strong evidence, but it is **inference, not a
control**. The decisive control is cheap and was not run to stay inside the measurement brief:
`go2cs -stdlib runtime -comments -go2cspath <tmp>\src` into a seeded temp root, diffed against the
committed tree. Recommend it be run as part of whichever regen lands.

**Net:** worth a chip — *`runtime`'s production corpus owes a regen, two arcs behind*. It is **not**
a G6 escape, does not affect any verdict in this document, and did not stop production compiling
clean.

---

## 7. Probe honesty

**One** probe, on an **untracked, emitted** artifact (`src/core/runtime/arena_test.cs`, `?? ` in
`git status`, never committed — `runtime` is not a banked row).

- **W2c** — un-nested the comment at `arena_test.cs:60` by removing the *inner* `/*` `*/`
  delimiters, a pure-ASCII single-occurrence replacement, byte-preserving elsewhere (read/written as
  UTF-8, no BOM, matching the file). The original was kept as `arena_test.cs.g6-orig` and both were
  removed with the tree.
- Everything in §5 is labelled as sitting behind that probe. **No verdict in this document comes
  from a test run, because no test ran.**
- **Restore:** `git checkout HEAD -- src/core/runtime` (17 files) then `git clean -fdx`.
  **Final `git status`: clean. HEAD `ba3be3c67`.** Worktree build output purged.

---

## 8. Reproduction ledger

| Step | Command | Wall | Result |
|---|---|---|---|
| Converter build | `go build -o bin\go2cs.exe .` | 6.5 s | exit 0, stamped `go1.23.12` |
| Pipeline | `go2cs -tests -test-action all -test-timeout 30m -go2cspath <wt>\src <GOROOT>\src\runtime <wt>\src\core\runtime` | **217 s** | conversion ✅, restore ✅, **W2c**: 3 errors |
| Build probe | `-test-action build` (W2c un-nested) | **~50 s** | **W3**: 154 errors |
| Oracle name set | `go test -list '.*' runtime` | ~5 s | 444 names, exact match to host |

**Total lane machine time: ~5 minutes.** `runtime` remains cheap to measure.

---

## 9. What this prices

### 9.1 For W3's landing

- **W3 is the last build wall, and it has not drifted.** 154 errors, same nine codes, same three
  roots, two days and several merges after the census. A lane can land it against a fixed target.
- **W2c must land with or before W3.** It is 1 site / 3 errors and it currently makes W3
  *unmeasurable* — not merely unfixed. Any W3 lane that does not carry W2c will keep re-applying the
  same probe to see its own work. Cheapest item in the stack, and now on the critical path for
  measurement as well as for passing.
- **W3a's price is confirmed low by structure, not by hope.** 43 of 48 error-bearing files are
  generated; the fix is the accessibility level the converter picks for test wrappers plus the
  matching go2cs-gen change. `InternalsVisibleTo` is already emitted, so nothing architectural moves.
- **W3b is one alias-minting site** and **W3c is two known escape families at two lines**. Both are
  independent of W3a and of each other — four separable pieces of work, none blocking another.
- **Landing W2c + W3 makes the `runtime` test assembly compile for the first time, and the run layer
  becomes reachable for the first time.** Nothing else stands in between. That is the single most
  consequential statement this run supports.

### 9.2 For W4 and the semantic tail

- **W4 is unchanged and still unmeasured** — 91 of 444 tests (20.5 %) build and exec a separate Go
  program. This run adds *no* information about it, because nothing ran. That is the honest answer,
  and the census's refusal to guess the semantic bill stands.
- **The denominator is firmer.** The host registers 444 and the oracle's top-level `Test` set is 444,
  exact, re-verified by a second instrument. The census's roster note holds: a real row banks against
  the **un-gated** suite at **843 leaf / 446 top-level**, with `ExampleFrames` and `FuzzPIController`
  in the surface but not registered by the host (§2.1 of the census — costs 1–2 verdicts).
- **First honest slice after W3:** ~79.5 % of the 444 (353 tests) do *not* touch the subprocess
  machinery, so a W3 landing should expose a substantial measurable population immediately rather
  than trading one wall for another. SCHED-S1 shapes (`proc_test.go` 29, `chan_test.go` 14),
  liveness (`gcinfo_test.go`, `unsafepoint_test.go`, `stack_test.go`, `traceback_test.go`) and the
  12 `unsafe.Sizeof` constants (`sizeof_test.go`) are the named candidates for the first real
  divergence measurements — all still **flagged, not chased**.
- **A C#-side `$longTimeouts` floor remains unsizable.** The Go oracle needs none (118 s un-gated),
  but `runtime`'s 18 `hash_test.go` tests are the `smhasher` family and `hash/maphash` is the
  standing precedent for seconds-in-Go / ~15-min-in-C#. It cannot be sized until the suite runs.

### 9.3 Recommended order (unchanged from the census, with one promotion)

1. **W2c** — promoted. 1 site, 3 errors, and it now gates *measurement* of W3, not just its fix.
2. **W3a** — the accessibility level. Largest count, low price, and it is what first makes any
   `runtime` verdict reachable.
3. **W3b / W3c** — three independent small emission fixes.
4. **Chip:** `runtime`'s production corpus regen (§6), two arcs behind; fold the `-stdlib` control
   into it.
5. **Then re-census at the run layer.** W4 and the semantic tail remain unbilled by design.

**W2b, the gate the census recommended first, is now moot for `runtime`** — W2a is fixed, so the
package no longer reproduces "Unresolved dynamic struct type". Its value as a *standing* gate for
future first-contacts is undiminished, but this package can no longer serve as its free positive
control.
