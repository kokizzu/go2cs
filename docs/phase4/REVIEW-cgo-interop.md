# REVIEW — PLAN-cgo-interop (`claude/cgo-go2cs-compatibility-72ae3d` @ `9ddd5939f`): RATIFY WITH BINDING CORRECTIONS

> Adversarial review, 2026-08-28, coordinator-commissioned, run on the coordinator i7 against
> current master `6fcf4be7b`. Verdict: **RATIFY WITH BINDING CORRECTIONS** — the frame (which
> targets, in which order) is sound and stays; every one of the plan's measured claims
> **reproduced exactly** on re-run, every source citation lands at the plan's own pinned base,
> and both "retired false premises" are verifiably retired against current source. What the
> plan cannot have known is what the twelve union-gated merge windows since its fork
> (`be58eb4aa`, 2026-08-25) landed on the exact surface it builds on: the keystone tether, the
> kind split, the I5/FromBox retention family, the struct-passing proactive ruling, the
> single-file host, and the scheduler-design amendment that resolves the plan's own §5.3 flag.
> All are foldable as one text-only revision increment; none reopens the phase ladder or an OQ
> ruling. Two genuine absences — security posture and campaign sequencing — carry the two
> corrections that are additions rather than reconciliations.

## Axis 1 — claim audit: CLEAN (measurements reproduced, citations land)

Every factual claim about the corpus, golib, the converter, and the toolchain was re-derived
against `origin/master` (`6fcf4be7b`) and, where the plan pinned lines, against its own base.

**§2 measurements — reproduced on this machine, 2026-08-28.** Converter rebuilt from the branch
tip (docs-only commit, so converter source ≡ `be58eb4aa`) with go1.23.12; fixture per §2
(preamble `#include <stdlib.h>` + `static int add_ints`); run with and without `-cgo=true`:

- **Finding 1 reproduced:** `go env CGO_ENABLED` → `0`; no `gcc`, `clang`, or `cl` on `PATH`.
- **Findings 2–3 reproduced verbatim:** both runs emit the exact transcript the plan prints —
  `WARNING: … did not fully type-check … [-: build constraints exclude all Go files in …]`,
  `INFO: Skipping conversion: no target Go source files found …`, **exit code 0**, output
  directory containing **only `go2cs.ico`**. The `-cgo` flag is inert exactly as claimed.
- **Finding 4 verified at master:** `conversionDriver.go:227-228` still zips `pkg.Syntax`
  against `pkg.GoFiles[i]` — the file is **unchanged since the fork**, so the latent bug the
  plan names is live at master. The x/tools doc-comment citations are exact in the pinned
  module (`golang.org/x/tools@v0.36.0/go/packages/packages.go`: GoFiles caveat at ~447-451
  incl. "subject to cgo preprocessing"; "Syntax … for the files listed in CompiledGoFiles" and
  the nils/"may be shorter" caveat at ~512-519). The `-tests` path's bounds-guarded
  `CompiledGoFiles` zip is confirmed (base `testConversion.go:1171/1175` as cited; master
  `:1183-1187` after unrelated growth).
- **Finding 5 verified at master:** no tracked `.go` file imports `"C"` — the claim survives
  all twelve windows, so zero-regression-risk still holds.
- **Finding 6 verified at master:** all three avoidance sites present — the never-firing fatal
  gate (`visitImportSpec.go:212-214`; the file grew +160 lines since fork but the gate is
  intact), `moduleConverter.go:226` (`classSkip`), `importOperations.go:1027`.

