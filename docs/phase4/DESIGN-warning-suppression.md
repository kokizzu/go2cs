# DESIGN — the .NET build-warning census, and what to suppress in the generated `.csproj`

> Measured **2026-08-07** (r46a-warnaudit) against `src/go2cs-stdlib.slnx` at `273f126340`,
> 304 projects, isolated builds (`MSBUILDDISABLENODEREUSE=1`, `-p:UseSharedCompilation=false`).
> The deliverable is the table in §4 plus the judgment in §5: which codes are structural to the
> emission model and will never go away, which are a converter defect wearing a warning's clothes,
> and which are a build *property* problem that `NoWarn` is the wrong instrument for.
>
> **STATUS: the configuration half is IMPLEMENTED** (r46b-warnsuppress, 2026-08-08) — §10 records
> what landed and what it measured. The two converter roots (§5.1 `CS0219`, §5.2 `CS8778`) and the
> golib/`go2cs-gen` items (§7) are deliberately NOT done here; they remain open board rows, and the
> do-not-suppress rulings in §4 stay binding.

## 1. The measured baseline

Four full solution builds, each capturing every warning to a file logger
(`-flp:LogFile=…;Verbosity=normal;WarningsOnly`) rather than a console filter:

| Run | Configuration | Warnings |
|---|---|---|
| A | `-c Debug`, as committed | **4,147** |
| B | `-c Release`, as committed | **4,147** (code-for-code identical to A) |
| B2 | `dotnet pack -c Release --no-build` | **0** additional — no `NU####`, no `NETSDK####` |
| C | `-c Debug -p:NoWarn=` (existing suppression cleared) | **15,445** |
| D | `-c Debug` + the §5 proposal | **6,181** measured / **1,903** with `CS8618` retained (§6) |

Debug and Release are *identical*, so there is one number to manage, not two. The "2,315 on a Debug
build" and "4,148 on the Release pack" figures that opened this arc reconcile as: 4,148 ≈ the true
full-build number (4,147 here, ±1 for a transient restore line), and 2,315 was an **incremental**
build — MSBuild reports warnings only for projects it actually recompiles, so a partially up-to-date
tree always under-reports. Any warning count quoted for this corpus must come from a clean or
`-t:Rebuild` build.

**199 of the 304 projects emit at least one warning**; 105 are already silent. The distribution is
heavily skewed — `math/rand` (614), `runtime` (478), `go/types` (352), `net/http` (229), `net` (178)
carry 45 % of the total between them.

Nothing is `TreatWarningsAsErrors`, and no code-style analyzer runs at the command line
(`EnforceCodeStyleInBuild` is unset), so `IDE####` diagnostics are a Visual Studio-only concern —
they are not in any of these counts.

## 2. Where the suppression lives today

One line, in one file, copied into every generated project:

```
src/go2cs/csproj-template.xml:17       <NoWarn>CS0282;CS0660;CS0661;CS8618;CS8981;IDE0060;IDE1006;CA2255</NoWarn>
src/go2cs/test-csproj-template.xml:34  <NoWarn>CS0282;CS0660;CS0661;CS8618;CS8981;IDE0060;IDE1006;CA2255</NoWarn>
```

*(Line numbers as of the audit; both lines moved when §10 landed — `csproj-template.xml:25`,
`test-csproj-template.xml:33`.)*

It grew by accretion: `CS0660;CS0661;CS8981;IDE0060;IDE1006` (`3805a0551`, 2025-01-16), `+CS0282`
(`6315d6658`, 2025-01-27), `+CA2255` (`1d2344db4`, 2026-07-11), `+CS8618` (`6411ed09f`, 2026-07-13).

`golib` keeps its own, older list — `660;661;1701;1702;IDE1006;CA2255;CS8500;CS8981` — with bare
numeric codes and two entries (`1701`/`1702`, assembly-binding redirects) that cannot fire on
`net9.0`.

Hand-owned `.csproj` files carry a *copy* of the template list and need the same edit by hand:
`src/core/unsafe/unsafe.csproj`, `src/core/internal/godebug/internal.godebug.csproj` (hand-owned by
consequence — its only Go file is fully hand-owned, so `unmarkedFileCount == 0` makes the driver
`continue` before `writeProjectFile`), and `src/core/testing/testing.csproj` (which carries a *shorter*
list, `CS8981;IDE1006`).

