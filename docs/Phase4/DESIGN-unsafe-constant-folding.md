# DESIGN — fold `unsafe.Sizeof` / `Alignof` / `Offsetof` at EXPRESSION sites

> Status: **OPEN — awaiting user ruling.** Surfaced 2026-07-31 by the crypto/md5 arc (the
> `Alignof`/`Offsetof` operand-resolution fix, commit `816563b6e`); analysis is that arc's,
> re-recorded here for review. Related landed work: the operand is now resolved through
> `go/types` (`docs/ConversionStrategies-Reference.md`, *"`unsafe.Alignof` / `unsafe.Offsetof`
> name a TYPE, resolved through `go/types`"*).

## Current state — two behaviors for one Go construct

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
