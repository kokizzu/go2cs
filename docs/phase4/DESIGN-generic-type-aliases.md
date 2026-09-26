# DESIGN — Go 1.24 generic type aliases (`type A[T any] = B[T]`)

**Status: for ruling. No cut proposed.** Written for the H4 item queue at COORD's direction: *"a
DESIGN RECORD, not a cut … sized on a population rather than a feature list."*

Author: R, 2026-09-07. Measured against the pinned `go1.24.13` and `go1.23.12` toolchains and
against .NET 10 / C# latest.

---

## 1. THE POPULATION IS ZERO, AND THAT IS THE HEADLINE

Go 1.24's headline language change does not appear anywhere in the tree go2cs converts.

```
  TREE                          FILES   plain aliases   generic DEFS   GENERIC TYPE ALIASES
  go1.23.12  src (std)           3580        135             38                0
  go1.24.13  src (std)           3840        147             43                0
  go1.24.13  src/cmd             1375          7             16                0
```

`cmd/` is included as a population data point only — go2cs never converts it.

**Derivation, and why it is not a regex.** A generic type alias is an `*ast.TypeSpec` with **both**
`Assign` valid (it is `=`, an alias, not a definition) **and** `TypeParams` non-empty. Three shapes
share the surface syntax and only the AST separates them:

```go
type A[T any] = B[T]     // generic ALIAS        <- the subject
type A        = B        // plain alias          Assign set, TypeParams nil
type A[T any] struct{}   // generic DEFINITION   TypeParams set, Assign unset
```

⚠ **The census was positive-controlled before its zeros were quoted**, on a fixture carrying all
three shapes plus a two-parameter alias and a test-file alias: it reported **3 generic aliases
(2 production / 1 `_test.go`)**, and counted the plain alias and the generic definition in their own
buckets rather than the subject's. A census that cannot distinguish the shapes it is counting would
report zero for the same reason a broken one does; this one was made to report non-zero first.

## 2. THE FEATURE IS LIVE AT 1.24.13 — the zero is ADOPTION, not availability

`go/types` carries a `GOEXPERIMENT=aliastypeparams` gate (`decl.go:587`) and a
`GODEBUG=gotypesalias=1` gate (`decl.go:609`), which invites the conclusion that the feature is
switched off. **It is not.** Measured directly rather than read from build tags:

```
  module `go 1.24`, DEFAULT GOEXPERIMENT (empty), go1.24.13
    type Alias[T any] = Box[T]         go build  ->  exit 0     <- compiles
    GOEXPERIMENT=aliastypeparams       go build  ->  exit 0
    control: a PLAIN alias             go build  ->  exit 0     <- the harness builds
```

So the std's zero is a statement about **adoption at 1.24.13**, not about what the language permits.
A converted end-user module (`-recurse`) may contain one on day one.

## 3. C# HAS NO OPEN GENERIC ALIAS — measured, with its control

```
  using Alias<T> = Box<T>;    error CS1002 ';' expected | CS1525 invalid expression term '='
  using AliasInt = Box<int>;  0 errors                                    <- the control
```

C# accepts a using-alias **naming** a constructed generic type; it does not accept an alias that
**declares** type parameters. The closed form compiling is what makes the open form's rejection a
language fact rather than a broken probe.

## 4. THE OPTIONS, WITH THEIR REAL COSTS

### (a) UNALIAS AT THE TYPE LEVEL — emit the target, drop the alias name  ← recommended

`Alias[int]` emits as `Box<int>`. **This is what the converter already does**: `types.Unalias`
appears across **52** converter files, and every alias-resolving path in the corpus today goes
through it.

- **Semantics: exact.** Go's alias IS the target type — same identity, same method set, same
  assignability. Unaliasing preserves all of it because there is nothing else to preserve.
- **Cost: cosmetic only.** Go source reading `Alias[int]` emits C# reading `Box<int>`. That is a
  reads-like-Go regression at the use site, and it is the *entire* price.
- **Work: none.** No converter change; the existing path already produces this.

### (b) CLOSED USING-ALIAS PER INSTANTIATION

Emit `using AliasInt = Box<int>;` for each distinct instantiation.

- Preserves the Go name for **concrete** instantiations only.
- **Cannot express the open form at all**, so any use inside generic code falls back to (a) — the
  emission becomes inconsistent between contexts, which reads worse than (a) does uniformly.