> **Correction (r46b).** That list of three is **incomplete**, and the audit had no way to see it: the
> hand-owned set is not a property of the file, it is whichever production `.csproj` a `-stdlib`
> reconvert does not re-emit — so the only reliable census is to reconvert into a seeded temp root and
> diff. Doing that found **five**: `internal/weak` and `internal/concurrent` are hand-owned by the same
> `unmarkedFileCount == 0` consequence as `internal/godebug` (their single Go file is fully hand-owned),
> and both were silently carrying the old list. Add `golib` (which keeps a deliberately different list,
> §7) and the by-hand set is six. Do **not** carry this number forward either — it moves whenever a
> package's last unmarked file acquires a marker; re-measure it the same way.

## 3. Re-justification of the existing eight entries

Measured by clearing `NoWarn` corpus-wide (run C). The existing list suppresses **11,298 warnings** —
73 % of everything the compiler has to say about this corpus.

| Entry | Fires | Projects | What it is | Verdict |
|---|---:|---:|---|---|
| `CS8981` | 6,166 | 303 | "type name `X` only contains lower-cased ascii characters" — *every* Go type name, plus the `any`/`rune`/`uint8` global-using aliases | **KEEP.** Structural and permanent; Go's naming convention is the whole point. |
| `CS8618` | 4,421 | 182 | non-nullable field uninitialized — Go zero values. **4,278 of them are in `go2cs-gen` output**, not converter output | **KEEP** — and see §6: it cannot be replaced by the `Nullable` property because generated files carry their own `#nullable enable`. |
| `CS0660` | 227 | 81 | type defines `==`/`!=` without overriding `Equals` — the converter emits Go comparison operators on value types | **KEEP.** Structural. |
| `CS0661` | 226 | 81 | …without overriding `GetHashCode` | **KEEP.** Same emission. |
| `CS0282` | 206 | 64 | "no defined ordering between fields in multiple declarations of partial struct" — go2cs splits a struct's fields between its type file and `package_info.cs` | **KEEP.** Structural to the partial-class model. |
| `CA2255` | 53 | 39 | `[ModuleInitializer]` "only intended for application code" — go2cs *aliases* it to `GoInitAttribute` to model Go `init()` | **KEEP.** The analyzer's premise (libraries shouldn't self-initialize) is exactly what Go semantics require. |
| `IDE0060` | 0 | 0 | unused parameter | **KEEP, inert at the CLI.** Only fires in Visual Studio's live analysis; that is where it would be loud (every `_`-shaped Go parameter). |
| `IDE1006` | 0 | 0 | naming rule violation | **KEEP, inert at the CLI.** Same — and the VS noise would be enormous (all Go identifiers). |

Nothing in the list is stale. Two entries in **golib's** list are: `1701` and `1702` are .NET Framework
assembly-binding warnings that cannot occur on `net9.0`, and the bare `660;661` should be written
`CS0660;CS0661` for consistency with everything else. `CS8500` in golib's list is *justified* (see §7).

## 4. The census — every code, sorted by count

Classification key: **S** = structural to the emission model (suppress) · **F** = fixable in the
converter (keep visible, root it) · **G** = golib/`go2cs-gen`-local · **P** = a build *property*
problem, not a `NoWarn` problem.

