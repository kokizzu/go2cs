# FINDING — a `*[N]T` view over a MANAGED box reads the pointee's bytes as an array header

**Filed 2026-09-08 (lane G), ruled DEFECT E by COORD in `5caa4fe76`: a golib + converter DESIGN
increment, not a seat.** Measured by i9 in `36b422bb6` while minting a golden on `19bb74012`.

This record exists so the next reader starts from a measurement rather than from the symptom, and so
the guard row that was RETIRED to reach it is not silently read as coverage that still exists.

## 1. The reading

Go's `key8` shape — the address of an element of a pointer-to-array conversion, from 1.24
`runtime/lock_spinbit.go`:

```go
func key8(p *uintptr) *uint8 { return &(*[8]uint8)(unsafe.Pointer(p))[0] }
```

emits, correctly (defect A's parenthesisation is right here and the accessor binds to the cast's
RESULT):

```csharp
return ((ж<array<uint8>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(Ꮡp))).at<uint8>(0);
```

**It compiles and then throws.** `IndexOutOfRangeException` at `go.z.at` (`golib/z.cs:416`), from
`main.cs:64`, C# exit 2 against Go exit 0, **deterministic at 10 of 10 runs**.

## 2. The site, as far as it is MEASURED

`at` bounds-checks through `arrayView`. Its FIRST branch is the one designed for exactly this shape
(`z.cs:343`) and calls `TryGetNativeArrayView` — a virtual returning `null` in the base (`z.cs:199`)
with **exactly one override, in the native-array-box file**. The box here is a **managed heap box**,
so that branch declines, and the fallback reads `Value` as an array interface — i.e. **the pointee's
BYTES AS AN ARRAY HEADER**, which the code's own comment at `z.cs:340–342` warns about.

Also on the record, un-rooted: the `uintptr`→pointer operator resolves a **managed pointer token**
before it ever mints a native box (`z.cs:694`).

⚠ **Neither i9 nor I name the root**, and that is deliberate. The choice between *the box kind never
becoming a native array box* and *the array length never being established by the conversion* is a
model question, and this file records the site and the determinism rather than prescribing a cut.

## 3. Re-census — the CONTAINER, and why it is not the population

Over the corpus pin's own sources (`go1.23.12`), pointer-to-array conversions, production files only:

```
total sites                                       218
  under cmd/          (NOT converted)              16 files
  under vendor/       (GOROOT-vendored)            13 files
  in CONVERTED-corpus packages                     54 files
```

The 54 span `crypto/internal/{edwards25519,mlkem768,nistec}`, `crypto/sha256`, `crypto/sha512`,
`image/png`, `internal/abi`, `internal/poll`, `internal/reflectlite`, `internal/syscall/windows`
(+`/registry`), `net`, `net/http`, `os/user`.

**That is the container, not the reaching population**, and the operands split at least three ways:

| operand shape | example | box kind | measured |
|---|---|---|---|
| a SLICE | `(*[Size224]byte)(sum[:])`, `(*[4]byte)(dst)` | slice-backed | not measured |
| a struct FIELD address | `(*[2]byte)(unsafe.Pointer(&sa.Port))` | field-reference box | not measured |
| a scalar VARIABLE address | `(*[8]uint8)(unsafe.Pointer(p))`, `p *uintptr` | managed scalar box | **THROWS** |

⚠ **Only the third row is measured.** The second appears in `net`, which is a BANKED row that
PASSES — so either those paths are not reached by that suite or a field box takes a different
branch, and **I have not measured which.** Do not read the 54 as a count of latent throws.

⚠ **No compile gate can see any of this.** The corpus compiles 307/307 with every one of these
sites present; defect E is a RUNTIME property. A text census bounds the container and a
`go/types`-plus-runtime instrument is what would bound the population.

## 4. What was retired to reach it, and the debt

`src/tests/Behavioral/SwitchPointerSentinelCase` carries `key8`/`key8Last` as **compile-shape rows
that are DELIBERATELY NEVER CALLED**. They were written to assert a VALUE — reading a known word's
bytes distinguishes "indexed the array" from "indexed something else" — and calling them reaches
this defect, so the stronger row was **retired rather than weakened**.

**The debt, stated so nobody reads the project as covering more than it does:** defect A's
parenthesisation is still guarded there (the declarations do not compile under the unparenthesised
form), but **nothing asserts that the accessor addresses the RIGHT ELEMENT.** That row returns when
this increment lands and the two functions can be called again.

## 5. How it was found — the unmasking chain

Each fix bought the next measurement, and the count rising is the corpus getting further rather than
worse:

```
root 2 (pointer case LABEL)   let the project reach Compile        -> unmasked defect C
root 1 A+C (tag + cast)       let it compile past CS0019/CS0029    -> unmasked defect D
defect D (native-width UL)    let it reach Output                  -> unmasked defect E (here)
```

⚠ One instrument note i9 disclosed rather than published: mid-investigation they ran the binary under
`bin/Release/Go`, saw `8 1` at exit 0 twenty times, and were one step from reporting a flavour
divergence. **That path is the GO oracle** (`BehavioralRunner/Program.cs:1049`); the C# program is
under `bin/Release/net10.0`. There is no flavour divergence — *"the 20 of 20 pass was the most
convincing wrong number I have produced today."*