- Scales with the instantiation count, and the alias must be minted in every consuming file
  (C# using-aliases are file-scoped).

### (c) A GENERIC WRAPPER TYPE — `struct Alias<T> { … }`

⚠ **Semantically wrong and not merely costly.** Go's alias is the *same type*; a wrapper is a
*different* type. Assignability between `Alias[int]` and `Box[int]` breaks, `reflect` identity
breaks, and the interface-satisfaction set diverges. This option is listed to be refused, with the
reason, so it is not re-proposed.

## 5. WHAT `go/types` GIVES US UNDER 1.24

`gotypesalias=1` is the 1.24 default, so `go/types` materialises `*types.Alias` nodes rather than
resolving them away at construction; `types.Alias` gained `TypeParams()`/`TypeArgs()` for the
generic case. The converter's existing `types.Unalias` calls continue to answer the target type,
which is precisely what option (a) needs — **no new go/types surface has to be adopted for (a) to
be correct.** A future option (b) would need the alias *name*, which is available from the
`*types.Alias` node, so the door is not closed by taking (a) now.

## 6. RECOMMENDATION

**Take (a), which requires no cut, and revisit only on evidence.** The population is zero in both
the outgoing and incoming corpora, the semantics are already exact, and the only cost is a name at
the use site.

**What would reopen this:** a converted end-user module (`-recurse`) that uses generic aliases
heavily enough that reads-like-Go suffers materially, or a future std adoption. Both are detectable
by re-running the census — `arm13_genalias`, an `go/ast` walk with its fixture control — against the
tree in question. **The census is the trigger, not a feature-list review.**

## 7. WHAT THIS RECORD DOES NOT CLAIM

- It does not claim the converter *handles* a generic alias end to end today. Nothing in either
  corpus exercises that path, so it is **unexercised, not proven** — the same distinction this lane
  drew for `TB.Chdir`. If (a) is ruled, a single behavioural test carrying one generic alias would
  convert that from an argument into a measurement, and that is the cheapest possible follow-up.
- It does not size the `-recurse` exposure, which depends on third-party module adoption nobody has
  measured.

---

## 2026-09-07 — RULED: option (a), unalias at the type level (COORD, `6daa32385`)

Appended, not rewritten: §1–§7 above are the record as posted for ruling, and this block is the
ruling on it.

**The ruling is option (a)** — emit the target and drop the alias name — resting on the three
measured facts in §1–§3 rather than on the preference in §6: the population is **zero** in both std
trees under a `go/ast` census that separates the three surface-identical shapes and was
positive-controlled 3/3 on a fixture; the feature is **live** at go1.24.13 under the default
`GOEXPERIMENT`, so a `-recurse` end-user module can carry one on day one even though std carries
none; and **C# rejects the open alias form** (CS1002/CS1525) while the closed form compiles, which is
a language fact with its own control.

⚠ **The reads-like-Go cost is the FLOOR, not a choice.** Against the end-user goal, a Go alias *is*
its target — identity, method set, assignability — so (a) is **exact** on "runs like Go". The cost
falls entirely on "reads like Go": the alias NAME disappears at use sites. **No C# construct can
carry that name**, so this is not a trade the design gets to make differently; §4(b) and §4(c) do not
buy the name back, they only pay more for the same loss or break the semantics to fake it.

**(c) is refused with the reason on record** — a wrapper is a *different type*, so `reflect`
identity, assignability and interface satisfaction all diverge. It is written down here so it is not
re-proposed by someone reading only the feature list.

### The guard is QUEUED, and the reason it cannot be written yet is the point

§7 says this design is **unexercised, not proven**: nothing in either corpus reaches the path. The
remedy named there — **one behavioural test carrying a generic alias** — is **queued to H4 behind
the H1+H2 pair**, because such a test **cannot build on the outgoing toolchain**: go1.23.12 rejects
the syntax, so the test would fail to compile for a reason unrelated to what it guards.

It converts "unexercised" into a measurement **the day the converter is built by go1.24**, and not
before. Until then the honest status of option (a) is: *semantically exact by argument, already the
converter's behaviour by inspection (`types.Unalias`, 52 files), and unmeasured end to end.*

**Re-running the census is the trigger for reopening**, per §6 — `arm13_genalias`, the `go/ast` walk
with its fixture control, against whatever tree is in question. Not a feature-list review.

---

## 2026-09-26 — MEASURED: option (a) was NOT the converter's behaviour; implemented (C2, `claude/c2-generic-type-alias`)

Appended, not rewritten. The ruling above rests on (a) being "already the converter's behaviour by
inspection (`types.Unalias`, 52 files)". **Measured, it was not.** At master `db1bd885a2`, on a
two-module probe that Go runs:

- each generic alias emitted `global using A = go.pkg.Box<go.T>;`, with its type parameter
  unbound, and published an `[assembly: GoTypeAlias("A", "…<go.T>")]` record. The declaring
  package failed to build (CS0234 on `go.T`, CS0307 on `List<T>` as a parameter type);
- every use named the alias with type arguments (`P<T, T>`, `new lib.Alias<nint>(…)`,
  `IntMap<@string>`), which no C# alias accepts (CS0307);
- an importer turns every exported record into a `global using`, so **one exported generic alias
  broke every consumer of its package**, including one that never names it;
- an alias of an anonymous struct emitted a slash inside a type name, and an alias of a pointer
  `go.go.T`.

The mechanism: go1.24's `go/types` materializes `*types.Alias` with `TypeArgs()`, and the
converter's type-name renderers and its alias declaration treated it as a plain alias. Whatever
the existing `types.Unalias` calls decide, the name a use renders came from the alias itself.

**Implemented as ruled:** the declaration emits a one-line comment keeping the Go text and
nothing else; `genericAliasTarget` renders `types.Unalias(t)` at every use, gated on the alias
having type parameters or arguments, so a plain alias keeps its name byte for byte. The
anonymous-struct/interface target is **not supported**: its declaration emits the comment and a
conversion warning (the reason is in the Reference, *Generic Type Aliases*).

The census that triggers a reopen, re-run for this cut (a `go/ast` walk, controlled 12/12 on the
probe including a `_test.go` alias): **0 generic aliases in GOROOT 1.24.13** (3840 files, all
targets, tests included), and 0 in `src/tests`. The guard §7 asked for exists now: the
`GenericTypeAlias` behavioral test, red at master and matching Go's output with the fix.