| Code | n | Projects | What the emission actually looks like | Class | Recommended action | Risk of suppressing |
|---|---:|---:|---|:--:|---|---|
| `CS0219` | 1,219 | 136 | `rune r = default!;` — the **named-return prologue**. `1,218 of 1,219` are exactly that shape; the one exception (`bufio/scan.cs:169`) is a folded local const. The function returns explicit tuples, so the declaration is dead | **F** | **Fix in converter**: emit the named-return local only when the body references it. Do *not* suppress | A dropped assignment to a named return would leave `default` flowing out of a bare `return` — `CS0219` is the only static signal for that. Suppressing hides the one case that matters |
| `CS8778` | 620 | 7 | `-(nint)4181792142133755926L` inside `new int64[]{…}` (`math/rand/rng.cs`, 607 of the 620). Untyped Go constants take the default `int`→`nint` type instead of the composite literal's `int64` element type | **F** | **Fix in converter** (two roots, §5.2). Do *not* suppress | This is a **live 32-bit truncation bug** the compiler is pointing straight at. Suppressing it deletes the only warning in the corpus that is unambiguously right |
| `CS0162` | 607 | 70 | Two shapes: **386** are the body of an `if (constFalse)` — `raceenabled`, `msanenabled`, `boring.Enabled`, `debugFloat`; **221** are a `break;` the converter appends after a `case` body that already ends in `throw panic(…)`/`return` | **S** | **Suppress in template** — *and* stop emitting the redundant `break;` (source cleanliness, not warning count) | Unreachable code is inert by construction; the residual risk is an unintended early `return`, which the Phase-4 verdict comparison catches |
| `CS8604` | 495 | 66 | `Possible null reference argument` — `(~pe)` after `err._<ж<fs.PathError>>(ᐧ)`; the type-assertion helper's failure value is `null` and the flow analysis does not follow the `ok &&` guard | **P** | `<Nullable>annotations</Nullable>` (§6) | See §6 — Go has no non-nullable pointer, so this analysis is answering a question the language cannot pose |
| `CS8602` | 243 | 53 | `Dereference of a possibly null reference` — same family | **P** | `<Nullable>annotations</Nullable>` | " |
| `CS8619` | 226 | 65 | `(nint, error? err)` doesn't match `(nint n, error err)` — the named-return local is `error err = default!` (non-null) while the tuple literal infers `error?` | **P** | `<Nullable>annotations</Nullable>` | " |
| `CS0164` | 181 | 35 | Unreferenced labels: **91** are the converter's synthetic `continue_<label>:` / `break_<label>:` pair, emitted for every labeled Go statement whether targeted or not; **90** are the Go label itself (`Loop:`, `bucketloop:`) | **S** | **Suppress in template**; optionally emit labels only when referenced | An orphaned label could mean a dropped `goto`, but a dropped `goto` changes behavior and is caught downstream |
| `CS8974` | 115 | 5 | `["and"u8] = and` in a `map<@string, any>` — Go stores function values in `map[string]any`; also `abi.FuncPCABIInternal(mapaccess2_fast64)` | **S** | **Suppress in template** | "Did you mean to invoke it?" — the converter decides call-vs-value from the AST, so a false positive here is not a class go2cs can produce accidentally |
| `CS1717` | 77 | 24 | `(x, err) = (x, default!);` — the named-return store on the `goto ᒐdone` path through a defer frame | **S** | **Suppress in template** | Self-assignment is a no-op; the emission is generated, never hand-written |
| `CS8603` | 69 | 36 | `Possible null reference return` | **P** | `<Nullable>annotations</Nullable>` | See §6 |
| `CS8601` | 40 | 20 | `Possible null reference assignment` | **P** | `<Nullable>annotations</Nullable>` | " |
| `IL2091` | 37 | 9 | trim analyzer: generic argument lacks `DynamicallyAccessedMembers` | **P** | Condition the publish properties (§6.2) | The analysis re-runs at *app* publish where it is actionable; nothing is permanently lost |
| `CS8600` | 35 | 18 | `Converting null literal or possible null value to non-nullable type` | **P** | `<Nullable>annotations</Nullable>` | See §6 |
| `IL2026` | 31 | 9 | `RequiresUnreferencedCode` — `StackFrame.GetMethod()` in `runtime/managed_impl.cs`, `encoding/json` reflection | **P** | Condition the publish properties | See `IL2091` |
| `IL2111` | 27 | 6 | `Delegate.CreateDelegate` reached via reflection (`time/sleep.cs`) | **P** | Condition the publish properties | " |
| `CS8714` | 19 | 2 | `K` doesn't satisfy the `notnull` constraint on golib's `IMap<TKey,TValue>` (`maps/iter.cs`, 15) | **P**/G | `<Nullable>annotations</Nullable>` clears it; the durable fix is golib relaxing `where TKey : notnull` (`src/core/golib/map.cs:50`) or the converter emitting `notnull` on Go type parameters | Low — it is a constraint-annotation mismatch, not a nullability defect |
| `CS8500` | 15 | 4 | `takes the address of … a managed type` — `fixed (void* ptr = &value.Value)` in `TypeGenerator` output (10) plus converter unsafe reinterprets in `runtime/iface.cs`, `os/user`, `internal/abi` | **G**/F | **Do NOT suppress corpus-wide.** golib's local suppression stays; the 10 generated ones are a `go2cs-gen` board row | **High.** This is the static signal for the exact managed-referent hazard the S1 fork ruling is about. Silencing it corpus-wide removes the only compile-time tell |
| `IL2070` | 14 | 2 | trim analyzer, `Type.GetFields` etc. (10 in golib) | **P**/G | Condition publish properties for the corpus; golib should **annotate**, not silence (§7) | See `IL2091` |
| `CS8625` | 14 | 6 | `Cannot convert null literal to non-nullable reference type` | **P** | `<Nullable>annotations</Nullable>` | See §6 |
| `CS0675` | 11 | 4 | `bits \|= (uint64)((byte)(c & ~0x20) - 'A' + 10)` — sign-extended `int` widened to `uint64` | **F** | **Keep visible.** Low volume, and signed/unsigned width choice is a defect class that has bitten this converter before | Real: a genuine width mistake looks exactly like this |
| `CS1718` | 10 | 3 | `return f != f;` — Go's NaN idiom, verbatim (`runtime/float.cs:17`) | **S** | **Suppress in template** | The idiom is deliberate and the warning can never be right in converted float code |
| `IL2067` | 9 | 1 | trim analyzer, golib only | **G** | golib-local (§7) | — |
| `CS8826` | 7 | 1 | partial-method signature differences — the hand-owned `runtime/debug/stubs_impl.cs` names parameters `_` where the converted declaration names them `fd`/`in`/`Ꮡp` | **F** | **Keep visible.** Fix by renaming the parameters in the hand-owned file | Real: a genuine signature drift between a hand-own and its converted declaration is exactly what this catches |
| `IL2075` | 5 | 2 | trim analyzer (2 golib, 3 `reflect`) | **P**/G | as `IL2091` | — |
| `CS8618` | 5 | 1 | golib fields (`slice.m_array`, `ж.m_val`) | **G** | golib-local — add `CS8618` to golib's own `NoWarn`, or `= null!` the fields | — |
| `CS0252` | 4 | 1 | `if (key == ᏑcancelCtxKey)` in `context/context.cs` — `any == ж<nint>` binds **reference** comparison, bypassing `ж<T>`'s `==` | **F** | **Keep visible** — board row (§5.3) | Real, and pointed at a correctness assumption (that `Ꮡ<global>` yields a stable box) that is currently unstated |
| `CS0649` | 3 | 3 | `traceviewer.staticContent` is a `//go:embed embed.FS` the converter cannot populate; `exithook.running` is dead in Go too; `windows._ᴛ1ʗ` | **F** | **Keep visible** — one of the three marks the unimplemented `//go:embed` | Real: "field never assigned" is how a dropped initializer would present |
| `CS8860` | 3 | 1 | `net/http/fcgi` has a Go type named `record` | **S** | **Suppress in template** (same family as `CS8981`) | None — Go type names are not negotiable |
| `IL2060` `IL2090` `IL2072` `IL2059` | 1 each | 1 | trim analyzer, all in golib | **G** | golib-local (§7) | — |
| `CS8620` | 1 | 1 | `slice<error>` vs `slice<error?>` in `fmt/errors.cs` | **P** | `<Nullable>annotations</Nullable>` | See §6 |
| `CS1522` | 1 | 1 | empty `switch` block (`net/http/httptest/server.cs:144`) | **F** | **Keep visible** — an empty switch is a converter artifact worth a look | Real, and it is a single site |

