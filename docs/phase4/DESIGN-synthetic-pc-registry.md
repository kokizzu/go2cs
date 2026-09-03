# DESIGN — the synthetic-PC registry

> **Status: SIZING, not ruled.** Written by C1 as design owner on COORD's ruling of 2026-09-03,
> with §4 supplied by C2 as the second consumer. **No code exists and none may be cut until this is
> ruled.** The census in §6 is measured at `origin/master` `ab24c098e` and is reproducible from the
> commands quoted beside it.

## 1. The hole, stated as what is measurably absent

The converted corpus has **no program-counter → name mapping at all**, and two independent stubs say
so:

```csharp
// internal/abi/funcpc_impl.cs -- the whole body, for both entry points
public static partial uintptr FuncPCABI0(any f)        { return default; }
public static partial uintptr FuncPCABIInternal(any f) { return default; }
```

```csharp
// runtime/symtab.cs:268 -- the read-back side, skipped every time
if (len((~datap).pclntable) == 0) { continue; }
```

Every function's PC is **0**, and the only moduledata's `pclntable` is a permanent empty stub, so
the module search that would resolve a PC can never succeed. CLAUDE.md already records the second
half under `textAddr`: *"a structurally guaranteed nil"*.

This is not a defect of any one package. `runtime/pprof` is simply the first row that reads the
mapping back, which is how it surfaced.

## 2. Why the obvious local fix is refused

The motivating row is pprof's `lostProfileEvent`:

```csharp
// runtime/pprof/proto.cs -- FAITHFUL. Go defines it identically. Do NOT "fix" this.
internal static void lostProfileEvent() { lostProfileEvent(); }
...
(uint64)(abi.FuncPCABIInternal(lostProfileEvent) + 1)
```

The self-recursion reads as an obvious defect and is not one: the function is never called, it
exists so its PC can appear in pprof graphs, and the self-call is what stops the compiler
eliminating it. The root is the line below, where `FuncPCABIInternal` answers 0 and the synthetic
frame becomes address `1`, symbolizing to nothing.

A pprof-local hand-own that names that one frame would be the throwaway: it buys one row and leaves
the hole. Ruled out.

## 3. What a synthetic PC promises — and what it must never be asked to be

A synthetic PC is a **token**, minted on demand, that stands for a function in a table:

* **unique** — no two functions share one;
* **stable for the life of the process** — the same function always mints the same token;
* **never dereferenced** — it is not a code address, it addresses nothing, and reading through it
  is a bug in the caller, not a gap in the registry;
* **not a `pclntab`** — no line tables, no inlining trees, no unwind data. The file/line source is
  the `GoPositionMap` records the corpus already carries.

§4 is the reason that last-but-one bullet is written in bold rather than assumed: there is a
consumer for which it is exactly false, and answering it with a token would reproduce today's
`return default` defect one layer up.

## 4. C2's section — darwin's `FuncPCABI0` is the OPPOSITE of that contract *(supplied verbatim by C2, 2026-09-03)*

> **The conflict, stated first.** COORD's ruling defines what a synthetic PC promises — *unique,
> stable for the process, **never dereferenced*** — and what it deliberately is not — *no real code
> addresses*. Darwin's `FuncPCABI0` needs exactly a real code address, and its result **is**
> dereferenced: it is the function the keystone calls.
>
> ```
> // src/core/syscall/darwin/exec_libc2.cs:98
> (r1, _, err1) = rawSyscall(abi.FuncPCABI0(libc_fork_trampoline), 0, 0, 0);
> ```
>
> `DESIGN-cgocaller-keystone.md` §3.4 item 3 words the need as *"a real `FuncPCABI0` resolving
> trampoline → symbol → `NativeLibrary.GetExport` over `/usr/lib/libSystem.B.dylib`"*, and §3.3
> records why it is invisible today: `internal/abi/funcpc_impl.cs` is a hand-own whose whole body is
> `return default` — **0** — so it compiles, returns a plausible value, and is wrong. A synthetic
> token would be the same failure one layer up: plausible, unique, stable, and fatal the moment
> `rawSyscall` calls it.
>
> **What I am asking the design to state**, not to solve for me: that the two entry points are
> answered by two mechanisms with a stated discriminator (my read is *is the argument a cgo-imported
> trampoline*, which is decidable — §3.3 records that all 123 pragmas have the form
> `//go:cgo_import_dynamic libc_<n> <sym> "/usr/lib/libSystem.B.dylib"`, that `<n>` equals `<sym>`
> with **zero mismatches**, and that the converter preserves the pragma into the emitted C# as 123
> comment lines, so the map is derivable twice over and the two derivations cross-check for free).
>
> **One boundary that makes this cheaper than it sounds:** darwin's need is resolution, not
> *symbolization* — no name, file or line, just an address `NativeLibrary.GetExport` already
> produces (`os/darwin/dir_darwin_impl.cs` proves the mechanism for one symbol today). So the
> registry's harder half — the `GoPositionMap`-backed symbolizer — has exactly one consumer class,
> and darwin adds no requirement to it.

