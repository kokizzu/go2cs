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