## 5. The three converter roots this census found

These are the *product* of the audit, separate from the suppression question. Each is a board row.

### 5.1 Dead named-return locals — `CS0219`, 1,219 sites, 136 packages

The converter emits every named result as a local at function entry:

```csharp
public static (rune r, nint size) DecodeRune(slice<byte> p) {
    rune r = default!;          // never read — the body returns explicit tuples
    nint size = default!;
```

`1,218 of 1,219` `CS0219` sites are that declaration. The names already survive in the C# return
type (`(rune r, nint size)`), so nothing is lost by omitting a declaration the body never touches —
and the emission gets *closer* to the "readable Go-like C#" goal, not further. The check is a body
walk: emit the local only if the named result is read, assigned, address-taken (`Ꮡ(err)`), or
captured by a defer closure. Doing this **preserves** `CS0219` as a live signal for a genuinely
dropped assignment, which suppression would destroy.

### 5.2 `nint`-typed constants that should be `int64` — `CS8778`, 620 sites

Two distinct roots wearing one warning code:

1. **Composite-literal element type ignored (607 sites, all `math/rand/rng.cs`).** Go's
   `rngCooked [607]int64 = [...]int64{-4181792142133755926, …}` becomes
   `array<int64> rngCooked = new int64[]{-(nint)4181792142133755926L, …}` — the untyped constants
   took `int`→`nint` instead of the literal's `int64` element type. On a 32-bit target these
   truncate and `math/rand` silently produces different numbers. The same defect appears sporadically
   elsewhere: `math/rand/v2/regress_test.cs:41` has `(nint)1000000000000000000L` sitting in a
   `new int64[]{…}` beside twelve correctly-typed siblings.
