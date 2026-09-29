# CENSUS — golib's Int32 narrowings of Go lengths, capacities and indices (read-only, 2026-09-29)

> **Record type:** CENSUS (point-in-time, read-only). Amend with dated blocks; never rewrite; never execute from.
> **Lane:** i9. **Asked by:** COORD (2026-09-29), moved from C2's queue because it overlaps R1-A commit 2.
> **Read at:** `claude/c2-a8-r1a` `7eeb468c76` (TRAIN I run 2 + R1-A commit 1 + g-inline-locations + A8), the tree
> nearest TRAIN J's golib. Scope: every `.cs` under `src/core/golib`, plus `src/core/unsafe/unsafe.cs` (where P1's
> known positive lives).
> **Method:** every `(int)` cast (checked, unchecked or plain) and every `CreateTruncating`: 126 sites. Each site was
> read in its member with its guards, and its Go-side shape was traced through the callers. Every site was assigned to
> exactly ONE class by a script that refuses an unassigned or doubly assigned site; the counts below are computed from
> those sets. Three further findings are NOT casts (int arithmetic that overflows, a bound checked before any cast);
> they are listed with their class and are not in the 126. Reachability past Int32 is READ from the constructors
> named below; it is not measured by a run, which is why the table says "reachable" rather than "observed".

## 0. The answer

| Class | Cast sites (of 126) | Non-cast findings | What it means |
|---|---:|---:|---|
| SILENT-WRAP | 14 (6 roots + 8 `copy` inheritors) | — | a Go value past Int32 is truncated and the program continues with a wrong length |
| CLR-ESCAPE | 3 | 2 | past `Array.MaxLength` the CLR throws (`OverflowException` / `OutOfMemoryException` / `ArgumentOutOfRangeException`), which `recover()` does not see: not silent, but not Go's panic either |
| REFUSE-BY-NAME | 2 | 1 | checked before the cast, with a Go-shaped recoverable panic |
| PROVABLY-IN-RANGE | 49 | — | a managed `T[]`, an `@string` (`int` length) or a span bounds the value, or the value is checked against such a length first |
| OUT OF SCOPE | 58 | — | not a Go length, capacity or index: shift counts already range-guarded, enum casts, `typeof(int)` tests, register reads, Go's own truncating integer conversions, ring clamps, an exit code |

**The dominant silent-wrap family is native-backed slices.**
- `slice<T>.OverNativeMemory`, the single creation door for a slice over native memory, checks nil, a negative
  length, and cap < len, but has **no upper bound**.
- Two live producers pass full-width Go lengths through it:
  - the header-slice rebase (`ж.HeaderSliceBox.cs:133`: a Go slice header whose data, len and cap are written
    through `unsafe`);
  - `NativeArrayBox` (`ж.NativeArrayBox.cs:97`: `(*[N]T)(p)` over native memory, with N as large as Go allows).
- Every native-arm consumer then narrows `m_length` with `(int)`:
  - `ToSpan`, and through it `copy`, `append`, the string conversions and `AliasOf`;
  - `AppendZeroed`.
- **ONE refusal at the door would close the whole family**: `OverNativeMemory` (and the native `Window`) refusing a
  length or capacity above `Array.MaxLength` by name, as `MakeChanSizeBeyondManagedBuffer` does for channels.
  - A `Span<T>` cannot be longer than Int32 at all, so widening the consumers is not an option.

**R1-A commit 2 covers NONE of the golib sites below.** Commit 2 removes the CONVERTER's `(int)` inside emitted C#
Ranges, a separate family (census R1 in the slice-bounds sizing), and the golib API it calls (`.slice(lo, hi)`,
R1-A commit 1) is already nint end to end. But commit 2 WIDENS the native family's reach:
- today the emitted `(int)` truncates a bound before a native window is cut;
- after commit 2, `(*[1<<40]byte)(p)[:n]` with n ≥ 2^31 reaches `OverNativeMemory` at full width, and then `ToSpan`.
So the door refusal should land with commit 2, or before it.

## 1. SILENT-WRAP

| # | Site | Go-side shape that reaches it | Note |
|---|---|---|---|
| S1 | `unsafe/unsafe.cs:925` `unsafe.Slice`: `int.CreateTruncating(len)` | `unsafe.Slice(p, 1<<32+5)` gives a 5-element slice | **P1's known positive; P1 is cutting it. Not cut here.** |
| S2 | `unsafe/unsafe.cs:1036` `unsafe.String`: `int.CreateTruncating(len)` | `unsafe.String(p, 1<<32+5)` | P1's, same cut |
| S3 | `unsafe/unsafe.cs:881` `unsafe.Add` over `ж<T>`: `int.CreateTruncating(len)` | `unsafe.Add(p, 1<<32+8)` on a native pointer adds 8 | **NEW**, not in P1's report. The `Pointer` overload at `:875` goes through `nint` and wraps exactly as Go's `uintptr` arithmetic does, so it is faithful |
| S4 | `golib/slice.cs:814` `ToSpan`, native arm: `new Span<T>(ptr, (int)m_length)` | a native window of 2^31 elements or more (the producers above) | a length in [2^31, 2^32) throws (negative span length: CLR-ESCAPE); 2^32 + k wraps to k, **silently**. Every `ToSpan` consumer inherits it: the native arm of `string.cs:188` (`AliasOf`), the string conversions, and the 8 `copy` sites `builtin.cs:954, 977, 990, 1029, 1044, 1142, 1168, 1233`. Their own `[..(int)min]` over `min = Min(dst.Length, src.Length)` narrows past Int32 only when BOTH sides are native windows that long; one managed side bounds `min` |
| S4b | `golib/slice.cs:1543` `AppendZeroed`, native arm: `new Span<T>(ptr, (int)count)` | appending a zeroed tail of 2^31 elements or more in place, within a native capacity | same family, same door |
| S5 | `golib/GoStructSynthesis.cs:687` `toIntDims` | `reflect.StructOf` with a map field whose key is an array of 2^31 elements or more (the map-key dims attribute) | remote; `reflect.ArrayOf` itself keeps dims as `nint` |

