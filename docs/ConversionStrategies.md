# Conversion Strategies

> **How `go2cs` turns each Go construct into C#, one short section per topic, with the Go and the C# it
> becomes side by side.** This page is for a Go developer reading converted code, or anyone evaluating
> go2cs. Each section ends with a link into the [reference](ConversionStrategies-Reference/README.md),
> which holds every emitted form, edge case and guard test, for maintainers.

The converted C# aims to be both **behaviorally** and **visually** similar to the Go it came from, so a Go
developer can read it and follow it. Two things make that possible: a hand-written runtime library,
[golib](#the-golib-runtime-library), and a set of Roslyn [source generators](#source-generators) that add
the members C# cannot spell directly.

> The C# snippets below are drawn from the actual converted standard library (`src/core/`,
> Go 1.24.13) wherever possible, paired with their original Go source; the rest come from the behavioral
> tests. Each code block carries an HTML comment naming the file and line it was copied from. The glyphs
> you will see, such as `ж`, `Ꮡ` and `Δ`, are listed in
> [Reading Converted Code](#reading-converted-code-names-and-glyphs).

---

## Contents

- **Start here:** [At a glance](#at-a-glance) · [Reading Converted Code: Names and Glyphs](#reading-converted-code-names-and-glyphs) · [The golib Runtime Library](#the-golib-runtime-library)
- **Packages & projects:** [Package Conversion](#package-conversion) · [Package-Level Variable Initialization Order](#package-level-variable-initialization-order) · [Converted Tests](#converted-tests) · [Compiled Library versus Source Code](#compiled-library-versus-source-code)
- **Numbers, constants & nil:** [Constant Values](#constant-values) · [Integer Types and Arithmetic](#integer-types-and-arithmetic) · [Named Numeric Types and Constant Contexts](#named-numeric-types-and-constant-contexts) · [Nil and Zero Values](#nil-and-zero-values) · [Built-in Functions](#built-in-functions) · [Empty Interface (`any`)](#empty-interface-any)
- **Assignment & scope:** [Multi-Assignment and Evaluation Order](#multi-assignment-and-evaluation-order) · [Short Variable Redeclaration (Shadowing)](#short-variable-redeclaration-shadowing) · [Multi-Result Values and Comma-Ok Forms](#multi-result-values-and-comma-ok-forms)
- **Composite types:** [Slices and Arrays](#slices-and-arrays) · [Strings (`@string` and `sstring`)](#strings-string-and-sstring) · [Maps](#maps) · [Generics](#generics) · [Type Aliasing](#type-aliasing)
- **Functions & control flow:** [Functions and Methods](#functions-and-methods) · [Function Values and Closures](#function-values-and-closures) · [Loops, Range and Labels](#loops-range-and-labels) · [Expression Switch Statements](#expression-switch-statements) · [Type Switch Statements](#type-switch-statements) · [Defer / Panic / Recover](#defer--panic--recover)
- **Concurrency:** [Goroutines](#goroutines) · [Channels and `select`](#channels-and-select)
- **Types & polymorphism:** [Struct Types](#struct-types) · [Struct Type Embedding](#struct-type-embedding) · [Interfaces](#interfaces) · [Reflection (`reflect`)](#reflection-reflect)
- **Pointers & memory:** [Pointers](#pointers) · [Implicit Pointer Dereferencing](#implicit-pointer-dereferencing) · [`unsafe.Pointer` and `uintptr`](#unsafepointer-and-uintptr)
- **The machinery:** [Source Generators](#source-generators) · [Functions Without a Go Body](#functions-without-a-go-body) · [Manually-Converted Declarations](#manually-converted-declarations) · [The standard library reproduces Go `-tags purego`](#the-standard-library-reproduces-go--tags-purego) · [Comments](#comments) · [Packages That Do Not Type-Check](#packages-that-do-not-type-check) · [Deterministic Output](#deterministic-output)

---

## At a glance

Each Go construct becomes a C# form a Go developer can read line by line. The table pairs each construct
with the form it takes, and its first column links to the section that explains it.

The last column, Provided by, names who supplies the C# form. The converter writes it, [golib](#the-golib-runtime-library)
implements it at run time, or a named [source generator](#source-generators) adds members at compile time.

The table uses a few glyphs. `ж`, `Ꮡ` and `~` are for pointers, `Δ` marks a renamed variable, and `@`
escapes a C# keyword. `ꟷ` and `ᐧ` pick comma-ok forms, `ᒐ` is the defer frame, and `goǃ` and `ᐸꟷ` are
for goroutines and channels. [Reading Converted Code](#reading-converted-code-names-and-glyphs) defines
each one.

<!-- sources, one per row, top to bottom (every C# form is copied from real emission):
  src/core/bufio/bufio.cs:17 · src/core/bufio/bufio.cs:14 ·
  src/tests/Behavioral/PackageVarInitOrder/registry.cs.target:22 + src/core/image/png/reader.cs:1227 ·
  src/core/reflect/all_test.cs:59 + src/core/reflect/go2cs_test_host.cs:1 ·
  src/tests/Behavioral/MapCommaOk/MapCommaOk.csproj:150 (ProjectReference; NuGet via -recurse=nuget) ·
  src/core/unicode/utf8/utf8.cs:23 + src/core/archive/zip/struct.cs:34 ·
  src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:17 + src/core/strconv/atoi.cs:145 ·
  src/core/golib/uintptr.cs:39 · src/core/bufio/bufio.csproj:131 (from src/go2cs/csproj-template.xml:116) ·
  src/core/encoding/gob/codec_test.cs:1156 ·
  src/core/golib/NilType.cs + src/tests/Behavioral/LambdaFunctions/LambdaFunctions.cs.target:62 ·
  src/tests/Behavioral/SliceAliasing/main.cs.target:8 · src/tests/Behavioral/MapCommaOk/MapCommaOk.csproj:104 ·
  src/tests/Behavioral/NamedReturnDefer/main.cs.target:41 · src/tests/Behavioral/ShadowedCompoundAssign/main.cs.target:10 ·
  src/core/strconv/atoi.cs:263 · src/tests/Behavioral/MapCommaOk/main.cs.target:12 ·
  src/tests/Behavioral/RangeStatements/RangeStatements.cs.target:20 · src/core/strconv/atoi.cs:260 ·
  src/tests/Behavioral/MapCommaOk/main.cs.target:11 · src/tests/Behavioral/GenericFuncDecl/GenericFuncDecl.cs.target:7 +
  src/tests/Behavioral/GenericInterfaceConstraint/GenericInterfaceConstraint.go:53 + GenericInterfaceConstraint.cs.target:53 ·
  src/tests/Behavioral/TypeConversionReturnType/TypeConversionReturnType.cs.target:1 ·
  src/tests/Behavioral/PackageVarInitOrder/registry.cs.target:14 · src/tests/Behavioral/LambdaFunctions/LambdaFunctions.cs.target:59 +
  src/tests/Behavioral/LocalFunctionEmission/main.cs.target:15 (local function only from a `name := func…` short declaration
  whose variable is only called; a `var f T = func…` stays a lambda, LambdaFunctions.go:49,52) ·
  src/tests/Behavioral/ForVariants/ForVariants.cs.target:53,59 ·
  src/tests/Behavioral/ExprSwitch/ExprSwitch.cs.target:101 · src/tests/Behavioral/TypeAssert/TypeAssert.cs.target:46,77 +
  src/tests/Behavioral/TypeSwitch/TypeSwitch.cs.target:52 · src/tests/Behavioral/DeferSimple/DeferSimple.cs.target:13-20 +
  src/tests/Behavioral/PanicRecover/PanicRecover.cs.target:26,49 · src/tests/Behavioral/SelectStatement/SelectStatement.cs.target:80 ·
  src/tests/Behavioral/SelectStatement/SelectStatement.cs.target:10,34,79,82 · src/tests/Behavioral/PackageVarInitOrder/registry.cs.target:5 ·
  src/tests/Behavioral/StructPromotion/StructPromotion.cs.target:30 · src/core/io/io.cs:86 ·
  src/tests/Behavioral/ReflectValueSingles/ReflectValueSingles.cs.target:56 + src/core/reflect/type.cs:1135 + src/core/golib/GoReflect.cs:19 ·
  src/tests/Behavioral/PointerToPointer/PointerToPointer.go:25 + PointerToPointer.cs.target:22,28 + src/core/golib/ж.cs:665 ·
  src/tests/Behavioral/ClosureSelfShadowCapture/main.cs.target:24 · src/tests/Behavioral/UnsafeOperations/UnsafeOperations.cs.target:80,93 ·
  src/core/math/dim_asm.cs:11 · src/core/unicode/utf8/utf8.cs:23 -->

| Section | Go | C# | Provided by |
|---|---|---|---|
| [Packages](#package-conversion) | `package bufio` | `partial class bufio_package` in `namespace go`; functions are `static` methods | converter |
| [Packages](#package-conversion) | `import "unicode/utf8"` | `using utf8 = unicode.utf8_package;` and a reference to that package's project | converter |
| [Initialization](#package-level-variable-initialization-order) | `var names = []string{…}` · `func init()` | a `static` field · `[GoInit] internal static void init()`, a .NET module initializer | converter |
| [Converted tests](#converted-tests) | `func TestBool(t *testing.T)` | a `static` method in a separate `<pkg>.tests` program, whose generated host runs each test | converter |
| [Library or source](#compiled-library-versus-source-code) | an imported package | a project reference, or a NuGet reference to the pre-converted standard library | converter |
| [Constants](#constant-values) | `const UTFMax = 4` · `Deflate uint16 = 8` | `public static UntypedInt UTFMax => 4;` · `public const uint16 Deflate = 8;` | converter |
| [Integers](#integer-types-and-arithmetic) | `int` · `uint` | `nint` · `nuint`, the C# native-sized integers | converter |
| [Integers](#integer-types-and-arithmetic) | `uintptr` | golib's `uintptr` struct | golib |
| [Integers](#integer-types-and-arithmetic) | `int32` · `rune` · `float64` · … | same-named aliases, declared in each project file, such as `<Using Include="System.Int32" Alias="rune" />` | converter |
| [Named numbers](#named-numeric-types-and-constant-contexts) | `type Float float64` | `[GoType("num:float64")] public partial struct Float;` | TypeGenerator |
| [Nil and zero](#nil-and-zero-values) | `nil` | `default!`, or golib's `nil` in a pointer comparison or pointer argument | golib |
| [Built-ins](#built-in-functions) | `len(s)` · `append(s, x)` · `make([]uint32, 6)` | `len(s)` · `append(s, x)` · `new slice<uint32>(6)`, the first two from golib's `builtin` class | golib |
| [`any`](#empty-interface-any) | `any` · `interface{}` | `any`, an alias for `object` declared in each project file | converter |
| [Multi-assignment](#multi-assignment-and-evaluation-order) | `a, b = b, a` | `(a, b) = (b, a);` | converter |
| [Shadowing](#short-variable-redeclaration-shadowing) | an inner `x := 5` | `nint xΔ1 = 5;` | converter |
| [Multiple results](#multi-result-values-and-comma-ok-forms) | `func Atoi(s string) (int, error)` | `public static (nint, error) Atoi(@string s)` | converter |
| [Comma-ok](#multi-result-values-and-comma-ok-forms) | `v, ok := m["a"]` | `var (v, ok) = m["a"u8, ꟷ];` | golib |
| [Slices and arrays](#slices-and-arrays) | `[]int{2, 3, 4}` · `[N]T` | `new nint[]{2, 3, 4}.slice()` · `array<T>` | golib |
| [Strings](#strings-string-and-sstring) | `string` · `"Atoi"` | `@string` · `"Atoi"u8` | golib |
| [Maps](#maps) | `map[string]int{"a": 1, "b": 2}` | `new map<@string, nint>{["a"u8] = 1, ["b"u8] = 2}` | golib |
| [Generics](#generics) | `func Swap[T any](a, b T) (T, T)` · `[S Shape]` | `public static (T, T) Swap<T>(T a, T b)` · `where S : Shape` | converter |
| [Type aliasing](#type-aliasing) | `type P = *bool` | `global using P = go.ж<bool>;` | converter |
| [Methods](#functions-and-methods) | `func (r *reg) add(name string) string` | `[GoRecv] internal static @string add(this ref reg r, @string name)`, plus a `ж<reg>` overload | RecvGenerator |
| [Closures](#function-values-and-closures) | `func() string { … }` as a value | a lambda typed `Func<@string>`, or a C# local function when a `name := func…` variable is only ever called | converter |
| [Loops](#loops-range-and-labels) | `for _, n := range nums` · `break scan` | `foreach (var (_, n) in nums)` · `goto break_scan;` | converter |
| [Switch](#expression-switch-statements) | `case 4, 5, 6:` | `case 4 or 5 or 6:` | converter |
| [Type switch](#type-switch-statements) | `i.(string)` · `s, ok := i.(string)` · `switch i.(type)` | `i._<@string>()` · `var (s, ok) = i._<@string>(ᐧ)` · `switch (i.type())` | golib |
| [Defer and panic](#defer--panic--recover) | `defer f()` · `panic(v)` · `recover()` | `defer(…, ref ᒐ)` inside `try`/`catch`/`finally` · `throw panic(v)` · `recover()` | golib |
| [Goroutines](#goroutines) | `go generate(ch)` | `goǃ(generate, …)` | golib |
| [Channels](#channels-and-select) | `make(chan int)` · `ch <- 12` · `<-ch` · `select` | `new channel<nint>(0)` · `ch.ᐸꟷ(12)` · `ᐸꟷ(ch)` · `switch (select(…))` | golib |
| [Structs](#struct-types) | `type reg struct { entries []string; … }` | `[GoType] partial struct reg { internal slice<@string> entries; … }` | TypeGenerator |
| [Embedding](#struct-type-embedding) | `type Record struct { Person; Employee }` | `public partial ref Person Person { get; }`, plus the promoted fields and methods | TypeGenerator |
| [Interfaces](#interfaces) | `type Reader interface { … }` | `[GoType] partial interface Reader`, plus the glue that lets each type used as a `Reader` implement it | ImplementGenerator |
| [Reflection](#reflection-reflect) | `reflect.TypeOf(want)` | `reflect.TypeOf(want)`, unchanged: the converted `reflect` package, backed by golib | converter |
| [Pointers](#pointers) | `*T` · `&x` · `*p` | `ж<T>` · `Ꮡx` · `~p` or `p.Value` | golib |
| [Implicit dereferencing](#implicit-pointer-dereferencing) | `s.val`, where `s` is a `*span` | `s.Value.val` | converter |
| [`unsafe`](#unsafepointer-and-uintptr) | `unsafe.Pointer` · `unsafe.Sizeof(x)` | `@unsafe.Pointer` · the constant `/* unsafe.Sizeof(x) */ 32` | converter |
| [No Go body](#functions-without-a-go-body) | `func archMax(x, y float64) float64` | `internal static partial float64 archMax(float64 x, float64 y);`, with a hand-written body or a throwing stub | PartialStubGenerator |
| [Comments](#comments) | `// maximum number of bytes …` | the same comment, in the same place | converter |

The machinery behind these forms has its own sections: [Source Generators](#source-generators),
[Manually-Converted Declarations](#manually-converted-declarations),
[the `purego` build](#the-standard-library-reproduces-go--tags-purego),
[Packages That Do Not Type-Check](#packages-that-do-not-type-check) and [Deterministic Output](#deterministic-output).

**Full detail:** [Reference → Contents](ConversionStrategies-Reference/README.md#contents) — one page per topic, with every emitted form, edge case and guard test.

---

## Reading Converted Code: Names and Glyphs

Converted C# keeps Go's names wherever it can: `bindAdd` stays `bindAdd`, and the entry point `main` becomes
`Main`. The naming rules below and the glyphs in the table are the exceptions. The glyphs mark the names and
helpers the converter adds; most are Unicode letters that C# accepts in identifiers and ordinary Go code does not use.
Go types such as `slice<T>` and `@string` come from golib: see [The golib Runtime Library](#the-golib-runtime-library).

<!-- sources, in row order (behavioral paths under src/tests/Behavioral/): GoCallVariations/GoCallVariations.cs.target:58; UnsafeOperations/UnsafeOperations.cs.target:26; GoCallVariations.cs.target:40 and UnsafeOperations.cs.target:26; GoCallVariations.cs.target:42; ReservedNameShadows/main.cs.target:39, src/core/time/time.cs:324, src/core/bufio/bufio_test.cs:11; ExprSwitch/ExprSwitch.cs.target:144, MultiFileInitOrder/a_first.cs.target:11; GoCallVariations.cs.target:25; src/core/errors/join.cs:19 (heap-moved value parameter: AddressOfParamWrite/main.cs.target:50); GoCallVariations.cs.target:21, ChannelRendezvous/main.cs.target:32, InitOrderTupleSpecs/main.cs.target:9; GoCallVariations.cs.target:9, src/core/strconv/atoi.cs:260; SStringTwinPilot/main.cs.target:42; src/core/encoding/json/package_info.cs:17;
src/core/errors/join.cs:7, ExprSwitch.cs.target:27 (also src/core/flag/flag.cs:1176), ChannelRendezvous/main.cs.target:33; AnyStringLitChanSend/main.cs.target:66, GoCallVariations.cs.target:39, ChannelRendezvous/main.cs.target:34; GoCallVariations.cs.target:21; ChannelCapLen/main.cs.target:28, AnonymousStructs/AnonymousStructs.cs.target:39; src/core/strconv/atoi.cs:293, ExprSwitch.cs.target:129, src/core/strings/iter.cs:57; ExprSwitch.cs.target:283; AppendOfMake/AppendOfMake.cs.target:76; src/core/errors/join.cs:39, CaptureHoistThroughConversion/main.cs.target:41;
DefinedTypeOverInterface/main.cs.target:10; GenericTypeNameCompanion/main.cs.target:24; GenericTypeInstantiation/GenericTypeInstantiation.cs.target:44; AdapterNameInterfaceCollision/main.cs.target:25; GoCallVariations.cs.target:46; GoCallVariations.cs.target:47; UnsafeOperations.cs.target:4 and 23; src/core/strings/strings.cs:19; src/core/strings/export_test.cs:8 and strings_test.cs:24.
appendꓸꓸꓸ: InterfaceCasting/InterfaceCasting.cs.target:323 (Funcꓸꓸꓸ: BlankIdentifierCollision/main.cs.target:88).
Buffer.Ꮡoff: PointerToPointer/PointerToPointer.cs.target:40; ᒐdone: NamedReturnDefer/main.cs.target:92; [GoRecv]: ReservedNameShadows/main.cs.target:61.
/*<-*/: SelectStatement/SelectStatement.cs.target:70; _<T>(): TypeAssert/TypeAssert.cs.target:77 (comma-ok: src/core/strconv/atoi.cs:293); ᴋ: src/core/internal/syscall/unix/linux/getrandom.cs:38; break_/continue_: ForVariants/ForVariants.cs.target:59 (goto break_scan;) and :63 (continue_scan:;); main_point: LiftedLocalTypes/main.cs.target:17.
Glyph constants: src/go2cs/symbols.go, generated from src/core/go2cs/symbols.json (DescriptorCarrierSuffix in src/go2cs/visitTypeSpec.go, DescriptorCompanionSuffix in src/go2cs/descriptorCompanion.go). Values of ᐧ, ᐧᐧ, ꟷ and ꓸꓸꓸ: src/core/golib/builtin.cs:160-196. The slice spread s.ꓸꓸꓸ: src/core/golib/slice.cs:490.
ᶠ is FuncValueMarker (symbols.json:172-183): only an sstring-twinned function has it, because a twinned function has no single method group; an ordinary function value stays a plain method group (GoCallVariations.cs.target:24).
_ΔpN: src/go2cs/visitFuncDecl.go:1226-1240 and :2416-2424 (a lone blank parameter stays `_`; `_Δp%d` only when blanks would collide); a lone one: CaptureHoistThroughConversion/main.cs.target:7 `public delegate void Handler(nint _);`; several: GenericTypeInstantiation.cs.target:44; interface method: AnonymousInterfaces.cs.target:51.
Other renames beyond `main` -> `Main`: a Go function named `Main` becomes `ΔMain` (identifierNaming.go getSanitizedFunctionName). -->

| Glyph or pattern | Meaning | Example | Explained in |
|---|---|---|---|
| `ж<T>`, `StandardBox<T>` | Go pointer `*T`: a golib heap box (read "zhe"); `StandardBox<T>` is the box class for an ordinary value | `ж<accum> Ꮡa`, `new StandardBox<Outer>(default(Outer))` | [Pointers](#pointers) |
| `Ꮡ` | Address-of `&x`; as a prefix, a variable that holds a box, or a field reference that `&s.f` uses | `Ꮡ(new accum(nil))`, `ᏑgOuter`, `Buffer.Ꮡoff` | [Pointers](#pointers) |
| `~p` | Reads through a pointer, and panics on nil like Go's `*p` | `(~acc).total` | [Implicit Pointer Dereferencing](#implicit-pointer-dereferencing) |
| `Δ` prefix | A name renamed to avoid a clash, including a package alias | `ΔGoFrame`, `ΔMonth`, `using Δio = io_package;` | The naming rules in this section |
| `Δ1` suffix | A second declaration of the same name: a variable that shadows an outer one, or a repeated `init` function | `hourΔ1`, `initΔ1` | [Shadowing](#short-variable-redeclaration-shadowing) |
| `ʗ1` suffix | A copy of a variable, taken for the lambda that uses it | `var f1ʗ1 = f1;` | [Function Values and Closures](#function-values-and-closures) |
| `ʗp` suffix | An incoming parameter the body redeclares: a variadic pack, or a value moved to the heap | `params ꓸꓸꓸerror errsʗp` | [Slices and Arrays](#slices-and-arrays) |
| `ᴛ` | A name the converter makes up: a temporary, a lambda parameter or an init helper | `ᴛ1`, `selᴛ2`, `initᴛsingle` | [Multi-Assignment](#multi-assignment-and-evaluation-order) |
| `ᴋ` | A temporary that keeps a pointer passed to a system call alive until the call returns | `var ᴋ0 = @unsafe.SliceData(p);` | [`unsafe.Pointer` and `uintptr`](#unsafepointer-and-uintptr) |
| `ˢ`, `ᶜ`, `ᶠ` suffix | A string literal or local constant stored once in a static field; `ᶠ` is the shared delegate used when a function with an `sstring` twin is taken as a value | `firstˢ`, `fnAtoiᶜ`, `fmt.Sprintfᶠ` | [Strings](#strings-string-and-sstring) |
| `ꓸ` | A dot inside one name, such as a package-qualified alias | `reflectꓸValue` | [Type Aliasing](#type-aliasing) |
| `ꓸꓸꓸ` | Go's `...`: `ꓸꓸꓸT` aliases `Span<T>` for a variadic parameter; `s.ꓸꓸꓸ` and `appendꓸꓸꓸ` spread a slice; `Funcꓸꓸꓸ<…>` is a variadic func type; a lone `ꓸꓸꓸ` marks a `select` case | `using ꓸꓸꓸerror = Span<error>;`, `fmt.Sprintf(format, a.ꓸꓸꓸ)`, `appendꓸꓸꓸ(all, batch)`, `ᐸꟷ(selᴛ2, ꓸꓸꓸ)` | [Slices and Arrays](#slices-and-arrays) |
| `ᐸꟷ`, `ꟷᐳ` | Channel send, receive, and receive inside `select` | `ch.ᐸꟷ(textˢ)`, `ᐸꟷ(done)`, `selᴛ2.ꟷᐳ(out var v)` | [Channels and `select`](#channels-and-select) |
| `/*<-*/` | A channel's direction, kept as a comment: `/*<-*/channel<T>` is `<-chan T`, `channel/*<-*/<T>` is `chan<- T` | `/*<-*/channel<nint> src` | [Channels and `select`](#channels-and-select) |
| `goǃ` | The `go` statement | `goǃ(ᴛ1 => fmt.Println(ᴛ1), firstˢ)` | [Goroutines](#goroutines) |
| `ꟷ`, `ᐧ` | golib's `const bool ꟷ = false` and `const bool ᐧ = true`: `ꟷ` picks a comma-ok form; `ᐧ` does so for a type assertion, and is the `true` of a tagless switch or endless loop | `var (v, ok) = ᐸꟷ(d, ꟷ);`, `seen[memo, ꟷ]`, `switch (ᐧ)`, `while (ᐧ)` | [Comma-Ok Forms](#multi-result-values-and-comma-ok-forms) |
| `ᐧᐧ` | A `true` that C# does not treat as a constant | `case {} when ᐧᐧ:` | [Expression Switch](#expression-switch-statements) |
| `._<T>()` | Type assertion `x.(T)`; with `ᐧ`, its comma-ok form | `i._<@string>()`, `err._<ж<NumError>>(ᐧ)` | [Empty Interface (`any`)](#empty-interface-any) |
| `ᒐ` | The `GoFrame` local that runs a function's deferred calls; names that start with `ᒐ`, such as the label `ᒐdone`, belong to the same frame | `GoFrame ᒐ = default;`, `ᒐdone: return (@out, label);` | [Defer / Panic / Recover](#defer--panic--recover) |
| `break_L`, `continue_L` | `goto` targets for Go's labeled `break L` and `continue L` | `goto break_scan;`, `continue_scan:;` | [Loops, Range and Labels](#loops-range-and-labels) |
| `XжI`, `XᴠI` | An adapter class: pointer `*X` (`ж`) or value `X` (`ᴠ`) as interface `I` | `new joinErrorжerror(e)`, `new HandlerᴠIface(…)` | [Interfaces](#interfaces) |
| `ᴅ`, `ᴺ` suffix | Keep a Go type name: `ᴅ` is an empty interface for a type converted to a C# alias; `ᴺ` is an extra type parameter for a type argument's name | `public interface Tokenᴅ { }`, `nameOf<T, Tᴺ>` | [Reflection](#reflection-reflect) |
| `_Δp0` | A made-up name for an unnamed Go parameter when a plain `_` will not do, such as several in one signature | `Seq2Like<K, V>(K _Δp0, V _Δp1)` | [Functions and Methods](#functions-and-methods) |
| `default!` | Go `nil` (golib `nil` in pointer contexts), and the zero value of a variable declared without a value | `return (n, default!);` | [Nil and Zero Values](#nil-and-zero-values) |
| `[GoType]`, `[GoRecv]` | Mark a converted Go type, or a pointer-receiver method; a source generator completes it | `[GoType] partial struct accum`, `[GoRecv] internal static nint len(this ref box b)` | [Source Generators](#source-generators) |
| `nint`, `nuint` | Go `int`, `uint` | `internal nint total;` | [Integer Types and Arithmetic](#integer-types-and-arithmetic) |
| `@name` | A C# keyword used as a name | `@in`, `@unsafe`, `@string` | The naming rules in this section |
| `<pkg>_package` | The static partial class that holds a package | `partial class strings_package` | [Package Conversion](#package-conversion) |
| `<func>_<type>` | A type declared inside a function, lifted to package scope | `main_point` | [Struct Types](#struct-types) |
| `<pkg>_internal_test_package`, `<pkg>_test_package` | The classes for in-package test files and for the external `_test` package | `strings_internal_test_package` | [Converted Tests](#converted-tests) |

**A C# keyword is escaped with `@`.** Go names such as `in`, `base` and `unsafe` are C# keywords. The `@`
prefix lets C# use them as names, so `import "unsafe"` becomes `using @unsafe = unsafe_package;`.

<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.go:21 -->
```go
type Outer struct {
	head byte
	in   Inner
}
```
<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.cs.target:21 -->
```csharp
[GoType] partial struct Outer {
    internal byte head;
    internal Inner @in;
}
```

**A name that would clash gets a `Δ` prefix.** The emitted code relies on golib names such as `GoFrame`,
`builtin` and `slice`, so a Go name that matches one is renamed. A Go type that shares its name with a method
is renamed the same way, for example time's `ΔMonth`.

<!-- reserved-name list: src/go2cs/identifierNaming.go:89-94; type-vs-method rename: src/core/time/time.cs:324 `[GoType("num:nint")] partial struct ΔMonth;` (Go type Month and method Time.Month) -->
<!-- source: src/tests/Behavioral/ReservedNameShadows/main.go:45 -->
```go
type GoFrame struct{ k int }
```
<!-- source: src/tests/Behavioral/ReservedNameShadows/main.cs.target:39 -->
```csharp
[GoType] partial struct ΔGoFrame {
    internal nint k;
}
```

**A capitalized Go name becomes `public`; any other name becomes `internal`.** This is Go's export rule in C#
terms. A method takes the stricter of its own name and its receiver type's name. A package-level struct or
interface type usually leaves its modifier to the [source generator](#source-generators), which adds it on its
own half of the `partial` type. A named function type becomes a delegate, which carries its own modifier.

<!-- receiver clamp: src/go2cs/visitFuncDecl.go:730-738 (receiverAccess from :1654-1671; getAccess at src/go2cs/identifierNaming.go:262); example src/core/errors/join.cs:46 `[GoRecv] internal static @string Error(this ref joinError e) {` -->
<!-- generator adds the modifier: src/gen/go2cs-gen/Templates/StructType/StructTypeTemplate.cs:55, Templates/InterfaceType/InterfaceTypeTemplate.cs:50 (`{{Scope}} partial struct/interface`). "usually": lifted local types carry `internal` (LiftedLocalTypes/main.cs.target:37) and an unexported type an exported field exposes carries `public` (PublicizedFieldType/main.cs.target:9); package-level declarations otherwise emit `[GoType] partial` bare (about 1150 of about 1280 [GoType] lines in the behavioral goldens). -->
<!-- func type as delegate carrying its own modifier: src/tests/Behavioral/CaptureHoistThroughConversion/main.cs.target:7 `public delegate void Handler(nint _);` (Go main.go:23 `type Handler func(int)`); GenericTypeInstantiation.cs.target:44 `public delegate bool Seq2Like<K, V>(K _Δp0, V _Δp1);` -->
<!-- source: src/tests/Behavioral/GoCallVariations/GoCallVariations.go:42 -->
```go
type accum struct{ total int }
…
func GetPrintLn() func(string) {
```
<!-- source: src/tests/Behavioral/GoCallVariations/GoCallVariations.cs.target:46 (GetPrintLn at :63; Go GetPrintLn at GoCallVariations.go:58) -->
```csharp
[GoType] partial struct accum {
    internal nint total;
}
…
public static Action<@string> GetPrintLn() {
```

<!-- The glyph table's intended reference home is a names-and-glyphs reference page (naming.md), which does not exist yet; until it lands, this section links shadowing.md, which is also the Shadowing section's reference page. -->
**Full detail:** [Reference → Shadowing the names go2cs itself spells](ConversionStrategies-Reference/shadowing.md#shadowing-the-names-go2cs-itself-spells-nil-golib-names-emitter-spelled-type-names-c-keywords) —
which Go names are `@`-escaped or `Δ`-renamed to avoid a clash with C# keywords or emitted names, and why.

---

<a id="the-gogolib-support-namespace"></a>

## The golib Runtime Library

Go's built-in types and behaviors become types and functions in [golib](../src/core/golib/), the
hand-written C# library behind every converted project. The project file go2cs writes references golib,
either as a project reference or as the NuGet package `go.lib`. Its names appear throughout converted
code, such as `slice<T>`, `@string`, `len` and `nil`.
[Reading Converted Code: Names and Glyphs](#reading-converted-code-names-and-glyphs) explains their glyphs.

**Go built-ins that .NET has no match for have golib counterparts.** This table points to the section that explains each one.

| Go | golib | Section |
|---|---|---|
| `[]T`, `[N]T` | `slice<T>`, `array<T>` | [Slices and Arrays](#slices-and-arrays) |
| `string` | `@string`, `sstring` | [Strings](#strings-string-and-sstring) |
| `map[K]V` | `map<K, V>` | [Maps](#maps) |
| `chan T` | `channel<T>` | [Channels and `select`](#channels-and-select) |
| `*T` | `ж<T>`, a heap box | [Pointers](#pointers) |
| `nil` | `nil`, a `NilType` value | [Nil and Zero Values](#nil-and-zero-values) |
| `error` | the `error` interface | [Interfaces](#interfaces) |
| `uintptr` | `uintptr` | [`unsafe.Pointer` and `uintptr`](#unsafepointer-and-uintptr) |
| `len`, `cap`, `append`, `copy`, `delete`, `close`, `clear`, `min`, `max` | methods of `builtin` | [Built-in Functions](#built-in-functions) |
| `defer`, `panic`, `recover` | `defer`, `panic`, `recover` in `builtin` | [Defer / Panic / Recover](#defer--panic--recover) |
| `go f(x)` | `goǃ(…)` in `builtin` | [Goroutines](#goroutines) |

**Built-in functions keep their Go names.** The project file go2cs writes imports golib's
[`builtin`](../src/core/golib/builtin.cs) class with `using static`, so a call such as `len(ch)` reads as
it does in Go ([Built-in Functions](#built-in-functions)).

<!-- source: src/tests/Behavioral/ChannelCapLen/main.go:11 -->
```go
ch := make(chan int, 3)
fmt.Println(len(ch), cap(ch))
```
<!-- source: src/tests/Behavioral/ChannelCapLen/main.cs.target:8 -->
```csharp
var ch = new channel<nint>(3);
fmt.Println(len(ch), cap(ch));
```

`nint` is Go's `int`, a native-width integer ([Integer Types and Arithmetic](#integer-types-and-arithmetic)).

**golib's core types sit in the `go` namespace.** Converted packages live in `go` or a namespace nested inside it
([Package Conversion](#package-conversion)), so converted code names `slice<T>` or `@string` with no `using`.

**An import alias takes the [`Δ`](#reading-converted-code-names-and-glyphs) rename mark when a namespace of the same
name would hide it.** Converting `io/fs` creates the namespace `go.io`. In namespace `go`, an alias named `io` would
lose to `go.io` in a package whose imports reach `io/fs`. There the alias is `using Δio = io_package;`, and uses read `Δio.EOF`.

**Some golib support types live in `go.golib`.** golib's runtime helpers, such as `SparseArray<T>`,
sit in this child namespace. No Go standard-library package is named `golib`, so this namespace never hides a
standard-library import alias. Converted code names these helpers through it, as in `golib.SparseArray<T>`, which
builds a slice from a keyed literal:

<!-- source: src/tests/Behavioral/IotaEnum/IotaEnum.go:62 -->
```go
var kindNames = []string{
	Invalid:       "invalid",
	…
}
```
<!-- source: src/tests/Behavioral/IotaEnum/IotaEnum.cs.target:59 -->
```csharp
internal static slice<@string> kindNames = new golib.SparseArray<@string>{
    [Invalid] = "invalid"u8,
    …
}.slice();
```

**Full detail:** [Reference → The go.golib support namespace](ConversionStrategies-Reference/golib-namespace.md#the-gogolib-support-namespace) — why golib's helpers avoid Go package names, exactly when an import alias takes the `Δ` rename mark, and how renamed types and aliases from other packages are spelled.

---

## Package Conversion

A Go package becomes one C# project holding a `static partial class` named `<name>_package`. Every Go
file in the package adds its code to that one class.

**Package-level functions become static methods of the package class.** Methods become C# extension
methods; [Functions and Methods](#functions-and-methods) shows how receivers map. The `ж<Reader>` result
is golib's heap box for Go's `*Reader`; see [Names and Glyphs](#reading-converted-code-names-and-glyphs).

<!-- source: GOROOT/src/bufio/bufio.go:62 -->
```go
func NewReader(rd io.Reader) *Reader {
	return NewReaderSize(rd, defaultBufSize)
}
```
<!-- source: src/core/bufio/bufio.cs:17 -->
```csharp
partial class bufio_package {
…
public static ж<Reader> NewReader(io.Reader rd) {
    return NewReaderSize(rd, defaultBufSize);
}
```

**An import becomes a `using` alias named for the package.** The import path's leading segments name a
namespace under the root `go`, which holds the package's class. So `unicode/utf8` is `utf8_package` in
`namespace go.unicode`. Its import also opens `unicode`, because C# finds extension methods by namespace.

<!-- source: GOROOT/src/bufio/bufio.go:10 -->
```go
import (
	…
	"io"
	…
	"unicode/utf8"
)
```
<!-- source: src/core/bufio/bufio.cs:12 -->
```csharp
using io = io_package;
…
using utf8 = unicode.utf8_package;
using unicode;
```

**Each package also gets a `package_info.cs`.** It sits at the package root, or in each per-OS folder
when the package has them. It declares the package class and holds package-wide `global using` aliases.

**A `main` package becomes an executable; every other package becomes a library.** The executable
project has `<OutputType>Exe</OutputType>`. Each imported package is its own library project, and the
importer references it.

**Imported packages initialize first, blank imports included.** A Go `init` becomes a method marked
`[GoInit]`, which .NET runs only when something in its assembly is first used. That can come late, and
for a blank import it never comes. So `package_info.cs` forces each import that has anything to
initialize, directly or through its own imports, ahead of the package's own `init`.

<!-- source: GOROOT/src/crypto/internal/fips140/aes/cast.go:7 -->
```go
import (
	"bytes"
	…
	_ "crypto/internal/fips140/check"
	…
)
```
<!-- source: src/core/crypto/internal/fips140/aes/package_info.cs:92 -->
```csharp
[GoInit] internal static void initᴛᴛimportꓸbytes() => builtin.initPackage(typeof(bytes_package));
…
[GoInit] internal static void initᴛᴛimportꓸcryptoꓸinternalꓸfips140ꓸcheck() => builtin.initPackage(typeof(go.crypto.@internal.fips140.check_package));
```

**Build constraints pick each target's Go files.** The converter selects Go files by their build
constraints and file-name suffixes, as `go build` does. Files that differ between Windows, Linux and
macOS sit in `windows/`, `linux/` and `darwin/` subfolders. The project compiles the flat files plus
the one folder named by the `GoTargetOS` build property, which defaults to `windows`:

<!-- source: src/core/os/os.csproj:161 -->
```xml
<Compile Include="*.cs" Exclude="package_info.cs" />
…
<Compile Include="$(GoTargetOS)/*.cs" Exclude="$(GoTargetOS)/package_info.cs" />
```

**Full detail:** [Reference → Package Conversion](ConversionStrategies-Reference/package-conversion.md#package-conversion) — project names and paths, cross-package references and NuGet use, exported type aliases, the import-initialization rules including blank imports and test projects, build-constraint file selection, the per-OS layout including hand-written files, and the generated solution files.

---

## Package-Level Variable Initialization Order

Go initializes package-level vars in dependency order. C# runs static field initializers in file order,
and in no defined order across files. Only a var that could read an unset value changes form.

**Such a var becomes a bare field plus an init method.** The method sits beside the field and is named
`initᴛ<name>`. Here `first` reads `base`, declared after it, so only `first` moves. The `ᴛ` marks a
generated name, and `@base` escapes the C# keyword `base` ([glyphs](#reading-converted-code-names-and-glyphs)).

<!-- source: src/tests/Behavioral/PackageVarInitOrder/main.go:8 -->
```go
var first = base + 1

var base = 41
```
<!-- source: src/tests/Behavioral/PackageVarInitOrder/main.cs.target:7 -->
```csharp
internal static nint first;
internal static void initᴛfirst() { first = @base + 1; }

internal static nint @base = 41;
```

**A dependency in another file moves the reader.** A var also moves when it reads a var declared later
in its own file, or a var that has itself moved. The read can be direct, or inside a function the
initializer calls.

In `syscall`, `procSetFilePointerEx` reads `modkernel32`, which is declared in another file, so it
moves (`ж<LazyProc>` is Go's `*LazyProc`; see [Pointers](#pointers)).

<!-- source: GOROOT src/syscall/syscall_windows.go:489 -->
```go
var procSetFilePointerEx = modkernel32.NewProc("SetFilePointerEx")
```
<!-- source: src/core/syscall/windows/syscall_windows.cs:493 -->
```csharp
internal static ж<LazyProc> procSetFilePointerEx;
internal static void initᴛprocSetFilePointerEx() { procSetFilePointerEx = modkernel32.NewProc("SetFilePointerEx"u8); }
```

**A generated `package_init.cs` calls the init methods in Go's dependency order.** It holds the package
class's static constructor. A package that moves nothing has no `package_init.cs`.

<!-- source: src/core/syscall/windows/package_init.cs:9 -->
```csharp
partial class syscall_package {
    static syscall_package() {
        initᴛprocSetFilePointerEx();
        …
    }
} // end syscall_package
```

This order is safe because C# runs every static field initializer, in every file, before the static
constructor body. Each moved initializer therefore finds its unmoved dependencies already set.

**A var that reads another package's var needs no ordering.** .NET initializes that package's class
before its field is first read.

**Full detail:** [Reference → Package-Level Variable Initialization Order](ConversionStrategies-Reference/variable-initialization-order.md#package-level-variable-initialization-order) — how dependencies are traced through called functions and func literals; the constants that count as dependencies; blank, addressed and tuple-deconstructing vars; test-conversion initializers; and the tests that guard each shape.

---

## Converted Tests

`go2cs -tests` converts a package's Go test suite along with its code. Each `x_test.go` becomes `x_test.cs` beside the production sources, and a file that holds only examples or benchmarks gets no `.cs`. A generated host program runs the tests on go2cs's hand-written [`testing`](../src/core/testing/) package, the way `go test` does.

**A test function keeps its Go shape.** `func TestX(t *testing.T)` becomes a `public static void` method that takes `ж<testing.T>`, golib's heap box standing in for Go's `*testing.T`. The parameter is named `Ꮡt`, with the address mark `Ꮡ`, and `@string` is Go's `string` (the [glyph table](#reading-converted-code-names-and-glyphs) lists all three). Calls such as `t.Errorf` go through that pointer, as `Ꮡt.Errorf`:

<!-- source: GOROOT/src/strings/clone_test.go:5 -->
```go
package strings_test
…
func TestClone(t *testing.T) {
	…
	for _, input := range cloneTests {
		clone := strings.Clone(input)
		if clone != input {
			t.Errorf("Clone(%q) = %q; want %q", input, clone, input)
		}
```
<!-- source: src/core/strings/clone_test.cs:11 -->
```csharp
partial class strings_test_package {
…
public static void TestClone(ж<testing.T> Ꮡt) {
    …
    foreach (var (_, input) in cloneTests) {
        @string clone = strings.Clone(input);
        if (clone != input) {
            Ꮡt.Errorf("Clone(%q) = %q; want %q"u8, input, clone, input);
        }
```

**Each kind of test file has its own class.** An external test package, `package strings_test`, becomes the class `strings_test_package`. A same-package test file normally goes to `<pkg>_internal_test_package`, a separate class that can still reach the package's unexported names.

External test files import the internal class with `using static`. So Go's `export_test.go` pattern needs no hand edits: a same-package file exposes internals, and the external tests use them.

**A generated host registers every runnable test.** `go2cs_test_host.cs` is the test program's `Main`. It registers each test by its Go name, its method, and its Go file and line. Tests are found at conversion time, not by reflection, so each `-json` result names the Go file and line where the test is declared:

<!-- source: src/core/container/list/go2cs_test_host.cs:6 -->
```csharp
internal static class Go2CsTestHost
{
    public static int Main(string[] args)
    {
        TestRegistry registry = new("container/list", new string[]
        …
        registry.Add("TestExtending", list_internal_test_package.TestExtending, "list_test.go", 159);
        …
        return TestHost.Run(registry, args);
    }
}
```

The host takes `go test`'s flags, such as `-run`, `-v`, `-count` and `-json`.

**Examples, benchmarks and fuzz targets convert but do not run.** Their methods still appear in any converted test file that also holds tests, as `BenchmarkClone` does in `clone_test.cs`. The host registers only tests and `TestMain`.

**A test project normally references the production project.** It does not recompile the production sources. Production types then keep one identity, shared with every other converted package that uses them. The production project grants the `.tests` assembly access to its internals, so same-package tests reach unexported names.

**Known differences from Go are listed beside the package.** A hand-written `go2cs_test_disclosures.json` names the tests whose C# result is known to differ from `go test`. Each entry gives its reason and, in most cases, the failure text the C# run must show. A test that fails in any other way still counts as a mismatch.

**Full detail:** [Reference → Test suites reference the production project](ConversionStrategies-Reference/shadowing.md#test-suites-reference-the-production-project-instead-of-recompiling-it) — the test-project models and when each applies, the internal bridge class and its metadata files, test-side name collisions, and exactly which test files get no `.cs`.

---

## Compiled Library versus Source Code

Go builds every package from source, so its compiler knows whether each pointer parameter escapes. go2cs
converts each Go package into its own C# library, as if its callers were compiled separately. As a result,
some values Go keeps on the stack become heap boxes, and the standard library can come from NuGet.

**An exported function's pointer parameter is a heap box.** It is a golib `ж<T>` box
([glyphs](#reading-converted-code-names-and-glyphs)) in every package, so the signature never depends on
the function body. A local whose address is passed to it lives in a box too, even where Go keeps it on
the stack.

<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.go:48 -->
```go
	b := Buffer{}
	PrintValPtr(&b.off)
	…
func PrintValPtr(ptr *int) {
	fmt.Printf("Value available at *ptr = %d\n", *ptr)
```
<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:38 -->
```csharp
    ref var b = ref heap<Buffer>(out var Ꮡb);
    b = new Buffer(nil);
    PrintValPtr(Ꮡb.of(Buffer.Ꮡoff));
    …
public static void PrintValPtr(ж<nint> Ꮡptr) {
    ref var ptr = ref Ꮡptr.DerefOrNull();
    …
```

`heap<Buffer>(out var Ꮡb)` allocates the box `Ꮡb` and returns a `ref` to its value, so `b` reads like a Go local.
`Ꮡb.of(Buffer.Ꮡoff)` is `&b.off`. In the callee, `ref var ptr` lets the body read like Go's `*ptr` ([Pointers](#pointers)).

**An unexported function can take a `ref` instead.** The converter sees all its callers, so a pointer
parameter that is only dereferenced usually becomes a C# `ref`. A local whose address reaches only such
parameters stays a plain local, as [Pointers](#pointers) shows.

**`go2cs -recurse=nuget` references the standard library as NuGet packages.** Each is `go.` plus its import
path, with dots for slashes. The program's own packages are converted from source. A program that imports
`fmt` also gets `go.lib`, the [golib runtime](#the-golib-runtime-library), and `go.gen`, the
[source generators](#source-generators):

<!-- expected app .csproj lines; emitted by src/go2cs/projectFileWriter.go:387,390,471 -->
<!-- source: src/go2cs/moduleConverter_integration_test.go:262 -->
```xml
<PackageReference Include="go.fmt" Version="$(GoStdLibVersion)" />
<PackageReference Include="go.lib" Version="$(GoStdLibVersion)" />
<PackageReference Include="go.gen" Version="$(GoStdLibVersion)" PrivateAssets="all" />
```

**`$(GoStdLibVersion)` defaults to the Go toolchain's release.** Restore takes its newest NuGet revision unless you
set the value. `-recurse=nuget` refuses a module on a Go language version the packages are not published for.

**Full detail:** [Reference → Compiled Library versus Source Code](ConversionStrategies-Reference/compiled-library-vs-source.md#compiled-library-versus-source-code) — why source availability shapes Go's escape analysis, and how the converter chooses between NuGet and source references.

---

## Constant Values

A typed numeric or boolean Go constant keeps its own type: it is a C# `const` where C# allows one, and
a get-only property otherwise, as for [named types](#named-numeric-types-and-constant-contexts). An
untyped numeric constant at package level is a get-only property of a golib wrapper:
[`UntypedInt`](../src/core/golib/UntypedInt.cs), [`UntypedFloat`](../src/core/golib/UntypedFloat.cs) or
[`UntypedComplex`](../src/core/golib/UntypedComplex.cs). Like an untyped Go constant, it takes its type
from context.

**An untyped constant is a property of its wrapper type.** `name => value` returns the value on each
read and never stores it, so the package can read it in any order. A `/* … */` comment keeps the Go
expression, and hex formatting is kept so masks stay recognizable:

<!-- source: GOROOT/src/compress/lzw/reader.go:39 -->
```go
const (
	maxWidth           = 12
	decoderInvalidCode = 0xffff
	flushBuffer        = 1 << maxWidth
)
```
<!-- source: src/core/compress/lzw/reader.cs:32 -->
```csharp
internal static UntypedInt maxWidth => 12;
internal static UntypedInt decoderInvalidCode => 0xffff;
internal static UntypedInt flushBuffer => /* 1 << maxWidth */ 4096;
```

**An untyped constant reads the same at its use site.** Each wrapper converts implicitly to the Go
integer and float types, and Go has already checked that the value fits. Here `r.last` is a `uint16`,
and no cast appears:

<!-- source: GOROOT/src/compress/lzw/reader.go:165 -->
```go
			r.last = decoderInvalidCode
```
<!-- source: src/core/compress/lzw/reader.cs:162 -->
```csharp
            r.last = decoderInvalidCode;
```

**A typed constant of a C# built-in type is a C# `const` when C# accepts its value as a constant.** An
untyped `bool` constant is a plain `const bool` as well. A `nint` or `nuint` constant must fit the
32-bit `int` range, and a wider value takes the property form. Go `int` is C# `nint` (see
[Reading Converted Code](#reading-converted-code-names-and-glyphs)):

<!-- source: GOROOT/src/os/file.go:80 -->
```go
	O_RDONLY int = syscall.O_RDONLY // open the file read-only.
```
<!-- source: src/core/os/windows/file.cs:86 -->
```csharp
public const nint O_RDONLY = /* syscall.O_RDONLY */ 0;             // open the file read-only.
```

**A constant group folds `iota` and implicit repetition to plain values.** Each name gets its own
declaration. Here the first keeps `/* iota */` as a reminder of where the numbering starts:

<!-- source: GOROOT/src/compress/lzw/reader.go:31 -->
```go
const (
	…
	LSB Order = iota
	…
	MSB
)
```
<!-- source: src/core/compress/lzw/reader.cs:29 -->
```csharp
public static Order LSB => /* iota */ 0;
public static Order MSB => 1;
```

**A local constant takes its type from its uses.** When every use agrees on one type, it is declared
at that type, as a C# `const` where C# allows one. Otherwise it keeps its wrapper type.

**A complex constant is a real complex value.** It is its real part plus its imaginary part, written
with golib's `.i()` suffix:

<!-- source: src/tests/Behavioral/ComplexConstContext/main.go:17 -->
```go
	cRational   = 5.5 + 1.5i          // …
…
const c64 complex64 = 1.5 + 2.5i
```
<!-- source: src/tests/Behavioral/ComplexConstContext/main.cs.target:9 -->
```csharp
internal static UntypedComplex cRational => /* 5.5 + 1.5i */ 5.5D + 1.5D.i();
…
internal static complex64 c64 => /* 1.5 + 2.5i */ 1.5F + 2.5F.i();
```

**Full detail:** [Reference → Constant Values](ConversionStrategies-Reference/constants.md#constant-values) — why a property rather than a field (and the string and big-integer forms that stay fields), the bare-`iota` form, the exact-float and complex rendering rules, when a local constant keeps its wrapper, `unchecked` casts for native-width values such as `^uintptr(0)`, and how an untyped constant takes its width at each use.

---

<a id="native-and-narrow-integer-types"></a>

## Integer Types and Arithmetic

Go's platform-sized `int` and `uint` become C#'s native-sized `nint` and `nuint`, and the fixed-width types
keep their Go names. Most arithmetic reads as in Go. Narrow arithmetic, unsigned negation, shifts and slice
bounds differ in C#, so there the converter adds a cast, a rewrite or a [golib](#the-golib-runtime-library) call.

**The fixed-width names are aliases.** `int8` through `uint64`, and `rune`, are aliases of the C# primitives
that each project file declares, such as `<Using Include="System.Int32" Alias="rune" />`. `byte` is C#'s own
`byte`, the same type as `uint8`.
<!-- src/core/bufio/bufio.csproj:131, from src/go2cs/csproj-template.xml:105-116 -->

<!-- source: src/tests/Behavioral/GoShiftSemantics/main.go:21 -->
```go
c := []uint{0, 1, 63, 64, 65, 200}
var u uint64 = 0x8000000000000001
```
<!-- source: src/tests/Behavioral/GoShiftSemantics/main.cs.target:14 -->
```csharp
var c = new nuint[]{0, 1, 63, 64, 65, 200}.slice();
uint64 u = 0x8000000000000001UL;
```

**`uintptr` is its own type.** It becomes golib's [`uintptr`](../src/core/golib/uintptr.cs), a struct that
holds one `nuint`. Go keeps `uint` and `uintptr` distinct, so `%T` and type switches tell them apart.
<!-- uintptr: a distinct golib struct, not an alias of nuint, so an alias cannot erase the uint/uintptr
     difference (src/core/golib/uintptr.cs:16-20, :39). Golden:
     src/tests/Behavioral/SwitchPointerSentinelCase/main.cs.target:89. -->

**Narrow arithmetic is cast back to its type.** Go computes `int8`, `uint8`, `int16` and `uint16` arithmetic
at that width, so `200 + 100` wraps to 44. C# promotes it to `int`, so the converter casts the result back
when it is passed, assigned, returned or compared. `int32` and wider types are not promoted, and C#
arithmetic is unchecked, so they wrap as in Go with no cast.

<!-- source: src/tests/Behavioral/NarrowArithmeticArg/main.go:28 -->
```go
var a, b uint8 = 200, 100
…
fmt.Println(takeU8(a + b)) // 300 wraps to 44
```
<!-- source: src/tests/Behavioral/NarrowArithmeticArg/main.cs.target:29 -->
```csharp
uint8 a = 200;
uint8 b = 100;
fmt.Println(takeU8((uint8)(a + b)));
```

**Unsigned negation subtracts from zero.** C#'s unary minus never keeps an unsigned type: it rejects `uint64`
and `nuint`, and widens smaller unsigned values to a signed type. Go's `-x` on an unsigned value of type `T`
becomes `((T)0 - x)`, which wraps the same way.
<!-- unsigned negation: src/go2cs/convUnaryExpr.go:1228-1234; golden
     src/tests/Behavioral/ShiftPrecedenceUnsigned/main.cs.target:19
     `fmt.Println((uint64)(z & ((uint64)0 - z)));` for Go `fmt.Println(z & -z)` (main.go:29).
     C# spec: unary minus on uint converts to long, on byte/ushort promotes to int, on ulong/nuint
     is a compile error. -->

**A shift count that can reach the width calls a golib helper.** Go gives 0 for a shift by the full width or
more, or -1 when a negative value is shifted right. C# masks the count, so a 64-bit value shifted by 64 comes
back unchanged. Such a count turns `>>` into `.Rsh(…)` and `<<` into `.Lsh(…)`, from
[`GoShift`](../src/core/golib/GoShift.cs).

**A count provably below the width keeps the native operator.** C#'s shift takes an `int` count, while Go
accepts any integer type, so the count is cast to `int`.

<!-- source: src/tests/Behavioral/GoShiftSemantics/main.go:25 -->
```go
for _, k := range c {
	fmt.Println(u>>k, u<<k)
}
…
fmt.Println(u >> (c[3] & 63)) // 64 & 63 = 0 -> u
```
<!-- source: src/tests/Behavioral/GoShiftSemantics/main.cs.target:16 -->
```csharp
foreach (var (_, k) in c) {
    fmt.Println(u.Rsh(k), u.Lsh(k));
}
…
fmt.Println((u >> (int)(((nuint)(c[3] & 63)))));
```

Division and remainder keep C#'s `/` and `%`. A zero divisor panics with Go's `integer divide by zero`, which
[`recover`](#defer--panic--recover) catches.

**Slice ranges cast their bounds to `int`**, because C#'s range syntax accepts only `int` ([Slices and Arrays](#slices-and-arrays)).

**Full detail:** [Reference → Native and Narrow Integer Types](ConversionStrategies-Reference/native-and-narrow-integers.md#native-and-narrow-integer-types) —
every narrowing context, the rules that prove a shift count in range, named-type shifts, and the literal edge cases.

---

## Named Numeric Types and Constant Contexts

A Go type over a numeric base, such as `type Duration int64`, becomes a C# `partial struct` marked
[`[GoType("num:<base>")]`](../src/core/golib/GoTypeAttribute.cs). The [`TypeGenerator`](#source-generators) gives it
its base's operators and conversions, so method bodies read almost line for line as in Go:

<!-- source: GOROOT/src/time/time.go:911 -->
```go
type Duration int64
…
func (d Duration) Seconds() float64 {
	sec := d / Second
	nsec := d % Second
	return float64(sec) + float64(nsec)/1e9
}
```
<!-- source: src/core/time/time.cs:910 -->
```csharp
[GoType("num:int64")] partial struct Duration;
…
public static float64 Seconds(this Duration d) {
    var sec = d / ΔSecond;
    var nsec = d % ΔSecond;
    return (float64)(int64)sec + (float64)(int64)nsec / 1e9D;
}
```

`ΔSecond` is Go's `Second` constant. It carries a `Δ` prefix because `Time` also has a `Second()` method.
C# cannot give two members of one class the same name (see [Names and Glyphs](#reading-converted-code-names-and-glyphs)).

**A conversion goes through the underlying type.** The struct declares its conversions against its exact underlying
type, plus untyped constants. So `float64(sec)` becomes `(float64)(int64)sec`, which gives the value Go's conversion gives.

**Constants of a named type become static properties typed with it.** They follow [Constant Values](#constant-values):
the value is folded, and an explicit Go expression stays beside it as a comment. Go's `int` base appears as
`num:nint` (see [Integer Types and Arithmetic](#integer-types-and-arithmetic)). `ΔMonth` takes its `Δ` for the same reason as `ΔSecond`.

<!-- source: GOROOT/src/time/time.go:320 -->
```go
type Month int

const (
	January Month = 1 + iota
	February
	…
)
```
<!-- source: src/core/time/time.cs:324 -->
```csharp
[GoType("num:nint")] partial struct ΔMonth;

public static ΔMonth January => /* 1 + iota */ 1;
public static ΔMonth February => 2;
…
```

**Operators keep the named type.** The struct has Go's arithmetic and comparison operators, plus bitwise and shift
operators on an integer base. Each arithmetic, bitwise and shift operator returns the named type, so `math/big`'s `Word`,
shifted right, is still a `Word`. C# shift counts are `int`, which is why the count gets an `(int)` cast:

<!-- source: GOROOT/src/math/big/arith.go:16 -->
```go
type Word uint
…
	c = x[len(z)-1] >> ŝ
```
<!-- source: src/core/math/big/arith.cs:16 -->
```csharp
[GoType("num:nuint")] partial struct Word;
…
    c = (x[len(z) - 1] >> (int)(ŝ));
```

<!-- source: src/core/runtime/mgcmark.cs:1394 (Go: GOROOT/src/runtime/mgcmark.go:1425) -->
**A constant takes the type Go gives it in context.** Where C# would bind a bare constant to a different operator or overload,
or to none, the converter casts it to Go's type. So `runtime`'s `min(n, maxObletBytes)` becomes `min(n, (uintptr)(maxObletBytes))`.

**Values print as Go prints them.** A folded constant keeps its named type, so `8 * time.Hour` prints
through `Duration`'s own `String` method as `8h0m0s`, not as a count of nanoseconds
([detail](ConversionStrategies-Reference/floating-point-formatting.md#a-folded-constant-of-a-named-type-carries-its-type-in-the-fold)).

**Full detail:** [Reference → Named Numeric Types and Constant Contexts](ConversionStrategies-Reference/named-numeric-types.md#named-numeric-types-and-constant-contexts) — how `++`/`--` are generated, conversions in both directions and between assemblies, the `(T)0 - x` form of unsigned unary minus, casts on `min`/`max` constant arguments, shift-width and bit-mask casts, constants wider than 64 bits, the `&^=` lowering, the operator sets of named boolean and complex types, and when a cast gets parentheses.

---

## Nil and Zero Values

Go's `nil` and zero values mostly become C#'s `default!`. A pointer compared with `nil` or passed `nil` uses
[golib](#the-golib-runtime-library)'s `nil`. Fixed-size arrays, some structs and directional channels are constructed.

**`nil` is `default!`, except in a written pointer comparison (`p == nil`) or a pointer argument, where it is
golib's `nil`.** `default!` is C#'s default value for the target type, and the `!` tells the compiler the null
is intended. A nil pointer that is returned, assigned or declared stays `default!`.

A Go pointer is a golib `ж<T>` box, and a pointer parameter takes the `Ꮡ` prefix
([glyph table](#reading-converted-code-names-and-glyphs)). A nil pointer to an array keeps the array's length,
as `ж<array<T>>.NilBoxOfDims(N)`. Other pointers follow the rule:

<!-- source: src/tests/Behavioral/NilPointerParamMethods/main.go:32 -->
```go
func checkArg(p *node, op string) error {
	if p == nil {
		return errNilArg
	}
	return nil
}
```
<!-- source: src/tests/Behavioral/NilPointerParamMethods/main.cs.target:16 -->
```csharp
internal static error checkArg(ж<node> Ꮡp, @string op) {
    if (Ꮡp == nil) {
        return errNilArg;
    }
    return default!;
}
```

**Slices, maps, channels and interfaces compare against `default!`.** Each golib type gives its nil value Go's
behavior. A slice's nil-ness is its representation, not its length, so `[]T{}` stays non-nil. A nil value of
each kind, pointers included, behaves like this:

| Nil value | What happens |
|---|---|
| slice | `len` and `cap` are 0, `append` works, and `s[0:0]` is still nil. |
| map | Reads return the zero value, `len` is 0, `range` is empty and `delete` does nothing. A write panics. |
| channel | A send or receive blocks, and in a `select` its case is never chosen. A nil directional channel keeps its direction, as `channel<T>.RecvOnly` or `channel<T>.SendOnly`. |
| pointer | Comparing it is safe, and a method can be called on it. Reading or writing through it panics with Go's `invalid memory address or nil pointer dereference`, which `recover` catches. |
| interface | `== nil` is true only when it holds nothing at all. |

**A nil pointer stored in an interface keeps its type.** In Go, `any((*int)(nil))` is not nil, and `%T`
prints `*int`. A pointer entering an interface value passes through golib's `OrTypedNil()`, which gives the
pointer type's shared nil instance:

<!-- source: src/tests/Behavioral/TypedNilInterface/TypedNilInterface.go:79 -->
```go
var dp *int
…
var dpi any = dp
```
<!-- source: src/tests/Behavioral/TypedNilInterface/TypedNilInterface.cs.target:101 -->
```csharp
ж<nint> dp = default!;
…
any dpi = dp.OrTypedNil();
```

**Fixed-size arrays, and structs that hold one or embed another type, are constructed.** The golib `array<T>`
for Go's `[N]T` keeps its length in the instance, so an unnamed `[N]T` with no initializer becomes `new(N)`.
C# `default` runs no constructor, so it skips field initializers and the box an embedded field lives in. A
struct with an array field becomes `new()`, one with an embedded type becomes `new(nil)`, and one of plain
fields keeps `default!`:

<!-- source: src/tests/Behavioral/ZeroValueStructVar/main.go:18 -->
```go
tbl  [8]int
…
var z holder
```
<!-- source: src/tests/Behavioral/ZeroValueStructVar/main.cs.target:9 -->
```csharp
internal array<nint> tbl = new(8);
…
holder z = new();
```

**Full detail:** [Reference → Nil and Zero Values](ConversionStrategies-Reference/nil-and-zero-values.md#nil-and-zero-values) — how nil and zero values behave for each golib type, typed-nil boxing at every interface boundary, the reflection read path, and pointer-to-interface assignment through selector fields.

---

## Built-in Functions

Most of Go's built-in functions keep their Go names. They are static methods of the
[golib](#the-golib-runtime-library) [`builtin`](../src/core/golib/builtin.cs) class. Every converted
project imports that class with a global `using static go.builtin`, so `len(x)` in C# reads as it does in Go.

| Go | C# | More in |
|---|---|---|
| `len(x)`, `cap(x)` | `len(x)`, `cap(x)`, both returning `nint` (Go's `int`) | [Slices and Arrays](#slices-and-arrays), [Strings](#strings-string-and-sstring) |
| `append(s, a, b)`; `append(s, t...)` for a slice `t` | `append(s, a, b)`; [`appendꓸꓸꓸ(s, t)`](#reading-converted-code-names-and-glyphs) | [Slices and Arrays](#slices-and-arrays) |
| `copy(dst, src)` | `copy(dst, src)` | [Slices and Arrays](#slices-and-arrays) |
| `clear(x)` | `clear(x)`: zeroes a slice's elements, empties a map | [Slices and Arrays](#slices-and-arrays), [Maps](#maps) |
| `make(T, …)` | a constructor: `new slice<T>(n)`, `new map<K, V>()`, `new channel<T>(n)` | [Slices and Arrays](#slices-and-arrays), [Maps](#maps), [Channels and `select`](#channels-and-select) |
| `new(T)` | usually `@new<T>()`, returning a [`ж<T>`](#reading-converted-code-names-and-glyphs) heap box; `@` escapes the C# keyword | [Pointers](#pointers) |
| `delete(m, k)` | `delete(m, k)` | [Maps](#maps) |
| `close(ch)` | `close(ch)` | [Channels and `select`](#channels-and-select) |
| `min(…)`, `max(…)` | `min(…)`, `max(…)` | — |
| `complex(r, i)`, `real(c)`, `imag(c)` | the same calls, over `complex64` or `complex128` | [Constant Values](#constant-values) |
| `print(…)`, `println(…)` | the same calls, writing to standard error as Go does | — |
| `panic(v)`, `recover()` | `throw panic(v)` (the `throw` tells C# the path ends); `recover()` | [Defer / Panic / Recover](#defer--panic--recover) |

**Most calls are unchanged.** The arguments and their order stay as Go wrote them, and a length is
an `nint`:

<!-- source: src/tests/Behavioral/MinMaxBuiltin/main.go:75 -->
```go
n := min(len(x), len(y))
```
<!-- source: src/tests/Behavioral/MinMaxBuiltin/main.cs.target:41 -->
```csharp
nint n = min(len(x), len(y));
```

**`make` becomes a constructor.** A slice, map or channel is a golib type, so `make` builds one with
`new` and passes its size arguments through. Inside a generic function, `make(S, n)` over a type
parameter calls golib's `make<S>(n)` instead.

<!-- source: src/tests/Behavioral/AppendOfMake/AppendOfMake.go:40 -->
```go
s := make([]int, 2, 4)
```
<!-- source: src/tests/Behavioral/AppendOfMake/AppendOfMake.cs.target:55 -->
```csharp
var s = new slice<nint>(2, 4);
```

**A slice spread has its own name.** `append(s, t...)` becomes `appendꓸꓸꓸ(s, t)`, where the `ꓸꓸꓸ`
glyph stands for Go's `...`. The whole slice `t` passes as one argument, as it does in Go.

<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.go:310 -->
```go
var all []sink
all = append(all, batch...)
```
<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.cs.target:322 -->
```csharp
slice<sink> all = default!;
all = appendꓸꓸꓸ(all, batch);
```

**A method named like a built-in makes the call `builtin.<name>`.** A converted
[method](#functions-and-methods) is a static member of the package class, so C# would bind a bare
`len(…)` to it. Naming the class keeps the call on Go's built-in
([detail](ConversionStrategies-Reference/shadowing.md#a-declaration-shadowing-a-built-in-makes-the-call-an-ordinary-call)):

<!-- source: src/tests/Behavioral/ReservedNameShadows/main.go:57 -->
```go
func (b *box) len() int { return len(b.items) + 1 }
```
<!-- source: src/tests/Behavioral/ReservedNameShadows/main.cs.target:61 -->
```csharp
[GoRecv] internal static nint len(this ref box b) {
    return builtin.len(b.items) + 1;
}
```

**Full detail:** [Reference → Slices and Arrays](ConversionStrategies-Reference/slices-and-arrays.md#slices-and-arrays) — how `make`, `append`, `copy` and `clear` build, grow and zero elements, the spread forms and the append-of-make form, and their edge cases.

---

## Empty Interface (`any`)

Go's empty interface, `interface{}` or `any`, becomes C# `any`: a global alias for `object` that every
converted project declares. A value is boxed with its Go type wherever it enters an `any`, so type
assertions, type switches and `==` see the dynamic type Go sees.

**A string literal boxes as a Go `string`.** A literal in an `any` slot is cast to golib
[`@string`](#reading-converted-code-names-and-glyphs), Go's string, so `x.(string)` and `case string:`
match it. A literal used only in `any` slots is [hoisted](#strings-string-and-sstring) once, into a
pre-boxed `object` field marked `ˢ`, such as `helloˢ`.

**An assertion reads the dynamic type back.** `x.(T)` becomes `x._<T>()`, a golib helper that returns
the value or panics with Go's `interface conversion` message. Comma-ok forms and type switches read
the same type ([comma-ok](#multi-result-values-and-comma-ok-forms), [type switches](#type-switch-statements)):

<!-- source: src/tests/Behavioral/TypeAssert/TypeAssert.go:60 -->
```go
var i interface{} = "hello"
…
s := i.(string)
```
<!-- source: src/tests/Behavioral/TypeAssert/TypeAssert.cs.target:76 -->
```csharp
any i = helloˢ;
@string s = i._<@string>();
```

**An untyped constant boxes at its Go default type.** Go's `int` is C# `nint`, so `7` in an `any` slot
is cast to `nint`. A bare C# `7` would box as `Int32`, and a later `.(int)` would panic:

<!-- source: src/tests/Behavioral/UntypedIntInterfaceBox/main.go:33 -->
```go
var a any = 7
fmt.Println(a.(int))
```
<!-- source: src/tests/Behavioral/UntypedIntInterfaceBox/main.cs.target:40 -->
```csharp
any a = (nint)(7);
fmt.Println(a._<nint>());
```

**`==` on interfaces compares dynamic type, then value**, through golib's `AreEqual` ([Interfaces](#interfaces)).
A test against `nil` stays a plain `== default!`.

**A pointer keeps its box.** Go stores the pointer itself in the interface, not the value it points to.
So when `keep` hands its `*pp` to `poolPut(x any)`, it passes the heap box itself,
[`Ꮡq`](#reading-converted-code-names-and-glyphs), of golib type `ж<pp>`.
`OrTypedNil()` keeps a nil pointer's [type](#nil-and-zero-values) ([detail](ConversionStrategies-Reference/pointers.md#a-pointer-value-passed-to-an-any-argument-takes-the-box)):

<!-- source: src/tests/Behavioral/PointerValueToInterfaceArg/main.go:45 -->
```go
func keep(q *pp) { poolPut(q) }
```
<!-- source: src/tests/Behavioral/PointerValueToInterfaceArg/main.cs.target:42 -->
```csharp
internal static void keep(ж<pp> Ꮡq) {
    poolPut(Ꮡq.OrTypedNil());
}
```

**Full detail:** [Reference → Empty Interface (`any`)](ConversionStrategies-Reference/empty-interface.md#empty-interface-any) — every position where a string literal or untyped constant is boxed, how named untyped constants are represented, and the exact rules for the uncomparable-type panic.

---

## Multi-Assignment and Evaluation Order

Go's parallel assignment becomes a C# tuple deconstruction. Both languages read every right-hand value before storing any, so the converted line reads like the Go one.

**Reassigning several variables is one tuple.** All targets are written only after every value is read. A `:=` that declares only new names cannot lose an old value, so it may stay as separate declarations.

<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.go:26 -->
```go
x, y := 0, 1
…
x, y = y, x+y
```
<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.cs.target:29 -->
```csharp
nint x = 0;
nint y = 1;
…
(x, y) = (y, x + y);
```

**A swap stays one statement, even for fields.** Written as two stores, the second would read the value the first just wrote. Fields, slice elements and pointer targets get the same tuple as plain locals. Here `inst` is a Go pointer, a golib `ж<T>`, and `.Value` reaches the struct it points to ([Pointers](#pointers)).

<!-- source: GOROOT/src/regexp/onepass.go:327 -->
```go
inst.Out, inst.Arg = inst.Arg, inst.Out
matchOut, matchArg = matchArg, matchOut
```
<!-- source: src/core/regexp/onepass.cs:355 -->
```csharp
(inst.Value.Out, inst.Value.Arg) = (inst.Value.Arg, inst.Value.Out);
(matchOut, matchArg) = (matchArg, matchOut);
```

**A `:=` that reuses a name declares only the new ones.** Go's `frac, e := normalize(frac)` assigns the existing `frac` and declares `e`. The converter declares each new element inside the tuple, as `var e` here, and leaves a reused one bare.

<!-- source: GOROOT/src/math/ldexp.go:30 -->
```go
frac, e := normalize(frac)
```
<!-- source: src/core/math/ldexp.cs:33 -->
```csharp
(frac, var e) = normalize(frac);
```

**A multi-value `return` runs its calls before it reads plain operands.** The Go compiler runs every function and method call in a return first and reads the plain operands after. A C# tuple reads strictly left to right. So when a call can change an earlier operand, the converter first moves the call into a temporary named with a `ᴛ` prefix.

<!-- source: GOROOT/src/crypto/x509/oid.go:28 -->
```go
func ParseOID(oid string) (OID, error) {
	var o OID
	return o, o.unmarshalOIDText(oid)
}
```
<!-- source: src/core/crypto/x509/oid.cs:27 -->
```csharp
public static (OID, error) ParseOID(@string oid) {
    OID o = default!;
    var ᴛ1 = o.unmarshalOIDText(oid);
    return (o, ᴛ1);
}
```

Without the temporary, `o` would be copied while still empty, and the parsed OID would be lost. `@string` is golib's Go `string` ([Strings](#strings-string-and-sstring)).

**Full detail:** [Reference → Multi-Assignment and Evaluation Order](ConversionStrategies-Reference/multi-assignment.md#multi-assignment-and-evaluation-order) — which targets count as reassignments, the read-after-write rule for a mixed `:=`, blank `_` elements, the element types that keep sequential stores, deconstruction into interface variables, and every case where a return moves its calls into temporaries.

---

## Short Variable Redeclaration (Shadowing)

Go lets a nested block declare a variable with a name its enclosing block also declares. C# forbids a local
from reusing an enclosing local's name, even one declared later, because C# scopes a local over its whole block.
So the converter renames the inner variable with a `Δ` suffix (`errΔ1`), and the outer one keeps its Go name.
The glyphs are listed in [Reading Converted Code: Names and Glyphs](#reading-converted-code-names-and-glyphs).

**Only the inner variable is renamed.** Every reference inside the inner scope uses the new name. The extra
braces around the C# `if` keep `n` scoped to the `if`, as in Go.

<!-- source: src/tests/Behavioral/NestedVarShadow/main.go:44 -->
```go
	if n := len(s); n >= 0 {
		v, err := check("")
		…
		_ = v
	}
	v, err := check(s)
```
<!-- source: src/tests/Behavioral/NestedVarShadow/main.cs.target:42 -->
```csharp
    {
        nint n = len(s); if (n >= 0) {
            var (vΔ1, errΔ1) = check(""u8);
            …
            _ = vΔ1;
        }
    }
    var (v, err) = check(s);
```

**A local named like a built-in the function calls is renamed, and the call keeps its name.** Go's
built-ins are methods of the [golib](#the-golib-runtime-library) [`builtin`](../src/core/golib/builtin.cs)
class, which converted code imports with `using static`. A C# local named `cap` would hide that method.

<!-- source: src/tests/Behavioral/BuiltinShadowLocal/main.go:29 -->
```go
func capPlusOne(s []int) int {
	cap := cap(s)
	return cap + 1
}
```
<!-- source: src/tests/Behavioral/BuiltinShadowLocal/main.cs.target:19 -->
```csharp
internal static nint capPlusOne(slice<nint> s) {
    nint capΔ1 = cap(s);
    return capΔ1 + 1;
}
```

**A local that shadows a package-level variable the function uses is renamed.** Go starts a local's scope
after its declaration, but C# scopes a local over its whole block. When the local sits directly in the
function body, each reference to the global also names the [package class](#package-conversion), here `main_package`.

<!-- source: src/tests/Behavioral/GlobalShadowedByLocal/main.go:31 -->
```go
func plainGlobalShadow() int {
	x := plainCounter * 2 // …
	plainCounter := 5     // local shadows the global
	return x + plainCounter // …
}
```
<!-- source: src/tests/Behavioral/GlobalShadowedByLocal/main.cs.target:29 -->
```csharp
internal static nint plainGlobalShadow() {
    nint x = main_package.plainCounter * 2;
    nint plainCounterΔ1 = 5;
    return x + plainCounterΔ1;
}
```

**Full detail:** [Reference → Short Variable Redeclaration (Shadowing)](ConversionStrategies-Reference/shadowing.md#short-variable-redeclaration-shadowing) — shadows of built-ins, packages and golib names, locals that shadow package-level constants, package-level names that collide with methods, and how renamed variables are captured by closures.

---

## Multi-Result Values and Comma-Ok Forms

A Go function with several results returns a C# value tuple, which the caller deconstructs with
`var (a, b) = f();`. Go's comma-ok forms call a second [golib](#the-golib-runtime-library) overload that
returns the same kind of tuple. Glyphs are listed in [Reading Converted Code](#reading-converted-code-names-and-glyphs).

**Named results keep their names.** A named result the body uses is a local, declared at the top with
its zero value. A bare `return` returns those locals' current values.

<!-- source: GOROOT/src/io/io.go:329 -->
```go
func ReadAtLeast(r Reader, buf []byte, min int) (n int, err error) {
	…
	return
}
```
<!-- source: src/core/io/io.cs:341 -->
```csharp
public static (nint n, error err) ReadAtLeast(Reader r, slice<byte> buf, nint min) {
    nint n = default!;
    error err = default!;
    …
    return (n, err);
}
```

**A multi-result call passed straight to another call goes through temporaries.** C# cannot pass one
tuple as several arguments, so `f(g())` deconstructs `g()` first. The `ᴛ` prefix marks a temporary:

<!-- source: src/tests/Behavioral/DeferFrameScopes/main.go:129 -->
```go
fmt.Println(classify(2))
```
<!-- source: src/tests/Behavioral/DeferFrameScopes/main.cs.target:177 -->
```csharp
var (ᴛ1, ᴛ2) = classify(2);
fmt.Println(ᴛ1, ᴛ2);
```

**A comma-ok form calls a second overload.** C# cannot overload on return type alone, so golib adds an
overload with one extra argument that returns `(value, ok)`. That argument is `ꟷ` or `ᐧ`, golib's
constants `false` and `true`, and it only selects the overload. `ᐸꟷ` is golib's receive function,
drawn to look like Go's `<-`.

| Go operation | Single value | Comma-ok |
|---|---|---|
| Map read `m[k]` | `m[k]` | `m[k, ꟷ]` |
| Channel receive `<-ch` | `ᐸꟷ(ch)` | `ᐸꟷ(ch, ꟷ)` |
| Type assertion `x.(T)` | `x._<T>()` | `x._<T>(ᐧ)` |

A failed single-value assertion panics as in Go. A failed comma-ok form returns the zero value and `false` instead.

**A comma-ok form in an `if` initializer keeps Go's scope** inside a C# `{ … }` block (`ж<T>` is golib's [heap box](#pointers), read through `.Value`):

<!-- source: GOROOT/src/strconv/atoi.go:273 -->
```go
if nerr, ok := err.(*NumError); ok {
	nerr.Func = fnAtoi
}
```
<!-- source: src/core/strconv/atoi.cs:292 -->
```csharp
{
    var (nerr, ok) = err._<ж<NumError>>(ᐧ); if (ok) {
        nerr.Value.Func = fnAtoi;
    }
}
```

**Full detail:** [Reference → Multi-Result Values and Comma-Ok Forms](ConversionStrategies-Reference/multi-result-and-comma-ok.md#multi-result-values-and-comma-ok-forms) — how a type assertion finds its target at run time, `var a, b = f()` at package level and in grouped declarations, when a named result is declared, and the rules for variadic parameters.

---

## Slices and Arrays

Go slices become [golib](#the-golib-runtime-library) [`slice<T>`](../src/core/golib/slice.cs): a struct
over a shared `T[]` backing array, with a start, a length and a capacity. Go arrays become
[`array<T>`](../src/core/golib/array.cs): a fixed-length array. Indexing, `len`, `cap`, `append` and
`copy` keep their Go names and read as they do in Go ([Built-in Functions](#built-in-functions)); a
`range` loop becomes a `foreach` over `(index, value)` pairs
([Loops, Range and Labels](#loops-range-and-labels)). Go's `int` appears as C# `nint`.

**Literals and `make`.** A positional composite literal builds a C# array and projects it with `.array()`
or `.slice()`. `make` calls a constructor. So `[]uint32{7, 8, 9}` becomes
`new uint32[]{7, 8, 9}.slice()`, and `make([]uint32, 6)` becomes `new slice<uint32>(6)`.

**A sub-slice shares its backing array.** `s[i:j]` becomes the C# range `s[i..j]`. A write through either
slice shows through the other, as in Go. (`base` is a C# keyword, so it appears as `@base`; see
[Names and Glyphs](#reading-converted-code-names-and-glyphs).)

<!-- source: src/tests/Behavioral/SliceAliasing/main.go:18 -->
```go
base := make([]uint32, 6)
d := base[2:5]
copy(d, []uint32{7, 8, 9})
…
d[0] = 42
base[3] = 43
```
<!-- source: src/tests/Behavioral/SliceAliasing/main.cs.target:8 -->
```csharp
var @base = new slice<uint32>(6);
var d = @base[2..5];
copy(d, new uint32[]{7, 8, 9}.slice());
…
d[0] = 42;
@base[3] = 43;
```

`append` writes into the shared backing while capacity allows, and reallocates when it runs out, as in Go.

**A slice range bound is cast to `int`.** C# range indices are `int`, but Go's `int` is `nint`. So a range
bound that is not an integer literal is cast: `p.items[:len(p.items)-1]` becomes
`p.items[..(int)(len(p.items) - 1)]`.
<!-- source: src/tests/Behavioral/GenericStructFields/GenericStructFields.go:86 and src/tests/Behavioral/GenericStructFields/GenericStructFields.cs.target:76 -->

**A three-index slice calls `.slice(low, high, max)`.** A C# range has no capacity bound, so
`base[1:3:4]` becomes `@base.slice(1, 3, 4)`. An omitted low bound is passed as `-1`.
<!-- source: src/tests/Behavioral/SliceAliasing/main.go:51 and src/tests/Behavioral/SliceAliasing/main.cs.target:29 -->
<!-- A three-index bound is cast only when it is wide or unsigned: `arr[:n:n]` over a `uintptr` n becomes `arr.slice(-1, (int)(n), (int)(n))` (src/tests/Behavioral/Slice3IndexWideBound/main.go:19 and main.cs.target:11); `nint` bounds such as `len(anys)` pass uncast (src/tests/Behavioral/AppendUntypedConst/main.cs.target:28). -->

**A nil slice is the default value.** `var zero []byte` becomes `slice<byte> zero = default!;`, and
`zero == nil` becomes `zero == default!`. An empty literal such as `[]byte{}` is not nil, as in Go
([Nil and Zero Values](#nil-and-zero-values)).
<!-- source: src/tests/Behavioral/SliceNilVsEmpty/main.go:15 and src/tests/Behavioral/SliceNilVsEmpty/main.cs.target:34 -->

**Arrays are values.** `array<T>` is a struct over a shared `T[]`, so a plain C# copy would share
elements. The converter adds `.Clone()` at Go's array copy sites: assignment, parameters, returns and
the other places Go copies a value. A `range` over an array with a value variable iterates a copy, as in
Go ([Loops, Range and Labels](#loops-range-and-labels)).
<!-- The array-copy claim is scoped to "Go's array copy sites": a fixed array reached only through an embedded (promoted) struct field is not seen by the value-clone stamping, so a by-value copy of such a struct leaves the array backing shared (src/go2cs/arrayCloneOperations.go:44-52, :80-83). -->

<!-- source: src/tests/Behavioral/ArrayPassByValue/ArrayPassByValue.go:53 -->
```go
d := garr
```
<!-- source: src/tests/Behavioral/ArrayPassByValue/ArrayPassByValue.cs.target:47 -->
```csharp
var d = garr.Clone();
```

**A struct with array fields copies them too.** A Go struct copy `c := d` becomes `var c = d.ΔClone();`.
A [source generator](#source-generators) writes `ΔClone()`. The `Δ` prefix keeps it from clashing with a
`Clone` method a Go type may declare.
<!-- source: src/tests/Behavioral/StructArrayFieldValueCopy/StructArrayFieldValueCopy.go:60 and src/tests/Behavioral/StructArrayFieldValueCopy/StructArrayFieldValueCopy.cs.target:66 -->

**A slice-to-array conversion copies.** `[4]byte(src)` becomes `new array<byte>(src, 4)`. The pointer form
`(*[4]byte)(dst)` becomes `Ꮡ(array<byte>.Alias(dst, 4))`, which aliases the slice instead. `Ꮡ` is go2cs's
address-of, Go's `&` ([Pointers](#pointers)).
<!-- source: src/tests/Behavioral/SliceToArrayPointerAlias/main.go:38 and src/tests/Behavioral/SliceToArrayPointerAlias/main.cs.target:16; src/tests/Behavioral/SliceToArrayPointerAlias/main.go:29 and src/tests/Behavioral/SliceToArrayPointerAlias/main.cs.target:8 -->

**Variadic parameters.** A Go `...T` parameter becomes `params ꓸꓸꓸT`. The glyph `ꓸꓸꓸ` stands for Go's
`...`, and `ꓸꓸꓸT` is a file-level alias for `Span<T>`. The raw pack keeps a `ʗp`-suffixed name
([Names and Glyphs](#reading-converted-code-names-and-glyphs)).
<!-- A type-parameter element type has no legal alias name and keeps `params Span<T>`. -->

When every use of the pack stays inside the call, the body binds the Go name to
[`sslice<T>`](../src/core/golib/sslice.cs), a stack-only view that allocates nothing. Otherwise it binds
`xsʗp.slice()`, a heap `slice<T>` copy.

<!-- source: src/tests/Behavioral/VariadicPackPassThrough/VariadicPackPassThrough.go:21 -->
```go
func sum(xs ...int) int {
```
<!-- source: src/tests/Behavioral/VariadicPackPassThrough/VariadicPackPassThrough.cs.target:23 -->
```csharp
internal static nint sum(params ꓸꓸꓸnint xsʗp) {
    var xs = xsʗp.sslice();
```

**A spread uses the `ꓸꓸꓸ` glyph.** `append(dst, xs...)` becomes `appendꓸꓸꓸ(dst, xs)`. `forward(a...)`
becomes `forward(a.ꓸꓸꓸ)`, which passes the slice's own storage as a span. When the callee binds the view,
a write to one of its elements reaches the caller's slice, as in Go. More on variadic parameters:
[Reference → Multi-Result Values and Comma-Ok Forms](ConversionStrategies-Reference/multi-result-and-comma-ok.md#multi-result-values-and-comma-ok-forms).
<!-- source: src/tests/Behavioral/VariadicPackPassThrough/VariadicPackPassThrough.go:31 and :63; src/tests/Behavioral/VariadicPackPassThrough/VariadicPackPassThrough.cs.target:36 and :106 -->
<!-- Known divergence, kept out of the summary body: a callee that binds `.slice()` (for example `keep`) works on a copy, so a spread of a caller's slice into it does not let its element writes or its returned slice reach the caller's storage as Go's would. VariadicPackPassThrough.go:44-48 records it as a pre-existing divergence that test does not claim to fix. -->

**Full detail:** [Reference → Slices and Arrays](ConversionStrategies-Reference/slices-and-arrays.md#slices-and-arrays) — named slice and array wrappers, keyed and sparse literals, declared-length array literals, zero-value element construction, every array clone site and deep copy, and nil-versus-empty identity.

---

## Strings (`@string` and `sstring`)

Go's `string` becomes golib [`@string`](../src/core/golib/string.cs): an immutable byte string. `len`,
indexing, comparison and concatenation work on bytes, as in Go, not on UTF-16 characters. Slicing is
cheap: `s[i:j]` is a window over the same bytes, not a copy
([detail](ConversionStrategies-Reference/strings.md#string-is-a-byte-string-and-slicing-it-is-a-window)).
A `range` loop becomes a `foreach` that yields each rune at its byte offset, as in Go.

**Most literals** render as C# UTF-8 literals, `"…"u8`, and become an `@string` only where a string value
is needed.

**Conversions to and from `[]byte` copy**, as they do in Go. `[]byte(s)` becomes a golib
[`slice<byte>`](#slices-and-arrays), and `string(b)` becomes `(@string)b`:

<!-- source: src/tests/Behavioral/StringLiteralSliceConversion/main.go:26 -->
```go
bs := []byte("hello")
…
fmt.Println(len(bs), string(bs))
```
<!-- source: src/tests/Behavioral/StringLiteralSliceConversion/main.cs.target:21 -->
```csharp
var bs = slice<byte>("hello"u8);
…
fmt.Println(len(bs), ((@string)bs));
```

**A literal that becomes a value is created once.** Go keeps literals in read-only memory, so they cost
nothing at run time. The converter moves such a literal into a `static readonly` field declared just
before the first function that uses it. The `ˢ` suffix marks the generated name:

<!-- source: src/tests/Behavioral/StringLiteralHoisting/main.go:12 -->
```go
func kind(n int) string {
	if n == 0 {
		return "zero value return"
	}

	return "other value return"
}
```
<!-- source: src/tests/Behavioral/StringLiteralHoisting/main.cs.target:8 -->
```csharp
// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string zeroValueReturnˢ = "zero value return"u8;
private static readonly @string otherValueReturnˢ = "other value return"u8;

internal static @string kind(nint n) {
    if (n == 0) {
        return zeroValueReturnˢ;
    }
    return otherValueReturnˢ;
}
```

A literal stays inline where it is already free or a field would not help. That covers comparisons,
concatenations, format strings and a few other places.

**A string constant declared inside a function** gets the same treatment. Its field takes the constant's
own name with a `ᶜ` suffix: `const fnAtoi = "Atoi"` becomes the field `fnAtoiᶜ`. The local `fnAtoi`
copies the field, which allocates nothing.

**Named string types** (`type version string`) are wrapper structs that keep the full string surface:
indexing, slicing, `len`, comparison, `+` and the type's own methods. A concatenation keeps the named type:

<!-- source: src/tests/Behavioral/NamedStringConcat/main.go:13 -->
```go
type version string
…
func bump(v version) version { return v + "-next" }
```
<!-- source: src/tests/Behavioral/NamedStringConcat/main.cs.target:7 -->
```csharp
[GoType("@string")] partial struct version;
…
internal static version bump(version v) {
    return v + "-next"u8;
}
```

**`sstring` is a string view that allocates nothing.** Golib's [`sstring`](../src/core/golib/sstring.cs)
is a stack-only `ref struct` over a span of bytes. C# rejects at compile time any code that stores a
`ref struct` in a field, array or map, boxes it, or captures it in a lambda. `sstring` appears in two
places:

- **A `string([]byte)` conversion that does not escape.** Go skips the copy when the string is only read
  while its bytes cannot change, and the converter does the same where it can prove it:

  <!-- source: src/tests/Behavioral/SStringElision/main.go:288 -->
  ```go
  return string(buf[3:6]) == " /x"
  ```
  <!-- source: src/tests/Behavioral/SStringElision/main.cs.target:224 -->
  ```csharp
  return ((sstring)(buf[3..6])) == " /x"u8;
  ```

- **String parameters of selected functions** (the sstring *twin*). Such a function, for example
  `fmt.Sprintf`, takes its string as an `sstring`, so a literal argument binds with no copy:

  <!-- source: src/tests/Behavioral/SStringTwinPilot/main.go:19 -->
  ```go
  fmt.Println(fmt.Sprintf("xxx"))
  ```
  <!-- source: src/tests/Behavioral/SStringTwinPilot/main.cs.target:35 -->
  ```csharp
  fmt.Println(fmt.Sprintf("xxx"u8));
  ```

  The callee's signature takes the `sstring`:

  <!-- source: src/core/fmt/print.cs:268 -->
  ```csharp
  [GoStr] public static @string Sprintf(sstring format, params ꓸꓸꓸany aʗp) {
  ```

  The [source generators](#source-generators) add an `@string` overload that forwards to it. Where Go
  uses the function as a value, the converter names one shared delegate, `Sprintfᶠ`. The `ꓸꓸꓸ`, `ʗ` and
  `ᶠ` glyphs are explained in [Names and Glyphs](#reading-converted-code-names-and-glyphs).

**Full detail:** [Reference → Strings (`@string` and `sstring`)](ConversionStrategies-Reference/strings.md#strings-string-and-sstring) — windows and conversions, literal rendering and byte-array literals, the exact hoisting rules, named-string wrappers, `sstring` eligibility, and how functions are chosen to take `sstring`.

---

<a id="maps-and-channels"></a>
## Maps

Go's `map[K]V` becomes golib [`map<K, V>`](../src/core/golib/map.cs), a small struct over a .NET
`Dictionary` whose copies share one store, as in Go. `len`, `delete` and `clear` read as they do in Go,
and the examples use the [names and glyphs](#reading-converted-code-names-and-glyphs) of converted code.

**A literal becomes an index initializer, and comma-ok adds the `ꟷ` sentinel.** The sentinel is a
second index that asks for the `ok` result, as described in
[Multi-Result Values and Comma-Ok Forms](#multi-result-values-and-comma-ok-forms):

<!-- source: src/tests/Behavioral/MapCommaOk/main.go:15 -->
```go
m := map[string]int{"a": 1, "b": 2}
…
v, ok := m["a"]
```
<!-- source: src/tests/Behavioral/MapCommaOk/main.cs.target:11 -->
```csharp
var m = new map<@string, nint>{["a"u8] = 1, ["b"u8] = 2};
var (v, ok) = m["a"u8, ꟷ];
```

**`range` becomes a `foreach` over key-value pairs.** The body may add and delete entries of the map
it walks, as Go allows. The order is unspecified, as in Go:

<!-- source: src/tests/Behavioral/MapMutateDuringRange/main.go:42 -->
```go
for k, v := range insert {
	if len(k) == 1 {
		visited++
		insert[k+"!"] = v * 10
	}
}
```
<!-- source: src/tests/Behavioral/MapMutateDuringRange/main.cs.target:37 -->
```csharp
foreach (var (k, v) in insert) {
    if (len(k) == 1) {
        visited++;
        insert[k + "!"u8] = v * 10;
    }
}
```

**A nil map reads as empty and panics on write.** A nil map is `default!`, the C# spelling of
[Go's nil](#nil-and-zero-values). Reading it yields the zero value, and `len` is 0. A write panics
with Go's message, "assignment to entry in nil map".

**A named map type is a wrapper struct.** The converter declares a `[GoType]` partial struct, and a
[source generator](#source-generators) fills in the full map surface. Its literal wraps a plain map
literal, and `make` passes the size hint to the constructor:

<!-- source: src/tests/Behavioral/EmptyStructMapSet/EmptyStructMapSet.go:37 -->
```go
type registry map[uint32]entry
…
reg := registry{2: {tag: "leaf", size: 8}}
…
reg2 := make(registry, 4)
```
<!-- source: src/tests/Behavioral/EmptyStructMapSet/EmptyStructMapSet.cs.target:21 -->
```csharp
[GoType("map[uint32, entry]")] partial struct registry;
…
var reg = new registry(new map<uint32, entry>{[2] = new(tag: "leaf"u8, size: 8)});
…
var reg2 = new registry(4);
```

**A lookup keyed by `string(b)` does not copy the bytes.** A map read never keeps its key, so Go
skips the `[]byte`-to-string copy there. Golib's `tmpstring(b)` does the same by viewing the slice's
bytes. A store keeps its key, so it still copies with a plain `(@string)` conversion:

<!-- source: src/tests/Behavioral/MapStringBytesLookup/main.go:31 -->
```go
v, ok := interned[string(b)]
…
w[string(k)] = 42
```
<!-- source: src/tests/Behavioral/MapStringBytesLookup/main.cs.target:20 -->
```csharp
var (v, ok) = interned[tmpstring(b), ꟷ];
…
w[((@string)k)] = 42;
```

**Full detail:** [Reference → Maps and Channels](ConversionStrategies-Reference/maps-and-channels.md#maps-and-channels) — how a range survives changes to its own map (NaN keys included), the nil-key slot, the Go-equality key comparer and its limits, exactly which `string(b)` lookups skip the copy, named map types, and map access through type parameters.

---

<a id="generic-constraints"></a>
## Generics

Go type parameters become C# generic type parameters, and Go's square brackets become angle brackets.
A constraint that C# can express becomes a `where` clause on the parameter. `any` and `comparable` add
none. Attributes such as `[GoType]` are listed in
[Reading Converted Code](#reading-converted-code-names-and-glyphs), and golib types such as `slice<T>`
in [The golib Runtime Library](#the-golib-runtime-library).

**An operator type set becomes `System.Numerics` operator interfaces**, so `+` and `<` compile
on the type parameter. The `where` clause lists those interfaces and names the Go constraint in a
comment. Each such clause also ends in `new()`.

<!-- source: GOROOT src/cmp/cmp.go:28 -->
```go
func Less[T Ordered](x, y T) bool {
```
<!-- source: src/core/cmp/cmp.cs:29 -->
```csharp
public static bool Less<T>(T x, T y)
    where T : /* Ordered */ IAdditionOperators<T, T, T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>, new()
```

**A method-set interface constraint becomes a plain `where` clause** that names the converted
interface. Go's `func totalArea[S Shape](shapes []S)` gets `where S : Shape`.
<!-- provenance for the inline example: src/tests/Behavioral/GenericInterfaceConstraint/GenericInterfaceConstraint.go:53 and GenericInterfaceConstraint.cs.target:52-53 -->

**A generic type keeps its type parameters, and each method repeats them.** A method becomes a generic
extension method on the type, and a pointer receiver takes `[GoRecv] this ref`, as described in
[Functions and Methods](#functions-and-methods). An extension method cannot borrow its receiver type's
parameters. So each one declares `<T>` and restates the constraint:

<!-- source: src/tests/Behavioral/GenericTypeInstantiation/GenericTypeInstantiation.go:6 -->
```go
type Stack[T ~int | ~string] struct {
…
func (s *Stack[T]) Push(element T) {
```
<!-- source: src/tests/Behavioral/GenericTypeInstantiation/GenericTypeInstantiation.cs.target:7 -->
```csharp
[GoType] partial struct Stack<T>
    where T : /* ~int | ~string */ IAdditionOperators<T, T, T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>, new()
…
[GoRecv] public static void Push<T>(this ref Stack<T> s, T element)
    where T : /* ~int | ~string */ IAdditionOperators<T, T, T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>, new()
```

**Type arguments are written out when Go writes them or C# cannot infer them.** Go's
`describe[fmt.Stringer]` becomes `describe<fmt.Stringer>`. Go also infers a type parameter that
appears only in a constraint, such as `E` in `Sort[S ~[]E, E cmp.Ordered](x S)`. C# never does, so
that call spells its type arguments, while calls C# can infer stay bare:
<!-- provenance for describe: src/tests/Behavioral/GenericTypeInstantiation/GenericTypeInstantiation.go:76 and GenericTypeInstantiation.cs.target:77 -->

<!-- source: GOROOT src/slices/iter.go:63 -->
```go
func Sorted[E cmp.Ordered](seq iter.Seq[E]) []E {
	s := Collect(seq)
	Sort(s)
```
<!-- source: src/core/slices/iter.cs:70 -->
```csharp
public static slice<E> Sorted<E>(iter.Seq<E> seq)
…
    var s = Collect(seq);
    Sort<slice<E>, E>(s);
```

**A slice type set becomes the golib interface for that shape, plus helpers.** `~[]E` becomes
[golib](#the-golib-runtime-library)'s `ISlice<E>`, the interface every slice type implements.
`ISupportMake<S>` lets `make(S, n)` build an `S`. `ISliceWrap<S, E>` lets `s[i:j]` and `append`
return `S` again, as they do in Go.

<!-- source: GOROOT src/slices/slices.go:96 -->
```go
func Index[S ~[]E, E comparable](s S, v E) int {
	for i := range s {
		if v == s[i] {
	…
```
<!-- source: src/core/slices/slices.cs:112 -->
```csharp
public static nint Index<S, E>(S s, E v)
    where S : /* ~[]E */ ISlice<E>, ISupportMake<S>, ISliceWrap<S, E>, new()
{
    foreach (var (i, _) in s) {
        if (AreEqual(v, s[i])) {
    …
```

**`comparable` adds no clause, and `==` on type-parameter operands calls `AreEqual`.** No C# constraint
admits every type Go can compare with `==`. Go's checker has already validated each instantiation, so
the parameter stays unconstrained and golib's `AreEqual` does the comparison. This holds under every
constraint, so an `Ordered` value's `x != x` becomes `!AreEqual(x, x)`.
<!-- provenance for the Ordered case: src/core/cmp/cmp.cs:71 -->

**Conversions to or from an integer type parameter go through golib**, because C# has no numeric cast
to or from a type parameter. Go's `Int(uint64(n) / 2)` becomes `ConvertToType<Int>(ConvertToUInt64<Int>(n) / 2)`.
<!-- provenance for the inline example: src/tests/Behavioral/GenericTypeInference/GenericTypeInference.go:247 and GenericTypeInference.cs.target:252 -->

**Full detail:** [Reference → Generic Constraints](ConversionStrategies-Reference/generic-constraints.md#generic-constraints) — array and map type sets, pointer and self-referential constraints, unions such as `string | []byte`, per-field equality in generic structs, and how explicit type arguments and constant arguments are chosen.

---

## Type Aliasing

A Go alias declaration, `type A = B`, becomes a C# `global using` directive that every file in the package's
project can see. Variables, parameters and results declared with a non-generic alias keep the alias name as their type.

**A type definition is not an alias.** `type Celsius float64` declares a distinct type that becomes a struct, as
[Named Numeric Types](#named-numeric-types-and-constant-contexts) describes. The exception is a definition over an
interface type, such as `type Token any`. It can have no methods of its own, so it becomes a `global using` too:

<!-- source: src/tests/Behavioral/CrossPkgLib/lib.go:16 -->
```go
type Celsius float64
…
type Temperature = Celsius
…
type Token any
```
<!-- source: src/tests/Behavioral/CrossPkgLib/lib.cs.target:1 -->
```csharp
global using Temperature = go.CrossPkgLib_package.Celsius;
global using ΔToken = object;
…
[GoType("num:float64")] partial struct Celsius;
```

Go's `any` is C# `object` ([Empty Interface](#empty-interface-any)). The `Δ` is the [rename mark](#reading-converted-code-names-and-glyphs): the package also has a `Token` method.

**An alias target is written out in full.** C# resolves it outside `namespace go;`, so each [golib](#the-golib-runtime-library)
and package type in it carries its `go.` path and a .NET type its `System.` path. Go's integer and floating-point types, and `any`, become C# keywords such as `nint` and `object`, while `uintptr` and the complex types keep their golib or `System.Numerics` names:

<!-- source: src/tests/Behavioral/PackageAliasRootedTypeArgs/main.go:44 -->
```go
type (
	…
	names = []string
	…
	fn  = func(string) int
	…
)
```
<!-- source: src/tests/Behavioral/PackageAliasRootedTypeArgs/main.cs.target:1 -->
```csharp
global using names = go.slice<go.@string>;
…
global using fn = System.Func<go.@string, nint>;
```

**An importing package gets its own copy of each exported alias.** A `global using` reaches only its own
project. So each importer declares the alias again as `<Package>ꓸ<Alias>`, where `ꓸ` stands in for Go's dot:

<!-- source: src/tests/Behavioral/AliasImport/main.go:15 -->
```go
var d AliasImportLib.DurFn = func(t time.Duration) int { return int(t / time.Second) }
```
<!-- source: src/tests/Behavioral/AliasImport/main.cs.target:19 -->
```csharp
AliasImportLibꓸDurFn d = (time.Duration t) => (nint)(int64)(t / time.ΔSecond);
```

**A generic alias is replaced by its target at every use.** A C# `using` cannot declare type parameters.
A Go alias is identical to its target, so each use names the target, and the declaration survives as a comment:

<!-- source: src/tests/Behavioral/GenericTypeAlias/main.go:15 -->
```go
type P[K comparable, V any] = Pair[K, V]
…
func swap[T comparable](p P[T, T]) P[T, T] { return P[T, T]{Key: p.Val, Val: p.Key} }
```
<!-- source: src/tests/Behavioral/GenericTypeAlias/main.cs.target:15 -->
```csharp
// type P[K comparable, V any] = Pair[K, V]
…
internal static Pair<T, T> swap<T>(Pair<T, T> p) {
    return new Pair<T, T>(Key: p.Val, Val: p.Key);
}
```

**Full detail:** [Reference → Type Aliasing](ConversionStrategies-Reference/type-aliasing.md#type-aliasing) — the bridging generated for type definitions, the qualification rules for every kind of alias target, which Go names become C# keywords, how importers read an alias record, aliases of aliases, and the generic-alias forms that are not supported.

---

## Functions and Methods

A Go function becomes a `static` method of its package class, and a method becomes a C# extension method
on its receiver. The receiver's form, `this T`, `this ref T` or the heap box `this ж<T>`
([glyphs](#reading-converted-code-names-and-glyphs)), shows how the method uses it.

**Go's export rule becomes C# access**, as [Names and Glyphs](#reading-converted-code-names-and-glyphs) describes,
so `func main` becomes `internal static void Main()`. An unexported type that appears in an exported signature
is emitted `public` itself, because C# forbids a public member from exposing an internal type.

<!-- source: src/tests/Behavioral/MultiFileInitOrder/a_first.cs.target:7 ([GoInit] internal static void init()), a_first.cs.target:11 (initΔ1) and b_second.cs.target:5 (initΔ2) -->
**Each `init` becomes a `[GoInit]` method.** C# cannot declare two methods with the same name and signature.
The first `init` keeps its name, and the rest take a `Δ` suffix ([glyphs](#reading-converted-code-names-and-glyphs))
and a number, `initΔ1`, `initΔ2`, …, in file order across the package.

**A single named result stays on the signature as a comment:** `func RuneCountInString(s string) (n int)` becomes
`nint /*n*/ RuneCountInString(…)`. Several become a named tuple ([Multi-Result Values](#multi-result-values-and-comma-ok-forms)).

<!-- source: src/tests/Behavioral/VariadicPackPassThrough/VariadicPackPassThrough.go:10 (func bump(xs ...int)) and VariadicPackPassThrough.cs.target:9 (internal static void bump(params ꓸꓸꓸnint xsʗp)) -->
**A variadic parameter is a `params` span:** `xs ...int` becomes `params ꓸꓸꓸnint xsʗp`, which
[Slices and Arrays](#slices-and-arrays) explains.

**A value receiver is `this T`.** The method gets its own copy of the value, as in Go:

<!-- source: GOROOT/src/time/time.go:267 -->
```go
func (t Time) After(u Time) bool {
	…
```
<!-- source: src/core/time/time.cs:269 -->
```csharp
public static bool After(this Time t, Time u) {
    …
```

**A pointer receiver is `[GoRecv] this ref T`.** The method reads and writes the caller's storage through a C#
`ref`, with no box and no allocation. `[GoRecv]` tells a [source generator](#source-generators) to add an
overload that takes the box, `this ж<T>`, and forwards to this method.

<!-- source: GOROOT/src/container/list/list.go:66 -->
```go
func (l *List) Len() int { return l.len }
```
<!-- source: src/core/container/list/list.cs:74 -->
```csharp
[GoRecv] public static nint Len(this ref List l) {
    return l.len;
}
```

**A method that needs the pointer itself takes the box, `this ж<T>`.** This happens when the body returns or
compares the receiver, or takes `&l.field`. `Init` takes `&l.root` and returns `l`. The `Ꮡ` prefix names a
pointer: the body binds `l` to the box's value, and `Ꮡl.of(List.Ꮡroot)` is Go's `&l.root` ([Pointers](#pointers)):

<!-- source: GOROOT/src/container/list/list.go:54 -->
```go
func (l *List) Init() *List {
	l.root.next = &l.root
	…
	return l
}
```
<!-- source: src/core/container/list/list.cs:58 -->
```csharp
public static ж<List> Init(this ж<List> Ꮡl) {
    ref var l = ref Ꮡl.DerefOrNull();

    l.root.next = Ꮡl.of(List.Ꮡroot);
    …
    return Ꮡl;
}
```

**A call picks the form that fits, as Go's automatic `&x` and `*p` do.** On a value, a `this ref` method is
called directly, as `c.Get()` is here. `Set` takes `&c.n`, so it needs a box. `heap(…)` puts `c` on the heap,
binds `c` to the heap value and hands back the box `Ꮡc` for the `Set` call:

<!-- source: src/tests/Behavioral/ReceiverFieldAddress/main.go:21 -->
```go
var c Counter // …
c.Set(100)
fmt.Println("after Set:", c.Get())   // 100
```
<!-- source: src/tests/Behavioral/ReceiverFieldAddress/main.cs.target:37 -->
```csharp
ref var c = ref heap(new Counter(), out var Ꮡc);
Ꮡc.Set(100);
fmt.Println(afterSetˢ, c.Get());
```

`afterSetˢ` is the hoisted string literal ([Strings](#strings-string-and-sstring)).

On a pointer, a `this ref` method binds its generated `ж<T>` overload. A value-receiver method runs on a copy, so
`pb.Len()` on a pointer `pb` becomes `(~pb).Len()`, where `~pb` is Go's `*pb`
([Implicit Pointer Dereferencing](#implicit-pointer-dereferencing)).

**Full detail:** [Reference → Pointers](ConversionStrategies-Reference/pointers.md#pointers) — box-taking methods called through fields, globals and slice elements, how a method that calls one on its receiver takes the box too, and nil and re-pointed receivers.

---

<a id="delegates-to-value-receiver-instances"></a>

## Function Values and Closures

Go func types become C# delegates, and func literals become lambdas or C# local functions. A closure shares
the variables it captures, as in Go.

**A func type becomes `Func<…>` or `Action<…>`.** Parameters map in order, a func with no result is an
`Action`, and several results become one tuple result. A variadic func type uses golib's
[`Funcꓸꓸꓸ<…>`](../src/core/golib/variadic.cs) or `Actionꓸꓸꓸ<…>`, where `ꓸꓸꓸ` reads as Go's `...`. A nil
func is a null delegate ([Nil and Zero Values](#nil-and-zero-values)).

<!-- source: src/tests/Behavioral/MethodValueReceiverEscape/main.go:42 -->
```go
func applyInt(f func(int) int, a int, b int) int { return f(a) + f(b) }
```
<!-- source: src/tests/Behavioral/MethodValueReceiverEscape/main.cs.target:32 -->
```csharp
internal static nint applyInt(Func<nint, nint> f, nint a, nint b) {
```

**A named func type becomes a C# `delegate`.** Its methods become extension methods on the delegate, as for
any named type ([Functions and Methods](#functions-and-methods)). A named func type with no methods, no type
parameters and no named func type in its signature has no declaration of its own. Its uses are written as the
underlying `Func<…>` or `Action<…>`, because Go converts freely between such a type and its underlying func type.

<!-- source: src/tests/Behavioral/MethodExpression/main.go:74 -->
```go
type reader func() int

func (f reader) sum(extra int) int { return f() + extra }
```
<!-- source: src/tests/Behavioral/MethodExpression/main.cs.target:61 -->
```csharp
internal delegate nint reader();

internal static nint sum(this reader f, nint extra) {
    return f() + extra;
}
```

**A func literal becomes a C# local function when its own `name := func…` statement declares it and it is
only ever called.** Every other literal becomes a lambda. C# closures capture variables, not values, as Go's
do, so a plain `int` local needs nothing extra. In the next example, `bump` is a local function.

**A heap-boxed local written after capture is reached through its box.** Here `t` lives in the
[heap box](#pointers) `Ꮡt`, where the [`Ꮡ` prefix](#reading-converted-code-names-and-glyphs) marks the box,
and `t` is a `ref` alias of its value. A C# closure cannot capture a `ref` local, so the local function `bump`
writes through the box. Both sides change the same variable.

<!-- source: src/tests/Behavioral/ClosureWriteVisibility/main.go:26 -->
```go
t := Tally{5, "s"}
bump := func() { t.total += 100 }
bump()
t.total++
```
<!-- source: src/tests/Behavioral/ClosureWriteVisibility/main.cs.target:18 -->
```csharp
ref var t = ref heap<Tally>(out var Ꮡt);
t = new Tally(5, "s"u8);
void bump() {
    Ꮡt.Value.total += 100;
}
bump();
t.total++;
```

**A captured variable that nothing writes after the closure exists is read through a snapshot.** This covers
a struct, array, slice, map or channel, and any heap-boxed variable. The copy always matches, because the
variable never changes afterward. Its name takes the `ʗ` suffix and a number, as in `tʗ1` ([detail](ConversionStrategies-Reference/pointers.md#a-capture-that-is-written-after-the-capture-point-routes-to-shared-storage-not-a-snapshot)).

**A value-receiver method value copies its receiver.** `d.printName` binds the value `d` has at that
moment, so this program prints `Name = James` twice. The converter snapshots `d` as `dʗ1` and calls the
method on it from a lambda. `gretchenˢ` is the hoisted `"Gretchen"` literal ([Strings](#strings-string-and-sstring)).

<!-- source: src/tests/Behavioral/VariableCapture/VariableCapture.go:14 -->
```go
d := data{name: "James"}
f1 := d.printName
f1()
d.name = "Gretchen"
f1()
```
<!-- source: src/tests/Behavioral/VariableCapture/VariableCapture.cs.target:22 -->
```csharp
var d = new data(name: "James"u8);

var dʗ1 = d;
var f1 = () => dʗ1.printName();
f1();
d.name = gretchenˢ;
f1();
```

**A pointer-receiver method value binds the variable itself.** `c.dec` is Go shorthand for `(&c).dec`, so
`c` moves into a heap box and the delegate binds to that box. Every write `dec` makes lands in `c`.

<!-- source: src/tests/Behavioral/MethodValueReceiverEscape/main.go:53 -->
```go
c := counter{n: 100}
sum := applyInt(c.dec, 5, 7)
```
<!-- source: src/tests/Behavioral/MethodValueReceiverEscape/main.cs.target:43 -->
```csharp
ref var c = ref heap<counter>(out var Ꮡc);
c = new counter(n: 100);
nint sum = applyInt(Ꮡc.dec, 5, 7);
```

**Full detail:** [Reference → Delegates to Value Receiver Instances](ConversionStrategies-Reference/value-receiver-delegates.md#delegates-to-value-receiver-instances) — how each method-value and method-expression form binds its receiver, receiver evaluation order, and bare or discarded function values.

---

<a id="labeled-control-flow-and-loop-variables"></a>
## Loops, Range and Labels
<!-- Length: 90 visible lines. Three rule groups here (for shapes and the constant-true loop, the Coro handoff and panic rethrow, init-clause blocks) have no home on the linked reference page yet, so they stay here in brief. -->

Go's one loop keyword becomes C# `for`, `while` or `foreach`, chosen by the loop's shape. A `ᴛ` suffix
marks a temporary where Go needs a fresh variable. A `goto` stands in for a `break` or `continue` whose target C# cannot name.
Glyphs such as `ᴛ` and `Δ` are listed in [Reading Converted Code](#reading-converted-code-names-and-glyphs).

<!-- source: src/tests/Behavioral/ForVariants/ForVariants.go:12, :22, :78 -> ForVariants.cs.target:19, :25, :70 -->
<!-- The constant matters for reachability: golib builtin.cs documents that an infinite loop relies on ᐧ folding to avoid CS0161 (not all code paths return a value). -->
**A `for` keeps its shape.** A three-clause loop stays a C# `for`, and `for i < 10 {` becomes
`while (i < 10) {`. An infinite `for {` becomes `while (ᐧ) {`, where `ᐧ` is golib's constant `true`.
C# treats a constant-true loop as endless, so a function that ends in one needs no trailing `return`.

<!-- sources by row (Go -> C#; GOROOT is Go 1.24.13; short paths are under src/tests/Behavioral/):
GOROOT/src/slices/iter.go:16 -> src/core/slices/iter.cs:17; ArrayRangeSnapshot/ArrayRangeSnapshot.go:18 -> .cs.target:21;
StringByteSemantics/main.go:12 -> main.cs.target:12; GenericTypeInference/GenericTypeInference.go:138 -> .cs.target:145;
ChannelSendToClosed/ChannelSendToClosed.go:15 -> .cs.target:18; RangeOverIntegerTypes/main.go:98 -> main.cs.target:80
Moved to the reference (range over every integer type): the row `for i := range b` -> `foreach (var i in range<uint8>(b))`,
RangeOverIntegerTypes/main.go:46 -> main.cs.target:37 -->
**Every `range` becomes a `foreach`.** Each golib collection enumerates in Go's own terms:

| Go | C# | Notes |
|---|---|---|
| `for i, v := range s` | `foreach (var (i, v) in s)` | slice: index and element |
| `for i, v := range a` | `foreach (var (i, v) in a.ΔRangeSnapshot())` | array value: iterates a copy ([Slices and Arrays](#slices-and-arrays)) |
| `for i, r := range s` | `foreach (var (i, r) in s)` | string: byte offset and rune |
| `for k, v := range m` | `foreach (var (k, v) in m)` | map ([Maps](#maps)) |
| `for i := range c` | `foreach (var i in c)` | channel: ends when closed and drained ([Channels](#channels-and-select)) |
| `for i := range size` | `foreach (var i in range(size))` | integer: golib's `range` helper |

<!-- Moved to the reference ("Reassigned or ref-bound range variable"): a range variable the body reassigns
iterates a temporary and the body declares a writable copy, foreach (var (_, rᴛ1) in s) { var r = rᴛ1; … }
(src/tests/Behavioral/RangeVarReassign/main.cs.target:22); and the = form, for i, num = range nums ->
foreach (var (iᴛ1, vᴛ1) in nums) { i = iᴛ1; num = vᴛ1; … } (src/tests/Behavioral/RangeStatements/RangeStatements.go:18
-> RangeStatements.cs.target:25). -->
<!-- A defer or go call that references i also selects the per-iteration form (src/go2cs/visitForStmt.go:337);
the copy-back fires when the body writes i or the variable is heap-boxed (visitForStmt.go:353; golden
src/tests/Behavioral/ForLoopPerIterationVars/main.cs.target:52-55, where the body only takes &i). -->
**Loop variables are per-iteration.** Go gives each iteration of `for i := …` its own `i`, but a C# `for`
shares one. When a closure captures `i` or the body takes its address, the loop counts with a hidden `iᴛ1`
and declares a fresh `i` from it. A body that writes `i` or takes its address copies `i` back to `iᴛ1`.

<!-- source: src/tests/Behavioral/ForLoopPerIterationVars/main.go:20 -->
```go
for i := 0; i < 3; i++ {
	fs = append(fs, func() int { return i })
}
```
<!-- source: src/tests/Behavioral/ForLoopPerIterationVars/main.cs.target:21 -->
```csharp
for (nint iᴛ1 = 0; iᴛ1 < 3; iᴛ1++) {
    var i = iᴛ1;
    fs = append(fs, () => i);
}
```

<!-- Reference gap: range over a function is covered only in generic-constraints.md (the named/generic Seq rule: .Invoke, spelled-out type arguments, yield false on break). No page states the Coro handoff or the panic rethrow into the ranging function (golib runtime/YieldFunctionEnumerator.cs). Move or add the rule in labels-and-loop-variables.md with its guard tests, and extend the Full detail line. -->
**Range over a function passes it a `yield` callback.** The iterator function becomes a lambda that
takes `Func<…, bool> yield`. A named func type such as `KVSeq[K, V]` becomes a C# delegate. The loop
passes the delegate's `.Invoke` method to golib's `range<…>` helper, with the element types spelled out:

<!-- source: src/tests/Behavioral/GenericTypeInference/GenericTypeInference.go:177 -->
```go
for k, v := range letters() {
	fmt.Println(k, v)
}
```
<!-- source: src/tests/Behavioral/GenericTypeInference/GenericTypeInference.cs.target:185 -->
```csharp
foreach (var (k, v) in range<@string, nint>(letters().Invoke)) {
    fmt.Println(k, v);
}
```
<!-- The iterator itself (GenericTypeInference.go:125 -> GenericTypeInference.cs.target:132):
func letters() KVSeq[string, int] { return func(yield func(string, int) bool) { _ = yield("a", 1) && yield("b", 2) } }
becomes internal static KVSeq<@string, nint> letters() { return (Func<@string, nint, bool> yield) => { _ = yield("a"u8, 1) && yield("b"u8, 2); }; } -->

The golib runtime runs the iterator on a second goroutine, a `Coro`, and hands each value across. A `break` or
`return` makes `yield` return false, so the iterator finishes and runs its defers. A panic in the
iterator is rethrown in the ranging function, where its defers can recover it.

**Labels become `goto` targets.** C# `break` and `continue` cannot name a label. So the converter keeps
the Go label and adds `continue_L:;` at the end of the loop body and `break_L:;` after the loop. A
labeled `switch` gets the same `break_L:;`, and a plain Go `goto L` stays `goto L;`.

<!-- The elided lines hold `if n+m > 5 { break scan }` and a print; the C# elision holds `goto break_scan;` (ForVariants.cs.target:58-61). -->
<!-- source: src/tests/Behavioral/ForVariants/ForVariants.go:59 -->
```go
scan:
	for _, n := range nums {
		for _, m := range nums {
			if n == m {
				continue scan
			}
			…
		}
	}
```
<!-- source: src/tests/Behavioral/ForVariants/ForVariants.cs.target:52 -->
```csharp
scan:
    foreach (var (_, n) in nums) {
        foreach (var (_, m) in nums) {
            if (n == m) {
                goto continue_scan;
            }
            …
        }
continue_scan:;
    }
break_scan:;
```

<!-- source: src/tests/Behavioral/IfStatements/IfStatements.go:6 -> IfStatements.cs.target:17 -->
<!-- Reference gap: labels-and-loop-variables.md has no rule for the if/switch init-clause block; add it there, with its guard tests, and extend the Full detail line. -->
**An `if` or `switch` init clause opens a block.** C# `if` and `switch` have no init clause, so the
declaration and the statement share new braces: `if a := -1; a < 0 {` becomes `{ nint a = -1; if (a < 0) { … } }`.

**Full detail:** [Reference → Labeled Control Flow and Loop Variables](ConversionStrategies-Reference/labels-and-loop-variables.md#labeled-control-flow-and-loop-variables) — the copy-back rules and heap-boxed loop variables, when a range variable is copied, the `=` range form, range over every integer type, blank range variables, allocation-free enumerators, and labels on empty statements.

---

## Expression Switch Statements
<!-- Length: four lowered forms, each needs its own example; secondary rules (break wrapping, default placement, relational patterns) live in the reference. -->

Go's expression `switch` runs one case and never falls into the next unless the case says `fallthrough`.
The converter emits a real C# `switch` where the labels allow one, and `if` statements where they do not.
Converter temporaries end in `ᴛ` and a number, such as `exprᴛ1`, and a name ending in `ˢ` is a hoisted string
literal ([glyphs](#reading-converted-code-names-and-glyphs)).

**Constant labels become a C# `switch`.** This needs every label to be a number or rune literal or a constant
of a plain numeric or boolean type, and no `fallthrough`. A tag of a named type or `uintptr` rules it out, because
a literal label takes the tag's type. A list of labels becomes an `or` pattern (`case 4, 5, 6:` becomes
`case 4 or 5 or 6:`), and each body ends in `break;` or a `return`.

**Other labels become an `if / else if` chain.** A C# case label must be a `const`, so variables, calls and Go
strings take the chain. So do constant names such as `true` and `false`, and named-type constants such as
`time.Saturday` ([constant values](#constant-values)). The tag is evaluated once into `exprᴛ1`, and `default`
becomes the final `else`.

<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.go:72 -->
```go
switch time.Now().Weekday() {
case time.Saturday, time.Sunday:
	fmt.Println("It's the weekend")
…
```
<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.cs.target:117 -->
```csharp
var exprᴛ1 = time.Now().Weekday();
if (exprᴛ1 == time.Saturday || exprᴛ1 == time.Sunday) {
    fmt.Println(itSTheWeekendˢ);
}
…
```

**A switch with no tag becomes `switch (ᐧ)`.** `ᐧ` is golib's constant `true`, and each case is
`case {} when <condition>:`, so the condition alone decides. A comparison with a number literal may become a
C# pattern such as `is < 12`.

<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.go:85 -->
```go
switch {
case t.Hour() < 12: // Before noon
…
```
<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.cs.target:129 -->
```csharp
switch (ᐧ) {
case {} when t.Hour() is < 12: {
…
```

**A switch that uses `fallthrough`, tagged or not, becomes an `if` chain in which each case that can be fallen
into starts a new `if`.** A local `matchᴛN` records that a case has matched. A case that falls through sets
`fallthrough`, a golib flag that clears when read. A case that can be fallen into runs when that flag is set, or
when nothing has matched and its own label does.

<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.go:188 -->
```go
switch Foo(2) {
case Foo(1), Foo(2), Foo(3):
	fmt.Println("First case")
	fallthrough
case Foo(4):
…
```
<!-- source: src/tests/Behavioral/ExprSwitch/ExprSwitch.cs.target:268 -->
```csharp
var exprᴛ6 = Foo(2);
var matchᴛ5 = false;
if (exprᴛ6 == Foo(1) || exprᴛ6 == Foo(2) || exprᴛ6 == Foo(3)) { matchᴛ5 = true;
    fmt.Println(firstCaseˢ);
    fallthrough = true;
}
if (fallthrough || !matchᴛ5 && exprᴛ6 == Foo(4)) {
…
```

**Full detail:** [Reference → Expression Switch Statements](ConversionStrategies-Reference/expression-switch.md#expression-switch-statements) —
how a `break` inside a case leaves it, where `default` may sit when cases fall through, and when a condition becomes a C# pattern.

---

## Type Switch Statements

A Go type switch becomes a C# pattern `switch` over `x.type()`, a [golib](#the-golib-runtime-library)
method that returns the value the interface holds. Each concrete-type `case` is a C# type pattern. Each
clause is a braced arm that ends in `break` or its own `return`, because Go clauses never fall through.

<!-- source: src/tests/Behavioral/TypeSwitch/TypeSwitch.go:13 -->
```go
switch t := i.(type) {
case nil:
	// A nil interface matches `case nil` — emitted as the C# `case null:` pattern.
	fmt.Println("I'm nil")
case bool:
	fmt.Println("I'm a bool")
case int, int64, uint64:
	fmt.Printf("I'm an int, specifically type %T\n", t)
default:
	fmt.Printf("Don't know type %T\n", t)
}
```
<!-- source: src/tests/Behavioral/TypeSwitch/TypeSwitch.cs.target:22 -->
```csharp
switch (i.type()) {
case null: {
    fmt.Println(iMNilˢ);
    break;
}
case bool t: {
    fmt.Println(iMABoolˢ);
    break;
}
case nint _:
case int32 _:
case int64 _:
case uint64 _: {
    var t = i;
    fmt.Printf("I'm an int, specifically type %T\n"u8, t);
    break;
}
default: {
    var t = i;
    fmt.Printf("Don't know type %T\n"u8, t);
    break;
}}
```

`iMNilˢ` is a string literal stored once in a static field ([glyphs](#reading-converted-code-names-and-glyphs)), and `"…"u8` is a UTF-8 literal.

**A single-type case binds its variable at that type.** `case bool` binds `bool t`. Go gives the case
variable the listed type only when the clause lists exactly one type.

**`default` and multi-type cases rebind the interface value.** Their body opens with `var t = i;`, so
`t` keeps the interface type, as in Go. A multi-type case stacks its labels over one body, and each
label binds only a discard, `_`.

**`case nil:` becomes `case null:`.** A nil interface is a C# `null`. No C# type pattern matches
`null`, so only this arm catches it.

**`case int:` also gets a `case int32:` label, unless the switch lists `int32` itself.** Go's `int`
becomes C#'s native-sized `nint` ([Integer Types](#integer-types-and-arithmetic)). A plain C# `int` in an
interface has the dynamic type `int32`, and that label routes it to Go's `int` clause. `case uint:` adds `case uint32:`.

<!-- source: src/tests/Behavioral/PanicRecover/PanicRecover.cs.target:79 (`case {} Δv when Δv._<error>(out var v): {`) -->
**An interface case matches by method set.** Go's `case error:` becomes
`case {} Δv when Δv._<error>(out var v):`. The `{}` pattern captures any non-null value in a hidden
`Δv`. The guard calls `_<T>`, golib's type assertion, in a form that returns false instead of panicking.

**Full detail:** [Reference → Type Switch Statements](ConversionStrategies-Reference/type-switch.md#type-switch-statements) — switches with no case variable, how cases that share one C# type merge, anonymous interface labels, and how interface cases find methods declared on a pointer.

---

## Defer / Panic / Recover

A Go function that defers or recovers keeps its body inline, inside a C# `try`/`catch`/`finally`.
You will notice a local named `ᒐ`, `defer(…, ref ᒐ)` calls, and `throw panic(x)`. Other names here, such
as `default!`, `ᴛ1` and the `ˢ` suffix, are explained in [Reading Converted Code](#reading-converted-code-names-and-glyphs).

<!-- Condensed to the forms a reader meets: the inline body with its frame, eager defer arguments, named
results, and which runtime errors recover() sees. In the reference: the receiver-field call lowered into the
finally (golden DeferFinallyLowering/main.cs.target:61; never in a function that calls recover() or has
named results), runtime.Goexit, the crash report, and (manual-conversions.md, runtime.Stack sections) the
Go-shaped tracebacks, the main-goroutine Goexit gate and the NoInlining pin on runtime.Caller callers.
src/core/golib/GoFrame.cs: m_d0..m_d3 are the inline slots; a fifth registration allocates the
List<Action> overflow. PanicException: src/core/golib/PanicException.cs:20. -->
**`ᒐ` is this call's defer list.** It is a golib [`GoFrame`](../src/core/golib/GoFrame.cs), a stack-only
`ref struct` that holds up to four deferred calls without allocating. `defer(…, ref ᒐ)` pushes a call, and
the `catch` parks a panic (a golib `PanicException`) for `recover()`. The `finally` runs `ᒐ.Run()`, which
calls them in reverse order on every exit path.

**`recover()` is a plain static call, and `panic(x)` is `throw panic(x)`.** A deferred closure needs no
handle on the frame, because `recover()` reads the panic that the `catch` parks. This is the Go blog's example:

<!-- source: src/tests/Behavioral/PanicRecover/PanicRecover.go:11 -->
```go
func f() {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println("Recovered in f", r)
		}
	}()
	…
}

func g(i int) {
	…
		panic(fmt.Sprintf("%v", i))
	…
	defer fmt.Println("Defer in g", i)
	…
}
```
<!-- source: src/tests/Behavioral/PanicRecover/PanicRecover.cs.target:21 -->
```csharp
internal static void f() {
    GoFrame ᒐ = default;
    try {
        defer(() => {
            {
                var r = recover(); if (r != default!) {
                    fmt.Println(recoveredInFˢ, r);
                }
            }
        }, ref ᒐ);
        …
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

internal static void g(nint i) {
    …
            throw panic(fmt.Sprintf("%v"u8, i));
    …
        defer((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), deferInGˢ, i, ref ᒐ);
    …
}
```

**`defer` evaluates its arguments where it stands**, as Go does. `deferInGˢ` and `i` are passed to
`defer` at once, and the lambda receives them later as `ᴛ1` and `ᴛ2`.

**Named results are declared before the `try` and returned after the `finally`.** Go runs deferred calls
after `return` assigns the results, so a deferred call can still change them. A C# `finally` cannot change
a value already returned. So each `return` only assigns the results, and an early one jumps to `ᒐdone`:

<!-- source: src/tests/Behavioral/NamedReturnDefer/main.go:57 -->
```go
func compute(x int) (out int, label string) {
	defer func() { out += 1000 }() // proves the named result is returned post-defer
	if x < 0 {
		out, label = -1, "neg"
		return out, label // …
	}
	return double(x), fmt.Sprintf("v=%d", x)
}
```
<!-- source: src/tests/Behavioral/NamedReturnDefer/main.cs.target:76 -->
```csharp
internal static (nint @out, @string label) compute(nint x) {
    nint @out = default!;
    @string label = default!;
    GoFrame ᒐ = default;
    try {
        …
        if (x < 0) {
            (@out, label) = (-1, negˢ);
            goto ᒐdone;
        }
        (@out, label) = (@double(x), fmt.Sprintf("v=%d"u8, x));
    }
    …
    finally { ᒐ.Run(); }
    ᒐdone: return (@out, label);
}
```

<!-- Verified against src/go2cs/refLoweringEmissionOperations.go:532 (the eager `nonnil(ref …)` wrap on a
pointer's deref alias, golden DirectBoxReceiverPassedWhole/main.cs.target:22), src/core/golib/builtin.cs
`nonnil`, src/core/golib/ж.cs `operator ~` and ж.StandardBox.cs `Value`, all throwing
RuntimeErrorPanic.NilPointerDereference(). No divide check is emitted anywhere in src/go2cs. -->
**Runtime errors are panics too.** A nil dereference, an integer divide by zero and an index out of range
each reach `recover()` with Go's `runtime error` message. The frame's `catch` maps the matching .NET
exception, and golib's own nil and bounds checks raise the rest.

<!-- An unrecovered panic crashes the process as in Go: the `panic: …` report on stderr and exit code 2,
even from a goroutine (reference: defer-panic-recover.md, crash-report section).
runtime.Stack, runtime.Caller and the crash report name frames Go's way with the Go file and line
(a hand-owned whole-file replacement has no GoPositionMap record and reports its C# position); inside a
deferred call the rendered tracebacks still show the panic site. Reference:
manual-conversions.md#runtimestack-renders-a-go-shaped-traceback-and-recovers-the-panic-site. -->
**Full detail:** [Reference → Defer / Panic / Recover](ConversionStrategies-Reference/defer-panic-recover.md#defer--panic--recover) — why the body is not a lambda, every named-result form, which deferred calls move into the `finally`, variadic and value-returning deferred calls, each runtime panic value, and the crash report.

---

## Goroutines

Go's `go` statement becomes a call to [golib](#the-golib-runtime-library)'s
[`goǃ`](../src/core/golib/builtin.GoroutineLaunchers.cs), which runs the call on a new goroutine. The name
ends in `ǃ`, a letter that looks like `!`, because a C# name cannot contain `!`. Each goroutine is a
dedicated operating-system thread, which is the main difference from Go.

**Arguments are evaluated at the `go` statement.** `goǃ` takes the function and its arguments
separately, so the arguments are computed before the goroutine starts. When the function cannot be passed
as it is, the converter writes a lambda over temporary parameters `ᴛ1`, `ᴛ2`, …
([glyphs](#reading-converted-code-names-and-glyphs)):

<!-- source: src/tests/Behavioral/GoCallVariations/GoCallVariations.go:83 -->
```go
func printSquare(n int) {
	go fmt.Println("Go thread square:", n*n)
	n++
	fmt.Println("Immediate n:", n)
}
```
<!-- source: src/tests/Behavioral/GoCallVariations/GoCallVariations.cs.target:100 -->
```csharp
internal static void printSquare(nint n) {
    goǃ((ᴛ1, ᴛ2) => fmt.Println(ᴛ1, ᴛ2), goThreadSquareˢ, n * n);
    n++;
    fmt.Println(immediateNˢ, n);
}
```

Called as `printSquare(5)`, the goroutine prints 25 even if it runs after `n` has become 6. A name
ending in `ˢ` is a string literal hoisted to a static field.

**A call to a named function that returns a value is wrapped in a lambda that drops the result.** Go
discards whatever a goroutine's function returns, and the lambda drops the result the same way:

<!-- source: src/tests/Behavioral/GoStmtValueReturn/main.go:48 -->
```go
go nib() // …
```
<!-- source: src/tests/Behavioral/GoStmtValueReturn/main.cs.target:44 -->
```csharp
goǃ(() => nib());
```

**A function literal becomes a lambda.** `go func() { … }()` becomes `goǃ(() => { … })`. A captured
local is often first copied into a local with a `ʗ` suffix, as
[Function Values and Closures](#function-values-and-closures) explains.

**An unrecovered panic in any goroutine ends the whole program**, as in Go: Go's `panic:` report goes to
standard error and the process exits with code 2. [Defer / Panic / Recover](#defer--panic--recover) covers `throw panic(…)`.

<!-- source: src/tests/Behavioral/GoroutinePanicExitCode/main.go:26 -->
```go
go func() {
	panic("goroutine boom")
}()
```
<!-- source: src/tests/Behavioral/GoroutinePanicExitCode/main.cs.target:13 -->
```csharp
goǃ(() => {
    throw panic("goroutine boom");
});
```

**A goroutine keeps its thread from start to finish.** golib's
[`Goroutine`](../src/core/golib/runtime/Goroutine.cs) class starts a new background thread for every
`goǃ`. A goroutine that blocks on a channel, a lock or a sleep blocks only its own thread. As in Go, the
process exits when `main` returns.

`goǃ` never uses the thread pool, because a blocked goroutine would hold a shared pool thread. golib does
not multiplex goroutines onto fewer threads either, because .NET cannot switch a running call to another
stack.

Go stacks grow, but a .NET stack overflow ends the process. Each goroutine thread therefore reserves 256 MB
of address space, committed only as it is used. The `GO2CS_GOROUTINE_STACK` environment variable sets a different size.

**Operating-system threads bound live goroutines at roughly ten thousand.** Go's goroutines reach about
a million. A Go program that starts hundreds of thousands of goroutines at once does not run the same
way after conversion.

**`runtime` keeps Go's goroutine contracts on top of threads.** `runtime.Goexit` runs the goroutine's
deferred calls and ends only that goroutine. `runtime.LockOSThread` keeps its guarantee because each
goroutine already owns its thread. These functions are hand-written C#
([detail](ConversionStrategies-Reference/manual-conversions.md#the-runtimes-process-control-surface-implement-the-contract-never-the-mechanism)).

**`sync` and `sync/atomic` run on .NET primitives.** A `sync.Mutex` is a binary `SemaphoreSlim`. Most
`sync/atomic` bodies are one line: `AddInt32` returns `Interlocked.Add(ref addr.Value, delta)`.

**Full detail:** [Reference → Goroutine callees](ConversionStrategies-Reference/defer-panic-recover.md#a-value-returning-goroutine-callee-is-wrapped-in-a-discarding-lambda) —
the other `go`-statement forms (named function types, builtins, value receivers, multi-value arguments),
where captured locals are copied, and the tests that guard each form.

---

## Channels and `select`

Go's `chan T` becomes golib [`channel<T>`](../src/core/golib/channel.cs), a port of Go's own channel
runtime. What the reader notices is the arrow glyph `ᐸꟷ`, which spells both send and receive
([glyph table](#reading-converted-code-names-and-glyphs)).

**Send is a method call and receive is a function call.** `c <- v` becomes `c.ᐸꟷ(v)`, and `<-c` becomes
`ᐸꟷ(c)`. `make` becomes a constructor whose argument is the buffer size, and `len`, `cap` and `close`
keep their names. A comma-ok receive passes the `ꟷ` sentinel, as a comma-ok map read does
([comma-ok forms](#multi-result-values-and-comma-ok-forms)):

<!-- source: src/tests/Behavioral/ChannelCapLen/main.go:18 -->
```go
fmt.Println(<-ch, len(ch))
…
d := make(chan int, 2)
d <- 7
…
close(d)
v, ok := <-d
```
<!-- source: src/tests/Behavioral/ChannelCapLen/main.cs.target:15 -->
```csharp
fmt.Println(ᐸꟷ(ch), len(ch));
…
var d = new channel<nint>(2);
d.ᐸꟷ(7);
…
close(d);
var (v, ok) = ᐸꟷ(d, ꟷ);
```

**Blocking, closing and nil match Go.** An unbuffered channel has capacity 0, so a send waits for a
receiver. A closed channel yields its buffered values, then the zero value with `ok` false. A nil
channel is `default!` ([nil and zero values](#nil-and-zero-values)), and a send or receive on it blocks forever.

**A channel's direction shows as a marker comment.** `<-chan T` renders as `/*<-*/channel<T>` and
`chan<- T` as `channel/*<-*/<T>`. All three directions are one C# type. The direction travels with the
value, so `reflect` still reports it.

**`range` over a channel becomes `foreach`,** which ends when the channel is closed and drained:

<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.go:62 -->
```go
func filter(src <-chan int, dst chan<- int, prime int) {
	for i := range src { // Loop over values received from 'src'.
		…
	}
}
```
<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.cs.target:70 -->
```csharp
internal static void filter(/*<-*/channel<nint> src, channel/*<-*/<nint> dst, nint prime) {
    foreach (var i in src) {
        …
    }
}
```

**`select` becomes a `switch` over golib's `select(…)`.** Each case registers with the runtime,
`c.ᐸꟷ(v, ꓸꓸꓸ)` for a send and `ᐸꟷ(c, ꓸꓸꓸ)` for a receive, where `ꓸꓸꓸ` marks a registration rather
than an operation. `select` waits for a ready case, commits one chosen at random as in Go, and returns
its position. A receive case's `when` guard calls `ꟷᐳ`, which hands over the received value:

<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.go:28 -->
```go
select {
case f <- x:
	x, y = y, x+y
case <-quit:
	fmt.Println("quit")
	return
}
```
<!-- source: src/tests/Behavioral/SelectStatement/SelectStatement.cs.target:32 -->
```csharp
var selᴛ1 = f.ᐸꟷ(x, ꓸꓸꓸ);
var selᴛ2 = quit;
switch (select(selᴛ1, ᐸꟷ(selᴛ2, ꓸꓸꓸ))) {
case 0: {
    (x, y) = (y, x + y);
    break;
}
case 1 when selᴛ2.ꟷᐳ(out _): {
    fmt.Println(quitˢ);
    return;
}}
```

**Case operands are evaluated once, in source order,** as Go requires. Each lands in a `selᴛN` temp
before the `switch`. Names ending in `ˢ` are [hoisted string literals](#strings-string-and-sstring).

**With a `default:` clause, the select calls `trySelect` instead.** It polls the same registrations
without blocking and returns -1 when none is ready. The C# `default:` label then runs exactly when Go's would.

**Full detail:** [Reference → Maps and Channels](ConversionStrategies-Reference/maps-and-channels.md#named-channel-types) — how golib ports Go's channel and `select` runtime, nil and closed channels inside a select, the exact operand-hoisting rules, a select that ends a function, comm-clause variables that escape to the heap, and named channel types.

---

## Struct Types

A Go struct becomes a C# `partial struct` marked `[GoType]` that holds only the fields. A
[source generator](#source-generators) writes everything else, so the declaration reads like the Go original.

**The declaration keeps Go's fields in Go's order.** An exported field is `public` and an unexported one
is `internal`. Go's `int` is C# `nint`; see [Integer Types and Arithmetic](#integer-types-and-arithmetic).

<!-- source: src/tests/Behavioral/AddressOfParamWrite/main.go:18 -->
```go
type Rect struct {
	Min, Max int
}
```
<!-- source: src/tests/Behavioral/AddressOfParamWrite/main.cs.target:7 -->
```csharp
[GoType] partial struct Rect {
    public nint Min, Max;
}
```

The [generator](../src/gen/go2cs-gen/Templates/StructType/StructTypeTemplate.cs) adds to every struct:

- a constructor that takes every field as an optional named argument, and one from `nil` for the zero value;
- `==` and `!=`, which compare field by field, as Go's `==` does;
- a field reference, `Ꮡname`, for each field, which `&s.name` uses (`Ꮡ` is the [address-of glyph](#reading-converted-code-names-and-glyphs));
- a `ToString()` that prints the fields in Go's `%v` form.

**A struct literal calls the generated constructor.** A keyed literal passes named arguments, an omitted
field keeps its zero value, and an empty named-type literal passes `nil`. `&T{…}` boxes it in `ж<T>` with `Ꮡ(…)`:

<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.go:66 -->
```go
person := Person{name: "Dr. Michał", age: 29}
…
l := &ledger{}
```
<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.cs.target:64 -->
```csharp
var person = new Person(name: "Dr. Michał"u8, age: 29);
…
var l = Ꮡ(new ledger(nil));
```

**A declared zero value is `default!` when C#'s default already equals Go's zero**; a struct that embeds a type or holds a fixed-size array is constructed instead ([Nil and Zero Values](#nil-and-zero-values)).

**A struct is copied by value, as in Go.** A C# struct assignment copies the fields, and a struct holding a fixed-size array copies it through a generated `ΔClone()` ([Slices and Arrays](#slices-and-arrays)).
<!-- shown by: src/tests/Behavioral/StructArrayFieldValueCopy/StructArrayFieldValueCopy.go:60 (c := d) -> StructArrayFieldValueCopy.cs.target:66 (var c = d.ΔClone();) -->

**A struct type declared inside a function moves to package scope**, since C# allows no type in a method body.
It takes the function's name as a prefix and is marked `[GoType("dyn")]`; `%T` still prints Go's `main.point`:

<!-- source: src/tests/Behavioral/LiftedLocalTypes/main.go:31 -->
```go
type point struct{ X, Y int }
```
<!-- source: src/tests/Behavioral/LiftedLocalTypes/main.cs.target:17 -->
```csharp
[GoType("dyn")] internal partial struct main_point {
    public nint X, Y;
}
```

**An anonymous struct is also lifted under `[GoType("dyn")]`.** It usually takes the name of the
variable or parameter where it first appears, prefixed by the function when local. Identical anonymous
structs in one function, or at package level in one file, reuse that type, as Go treats them as one type.
Here the variable itself is named `settings`, so the type takes a `ᴛ1` suffix to stay unique (`ᴛ` marks a converter-made name):

<!-- source: src/tests/Behavioral/AnonymousStructs/AnonymousStructs.go:16 -->
```go
var settings = struct {
	Verbose bool
	Retries int
}{Verbose: true, Retries: 3}
```
<!-- source: src/tests/Behavioral/AnonymousStructs/AnonymousStructs.cs.target:13 -->
```csharp
[GoType("dyn")] partial struct settingsᴛ1 {
    public bool Verbose;
    public nint Retries;
}
```

**Full detail:** [Reference → Struct Types](ConversionStrategies-Reference/struct-types.md#struct-types) — field-name and combined-field rules, access modifiers, the zero-value forms and their exceptions, how lifted types are found at any depth and deduplicated, `[GoLocalName]`, the empty struct as `EmptyStruct`, conversions between identical anonymous structs, and positional literals such as the one-field `nil` literal.

---

## Struct Type Embedding

Go embedding promotes an embedded type's fields and methods to the outer struct. C# structs cannot
inherit, so the converter declares each embed as a member and the [source generator](#source-generators)
writes the promotion. Promoted names are then used almost exactly as they are in Go. Names such as
`ж<T>` and `Ꮡ` are listed in [Reading Converted Code: Names and Glyphs](#reading-converted-code-names-and-glyphs).

**An embedded struct becomes a `ref` property named for its type.** The converter emits
`partial ref T T { get; }`, and the generator supplies the body. It is a `ref` because a Go selection
such as `record.Person` is a variable. It can be assigned, have its address taken, or be a receiver.

<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.go:29 -->
```go
type Record struct {
	Person
	Employee
}
```
<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.cs.target:29 -->
```csharp
[GoType] partial struct Record {
    public partial ref Person Person { get; }
    public partial ref Employee Employee { get; }
}
```

The embed is stored inline, not in a separate heap allocation. Copying the outer struct copies the
embedded one, as in Go.
<!-- Value copy: EmbeddedStructValueCopy (a := mid{...}; b := a; b.n = 2 leaves a.n == 1). -->

**Promoted fields and methods are used on the outer value directly.** The generator writes a `ref`
accessor for each promoted field and a forwarding method for each promoted method. A method the outer
type declares itself hides the promoted one: `Record` has its own `IsDr`, so that is the one called.

<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.go:72 -->
```go
	record.age = 18
	…
	fmt.Println(record.IsAdult())   // true
	fmt.Println(record.IsManager()) // false
	fmt.Println(record.IsDr())      // false
```
<!-- source: src/tests/Behavioral/StructPromotion/StructPromotion.cs.target:69 -->
```csharp
    record.age = 18;
    …
    fmt.Println(record.IsAdult());
    fmt.Println(record.IsManager());
    fmt.Println(record.IsDr());
```

**A pointer-receiver method promoted through a value embed receives the real field's address, never a
copy.** When the outer value is a pointer, the converter writes that address out: Go's `o.bump(5)` becomes
`o.of(outer.Ꮡinner).bump(5)`, and `o.of(outer.Ꮡinner)` is the address of `o`'s `inner` field. When the
receiver is already addressable, Go's `c.set(1)` becomes a direct call on the field, `c.flags.set(1)`.
<!-- source: src/tests/Behavioral/EmbeddedValuePointerMethod/main.go:75; src/tests/Behavioral/EmbeddedValuePointerMethod/main.cs.target:68; src/tests/Behavioral/EmbeddedValuePointerMethod/main.go:54; src/tests/Behavioral/EmbeddedValuePointerMethod/main.cs.target:50 -->

**An embedded pointer holds a `ж<T>`.** `ж<T>` is golib's [pointer](#pointers) type, and promoted
members go through it. Copying the outer struct copies the pointer, so both copies share one pointee.
<!-- Shared pointee after a copy: EmbeddedStructValueCopy, ptrHolder (h3 := h1; h3.n = 70). -->

<!-- source: src/tests/Behavioral/PointerEmbeddingPromotion/main.go:12 -->
```go
type holder struct {
	*leaf
	tag string
}
```
<!-- source: src/tests/Behavioral/PointerEmbeddingPromotion/main.cs.target:19 -->
```csharp
[GoType] partial struct holder {
    internal partial ref ж<leaf> leaf { get; }
    internal @string tag;
}
```

**An embedded interface is a plain field.** A direct call goes through it: Go's `c.Done()`, on a struct
that embeds `context.Context`, becomes a call to the `Context` field's `Done` method. When the outer type
is used as an [interface](#interfaces), the generated implementation forwards to that field.
<!-- source: src/core/os/signal/signal.cs:303 (pointer receiver form: (~cʗ1).Context.Done()); interface dispatch: src/tests/Behavioral/ReverseSortNaNOrder/package_info.cs:42 GoImplement<reverse, Interface>(Promoted = true). -->

**Full detail:** [Reference → Struct Type Embedding](ConversionStrategies-Reference/struct-embedding.md#struct-type-embedding) —
how embed members are named, transitive promotion and Go's depth rule, value copies, zero-value construction,
embeds from other packages, how promoted pointer-receiver calls are routed, nil embedded pointers, and the address
of a field promoted through a pointer.

---

## Interfaces

A Go interface becomes a C# `partial interface` marked `[GoType]`, the attribute that tells a
[source generator](#source-generators) to write the code that lets each Go type satisfy it. In
converted code a struct value from the same package enters an interface directly, while a pointer
enters wrapped in a generated adapter.

**An interface keeps its method list, and an embedded interface becomes a C# base interface.**
Method types use golib types such as `@string` ([Strings](#strings-string-and-sstring)).

<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.go:270 -->
```go
type rdr interface{ read() string }
type clsr interface{ close() string }

type rdCloser interface {
	rdr
	clsr
}
```
<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.cs.target:260 -->
```csharp
[GoType] partial interface rdr {
    @string read();
}
…
[GoType] partial interface rdCloser :
    rdr,
    clsr
{
}
```

The empty interface is C# `any`, covered in [Empty Interface](#empty-interface-any). Methods promoted by
embedding satisfy interfaces as they do in Go ([Struct Type Embedding](#struct-type-embedding)).

**A struct value goes into an interface as itself.** The generator adds the interface to the struct's
own declaration. The struct converts by a copy, just as Go copies a value into an interface.

<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.go:14 -->
```go
func f() error {
	return MyError{"foo"}
}
```
<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.cs.target:15 -->
```csharp
internal static error f() {
    return new MyError("foo"u8);
}
```

A named function type becomes a C# delegate, and a delegate cannot implement an interface. So a value of
such a type always goes in wrapped in a value adapter, such as net/http's `HandlerFuncᴠΔHandler` (the
glyphs are listed in [Names and Glyphs](#reading-converted-code-names-and-glyphs)).

**A pointer goes into an interface wrapped in an adapter.** `Ꮡ(x)` takes an address and yields a `ж<T>`,
the golib heap box a Go pointer refers to. The generated class `CounterжIncrementer` holds that box and
forwards each interface method to it.

<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.go:134 -->
```go
c := &Counter{}
var inc Incrementer = c // …
inc.Inc()
```
<!-- source: src/tests/Behavioral/InterfaceCasting/InterfaceCasting.cs.target:156 -->
```csharp
var c = Ꮡ(new Counter(nil));
Incrementer inc = new CounterжIncrementer(c);
inc.Inc();
```

Because the adapter holds the box itself, `inc.Inc()` changes the `Counter` that `c` points to. An
assertion back to `*Counter` returns that same pointer.

**Calls and nil checks read as ordinary C#; an assertion is golib's `_<T>()`.** A call through an
interface is a plain C# interface call, `animal.Speak()`. A nil interface is `default!`, a null
reference, so `err == nil` becomes `err == default!`. The assertion `back, ok := inc.(*Counter)` becomes
`inc._<ж<Counter>>(ᐧ)`, where `ᐧ` selects the comma-ok form ([Comma-Ok Forms](#multi-result-values-and-comma-ok-forms)).

**Comparing interface values goes through golib's [`AreEqual`](../src/core/golib/builtin.cs).** C#'s `==`
compares interface references, and it has no operator between an interface and the struct that implements it.
Go compares an interface value by its dynamic type and value, and `AreEqual` does the same.

<!-- source: src/tests/Behavioral/InterfaceImplementation/InterfaceImplementation.go:84 -->
```go
if err == errAgain {
```
<!-- source: src/tests/Behavioral/InterfaceImplementation/InterfaceImplementation.cs.target:82 -->
```csharp
if (AreEqual(err, errAgain)) {
```

Adapters are unwrapped first, so two interfaces holding the same pointer are equal. Comparing two values
of a type Go cannot compare, such as a slice or map, panics as it does in Go. Map keys of interface type
compare the same way ([Maps](#maps)).

**Full detail:** [Reference → Interfaces](ConversionStrategies-Reference/interfaces.md#interfaces) — how the converter records which types satisfy which interfaces across packages, adapter naming and accessibility, value adapters for types from other packages, the run-time interface shells, keyword-named methods, and publicized unexported types.

---

## Reflection (`reflect`)

Go's `reflect` converts like any other package, but its entry points are hand-written. They read each converted
value and its .NET `System.Type`, plus a few attributes the converter puts on converted types. The glyphs are in
[Reading Converted Code](#reading-converted-code-names-and-glyphs).

**The entry points read a `System.Type`, not a type word.** Go's `reflect` reads an interface's type and data
words through `unsafe.Pointer`, but a converted `any` is one `object` reference. So the
[hand-written files](#manually-converted-declarations) of [`reflect`](../src/core/reflect/value_impl.cs) and
`internal/abi` carry the value's `System.Type` and the boxed value instead. Golib's
[`GoReflect`](../src/core/golib/GoReflect.cs) answers every question from those two.

**`Kind` follows the C# representation.** Each Go kind has its own C# form, so the kind is read off the type:

| C# type | `reflect.Kind` |
|---|---|
| `bool`, `nint`, `nuint`, `int8` … `uint64`, `uintptr`, `float32`, `float64`, `complex64`, `complex128` | the matching scalar kind (`nint` is `Int`) |
| `@string`, `slice<T>`, `array<T>`, `map<K, V>`, `channel<T>` | `String`, `Slice`, `Array`, `Map`, `Chan` |
| `ж<T>`, `@unsafe.Pointer` | `Pointer`, `UnsafePointer` |
| a delegate | `Func` |
| `object` (Go `any`) or a C# interface | `Interface` |
| a `[GoType]` struct | `Struct` |
| a named type, such as `[GoType("num:nint")] partial struct Code` | its underlying kind, here `Int` |

**A struct tag is copied verbatim into `[GoTag]`.** A C# field has no place for a Go tag, so the converter
writes it as an attribute, and `StructField.Tag` reads it back:

<!-- source: src/tests/Behavioral/ReflectStructTagCopy/main.go:30 -->
```go
type record struct {
	Version  int
	Name     string `json:"name" asn1:"optional,explicit,tag:0"`
	…
}
```
<!-- source: src/tests/Behavioral/ReflectStructTagCopy/main.cs.target:8 -->
```csharp
[GoType] partial struct record {
    public nint Version;
    [GoTag(@"json:""name"" asn1:""optional,explicit,tag:0""")]
    public @string Name;
    …
}
```

**Other attributes carry what a CLR type cannot hold.** `[GoArrayDims]` and `[GoMapKeyDims]` give an array
length that the type `array<T>` does not hold. `[GoChanDir]` marks a named directional channel type, and
`[GoEmbedded]` an embedded predeclared type such as `struct{ int }`. `[GoLocalName]` keeps the Go name of a
function-local type lifted to package scope.

**A type defined over a named interface gets a descriptor carrier.** `type eface any` becomes a C#
`global using` alias rather than a struct of its own, which lets any value be assigned to it. An alias leaves
nothing in compiled code, so the converter also emits an empty interface, marked `ᴅ`, whose `[GoLocalName]` carries
the Go name. A field of that type points `reflect` at it with `[GoDescriptorType]`:

<!-- source: src/tests/Behavioral/DescriptorCarrierFieldName/main.go:23 -->
```go
type eface any // …
…
type holder struct {
	E eface
	…
```
<!-- source: src/tests/Behavioral/DescriptorCarrierFieldName/main.cs.target:1 -->
```csharp
global using eface = object;
…
[GoLocalName("eface")] internal interface efaceᴅ { }
…
[GoType] partial struct holder {
    [GoDescriptorType(Self = typeof(efaceᴅ))]
    public eface E;
    …
```

**A generic function that passes a type parameter to `reflect.TypeFor` gains a companion, marked `ᴺ`.** A type
argument cannot carry an attribute, and `eface` and `any` are one CLR type. So each call passes the carrier as the
`ᴺ` argument when a C# alias erases the type, and the type argument itself otherwise:

<!-- source: src/tests/Behavioral/GenericTypeNameCompanion/main.go:45 -->
```go
func nameOf[T any](label string) {
	t := reflect.TypeFor[T]()
	…
nameOf[eface]("eface")
…
nameOf[any]("any")
```
<!-- source: src/tests/Behavioral/GenericTypeNameCompanion/main.cs.target:24 -->
```csharp
internal static void nameOf<T, Tᴺ>(@string label) {
    var t = reflect.TypeFor<Tᴺ>();
    …
nameOf<eface, efaceᴅ>(efaceˢ);
…
nameOf<any, any>(anyˢ);
```

**Types built at run time are real CLR types.** `PointerTo`, `SliceOf`, `ArrayOf`, `MapOf` and `ChanOf`
instantiate the matching golib generic type, recording any array length or channel direction beside it. `FuncOf`
builds a delegate type, and `MakeFunc` compiles a delegate that calls your function. `StructOf` emits a new
value type with `System.Reflection.Emit`, so downstream code treats it like a converted struct.

**Full detail:** [Reference → Manually-Converted Declarations: `StructField.Tag` and the reflection bridge rules that follow it](ConversionStrategies-Reference/manual-conversions.md#structfieldtag-is-a-real-read--the-converter-has-always-emitted-the-tag-nothing-had-ever-read-it) — the bridge's rules one by one: tag reads, `Copy`, type names, assignability, the channel-direction and array-length attributes, embedded fields, `Bytes`/`SetBytes`, map entries, and the `ArrayOf`, `StructOf` and `SliceOf` constructors.

---

## Pointers

Go's `*T` becomes golib [`ж<T>`](../src/core/golib/%D0%B6.cs) (read "zhe"): a reference-type box that
holds, or points at, one `T`. The glyph `Ꮡ` marks an address: `Ꮡ(…)` makes a pointer, and a name such
as `Ꮡa` is the box that holds `a`. The [glyph table](#reading-converted-code-names-and-glyphs) lists the
other glyphs. <!-- ж<ж<T>>: src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:19 (also :80, `PrintValPtr2Ptr(ж<ж<nint>> Ꮡpptr)`) ; box nil compare: src/tests/Behavioral/NilPointerParamMethods/main.cs.target:17 -->

**An address-taken local has two names for one storage.** golib's `heap(…)` allocates the local in a box,
so a pointer to it can outlive the function, as in Go. `a` is a C# `ref` alias for ordinary reads and
writes, and `Ꮡa` is the box, used wherever Go writes `&a`:

<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.go:17 -->
```go
var a int
…
ptr = &a
```
<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:17 -->
```csharp
ref var a = ref heap(new nint(), out var Ꮡa);
…
ptr = Ꮡa;
```

A local that a closure, a `go` statement or a `defer` statement shares with its caller can also get this
form, so both sides see one variable. [Function Values and Closures](#function-values-and-closures)
covers captures. <!-- other routes to the box: `&a`, a method that keeps its receiver's address, a pointer-method value: src/go2cs/escapeAnalysisOperations.go:1143, 1150. rule: src/go2cs/escapeAnalysisOperations.go:1127-1129 (`escapes = (closureContainsIdent && !isValueType(...)) || takesAddress || usedAsRef`; isValueType is true only for basic types, variableAnalysisOperations.go:2834-2843); go/defer arms: escapeAnalysisOperations.go:982, 1026; written-after-capture picks box versus snapshot routing: variableAnalysisOperations.go:2092-2097. reference-shaped types: escapeAnalysisOperations.go:2036-2044, 1852-1866; probeI1 main.go:152 -> main.cs.target:162. Examples in src/tests/Behavioral/ClosureWriteVisibility: probeA1 boxed and written through the box, main.cs.target:18-22 (`ref var t = ref heap<Tally>(out var Ꮡt);` … `Ꮡt.Value.total += 100;`); probeA3 boxed but read-only after all writes, so the closure takes a snapshot, main.cs.target:37-41 (`var tʗ1 = t;`); probeN1 captured int stays plain, main.go:193-197 -> main.cs.target:210-215 (`nint n = 0; void inc() { n++; }`). Reference: pointers.md:311 -->

**`&T{…}` and `new(T)` allocate a box directly.** `Ꮡ(…)` boxes a new value, and golib's `@new<T>()`
boxes a zero value. <!-- @new: src/tests/Behavioral/PointerToArrayElementAddress/main.cs.target:20 -->

<!-- source: src/tests/Behavioral/IncDecPointerField/main.go:21 -->
```go
base := &counter{n: 5}
```
<!-- source: src/tests/Behavioral/IncDecPointerField/main.cs.target:21 -->
```csharp
var @base = Ꮡ(new counter(n: 5));
```

**`*p` is `.Value`, and it reads and writes the real storage.** `Ꮡ(s, i)` is the address of a slice
element, so a write through it lands in the slice itself, as in Go. A field address points into its
box the same way. <!-- field: src/tests/Behavioral/PointerToPointer/PointerToPointer.go:49 -> PointerToPointer.cs.target:40 (`Ꮡb.of(Buffer.Ꮡoff)`) ; array element `.at<T>(i)`: src/tests/Behavioral/PointerToArrayElementAddress/main.cs.target:22 -->

<!-- source: src/tests/Behavioral/SlicePointerIdentity/main.go:51 -->
```go
p := &s[1]
*p = 42
```
<!-- source: src/tests/Behavioral/SlicePointerIdentity/main.cs.target:58 -->
```csharp
var p = Ꮡ(s, 1);
p.Value = 42;
```

**A pointer receiver is `this ref T`**, or the box `this ж<T>` when the method stores or returns its
receiver ([Functions and Methods](#functions-and-methods)). <!-- source: src/core/container/list/list.cs:104 -->

**A pointer parameter the body dereferences binds a `ref` alias on entry**, so the body reads like Go
and a nil pointer panics at its first dereference
([Implicit Pointer Dereferencing](#implicit-pointer-dereferencing)). <!-- DerefOrNull returns Unsafe.NullRef<T>() for a nil box; the NullReferenceException at first use maps to "invalid memory address or nil pointer dereference": src/core/golib/ж.PointerExtensions.cs:445. box-only parameter, no alias: src/tests/Behavioral/IncDecPointerField/main.cs.target:15-17 -->

**An unexported package-level function that only dereferences a pointer parameter takes `ref T`.** At an
ordinary call, `&x` becomes `ref x`, so a local whose address feeds only such calls needs no box (a
`defer` or `go` call still passes the box). golib's `nonnil` raises Go's nil panic when the pointer is
nil: <!-- signature `internal static void addTo(ref uint64 @out, uint64 v)` at src/tests/Behavioral/RefLoweredParams/main.cs.target:11 (Go: main.go:16). Stdlib instance: src/core/crypto/internal/fips140/nistec/fiat/p224.cs:127. defer keeps the box: main.go:73 `defer printVal(&x)` -> main.cs.target:68 `defer(ᴛ1 => printVal(ref ᴛ1.DerefOrNull()), Ꮡx, ref ᒐ);`; reference: docs/ConversionStrategies-Reference/pointers.md:186, :190 -->

<!-- source: src/tests/Behavioral/RefLoweredParams/main.go:23 -->
```go
addTo(&v.x, k)
```
<!-- source: src/tests/Behavioral/RefLoweredParams/main.cs.target:16 -->
```csharp
addTo(ref nonnil(ref v).x, k);
```

**A conversion between pointer types with the same underlying type shares the storage.** golib's
`Reinterpret<T, U>()` returns a view of the same box, not a copy. So `(*point)(c)` becomes
`Ꮡc.Reinterpret<coord, point>()`, and a write through either pointer shows through the other. <!-- src/tests/Behavioral/NamedNumericPointerReinterpret/main.go:67-68 `p := (*point)(c)` / `p.X, p.Y = 3, 4` -> main.cs.target:60-61 `var p = Ꮡc.Reinterpret<coord, point>();` / `(p.Value.X, p.Value.Y) = (3, 4);` ; stdlib instance: GOROOT/src/flag/flag.go:130 `return (*boolValue)(p)` -> src/core/flag/flag.cs:133 `return Ꮡp.Reinterpret<bool, boolValue>();` -->

**Pointers compare by address.** Two pointers are equal when they point to the same storage, so a
pointer works as a map key. Raw addresses are covered in
[`unsafe.Pointer` and `uintptr`](#unsafepointer-and-uintptr). <!-- equality: src/tests/Behavioral/SlicePointerIdentity/main.go:20-69 (two pointers to one element are equal even when taken through different slices, and element pointers work as map keys). pinning: golib pins the storage only where it is pinnable, src/core/golib/ж.cs:62 (PointerStorage: None = order token, Unpinnable = correct when taken but may move, Pinnable = pinned), ж.cs:547 (EnsureStableAddress), ж.cs:979 (uintptr conversion returns an order token for a reference-holding pointee) -->

**Full detail:** [Reference → Pointers](ConversionStrategies-Reference/pointers.md#pointers) — which parameters become `ref` and their call forms, the deferred nil panic, per-iteration loop boxes, closures over boxed locals, equality and pinning, and `unsafe.Pointer` conversions.

---

## Implicit Pointer Dereferencing

In Go, `p.f` means `(*p).f`, and calling a pointer method on a variable `v` means `(&v).M()`. Converted
C# keeps these short forms wherever a C# `ref` can carry the pointer. Where a pointer local holds a golib
[`ж<T>` heap box](#pointers), the dereference is visible: [`~p`](#reading-converted-code-names-and-glyphs) or `p.Value`.

**A pointer parameter's body reads like Go.** The function binds a `ref` alias to the pointed-to value
under the Go name, so the body needs no dereference. The box keeps the name with a `Ꮡ` address prefix.
<!-- Moved to the reference (pointers.md): some pointer parameters instead become plain ref T parameters. -->

<!-- source: src/tests/Behavioral/PointerToArrayElementAddress/main.go:18 -->
```go
func populate(t *row, base uint32) {
	for i := 0; i < len(t); i++ {
		t[i] = base + uint32(i)
	}
}
```
<!-- source: src/tests/Behavioral/PointerToArrayElementAddress/main.cs.target:11 -->
```csharp
internal static void populate(ж<row> Ꮡt, uint32 @base) {
    ref var t = ref Ꮡt.DerefOrNull();

    for (nint i = 0; i < 4; i++) {
        t[i] = @base + (uint32)i;
    }
}
```

`DerefOrNull()` lets a nil pointer panic at first use, as in Go. `row` is `[4]uint32`, so `len(t)` is 4. `@base` is Go's `base`, [escaped](#reading-converted-code-names-and-glyphs) because it is a C# keyword.
<!-- source: src/tests/Behavioral/PointerToArrayElementAddress/main.go:11 (type row [4]uint32) -->

**A pointer local reads through `~` and writes through `.Value`.** `~p` returns a copy of the value and
panics on nil, like Go's `*p`. A write, `++` or `--` goes through `p.Value`, a `ref` to the real storage.

<!-- source: src/tests/Behavioral/IncDecPointerField/main.go:21 -->
```go
	base := &counter{n: 5}
	base.sub.k = 3
	…
	fmt.Println(base.n, base.sub.k) // …
```
<!-- source: src/tests/Behavioral/IncDecPointerField/main.cs.target:21 -->
```csharp
    var @base = Ꮡ(new counter(n: 5));
    @base.Value.sub.k = 3;
    …
    fmt.Println((~@base).n, (~@base).sub.k);
```
<!-- Moved to the reference: a value method called through a pointer copies the same way, rq.BumpedRecv() becomes (~rq).BumpedRecv() (src/tests/Behavioral/AddressOfParamWrite/main.go:191; main.cs.target:169). Each pointer in a chain gets its own ~: a.next.data reads (~(~a).next).data (src/tests/Behavioral/ReceiverPointerValue/main.go:64; main.cs.target:48). -->

**A pointer method on a variable takes its address for you.** Its receiver is `this ref T` ([Functions and Methods](#functions-and-methods)), so
`c.alloc(5)` stays `c.alloc(5)` and C# passes `c` by reference. `[GoRecv]` asks the [source generators](#source-generators) for a `ж<T>` overload.
<!-- source: src/tests/Behavioral/EmbeddedValuePointerMethod/main.go:90-91 and main.cs.target:77-78 (c := chunk{}; c.alloc(5) stays c.alloc(5)); src/tests/Behavioral/ReceiverPointerValue/main.go:69; src/tests/Behavioral/ReceiverPointerValue/main.cs.target:52 (b.linkTo(c)); src/tests/Behavioral/ReceiverPointerValue/main.cs.target:18 (the [GoRecv] this ref ring declaration) -->
<!-- Moved to the reference (pointers.md): a method that keeps its receiver as a pointer takes this ж<T>, and a variable used as its receiver lives in a heap box, so b.Grow(n) becomes Ꮡb.Grow(n) (src/core/strings/strings.cs:522-523; src/core/strings/builder.cs:76). -->

<!-- source: src/tests/Behavioral/EmbeddedValuePointerMethod/main.go:51 -->
```go
func (c *chunk) alloc(n uint16) {
```
<!-- source: src/tests/Behavioral/EmbeddedValuePointerMethod/main.cs.target:47 -->
```csharp
[GoRecv] internal static void alloc(this ref chunk c, uint16 n) {
```

**Full detail:** [Reference → Implicit Pointer Dereferencing](ConversionStrategies-Reference/implicit-dereferencing.md#implicit-pointer-dereferencing) — how the converter decides that a selector base needs a dereference, promoted fields through a pointer local, nested and indexed write targets, and `*p.field` through parameters and receivers.

---

## `unsafe.Pointer` and `uintptr`
<!-- length: six rules a reader meets in converted runtime and syscall code; four carry an example pair, the syscall keep-alive shows its C# only (its Go lives in GOROOT, quoted inline), order tokens have no emitted form and are described in words; the address-arithmetic example, unsafe.Slice/String aliasing and their copy cases live in the reference -->

Go's `unsafe.Pointer` becomes [`@unsafe.Pointer`](../src/core/unsafe/unsafe.cs), a class from go2cs's
hand-written `unsafe` package. It holds the address as a `uintptr`, and can also hold the pointer box it
was made from. Go's `uintptr` becomes golib's [`uintptr`](../src/core/golib/uintptr.cs) struct. The
glyphs are explained in [Reading Converted Code](#reading-converted-code-names-and-glyphs).

**`Sizeof`, `Alignof` and `Offsetof` fold to Go's numbers.** The converter computes them with Go's
layout rules, whatever layout the C# struct has. It keeps the Go expression as a comment:

<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.go:98 -->
```go
	var x struct {
		a int64
		b bool
		c string
	}
	const M, N = unsafe.Sizeof(x.c), unsafe.Sizeof(x)
```
<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.cs.target:92 -->
```csharp
    uintptr M = /* unsafe.Sizeof(x.c) */ 16;
    uintptr N = /* unsafe.Sizeof(x) */ 32;
```

**A pointer converted to `uintptr` is a real, pinned address when its storage can be pinned.**
`uintptr(unsafe.Pointer(p))` becomes a plain `(uintptr)` cast of the pointer's box. The CLR garbage
collector moves objects and Go's does not, so the cast pins pinnable storage while the box lives. Adding
an offset gives a real address, and converting it back to a pointer aliases that memory, as in Go:

<!-- source: src/tests/Behavioral/UintptrUnsafePointerIdiom/main.go:31 -->
```go
func addrOf(p *point) uintptr {
	return uintptr(unsafe.Pointer(p))
}
```
<!-- source: src/tests/Behavioral/UintptrUnsafePointerIdiom/main.cs.target:15 -->
```csharp
internal static uintptr addrOf(ж<point> Ꮡp) {
    return (uintptr)Ꮡp;
}
```

**A pointer to a variable or element whose type holds managed references gets an order token, not an
address.** The CLR cannot pin a value with a string, slice or pointer inside it. A pointer to a struct
field always gets a real address, but it is held still only when the enclosing variable can be pinned.
<!-- the three storage kinds: src/core/golib/ж.cs:62-83 (None = order token, Unpinnable, Pinnable); ж.StandardBox.cs:174 and ж.ElemRefBox.cs:252 (None for a reference-bearing T); ж.FieldRefBox.cs:153-161 (a field reference always names a real interior address). Shown by: src/tests/Behavioral/ReflectFieldAddrWrite (a reflect-projected token converted back to a pointer and written through); GolibTests PointerTokenConversionTests, ManagedPointerTokenMintTests. -->

A token compares and sorts like an address, and converts back to the same box while that box lives.
Converting a token plus an offset back to a pointer, or dereferencing a token as another type, raises a
recoverable Go panic that names the problem.
<!-- the panics: src/core/golib/ж.cs:751-930 (token arithmetic refused, arm 2a panics at dereference), RuntimeErrorPanic.cs:41; GolibTests TokenArithmeticRefusalTests, OrderTokenOffsetZeroRefusalTests -->

**`unsafe.Pointer(p)` keeps its referent.** The conversion becomes `@unsafe.Pointer.FromPinnedBox` or
`FromBox`, which stores the source box beside the number. Holding the box keeps the referent alive, as
Go's collector does. For the pinned form, the round trip back to `uintptr` is exact:

<!-- source: src/tests/Behavioral/UintptrUnsafePointerIdiom/main.go:57 -->
```go
	up := unsafe.Pointer(e0)
	fmt.Println("round trip:", uintptr(up) == a0)
```
<!-- source: src/tests/Behavioral/UintptrUnsafePointerIdiom/main.cs.target:43 -->
```csharp
    @unsafe.Pointer up = @unsafe.Pointer.FromPinnedBox(e0);
    fmt.Println(roundTripˢ, (uintptr)up == a0);
```

**A numeric value pun is a `bitcast`.** Reading a number's bits as another number of the same size, as
`math.Float64bits` does, becomes golib's `bitcast<TSrc, TDst>`, which boxes nothing.

<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.go:57 -->
```go
func Float64bits(f float64) uint64 {
	return *(*uint64)(unsafe.Pointer(&f))
}
```
<!-- source: src/tests/Behavioral/UnsafeOperations/UnsafeOperations.cs.target:50 -->
```csharp
public static uint64 Float64bits(float64 f) {
    return bitcast<float64, uint64>(f);
}
```

**A pointer passed to a syscall as `uintptr(unsafe.Pointer(p))` stays alive for the whole call, as in
Go.** For `syscall.Syscall(getrandomTrap, uintptr(unsafe.Pointer(unsafe.SliceData(p))), …)` in
`internal/syscall/unix`, the pointer moves into a keep-alive temp whose name starts with `ᴋ`, and
`GC.KeepAlive` runs on it after the call:
<!-- Go side: Go 1.24.13 src/internal/syscall/unix/getrandom.go:36-39 (GOROOT; not in this repo): `syscall.Syscall(getrandomTrap, uintptr(unsafe.Pointer(unsafe.SliceData(p))), uintptr(len(p)), uintptr(flags))` -->
<!-- source: src/core/internal/syscall/unix/linux/getrandom.cs:38 -->
```csharp
    var ᴋ0 = @unsafe.SliceData(p);
        var (r1, _, errno) = syscall.Syscall(getrandomTrap, (uintptr)ᴋ0, (uintptr)len(p), (uintptr)flags);
    System.GC.KeepAlive(ᴋ0);
```

**Full detail:** [Reference → Converting a Go pointer to `unsafe.Pointer`](ConversionStrategies-Reference/pointers.md#converting-a-go-pointer-to-unsafepointer) — every conversion form and when each factory is used, address arithmetic, pinning, order tokens, layout folding, value puns, the cases where `unsafe.Slice` or `unsafe.String` copies instead of aliasing, and the syscall keep-alive rule.

---

## Source Generators

Some Go behavior cannot be written directly in C#. The converter emits a short attributed declaration, and
Roslyn source generators ([`src/gen/go2cs-gen`](../src/gen/go2cs-gen/)) write the rest when the project
compiles.

**Generated code is not in the converted `.cs` files.** A constructor, overload or conversion that seems
to be missing comes from a generator. Its output is saved as files you can read: a single package's
`Generated` folder, or `.artifacts/gen` under a `-recurse` output root.

**Each generator keys on one marker the converter emits.** In the examples, `ж<T>` is golib's heap box,
the object a Go pointer points to, and a `Ꮡ` prefix names an address ([Names and Glyphs](#reading-converted-code-names-and-glyphs)).

| Generator | Driven by | Produces |
|---|---|---|
| `TypeGenerator` | `[GoType]` on a type | the body of each type: struct constructors, field references, equality and `ToString`; wrappers for named numeric, slice, array, map and channel types; interface support code; members promoted by [struct embedding](#struct-type-embedding) |
| `RecvGenerator` | `[GoRecv]` on a method | a `ж<T>` overload of each pointer-receiver method |
| `ImplementGenerator` | `[assembly: GoImplement<T, I>]` | the code that lets `T` or `*T` be used as interface `I` |
| `ImplicitConvGenerator` | `[assembly: GoImplicitConv<S, T>]` | an implicit conversion operator between two types C# cannot convert directly, such as two named numeric types or two structs with the same underlying type |
| `StrGenerator` | `[GoStr]` on a method | the `@string` overload of an [`sstring` twin](#strings-string-and-sstring); for a package-level function, also the delegate used as its value, such as `Sprintfᶠ` |
| `PartialStubGenerator` | a `partial` method with no body | a stub that throws, when no hand-written body exists (see [Functions Without a Go Body](#functions-without-a-go-body)) |

**A `[GoType]` struct lists only its fields.** `TypeGenerator` adds the rest: constructors such as
`new Buffer(nil)`, the `==` operator, and field references such as `Buffer.Ꮡoff`. The converted
`&b.off` uses `Buffer.Ꮡoff` to take the address of a field of the heap box `Ꮡb` ([Pointers](#pointers)).

<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.go:5 -->
```go
type Buffer struct {
	buf      []byte
	off      int
	lastRead int8
}
…
	b := Buffer{}
	PrintValPtr(&b.off)
```
<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:7 -->
```csharp
[GoType] partial struct Buffer {
    internal slice<byte> buf;
    internal nint off;
    internal int8 lastRead;
}
…
    ref var b = ref heap<Buffer>(out var Ꮡb);
    b = new Buffer(nil);
    PrintValPtr(Ꮡb.of(Buffer.Ꮡoff));
```

**A pointer-receiver method gains a box overload.** The converter usually emits it as an extension on
`ref T` marked `[GoRecv]` ([Functions and Methods](#functions-and-methods)). `RecvGenerator` adds an
overload on `ж<T>`, so a call on the box `&Buffer{buf: p}` binds it.

<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.go:55 -->
```go
func (b *Buffer) Read(p []byte) (n int, err error) {
	…
	(&Buffer{buf: p}).Read(p)
```
<!-- source: src/tests/Behavioral/PointerToPointer/PointerToPointer.cs.target:45 -->
```csharp
[GoRecv] public static (nint n, error err) Read(this ref Buffer b, slice<byte> p) {
    …
    (Ꮡ(new Buffer(buf: p))).Read(p);
```

**The converter decides interface satisfaction; a generator builds it.** The converter records each
distinct cast of a type to an interface as one attribute in `package_info.cs`, here `[assembly: GoImplement<Setting, Describer>(Pointer = true)]`.
For a pointer, `ImplementGenerator` emits an adapter named `XжI`, here `SettingжDescriber`, that wraps the
`ж<T>` box ([Interfaces](#interfaces)).

<!-- source: src/tests/Behavioral/PointerInterfaceStructField/main.go:33 -->
```go
func assignDescriber(h *holder, s *Setting) {
	h.d = s
}
```
<!-- source: src/tests/Behavioral/PointerInterfaceStructField/main.cs.target:25 -->
```csharp
internal static void assignDescriber(ref holder h, ж<Setting> Ꮡs) {
    h.d = new SettingжDescriber(Ꮡs);
}
```

**Full detail:** [Reference → Source Generators](ConversionStrategies-Reference/source-generators.md#source-generators) — how `package_info.cs` pins each type's accessibility, which attributes stay on a declaration and which move to `package_info.cs`, variadic receiver overloads, and the forwarding rules for `sstring` twins.

---

## Functions Without a Go Body

A Go function declared without a body keeps its code in assembly, cgo or another package. It becomes a C#
`partial` method with no body, or a real body when the converter knows where Go keeps the code.

**A bodyless declaration stays a `partial` method.** Go implements `archMax` in assembly, which C# cannot
compile. The declaration keeps its Go name and signature, so every caller converts normally:

<!-- source: GOROOT/src/math/dim_asm.go:11 -->
```go
func archMax(x, y float64) float64
```
<!-- source: src/core/math/dim_asm.cs:11 -->
```csharp
internal static partial float64 archMax(float64 x, float64 y);
```

**A hand-owned companion supplies the body.** A [hand-owned](#manually-converted-declarations) file named
`*_impl.cs` beside the converted files holds the implementing half. Here it calls Go's portable fallback:

<!-- source: src/core/math/math_impl.cs:31 -->
```csharp
internal static partial float64 archMax(float64 x, float64 y) => max(x, y);
```

**A linkname pull becomes a forwarder.** `//go:linkname` binds a Go name to a body kept elsewhere. A known C#
target gets a real body that calls its [package class](#package-conversion), here `go.time_package` (`@string` is Go's
[`string`](#strings-string-and-sstring)). `[StackTraceHidden]` hides the forwarder from traces, as in Go the two names are one function:

<!-- source: GOROOT/src/time/tzdata/tzdata.go:31 -->
```go
//go:linkname registerLoadFromEmbeddedTZData time.registerLoadFromEmbeddedTZData
func registerLoadFromEmbeddedTZData(func(string) (string, error))
```
<!-- source: src/core/time/tzdata/tzdata.cs:30 -->
```csharp
//go:linkname registerLoadFromEmbeddedTZData time.registerLoadFromEmbeddedTZData
[global::System.Diagnostics.StackTraceHidden] internal static void registerLoadFromEmbeddedTZData(Func<@string, (@string, error)> _) {
    go.time_package.registerLoadFromEmbeddedTZData(_);
}
```

**A linkname push forwards too, or panics with a reason.** In a push, the directive sits on the other package's
body and names this declaration. A curated list records each pair; when that body needs runtime machinery go2cs
does not model, the declaration panics naming the pair instead.

**An assembly trampoline forwards the same way.** Some assembly only jumps to another Go function. The forwarder
appears when the signatures are identical, or differ only in pointers to structs with the same field layout.

**In the standard library, `purego` removes many bodyless declarations.** The library converts
[as Go builds with `-tags purego`](#the-standard-library-reproduces-go--tags-purego), so Go's portable file
replaces the assembly declaration and the C# method has a real body.

**Anything left gets a throwing stub.** A [source generator](#source-generators) completes every other `partial`,
cgo functions included. It throws `NotImplementedException` naming the function, so the gap surfaces at first call.

**Full detail:** [Reference → `//go:linkname` and assembly forwarders](ConversionStrategies-Reference/manual-conversions.md#a-cross-package-golinkname-pull-emits-a-forwarder-not-a-throwing-stub) — how pull and push forwarders are chosen and how they bridge types, the curated target lists, assembly-trampoline limits, cgo dynamic-import records, stub addresses, and the guard tests.

---

## Manually-Converted Declarations

A small set of Go declarations becomes C# written by hand, which the converter never writes over. The
reader meets these *hand-owned* files and functions as ordinary C# beside converted code.

**A declaration is hand-owned when Go's mechanism has no .NET equivalent.** Such code hides a pointer
inside an integer, walks an interface's two-word layout through `unsafe.Pointer`, calls a scheduler
primitive, or is written in assembly. In C# an `any` is one object reference, and a
[`ж<T>`](#reading-converted-code-names-and-glyphs) heap box is a reference the .NET garbage collector
must see. So the hand-written C# implements the observable contract, not the mechanism.

**A whole file is hand-owned with `[module: go.GoManualConversion]`.** When such a file stands in for a
converted Go file, the converter writes its own version beside it as an uncompiled `<name>.cs.auto`.
`sync/atomic`'s `Value` lives in one such file. Go loads a `Value` by reinterpreting the interface's
type and data words:

<!-- source: GOROOT/src/sync/atomic/value.go:28 -->
```go
func (v *Value) Load() (val any) {
	vp := (*efaceWords)(unsafe.Pointer(v))
	typ := LoadPointer(&vp.typ)
	…
```
<!-- source: src/core/sync/atomic/value.cs:6 -->
```csharp
[module: go.GoManualConversion]
…
[GoType] partial struct Value {
    internal any v;
}
…
[GoRecv] public static any /*val*/ Load(this ref Value v) {
    return Volatile.Read(ref v.v);
}
```

**A single declaration is hand-owned through a registry.** The converter keeps a list of Go types and
functions, by package and name, that it does not emit. It leaves a placeholder comment in their place,
and a hand-written `*_impl.cs` file in the same package supplies them. `runtime.Gosched` is one: its Go
body switches stacks with `mcall`, which .NET does not have.

<!-- source: GOROOT/src/runtime/proc.go:362 -->
```go
func Gosched() {
	checkTimeouts()
	mcall(gosched_m)
}
```
<!-- source: src/core/runtime/windows/proc.cs:351 -->
```csharp
// go2cs generated this placeholder — func Gosched is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])
```
<!-- source: src/core/runtime/managed_impl.cs:264 -->
```csharp
public static void Gosched()
{
    …
    golib.GoschedBackoff.Yield();
}
```

Golib's `GoschedBackoff` yields the current .NET thread, as Go's `Gosched` yields the processor.

**A hand-owned file can be specific to one operating system.** A package's OS-specific files, converted
or hand-owned, live in its `windows/`, `linux/` and `darwin/` subfolders. A registry entry can likewise
name the operating systems it covers, since Go can declare the same name once per OS.

**The main hand-owned surfaces implement Go's contract on .NET primitives:**
- `sync.Pool` and `sync.Cond`'s copy check.
- `runtime.SetFinalizer`, `runtime.AddCleanup` and `weak.Pointer`, on .NET object lifetime.
- `runtime.GOMAXPROCS`, `Gosched`, `LockOSThread` and `Goexit`, on .NET threads; see [Goroutines](#goroutines).
- `runtime.GC` and `runtime.ReadMemStats`, on the .NET garbage collector.
- `time`'s timers.
- `hash/crc32`, on .NET hardware intrinsics.
- `reflect` and `internal/reflectlite`; see [Reflection](#reflection-reflect).

**The operating-system boundary is hand-owned.** Go's network poller is half in `internal/poll` and half
in the runtime scheduler. `internal/poll` declares the `runtime_poll*` functions with
[no Go body](#functions-without-a-go-body). go2cs implements them on .NET threads, and the poller code
that calls them stays converted.

On Linux, Go's `syscall` wrappers end in assembly, chiefly `internal/runtime/syscall.Syscall6`. go2cs
hand-owns `Syscall6` with one glibc `syscall(2)` binding, and every converted wrapper runs on it.

**Full detail:** [Reference → Manually-Converted Declarations](ConversionStrategies-Reference/manual-conversions.md#manually-converted-declarations) — every hand-owned surface and why, `//go:linkname` forwarders in both directions, the poller and system call internals on each OS, native bindings, how hand-owned files and registry entries are scoped per platform, and the reflection bridge.

---

## The standard library reproduces Go `-tags purego`

Go writes its hottest crypto and hash routines in `.s` assembly, which a transpiler cannot convert.
So the converted standard library reproduces Go built with `-tags purego`: portable pure-Go code only.

**The tag selects the file with a real body.** The default amd64 build binds SHA-256's block function to
assembly. Under `purego`, a sibling file with a pure-Go body is chosen instead:

<!-- The default-build side: GOROOT/src/crypto/internal/fips140/sha256/sha256block_amd64.go:5
     is `//go:build !purego` and declares `//go:noescape func blockAMD64(dig *Digest, p []byte)` with no
     body; its `_amd64` filename suffix limits it to that architecture. The noasm file is at the same
     path; neither file is vendored in this repo. -->
<!-- source: GOROOT/src/crypto/internal/fips140/sha256/sha256block_noasm.go:5 -->
```go
//go:build (!386 && !amd64 && !arm64 && !loong64 && !ppc64 && !ppc64le && !riscv64 && !s390x) || purego
…
func block(dig *Digest, p []byte) {
	blockGeneric(dig, p)
}
```

Only that file is converted. The C# keeps its build line as a comment, and
[`slice<byte>`](#slices-and-arrays) is Go's `[]byte`:

<!-- source: src/core/crypto/internal/fips140/sha256/sha256block_noasm.cs:4 -->
```csharp
//go:build (!386 && !amd64 && !arm64 && !loong64 && !ppc64 && !ppc64le && !riscv64 && !s390x) || purego
…
internal static void block(ref Digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}
```

**Two tags carry the same decision.** A `-stdlib` or `-tests` conversion applies `purego,math_big_pure_go`
by default; an explicit `-tags` replaces it. `math/big` names its portable fallback `math_big_pure_go`
rather than `purego`, so both are needed. Other conversions, such as `-recurse`, use exactly the tags you pass.

<!-- The math_big_pure_go member: arith_decl.go (`!math_big_pure_go`) declares eight bodyless
     functions whose bodies are arith_$GOARCH.s; arith_decl_pure.go (`math_big_pure_go`) forwards each
     to the `_g` pure-Go implementation in arith.go. With purego alone, every big.Int/Float/Rat
     arithmetic path compiled and threw on first use (surfaced as time.TestTruncateRound -> big.Int.Mul
     -> mulAddVWW). Default set: ../src/go2cs/commandLineOptions.go (defaultStdLibBuildTags);
     resolveBuildTags keys the default on the -stdlib and -tests flags, not on the package being
     standard library, so a -tests run on any package gets it. `-tags=` clears the default. A -stdlib
     run prints the tags it applies, e.g.
     `Applying build tags: purego,math_big_pure_go (default; pass -tags to override)`
     (../src/go2cs/stdLibConverter.go:60); -tests shares the default but does not print it. -->

**An assembly-backed declaration ends in one of three ways:**

- **A `purego` sibling exists.** The tag selects the real body, as for SHA-256. This is the common case.
- **The code is gated on architecture alone.** Hand-written C# supplies the body, as a companion file or a
  whole-file replacement; see [Manually-Converted Declarations](#manually-converted-declarations).
- **Nothing supplies a body.** The declaration compiles as a stub that throws if called; see
  [Functions Without a Go Body](#functions-without-a-go-body).

**Behavior matches the `purego` build.** `crypto/elliptic`'s P-256 `Inverse` panics under `purego`, in
real Go and in the converted code alike.

<!-- Upstream gating: crypto/elliptic/nistec_p256.go is `amd64 || arm64` with no `!purego`, while
     crypto/internal/fips140/nistec/p256_ordinv.go is `(amd64 || arm64) && !purego`; under purego
     p256_ordinv_noasm.go returns errors.New("unimplemented") and Inverse panics with
     `crypto/elliptic: nistec rejected normalized scalar`. Checked against the Go 1.24.13 tree; the
     panic text is in ../src/core/crypto/elliptic/nistec_p256.cs:28. -->

**Full detail:** [Reference → The standard-library conversion applies `-tags purego`](ConversionStrategies-Reference/purego.md#the-standard-library-conversion-applies--tags-purego) — why the tag is on by default and the alternatives weighed, how `-tests` shares it, the `math/big` fallback tag, the three outcomes with more packages named, and the `crypto/elliptic` gating in full.

---

## Comments

A Go comment that reaches the C# is copied word for word: a `//` line stays a `//` line, and a `/* … */` block stays a block. Apart from the license header, Go's comments appear only with `-comments`, which is off by default and always on for converted tests. Notes the converter writes itself, such as the one above [hoisted string literals](#strings-string-and-sstring), always appear. `nint`, `slice<byte>` and `UntypedInt` are Go's `int`, `[]byte` and an untyped constant ([glyphs](#reading-converted-code-names-and-glyphs), [Slices and Arrays](#slices-and-arrays), [Constant Values](#constant-values)).

**The license header always survives.** A comment group ahead of `package` that mentions a copyright, a license or an SPDX tag is copied to the top of the C# file. The converted file is a derivative work of the Go source, so its notice travels with it. Without `-comments`, every other Go comment is dropped:

<!-- source: src/tests/Behavioral/FirstClassFunctions/FirstClassFunctions.go:1 -->
```go
// Copyright 2011 The Go Authors. All rights reserved.
…
	win            = 100 // The winning score in a game of Pig
```
<!-- source: src/tests/Behavioral/FirstClassFunctions/FirstClassFunctions.cs.target:1 -->
```csharp
// Copyright 2011 The Go Authors. All rights reserved.
…
internal static UntypedInt win => 100;
```

**With `-comments`, doc comments sit above their declarations.** They stay plain `//` lines rather than becoming XML documentation comments, so Go's doc links like `[RuneError]` read exactly as in Go. The `DecodeRune` example in the next rule shows a doc comment and a trailing comment together.

**A statement's comment keeps its line.** A comment on its own line ahead of a statement stays on its own line, indented with its block. A statement's trailing comment follows its last C# line after one space, even at the end of a block. A trailing comment does not keep Go's column alignment, because the converted lines have different lengths:

<!-- source: Go toolchain src/unicode/utf8/utf8.go:149 (Go 1.24.13; the Go source of src/core/unicode/utf8/utf8.cs; cited from the toolchain because src/core holds no .go files and the behavioral goldens are captured without comments) -->
```go
// DecodeRune unpacks the first UTF-8 encoding in p and returns the rune and
// its width in bytes. If p is empty it returns ([RuneError], 0). Otherwise, if
…
func DecodeRune(p []byte) (r rune, size int) {
	…
		mask := rune(x) << 31 >> 31 // Create 0x0000 or 0xFFFF.
```
<!-- source: src/core/unicode/utf8/utf8.cs:156 -->
```csharp
// DecodeRune unpacks the first UTF-8 encoding in p and returns the rune and
// its width in bytes. If p is empty it returns ([RuneError], 0). Otherwise, if
…
public static (rune r, nint size) DecodeRune(slice<byte> p) {
    …
        var mask = (((rune)x << (int)(31)) >> (int)(31)); // Create 0x0000 or 0xFFFF.
```

**Full detail:** [Reference → Comments](ConversionStrategies-Reference/comments.md#comments) — how attached and free-floating comments are told apart, which statement positions take a trailing comment, multi-line block comments, and the leading shapes that keep their own line.

---

## Packages That Do Not Type-Check

A Go package that the type checker cannot fully resolve still converts, and the run goes on. This
happens in application code, such as the packages a [`-recurse`](#package-conversion) run reaches.
One missing symbol costs only its own code, not its file or its package.

**The converter reports, then converts.** Here `addressTaken` calls a function that does not exist:

<!-- source: src/go2cs/untypedPackageConversion_test.go:74 -->
```go
func addressTaken() {
	x := 1
	undefinedFunc(&x)
	fmt.Println(x)
}
```

The package still converts. The warning names the package, then lists the errors Go reports:

<!-- source: src/go2cs/conversionDriver.go:224 -->
```text
WARNING: … did not fully type-check; converting best-effort — code depending on the following is emitted untyped: …
```

**Only the dependent code fails to build.** Go's type checker records no type for an expression built
on an unresolved symbol. The converter still emits it, and an unresolved type shows Go's placeholder,
so `var u UndefinedType` emits `invalid type u = default!`. Nothing declares what is missing, so the
build fails at those lines.

**Faults stay inside one file or package.** If the converter itself fails on a file, it skips that
file with a warning. In a `-recurse` run, a package that cannot be loaded or converted is recorded as
failed and the next package proceeds. The run ends by naming the packages that failed.

**The standard-library conversion stops on a load failure instead.** `-stdlib` orders packages by
their imports before converting any. A package that cannot be loaded leaves no correct order, so the
conversion aborts and names every package that failed to load.

**Full detail:** [Reference → Packages That Do Not Type-Check](ConversionStrategies-Reference/packages-that-do-not-type-check.md#packages-that-do-not-type-check) — why an unresolved expression has no type, how the converter tolerates it, how one package's fault stays inside that package, and the guard tests.

---

## Deterministic Output

Converting the same Go source with the same converter build, build tags and target platform produces
byte-identical C# every run. Any change in the converted C# comes from a change in the Go, its
dependencies, the converter, the build tags or the target platform.

So converted code can be diffed, committed and reviewed like any other source. go2cs's golden tests rely
on this: each converted file is compared with its checked-in `.cs.target`, byte for byte apart from line
endings.

**Files convert one at a time, in sorted filename order.** This order fixes names numbered across the
package. Go allows many `init` functions per package, and C# needs a distinct name for each, so each
`init` after the first becomes `initΔN` ([Functions and Methods](#functions-and-methods)).
Here `a_first.go` holds two, so `b_second.go` starts at `initΔ2`:

<!-- source: src/tests/Behavioral/MultiFileInitOrder/b_second.go:3 -->
```go
func init() {
	order = append(order, "b_second#1")
}
```
<!-- source: src/tests/Behavioral/MultiFileInitOrder/b_second.cs.target:5 -->
```csharp
[GoInit] internal static void initΔ2() {
    order = append(order, "b_second#1"u8);
}
```

`[GoInit]` marks a method that runs when the package loads; see [Package Conversion](#package-conversion).

**Standard-library packages convert in dependency order.** An importer reads each dependency's
`package_info.cs`, so every dependency converts first ([Package Conversion](#package-conversion)). The
queue is built from package paths in sorted order, so it is the same every run.

**Names the converter collects in a map are sorted before they are written.** The converter is a Go
program, and Go's map iteration order changes from run to run. In this pointer swap from `math/big`, the
pointer parameters `Ꮡx` and `Ꮡy` each have a `ref` alias, `x` and `y` ([Pointers](#pointers)). After
the swap, each alias is refreshed in sorted name order:

<!-- source: src/core/math/big/int.cs:1323 -->
```csharp
    if (x.neg) {
        (Ꮡx, Ꮡy) = (Ꮡy, Ꮡx); x = ref Ꮡx.DerefOrNull(); y = ref Ꮡy.DerefOrNull(); // & is symmetric
    }
```

**Full detail:** [Reference → Deterministic Output](ConversionStrategies-Reference/deterministic-output.md#deterministic-output) — which shared converter state each rule protects, and the unstable or broken output it prevents.