2. **Folded constants not wrapped in `unchecked` (13 sites).** `bufio/scan.cs` shows both halves of
   this in three lines: the const *declaration* is emitted correctly as
   `unchecked((nint)9223372036854775807)`, but the folded *use* of `maxInt/2` two lines later is
   emitted bare as `(nint)(4611686018427387903L)`. (The declaration is then dead — it is the single
   non-named-return `CS0219` in the whole corpus.)

### 5.3 Interface-vs-pointer comparison binds reference equality — `CS0252`, 4 sites in `context`

```csharp
internal static any Value(this ж<cancelCtx> Ꮡc, any key) {
    if (key == ᏑcancelCtxKey) {          // any == ж<nint>  ->  object reference comparison
```

Go's `key == &cancelCtxKey` compares an interface to a pointer by dynamic type + pointer value. The
emitted form compares two *object references*, bypassing `ж<T>`'s own `==`. It works today only
because `Ꮡ<global>` yields a stable singleton box — an invariant nothing states or guards.
`context` validates at 36/38, so this is not a live failure; it is an unstated dependency that a
future boxing change would break silently.

## 6. The two property changes — why `NoWarn` is the wrong instrument twice

### 6.1 `<Nullable>enable</Nullable>` → `<Nullable>annotations</Nullable>`

The nullable family is **1,142 warnings across nine codes** (`CS8604` 495, `CS8602` 243, `CS8619` 226,
`CS8603` 69, `CS8601` 40, `CS8600` 35, `CS8625` 14, `CS8714` 19, `CS8620` 1) — plus the 4,421 `CS8618`
already suppressed. Listing nine codes in `NoWarn` is the wrong shape for one decision.

The decision is: **Go's type system has no non-nullable pointer, interface, map, slice, channel or
func.** Every one of them is nil-able by construction, so C#'s flow analysis can only be satisfied by
annotating the entire emitted corpus `?` — which would bury the Go shape the project exists to
preserve. And the analysis is not protecting a semantic go2cs wants: a converted program that
dereferences a nil Go value *should* fault, because that is Go's nil-pointer panic.

`annotations` keeps `?` meaningful (golib's `ж<T>?`, `PanicException?`) and keeps `default!` legal,
while turning the warnings off. `disable` would be wrong — it makes every `?` in the emitted code a
fresh `CS8632`.

**Measured, and the reason `CS8618` must stay in `NoWarn`:** run D applied
`Nullable=annotations` and *removed* `CS8618` from the list. `CS8618` came back at **4,278** — and
**all 4,278 are inside `go2cs-gen` output**, because the generator emits `#nullable enable` in each
`.g.cs` (`Templates/TemplateBase.cs:83`) and a file-level directive beats the project property.
`NoWarn` does not lose that fight, which is why the entry earns its place.

That same mechanism is a *feature* for everything else: under `annotations`, the residual nullable
warnings are **30, every one of them in generated `.g.cs`** (`CS8619` 19, `CS8604` 8, `CS8714` 2,
`CS8603` 1). The corpus goes quiet and the generator stays checked — a real, small, gen-local to-do
list instead of 1,142 lines of noise.

### 6.2 `<PublishTrimmed>True</PublishTrimmed>` on library projects

Every `IL2###` warning in the corpus — **127 across ten codes** — is caused by this one line in the
template. The SDK turns `PublishTrimmed` into `EnableTrimAnalyzer=true` **at build time**, so the trim
analyzer runs on ordinary `dotnet build`. Verified directly: `archive.zip` rebuilds with 24 `IL`
warnings as committed and **0** with `-p:PublishTrimmed=false`; the full run D emitted zero `IL####`
corpus-wide.

On a `Library` project `PublishTrimmed` does nothing else — trimming is an *application* publish
operation. The same is true of `PublishReadyToRun`, `IncludeNativeLibrariesForSelfExtract` and
`EnableCompressionInSingleFile`, which sit in the same `PropertyGroup`. The honest fix is to condition
that group on `'$(OutputType)' != 'Library'`, so a converted `main` package (which *is* published, and
where the Performance suite really does Native-AOT it) keeps the analysis, and 302 library packages
stop paying for it.

Nothing is permanently lost: the trimmer re-runs over the whole closure at app publish, where the
warning is actionable.

## 7. golib and `go2cs-gen` — separate owners, separate answers

- **golib's 26 `IL####` warnings should not be silenced.** golib is the reflection core (`GoReflect`,
  `AdapterBinder`, `PointerExtensions`, `builtin.ZeroFacts<T>`) and it is exactly the assembly a
  trimmed or AOT-published converted app will break on. The right work is `DynamicallyAccessedMembers`
  annotations and targeted `UnconditionalSuppressMessage` with a justification — not a `NoWarn`.
