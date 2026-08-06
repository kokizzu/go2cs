# DESIGN — fold `unsafe.Sizeof` / `Alignof` / `Offsetof` at EXPRESSION sites

> Status: **IMPLEMENTED 2026-07-31** — accepted the same day by user ruling ("fold, per the
> recommendation") and landed as its own gated arc; see the *Outcome ledger* below.
> Surfaced 2026-07-31 by the crypto/md5 arc (the
> `Alignof`/`Offsetof` operand-resolution fix, commit `816563b6e`); analysis is that arc's,
> re-recorded here for review. Related landed work: the operand is now resolved through
> `go/types` (`docs/ConversionStrategies-Reference.md`, *"`unsafe.Alignof` / `unsafe.Offsetof`
> name a TYPE, resolved through `go/types`"*).

## Outcome ledger — what the fold actually measured

Seeded two-temp-root A/B reconvert of the full stdlib (master exe vs. folding exe), Go 1.23.1,
`windows/amd64`:

| Measure | Value |
|---|---|
| Expression sites folded | **262**, on 249 emitted lines, across **61 files / 16 packages** |
| Per package | `runtime` 129 · `debug/elf` 70 · `syscall` 17 · `internal/poll` 13 · `internal/syscall/windows` 8 · `internal/abi` 4 · `sync` 3 · `runtime/pprof` 3 · `net` 3 · `internal/coverage/decodecounter` 3 · `crypto/x509` 3 · `internal/fuzz` 2 · `reflect` 1 · `os` 1 · `internal/coverage/encodemeta` 1 · `internal/coverage/cfile` 1 |
| Diff classification | every changed line is exactly this class — 0 added lines that are not a fold, 0 removed lines that are not an `@unsafe.Sizeof/Alignof/Offsetof` run-time call, 0 files added or removed |
| Declaration sites | **byte-identical** — none of the 22 pre-existing folds was touched |
| Sites that resisted folding | **4**, legitimately (below) |

The proposal estimated "~263 expression sites across ~30 files"; the measurement is **262 sites
across 61 files**.

### What the PRE-fold emission actually answered

Probed on the built corpus against the real converted `debug/elf` types, calling exactly what the
old emission called (`unsafe_package.Sizeof<T>` / `Offsetof(Type, string)`):

| Call | Go | pre-fold emission |
|---|---|---|
| `unsafe.Sizeof(hdr)` — `Header32` | 52 | **56** |
| `unsafe.Sizeof(hdr)` — `Header64` | 64 | 64 |
| `unsafe.Offsetof(hdr.Type)` / `.Shstrndx` / `.Entry` | 16 / 50 / 24 | 16 / 50 / 24 |
| `unsafe.Sizeof(uint32(0))`, `unsafe.Alignof(uint32(0))` | 4, 4 | 4, 4 |
| `unsafe.Sizeof(elf.ImportedSymbol{})` — 3 strings | 48 | **24** |
| `unsafe.Sizeof(elf.Symbol{})` — strings + numerics | 72 | **56** |
| `unsafe.Sizeof(elf.Section{})` — embedded + iface + ptr | 104 | **throws `ArgumentException`** |
| `unsafe.Sizeof(elf.Prog{})` — embedded + iface + ptr | 96 | **throws `ArgumentException`** |
| `unsafe.Offsetof(s.SectionHeader)` — embedded | 0 | **throws `ArgumentException`** |

So the failure was **two** classes, not one: the throw the proposal predicted (non-blittable
operands — `… cannot be marshaled as an unmanaged structure`; an embedded field is emitted as a
*property*, which `Marshal.OffsetOf` cannot see at all), **and a silent wrong number** wherever the
marshalled layout merely differs — `Header32` read a 56-byte buffer for a 52-byte on-disk ELF32
header, and a struct of Go `string`s came back at half its Go size. Blittable scalars and
numeric-field offsets agreed, which is why nothing had ever looked wrong.

### The 4 sites that do NOT fold — variable-size operands DO exist

The proposal assumed variable-size operands "do not exist for these builtins". They do: since Go
1.18 the operand may be **type-parameter-typed**, and the spec then makes the call *non-constant*.
`go/types` correctly hands back no value, so the converter emits the run-time golib form and
reports the site rather than losing it:

- `slices/slices.go` — `unsafe.Sizeof(a[0])`, `a` of `S ~[]E`
- `internal/saferio/io.go` — `unsafe.Sizeof(v)`, `v` of type parameter `E`
- `runtime/minmax.go` — `unsafe.Sizeof(x)` ×2, `x` of type parameter `T`

These four are why golib's `@unsafe` run-time forms are **retained, not dead**. They are also the
only remaining callers, hence the only thing keeping `Marshal.SizeOf`'s throw reachable — a generic
`Sizeof` over a non-blittable `E` still fails. Out of scope for this arc; the fix would be a
`[GoType]`-metadata implementation, and only for these four shapes.

### A correction the fold delivered for free

The pre-fold `Offsetof` reshape measured a **promoted** field against the struct that *declares* it.
Go measures it against the **operand** struct: `unsafe.Offsetof(e.count)`, where `count` sits at 8
inside an embedded `Padded` that itself sits at 8, is **16** — not 8. `go/types` folds the whole
path; the reflection form could only ever see one hop. Now guarded by `UnsafeOperations`.

### Emission detail settled during implementation

The folded literal carries its type — `/* unsafe.Sizeof(hdr) */ (uintptr)52`, not a bare `52`. Go's
constant is **typed** `uintptr`, and a bare C# number is not: `uadd := unsafe.Sizeof(*t)` would then
infer `int`, which has no implicit conversion back to `nuint` (`internal/abi`'s `FuncType.InSlice`
hands it to a `uintptr` parameter — CS1503). Everywhere else the cast is inert, since C#'s
constant-expression conversion would have bound a bare literal anyway, and a cast binds tighter than
every binary operator, so no site needs added parentheses. No folded site is a shift count (checked
across the corpus) — that is the one context where the cast, rather than the bare form, would bind
wrong.

## Original analysis — two behaviors for one Go construct

Go defines all three as **compile-time constants** computed from the operand's static type; the
operand is never evaluated.

- **Declaration sites already fold.** Where one of these initializes a package-level constant,
  `go/types` has the folded value and the converter emits it, keeping the Go expression as a
  comment: `internal static uintptr offsetX86HasAVX => /* unsafe.Offsetof(cpu.X86.HasAVX) */ 66;`
- **Expression sites emit a runtime call** into golib `@unsafe` — now correctly *shaped* (the
  `typeof(T)` forms), but still a **reflection call at run time** where Go has a constant.

## Why this is more than cosmetics

1. **`Sizeof` at expression sites is likely broken at runtime today.** golib's implementation
   rides `Marshal.SizeOf<T>`, which **throws** for non-blittable `T` — and a converted Go struct
   holding a `slice<T>`, `@string`, interface, or `ж<T>` field is non-blittable. The ~263
   expression sites across `runtime`, `reflect`, `syscall`, `debug/elf`, `internal/poll`, … have
   never executed under a validated suite; the first package that runs one will fail.
2. **Semantic fidelity.** Go's answer is the size/alignment/offset of the *Go* type under Go's
   layout rules. A reflection answer measures the *C# marshalled* layout — a different number
   whenever golib's field representation differs from Go's (which is most of the time:
   `string` is 16 bytes in Go/amd64; `@string` is a managed struct). `debug/elf`-style code that
   uses these values as **on-disk format offsets** needs Go's numbers, not the CLR's.
3. **Non-evaluation.** Go never evaluates the operand; the emitted call does. A side-effecting
   operand (rare but legal) diverges.
4. **AOT/trim friendliness.** A folded constant needs no reflection metadata; `Marshal.SizeOf`
   is a trim-analysis liability.

## The proposal

Fold **all three** at expression sites exactly as declaration sites already do: `go/types`
supplies `types.Sizes` (the converter already configures Go's `SizesFor("gc", arch)` for
constant folding), the emitted form is the literal with the Go expression preserved as a
comment — the same visual convention the declaration sites use. golib's `@unsafe` runtime forms
remain only for the shapes `go/types` cannot fold (variable-size cases do not exist for these
builtins; the runtime forms become dead-but-retained, or are removed with the dead-code wave).

**Cost / risk:** a ~263-site emission change concentrated in the S1 raw-metal packages;
per-arch correctness (the folded numbers are `GOARCH`-dependent — the converter already pins
`amd64` layout for its other native-size decisions, so this is consistent, but it bakes the
choice into emitted text); goldens re-baseline; and any site whose *current* reflection answer
some code accidentally depends on would change value (no validated package exercises one —
that is also the evidence they're unexercised).

**Alternative (status quo plus):** keep runtime calls but reimplement golib `Sizeof` to compute
Go-layout sizes from `[GoType]` metadata instead of `Marshal.SizeOf`. Fixes the throw and the
fidelity number, keeps evaluation and reflection cost, and duplicates Go's layout algorithm in
C# — strictly more machinery for a strictly worse match to Go semantics. Not recommended.

## Recommendation

Fold. It is Go's exact semantics, removes a latent runtime-throw class, and is the
nothing-throwaway path (the alternative builds machinery the fold makes dead). Run it as its
own gated arc (golden re-baselines + full corpus + sweep), sized medium — the mechanism exists
at declaration sites; the work is routing expression sites through it and classifying the
~263-site diff.
