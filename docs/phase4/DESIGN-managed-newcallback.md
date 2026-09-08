# DESIGN — a managed `syscall.NewCallback`

**Status:** design record, written before any body. **Ruled by COORD 2026-09-08**: the linkname push
is DECLINED, the row is bucket-3 **frontier**, and the remedy is a **managed** implementation on the
`syscall` side.

⚠ **The author of this record cannot build or run any of it.** C1 is a Linux container with no C#
toolchain of any kind — no dotnet, no mono, no csc. Every claim below is read at the code or at Go's
sources with a file and line, or is a CLR constraint explicitly flagged as owing confirmation at the
first build. **The i7 compiles, runs the guard and runs the F8 registration check, and posts the
readings; C1 scores them.**

## 1. The census — why the push is declined

Four reasons, each measured at master `f4d2b981b`:

1. **The signatures cannot meet.** The destination is `internal static partial uintptr
   compileCallback(any fn, bool cleanstack);` (`syscall/windows/syscall_windows.cs:223`, bodyless, so
   the `PartialStubGenerator` fills it with a throwing stub). The producer is
   `internal static uintptr compileCallback(eface fn, bool cdecl)`
   (`runtime/windows/syscall_windows.cs:278`) carrying `//go:linkname compileCallback
   syscall.compileCallback`. Go linknames them because `any`'s runtime representation *is* `eface`;
   ours are two different C# types — `object` and a `[GoType] partial struct`.
2. **No descriptor is obtainable.** Bridging the signature would need a real `fn._type`. `efaceOf`
   (`runtime2.cs:141`) is a hand-own that **returns an inert nil eface** — its own comment records
   why: the reinterpret "panicked on first touch, taking the whole runtime_package type initializer
   down with it". `compileCallback`'s first check is `fn._type == nil → panic`, so **even runtime's
   own internal caller at `os_windows.cs:314` panics today.**
3. **The producer's body cannot run here anyway.** It walks the func type into an `abiDesc`
   translating Go's stack ABI to the Windows C ABI, then resolves a code address through
   `callbackasmAddr` into `callbackasm` — and **`callbackasm` and `callbackasm1` are BOTH bodyless
   partials** (`windows/syscall_windows.cs:236`, `windows/os_windows.cs:1143`). The producer's own
   dependencies are assembly we have not implemented.
4. **There is no managed substitute in the tree.** `GetFunctionPointerForDelegate` has **zero**
   occurrences across `golib` and every `*_impl.cs`.

**Classification.** Go HAS an implementation and it is assembly-backed; we have not built the
capability. That is **frontier**, not "a push that did not arrive" — the distinction the bucket-3
census exists to make.

## 2. The reach