- **golib's `CS8500` suppression is justified** and should stay: golib is where the deliberate
  unsafe machinery lives. The corpus must *not* inherit it.
- **golib's `NoWarn` needs a small clean-up**: drop `1701;1702` (net9.0 cannot emit them), spell
  `660;661` as `CS0660;CS0661`, and add `CS8618` (5 sites) or initialize the two fields `= null!`.
- **`go2cs-gen` has a 30-warning nullable to-do list of its own** (visible only once the corpus goes
  quiet, §6.1) plus **10 `CS8500`** from `TypeGenerator`'s `fixed (void* ptr = &value.Value)` over a
  managed type. If the generator stopped emitting a blanket `#nullable enable`, `CS8618` could
  eventually leave the corpus `NoWarn` list entirely.

## 8. Projected totals

| Stage | Warnings | Δ |
|---|---:|---|
| Today (Debug or Release, clean build) | **4,147** | — |
| \+ `NoWarn` additions: `CS0162`, `CS0164`, `CS1717`, `CS8974`, `CS1718`, `CS8860` | 3,154 | −993 |
| \+ `<Nullable>annotations</Nullable>` | 2,042 | −1,112 |
| \+ publish properties conditioned to non-`Library` (golib keeps trim analysis) | **≈1,941** | −101 |
| \+ converter fix §5.1 (dead named-return locals) | ≈722 | −1,219 |
| \+ converter fix §5.2 (`nint` constants) | **≈102** | −620 |

The residual ~100 is the honest signal: golib's 26 trim warnings and its own 6 nullable ones, the 30
gen-local nullable items, 15 `CS8500`, 11 `CS0675`, 7 `CS8826`, 4 `CS0252`, 3 `CS0649`, 1 `CS1522` —
every one of them a real item with an owner, and none of them noise.

For reference, the exact configuration of run D (`Nullable=annotations`, `PublishTrimmed=false`,
proposed `NoWarn` **including** `CS8618`) measured **1,903**; the ≈1,941 above adds back `CS0675`
(kept visible by recommendation) and golib's own warnings, which the global `-p:NoWarn` in run D
overrode.

## 9. Footprint

The template edit regenerates `<NoWarn>` and the `Nullable`/publish properties in **all 304
`.csproj`** at the next `-stdlib` reconvert — a one-family corpus diff, mechanically verifiable
(every changed line is inside those two `PropertyGroup`s). Hand-owned project files need the same
edit by hand (see the §2 correction: five under `core/`, plus `golib` with its own list). Both
templates change together — `csproj-template.xml` and `test-csproj-template.xml` carry the same
`NoWarn` line and the same `Nullable` property, and `csprojTemplate_test.go` gates that both still
render well-formed XML.

> The audit under-counted the footprint in one more way: `csproj-template.xml` is rendered for
> **every** conversion, not just `-stdlib`, so the same delta lands on the 574 behavioral-test and
> 13 performance-benchmark `.csproj` the moment any transpile gate runs. That is not optional churn
> to be restored — those project files *are* converter output, and leaving them behind would make
> every future `check-no-regression.ps1` run dirty the tree. They land with the template.

## 10. What landed — r46b-warnsuppress, 2026-08-08

