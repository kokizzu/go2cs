# CENSUS — type-name erasure for named types over an interface

> Point-in-time census + design record, **2026-09-01**, coordinator sub-agent lane
> `claude/coord-type-name-erasure-census`, worktree HEAD `e06c04cc7` (master).
> Commissioned from the `unique` non-bank entry on
> [`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md) — *"Blocker A — the
> `testEface` naming divergence"* (that entry lives on `origin/claude/coord-unique-bank`, not yet
> merged; its measurements are quoted here, never re-derived from prose).
>
> **Every number below is measured at this head.** Two independent derivations are used and they
> are kept apart on purpose: a `git grep` over the committed emission (the converter's *output*),
> and a `go/parser` walk over the Go 1.23.12 standard-library *sources* that reproduces the
> converter's predicate without reading a byte of go2cs output. An instrument built out of the
> thing under test cannot independently measure it.
>
> Doc type: **CENSUS** — a point-in-time record. Amend with dated blocks; never rewrite, never
> execute from.

---

## 0. Headline

| | Measured |
|---|--:|
| `global using <Name> = object;` lines, `src/core` (tracked `.cs`) | **166** in 67 files |
| … of which **declaring** sites (bare alias name) | **16** |
| … of which **imported re-export** sites (`<pkg>ꓸ<Name>`) | **150** |
| Same pattern, `src/tests/Behavioral` | **25** in 13 files |
| Repo-wide total | **191** in 80 files |
| Distinct `src/core` packages carrying at least one | **39** |
| … of which are **banked roster rows** | **37** (19,169 matching verdicts, 92 disclosed) |
| Distinct **production** Go types behind the whole population | **7** |
| Class (ii) — `global using <Name> = <non-empty interface>;` from a DEFINED type, in the stdlib | **0** |
| GOROOT `reflect.TypeFor[…]` sites, all packages | **122** |
| … reading a **type parameter** (the shape no call-site metadata can fix) | **2** — both in `unique` |
| Reflect-visible positions measured divergent by the live probe | **9 of 9** |

**The board's figure was 167; this head measures 166.** The one-line difference is not
reconciled — the board's census was taken on a different tree at a different hour and its
instrument is not recorded. It does not change any conclusion; 166 is what `git grep` reports at
`e06c04cc7` and is the number used throughout.

⚠ **Census-instrument note, paid on the first pass.** The obvious anchored pattern
`^\s*global using [A-Za-z_][^ =]*\s*=\s*object\s*;` returns **162**, not 166. The four it misses
are all `ΔToken` — the converter's collision rename puts a **Δ** (U+0394) at the head of the
identifier and `[A-Za-z_]` does not match it. This is CLAUDE.md's *"a census over converted C#
never keys on a type's spelled NAME"* rule landing inside the census of this very defect. The
counts above use `[^ =]+` for the identifier.

---

## 1. The census (a) — the committed emission at head

Instrument: `git grep -n -E '^[[:space:]]*global using [^ =]+[[:space:]]*=[[:space:]]*object[[:space:]]*;' -- '*.cs'`.
`git grep` (not bare `rg`) because `src/core/.gitignore` makes ripgrep under-count; `git grep`
searches tracked files regardless.

### 1a. Class (i) — `global using <Name> = object;` (empty-interface underlying)

**The 16 declaring sites** — the alias is spelled bare because the package that declares the Go
type is the package that emits it:

| File:line | Alias | Go declaration | Kind |
|---|---|---|---|
| `src/core/crypto/crypto.cs:6` | `PublicKey` | `type PublicKey any` | production, exported |
| `src/core/crypto/crypto.cs:7` | `PrivateKey` | `type PrivateKey any` | production, exported |
| `src/core/crypto/crypto.cs:8` | `DecrypterOpts` | `type DecrypterOpts any` | production, exported |
| `src/core/database/sql/driver/driver.cs:39` | `Value` | `type Value any` | production, exported |
| `src/core/database/sql/driver/package_test_info.cs:10` | `Value` | (same, re-declared for the test compilation) | test-compilation copy |
| `src/core/encoding/json/stream.cs:4` | `ΔToken` | `type Token any` | production, exported (Δ-renamed: name/method collision) |
| `src/core/encoding/json/package_test_info.cs:35` | `ΔToken` | (same) | test-compilation copy |
| `src/core/encoding/xml/xml.cs:7` | `ΔToken` | `type Token any` | production, exported (Δ-renamed) |
| `src/core/encoding/xml/package_test_info.cs:27` | `ΔToken` | (same) | test-compilation copy |
| `src/core/plugin/plugin.cs:66` | `Symbol` | `type Symbol any` | production, exported |
| `src/core/internal/reflectlite/all_test.cs:4` | `Loopy` | `type Loopy any` | **test-only**, exported |
| `src/core/archive/tar/reader_test.cs:4` | `TestFileReader_testFnc` | `type testFnc any` (func-local) | **test-only**, function-local lift |
| `src/core/archive/tar/reader_test.cs:5` | `TestFileReader_fileMaker` | `type fileMaker any` (func-local) | test-only, lift |
| `src/core/archive/tar/writer_test.cs:4` | `TestWriter_testFnc` | `type testFnc any` (func-local) | test-only, lift |
| `src/core/archive/tar/writer_test.cs:5` | `TestFileWriter_testFnc` | `type testFnc any` (func-local) | test-only, lift |
| `src/core/archive/tar/writer_test.cs:6` | `TestFileWriter_fileMaker` | `type fileMaker any` (func-local) | test-only, lift |

So the whole 191-site population is generated by **7 production Go types** —
`crypto.PublicKey`, `crypto.PrivateKey`, `crypto.DecrypterOpts`, `database/sql/driver.Value`,
`encoding/json.Token`, `encoding/xml.Token`, `plugin.Symbol` — plus `internal/reflectlite.Loopy`
and `archive/tar`'s three function-local types on the test side.

**The exported names, in full** (deliverable's "list the exported ones by name"): `PublicKey`,
`PrivateKey`, `DecrypterOpts`, `Value`, `Token` (json), `Token` (xml), `Symbol`, `Loopy`
(test-only). There are no others — the census's other 150 core lines and all 25 behavioral lines
are consumer-side re-exports of these same names.

**The 150 imported re-export sites** are the `<ImportedTypeAliases>` mechanism, not
`visitTypeSpec`: every consumer package re-declares the alias so the imported name resolves.
They travel as a **two-hop published chain** — `<pkg>ꓸ<Name>` and `<pkg>ꓸΔ<Name>` both emitted
where a Δ-rename is in play (`jsonꓸToken` + `jsonꓸΔToken`), matching the chain
`GoTypeAlias("Token","ΔToken")` → `GoTypeAlias("ΔToken","object")` the `encoding/xml` CS0426 arc
already documented.

### 1b. Per-package counts, `src/core` (39 packages, 166 lines)

| Package | lines | Package | lines | Package | lines |
|---|--:|---|--:|---|--:|
| crypto/tls | 8 | archive/tar | 5 | net/netip | 2 |
| crypto/internal/hpke | 8 | net/rpc/jsonrpc | 4 | net/http | 2 |
| crypto/ed25519 | 8 | math/big | 4 | net | 2 |
| crypto/sha512 | 6 | log/slog | 4 | mime/multipart | 2 |
| crypto/sha256 | 6 | internal/coverage/cfile | 4 | internal/trace/traceviewer | 2 |
| crypto/sha1 | 6 | html/template | 4 | internal/platform | 2 |
| crypto/rsa | 6 | expvar | 4 | internal/fuzz (+3 per-GOOS) | 2 each |
| crypto/md5 | 6 | encoding/xml | 4 | go/doc/comment | 2 |
| crypto/internal/boring | 6 | encoding/json | 4 | database/sql/driver | 2 |
| crypto/ecdsa | 6 | database/sql | 4 | time | 2 |
| crypto/ecdh | 6 | vendor/…/x/crypto/sha3 | 3 | testing/slogtest | 2 |
| crypto/x509 (+3 per-GOOS) | 3 each | crypto | 3 | plugin | 2 |
| | | | | os/exec | 2 |
| | | | | net/url | 2 |
| | | | | internal/reflectlite | 1 |

(The per-GOOS rows — `crypto/x509/{windows,linux,darwin}`, `internal/fuzz/{windows,linux,darwin}` —
are layout-L3 folders of the same package; they are counted once in the 39-package figure.)

### 1c. Class (ii) — `global using <Name> = <non-empty interface>;`

**This class EXISTS as an emission shape and has ZERO instances in the Go 1.23.12 standard
library.** `visitTypeSpec` routes `type Reader io.Reader` down the same branch — its own comment
names that case explicitly — and emits the alias to the rendered interface rather than to
`object`. The live probe (§4) reproduces it: `type namedIface fmt.Stringer` emits

```csharp
global using namedIface = go.fmt_package.Stringer;
```

The measurement that it does not occur in the stdlib comes from the source-side instrument (§2),
positive-controlled in six directions before its zero was believed.

**Class (ii)'s divergence is WORSE than class (i)'s**, and this is measured, not argued: class (i)
answers an *empty* name (visibly wrong, and Go's own answer for a genuinely unnamed type), while
class (ii) answers a *confidently wrong* one —

```
Go : Name="namedIface"  String="main.namedIface"  PkgPath="main"
C# : Name="Stringer"    String="fmt.Stringer"     PkgPath="fmt"
```

— a silent misattribution to a different package. No consumer can tell it from the truth. It is
latent in the corpus and live for any `-recurse` end-user module.

### 1d. Other alias forms for defined-over-interface types

None. `visitTypeSpec`'s defined-over-interface branch has exactly two outcomes: `object` when the
underlying interface is empty (`visitTypeSpec.go:107-108`), and the rendered interface type
otherwise. The `global using` histogram's other high-frequency targets
(`go.io.fs_package.FileMode` ×190, `go.os_package.ΔSignal` ×187, …) are **Go type ALIASES**
(`type DirEntry = fs.DirEntry`) and imported-type re-exports — a different mechanism, and correct:
Go itself gives an alias no name of its own.

---

## 2. The census (b) — an independent second derivation from the Go SOURCES

Instrument: `tne-census`, ~200 lines of Go using **`go/parser` only** (no `go/types`, no
`go/packages`, and deliberately no go2cs artifact of any kind), walking
`$GOROOT/src` and reproducing `visitTypeSpec`'s predicate syntactically:

> `typeSpec.Assign` is invalid (a DEFINED type, not a Go alias) **and** `typeSpec.Type` is
> `*ast.Ident` or `*ast.SelectorExpr` (a NAMED right-hand side) **and** that named type's
> underlying type is an interface

then splitting by empty / non-empty and resolving named RHS transitively (same-package by name,
cross-package through the file's import table).

**Result: 18 candidates, 18 empty, 0 non-empty.**

| Package | Name | RHS | test? | func-local? | exported? | File:line |
|---|---|---|:--:|:--:|:--:|---|
| archive/tar | fileMaker | any | ✔ | ✔ | | reader_test.go:1386 |
| archive/tar | fileMaker | any | ✔ | ✔ | | writer_test.go:1076 |
| archive/tar | testFnc | any | ✔ | ✔ | | reader_test.go:1373 |
| archive/tar | testFnc | any | ✔ | ✔ | | writer_test.go:72 |
| archive/tar | testFnc | any | ✔ | ✔ | | writer_test.go:1063 |
| crypto | DecrypterOpts | any | | | ✔ | crypto/crypto.go:223 |
| crypto | PrivateKey | any | | | ✔ | crypto/crypto.go:176 |
| crypto | PublicKey | any | | | ✔ | crypto/crypto.go:162 |
| database/sql/driver | Value | any | | | ✔ | driver/driver.go:62 |
| encoding/json | Token | any | | | ✔ | json/stream.go:289 |
| encoding/xml | Token | any | | | ✔ | xml/xml.go:52 |
| internal/reflectlite | Loopy | any | ✔ | | ✔ | all_test.go:514 |
| net/http | http2WriteScheduler | any | | | | omithttp2.go:67 |
| plugin | Symbol | any | | | ✔ | plugin/plugin.go:120 |
| reflect | Loopy | any | ✔ | | ✔ | all_test.go:1033 |
| reflect | structFieldType | any | ✔ | ✔ | | all_test.go:5399 |
| syscall | Sockaddr | any | | | ✔ | syscall/net_fake.go:50 |
| unique | testEface | any | ✔ | | | unique/handle_test.go:22 |

**The two censuses agree.** Every source-side row that the default Windows build selects and whose
test sources are committed appears in §1a, and vice versa. The four source-side rows with no
emission counterpart are each accounted for: `net/http.http2WriteScheduler` is behind the
`nethttpomithttp2` build tag; `syscall.Sockaddr`'s `net_fake.go` is `js/wasm` (the real
`syscall.Sockaddr` on windows/linux is an inline non-empty interface, a different construct);
`reflect`'s two and `unique.testEface` are in packages whose test sources are **not banked**, so
nothing of theirs is committed. The instrument scans all files regardless of build tag, which is
why it over-reports by exactly those two — a known and deliberate scope difference.

### Positive control (the zero is a measurement, not a dead detector)

`nonempty=0` is a strong claim, so the instrument was run against a synthetic tree carrying every
shape it must and must not report:

| Control declaration | Required | Reported |
|---|---|---|
| `type ctrlNonEmptySameFile Local` (`Local` is a non-empty iface) | nonempty | **nonempty** ✔ |
| `type ctrlNonEmptySelector io.Reader` | nonempty | **nonempty** ✔ |
| `type ctrlEmptyAny any` | empty | **empty** ✔ |
| `type ctrlEmptySelector io.Any` | empty | **empty** ✔ |
| `type ctrlAliasAny = any` (a Go ALIAS) | not reported | **not reported** ✔ |
| `type ctrlStruct S` (RHS not an interface) | not reported | **not reported** ✔ |
| `type ctrlInlineIface interface{ Foo() }` (inline definition) | not reported | **not reported** ✔ |

`TNE-CENSUS total=4 empty=2 nonempty=2`. The detector fires in both directions and excludes all
three near-misses, so the stdlib's `nonempty=0` is real.

---

## 3. Where the emission decision lives

**`src/go2cs/visitTypeSpec.go`** — one function, three load-bearing spots.

**(1) The predicate, lines 37-54.** The rationale is recorded at the site and is worth quoting in
full because it is the reason the alias exists at all:

> ```go
> // A DEFINED type over an INTERFACE (`type Token any`, `type Reader io.Reader`) has EXACTLY the
> // interface's method set and can carry no methods of its own (Go forbids an interface receiver),
> // so it is emitted as a `global using` alias to that interface — the SAME form as a real Go type
> // alias below — never a `[GoType] partial struct` wrapper. A struct wrapper over `any` (= object)
> // admits no implicit conversion FROM a concrete value (C# bars user-defined conversions from
> // object), so every `StartElement → Token` assignment was CS0029 (encoding/xml's `type Token
> // any`, ×16). Restricted to a NAMED-type RHS (Ident/Selector); an inline interface DEFINITION
> // (`type X interface{…}`) is an *ast.InterfaceType and still emits a C# interface via the switch.
> definedOverInterface := false
>
> if !typeSpec.Assign.IsValid() {
>     switch typeSpec.Type.(type) {
>     case *ast.Ident, *ast.SelectorExpr:
>         if _, isIface := identType.Underlying().(*types.Interface); isIface {
>             definedOverInterface = true
>         }
>     }
> }
> ```

**(2) The collapse to `object`, lines 103-108.**

> ```go
> // The empty interface target (`type X any` / `type X = any` / `type X interface{}`) renders
> // as `go.any`, which does not resolve in a using-alias RHS (any is a csproj-level alias, and
> // the safe-name rewrite below deliberately skips `.`-qualified names) — it IS `object`. Emit
> // object directly (encoding/xml's `type Token any`).
> if iface, ok := typeSpecType.Underlying().(*types.Interface); ok && iface.Empty() {
>     typeName = "object"
> }
> ```

**(3) The write, line 154, and the metadata record, lines 156-163.**

> ```go
> v.typeAliasDeclarations.WriteString(fmt.Sprintf("global using %s = %s;%s", aliasName, typeName, v.newline))
>
> // Add exported type aliases to package info. Never for a function-local declaration: it is
> // not part of the package's exported surface whatever its Go name looks like, and the lift
> // above means the name a consumer would import does not exist.
> if !v.inFunction && getAccess(name) == "public" {
>     exportedTypeAliases[name] = typeName
> }
> ```

**Also participating:**

* **`src/go2cs/visitTypeSpec.go:135-152`** — the function-local lift
  (`liftLocalTypeDeclName`). A `global using` is compilation-scoped while a Go local type is
  function-scoped, so `archive/tar`'s three `type testFnc any` declarations become
  `TestWriter_testFnc` / `TestFileWriter_testFnc` / `TestFileReader_testFnc`. This is why the
  banked `archive/tar` row's own roster description says *"Its table-driven suite is written on
  function-local `any` types, which is what put all 97 verdicts behind one alias emission."*
* **`importOperations.go` / the `<ImportedTypeAliases>` block** — re-emits each exported alias in
  every consumer file. This mechanism, not `visitTypeSpec`, produces 150 of the 166 core lines.
* **`package_info.cs`** — `[assembly: GoTypeAlias("<GoName>", "<C# target>")]`, one per exported
  alias. **Measured at `src/core/crypto/package_info.cs:30-32`:**
  ```csharp
  [assembly: GoTypeAlias("DecrypterOpts", "object")]
  [assembly: GoTypeAlias("PrivateKey", "object")]
  [assembly: GoTypeAlias("PublicKey", "object")]
  ```
  **Three distinct Go names mapping onto one C# target, in one assembly.** This record therefore
  exists but is *not invertible*: no runtime `object → Go name` lookup can be written, in this
  package or any other. That single measurement rules out an entire family of "recover the name at
  runtime" designs before any of them is attempted. Unexported and function-local aliases
  (`testEface`, `Loopy`, `tar`'s three) get **no record at all** — line 159's guard — so for the
  motivating row there is not even an ambiguous mapping to consult.

---

## 4. What reflect actually reads — the trace, and where the name is already gone

### 4a. The chain, site by site

| Step | Site | What happens to a named empty-interface type |
|---|---|---|
| 1. Emission | `visitTypeSpec.go:107-108,154` | Go name → the literal token `object` in a `global using` |
| 2. **C# compilation** | Roslyn alias substitution | **The name is erased here.** A `using` alias is compile-time only; it leaves **no metadata whatsoever** in IL. Every use site becomes `System.Object`. |
| 3. `reflect.TypeFor<T>()` | `src/core/reflect/type.cs:2082` | `T` is `object`; falls through to `TypeOf(((ж<T>)nil)).Elem()` |
| 4. `reflect.TypeOf(any i)` | `src/core/reflect/type.cs:1154` | `return toType(abi.TypeOf(i));` |
| 5. **Descriptor synthesis** | `src/core/internal/abi/type_impl.cs:90-96, 138-168` | `synthType(System.Type st, …)` interns on `(st, dimsKey)` and calls `synthesizeDescriptor`. **`st` IS `typeof(object)`.** `t.sysType = st` (`:164`) is the only identity the descriptor ever carries. |
| 6. Named bit | `type_impl.cs:161` | `if (GoReflect.HasGoName(st)) t.TFlag \|= TFlagNamed;` |
| 7. `HasGoName` | `src/core/golib/GoReflect.TypeNaming.cs:243-244` | `if (t == typeof(object)) return false;` — *"Go's empty interface is an unnamed type."* |
| 8. `abi.Type.HasName()` | `src/core/internal/abi/type.cs:147` | reads `TFlagNamed` → **false** |
| 9. `rtype.Name()` | `src/core/reflect/value_impl.cs:2075-2082` | `if (!GoReflect.HasGoName(st)) return "";` → **`""`** |
| 10. `rtype.String()` | `src/core/reflect/value_impl.cs:2063-2065` | `GoReflect.GoTypeName(sysType, …)` |
| 11. `GoTypeName` | `GoReflect.TypeNaming.cs:141` | `if (t == typeof(object)) return "interface {}";` → **`"interface {}"`** |
| 12. `rtype.PkgPath()` | `src/core/reflect/value_impl.cs:2089-2091` | `GoReflect.GoPackagePath(sysType)` → **`""`** |
| 13. `Kind()` | `GoReflect.cs:244` | `if (t == typeof(object)) return Interface;` → **`interface`, correct** |
| — mini-bridge mirror | `src/core/internal/reflectlite/type_impl.cs:255, 278` | same two gates, same answers |

### 4b. The exact points where a name would be needed, and where nothing is left

**Where a name would be needed** — every descriptor minted from a *static* Go type. Each is a
distinct call into `synthType` and each is a separate place a fix must reach:

| Position | Minting site |
|---|---|
| Struct **field** descriptors | `internal/abi/type_impl.cs:314` — `Typ: synthType(info.Type, dims, null, fieldDir, fieldKeyDims)` |
| Func **param / result** descriptors | `internal/abi/type_impl.cs:414` — `descriptors[i] = synthType(side[i], paramDims)` |
| **Elem** (slice / array / pointer / chan) | `internal/abi/type_impl.cs:471`, `:552-553` |
| Map **key** | `internal/abi/type_impl.cs:519` — `synthType(key, Ꮡt.Value.keyDims)` |
| `TypeFor` / `TypeOf` from a static type | `reflect/type.cs:2082`, `:1154` |

**Where nothing is left to recover** — one sentence, and it is the whole design constraint:

> By step 5 the only identity in hand is `System.Type st`, and for every named empty-interface type
> in the corpus `st` is the single CLR type `System.Object`. The erasure happened at step 2, inside
> the C# compiler; there is no attribute, no modreq, no distinct handle. `crypto`'s own
> `package_info.cs` proves the mapping is many-to-one (three Go names, one target), so the loss is
> not merely unrecorded — it is **not invertible in principle**.

The corollary for class (ii) is the same shape with a different victim: `st` is
`go.fmt_package.Stringer`, a perfectly good CLR type that carries `fmt.Stringer`'s name — so the
bridge reconstructs a *real* name that belongs to a *different* Go type.

---

## 5. The consumer census (d) — who observes the name

### 5a. The probe — positive control for the entire census

A 40-line Go program exercising every reflect-visible position, converted by the **real converter
built from this head** (`go2cs -go2cspath <worktree>/src <probe-dir>`), compiled against the
worktree corpus (`-p:go2csPath=<worktree>/src/`), and run. Go oracle from `go run .` on the same
sources, Go 1.23.12.

Emitted C# (verbatim, first two lines of `main.cs`) — **both classes in one emission**:

```csharp
global using testEface = object;
global using namedIface = go.fmt_package.Stringer;
```

| Position | Go | C# | |
|---|---|---|:--:|
| `TypeFor[testEface]` | `Name="testEface" String="main.testEface" PkgPath="main" Kind=interface` | `Name="" String="interface {}" PkgPath="" Kind=interface` | ✗ |
| `TypeFor[namedIface]` | `Name="namedIface" String="main.namedIface" PkgPath="main"` | `Name="Stringer" String="fmt.Stringer" PkgPath="fmt"` | ✗ |
| `holder.Field(0)` (field descriptor) | `main.testEface` | `interface {}` | ✗ |
| `holder.Field(1)` | `main.namedIface` | `fmt.Stringer` | ✗ |
| `take In(0)` (param descriptor) | `main.testEface` | `interface {}` | ✗ |
| `take Out(0)` (result descriptor) | `main.namedIface` | `fmt.Stringer` | ✗ |
| `map key` | `main.testEface` | `interface {}` | ✗ |
| `map elem` | `main.testEface` | `interface {}` | ✗ |
| `slice elem` | `main.testEface` | `interface {}` | ✗ |
| `%T` of a nil value | `<nil>` | `<nil>` | ✔ |
| **generic** `named[T]()` | `named[testEface]="testEface" named[any]="" named[namedIface]="namedIface"` | `named[testEface]="" named[any]="" named[namedIface]="Stringer"` | ✗ |
| `Kind()` everywhere | `interface` | `interface` | ✔ |

**9 of 9 static positions diverge. `Kind()` and `%T`-of-a-value are correct** — `%T` reads the
*dynamic* type, which is untouched, and that is exactly why `archive/tar` banks 97/97 while
carrying five of these aliases: its `testFnc`/`fileMaker` types are only ever slice-element and
field types that nothing reflects a *name* out of.

**The generic row is the decisive measurement of this census.** The emitted call is

```csharp
genericˢ, named<testEface>(), named<any>(), named<namedIface>());
```

`testEface` and `any` are both aliases for `object`, so `named<testEface>` and `named<any>` are
**the same CLR instantiation** — two distinct Go instantiations answering `"testEface"` and `""`
collapse into one method the runtime cannot tell apart. No amount of call-site metadata can
separate them; only a distinct **type argument** can.

### 5b. Who reads a name, in the standard library

`reflect.TypeFor[…]` census over `$GOROOT/src`: **122 sites**. Exactly **2** pass a *type
parameter*, and both are in `unique`:

* `unique/handle_test.go:51` — `name := reflect.TypeFor[T]().Name()`, then
  `t.Run(fmt.Sprintf("%s/%#v", name, value), …)` (`TestHandle`).
* `unique/clone_test.go:29` — `typName := reflect.TypeFor[T]().Name()`, then `t.Run(typName, …)`
  (`TestMakeCloneSeq`).

The other 120 pass a concrete or ordinary named type and are unaffected.

**Other name-observing consumers found:**

* `reflect/all_test.go:5399-5407` — `type structFieldType any`;
  `Type: TypeOf((*structFieldType)(nil)).Elem()` fed to `StructOf` and compared against
  `struct{ F structFieldType }{}`. A *static-position* consumer in the package the reflect arc is
  actively working. `reflect` is not banked, so this is a future row's problem, named here.
* `reflect/all_test.go:1033` and `internal/reflectlite/all_test.go:514` — `type Loopy any`, used
  only as a variable type inside `DeepEqual` cycle tables. **Not name-observing**: both sides erase
  identically and `DeepEqual`'s type comparison still agrees. `internal/reflectlite` banks 30/30
  with the alias on disk, which is the evidence.
* `archive/tar` reader/writer tests — `testFnc`/`fileMaker` as slice element and struct field
  types only. **Not name-observing.** Banked 97/97.

**`crypto/*` was checked specifically and observes nothing.** `crypto.PublicKey`,
`crypto.PrivateKey` and `crypto.DecrypterOpts` appear in **zero** Go string literals anywhere in
GOROOT, and no test reflects their name. The nine string-literal hits for `driver.Value` are all
hard-coded English in `database/sql/convert.go` error text (`"converting driver.Value type %T …"`)
— the `%T` reads the *dynamic* type, so those match. This is why `crypto/rsa` (559),
`crypto/tls` (3,643), `crypto/x509` (341), `database/sql` (138) and `encoding/json` (491) all bank
today while carrying the erasure: **the defect is real corpus-wide and currently observed in
exactly one place.** That is a statement about today's test corpus, not about the emission —
`-recurse` end-user code can observe it anywhere.

### 5c. Banked-row exposure

37 of the 39 alias-carrying `src/core` packages are banked roster rows, together
**19,169 matching verdicts and 92 disclosed**:

`archive/tar` 97 · `crypto` 6 · `crypto/ecdh` 47 · `crypto/ecdsa` 82 · `crypto/ed25519` 8+1 ·
`crypto/internal/boring` 3 · `crypto/internal/hpke` 19 · `crypto/md5` 11+1 · `crypto/rsa` 559+1 ·
`crypto/sha1` 12+1 · `crypto/sha256` 23+1 · `crypto/sha512` 36+1 · `crypto/tls` 3643+1 ·
`crypto/x509` 341 · `database/sql` 138+2 · `database/sql/driver` 1 · `encoding/json` 491 ·
`encoding/xml` 386 · `expvar` 11 · `go/doc/comment` 10059 · `html/template` 243 ·
`internal/coverage/cfile` 15+1 · `internal/fuzz` 52 · `internal/platform` 1 ·
`internal/reflectlite` 30 · `log/slog` 194+19 · `math/big` 224+2 · `mime/multipart` 52 ·
`net` 472+2 · `net/http` 1343+2 · `net/netip` 210+57 · `net/rpc/jsonrpc` 9 · `net/url` 48 ·
`os/exec` 116 · `plugin` 1 · `testing/slogtest` 17 · `time` 169.

Not banked: `internal/trace/traceviewer`, `vendor/golang.org/x/crypto/sha3`.

---

## 6. Design options

Three facts constrain every option, and all three are measured above:

* **C1 — no C# type other than `object`/`dynamic` accepts every value implicitly.** The C# spec
  forbids a user-defined conversion whose source or target is `object`, or an interface type; a
  marker interface would require *every* type in *every* assembly to implement it. This is not a
  deduction — it is the CS0029 ×16 that `visitTypeSpec.go:40-43` records for `encoding/xml`.
* **C2 — the name is erased by the C# compiler, before IL, and the loss is not invertible.**
  `crypto/package_info.cs:30-32`: three Go names, one C# target, one assembly.
* **C3 — generic instantiation collapses distinct Go instantiations into one CLR instantiation.**
  Measured: `named<testEface>` and `named<any>` are the same method.

### Option 1 — name-carrying metadata at reflect-visible sites only (keep the alias)

**Mechanism.** The converter knows the Go static type at every descriptor-minting position (`go/types`
resolves `unique.testEface` to a `*types.Named` however the alias renders). Emit the Go name
alongside: a `TypeForNamed`-style overload at direct `reflect.TypeFor[X]()` /
`reflect.TypeOf((*X)(nil)).Elem()` sites; a per-field / per-parameter Go-type-name stamp read by
`synthType`'s field walk (`type_impl.cs:314`), its param walk (`:414`), and its key/elem arms
(`:471`, `:519`) — the same *dims-cargo* pattern `GoArrayDimsAttribute` / `GoMapKeyDimsAttribute`
already use for exactly this class of "the CLR type cannot carry it" problem.

**Preserves.** Assignability exactly — not one value-level byte changes. Every banked row's runtime
behavior is untouched. Zero risk to the 19,169 verdicts.

**Loses.** The generic case (C3), measured. `unique` — the row that commissioned this census — does
**not** move. Also anything reached from a value rather than a static position.

**Blast radius.** Converter + golib descriptor synthesis; emission changes only where such a
descriptor is minted. Cross-package alias *uses* measured at **60** (`crypto` 30, `driver` 30;
`json`/`xml`/`plugin` re-exports have **0** real uses — they are declaration-only), plus
declaring-package uses. Re-goldening owed for the touched behavioral projects.

**Gates.** Converter `go test ./...` · CNR · `go2cs-stdlib.slnx` (windows + linux) · full
behavioral (a `go2cs-gen`-adjacent change would additionally owe route #7's cross-assembly
consumer gate) · the five reflect-importer canaries recomputed at gate time · post-merge filtered
sweeps of every banked row whose emission moved.

### Option 2 — emit a real marker type

**Mechanism.** Make `type X any` a real CLR type. Two sub-variants, both dead for the same reason
(C1):

* *(a) an empty C# interface* — `int` does not implement it, so every assignment is CS0029. The
  converter would have to insert conversions, and cross-assembly extension is impossible: a
  consumer's own type cannot be made to implement a marker declared in another assembly without
  `go2cs-gen` minting an adapter for **every type in the program**.
* *(b) a `readonly struct` wrapper over `object`* — a user-defined conversion *from* `object` is
  spec-forbidden. This is precisely the shape `visitTypeSpec.go:40-43` already tried and recorded
  as CS0029 ×16.

Either way the converter must insert an explicit wrap/unwrap at **every** assignment, argument,
return, comparison, type switch, composite literal, map/slice element write, `append`, variadic
spread and closure capture — in the corpus, in every hand-owned `*_impl.cs`, in golib, **and in
every end-user program go2cs will ever convert**.

**Preserves.** The name, hence all of reflect including the generic case.

**Loses.** Free assignability — the single property the alias exists to provide. Introduces a new
conversion-insertion class with a large unknown-shape tail.

**Blast radius.** 39 packages, 37 banked rows, 19,169 matching verdicts + 92 disclosed all at
risk; unbounded in end-user code.

**Verdict: reject.** It is the option that makes the CS0029 storm the current emission was written
to escape, at 40× the scale.

### Option 3 — the hybrid: keep the alias, add an uninhabited DESCRIPTOR CARRIER

**Mechanism.** Emit **both**:

1. `global using testEface = object;` — unchanged. Storage, assignability, IL: all identical to
   today. Zero value-level risk, by construction.
2. **A carrier**: a real, *uninhabited* C# interface declared in the same `<pkg>_package` class,
   under a non-colliding C# identifier, stamped with the Go name:
   ```csharp
   [GoLocalName("testEface")] internal interface testEfaceᴰ { }
   ```
   Nothing ever implements it, nothing is ever assigned to it, no value is ever of this type. It
   exists solely to be a `System.Type` the descriptor bridge can name.

The converter then substitutes the **carrier** at exactly the positions where a descriptor is
minted from a static Go type: `TypeFor` / `TypeOf`-of-a-static-type call sites, and — via the
existing dims-cargo attribute pattern — struct fields, func params/results, map keys and elems.

**Why this is cheap: the golib side already works, unchanged.** Every reconstruction the bridge
performs answers correctly for such a carrier *today*, and each was read at this head:

| Reader | Site | Answer for the carrier |
|---|---|---|
| `KindOf` | `GoReflect.cs:290` — `if (t.IsInterface) return Interface;` | `interface` ✔ |
| `HasGoName` | `GoReflect.TypeNaming.cs:237-285` | falls through every exclusion arm → `true` ✔ |
| `goBareTypeName` | `GoReflect.TypeNaming.cs:596` — *"prefers its stamped original Go name (`[GoLocalName]`)"* | `testEface` ✔ |
| `GoQualifiedName` | `GoReflect.TypeNaming.cs:581` — nesting in `<pkg>_package` | `unique.testEface` ✔ |
| `GoPackagePath` | `GoReflect.TypeNaming.cs:662` — via `[GoPackage]` on the declaring class | `unique` ✔ |
| `TFlagNamed` | `abi/type_impl.cs:161` (gated on `HasGoName`) | set ✔ |

So Option 3's golib delta is plausibly **zero**, and its converter delta is emission-side only.
`[GoLocalName]` is deliberately the existing attribute for "the C# identifier is not the Go name",
which is the same problem one step over.

**The generic case (C3) — the one place Option 3 must do more.** Because the carrier is a real
distinct type, it *can* travel as a type argument, which is the only mechanism C3 leaves open: a
converted generic function whose body reads a type parameter's name gains a companion type
parameter (`f<T>` → `f<T, TName>`), instantiated at each call site with the carrier (or with a
canonical unnamed carrier for `any`). **The measured price of this sub-mechanism in the whole
standard library is 2 functions** — `unique.testHandle` and `unique.testCloneSeq` — out of 122
`TypeFor` sites. It needs a fixed-point pass over the generic call graph (if `f[T]` calls `g[T]`
which reads the name, `f` must thread too), and it changes the C# signature of any generic function
it touches, which is real cross-assembly surface.

**Preserves.** Assignability exactly (the carrier is never a value); `Kind()`; every banked row's
runtime behavior; the `-tests` graph invariant (the carrier lives in the declaring package, which
every consumer already references — **no new project reference, no new cycle**).

**Loses.** Nothing measured. The honest residual is the unknowns in §7.

**Blast radius.** Declaring packages gain a type ⇒ their `package_info.cs` records move ⇒
**`go generate .` in `src/go2cs` is owed** (`stdlib-metadata.txt`, gated by
`TestStdLibMetadataInSync`), and the merge preflight for `package_info.cs` ⇒
`stdlib-metadata.txt` applies. Consumer files change only where a carrier is referenced.

**Gates.** Same ladder as Option 1, plus: `check-solution-integrity.ps1`'s per-GOOS cycle
assertion (the carrier must add no edge); a behavioral **compile** phase if `go2cs-gen` is touched
at all (route #7); and — because the carrier is a bare interface nested in a `<pkg>_package` class
— an explicit check that `ImplementGenerator` does **not** mint witnesses or adapters for it.

### Recommendation

**Take Option 3, staged, with Option 1 as its own first increment.**

The staging falls out of the measurements rather than being imposed:

* **Stage A — the carrier and the static positions** (Option 1's coverage, Option 3's mechanism).
  Fixes 9 of the 9 measured divergent positions *except* the generic one, for both class (i) and
  class (ii). Golib delta plausibly zero. Assignability provably untouched, because no value-level
  emission changes at all. This is the increment that closes the *corpus-wide reflect-fidelity*
  half — `crypto.PublicKey` in a field descriptor, `driver.Value` in a param descriptor,
  `reflect`'s own `structFieldType`-in-`StructOf` consumer — and it is also the increment that
  fixes class (ii)'s silent misattribution, which is the more dangerous of the two defects and has
  **zero** current instances, hence zero regression risk.
* **Stage B — the type-parameter thread.** Two functions in the stdlib, both in `unique`, and it is
  what actually moves the commissioning row. Costs a call-graph fixed point and a generic-signature
  change; sized honestly, it is the smaller-value and higher-risk half, which is why it goes second
  and not first.

Why not Option 1 alone: it cannot move `unique`, measured (C3), and the census was commissioned by
`unique`. Why not Option 2: it destroys the property the emission exists for, at 37 banked rows.
Option 3 is the only shape found that carries a name **and** keeps free assignability, and it does
so by refusing to make the name and the value the same type — which is, on the evidence, the actual
insight: **the value must stay `object`; only the descriptor needs an identity.**

---

## 7. What could NOT be established

1. **The board's 167 vs this head's 166.** Not reconciled. The board's instrument is not recorded
   and its tree is not this one. Nothing turns on it.
2. **Whether `go2cs-gen`'s `ImplementGenerator` / `TypeGenerator` leave an uninhabited nested
   interface alone.** Not probed — it needs a generator change to test properly, and this arc was
   scoped census-and-design. It is Option 3's single largest unknown and its first experiment.
3. **The transitive depth of the Stage-B threading pass.** Measured at the leaves (2 functions,
   both reading directly); the fixed point over generic-to-generic call chains was not computed.
   The stdlib almost certainly has depth 1 here, but "almost certainly" is not a measurement.
4. **Whether a converted generic function's signature change is safe across assemblies at scale.**
   Not probed.
5. **`unique`'s post-fix arithmetic** is a derivation, not a re-measurement — see §8.
6. **End-user (`-recurse`) exposure.** Only the stdlib was censused. Class (ii) has zero stdlib
   instances but is live for any end-user module, and its failure mode is a *wrong* name, so its
   real-world frequency is unmeasured and its severity is higher than class (i)'s.
7. **No fix was cut.** Per the commission, this arc is census + design only.

---

## 8. The `unique` rows expected to move (f)

From the board entry's own measured table (its numbers, not re-run here): the C# host produced
**20 terminal rows — 8 pass, 12 fail**; Go produced 20; **7 match**.

The three naming pairs, each with no shared row today:

| Go row | C# row today | After the fix |
|---|---|---|
| `TestHandle/testEface/<nil>` | `TestHandle//<nil>` | one named row, Go pass / C# fail |
| `TestHandle/testEface/"hello"` | `TestHandle//"hello"` | one named row, Go pass / C# fail |
| `TestMakeCloneSeq/testEface` | `TestMakeCloneSeq/#00` | one named row, **pass/pass** |

**A correction the board's own arithmetic supports.** The board projects *"20/20 with 10
disclosed"*. Its measured C# split says otherwise: 8 C# passes, of which 7 are the matched
`TestMakeCloneSeq` parent + 6 subtests — so the 8th C# pass can only be `TestMakeCloneSeq/#00`,
and the 12 C# failures are `TestHandle` parent + all 10 of its subtests + `TestMakeClonesStrings`.
Renaming therefore lands **8 matching + 12 codegen-liveness disclosures = 20/20**, not 10
disclosed: the two `TestHandle/testEface/*` subtests join the disclosure set rather than
disappearing from it. The fixing lane must confirm this from its own run — it is a derivation from
the board's numbers, and §7 lists it as such.

**Other banked rows whose emission a fix would touch** — the banked-row rule applies, so each owes
a **post-merge filtered sweep at the merge RESULT**, not at the lane tip:

* **Certain to move** (they declare a named-eface type, so the carrier lands in their own files):
  `crypto` 6 · `database/sql/driver` 1 · `encoding/json` 491 · `encoding/xml` 386 · `plugin` 1 ·
  `internal/reflectlite` 30 · `archive/tar` 97 (five function-local lifts).
* **Move if consumer-side descriptor sites are rewritten** (they hold real uses of a re-exported
  alias — measured 60 cross-package uses, concentrated in `crypto` 30 and `driver` 30): the
  `crypto/*` family (`ecdh` 47, `ecdsa` 82, `ed25519` 8+1, `internal/boring` 3, `internal/hpke` 19,
  `md5` 11+1, `rsa` 559+1, `sha1` 12+1, `sha256` 23+1, `sha512` 36+1, `tls` 3643+1, `x509` 341) and
  `database/sql` 138+2.
* **Declaration-only carriers** (they hold the alias but zero real uses — `jsonꓸToken`/`ΔToken`,
  `xmlꓸToken`, `pluginꓸSymbol`): `expvar` 11 · `go/doc/comment` 10059 · `html/template` 243 ·
  `internal/coverage/cfile` 15+1 · `internal/fuzz` 52 · `internal/platform` 1 · `log/slog` 194+19 ·
  `math/big` 224+2 · `mime/multipart` 52 · `net` 472+2 · `net/http` 1343+2 · `net/netip` 210+57 ·
  `net/rpc/jsonrpc` 9 · `net/url` 48 · `os/exec` 116 · `testing/slogtest` 17 · `time` 169. These
  should NOT move under Option 3 (nothing is emitted for an unused import alias) — and that
  prediction is itself a cheap CNR check the fixing lane gets for free.

`crypto/tls` is in the second group and is both a reflect-importer canary and the corpus's largest
row; `encoding/json` (491), `encoding/xml` (386) and `crypto/x509` (341) are canaries too. The
canary set is **recomputed from the roster at gate time, never carried from this document.**

---

## 9. Reproducing this census

```
# (a) the emission census - git grep, NOT bare rg, and the identifier class must admit Δ
git grep -n -E '^[[:space:]]*global using [^ =]+[[:space:]]*=[[:space:]]*object[[:space:]]*;' -- '*.cs'

# (b) the independent source-side census - go/parser only, positive-controlled first
#     (instrument built at <scratch>/tne-census; see §2 for the control tree)
tne-census.exe "$GOROOT/src"

# (c) the probe - the positive control for the whole record
go2cs -go2cspath <worktree>/src <probe-dir>
dotnet build <probe-dir>/TneProbe.csproj -c Debug -p:go2csPath=<worktree>/src/
```

Environment for every step: `GOROOT=<sdk-root>\go1.23.12` in its **backslash** spelling
(a forward-slash GOROOT misroutes emission into `namespace go.std.*`), .NET 10 first on PATH,
`MSBUILDDISABLENODEREUSE=1`. Verified with bare `go version` → `go1.23.12` and
`dotnet --version` → `10.0.400` before any number here was believed.
