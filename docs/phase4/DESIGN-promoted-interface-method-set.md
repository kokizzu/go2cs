# DESIGN — a promoted interface method belongs to the type's GO METHOD SET

*Lane G, 2026-08-23. Branch `claude/implgen-embedded-witness`. Status: **DESIGN, not implemented**.*

F2 of the resolver findings: a type satisfying an interface via an **embedded interface plus
directly-added methods** is not recognized by a converted type assertion. Go takes one arm, the
conversion takes the other. Reported originally as `ImplementGenerator` witness territory; the
root is elsewhere, and this note records where, why the obvious fixes are wrong, and what to build.

## The measurements

Reproduced without `net`, in a two-package module (`iolike` declares `Reader` and `ReadWriter`;
`main` declares `wrapper` embedding `iolike.Reader` and adding `Write`):

```
Go : ReadWriter: yes
C# : ReadWriter: no
```

Four controls, each of which killed a candidate root:

| # | Variant | Result | What it kills |
|---|---|---|---|
| 1 | Same-package `Reader`/`ReadWriter` | **works** | "embedding is the defect" |
| 2 | `plain` — both methods declared directly, foreign interface, **no record** | **works** | "the `GoImplement` record is the mechanism" |
| 3 | Direct call `w.Read()` | **works** (emits `w.Reader.Read()`) | "the promotion is missing" — the *converter* resolves the hop at call sites |
| 4 | `holder` — an ordinary field whose name equals its type's simple name | Go says **no**, and the converter emits **no** `Promoted` record | "the name heuristic can drive this" — it cannot, but the record can |

## The root

`ImplementGenerator` **already** emits the promoted method, from the `Promoted = true` record it
**already** writes:

```csharp
partial struct wrapper : iolike_package.Reader
{
    public @string Read() => Reader.Read();   // a real MEMBER
}
```

`builtin.Implements<T>` tries C# `is T` first — which is why asserting `wrapper` to `Reader`
succeeds — and otherwise falls to a **structural** check over the type's Go method set. That set
is built by `TypeExtensions.GetGoMethodSetCandidates` **exclusively from EXTENSION methods**
(`GetExtensionMethods()` is its only source). The promoted method is a member, not an extension
method, so it is invisible there:

| | `Read` | `Write` | assert `Reader` | assert `ReadWriter` |
|---|---|---|---|---|
| `plain` | extension | extension | — | **yes**, both in the set |
| `wrapper` | generated **member** | extension | **yes**, via C# `is` | **no**, set holds `Write` alone |

**A promoted interface method is a Go method of the type, and it is the one kind of Go method that
never becomes an extension method.** That is the defect, stated once.

## Why the two obvious fixes are wrong

**Widening the candidate list with member `MethodInfo`s breaks its contract.** Every candidate is
assumed to be an extension method whose FIRST PARAMETER is the receiver: `PrefersBindableShape`
indexes `GetParameters()[0]`, and `ResolveReceiverElement` / `IsUniversalReceiver` read the same
slot. A no-argument member (`Read()`) throws there. The contract is load-bearing, not incidental.

**Widening only the probe splits it from the binder.** `GetGoMethodSetEntries`' own header states
the invariant: the probe, the count (`GoMethodSetCount` → `reflect.Type.NumMethod`) and the shell
binder (`AdapterBinder`) all resolve through ONE candidate source *precisely so they cannot
disagree about a method set*. Teaching `StructurallyImplements` about promoted members while
`AdapterBinder.TryCreate` still cannot bind them produces the exact failure that invariant exists
to prevent: `Implements` answers yes, shell creation fails, and the assert reports a type that
"implements but cannot be bound".

## The design

**Emit the promoted method as an EXTENSION METHOD as well as a member**, in `InterfaceImplTemplate`
alongside the member it already writes. Then it is a Go method like every other, and:

* the probe finds it — no golib change,
* the binder binds it — same source, invariant intact,
* `NumMethod` counts it — which is *correct*: Go counts a promoted method in the method set,
* `wrapper` and `plain` become structurally identical, which is the point.

The generator already holds every input: `Promoted`, the embed field name (`InterfaceName`
Δ-stripped), the signature, and the shadowing answer (`Overrides` / `methodOverriden`, which is
Go's rule that a directly-declared method beats the promoted one).

### Open questions the implementation must settle — measured, not assumed

1. **Where the extension method lands.** `TemplateBody` today emits only the partial struct; an
   extension method needs the enclosing `<pkg>_package` static class. The template needs a second
   emission region outside the struct body.
2. **Accessibility.** The converter emits a Go method's extension at the accessibility of its
   receiving TYPE (`internal static … Write(this wrapper …)` for an unexported type). The promoted
   twin must match, or a cross-assembly consumer's method set disagrees with the declaring one.
   **Check `ExtensionMethodRegistry`'s discovery visibility before choosing** — if it only registers
   public extensions, an `internal` promoted twin is invisible exactly where F2 bites (a foreign
   assertion), which would reproduce the bug with extra steps.
3. **Collision with the member.** C# permits a member and an extension of the same name; the member
   wins for direct calls, which is what we want (identical behavior), while the extension exists for
   the registry. Confirm no CS0111/ambiguity for the `[GoRecv]`-shaped cases.
4. **Interaction with the existing `Promoted = true` record** — measured, per the ruling, not assumed.
5. **Depth.** `ImplementGenerator` forwards through at most ONE embed hop and says so. This change
   inherits that bound; it must not silently widen it.

### Footprint expectation

Generator-only. Generated output lives under `Generated/` (git-ignored), so **CNR stays
byte-identical** and no corpus file moves — materially cheaper than the ruling anticipated when the
fix was believed to be emission-visible. Gates: the repro as a behavioral test (it cannot be banked
until the fix exists — it would red the suite), the full behavioral suite, and an explicit check that
`NumMethod` does not move for types that have no embedded interface.

## Repro

Two files plus a bare `module EmbedWitness` go.mod; `iolike/iolike.go` declares `Reader`,
`ReadWriter` and a `Base` implementing `Read`; `main.go` declares `wrapper` (embed + `Write`),
`plain` (both direct) and `holder` (named field), and asserts each to `iolike.ReadWriter`. Go
answers yes / yes / no; the conversion answers no / yes / no. The `holder` row is the control that
keeps any future fix honest: it must stay **no**.