The configuration half only. Everything §4 marks **F** stays visible, and both converter roots (§5.1,
§5.2) remain open.

**The edits.** `csproj-template.xml` and `test-csproj-template.xml`: `<Nullable>enable</Nullable>` →
`<Nullable>annotations</Nullable>`; the six new `NoWarn` entries merged into one numerically-sorted
list, `CS8618` **retained** for the reason in §6.1; and the publish `PropertyGroup` conditioned
`'$(OutputType)'!='Library'`.

⚠ **`AllowUnsafeBlocks` had to come out of that group first.** §6.2 named the group by its four
publish properties, but the fifth element in it is `<AllowUnsafeBlocks>%s</AllowUnsafeBlocks>` — a
*compile* setting the converted stdlib's library packages cannot build without. Conditioning the group
as written would have taken `AllowUnsafeBlocks` off every library project in the corpus. It now lives
in its own unconditional `PropertyGroup` immediately below, and
`TestPublishPropertiesAreScopedOffLibrariesButAllowUnsafeBlocksIsNot` pins both halves.

**Measured, `src/go2cs-stdlib.slnx`, 304 projects, `-t:Rebuild`, isolated
(`MSBUILDDISABLENODEREUSE=1`, `-p:UseSharedCompilation=false`):**

| Run | Configuration | Warnings | Errors |
|---|---|---:|---:|
| before | `-c Debug` | **4,147** | 0 |
| before | `-c Release` | **4,147** | 0 |
| after | `-c Debug` | **1,945** | 0 |
| after | `-c Release` | **1,945** | 0 |

The before runs reproduce the audit's baseline **code for code**, so the two measurements are
comparable line by line. Debug and Release stay identical on both sides. **−2,202, or −53.1 %.**
Against §8's projection of ≈1,941 the measured 1,945 is +4, and the four are named: run D's global
`-p:NoWarn` also overrode `go2cs-gen`'s own generated files, so the gen-local nullable list is 34
here rather than 30 (`CS8619` 19, `CS8604` 9, `CS8603` 2, `CS8714` 2, `CS8625` 1, `CS8600` 1).

The residual, in full: `CS0219` 1,219 and `CS8778` 620 (the two converter roots — 94.5 % of what is
left, and both are *supposed* to be there until §5.1 and §5.2 land), 34 gen-local nullable, `CS8500`
15, `CS0675` 11, `CS8826` 7, `CS8618` 5 (golib), `CS0252` 4, `CS0649` 3, `CS1522` 1, and 26 `IL####`.

**Every `IL####` warning left in the corpus is golib's** — 127 → 26, and all 26 are attributed to
`golib.csproj`, which keeps its trim analysis on purpose (§7). That is the §6.2 hypothesis confirmed
end to end: nothing but `PublishTrimmed` on a library was producing the other 101.

**The nullable family went to zero in converter output.** 1,142 across nine codes → 34, every one of
them inside a `go2cs-gen`-emitted `.g.cs` (the `#nullable enable` those files carry beats the project
property). §7's "small, real, gen-local to-do list" is now visible instead of buried.

**golib's own list** was cleaned as §7 asked: `660;661;1701;1702;IDE1006;CA2255;CS8500;CS8981` →
`CS0660;CS0661;CS8500;CS8981;IDE1006;CA2255`. `CS8618` was **not** added — the five sites
(`slice.m_array`, `ж.m_val`, one in `GoReflect.ValueMarshalling`) stay visible as the to-do §7 wants;
the durable fix is `= null!` on the fields, not a suppression.

**Corpus footprint:** 303 `.csproj` under `src/core` (297 regenerated by a seeded reconvert + 6
hand-edited), 574 behavioral and 13 performance `.csproj` regenerated by their transpile gates, and
110 `.tests.csproj` regenerated by the validated sweep. One family: every changed line is inside the
two `PropertyGroup`s, at a uniform +18/−4 per generated project (`unsafe` +15/−4, `testing` +5/−2,
`golib` +6/−1, reflecting their divergent shapes). The seeded reconvert's 12,520 emitted `.cs`/
`.csproj`/`README.md` were byte-compared against the committed tree first: **zero** `.cs` or
`README.md` content differences (the 50 that differ raw are pure CRLF phantoms — CR-stripped equality
is exact), so the `.csproj` delta is provably the whole change.
