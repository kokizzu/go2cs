<!-- {% raw %} — Jekyll/Liquid guard: this doc contains {{ sequences (Go composite-literal syntax) that Liquid would otherwise parse or silently eat. Keep the matching endraw as the final line. -->
# DESIGN — `reflect.StructOf`: runtime struct synthesis in the managed reflect bridge

> **STATUS: DESIGN — NOT IMPLEMENTED. Written first, committed first, so a veto costs rework rather
> than archaeology.** No converter change, no golib change and no corpus change is carried on this
> branch; §11 is this document's adversarial pass against its own first draft, and §10 lists the
> questions that are the coordinator's to rule rather than this lane's to self-rule.
>
> **Commissioned by** the board's pricing of `encoding/gob`'s last verdict — *"the last verdict costs
> one small hand-own (`reflect.ArrayOf`, roughly free over the dims cargo) **plus** `reflect.StructOf`,
> which is runtime struct synthesis over `System.Reflection.Emit` and a feature arc in its own right"*
> ([`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md), lane
> `claude/map-key-elem-cargo`, 2026-08-20).
>
> **Companions (read alongside):** [`DESIGN-reflection-bridge.md`](DESIGN-reflection-bridge.md) (the
> bridge's architecture and the `canonType` interning fix), [`DESIGN-reflection-bridge-phase3-plan.md`](DESIGN-reflection-bridge-phase3-plan.md)
> (increment 2 — the call & construction half, which is where `New`/`MakeSlice`/`MakeMap` live), and
> the concurrent `reflect.ArrayOf` lane (`claude/reflect-arrayof`), whose dims-cargo composition §7.1
> depends on.
>
> **Sources of record for the mechanism claims below:** `src/core/internal/abi/type_impl.cs`,
> `src/core/reflect/value_impl.cs`, `src/core/golib/GoReflect.{cs,TypeLayout,TypeNaming,FieldAccess,MethodSets}.cs`,
> `src/go2cs/manualTypeOperations.go`. Line numbers are pinned at this note's banking commit.

---

## 1. What Go promises, and the subset the corpus exercises

### 1.1 The contract

```go
func StructOf(fields []StructField) Type
```

Go returns *the* struct type containing `fields` — interned, so `StructOf(f) == StructOf(f)` holds as
Go type identity. Each `StructField` supplies `Name`, `Type`, and optionally `Tag`, `PkgPath` (required
and only permitted for an unexported field) and `Anonymous` (embedding). Go panics, by its own
messages, on: a field with no name, an invalid name, no type, an unexported field missing `PkgPath`,
an anonymous field *with* `PkgPath` set, a duplicate field name, and a struct whose size would exceed
the virtual address space. Its doc comment carries one documented gap of its own —
*"StructOf currently does not support promoted methods of embedded fields"*
(`src/core/reflect/type.cs:2056`, and Go's `type.go:2354`).

Two properties of the contract are load-bearing here and easy to miss:

1. **Interning is observable.** `encoding/gob` keys `types map[reflect.Type]gobType` and
   `enc.sent map[reflect.Type]typeId` on the returned `Type`. A `StructOf` that minted a fresh
   descriptor per call would make every recursion a cache miss and every mutually-recursive type an
   infinite regress. Interning is not an optimization; it is the contract.
2. **The returned type is *storable*.** `reflect.New(StructOf(...))` must produce a real addressable
   zero value whose fields can be read, written and handed back out through `Interface()`.

### 1.2 The census — every `StructOf` site that exists

**Converted corpus** (`git grep -n StructOf -- src/core`, 33 matching lines):

| Class | Count | Where |
|:--|:--:|:--|
| the auto-converted declaration, its panic strings and comments | 20 | `src/core/reflect/type.cs` (`StructOf` at :2058, `runtimeStructField` at :2435) |
| `runtime.stringStructOf` — an unrelated name | 10 | `runtime/{map_faststr,print,string,tracestring}.cs`, `runtime/{windows,linux,darwin}/arena.cs` |
| `calcStructOffset` in a Go source citation | 2 | `go/types/{sizes,gcsizes}.cs` |
| **real call sites** | **3** | below |

The three real call sites, and what each is:

| Call site | What it is | Reachable? |
|:--|:--|:--|
| `src/core/reflect/type.cs:1571` — `funcTypes[n] = StructOf(...)` in `initFuncTypes` | Go builds a fake struct to *describe a func's argument frame in memory* for `funcLayout` | **No.** `Value.Call` is hand-owned (`value_impl.cs`) and invokes the boxed delegate; the frame layout has no managed meaning |
| `src/core/reflect/type.cs:2260` — `New(StructOf(...))` inside `StructOf` itself | Go builds `struct{S structType; U uncommonType; M [n]Method}` to get *an rtype followed in memory by a method array* | **No.** Same class: a linker-layout reconstruction. It is reached only from the `len(methods) != 0` arm, i.e. embedded fields with methods — which Go's own doc comment says StructOf does not support |
| `src/core/encoding/json/bench_test.cs:613` — `types[i] = reflect.StructOf(fs)` | `BenchmarkTypeFieldsCache`, minting `maxTypes` distinct single-field structs (10⁶, or 10³ on a builder) | **Compiles, never runs.** Phase-4D defers every `Example`/`Benchmark` declaration from the run registry (`src/go2cs/testConversion.go:1342`), so it is a *latent* consumer — and §5.1 shows it is the one that prices the mechanism |

Both in-`reflect` callers are **self-hosting layout tricks**. This is the same argument the board made
for `ArrayOf` — *"the auto form dies in `typesByString` only because it is reconstructing Go's
linker-allocated `arrayType` record, which the managed bridge never needs to"* — and it means a
hand-owned `StructOf` **is not obliged to satisfy reflect's own two callers**. That is worth stating
plainly, because a first read of the census suggests StructOf is load-bearing inside reflect and it
is not.

**GOROOT 1.23.12**, outside `reflect` itself and outside `cmd/` (which go2cs does not convert):
**5 call sites in 2 packages** — `encoding/gob/gobencdec_test.go` (4, all inside one test) and
`encoding/json/bench_test.go` (1). There is no third consumer in the standard library.

### 1.3 The subset the corpus actually exercises

Across every reachable and latent site above:

| Contract feature | Exercised? |
|:--|:--|
| exported, named, non-embedded field | **yes** — every site |
| a field whose type is itself synthesized (`ArrayOf`, `StructOf`) | **yes** — gob, 101 levels deep |
| `StructField.Index` populated by the caller | yes (json's bench; ignored by Go, recomputed) |
| interning / `Type` as a map key | **yes** — gob's `types`, `enc.sent` |
| `reflect.New` over the result, then `.Interface()` | **yes** — gob |
| field tags | no |
| unexported fields + `PkgPath` | no |
| embedded (`Anonymous`) fields | no |
| embedded fields *with methods* | no — and Go does not support them either |
| directional-channel field types | no |

That table is the design's scope statement, and §6 turns it into a contract subset with a loud panic
at its boundary. It is **not** a licence to implement gob's shape: the difference between scoping to a
contract subset and scoping to a test is the whole of §6.3.

### 1.4 gob's residual, named

`TestIgnoreDepthLimit`, `<GOROOT>/src/encoding/gob/gobencdec_test.go:799-834`. It builds a 101-deep
nested array with `ArrayOf`, wraps it in a `StructOf`, encodes it, and asserts the decoder rejects the
depth with the exact error `"invalid nesting depth"` (`decode.go:911-919`); it then repeats the same
assertion with a 101-deep nested *struct* built entirely from `StructOf`. The board measured
`encoding/gob` at **105 of 106 matching verdicts** with this one row divergent
(lane `claude/map-key-elem-cargo`, 2026-08-20, on Go 1.23.1).

**That denominator survives the corpus hop.** Measured this lane: all **21** files of
`encoding/gob` are byte-identical between Go 1.23.1 and Go 1.23.12 (`diff` over the directory, 0 of 21
differing, with `runtime/proc.go` as the positive control confirming the comparison can go red). So
unlike the four rows the hop re-derived, gob's 106 needs no re-derivation before it is used as row
math — though §8 still re-measures it, because a *measurement* is what banks a row, never an argument
that nothing moved.

**What the test demands of the bridge, read off gob's own source** rather than inferred:

| gob code | Demands |
|:--|:--|
| `buildTypeInfo` → `name := rt.String()` | `Type.String()` renders the synthetic struct |
| `newTypeObject`, struct arm (`type.go:535-563`) | `NumField()`, `Field(i)` with truthful `.Name`, `.PkgPath`, `.Type`, `.Tag`; `isSent` reads `.Name` for exportedness |
| `getType` → `types[rt]` (`map[reflect.Type]gobType`) | the descriptor is a correct, interned **map key** |
| `compileEnc` → `encInstr{…, f.Index, …}` | `StructField.Index` is `[]int{i}` |
| `encodeStruct` (`encode.go:306-330`) → `value.NumField()`, `value.FieldByIndex(instr.index)` | `Value.NumField`, `Value.FieldByIndex` over a synthesized instance |
| `encodeArray` (`encode.go:333-353`) → `value.Index(i)` ×101 | real storage 101 levels deep, walked by the ordinary `Value.Index` |
| `validUserType` → `implementsInterface` (GobEncoder/Decoder/BinaryMarshaler/…) | `Type.Implements` answers **false**, and does not throw, for a type no generator ever saw (§7.3) |

Note what is *not* on that list: the decoder side is pure gob logic over the wire, so nothing about
`StructOf` reaches it. **The entire cost of the row is on the encoder's walk of a synthesized type.**

---

## 2. The mechanism today, and exactly where the auto form dies

### 2.1 How the bridge mints a type descriptor

There are three layers, and the bottom one is a CLR `System.Type`:

| Layer | Type | Where |
|:--|:--|:--|
| descriptor | `ж<abi.Type>` | `src/core/internal/abi/type.cs:19` |
| reflect wrapper | `ж<rtype>` | `src/core/reflect/type.cs:283` |
| client-facing | `ΔType` (concretely `rtypeжΔType`) | minted at `src/core/reflect/value_impl.cs:1492` |

The *truth* is none of those. It is the `System.Type` carried on the descriptor as companion cargo
(`src/core/internal/abi/type_impl.cs:54-60`):

```csharp
partial struct Type {
    [GoReflectCompanion] public System.Type? sysType;
    [GoReflectCompanion] public nint[]? arrayDims;
    [GoReflectCompanion] public nint[]?[]? funcParamDims;
    [GoReflectCompanion] public GoChanDir chanDir;
    [GoReflectCompanion] public nint[]? keyDims;
}
```

`abi.synthType(System.Type, dims, funcParamDims, chanDir, keyDims)` (`type_impl.cs:90`) is the sole mint,
interning on `(System.Type, descriptorDimsKey(...))` in `s_descriptors` (`:80`); `reflect.canonType`
(`value_impl.cs:1468-1492`) interns the `ΔType` wrapper on the **same** key. Everything a struct
descriptor answers is derived, memoized per `System.Type`, from CLR metadata plus converter-emitted
attributes:

- **Kind** — `GoReflect.KindOf` (`GoReflect.cs:129-220`); its last arm is literally `if (t.IsValueType) return Struct;` (`:204`), and anything reference-typed that falls through is classified `Pointer` (`:219`).
- **Fields** — `GoReflect.GoFields(Type)` (`GoReflect.FieldAccess.cs:322`), memoized in `s_goFields`, projecting real `FieldInfo`s to Go names, tags (`[GoTag]`), dims (`[GoArrayDims]`, `[GoMapKeyDims]`), embeddedness and Go declaration order.
- **Offsets/size/align** — `GoReflect.GoFieldOffsets` → `structLayoutOf` (`GoReflect.TypeLayout.cs:139, 190-233`): one memoized pass computing **Go amd64** offsets over the Go-projected field list, never `Marshal.OffsetOf`.
- **Name/String** — `GoReflect.GoTypeName` / `HasGoName` (`GoReflect.TypeNaming.cs:195, 233-274`); a `[GoType("dyn")]` stamp with no `[GoLocalName]` makes `HasGoName` answer **false**, so `Name()` is `""` and `String()` renders structurally through `goStructTypeString` (`:360-391`).
- **PkgPath** — `GoPackagePath(t)` = `GoPackageClassPath(t.DeclaringType)` (`TypeNaming.cs`), derived purely from the declaring class's **namespace plus package-class name**. §4.4 uses that.
- **Storage** — `reflect.New` (`value_impl.cs:1079-1092`) = `GoReflect.NewPointerBox(st, GoReflect.ZeroValueOf(st, …))`, whose struct arm is `Activator.CreateInstance(t)` (`ValueMarshalling.cs:329`).
- **Addressable field access** — `Value.Field` (`value_impl.cs:784-823`) obtains a write-through alias via `GoReflect.FieldAliasBox` → `buildFieldAccessor` (`FieldAccess.cs:573-603`), which emits a **`DynamicMethod`** doing `Castclass` → `callvirt ValueSlot` → a chain of `Ldflda` on real `FieldInfo`s.

**Not one of those looks at where a `System.Type` came from.** That single observation is what makes §4
cheap and §5.2 expensive.

### 2.2 Why the current machinery cannot satisfy StructOf

Because every entry point is a *function of an existing `System.Type`*, and `StructOf` is asked to
produce a Go type for which **no `System.Type` exists**. There is no synthesis direction in the bridge
at all. `PointerTo` (`value_impl.cs:2131-2137`) is the one hand-owned type constructor, and it works
precisely because `typeof(ж<>).MakeGenericType(st)` hands it an existing CLR type; the same idiom
serves `abi.synthesizeArrayType`'s internal `typeof(slice<>).MakeGenericType(elem)` (`type_impl.cs:411-429`),
`rtype.Elem` and `Value.Slice`. A struct has no generic container to instantiate.

Three constraints follow, and they are **forced, not chosen**:

1. **The descriptor must carry a real CLR `System.Type`.** `canonType` `Debug.Assert`s on a null `sysType` (`value_impl.cs:1481-1484`), and that assert *kills the process* (0x80131623) — documented at `value_impl.cs:1735` as having already happened once, via `rtype.FieldByIndex`.
2. **It must be a CLR value type**, or `KindOf` calls it `Pointer`.
3. **Its fields must be real `FieldInfo`s reachable by `Ldflda`** through `ж<T>.ValueSlot`, or `Value.Field` cannot produce a write-through alias and `Activator.CreateInstance` cannot make a zero value.

### 2.3 Where the auto form dies, precisely

`typelinks` is a bodyless partial (`src/core/reflect/type.cs:1322`) that `go2cs-gen`'s
`PartialStubGenerator` fills with a throw (`src/gen/go2cs-gen/PartialStubGenerator.cs:111`), so the
runtime message is exactly:

```
typelinks: external (assembly or cgo) function is not implemented
```

`typesByString` calls it on its first line (`type.cs:1356`), and **seven** auto type constructors call
`typesByString`:

| Function | call site | status |
|:--|:--|:--|
| `ptrTo` | `type.cs:1163` | bypassed — `PointerTo` and `Value.Addr` are hand-owned |
| `ChanOf` | `type.cs:1458` | dead |
| `MapOf` | `type.cs:1502` | dead |
| `FuncOf` | `type.cs:1670` | dead |
| `SliceOf` | `type.cs:1941` | dead |
| **`StructOf`** | **`type.cs:2333`** | **dead** |
| `ArrayOf` | `type.cs:2510` | dead — the concurrent lane's target |

So the *observed* failure is `typelinks`, and it is a red herring in the same way the board said it
was for `ArrayOf`: `StructOf` reaches `typesByString` only in its "look in known types" step, and even
if that step were satisfied, everything after it — `structTypeFixedN` prototypes, GC-program
construction, `resolveReflectName` into the linker's name blob, `unsafe_New` — is Go's runtime
reconstructing linker output. **The auto form is unreachable in principle, not blocked by one stub.**
A hand-own is the only route, which is why §4 does not consider "implement `typelinks`" as an option.

---

## 3. The mechanism space, priced

Four shapes were considered. The axes that decide it are: how many places in the bridge grow a
**second path** (the repo's dominant reflection failure mode — a synthetic path that answers a
question by a different rule than the converted path, and is therefore what a green row proves);
**cost per synthesized type**; and **blast radius** at the gate.

| | new forks in the bridge | cost / type (measured, §5.1) | ceiling | verdict |
|:--|:--|:--|:--|:--|
| **A. ⭐ `TypeBuilder` synthesis** — mint a real CLR struct; the descriptor is derived from it exactly as for a converted struct | **zero** | 582 µs, ~2.4 KB working set | 10⁶ types ≈ 9.7 min / ~2.4 GB | **recommended** (§6) |
| **B. Descriptor-only synthetic type** — an `abi.Type` with computed offsets and no backing CLR type | **≈ 10**, listed in §3.2 | ~1 µs | none | rejected on fork count |
| **C. Generic-arity-backed struct** — `SynthStruct<T1…Tn>` in golib, instantiated by `MakeGenericType`; names/tags/dims ride as new descriptor cargo | **2–3** (`GoFields`, `GoTypeName`, the identity key) | ~1 µs after first instantiation | arity cap | **the escape hatch** (§6.4) |
| **D. Implement `typelinks`** | — | — | — | not a candidate (§2.3) |

### 3.1 A — `TypeBuilder` synthesis

Mint one process-wide `AssemblyBuilder`/`ModuleBuilder`; per synthesized Go struct emit a
`TypeBuilder` with `TypeAttributes.SequentialLayout | Sealed`, parent `typeof(ValueType)`, one public
field per Go field in Go declaration order, and these stamps:

| Stamp | Carries | Read by |
|:--|:--|:--|
| `[GoType("dyn")]` on the type | "this is a Go-anonymous struct" | `HasGoName` → `Name()==""`; `GoTypeName` → structural `String()` |
| `[GoTag("…")]` on a field (aliased to `DescriptionAttribute`) | the Go struct tag | `GoReflect.goTagOf` |
| `[GoArrayDims(…)]` on a field | an array/pointer/map-elem field's Go dimensions | `GoReflect.FieldStampedDims` |
| `[GoMapKeyDims(…)]` on a field | a map field's key dimensions | `GoReflect.FieldMapKeyDims` |
| nesting inside a synthesized `<pkg>_package` container (§4.4) | the Go import path for unexported fields' `PkgPath` | `GoPackageClassPath` |

Then `abi.synthType(builtType)` and everything downstream — `GoFields`, `structLayoutOf`,
`GoFieldOffsets`, `structFieldOf`, `FieldAliasBox`, `ZeroValueOf`, `haveIdenticalUnderlyingType`,
`GoTypeName`, `canonType` — runs **unmodified**, because none of it asks where a `Type` came from.

**What works:** everything in §1.4's demand table, by construction.
**What breaks:** nothing measured. The two ceilings are §5.1's per-type cost and the unbounded
`ConcurrentDictionary<Type, …>` memoization (`s_goFields`, `s_structLayouts`, `s_zeroInstances`,
`s_descriptors`, `s_canonTypeCache`) that a synthesized type permanently occupies — fine at gob's 101,
not at json's 10⁶ (OQ-3, OQ-5).
**Blast radius:** one entry in `manualConversionFuncs`, one new golib file, one hand-own in
`reflect/*_impl.cs`. No converter *emission* change, so no corpus regen and no golden movement — but
the `reflect` package's own emitted `type.cs` does change (a hand-own becomes a placeholder comment),
so the corpus build and CNR both apply (§9).
**Honest-implementation test:** the row must pass with gob's encoder walking the synthetic type through
the *same* `GoFields`/`structLayoutOf`/`FieldAliasBox` machinery every converted struct uses. If any of
those grew a synthetic branch, the green row would prove the branch rather than the bridge. Under A
they cannot, because there is nothing to branch on.

### 3.2 B — descriptor-only synthetic type

An `abi.Type` with `sysType == null`, a computed field table and computed offsets; values backed by
some `GoSyntheticStruct { object[] fields }` container.

**What works:** unlimited types at ~1 µs each; it is the only shape that is Native-AOT-clean by
construction; offsets are already Go-computed so the layout half is nearly free.

**What breaks** — the enumeration the brief asked for, each item a place that must grow a second path:

1. `canonType`'s null-`sysType` assert (`value_impl.cs:1481`) — today a **process kill**.
2. `GoReflect.KindOf` — must answer `Struct` from the descriptor, not the CLR type.
3. `GoReflect.GoFields` — `Type`-keyed and memoized; a synthetic type has no `FieldInfo`s at all.
4. `GoReflect.structLayoutOf` / `GoFieldOffsets` / `GoSizeOf` / `GoAlignOf` — same key problem.
5. `GoReflect.GoTypeName` / `HasGoName` / `goStructTypeString` — `String()` must be rendered from the descriptor.
6. `reflect.New` → `ZeroValueOf` → `Activator.CreateInstance` — needs a synthetic allocator.
7. `Value.Field` → `FieldAliasBox` → the `Ldflda` `DynamicMethod` — needs a synthetic slot-alias.
8. `Value.Set` / `Set*` → `WritePointerSlot` over `ж<T>.ValueSlot` — needs a synthetic write path.
9. `Value.Interface()` → the packed object's `GetType()` is what `abi.TypeOf`/`GoDynamicTypeOf` reads back. **Every synthetic struct would report the same CLR type**, so all of them would collapse to one descriptor unless the *value* carries its own Go type — a change to the deepest assumption in the bridge.
10. `haveIdenticalUnderlyingType` / `Implements` / `AssignableTo` — the struct arm compares field tables that no longer come from one source.

**Blast radius:** the whole bridge, plus the `internal/reflectlite` mirror, which is the exact shape
whose divergences the board has recorded repeatedly (`rtype.String` answering `""` for every type;
`haveIdenticalUnderlyingType` returning `true` for any two structs). **The honest-implementation risk is
not that B cannot be made to work — it is that ten forks give ten places for the synthetic answer and
the converted answer to disagree, and gob's row would only ever exercise the synthetic one.** That is
the argument that decides against B, not the line count.

### 3.3 C — generic-arity-backed struct

Declare in golib `readonly struct SynthStruct<T1>`, `<T1,T2>`, … up to some arity *n*, each with public
fields `F1…Fn`; `StructOf` instantiates `typeof(SynthStruct<,>).MakeGenericType(clrFieldTypes)` and
carries the Go **names**, **tags**, **embeddedness** and per-field **dims** as new descriptor cargo,
widening `descriptorDimsKey` exactly the way `keyDims` widened it in the map-dims arc.

**What works:** real `FieldInfo`s (so `FieldAliasBox`, `Ldflda`, `Activator` and `Set` all work
unmodified — B's items 1, 2, 6, 7, 8, 9 all vanish); no `AssemblyBuilder`; near-zero per-type cost
after the first instantiation of each arity, which is what makes it the only shape that survives
json's 10⁶.

**What breaks:** two forks remain — `GoFields` must prefer descriptor cargo over `FieldInfo` metadata
for these types (and `GoFields` is `Type`-keyed, so the cargo cannot reach it without a signature
change or a parallel entry point), and `GoTypeName` likewise. Field *identity* moves into the cargo,
so two Go structs differing only in a field **name** share one CLR type and are distinguished only by
the widened key — correct, but it makes the key load-bearing for correctness rather than for caching.
And the arity is capped; beyond it there must be a loud panic, never a truncation.

**Blast radius:** golib (the `SynthStruct` family plus the cargo), `abi` (the key), `GoFields`,
`GoTypeName`. Real, but an order less than B.

### 3.4 What is FORCED and what is CHOSEN

**Forced** (by mechanism, not preference): a real `System.Type` on the descriptor; a CLR **value**
type; real `FieldInfo`s for `Ldflda`; **interning on Go struct identity** (gob's `map[reflect.Type]`
keys); field dims travelling as `[GoArrayDims]`/`[GoMapKeyDims]` because that is the only route
`GoFields` reads; `[GoType("dyn")]` on the type, because without it `Name()` answers a CLR name and
`String()` is wrong.

**Chosen** (and reversible): `TypeBuilder` over C's generic family; one dynamic assembly per process;
non-collectible `Run` (OQ-2); the increment-1 contract subset (§6). §6.4 makes the reversibility
structural.

---

## 4. Mechanism A, in detail

### 4.1 The interning key

Go interns on struct identity: the ordered list of (name, pkgpath, type, tag, embedded). The converter
already proves this key is sufficient and complete — its own compile-time anonymous-struct dedup keys
on `go/types`' `types.String()` **including field tags**, package-scoped
(`src/go2cs/visitStructType.go:84-90`), and the board records the consequence of getting it wrong:
*"repeated textual occurrences of `struct{ A Struct }` … must lift to a SINGLE C# type, or
reflect.Type identity splits per occurrence"*.

So `StructOf` renders the same key at run time and interns the **CLR type** on it. `synthType` and
`canonType` then intern the descriptor and the wrapper on top, so `StructOf(f) == StructOf(f)` holds
at all three layers. This is the runtime analogue of `structLookupCache` (`reflect/type.cs:1964`) and
of the converter's deterministic-winner registry (`src/go2cs/dynamicTypeOperations.go:91-97`).

### 4.2 Field order, and the one reorder hazard

`GoReflect.collectGoFields` re-sorts to Go declaration order via `reorderToGoDeclarationOrder`
(`FieldAccess.cs:394`), recovering the order from the **all-fields constructor's parameter names** —
a path that exists because promoted embeds perturb CLR order in converted code. A `TypeBuilder` type
emitting fields already in Go order makes the reorder a no-op whichever branch it takes. To keep that
true rather than incidental, increment 1 also emits the all-fields constructor with matching parameter
names whenever any field is embedded. Guarded by a behavioral test that synthesizes a struct with an
embedded field and asserts `Field(i).Name` order against `go run` (§9).

### 4.3 Dims, and why this is where the ArrayOf lane composes

`StructOf` receives `reflect.Type` field types, and a synthesized array type's **length is descriptor
cargo, not part of its CLR type** — `ArrayOf(1, int)` is `array<nint>` plus `arrayDims [1]`. The CLR
field type therefore cannot carry the length, and the only route by which `GoFields` recovers a
field's dims is `[GoArrayDims]` / `[GoMapKeyDims]` on the `FieldInfo`. So `StructOf` reads each field
type's descriptor cargo and re-stamps it onto the `FieldBuilder` via `SetCustomAttribute`.

For gob's `struct{ F [1][1]…[1]int }` that is one field of CLR type
`array<array<…<nint>>>` carrying `[GoArrayDims(1,1,…,1)]` — 101 dimensions. §5.1 measures that this
loads.

**Channel direction is the one cargo that does not travel this way.** `GoReflect.FieldChanDir`
(`TypeLayout.cs:441`) reads the direction off a *cached zero instance* of the declaring struct,
because the converter emits it as a field **initializer** (`= channel<@string>.SendOnly`) that the
generated parameterless constructor runs. `TypeBuilder` can emit such a constructor, so the route
exists; increment 1 does not take it, because no measured consumer synthesizes a directional-channel
field and the `chan-direction` disclosure class has already retired. Recorded here so it is not
re-discovered (OQ-8).

### 4.4 PkgPath for unexported fields — free, via nesting

`GoPackagePath(t)` is `GoPackageClassPath(t.DeclaringType)`, which derives the Go import path purely
from the declaring class's **namespace plus package-class name** (`GoReflect.TypeNaming.cs`). So a
synthesized type nested inside a synthesized container class named `<pkg>_package` in namespace
`go.<dir>.<...>` reports the caller's `PkgPath` with **no golib change at all** — one container per
distinct pkgpath, interned. Go itself requires all unexported fields of one `StructOf` call to share a
single pkgpath, so one container per type is exactly the right granularity (OQ-6).

### 4.5 Where the code lands

| File | Change |
|:--|:--|
| `src/go2cs/manualTypeOperations.go` | one entry, `"StructOf": goosAny`, in the `reflect` map |
| `src/core/golib/GoStructSynthesis.cs` *(new)* | the `AssemblyBuilder`/`ModuleBuilder`/`TypeBuilder` minting, the shape key, the pkgpath containers, the intern caches |
| `src/core/reflect/value_impl.cs` | `StructOf` — validate per Go's messages, resolve each field's CLR type + cargo, call the synthesizer, `synthType` → `toType` |
| `src/tests/Behavioral/ReflectStructOf` *(new)* | the guard (§9) |

Nothing in the converter's **emission** moves, so no corpus regen is owed for the arc itself; the
`reflect` package's own `type.cs` does change (the hand-own placeholder), which is a targeted
reconvert.

---

## 5. Measurements

All figures measured by this lane on the coordinator i7-5820K, **on `net9.0`** — this box has no .NET 10
SDK installed (`dotnet --list-sdks` tops out at 9.0.317), which is itself worth recording. §9 requires
re-measurement on a net10.0 host before implementation banks; nothing here depends on a .NET-10-only
behavior, but the numbers are the wrong TFM and are labelled so rather than quietly reused.

### 5.1 Cost per synthesized type — `TypeBuilder`

10,000 single-field synthetic structs, one `ModuleBuilder`, `TypeAttributes.SequentialLayout | Sealed`,
parent `ValueType`, `CreateType()` per type, then a field set/get round-trip on the last:

| mode | wall | per type | managed alloc | working-set delta | round-trip |
|:--|--:|--:|--:|--:|:--|
| `AssemblyBuilderAccess.Run` | 5,824 ms | **582 µs** | 10.9 MB | +24.1 MB | OK, `IsValueType=True` |
| `AssemblyBuilderAccess.RunAndCollect` | 5,410 ms | **541 µs** | 11.4 MB | **+8.5 MB** | OK, `IsValueType=True` |

Extrapolated: **gob's 101 struct types ≈ 59 ms** — irrelevant beside the suite's runtime.
**json's `BenchmarkTypeFieldsCache` at 10⁶ distinct types ≈ 9.7 minutes and ~2.4 GB** (`Run`), and
interning cannot help because its types differ by field name by construction. That is the mechanism's
ceiling, stated rather than discovered later (OQ-3).

### 5.2 The 101-deep composition loads

`MakeGenericType` + `Activator.CreateInstance` at **every** level of a nested generic `readonly struct`
(the shape `array<T>` is — `src/core/golib/array.cs:45`) reached depth **130 in 3 ms**; the depth-130
type's `FullName` is 10,820 characters. A `TypeBuilder` struct with a field of the 101-deep type built
and instantiated in under a millisecond. So neither the CLR's generic nesting nor `SequentialLayout`
over a deep field type is a wall — a question worth asking, since 101 levels of value-type
instantiation is not an ordinary shape.

### 5.3 Dynamic-code posture

`RuntimeFeature.IsDynamicCodeSupported` is `True` under the JIT, and under Native AOT it is `False` —
`DefineDynamicAssembly` would throw `PlatformNotSupportedException`. **This is not a new class for
go2cs.** golib already emits IL and compiles expression trees at run time in three places:

- `GoReflect.FieldAccess.cs:575` — `DynamicMethod` + `ILGenerator`, the `Ldflda` field-alias accessor **the reflect bridge's entire addressable-field path depends on**
- `ж.Contracts.cs:143` — `DynamicMethod` + `ILGenerator`, `FieldRef<T>.Create<TElem>`
- `GoReflect.MethodSets.cs:311` — `Expression.Lambda(...).Compile()`, under a file-scoped `#pragma warning disable IL3050`

So the reflect bridge is *already* outside a strict AOT profile, and `StructOf` widens the same
dependency rather than introducing one. What is genuinely unmeasured is whether the perf suite's
`PerfAot` profile (`src/tests/Performance/Directory.Build.targets:11`, `TrimMode=partial`) reaches
those sites today. That is a measurement, not an opinion (OQ-4).

---

## 6. The contract subset — and the line between scoping and fabricating

### 6.1 What increment 1 implements

Exported and unexported named fields; caller-supplied `PkgPath`; tags; `Anonymous` (embedded) fields
without methods; any field type the bridge can already name, including synthesized ones with dims
cargo; interning; `New`/`Field`/`Set`/`Interface` over the result.

### 6.2 What it panics on, with Go's own messages

Every validation Go performs (`type.cs:2075-2447`): no name, invalid name, no type, unexported without
`PkgPath`, anonymous with `PkgPath`, duplicate field, size overflow. Plus **embedded fields with
methods**, where Go's own `StructOf` documents no support and its implementation reaches the
`uncommonType` layout trick §1.2 rules out — panicking there matches Go's documented behavior rather
than diverging from it. Directional-channel field types are accepted but carry no direction (§4.3);
that is a *narrowing*, and it must be recorded in `ConversionStrategies-Reference.md` when it lands,
not left implicit.

### 6.3 Why this is scoping, not fabricating

The doctrine is the `host-limit` bar: an entry must name *"a structural property of the deployment
shape, never an unimplemented-but-fixable defect"*, and the charter's *"a disclosure is only for
asserts the CLR provably cannot satisfy"*. **Nothing in this design is a disclosure**, and the board
was explicit that gob's row is not disclosable — *"there is no class to disclose under"*. A priced arc
is never a disclosure, and this note prices an arc.

The scoping line, stated so it can be checked: **it is legitimate to implement a contract subset with a
loud panic at its boundary, and illegitimate to implement the shape of a test.** Concretely — a
`StructOf` that handled only single-field structs, or that recognized `[1][1]…int`, or that returned a
descriptor whose fields it could not actually store, would be fabrication even if the row went green.
The check is §3.1's honest-implementation test: gob's encoder must reach the synthetic type through
`GoFields`, `structLayoutOf` and `FieldAliasBox` — the same code paths a converted struct uses — so
that a green row proves the bridge and not a bypass.

### 6.4 Reversibility is structural

All CLR-type minting sits behind **one** function in `GoStructSynthesis.cs`:

```csharp
internal static Type SynthesizeStructType(ReadOnlySpan<GoSynthField> fields, string pkgPath)
```

Swapping mechanism A for mechanism C later is then one file's body plus the cargo widening, with the
hand-own, the interning, the validation and the guards untouched. That is what makes §5.1's ceiling a
deferred cost rather than a trap.

---

## 7. Interactions

### 7.1 The `reflect.ArrayOf` lane (`claude/reflect-arrayof`, concurrent)

**Composition, not conflict, and StructOf depends on it in one direction only.** `ArrayOf` mints
`synthType(typeof(array<>).MakeGenericType(elem), [n, …elemDims])`; `StructOf` consumes the result as a
field type and re-stamps its dims onto the `FieldBuilder` (§4.3). Two consequences worth agreeing
before either lands:

- `StructOf` reads a field type's dims through the **descriptor**, so `ArrayOf` must place them there and nowhere else. It already does.
- gob's row needs **both**, and neither flips it alone — the board said so and it is worth restating, because `ArrayOf` alone landing green invites the reading that gob is nearly banked. It is not: `TestIgnoreDepthLimit` wraps its array in a `StructOf` on the very next line, and its second half is 101 `StructOf` calls with no array at all.
- Merge order is immaterial, but **the merge result must be swept** — CLAUDE.md's banked-row protection rule, whose whole point is that each lane's proof binds its own tree. If both land, gob's row is measured at the union, not on either tip.

### 7.2 The zero-size-field LAYOUT arc (Ruling A, `sync/atomic`)

A synthesized struct's offsets come from `structLayoutOf` — the same memoized Go-amd64 walk that
answers `StructField.Offset` for every converted struct and that Ruling A made
`ж<T>.PointerOrderToken` alignment-truthful against (*"a token whose LOW BITS mirror the Go-computed
layout … answers `p & 7` from the SAME metadata that answers `Offset`"*). Under mechanism A a
synthesized struct **inherits that arc for free and forks nothing**, provided the emitted CLR field
order equals the Go field order (§4.2) and no field is elided.

One boundary to state rather than trip over: C# has no zero-size struct, so a synthesized field whose
Go type is `struct{}` occupies a CLR byte while `structLayoutOf` gives it Go's zero width. That
divergence is **pre-existing and already ruled** — it is exactly the `readonly`-emission remedy the
layout arc landed (*"the field stays DECLARED, so reflect's walk, `NumField()` and
`StructField.Offset` still match Go, while the one unfaithful operation becomes unexpressible"*).
Increment 1 emits such a field `initonly` for the same reason, and no measured consumer synthesizes
one.

### 7.3 `GoImplement` / witness machinery

A synthesized type has an **empty Go method set**, which is the correct Go answer: `reflect.StructOf`
does not support promoted methods of embedded fields, so no `StructOf` result has methods.

The question that matters is whether asking is *safe*, because gob asks about every type it sees —
`validUserType` tests `GobEncoder`, `GobDecoder`, `BinaryMarshaler`, `BinaryUnmarshaler` on the way in.
It is: `rtype.Implements` → `GoReflect.GoImplements` →
`ifaceType.IsAssignableFrom(valueType) || valueType.StructurallyImplements(ifaceType)`
(`GoReflect.ValueMarshalling.cs:132-141`), and the runtime tier is **fail-soft by design** —
`AdapterBinder` *"answers `false` for anything it cannot build … A MISS is normal control flow"*
(`AdapterBinder.cs:70-76`). A type no generator ever saw is precisely the case that tier exists for
(*"a dynamic type in an assembly converted AFTER the interface's own"*). So `Implements` answers
`false` without throwing, and `interface{}` — emitted as `object` — is satisfied by the special case
`GoImplements` already carries.

**This is a gate item, not an assumption.** A synthesized type is the first CLR type in the system
with *no* extension-method registration at all, and "answers false" is a claim to be measured (§9).

### 7.4 The `[GoType("dyn")]` machinery

The stamp is required for `Name()`/`String()` (§3.1), and there are exactly four other runtime readers
of `"dyn"`. Three are naming (`GoTypeName`, the interface arm, `HasGoName`). The fourth is
`Type.IsDynamicType()` (`golib/runtime/TypeExtensions.cs:163-169`), which gates **struct-to-struct
dynamic conversion** in `builtin.TryTypeAssert`. Stamping a synthesized type therefore also enrolls it
in that conversion — plausibly correct (a synthesized struct *is* a Go anonymous struct) but a side
effect of a stamp taken for a naming reason, which is the kind of coupling worth a ruling rather than
a shrug (OQ-7).

---

## 8. Recommendation and acceptance

**Take mechanism A**, scoped per §6, behind the single seam of §6.4.

**Acceptance measurements — the row math.** Roster today: **162 / 215 packages, 18,598 matching
verdicts, 85 disclosed** (`docs/ValidatedTestPackages.md`, 2026-08-25). `encoding/gob` measured
**105 of 106** on Go 1.23.1, one divergent row, zero disclosed; its 21 sources are byte-identical at
Go 1.23.12 (§1.4), so the denominator is unmoved. With `ArrayOf` and `StructOf` both landed:

| | before | after |
|:--|--:|--:|
| roster rows | 162 | **163** |
| matching verdicts | 18,598 | **18,704** (+106) |
| disclosed | 85 | **85** (unchanged — nothing here is a disclosure) |
| `encoding/gob` | 105 / 106, `failing` | **106 / 106, banks** |

The +106 is the whole suite, not the delta: a row's contribution is its verdict count, and gob's is 106
(98 top-level `Test` functions plus subtests; 19 `Example`/`Benchmark` declarations excluded by
Phase-4D). **This arithmetic is a projection and does not bank anything** — the measurement is the
pipeline run in §9, and if the re-measured denominator differs the projection loses, not the
measurement.

**Other consumers of StructOf, measured (§1.2):** there are none beyond gob on the frontier.
`encoding/json`'s single site is `BenchmarkTypeFieldsCache`, already banked at 491 verdicts and
unaffected because Phase-4D never runs it; reflect's own two callers are self-hosting layout tricks a
hand-own replaces. So **`StructOf` buys exactly one row today** — which is worth saying, because it
sets the honest expectation for an arc of this size and is an argument for landing `ArrayOf` first on
its own merits (it is reachable from user code and roughly free over the dims cargo).

---

## 9. Gates

Change class: **golib + hand-own + one converter-registry entry**. That is the widest class the repo
has, and the list is not negotiable downward.

1. Converter `go test ./...` from `src/go2cs` — includes `projitemsIntegrity_test` (the new golib file needs no registration, but a new converter file would) and `TestStdLibMetadataInSync`.
2. `GolibTests`, extended with: the shape-key intern test (`StructOf(f) == StructOf(f)`, and two shapes differing only in a tag are distinct); the field-order test with an embedded field (§4.2); the dims round-trip (`ArrayOf`-composed field → `Field(0).Type.Len()`); **the `Implements`-answers-false-without-throwing test (§7.3)**; and the `PkgPath` nesting test (§4.4).
3. A new behavioral test `ReflectStructOf`, compared against `go run` — the only gate that checks the answer against **Go** rather than against our own expectation. It must cover `New`/`Field`/`Set`/`Interface` round-tripping and `Type.String()`.
4. `check-no-regression.ps1` — full, expecting byte-identical `.cs` and `.csproj` across the behavioral corpus (nothing here changes emission).
5. Full behavioral suite via `run-behavioral.ps1`.
6. Full `src/go2cs.slnx` build — the only gate that compiles the non-generated solution members, owed by any golib API change.
7. Full `go2cs-stdlib.slnx` build at `-p:GoTargetOS=windows` **and** `-p:GoTargetOS=linux`, `--no-incremental`, with `bin`/`obj`/`Generated` purged between target switches.
8. **The reflect-bridge canary set** — the five largest banked reflect consumers **by verdict count, re-derived from `docs/ValidatedTestPackages.md` at gate time, never carried forward from this note**. (CLAUDE.md's rule; the escape it was written about happened precisely because a canary set predated the newest bank.)
9. The `encoding/gob` pipeline: `go2cs -tests -test-action all -test-timeout 10m "<GOROOT>/src/encoding/gob" src/core/encoding/gob`, expecting **106 / 106**. Pass the timeout explicitly — the hand-invoked default is 2m, five times smaller than the sweep's.
10. Full `run-validated-sweep.ps1` before banking, and — per CLAUDE.md's banked-row protection rule — a **post-merge filtered sweep of `encoding/gob` at the merge result**, not at the lane tip.
11. Re-measure §5.1 on a **net10.0** host (this box has no .NET 10 SDK), and measure the `PerfAot` profile question (OQ-4).
12. A **positive control** on the new guards before trusting any of them: regress one synthesized-field stamp deliberately, confirm the guard names exactly that site, restore, confirm byte-identical. A green that cannot go red is not a measurement.

Commit policy on a bank: gob's converted test sources join `src/core/encoding/gob` per the
validated-package commit policy, and the arc's decision is recorded in
`ConversionStrategies-Reference.md` (with the §6.2 narrowings named) in the same change.

---

## 10. Open questions — for coordinator ruling

- **OQ-1 — Staffing.** Does `StructOf` run as its own lane or fold into `claude/reflect-arrayof`? They compose (§7.1) and gob needs both, but `ArrayOf` is independently useful and an order smaller; folding risks the smaller fix waiting on the larger.
- **OQ-2 — `Run` vs `RunAndCollect`.** Measured 582 vs 541 µs/type and +24.1 vs +8.5 MB per 10k. Collectible is cheaper on both axes but implies synthesized types can be *released*, which no consumer requests and which interning actively prevents. Recommendation: `Run`, for the simpler lifetime story. Ruling wanted because it is hard to change after the first bank.
- **OQ-3 — The 10⁶ ceiling.** `BenchmarkTypeFieldsCache` would cost ≈9.7 min / ~2.4 GB under A. It is Phase-4D-deferred today, so nothing is owed. Does the coordinator want mechanism C (§3.3) designed now as the escape hatch, or is the §6.4 seam sufficient and the swap deferred until Phase 4D actually runs benchmarks?
- **OQ-4 — Native AOT.** golib already has three dynamic-code sites (§5.3), one of which the bridge's addressable-field path *requires*. Is the reflect bridge already outside the supported AOT profile — in which case `StructOf` adds no new class and owes no `[RequiresDynamicCode]` annotation — or does the `PerfAot` profile reach those sites today? This is a measurement (gate 11), and the answer changes whether an annotation arc is owed.
- **OQ-5 — Cache growth.** A synthesized type permanently occupies entries in five `ConcurrentDictionary<Type, …>` caches (`s_goFields`, `s_structLayouts`, `s_zeroInstances`, `s_descriptors`, `s_canonTypeCache`). Fine at 101; a leak at 10⁶. Must increment 1 bound them, or is that deferred with OQ-3?
- **OQ-6 — PkgPath mechanism.** §4.4's nested-container trick costs zero golib change but makes a synthesized type's *namespace* semantically load-bearing. The alternative is an explicit type-level pkgpath stamp read by a small `GoPackagePath` addition. Recommendation: the nesting, because it reuses the rule converted code already obeys rather than adding a second rule.
- **OQ-7 — The `dyn` stamp's side effect.** `[GoType("dyn")]` is required for correct naming but also enrolls the synthesized type in `Type.IsDynamicType()`'s struct-to-struct dynamic conversion (§7.4). Intended, or should the naming bit be separated from the conversion bit?
- **OQ-8 — Directional-channel fields.** Increment 1 accepts a `chan` field type but carries no direction (§4.3). The route exists (a `TypeBuilder`-emitted parameterless constructor with a field initializer). Narrow now and record it, or implement it since the mechanism is understood?

---

## 11. Adversarial review (charter §7) — against this note's own first draft

Five objections worth answering in place rather than in a later post-mortem.

**"`typelinks` is the blocker; implement it and every constructor works."** No. `typesByString` is one
step of `StructOf`, and everything after it reconstructs linker output the managed model does not have
(§2.3). This note's first framing treated the stub as the wall, which is the same misreading the board
corrected for `ArrayOf`.

**"A is the heavy option; B is the clean one."** B *looks* cleaner because it has no `AssemblyBuilder`.
It is the heavy option measured the way this repo has learned to measure: by how many places the
synthetic answer and the converted answer can disagree — ten (§3.2) against zero. The bridge's recorded
failure history is almost entirely of that kind (`rtype.String` answering `""` for every type;
`haveIdenticalUnderlyingType` returning `true` for any two structs; the reflectlite mirror drifting).
A green gob row under B would exercise only the synthetic path, which is exactly the shape of proof
this campaign has learned not to accept.

**"Reflection.Emit is an AOT regression."** Measured (§5.3): golib already depends on `DynamicMethod`
in the reflect bridge's addressable-field path, so the dependency exists and StructOf widens it. That
is a real thing to know and a smaller thing than the board's phrasing implied — but it is *not* a
dismissal, which is why OQ-4 asks for a measurement rather than asserting the answer.

**"101 levels of nested generic value types will not load."** A reasonable fear; measured false —
depth 130 in 3 ms (§5.2). This is recorded because it is the kind of question that otherwise gets
argued rather than run.

**"Scoping to a subset is how a row gets faked."** It is, if the subset is drawn around the test. §6.3
draws the line where the repo's doctrine draws it and gives the check — the row must go green through
the same `GoFields`/`structLayoutOf`/`FieldAliasBox` machinery a converted struct uses. Under
mechanism A there is no other machinery available, which is a stronger guarantee than a promise.

---

## 12. Deliberately not in scope

`SliceOf`, `MapOf`, `ChanOf` and `FuncOf` — all dead auto code on `typesByString` (§2.3), all with no
measured consumer, and all cheaper than `StructOf` once someone needs them (each is one
`MakeGenericType` plus cargo, the `PointerTo` shape). `MakeFunc`. Promoted methods on synthesized
embedded fields — Go does not support them either. Any change to the `internal/reflectlite` mirror: it
has no `StructOf` and a lane that needs one there owes its own measurement.

<!-- {% endraw %} -->