**Other citations, all verified:** `main.go:344` verbatim including the quoted error text,
unchanged at master; no `CGO_ENABLED` override at any go2cs `packages.Config` site (grep
empty), and `LoadAllSyntax` includes `NeedCompiledGoFiles` (⟨OQ-1⟩'s premise);
`cmd/cgo/doc.go` ~324-329 verbatim in the go1.23.12 GOROOT ("it must not contain any
definitions, only declarations" — Phase 4/5 mutual exclusion stands); `packages.Package` has
**no** `CgoFiles` field in v0.36.0 (the §9 non-adoption is correct);
`TestingInfrastructureRequirements.md:41-42` principle-8 quote exact; `Glossary.md:321-324`
strategy-plan quote exact; exactly five `docs/PLAN-*.md` at master (⟨OQ-6⟩'s "beside the five
existing" is right); `Roadmap.md` untouched base→master, so the branch's pointer merges clean
and its `:770-784` cite lands on the Phase 5B companion-pattern block it describes.

**Unmeasured-but-plausible, correctly disclosed:** the §1 weight table ("the bulk of cgo
usage") carries no figures, per the no-frozen-figures discipline; the plan claims measurement
only where it measured. ⟨OQ-1⟩'s cgo-rewrite mechanism is correctly framed as unverifiable on
this host.

## Axis 2 — feasibility against the box model: HOLDS, but three post-fork landings must be absorbed by name

The composition question — does the `import "C"` ladder compose with golib's pointer/box model
and the marshaling classes this campaign convicted — comes out **yes**, and the post-fork
evidence makes the case *stronger* than the plan states, at the price of three named
obligations the plan predates:

- **2A — the kind split landed (B2-I3, merge `3174009a8`).** `ж<T>` is now abstract over
  `StandardBox`/`FieldRefBox`/`ElemRefBox`/`NativeBox` with a 754-site emitter flip. The
  plan's §4 statements survive intact (provenance RATIFIED, confirmed; native-backed slice
  RATIFIED and landed, confirmed — `OverNativeMemory` at `slice.cs:103`), but its load-bearing
  cite drifted: the pin-throw is `slice.cs:431` at master (was `:396-397` at base, correct
  when written). The Phase 3 direction split (native-backed slice C→Go, `PinnedBuffer` Go→C)
  remains exactly right under the split.
- **2B (material) — the keystone tether re-grounds Phase 3's pin-scope discipline.** The
  ж→uintptr lifetime gap was measured post-fork (os/exec GC-mark SIGSEGV, rooted 2026-08-26)
  and closed at the one funnel every Linux syscall crosses:
  `src/core/internal/runtime/syscall/linux/syscall_linux_impl.cs:105-119` re-roots each
  argument's box (registry `Resolve`, strong locals, `GC.KeepAlive` past the return), because
  a `(uintptr)Ꮡbuf` argument's pin lives ON the box and the box can be collected **before the
  call runs**. cgo has **no single funnel** — every generated extern is its own crossing — so
  the plan's "the generated wrapper owns the pin scope" is the structurally correct answer
  *only if* the wrapper receives the **pinnable object** (slice/string/ж, or a box-carrying
  `Pointer`), never a pre-extracted address. The plan's own case 3
  (`unsafe.Pointer(&b[0])`) is precisely the shape that arrives as a number; the corpus
  already mints it box-carrying (`unsafe.Pointer.FromBox`, `unsafe.cs:368-380` — number plus
  retained box). The plan must state this contract; it could not, since both the finding and
  the mint post-date its fork.
- **2C — the I5/FromBox NativeBox retention family is a missing §4 foundation (favorable).**
  `ж.NativeBox.cs:31/47/79` (`m_retainedSource`, the B1 §4 source-retention slot) and the
  corpus-wide `FromBox` emission (merge `a7c964d80`, 57 mint files) are exactly the
  C-retains-a-pointer machinery Phase 3's `C.CString`-class helpers and any
  callee-retains scenario need. The plan lists two foundations; there are now three.
- **2D (material) — the struct-passing PROACTIVE ruling reads directly onto Phase 2's
  generator.** `src/core/syscall/linux/structclass_linux_impl.cs` (2026-08-28, post-fork)
  records the ruling: for structs whose converted form holds `array<>` fields as **managed
  references**, a native write over them is heap corruption surfacing inside the GC
  (verifyheap: zeroed MethodTable, text-where-pointers-belong — Uname was the measured root),
  and the per-member-**when-reached** deferral is ruled **WRONG for the native-overwrite
  sub-class**; the remedy family is a blittable mirror **plus a size assertion at the
  boundary** (a wrong mirror fails the call loudly) **plus explicit two-way copy-back** for
  read-modify-write members. The plan's Phase 2 proposes the mirror and stops there. A
  generator that emits mirrors without the size-assert/copy-back discipline — or that ever
  falls back to passing a managed layout to native code — recreates the Uname corruption at
  generated scale. The plan names the Windows wrapper family (§3) as its evidence base; the
  Linux family, which post-dates it and carries the ruling, must join it by name.
- **2E — minor, favorable:** `PinnedBuffer.Clone` re-pins since merge `1d01200a9` (the
  two-finalizers-one-slot bomb) — evidence that `PinnedBuffer`'s lifetime discipline is
  actively maintained; Phase 3 inherits the fix for free.

## Axis 3 — cost and sequencing: HONEST, but the plan is silent where it must speak

The plan claims "a real subproject, not a flag flip," keeps §7 unscoped, and claims **no**
campaign relevance — all honest. What it never states is where the ladder sits relative to the
live campaign: the roster is **181/215 (84.2%)** at master (`ValidatedTestPackages.md:106`),
the owner's 2026-08-26 ruling puts Win/Linux/Darwin at 100% honest validation before leaving
Go 1.24, and go1.24.13 is the next hop. The plan's own Finding 5 supplies the argument it
never draws: the corpus is wholly `CGO_ENABLED=0` output, so **nothing on the active roster
and nothing in the 1.24 hop depends on cgo** — this ladder staffs after parity, gates nothing,
and a ratification that does not say so invites the next planner to read Phase 1's "worth
doing on its own merits" as a license to staff now. One sentence and one OQ close it. (The
Phase 1 zip fix is the deliberate exception: a latent correctness bug independent of cgo,
still live at master — it may staff on its own merits without staffing the ladder.)

## Axis 4 — security posture: MISSING, and the plan's own subject matter demands it

cgo means compiling and loading attacker-authored native input; the plan says nothing about
the trust boundary anywhere. Three concrete surfaces, each with an in-tree or in-toolchain
precedent the plan can absorb by name:

- **Build-time flag injection.** `#cgo CFLAGS/LDFLAGS` in a third-party module are input to
  the C toolchain, and real Go isolates an entire file to the problem —
  `$GOROOT/src/cmd/go/internal/work/security.go`: *"We must avoid flags like -fplugin=, which
  can allow arbitrary code execution during the build."* Phase 2's `-lfoo`-only scoping
  accidentally approximates the allowlist without naming the threat; Phase 7's "arbitrary
  LDFLAGS" line, taken without the allowlist doctrine, is a build-time RCE on the converting
  machine — and `-recurse` conversion of an untrusted module is exactly the exposed path.
- **Load-time library resolution.** "Per-OS naming is .NET's own native-library resolution"
  delegates the search-order question without naming the Windows DLL-planting surface.
  Generated bindings must pin resolution (`[DefaultDllImportSearchPaths]` /
  an explicit `NativeLibrary` resolver), stated in Phase 2, not discovered in Phase 6.
- **The boundary statement.** One sentence the document owes its readers: a loaded native
  library is full-trust in-process code; the trust decision is made when the module is chosen,
  and no generated wrapper confines it.

## Axis 5 — completeness of the OQ list: two absent questions, one absent long-tail entry

- ⟨OQ-7⟩ (sequencing) and a security section/OQ — Axes 3 and 4 above.
- **Signal-handler coexistence is absent from §7's named long tail.** Real cgo documents the
  signal contract at length; this corpus now carries a live signal bridge —
  `src/core/runtime/linux/signal_posix_impl.cs` (`PosixSignalRegistration`, the os/signal arc,
  merge `9b4699ff1`, post-fork) — and a loaded C library that installs its own handlers
  (SIGSEGV for its own purposes, SIGCHLD, sigaltstack) will fight both the CLR and that
  bridge. Phase 7 names harder things; this one is nameable today and interacts with a
  banked row.
- Otherwise the OQ list is complete and correctly framed; ⟨OQ-3⟩'s ship-nothing and ⟨OQ-5⟩'s
  keep-opt-in recommendations are endorsed as written. ⟨OQ-6⟩'s ruling is consistent with the
  Glossary definition it quotes.

## Axis 6 — the two retired premises: retirement COMPLETE, verified at current master

- **Premise 1 (hard-stop gate).** Retired by measurement and the measurement reproduces (Axis
  1): the gate at `visitImportSpec.go:212-214` never fires in either configuration; the
  observed behavior is the catalogued false-green shape, correctly worse than a hard stop.
- **Premise 2 (pool starvation).** Retired against current source, re-derived here
  independently of any design doc: `Goroutine.cs:202-209` (`new Thread(() => Run(body),
  s_stackReserve)`, `IsBackground = true`) — one dedicated thread per goroutine; `:26-38`
  records the `QueueUserWorkItem` history and the 28.7-minute singleflight ladder as
  historical; `:43` carries the ~10⁴ thread bound the plan cites; `:68` the 256 MB reserve;
  and `builtin.cs` carries the "deliberately NO `ThreadPool.SetMinThreads` floor here any
  more" self-indictment. Every §5.3 position checks out: direct P/Invoke on the calling
  thread is the zero-work default; the `[SuppressGCTransition]` characterization
  (process-wide GC-suspension block, exclude callback-reachable/blocking symbols) is
  accurate; the errno/thread-affinity argument against thread-hopping is correct; the real-Go
  P-detach-with-M-reuse description is correct.
- **The §5.3 flag is RESOLVED at master.** `DESIGN-cooperative-scheduler.md`'s status block
  now reads RATIFIED AND LANDED with a dated amendment that names the pre-landing §1/§2
  description as retired — and names *this plan's blocking-call section* as the trap that
  motivated the amendment. The plan's parenthetical ("still describes the launch path in the
  present tense … flagged for the coordinator, not corrected here") and the header's
  flagged-not-corrected sentence are therefore stale in the good direction: the flag was
  actioned. Retire both at fold time.

## Stale-base reconciliation — what the twelve windows did to the plan's claims

| Plan claim / dependency | Status at `6fcf4be7b` |
|---|---|
| §5.3 flag: scheduler design doc "still present-tense QueueUserWorkItem, still PROPOSED" | **RESOLVED** — status block amended (RATIFIED AND LANDED + dated staleness amendment naming this plan). Retire the flag (correction 1). |
| §4 foundations: provenance + native-backed slice | **RE-GROUNDED** — both survive; the kind split (`3174009a8`) landed beneath them; `slice.cs` pin-throw cite drifts 396→431. |
| §4/Phase 3: "the generated wrapper owns the pin scope" | **RE-GROUNDED, obligation added** — keystone tether (`internal/runtime/syscall/linux/syscall_linux_impl.cs:105-119`) proves the pre-extracted-address window is real and measured; wrapper contract must be stated (correction 2). |
| Phase 3 retention (`C.CString`-class, callee retains) | **STRENGTHENED** — I5/FromBox NativeBox retention (`unsafe.cs:368`, `ж.NativeBox.cs:31-79`) landed; cite as third foundation (correction 2). |
| §3 evidence base: Windows wrapper family only | **WIDENED** — Uname/struct-passing closure + PROACTIVE ruling (`structclass_linux_impl.cs`), posix_spawn hand-own + startup FD-dup hygiene (`syscall/linux/exec_unix.cs`, os/signal merge), PosixSignalRegistration bridge — all post-fork, all on the surface §3 argues from (corrections 3, 8). |
| Phase 6: `-tests` pipeline validation | **RE-GROUNDED** — the host now publishes single-file self-contained (`test-csproj-template.xml:81-85`); native-artifact travel/resolution under `PublishSingleFile` is a new named obligation (correction 6). |
| §2 Finding 4: the GoFiles zip | **STILL LIVE** — `conversionDriver.go` unchanged since fork; Phase 1 step 1 stands as written. |
| §2 Finding 5: no `import "C"` in the corpus | **STILL TRUE** at master. |
| §2 Finding 6 / §9 gate cites | **LINE DRIFT ONLY** — `visitImportSpec.go` grew +160 lines; gate intact at 212-214. |
| Roadmap pointer + ToDo item 48 edit | **MERGE-CLEAN** — `docs/Roadmap.md` untouched base→master. |
| Roster context (plan makes no numeric roster claims) | Roster moved 162→**181/215**; the plan's no-figures discipline means nothing to reconcile — the discipline worked. |
| Phase 5 golib state names | **WRONG AT BASE AND MASTER** (the audit's one naming defect): no `t_onGoroutine` field exists — the root is `[ThreadStatic] Goroutine? t_current` (`Goroutine.cs:83-84`) with property `OnGoroutine` (`:138`); `t_procId` lives in `src/core/sync/runtime_impl.cs:273`, not golib. And `Goroutine.Enter()` (`:176`) is an **existing** host-attach scope API, already used by the test host for foreign threads — Phase 5's registration work is applying it plus the panic boundary, not inventing it (correction 7). |

## Binding corrections (fold as one text-only revision increment before implementation staffs)

1. Retire the §5.3/header flag on `DESIGN-cooperative-scheduler.md` — actioned at master; the
   design's own status block now cites this plan. Replace with a pointer to the amendment.
2. §4 gains the post-fork native-crossing foundations by name: (a) the keystone tether and the
   ж→uintptr lifetime gap, with the stated Phase 3 contract — *the generated wrapper receives
   the pinnable object or a box-carrying `Pointer` (`unsafe.Pointer.FromBox`), never a bare
   pre-extracted address; where Go hands it `unsafe.Pointer(&b[0])`, the emission is
   FromBox-shaped so the wrapper can pin or re-root*; (b) the I5/FromBox NativeBox retention
   slot as the callee-retains mechanism.
3. Phase 2 absorbs the struct-passing ruling by name: generated mirrors carry the boundary
   size assertion and explicit copy-back for out/in-out structs; the generator never emits a
   call in which native code writes over storage holding managed references; the
   per-member-when-reached deferral is ruled out for this class (cite
   `structclass_linux_impl.cs`).
4. Add a security-posture section (or ⟨OQ-7a⟩ with a recommendation): the `#cgo` flag
   allowlist discipline (cite `cmd/go/internal/work/security.go`) as a precondition for any
   flag pass-through beyond `-lfoo`; pinned library resolution
   (`[DefaultDllImportSearchPaths]` / explicit resolver) in Phase 2; the one-sentence trust
   boundary statement.
5. Add the sequencing statement (or ⟨OQ-7b⟩): the ladder staffs after the platform-parity
   campaign and gates neither the roster push nor the go1.24 hop — the corpus is wholly
   `CGO_ENABLED=0` (own Finding 5); the Phase 1 zip fix is severable and may staff
   independently.
6. Phase 6 names the single-file self-contained host and the native-artifact
   placement/resolution question it creates.
7. Phase 5 corrects the state-root names (`t_current`/`OnGoroutine`; `t_procId`'s real home)
   and cites `Goroutine.Enter()` as the existing attach point the entry thunk composes with.
8. §7's long tail gains signal-handler coexistence (C-installed handlers vs the CLR and the
   `PosixSignalRegistration` bridge).
9. Line-cite refresh at fold time: `slice.cs:396→431` (pin throw; `buffer` property `:430`),
   `visitImportSpec.go:211→212-214`, `testConversion.go:1170-1175→1183-1187`, marker probe
   `conversionDriver.go:244-246→245-249`. Content verified identical in all four; cites only.

None of the corrections moves a phase boundary, reverses an OQ recommendation, or reopens
⟨OQ-6⟩. The ladder's shape — recognition, P/Invoke generation, marshaling, side-build,
callbacks, validation — survives adversarial contact with the post-fork tree; the plan's own
§9 record is accurate and its two retirements are complete. Ratify on the corrected text.

Probe artifacts: coordinator scratchpad `lane-cgr-*` (converter build, fixture, both run
transcripts). Review worktree: `.claude/worktrees/lane-cgo-review`.