## 2. CLR-ESCAPE (past `Array.MaxLength`; the CLR's exception, not Go's panic)

| # | Site | Go-side shape | Note |
|---|---|---|---|
| E1 (non-cast) | `golib/slice.cs:1426-1427` / `:1554-1555`: append growth, `CalculateNewCapacity` (nint) into `AllocationCounter.NewArray` | an `append` that grows a managed slice past `Array.MaxLength` | `make` already refuses the same bound by name (R1); append does not |
| E2 (non-cast) | `golib/string.cs:793` `@string operator +`: `sa.Length + sb.Length` (int + int, unchecked) | concatenating strings whose total length exceeds Int32 | two ints can only wrap NEGATIVE, so `new byte[negative]` throws; never a silent short string |
| E3 (3 casts) | `golib/array.cs:75, 81, 96` `array(nint/ulong length)`: `NewArray` before `m_length = (int)length` | a `[N]T` with N past `Array.MaxLength` | `NewArray` throws first, so the cast itself is never reached with a wrapped value |

## 3. REFUSE-BY-NAME

| # | Site | Go-side shape |
|---|---|---|
| R1 (non-cast) | `golib/slice.cs:461-465` (`make([]T, len, cap)`) | `makeslice: len/cap out of range`, Go's text, above `Array.MaxLength`. A zero-size T is exempt, as in Go |
| R2 (cast) | `golib/channel.cs:399` `ChanCore`'s `(int)size`, guarded by `checkMakeChanSize` (`channel.cs:1250`) | `make(chan T, n)`, n past `Array.MaxLength`: `MakeChanSizeBeyondManagedBuffer` |
| R3 (cast) | `golib/slice.cs:830` `ZeroSizeSpan` | a zero-size slice longer than `Array.MaxLength` spanned: a named panic |

## 4. PROVABLY-IN-RANGE (49 cast sites)

- **Bounded by a managed `T[]` (at most `Array.MaxLength`), or checked against such a length first:**
  - `array.cs`: 123-124 (after `checkArrayConversionLength`), 168, 213, 299, 315, 330 (`Slice2`, after its check),
    908, 914;
  - `slice.cs`: 771 and 818 (managed arms), 948 (hash mixing), 1186, 1199, 1205 (the .NET collection interfaces;
    a native window past Int32 would only mis-report `Count` to .NET callers, never to Go), 1417, 1549 (managed arms),
    1431 (append's copy into the NEW managed array, reached only after `NewArray` accepted the grown capacity);
  - `builtin.cs`: 2443 and 2559 (the `Ꮡ` overloads' managed arms, after `CheckElementIndex`), 2599, 2613, 2629, 2641;
  - `ж.cs:420, 439` (`at`, after its index check) and `ж.ElemRefBox.cs:155, 170, 297, 298, 379` (an index the box's
    constructors took as `int` or checked against a managed length; a native element takes a `NativeBox`, never an
    `ElemRefBox`).
- **`string.cs:188`** (`AliasOf`'s MANAGED arm; its native arm is S4's inheritor).
- **`@string` (its length field is `int`), checked first:** `string.cs:249, 263, 293, 337`; `slice.cs:1745`.
- **Spans (int-bounded), checked first:** `sstring.cs:109, 120, 141`; `sslice.cs:87, 140`.
- **Unchecked C# `Slice(nint start, nint length)` APIs:** `array.cs:344`, `string.cs:311`, `sstring.cs:153`,
  `PinnedBuffer.cs:148`. No converter emission reaches them (the converter emits no `.Slice(`); their callers are
  hand-owned and pass in-range values.
- **Other:**
  - `map.cs:239`: a hint above Int32 clamps to 0, which is a hint, not a length;
  - `PinnedBuffer.cs:35`: its one external caller (`ж.cs:667`) passes a managed array's own length;
  - `GoReflect.FieldAccess.cs:665`: reflect's converted `Index` checks the index first.

## 5. Recommendation (no cut; for routing)

- **One door, before or with R1-A commit 2.** `OverNativeMemory` and the native `Window` refuse a length or
  capacity above `Array.MaxLength` by name, as a platform bound of go2cs. This closes S4, S4b and every inherited
  consumer in one place. A span cannot be widened past Int32, so the alternative (widening the consumers) does not
  exist.
- **S3 `unsafe.Add`:** offset in `nint` (the `Pointer` overload's form) for native pointers, and for the managed
  element step, refuse past the backing's length by name. This can join P1's S1/S2 cut or follow it.
- **E1:** `append` refuses past `Array.MaxLength` with a named panic, as `make` does. E2 and E3 are platform bounds
  that could take the same named form. None is silent today.
- **S5:** name it and leave it (remote).
