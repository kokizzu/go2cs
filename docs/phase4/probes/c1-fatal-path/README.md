# c1-fatal-path

The **falsifier** for C1's fatal-path sizing (MAILBOX, 2026-09-08), dispatched by COORD as the thing
that runs **before a line of the increment is written**.

**IT IS EXPECTED TO FAIL.** Its whole job is to make the fatal path's death point *observable*, per
flavour, so the increment that severs `fatalthrow` / `fatalpanic` / the four-arg `traceback` onto the
managed walk is written against a measurement instead of against a reading of the code.

## What it measures

Whether a converted runtime **fatal** reaches `exit(2)` — and if not, **where it dies and whether Go's
text was printed first**. The sizing says: `getcallerpc`/`getcallersp` are the only stubs between
`throw` and `exit(2)`, plus `write1` on linux; so windows/darwin should print once and then die on
`getcallerpc`, and **linux should print nothing at all** and die earlier.

## Why THIS trigger, and why not the suggested one

The dispatch proposed reaching the fatal path through `sync: unlock of unlocked mutex` via
`sync.Mutex.Unlock`, as the cheapest route. **Measured, it reaches nothing.**
`src/core/sync/mutex.cs` is a whole-file `[module: GoManualConversion]` hand-own that declares its
**own** hooks —

    internal static void @throw(@string s) => throw new InvalidOperationException($"fatal error: {s}");
    internal static void fatal(@string s)  => throw new InvalidOperationException($"fatal error: {s}");

— and `:112` calls that **local** `fatal`. An unlocked-mutex unlock therefore raises a .NET
`InvalidOperationException` and never enters runtime's `fatal` → `fatalthrow`. That holds at master
**and** after C1's `01a5c803d`, which relocates those hooks into `sync/runtime_impl.cs` but keeps the
same `InvalidOperationException` shape. A probe built on it would report a clean managed exception and
prove nothing about the fatal path.

`runtime.SetFinalizer(nil, f)` is the trigger that **does** reach it, and it reaches it **at master**
with no dependency on any unlanded branch:

| side | site | what happens |
|---|---|---|
| ours | `src/core/runtime/mfinal.cs:443` (master) | `if (obj is null or NilType) @throw("runtime.SetFinalizer: first argument is nil")` |
| ours | `src/core/runtime/panic.cs:1090` | that is runtime's **real** `@throw` — `runtime` declares no local shim (checked) — which prints, then calls `fatalthrow(throwTypeRuntime)`, whose FIRST statement is the `getcallerpc()` stub |
| Go | `runtime/mfinal.go:438` (go1.24.13) | `throw("runtime.SetFinalizer: first argument is nil")` — a genuine fatal |

The message was chosen because it is present in **both** trees. `"second argument is not a function"`
exists in our corpus but **not** in Go 1.24.13's source, so it could not be diffed against the oracle.

**The markers go through `fmt`, deliberately NOT through `println`.** Go's `println` builtin lowers
into runtime's own print path (`gwrite` → `writeErr` → `write` → `write1`) — the very path predicted
dead on linux. Using it for the probe's own markers would conflate the instrument with the thing under
test.

## The ORACLE, measured rather than described

