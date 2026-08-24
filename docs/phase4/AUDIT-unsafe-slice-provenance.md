# AUDIT — the 53 `unsafe.Slice` call sites, by pointer provenance

*Lane G, 2026-08-23. Gate #2 of the ratified pointer-provenance increment (⟨OQ-P3⟩ as amended:
mechanism → **this audit** → slice consumer). Closed-form by construction: `OverNativeMemory` has
exactly ONE caller — `unsafe.cs`'s `ptr.IsNative` arm — so every native-backed slice that exists
came through `unsafe.Slice`, and the §3 hazard (a native-backed slice over PINNED-MANAGED storage,
dropping the pin) is reachable only from a call site whose pointer can carry a pinned-managed
address in `m_nativeAddr`. Census: all `.cs` under `src/core` excluding `bin`/`obj`/`Generated`
and `*_test.cs`, at the ledger-era head. Re-derive at consumption.*

## Verdict first

**No roster-exercised site supplies a pinned-managed pointer to the native arm today.** The §3
hazard is **latent-with-live-trigger** — the same durable name the byte-view finding earned — and
the trigger is any future site that round-trips a managed address through `uintptr` before
`unsafe.Slice`. Five sites sit on that shape now; every one is in an inert or off-roster surface,
and they are the watch list the slice consumer's validate-on-read exists for.

## Classification

**W — managed window (33 sites).** The pointer is an element box, field box, or `Reinterpret`
alias over managed storage, built pointer-to-pointer (never through `uintptr`), so
`m_nativeAddr == 0` and the call takes `TryGetElementWindow` — the managed-aliasing arm, which
never mints a native-backed slice. Representative members: `crypto/internal/bigmod`'s six
`Ꮡz`/`Ꮡx` array-pointer sites, `crypto/subtle`'s three `xorBytes` rebuilds, the three
`os/*/file.cs` `StringData` sites, `log/slog`'s two group pointers (an `unsafe.Pointer` that
CARRIES the box, not a `uintptr` that strips it), `reflect/value.cs` 845/1415, `runtime/iface`'s
five cache-entry sites, `runtime/pinner` 272, `runtime/tracestack` 164, `internal/abi` GCData,
the four `internal/coverage/cfile` counter sites, `syscall_windows_impl` 321/332,
`syscall_bsd` 266 and `syscall_darwin` 331 (both over R's decoded managed mirrors), `bbig`'s two
word-reinterprets. **These are safe by construction today; they are listed so a future edit that
inserts a `uintptr` hop into one is recognizable as a class change, not a refactor.**

**N — genuinely native (13 sites).** The address comes from the kernel or a native allocator, and
the native-backed slice is exactly the mode's intended use: `syscall/{darwin,linux}`
`syscall_unix.cs:62` (the founding W1b mmap site), `net/darwin/cgo_unix` 398/439 (`_C_malloc`),
`internal/fuzz/windows` (MapViewOfFile), `execenv_windows` + `env_windows` (environment blocks),
the two `UTF16ToString` pointer paths, `corefoundation.cs:32` (CFData bytes), the four
CryptoAPI chain sites in `root_windows.cs` + `zsyscall_windows_certchain_impl.cs:703` (native
CERT contexts, per the certchain hand-own's transcription contract).

**U — `uintptr`-sourced, provenance not provable at the site (5 sites, the watch list):**

| Site | Source of the address | Liveness |
|---|---|---|
| `runtime/slice.cs:409` | `mallocgc(...)` result through `uintptr` | inert — golib `slice<T>` supersedes; bookkeeping-artifact class |
| `runtime/string.cs:294` | same `mallocgc` shape (`rawstring`) | inert — golib `@string` supersedes |
| `runtime/stkframe.cs:260` | stack-map record pointer | inert — no converted caller walks stkframes |
| `internal/fuzz/counters_supported.cs:18` | coverage-counter section address | off-roster; fuzz instrumentation not exercised |
| `reflect/type.cs:1611` | **`Value.Pointer()` — the identity TOKEN, not an address** | off-roster path (`funcTypeFixed` args walk); see below |

**`reflect/type.cs:1611` is a distinct latent defect, not merely unproven provenance.** Since the
alignment-truthful token ruling, `Value.Pointer()` answers an identity token whose low bits mirror
Go layout — explicitly non-dereferenceable. Casting it to `ж<ж<rtype>>` and slicing fabricates a
slice over a token. If that path is ever exercised it fails at the FIRST element read, loudly under
the ratified validate-on-read, silently before it — one more reason the mechanism's ordering
(validate-on-read ships with the table) is right. Boarded as its own watch item rather than fixed
here: the fix belongs to whatever arc makes that reflect path live, and inventing a caller to test
it would be speculative machinery.

## What the slice consumer takes from this

1. The `IsNative`-first arm in `unsafe.cs` needs the provenance consult exactly ONCE — no other
   entry exists.
2. The N class must keep working unchanged (13 sites, two of them roster-critical: mmap and the
   resolver).
3. The U class is why MISS-means-genuinely-native is sound *today* and why validate-on-read is the
   correct backstop for the day a U site goes live: every U site would register no pin (their
   addresses never passed through `EnsureStableAddress`), so a table MISS still reads native —
   the failure surfaces at first read, named, instead of as corruption.
4. `unsafe.cs:653`'s *"lifetime is the mapping's own"* comment is amended by the consumer change —
   it currently states the assumption §3 falsified.