**Zero production call sites in the converted corpus** — every `NewCallback` mention outside its
declaring file is a comment (three, repeated per-GOOS, quoting Go's issue #6751).

The consumers are **`runtime`'s own Windows test suite**. Measured at `go1.24.13`
(`runtime/syscall_windows_test.go`) by parsing each call's enclosing function:

- **6 tests call it directly** — `TestBigStackCallbackSyscall`, `TestCallbackInAnotherThread`,
  `TestEnumWindows`, `TestRegisterClass`, `TestReturnAfterStackGrowInCallback`,
  `TestStdcallAndCDeclCallbacks`.
- **1 helper** calls it: `nestedCall(t, f)`, whose first line is `syscall.NewCallback(callback)`.
- **5 more tests reach it through that one hop** — `TestCallback`, `TestCallbackGC`,
  `TestCallbackPanic`, `TestCallbackPanicLocked`, `TestBlockingCallback`.

**Total reach: 11 tests.** The row is **windows-only and test-only**. ⚠ `go1.23.12` is not installed
on the authoring host, so this count is at `go1.24.13` and is **not** extrapolated to the corpus pin.

## 3. The remedy, and where it lives

Complete the **bodyless partial** at `syscall/windows/syscall_windows.cs:223`. That is the
**partial-completion displacement**: no `manualConversionFuncs` entry, no converter change, no
two-seeded diff. A behavioral **COMPILE** is owed, because generated stubs disappear (route #7's
neighbourhood).

**File:** a NEW `src/core/syscall/windows/syscall_windows_callback_impl.cs`, **not** an addition to
the existing `syscall_windows_impl.cs`. The existing companion is 687 lines scoped to the
socket-address seam, and the sibling companions in that directory are **named per seam** —
`zsyscall_windows_addrinfo_impl.cs`, `zsyscall_windows_certchain_impl.cs`,
`zsyscall_windows_dnsrecord_impl.cs`. This follows that convention rather than growing a file whose
header states a different scope.

`syscall` already references `runtime` (`syscall.csproj:178`), so **no reference injection is
needed** — the `TB.Context()` class of blocker does not apply here. Noted because it was a real
candidate blocker until measured.

## 4. The delegate-lifetime rule

`Marshal.GetFunctionPointerForDelegate(d)` yields a pointer valid **only while `d` is alive**. Go's
callbacks are never freed (`cbs` is process-lifetime and its exhaustion is a fatal
`throw("too many callback functions")`, not a reclaim). So the managed side **must root the delegate
for the life of the process**: a static table holding both the delegate and its pointer, which roots
the delegate by holding it as a key.

This is not an optimisation. Without it the GC may collect the delegate while native code still holds
the pointer, and the failure is an access violation at an arbitrary later time — the worst shape of
defect this project can ship.

## 5. The identity rule — the same func value must yield the same pointer

Go caches, and the record must reproduce the semantics rather than approximate them.
`runtime/syscall_windows.go:326`: `if n, ok := cbs.index[key]; ok { return callbackasmAddr(n) }`,
keyed by

    type winCallbackKey struct { fn *funcval; cdecl bool }

built as `winCallbackKey{(*funcval)(fn.data), cdecl}` — i.e. **the func value's pointer**, not its
type.

**The managed analogue** is a table keyed on the delegate, with `Delegate` equality (Method + Target).

⚠ **A DIVERGENCE TO STATE RATHER THAN DISCOVER.** C# `Delegate.Equals` compares *method and target*,
where Go compares *funcval pointers*. For a closure the two agree in practice (distinct display-class
instances are unequal on both sides). For a **static method group with no capture** C# is *more*
aggressive: two separately-created delegates over the same static method compare equal, so we would
return the **same** pointer where Go might mint two. That direction is the safe one — it returns a
pointer that works — but it is a divergence, and a test asserting two distinct pointers for two
distinct `NewCallback` calls on the same static function would see it. None of the 11 tests is known
to assert that; **that is an unverified claim by the author and the i7's run is what settles it.**

## 6. The calling-convention split

`NewCallback` is stdcall, `NewCallbackCDecl` is cdecl — **and on anything but 386 the distinction does
not exist.** `compileCallback`'s first statement is

    if GOARCH != "386" { cdecl = false }   // cdecl is only meaningful on 386

so on amd64/arm64 both entry points produce the **same key and the same pointer** for the same func
value. The corpus is not built for 386. The record says so explicitly rather than leaving a reader to
wonder why the two entry points collapse: on our targets there is **one** Windows ABI, and the managed
side should declare its shim `[UnmanagedFunctionPointer(CallingConvention.Winapi)]` — with
`StdCall`/`Cdecl` distinguished only if a 386 target ever exists, which it does not.

## 7. The argument-marshalling boundary — refuse BY NAME

Go permits only `uintptr`-sized arguments and exactly ONE `uintptr`-sized result, and it **panics**
outside that contract. The body must refuse the same shapes with the same text, so a converted test
asserting the panic string still matches. Go's texts, verbatim from
`runtime/syscall_windows.go` at `go1.24.13`:

| site | text |
|---|---|
| `:104` | `compileCallback: argument size is larger than uintptr` |
| `:114` | `compileCallback: float arguments not supported` |
| `:203` | `compileCallback: type <T> is currently not supported for use in system callbacks` |
| `:273`, `:288`, `:291` | `compileCallback: expected function with one uintptr-sized result` |
| `:297` | `compileCallback: float results not supported` |
| `:335` (fatal, not panic) | `too many callback functions` |

**A refusal is a PANIC, not a plain exception** — the host classifies a non-panic exception as an
infrastructure error, which is unbankable and also untrue (the host is fine).

## 8. ⚠ THE CONSTRAINT THAT DECIDES THE BODY'S SHAPE, and it is the one claim here I could not measure

`Marshal.GetFunctionPointerForDelegate` **does not accept a generic delegate type.** The CLR cannot
marshal a generic delegate, and the converter's emission for a Go func value is exactly that — a
`Func<…>` / `Action<…>`, as every arm of `FinalizerBindingTests` shows (`Action<ж<nint>>`,
`Action<object>`). So the naive body — take the `any`, cast to `Delegate`, hand it to
`GetFunctionPointerForDelegate` — **throws for the shapes this row actually receives.**

**Consequence for the body.** The seam needs a **non-generic** delegate type declared in the seam file
per arity, attributed `[UnmanagedFunctionPointer(...)]`, whose instance forwards to the Go delegate.
The forwarding instance is what gets marshalled and what the table roots.

⚠ **AND THE FORWARD MUST NOT GO THROUGH `Delegate.DynamicInvoke`.** Measured in this same arc:
`DynamicInvoke` binds through the **default binder** — identity, reference, boxing and primitive
widening only — and **never invokes user-defined conversion operators**. A Go callback's parameters
arrive as native `uintptr`-sized words that must become the converted parameter types, and several of
those conversions are exactly the user-defined operators the default binder will not call. A
`DynamicInvoke` forward would therefore fail at run time for a subset of shapes, silently narrower
than the contract §7 states. The forward must be a **typed** invocation.

**THIS SECTION IS THE ONE OWING CONFIRMATION.** The generic-delegate restriction is a documented CLR
constraint the author knows but **could not execute here**. It is stated first, before the body is
written, precisely so the i7's first compile either confirms it or refutes it cheaply — and if it is
refuted, the body gets simpler and this section is amended with a dated block rather than rewritten.

## 9. What is NOT covered

- **Anything but windows.** The declaration is windows-only in Go and in our corpus.
- **386.** Not a target; §6 says what that costs (nothing).
- **Float arguments or results, oversized arguments, non-conforming func types.** Refused by name per
  §7 — the same shapes Go refuses. Not covered means *deliberately refused*, not *unhandled*.
- **Reclaim.** Callbacks are process-lifetime, as in Go. `too many callback functions` stays fatal.
- **The runtime-internal caller.** `os_windows.cs:314` calls runtime's OWN `compileCallback` through
  the nil-returning `efaceOf` and panics today (§1.2). This design does **not** fix that path — it is a
  separate defect on the runtime side, and it is named here so nobody reads this record as closing it.

## 10. The guard

A windows-native behavioral project carrying **`[GoPlatformExclusive("windows")]`** (F8 — ⚠ **commit
the marker before any CNR**, which destroys uncommitted ones), handing `syscall.NewCallback` to
`EnumWindows` / `EnumThreadWindows` and printing **count-independent** lines:

1. the callback **ran at least once**;
2. the **same func value yields the same pointer twice** (§5's identity rule);
3. a **non-conforming func type panics with Go's text** (§7).

Count-independent because the number of top-level windows is a property of the machine, not of the
code — a count would make the golden host-dependent.

The golden is captured **on windows by the i7**. The project must be registered in `go2cs.slnx` and
verified with `check-solution-integrity.ps1`; note that a `windows` marker changes registration **not
at all** (the exemption criterion is platform-exclusive AND *not*-windows-native), so the registration
is ordinary.

## 11. Who measures what

| item | who |
|---|---|
| this record, the body, the guard's source | C1 (cannot build) |
| compile, guard run, golden capture | i7 (windows) |
| `check-solution-integrity.ps1` / F8 registration | i7 |
| scoring the readings against §5 and §8's predictions | C1 |

**Order:** this record → the body → the guard. The body does not start until this record is seated,
because §8 may change its shape and §5 may change its semantics.

---

## 12. ADDENDUM 2026-09-08 — the shim set, enumerated (COORD `eb7992e07`)

COORD's ruling notes that *"the arities are bounded by the contract you transcribed … so the shim set
is finite and enumerable in the record"*. It is, and here it is — **measured at `go1.24.13`, from
declarations rather than from names.**

⚠ **This section is CONDITIONAL on §8 holding.** If the i7's seven arms refute the generic-delegate
restriction, the body needs no shims and this section becomes a record of what was not needed. It is
written now because it costs nothing and it bounds the work either way.

**The theoretical bound is useless.** `callbackMaxFrame = 64 * goarch.PtrSize`
(`runtime/syscall_windows.go:256`), checked at `:310` — up to **64 words** of arguments. Nobody
enumerates 64 shims.

**The reach of the 11 consumers is what bounds it**, and it splits in two:

| set | arities | source | gate |
|---|---|---|---|
| **ungated** | **0, 1, 2, 4** | the direct `NewCallback` call sites in the other tests | none |
| gated | **2 … 10** | `TestStdcallAndCDeclCallbacks`'s tables | ⚠ `t.Skip("skipping test: gcc is missing")` |

- `cbFuncsRegABI` (selected when `runtime.SetIntArgRegs(-1) > 0`, i.e. on amd64) holds `sum2`…`sum10`
  — verified from their declarations as **2…10 params** — plus `sum5andPair` (**5**, all of them the
  `uint8Pair` struct), `sum9uint8` / `sum9uint16` / `sum9int8` / `sum9andGC` (**9**) and `sum5mix`
  (**5**). `cbFuncs`, the non-register-ABI table, spans **2…9**.
- **Union: arities 0 through 10 — ELEVEN shims for full coverage, FOUR (0, 1, 2, 4) for everything a
  host without gcc can reach.**

**Two consequences for the body.**

1. **Start at four, not eleven.** The 2…10 range lives entirely behind a gcc gate that skips on both
   sides, so on a bank host without gcc it contributes no verdict pressure at all. Four shims reach
   every row such a host can score; the remaining seven are a bounded, enumerated follow-on rather
   than an open set.
2. ⚠ **The parameters are NOT uniformly `uintptr`.** The tables use `uint32`, `uint8`, `uint16`,
   `int8` and the `uint8Pair` **struct** — all uintptr-sized or smaller, so all inside Go's contract
   (§7's "argument size is larger than uintptr" is what refuses the rest). The native shim's own
   parameters are machine words either way; it is the **forward** that must produce the converted
   parameter types. **That is a second, independent reason the forward must be TYPED rather than
   `DynamicInvoke`** — converting a machine word to `uint8Pair` is exactly the user-defined operator
   the default binder will not invoke (§8). The two arguments are separate and both hold.

**One correction against myself, on the record:** I first read `sum5andPair` as "5 arguments plus a
pair" from its name. Its declaration says `func sum5andPair(i1, i2, i3, i4, i5 uint8Pair) uintptr` —
**five** parameters, every one of them the struct. The name encodes the *shape*, not the count, and
the count came from the declaration.

---

## 13. ADDENDUM 2026-09-08 — §8 MEASURED ON THE i7; THE RECORD SEATS (COORD `7c5cbd767`)

§8 was written as the one claim its author could not execute, stated first so a cheap measurement
could settle it. **It has been measured on the i7 across seven arms, and BOTH claims are CONFIRMED.**
Per the standing rule this lands as a **dated block, never a rewrite** — §8 stands as written and this
is the reading on top of it.

| §8 claim | i7 reading |
|---|---|
| `Marshal.GetFunctionPointerForDelegate` refuses a **generic** delegate type | **CONFIRMED** |
| `DynamicInvoke` skips **user-defined conversions** | **CONFIRMED** |
| rooting holds the pointer valid across a forced collection (§4) | **CONFIRMED — sufficient** |

**So the body is the shape §8 predicted:** a **per-arity non-generic `[UnmanagedFunctionPointer]`
shim** forwarding by a **TYPED** call to the `Func<…>`/`Action<…>` behind the `any`, with the arities
bounded and enumerated in §12 — **four** shims for everything a gcc-less host reaches, eleven for full
coverage.

### ⚠ 13.1 A THIRD READING THAT SHARPENS §5, and it is not a confirmation — it is new information

**Pointer identity is per delegate INSTANCE.** `GetFunctionPointerForDelegate` returns a *different*
pointer for two distinct instances even when they are `Equals`-equal. §5 reasoned about
`Delegate.Equals` as the table's key without knowing this; the consequence is that **the table is not
merely an optimisation and not merely the rooting — it is what MAKES the identity rule true at all.**

    caching the shim per func value  ==  the rooting  ==  Go's `cbs.index` cache

Those are one mechanism here, where the record had them as two (§4 rooting, §5 identity). Without the
table, two `NewCallback` calls on one func value would yield two different pointers — which is
**neither** Go's behaviour **nor** the safe divergence §5 described. **§5's stated divergence is
unchanged in direction** (a static method group whose delegate instance C# caches still collapses to
one entry, and that is still the safe side); what changes is that the table is load-bearing for
correctness rather than for cost, and the body must not treat it as an optimisation it could skip.

### 13.2 What is now owed, in COORD's order (`84efb3ac5`)

1. **This block** — the record seats. ✅
2. **The `NewCallback` body** — the seam-named companion, the four-shim first cut, the typed forward,
   the table as rooting-and-identity, the seven refusal texts, the divergence in the companion's own
   header per ruling 1.
3. **The fatal-path increment** — separately, and per its own ruling: **ONE shape on all three
   flavours**, since the `write1` arm came off (see the fatal record's own addendum).

**Unchanged:** the author still cannot build or run any of it; the i7 compiles, runs the guard and the
F8 registration check, and posts the readings; C1 scores them.