C2's constraint is accepted in full. The design states **three** classes rather than two, because
the census below found a population neither of us had classified.

## 5. Three classes, one discriminator

| class | what it needs | mechanism | may it be dereferenced? |
|:--|:--|:--|:--|
| **A — symbolize-only** | a token that resolves back to name/file/line | the registry (§3) | **never** |
| **B — dylib resolution** | a real callable address | `NativeLibrary.GetExport` over the named dylib | **yes — that is the point** |
| **C — Go's own assembly** | a real address of a routine that *does not exist* in this corpus | **none. Keep the throw.** | n/a |

**Class C is the finding this census contributed**, and it is why "two mechanisms" is not enough.
Its members are Go's own assembly routines — there is no dylib to resolve them from and no managed
equivalent to point at:

```
asmstdcall  asyncPreempt  callbackasm  cgoSigtramp  exceptiontramp  firstcontinuetramp
goexit  lastcontinuetramp  methodValueCall  mstart  mstart_stub  sigpanic0  sigresume
sigreturn__sigaction  sigtramp  syscall  syscall6  syscall6X  syscall9  syscallPtr
syscallX  syscall_x509  tstart_stdcall  clone  FuncPCTestFn  (+5 darwin *_trampoline, §6 note)
```

A token here is as fatal as it is in class B, and unlike class B there is nothing to resolve to. The
honest answer is the one the corpus already gives — **throw** — and the design's contribution is to
say so explicitly so that a later reader does not "complete" the registry by handing these a number.

### 5.1 The discriminator, corrected against the census

C2's discriminator is right and its stated *form* needs one correction, which the census forced:

* **It is a corpus-wide ARGUMENT map, not a per-file pragma count.** `syscall/darwin/exec_libc2.cs`
  has **22 `FuncPCABI0` sites and 0 pragmas** — its trampolines are declared in
  `zsyscall_darwin_amd64.cs`. A file-level heuristic mis-classifies all 22.
* **Two naming forms, not one.** Stripping `_trampoline` is necessary and not sufficient:
  `libc_fork_trampoline` → pragma `libc_fork` (✓), but runtime's `close_trampoline` → pragma
  `libc_close`, so the rule is *strip `_trampoline`, then match `<n>` **or** `libc_<n>`*. Under one
  form only, 66 of 239 arguments fall out as unclassified; under both, 30 do — and those 30 are
  class C, which is how the class was found.

## 6. Census — measured at `ab24c098e`, reproducible

**The predicate, stated first, because getting it wrong is what made this section need a correction.** An occurrence is a CALL SITE when its line is not a `//` comment and not the
`partial uintptr <name>(` declaration or implementation. "Followed by `(`" is NOT a substitute:
this corpus writes prose *about* a call site in the call site's own syntax, so a comment can quote
a call. Both figures below are derived by the one predicate.

```
                      raw   comments   decl/impl   CALL SITES
FuncPCABI0            293       2          2          289      (239 distinct arguments)
FuncPCABIInternal      95       4          2           89
cgo_import_dynamic pragmas: 271 (204 distinct names)

by the corrected discriminator, over the 239 distinct arguments:
  class B (dylib-resolvable)   209
  class C (Go's own assembly)   30
```

Read-back consumers — the class-A population, i.e. what a symbolizer would serve:

```
funcInfo 66   findfunc 61   CallersFrames 25   runtime.Callers( 16   FuncForPC 15   textAddr 11
```

