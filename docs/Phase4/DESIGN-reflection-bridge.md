# DESIGN — Native reflection bridge (Phase 4 operational)

> **Status (2026-07-24): Phases 1, 2, and PHASE-3 INCREMENT 1 (the write-back half) are SHIPPED.**
> Increment 1 landed via the §6.1 chip (session `optimistic-bassi-496625`; design + adversarial-review
> ledger in [`DESIGN-reflection-bridge-phase3-plan.md`](DESIGN-reflection-bridge-phase3-plan.md)),
> validating **errors (#34, 61/61 vs `go test -json`)** as its demonstrated consumer.
> Companion to the Phase-4 operational campaign in [`../Roadmap.md`](../Roadmap.md).
>
> ⚠ **"Phase 1/2/3" below are THIS DOCUMENT's scope phases** (§ *Scope*), unrelated to the project's
> Phase-3 / Phase-4 milestones.
>
> **Implemented** — `manualConversionFuncs` in `src/go2cs/manualTypeOperations.go` is the authoritative
> list; derive it fresh rather than trusting this summary: the Kind classifier + type helpers (golib
> `GoReflect`: `KindOf`, `GoTypeName`, `ElementType`, `IsComparable`, `TryAdapterWrappedType`),
> `internal/abi.TypeOf` (`type_impl.cs` synthesizes the descriptor from the managed `System.Type`),
> `reflect.{ValueOf, unpackEface, valueInterface}`, the ~17 `Value` readers + `MapIter.{Next,Key,Value}`,
> `rtype.{String, Name, Elem, Field, NumField}`, **canonical interned `Value.Type`/`toType`**
> (`canonType` — the map-key-ordering fix, § below), `deepValueEqual` (`deepequal_impl.cs`), the
> `internal/reflectlite` mini-bridge (`ValueOf`/`Len`/`Swapper`), and the `synthType.Equal`
> comparability signal (encoding/csv, 2026-07-21).
>
> **Phase-3 increment 1 (SHIPPED 2026-07-24, the chip):** the ONE new primitive — an **addressable
> Value carries the `ж<T>` box it aliases** (`addrBox` companion; reads go through the box lazily,
> `Set` writes through its slot ref via cached compiled accessors over `ValueSlot`) — plus
> `reflect.{Value.Set, Zero, methodName}` and the reflectlite errors.As surface
> (`Value.{Elem,IsNil,Set}`, `rtype.{Elem,Implements,AssignableTo}`, `methodName`), all over shared
> golib machinery (`GoReflect.{TryMarshalAssignable, GoImplements, ReadPointerSlot/WritePointerSlot,
> CanonicalNilPointer, GoDynamicTypeOf}`) so reflection and emitted `_<T>` asserts can never disagree
> about a method set. With it landed: **canonical typed-nil pointer boxing** (X2 — converter + golib
> `ж<T>.NilBox` + gen; see ConversionStrategies-Reference `Canonical typed-nil pointer boxing`),
> structural nil-pointer identity (`INilPointer`; heap-box-holding-nil ≠ nil pointer), the R10
> adapter unwrap (`GoDynamicTypeOf` — descriptors classify the Go dynamic type, so adapter-held and
> raw-box values intern to ONE canonical Type), the golib assert-fallback unwrap + ж-overload
> closing (X1) with the non-generic `TryTypeAssert(object, Type, out object)`, dyn-wrapper
> `IInterfaceAdapter` transparency + the `[GoRecv]` method-set fix (X3), Go-parity empty-name
> subtest numbering (X4), the oracle's two-phase address-token pairing (X5), and `%v`-of-
> method-bearing-pointer fidelity (handleMethods wins, never `&`-prefixed). The `getcallersp`
> `NotImplementedException` chain is severed at `methodName` — the semantic boundary where a
> managed answer exists.
>
> **Phase-3 increment 3 (SHIPPED 2026-07-26) — the type-relation mirrors + conversion.** The
> deferred "reflect-side `Implements`/`AssignableTo` mirrors" row landed with its demonstrated
> consumers (encoding/gob's init via `validUserType → implementsInterface`, go/token's
> `TestSerialization`, internal/fmtsort's `ct()` table): hand-owned `rtype.{Implements,
> AssignableTo, FieldByName}`, `PointerTo`, `Value.{Convert, Cap, SetLen}` — each severing a
> descriptor-SPECIALIZATION read (`Reinterpret<abi.Type, interfaceType/structType>`, the ptrType
> prototype, the cvt*/unsafe_New chain) that cannot exist behind a synthesized descriptor — plus
> the `StructField.Index` stamp (gob's `FieldByIndex` walk), golib **pointer order tokens**
> (`INilPointer.PointerOrderToken`: equal pointers token equally, same-storage elements order by
> index — fmtsort's map-key ordering), `runtime.Pinner.Pin/Unpin` as no-ops by construction, and
> `-tests` init-order relocation (the erasable `initᴛᴛtests` static-ctor hook; see
> ConversionStrategies-Reference *Package-Level Variable Initialization Order*). Validated:
> internal/fmtsort 3/3, go/token 31/31; gob runs its Encoder/Decoder engines end-to-end (full gob
> validation remains open).
>
> **Phase-3 increment 3a (SHIPPED 2026-07-26) — `Value.Addr`, `rtype.PkgPath`, and the EMPTY
> interface.** Found by running encoding/gob's own 98-Test suite (68 → 79 passing). Three roots,
> each general:
>
> - **`Value.Addr`** derived its pointer TYPE through `ptrTo → typesByString → typelinks()` — the
>   linker-built, string-sorted type table, a runtime intrinsic with no managed form (its stub
>   throws). Every `Addr` therefore threw, taking out ELEVEN gob GobEncoder round-trip tests. But
>   the bridge already *holds* the address: an addressable Value ALIASES the `ж<T>` box its storage
>   lives in (`addrBox`), so `Addr` surfaces that box as a Pointer-kind Value, and `Elem` on the
>   result re-enters the same box — Go's `v.Addr().Elem() == v` contract (#32772) by construction.
> - **`rtype.PkgPath`** read the descriptor's `TFlagNamed` bit and `uncommon().PkgPath` name-offset,
>   which a synthesized descriptor never populates, so it answered `""` for EVERY type — gob's
>   `Register` then keyed its wire-type registry on the bare `"N2"` instead of `"encoding/gob.N2"`
>   (`TestRegistrationNaming`). The managed nesting carries the package identity: the declaring
>   `<pkg>_package` class names the package and the enclosing namespace names its parent
>   directories (`GoReflect.GoPackagePath`). Not a strict inverse for a major-version directory
>   (`math/rand/v2` → recovers `"math/rand"`) or a module dependency renamed away from its path
>   segment; exact everywhere else.
> - **The EMPTY interface is `object`, which reports `IsInterface == false`.** `GoReflect`'s
>   `GoImplements` and `TryMarshalAssignable` both gated their interface arms on `IsInterface`, so
>   `AssignableTo(t, interface{})` and `Set` into an `any` slot answered NO for every type —
>   gob's `decodeInterface` rejected every concrete value it had just decoded, then
>   `reflect.Value.Set` rejected it again ("gob: int is not assignable to type interface {}" →
>   "reflect.Set: value of type int is not assignable to type interface {}"). Both now carry the
>   explicit `typeof(object)` arm `KindOf` and `TryConvertTo` already had. Cleared gob's
>   TestInterfaceBasic / TestInterfacePointer / TestNestedInterfaces.
>
> **✅ gob's build blocker is CLOSED (2026-08-02, r37-gob) and gob is MEASURED — the "79 of 98" above
> is superseded by 86 of 106.** It was build-blocked when increment 5 looked:
> `package_info_internal_test.cs` emitted
> `[assembly: GoImplement<gob_internal_test_package.Point, Pythagoras>]`, but `Pythagoras` is declared
> in the EXTERNAL test package (`example_interface_test.go`, `package gob_test`) and gob declares a
> SECOND, unrelated `Point` there — so the record anchored the internal variant's same-named type and
> left the interface unqualified in a file where it is not in scope: `CS0246`, no test host, all 106
> verdicts empty. That was test-project-model record anchoring (the `splitWhiteboxVariantRecords`
> family), **not** reflection surface: a BARE record name resolves in the variant that RECORDED it, and
> the bridge's declared-name set was being consulted across variants. Fixed and guarded — see
> `ConversionStrategies-Reference.md`, *A BARE record name resolves in the variant that RECORDED it*.
>
> **Measured, one run, zero empty verdicts: 86 of 106 match** (C# 81 pass + 5 skip vs Go 101 pass + 5
> skip; 19 capability-excluded, 0 disclosed). Full per-root census — seven roots, only one of them this
> arc's descriptor surface — is the `encoding/gob` section of
> [`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md).
>
> **Rooted gob residues owned by THIS arc (open, NOT disclosure candidates) — 3 of the 20.** The
> managed `array<T>` type does not carry its LENGTH, so `reflect.Type.Elem()` of a `*[N]T` loses N and
> gob sees the wrong type where the wire says `[7]int` — `TestSingletons` and `TestIndirectSliceMapArray`
> (`Value.Elem`/`abi.TypeOf` recover dims from the LIVE value, but a type-only walk cannot).
> `TestIgnoreDepthLimit` is `reflect.ArrayOf` → the `typelinks` stub. A fourth is adjacent and worth this
> arc's attention even though it is not the descriptor surface: the five remaining GobEncoder tests share
> ONE root that is not reflection at all — their `GobDecode` bodies write through a reinterpreted
> named-type pointer, `fmt.Sscanf(s, "VALUE=%s", (*string)(v))`, and the reinterpret is emitted as a
> value conversion into a fresh box (`Ꮡ((@string)(v))`) because `reinterpretManagedEmission` is gated on
> a deref context or a raw-address source, neither of which a pointer conversion used as an ARGUMENT
> satisfies. The direct-field-write case (`ByteStruct`, `TestGobEncoderStructSingleton`) passes, so
> `Addr`'s write-back path is sound. `TestNetIP` was never blocked on `net`'s package init (that claim is
> retracted — `fd_windows` is not on its stack): it dies in `unique`'s initializer, where r37-gob fixed a
> dead deref alias in `internal/concurrent` and thereby exposed the root behind it —
> `abi.TypeOf(m).MapType()` over a zero map yields a descriptor with no `Hasher`, so `NewHashTrieMap`
> throws `Delegate to an instance method cannot have null 'this'`. That one IS this arc's surface. The
> rest are gob wire/typed-nil behaviours, all bucketed on the board.
>
> **Phase-3 increment 4 (SHIPPED 2026-07-31) — the `getcallersp` chain: `runtime.Callers` +
> `Frames.Next` managed.** The chip's io increment. The failing surface was never the flatten
> OPTIMIZATION (pure Go, converts fine) but the tests' MEASUREMENT of it: `runtime.Callers` →
> `callers()` opens with `getcallersp()` (assembly) and `Frames.Next` reads linker `funcInfo`
> tables. Both contracts are answered natively in `runtime/managed_impl.cs` over
> `System.Diagnostics.StackTrace` with a GO-LOGICAL frame projection — only source-declared Go
> functions count; adapter shells (`IGoAdapter`) and go2cs-gen forwarders (`[GeneratedCode]`) are
> invisible, exactly as Go's interface dispatch adds no frame — so relative depths match Go's
> logical model (io's `readDepth == myDepth+2` holds bit-exactly); PCs are opaque interned
> process-lifetime tokens. **`getcallersp` itself stays an honest stub** — the chain is severed at
> the API boundary where a managed answer exists, the `methodName` rule. io: 45/54 → **47/54**;
> the remaining seven verdicts keep their non-reflection owners (os arc, StringCheckCall, two
> alloc-profile disclosure rulings). Full design:
> ConversionStrategies-Reference `runtime.Callers / Frames.Next walk the managed stack projected
> to GO-LOGICAL frames`. (Same landing repairs the whitebox adapter-pair resolver — a bare cast
> of a variant-local type resolved to the first same-simple-name FOREIGN record
> (`bytes_BufferжReader` for io_test's own `Buffer`, CS1503 ×20), which had silently re-walled
> the io build the board records as closed; anchor-local records now win, guarded by
> `TestBareCastPrefersAnchorLocalRecordOverForeignSimpleNameMatch`.)
>
> **Phase-3 increment 5 (SHIPPED 2026-08-02) — `reflectlite`'s `rtype.String`.** The chip's
> `context` increment, and the quietest descriptor read yet: `String()` is
> `t.nameOff(t.Str).Name()`, a **name OFFSET** into the linker-built name blob that a synthesized
> descriptor never populates — so every `reflectlite.TypeOf(x).String()` in the corpus answered
> `""`, and answered it **without faulting**, because `""` is a legal name for an unnamed type.
> `context`'s `stringify` fallback printed `WithValue(, c1k1)` where Go prints
> `WithValue(context_test.key1, c1k1)`. Bridged in `internal/reflectlite/type_impl.cs` over
> `GoReflect.GoTypeName` — the same answer `reflect`'s long-hand-owned `rtype.String` and `%T`
> give, so the full bridge and the mini bridge cannot disagree about a type's name — with array
> dims threaded as on the `reflect` side. Blast radius is three call sites (the fix plus two panic
> messages); `rtype.Name` is untouched and still `""` (it gates on the `TFlagNamed` bit
> `synthesizeDescriptor` never sets), and `errors` never reaches `String()` at all. `context`:
> 36/38 → **37/38**, leaving only the measured `TestAllocs` alloc-count disclosure its banking
> commit owns. Full design: ConversionStrategies-Reference *The type NAME is a descriptor read
> too*. **Recorded next gap of the same shape: `rtype.Name`** — deliberately not fixed without a
> consumer that demonstrates it.
>
> **NOT implemented — remaining Phase-3 surface:** `MakeFunc`; variadic `Call`/`CallSlice`
> (text/template); `SetMapIndex` delete-on-invalid + `MapKeys` (encoding/json); the Go
> unnamed↔named `directlyAssignable` refinement beyond identity+wrapper (binary named-slice cases
> if they surface); `FieldByName`'s embedded-field depth search (a promoted name currently answers
> the not-found path); open question 3 (field-name/tag fidelity — `[GoTag]` is carried but not yet
> projected). Known limitation (recorded): named func types collapse to their structural
> `Func<>`/`Action<>` under `canonType` System.Type interning — carried named-func identity is
> needed if a consumer (gob's type registry) lands on it.

## The problem

Go's `reflect` is built on reading an interface's internal two-word layout through `unsafe.Pointer`.
An `any` value is an `eface = { *abi.Type type; unsafe.Pointer data }`; `reflect` reinterprets the
address of the interface as that struct, reads the `type` word (a pointer to the runtime **type
descriptor** `abi.Type`), and reads/writes the value through the `data` word as flat memory at
computed field/element offsets.

None of that exists in the managed world. A go2cs `any` is a `System.Object` **reference** — one
word, no adjacent type descriptor, no flat-memory value the GC will let you address by offset.
Reinterpreting the object reference as `{type,data}` reads garbage and NREs. Concretely, the first
operational hit is:

```
color.New(FgGreen).Println("…")
  → fmt.Sprint(a…) → fmt.doPrint → reflect.TypeOf(arg).Kind()   // spacing decision
  → internal/abi.TypeOf(any a):  ~(ж<EmptyInterface>)(uintptr)(Ꮡ(a))   // reinterpret → NRE
```

(Plain `fmt.Println` sidesteps this: `doPrintln` never calls `reflect.TypeOf`; only `doPrint` — used
by `Print`/`Sprint`/`Sprintf` fallbacks — does. That is why the `fmt.Println("hi")` milestone runs.)

## Key insight — the entire unsafe chain enters at THREE constructors

A full read of the `reflect`/`internal/abi`/`fmt` surface (see the surface map below) shows the whole
model bottoms out on **one** primitive — the eface `{type,data}` reinterpret — reached at exactly
three points:

| # | Function | File | What it does today |
|---|---|---|---|
| 1 | `internal/abi.TypeOf(any a)` | `internal/abi/type.cs:125` | `&a` → `*EmptyInterface` → read `.Type` word |
| 2 | `reflect.unpackEface(any i)` (→ `ValueOf`) | `reflect/value.cs:156` | `&i` → `*EmptyInterface` → read `.Type` + `.Data` |
| 3 | `reflect.toType(*abi.Type)` | `reflect/type.cs:3040` | wraps a descriptor pointer as an `rtype` |

Every downstream `Type`/`Value` method then reads *from those words* — `Type.Kind()` masks
`abi.Type.Kind_`; `Value.Int()` does `~(ж<int64>)(uintptr)(v.ptr)`; `Value.Field(i)` does
`add(v.ptr, offset)`; etc.

**So the bridge is: replace those three constructors so they carry a `System.Type` (+ the boxed
`object` for a `Value`) instead of two raw words. The ~30 downstream methods `fmt` needs then become
ordinary managed-reflection wrappers** (`obj.GetType()`, `FieldInfo.GetValue`, array indexing,
`Convert.ToInt64`, `IDictionary` enumeration) with no `unsafe` anywhere.

## Architecture — a native `reflect`/`abi` shim over `System.Type` + `System.Object`

Hand-own the reflection **entry points and the exercised methods** (whole-file
`[module: GoManualConversion]`, the established pattern — cf. native `sync`, `atomic.Value`), so:

- `reflect.TypeOf(x)` returns a `Type` carrying `x.GetType()` (a `System.Type`), **not** a
  `ж<abi.Type>` descriptor pointer.
- `reflect.ValueOf(x)` returns a `Value` carrying `(object box, System.Type)`.
- `internal/abi.TypeOf(x)` returns a compatible handle (or `reflect` stops calling it — TBD, see Q4).
- Each `Type`/`Value` method is reimplemented over managed reflection.

The one genuinely new primitive is the **Kind classifier** — `System.Type → reflect.Kind` — the root
of every method. It reads go2cs's own representations and attributes:

| Go Kind | go2cs C# representation | detect |
|---|---|---|
| Bool / Int* / Uint* / Float* | `bool`,`nint`,`int`,`long`,`byte`,`double`,… | `typeof` |
| Uintptr | `uintptr` (golib struct) | `typeof(uintptr)` |
| Complex128 | `System.Numerics.Complex` | `typeof` |
| String | `@string` | `typeof(@string)` |
| Slice / Array / Map / Chan / Pointer | `slice<T>`/`array<T>`/`map<K,V>`/`channel<T>`/`ж<T>` | open generic typedef |
| Func | `GoFunc` / delegate | `IsSubclassOf(Delegate)` |
| Struct | `[GoType]` value struct (no `num:`) | `[GoType]` + `IsValueType` |
| Interface | Go interface → C# interface | `IsInterface` |
| UnsafePointer | `@unsafe.Pointer` (`: ж<uintptr>`) | `typeof` |
| *named* `type Celsius float64` | `[GoType("num:float64")] struct Celsius` | Kind = underlying; `Name`/`String` from the type |

The metadata the shim recovers Go type info from is **already emitted**: `[GoType(def)]` (struct/
interface marker + `num:<kind>` for named numerics), `[GoTag]` (= `DescriptionAttribute`, the raw Go
struct-field tag), `[GoRecv]` extension methods (the method set), and the golib generic types. C#
`FieldInfo` enumeration returns fields in declared (= Go source) order.

## Scope — three phases, each independently useful

**Phase 1 — `TypeOf().Kind()` / `.String()` (unblocks the color sample + scalar `Print`/`Sprint`).**
`doPrint` calls `reflect.TypeOf(arg).Kind()` for *every* arg; the value itself formats via the fast
path or the `Stringer`/`error`/`Formatter` C# interface assertions in `handleMethods` — which use **no
reflection at all**. So a `Type` shim exposing `Kind()`, `String()`, and `Elem()` (byte-slice check),
plus the Kind classifier, makes `fmt.Print`/`Sprint`/`Sprintf` work for every scalar and every type
with a `String()` method. **~1 shim type, ~4 methods, +1 classifier.** This is the minimal color-sample
unblock.

**Phase 2 — full `printValue` walk (composites without a `String()` method format correctly).**
Add the `Value` shim (~21 methods actually exercised: `Kind, IsValid, CanInterface, Interface, Type,
Bool, Int, Uint, Float, Complex, String, IsNil, NumField, Field, Elem, Index, Len, Bytes, CanAddr,
UnsafePointer, Pointer`), `Type.{Elem, Field(i).Name}`, a `MapIter` (`Next/Key/Value` over
`IDictionaryEnumerator`, for `fmtsort`), and `StructField.{Name,Tag,Type}`. **~2 core shim types +
MapIter + StructField, ~30 methods**, all managed reflection. Makes `%v`/`%+v`/`%#v` of structs,
slices, maps, and pointers correct.

**Phase 3 — write-back & call (`Value.Set*`, `Value.Call`, `MakeFunc`, addressability) — OPEN; the
chip's scope.** Needed by `encoding/binary`, `encoding/gob`·`json`·`xml`, `testing/quick`,
`text/template`, and (transitively) `math/big`; not by `fmt`. Larger, and **best designed against a
concrete consumer** — which is exactly why the charter (§6.1) spawns this chip only once a package's
differential actually lands on this surface, and requires designing WITH the user + adversarial design
review before implementation. Carry the `getcallersp` stub and the adapter-type `Kind`/`Elem` unwrap
in the same chip.

## Open design questions (for review)

> **Resolved by what shipped (2026-07-22):** **Q1** — the native shim was confirmed and built; the
> descriptor-synthesis alternative was not pursued. **Q2** — both Phase 1 *and* Phase 2 were built.
> **Q4** — the answer was *both*, deliberately: the entry points and exercised methods are whole-file
> hand-owned `*_impl.cs`, while the reusable managed logic (Kind classification, Go type naming,
> element types, comparability, adapter unwrap) lives in golib `GoReflect` so `reflect`,
> `internal/abi`, `internal/reflectlite`, and golib's own `builtin` formatting all share one
> implementation. **Q3 is still open** and belongs to the Phase-3 chip. Q1–Q4 are kept below as the
> record of the decision.

1. **Approach — confirm the native shim.** Replace the 3 constructors + reimplement the exercised
   methods over `System.Type`/`System.Object` (recommended). The alternative — synthesize faithful
   `abi.Type` *descriptors* from `System.Type` and keep Go's converted `reflect` reading them via
   `unsafe` offsets — is judged infeasible (the offset reads themselves don't work in managed memory;
   it is strictly more surface than the shim).

2. **Scope to build now — Phase 1 only, or Phase 1 + 2?** Phase 1 unblocks the color sample and all
   scalar/Stringer formatting for the least work; Phase 2 makes composite formatting correct. Phase 3
   is deferred regardless.

3. **Field-name / tag fidelity.** `fmt`'s `%+v`/`%#v` (and later `json`) need the **exact Go** field
   name + tag. C# field names are the Go names except where escaped (`@string`) or Δ-collision-renamed;
   `[GoTag]` already carries the tag. Options: reverse-map the escapes at the shim, or have the
   converter emit a per-struct Go-name table (a small attribute) the shim reads. (Phase 2 concern.)

4. **Where the shim lives.** Hand-own `reflect` + `internal/abi.TypeOf` as whole-file
   `[module: GoManualConversion]` (consistent with native `sync`/`atomic.Value`), **or** put the
   managed logic in a golib `GoReflect` helper that the converted `reflect` delegates into (smaller
   hand-owned footprint, but a converted↔golib seam through the shim). Recommend whole-file hand-own of
   the entry points, since the value/type model is pervasively unsafe and not worth converting.

## Surface map (reference)

`abi.Type` fields: `Size_`, `Kind_` (the field everything keys off), `TFlag`, `Str`/`PtrToThis`
(offset-encoded), `PtrBytes`/`Hash`/`Equal`/`GCData` (unused by fmt). Kind enum (values 0–26) is
defined identically in `internal/abi/type.cs:40` and `reflect/type.cs:245`. `fmt` calls only
`Type.{String, Kind, Elem, Field(i).Name}` and the ~21 `Value` methods tabulated in Phase 2.
`handleMethods` (`fmt/print.cs:795`) formats `Stringer`/`error`/`GoStringer`/`Formatter` via C#
interface assertions — no reflection. Fast-path `printArg` types (no reflection):
`bool, int/8/16/32/64, uint/8/16/32/64, uintptr, float32/64, complex64/128, string, []byte,
reflect.Value`.

## Fix — reflect.Type must be canonical (map-key ordering)

Go's `reflect.Type` is a canonical interned descriptor: `TypeOf(x) == TypeOf(y)` exactly when `x`
and `y` share a dynamic type, so `aType == bType` is a pointer compare. `internal/fmtsort.compare`
(the map-key ordering used by `fmt`'s `%v`) relies on it: `if aType != bType { return -1 }`.

The bridge minted a **fresh** wrapper per access — `abi.TypeOf` allocates a new `abi.Type` box, and
both `Value.Type()` and `toType` then build a fresh `rtypeжΔType` (an `IжAdapter` compared by box
identity via `golib` `AreEqual`). So two Types describing the *same* type never compared equal →
`compare` returned `-1` for every pair → the stable sort **reversed** the keys
(`map[b:2 a:1]` instead of `map[a:1 b:2]`).

**Fix:** hand-own `Value.Type` and `toType` in `reflect/value_impl.cs` (registered in
`go2cs/manualTypeOperations.go` `manualConversionFuncs["reflect"]`) so both route through `canonType`,
which **interns** the `ΔType` wrapper in a `ConcurrentDictionary<System.Type, ΔType>` keyed on the
`abi.Type.sysType` the Phase-1 `synthType` stamped. Identity-equality then matches Go. The cache is
process-lifetime (type descriptors are permanent, like Go's). Interning by `System.Type` preserves
Go's named-type distinctness for free: a `type Celsius float64` is a distinct `[GoType("num:float64")]`
struct whose `System.Type` differs from `float64`, so `TypeOf(Celsius) != TypeOf(float64)`. `typeSlow`
(method-value Types) stays auto — not exercised by `fmt`.

## Fix — the bridge's per-type marker reads are memoized (2026-07-26)

The bridge recovers Go type identity from managed metadata, so it reads the converter's per-type
markers — `[GoType]` for a type's kind and underlying, `[GoLocalName]` for a lifted local type's
original Go name — from its hottest entry points: `KindOf` (under every `ValueOf`, `Value.Field`,
`Value.Elem`, `MakeSlice`, `rtype.FieldByName`, and `abi.TypeOf`), `GoTypeName` (under every `%T` and
`reflect.Type.String()`/`Name()`), and `TryConvertTo`/`TryUnwrapWrapperValue` (under the whole
`Value.Set`/`SetMapIndex`/`Call`/`Convert` marshalling surface). **Custom-attribute retrieval
materializes fresh attribute instances on every call** and none of those callers caches its own
result, so the cost was paid per VALUE rather than per type — measured at 165–558 ns and 72–448 bytes
per call across those entry points, of which the attribute read was the bulk. All four reads now route
through two per-type `ConcurrentDictionary` memos (`goTypeMarkerOf` / `goLocalNameOf`), which is
sound for the same reason `canonType`'s intern above is: type descriptors are permanent, and a loaded
type's own attributes cannot change. Full measurement and the two remaining non-attribute residuals
are in [`DESIGN-iface-shell-caching.md`](DESIGN-iface-shell-caching.md) §11.2, which is where the
audit that found them lives.
