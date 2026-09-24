# DESIGN (stub) -- REC-C: allocations two slice idioms cost that Go's compiler does not pay

> **Status: STUB, 2026-09-23.** Minted by the H10 relabel ruling (ledger 2026-09-23 03:37, X(2), O2,
> O4, O5) so that the params-pack and append-of-make families' `deferred` entries cite a record that
> names a stage which removes the counted allocation. **Owner: C1. Full design: phase-4D kickoff.**
> Nothing is cut against this stub; line numbers are read at `bb54ff0920` and figures are the i7
> reading run's (`claude/coord-h10-readings` ac9f8251ee, Release with tiering off). Feasibility is
> UNMEASURED.
>
> **Fixed up 2026-09-23** per COORD's ACCEPT-WITH-FIXES (ledger 3942e083ad; list
> `docs/phase4/briefs/h10-relabel-fixup.md` at claude/coord-handover 83e4f16ea6, items 1 and 5 and the
> REC-C note): §A2 added for a pack passed as `slice<T>`, copy-source's golib precondition stated, and a
> members section added.

## Members (by entry name)

log/slog TestAlloc/pairs, /2_pairs, /2_pairs_disabled_inline, /9_kvs, /attrs1, /attrs3,
/attrs3_disabled, /attrs6, /attrs9 (F1, §A and §A2; attrs6 and attrs9 also F5, §B); slices TestInsert
(§A, §A2 and §B); slices TestGrow (§B); slices TestConcat's legs 2-4 (§B; the entry stays
alloc-count-semantics on leg 1); fmt TestCountMallocs (§A, per leg). log TestDiscard is NOT a member:
§A refuses its pack.

## §A. The params-span view

**The site.** A Go variadic `v ...T` arrives in C# as `params Span<T> vʗp`, and the body's first line
is `var v = vʗp.slice();`, which copies the span into a fresh heap array (golib/slice.cs:1520,
`AllocationCounter.CopyOf`) -- one counted object per call with a non-empty pack. Go passes the pack's
slice header and allocates nothing when the pack does not escape. The corpus carries **317**
`ʗp.slice()` sites in 149 files at `bb54ff0920` (240 in 97 non-test files), counted by
`grep -o 'ʗp\.slice()'` over `src/core/**/*.cs`.

**The mechanism already exists for part of the population.** golib's `sslice<T>` (golib/sslice.cs:19)
is a stack-only view over the `Span<T>`, and the converter emits `vʗp.sslice()` when every use of `v`
is `len`/`cap`, an element index or a range (`ssliceUsesAreSafe`, go2cs/escapeAnalysisOperations.go:328-333);
80 sites use it today, `slices.Concat` among them (slices.cs:498).

**The stage that REMOVES the counted allocation:** widen that predicate to two more uses --
**copy source** (`copy(dst, v)`) and **pass-through** (`f(x, v...)` spreading the pack into another
variadic, builtin `append` included, which receives it as the same `Span<T>`). *Removes:* one counted
object per call at every admitted site; on log/slog's path it removes two of the three copies per call
(the public wrapper and `log`/`logAttrs` only forward the pack; `Record.Add` and `AddAttrs` hand it to
`argsToAttr`/`countEmptyGroups` as a `slice<T>` and are refused below). *Preconditions:* the callee of a
pass-through is itself a params-`Span` method (a `slice<T>` parameter needs the heap copy); a
generic callee's constraint admits the span form.

**Refusals** (the pack keeps `.slice()`): stored into a field, global or map; returned; appended INTO;
captured by a closure -- log's `Printf` copies at log.cs:289 and captures the copy at :291-292, so it
is refused and log TestDiscard is unchanged by this stage; and passed as a `slice<T>` argument, unless
§A2 admits the callee.

*Copy source's own precondition:* golib has no `copy(slice<T>, ReadOnlySpan<T>)` overload today -- every
`copy` in golib/builtin.cs (:761-1109) takes an `array<T>`, a `slice<T>`, an `ISlice<T>`, a pointer to
one of those, or a `@string` -- so admitting copy source adds that overload.

## §A2. A pack passed as `slice<T>` to a callee that neither keeps nor mutates it

§A refuses a pack passed as a `slice<T>` argument, and three of the members' copies sit exactly there.
Two arms admit them, each through a SPAN OVERLOAD of the callee, emitted beside the `slice<T>` form:

- **(a) A read-only, non-retaining callee.** The callee reads its parameter (index, `len`, range, copy
  source) and never stores, returns, captures or writes through it. log/slog `AddAttrs` hands
  `attrs[i:]` to `countEmptyGroups` (log/slog/record.cs:124); slices `Insert` hands `v` to the hand-owned
  `overlaps(v, ...)` (slices/slices.cs:196; slices/slices_impl.cs:36), which gains a `ReadOnlySpan<E>`
  overload by hand. **The Go-true consequence, gated:** today `v` is a fresh copy, so `overlaps` can never
  see it alias `s`, and Insert's HARD case (slices/slices.cs:214-229, the branch Go takes when the spread
  aliases the destination) is unreachable in the converted package. Under the span view `overlaps` sees
  the caller's own storage, so an aliasing spread now takes the hard case, as it does in Go. The gate is
  Go's own TestInsertOverlap, which must pass before and after.
- **(b) A callee that returns only subslices of the pack.** log/slog `Record.Add` loops
  `(a, args) = argsToAttr(args)` (record.cs:141), and `argsToAttr` returns `args[2..]`, `args[1..]` or nil
  (record.cs:179-192): the pack never leaves the loop except as a shorter view of itself. The span
  overload returns the remainder through an `out Span<T>` (or, if C#'s ref-safety rules refuse that form,
  as an index into the caller's span -- the index-returning rewrite). *Precondition:* every return of the
  parameter is a subslice of it, and the tuple result is lowered to an `out` parameter.

*Removes:* the pack copy at each admitted callee. *Preconditions:* a callee classification, cached per
function, for each arm; a generic callee's constraint admits the span form; the callee is in the
converted corpus or hand-owned (overlaps). *Refusals:* a callee that stores, captures or writes through
the parameter; a callee reached through an interface or a func value.

**Predictions (counted objects per run):** slices TestInsert 58 -> about 8 (the per-call pack copy at
slices.cs:161 removed by §A's copy source and §A2 (a)); fmt TestCountMallocs one fewer per leg whose
pack is only read or forwarded (per-leg figures at the kickoff); log TestDiscard unchanged (refused);
log/slog TestAlloc/* all three copies per call with a non-empty pack (§A takes the wrapper's and
`log`/`logAttrs`'s; §A2 (a) takes AddAttrs's, §A2 (b) takes Add's), and the disabled paths' two (§A).
All UNMEASURED.

## §B. Append-of-make (extendslice)

**The site.** Go compiles `append(s, make([]T, n)...)` without allocating the `make`: the compiler
recognises the shape and grows `s` in place (extendslice), which is why Go's own `slices.Grow` says
"This expression allocates only once". The emission builds the `make` and then appends it, two counted
objects where Go has one:

- slices.Grow, slices.cs:441 -- `appendꓸꓸꓸ(subslice(s, 0, cap(s)), new slice<E>(n))`;
- slices.Insert, slices.cs:177 -- `appendꓸꓸꓸ(subslice(s, 0, i), make<S>(n + m - i))`;
- bytes `growSlice` (bytes/buffer.cs:251 onward), Go's append-make-with-nil pattern -- POPULATION ONLY,
  no member reads it;
- the hash `Sum`/`AppendBinary` appends, e.g. crypto/internal/fips140/sha256/sha256.cs:76 -- POPULATION
  ONLY, no member reads it;
- log/slog `AddAttrs` through slices.Grow (F5).

**The stage that REMOVES the counted allocation:** recognise `append(x, make([]T, n)...)` (and the
`[:len(x)]` re-slice that follows it in Grow) and emit one golib grow operation that extends `x`'s
backing by `n` zeroed elements. *Removes:* the `make`'s object at every recognised site.
*Preconditions:* the `make` has no other use; golib's grow rounds capacity by the same rule `append`
uses today, so `cap()` is unchanged by the rewrite.

**Refusals:** a `make` bound to a name or used twice; a spread of anything but a direct `make`.

**Predictions:** slices TestGrow 2 -> 1 on its insufficient-capacity leg (want 1); slices TestConcat
legs 2-4 (which print 2 against want 1, COUNT) -> 1; log/slog attrs6 and attrs9 one fewer each (of
F5's two); slices TestInsert's remaining objects fall by one per growing insert.

## Gates

§A's predicate is controlled both ways: a pack that is stored, returned or captured must keep
`.slice()`, and one only forwarded must take the span. §A2's classification is controlled the same way
(a callee that writes through its parameter must keep the copy), and slices TestInsertOverlap gates the
newly reachable hard case. §B's recognition is controlled against a
`make` bound to a name. Each stage reads its members' rows before and after at Release with tiering
off. The emission diff of each stage is measured over the whole corpus (two-seeded reconvert).