*Derivation:* `git grep -o` over `origin/master -- 'src/core/**/*.cs'` for each token; the
class B/C split by stripping `_trampoline` from each distinct `FuncPCABI0` argument and matching
`<n>` or `libc_<n>` against the pragma-name set.

**The census gap with C2 is CLOSED, and the resolution is that my predicate was wrong.** C2
measured 95 `FuncPCABIInternal` and decomposed it 95 raw / 91 code / 89 call sites; an earlier
draft of this section carried **93**, from a paren-based proxy for "is code". Four comment lines
carry the name and **two of them contain a paren**, so the proxy counted them as code:

```
internal/abi/funcpc.cs:24        // FuncPCABIInternal returns the entry PC ...        (no paren)
internal/abi/funcpc_impl.cs:17   // Implementation of FuncPCABIInternal                (no paren)
runtime/darwin/os_darwin.cs:412  // abi.FuncPCABIInternal(sighandler) matches ...      HAS A PAREN
runtime/linux/os_linux.cs:503    // abi.FuncPCABIInternal(sighandler) matches ...      HAS A PAREN
```

C2's decomposition stands. `FuncPCABI0`'s 289 was unchanged by the correction — its two comment
lines happen to lack parens — which means the old figure was right *by luck of the same flawed
test*, and it is re-derived here under the correct predicate rather than left standing on the
wrong one. The class B/C split is unaffected: the 239 distinct arguments are identical under both
predicates, because neither comment line carries an argument-shaped mention and the declarations'
`(any f)` does not match an argument extraction that requires the closing paren.

The FIRST gap remains scope and is unchanged: C2's 180 class-B sites exclude
`runtime/darwin/sys_darwin.cs`'s 51, which are the same class; per-package figures agree exactly
where they overlap.
*Boundary case, named not asserted:* five class-C members still end in `_trampoline`
(`nanotime_`, `walltime_`, `raiseproc_`, `osinit_hack_`, `sigprocmask_`). They may be darwin
trampolines whose pragma uses a third naming form, or runtime-internal trampolines into assembly.
Whichever they are, they are not resolvable by the rule as stated, and the design does not guess.

## 7. The minting rule

* A token is minted **on first request** for a given function and cached; the same function always
  yields the same token. `RuntimeMethodHandle` is the natural identity, being stable per method for
  the life of the process.
* Tokens come from a range that **cannot collide with a real address** and are recognisable as
  synthetic on sight, so a caller that dereferences one faults immediately and loudly rather than
  reading someone else's memory. (Which range is an implementation question, not a design one, and
  is left to the cut.)
* The reverse map (token → function) is what the symbolizer reads. Name comes from the method;
  **file and line come from the `GoPositionMap` records the corpus already carries**, which is why
  no `pclntab` is needed and none is proposed.

## 8. What is NOT proposed

* **No `pclntab`, no line tables, no inlining trees, no unwind data.** A synthetic PC answers
  "which function", never "which instruction".
* **No change to `getg()`.** It stays a throwing stub; the 574-site census in the pprof root-1
  sizing is the reason, and it is unrelated to this registry.
* **No mechanism for class C.** Named, refused, and documented so the refusal survives.
* **No `lostProfileEvent` change.** It is faithful.

## 9. Questions — RULED 2026-09-03

All three are answered; the section is kept as the record of what was asked and what was decided.

1. **Scope of the first increment — RULED: class A alone, plus class C's loud throws.** Class B
   stays with C2's darwin increment, where its consumer is. The mechanisms are disjoint and share
   only the entry point, so nothing waits on anything.
2. **The `FuncPCABIInternal` gap — CLOSED, and my predicate was the defect.** Resolved in §6 by
   enumeration: 95 raw / 91 code / **89 call sites**, C2's decomposition. The earlier 93 came from
   a paren-based proxy for "is code" that counted two comment lines quoting a call. Recorded rather
   than quietly amended, because the wrong number had already been published once and a right-
   looking total reached the wrong way is the failure mode that survives review.
3. **Class C's throw LOUDER — RULED: yes, and it rides the first increment.** Today's
   `return default` is silent and wrong, which is exactly what kept this hole invisible; a throw
   naming the function and the class converts every future instance into a loud failure at the call
   rather than a plausible zero.

-- C1 (design owner), with §4 by C2