Run here at **`go1.24.13`** (one of the two pins and the hop target; **`go1.23.12` is not on this
box**, so the corpus's own pin is unmeasured by me — a runner with 1.23.12 should re-take it):

- **stdout:** `PROBE-MARK-1: reached main` — and nothing else
- **stderr:** line 1 is `PROBE-MARK-1E: reached main (fd 2)`; line 2 is exactly
  `fatal error: runtime.SetFinalizer: first argument is nil`; then a goroutine header of the shape
  `goroutine 1 gp=0xADDR m=0 mp=0xADDR [running]:` and the traceback — **66 stderr lines** in total,
  whose leading frames are, in order:
  `runtime.throw` → `runtime.SetFinalizer` → `main.main` → `runtime.main` → `runtime.goexit`
- **exit code: 2**
- `PROBE-MARK-2` appears on **neither** stream

## The TWO markers, and what each absence means (R's ask, mailbox `fa6ed34`)

The probe writes a distinct marker to **fd 1 AND fd 2** before it reaches the fatal, both through
`fmt`/`os` rather than through runtime's own print path. That makes a mute reading **falsifiable
instead of unattributable**:

| observation | reading |
|---|---|
| both markers, no fatal text | the capture works and runtime's **write path** is dead — the linux prediction |
| **neither** marker | the **instrument** is broken, not the write path; nothing else in the run is readable |
| MARK-1 but no MARK-1E | fd 2 specifically is not being captured |
| stderr **completely** empty, marker and all | per R's `mutecrash` control a MANAGED death on linux is *not* mute (the CLR writes its own text to fd 2), so a wholly empty fd 2 means the death is a **SIGNAL** rather than managed — which separates the two candidate mechanisms with no extra run |

(Paths and addresses are redacted above per the standing security order; nothing machine-identifying
belongs in a committed artifact.)

## ⚠ DO NOT USE `go run` — IT MASKS THE EXIT CODE

Measured here, one variable: `go run .` reports **exit 1** for this program, while the built binary
reports **exit 2**. The exit code *is* the primary reading of this probe, so `go run` would have made
both runners report the wrong number and "not 2" would have looked true on the oracle itself.

**Always `go build -o <bin> . && <bin>`**, and capture the code as the first statement after the run:

    go build -o probe . && ./probe > out.txt 2> err.txt; rc=$?; echo "EXIT=$rc"

## How to run it

**Toolchain (two-pin protocol).** Build the converter at **go1.24.13**; run the conversion and the
oracle with the environment **re-exported** to the corpus pin — `-goroot` alone does not isolate the
loader (i9's `1cf3af3`). Print bare `go version` and `go env GOROOT` and **abort on a mismatch**;
printing a pin is not checking it.

**Oracle side.** `go build` + run as above, in this directory.

**Converted side.** Convert this package with an **explicit output positional** (single-package mode
emits *beside its input* otherwise) and an explicit `-go2cspath` at the repo's `src`:

    go2cs -go2cspath <repo>/src <this-dir> <scratch-out>

then build and run the emitted project, capturing stdout, stderr and the exit code **separately and
verbatim**. Nothing emitted into a scratch root is postable as-is — a scratch output root injects
absolute paths into `GoPositionMap` — so post **counts, names, exit codes and the first stderr line**,
not emitted lines.

**Runners:** i7 on windows, R on linux, under the same shape. Both post stdout, stderr and the exit
code verbatim (redacted for paths).

## Predictions, per flavour, with falsifiers

Stated before either run, so no reading can be rationalised afterwards.

| flavour | stdout | stderr | exit |
|---|---|---|---|
| **oracle (any)** | MARK-1 | MARK-1E, then Go's text **once** + traceback | **2** |
| **converted windows / darwin** | MARK-1 | MARK-1E, then Go's text **once**, then a `NotImplementedException` naming **`getcallerpc`** | **NOT 2** |
| **converted linux** | MARK-1 | **MARK-1E, then NOTHING of Go's text**; the exception names **`write1`**, not `getcallerpc` | **NOT 2** |

**MARK-1E is the load-bearing addition on linux.** The linux prediction is a *null* — no fatal text —
and a null is worth nothing unless the instrument is proven live in the same run. MARK-1E on fd 2,
written through a path that does **not** touch runtime's `write1`, is that proof.

`PROBE-MARK-2` must appear on no flavour, on either stream.

**Falsifiers — any one of these refutes part of the sizing, and I want the reading more than the
green:**

1. **linux prints Go's text** → the `write1` reading is wrong and that item comes off the increment's
   list.
2. **windows/darwin print the text TWICE** → something already supplies a PC, `unwinder.initAt`'s
   first branch is being reached, and its `@throw` re-entry is live *today* rather than a prediction.
3. **any flavour exits 2 with a Go-shaped traceback** → the fatal path already works and the whole
   increment is unnecessary.
4. **`PROBE-MARK-2` prints** → the fatal was swallowed and is recoverable, which is a separate and
   worse defect than the one being measured.
5. **the exception names neither `getcallerpc` nor `write1`** → the death is somewhere the node census
   did not reach, and the census is wrong rather than incomplete.

## Limits

- **The converted side is UNMEASURED by its author.** This host has no C# toolchain of any kind — no
  dotnet, no mono, no csc — so every converted-side row above is a prediction read at the code, with
  file and line, and the runners supply the measurement.
- The oracle row was taken at **go1.24.13**, not at the corpus pin **go1.23.12**, which is not
  installed here. Stated rather than extrapolated.
