# Handoff — issue #34 (`deploy-core.ps1` fails on recent C# versions)

> **Transient working doc.** It exists to carry a verification task across machines. Delete it (or fold
> anything durable into `docs/`) before this branch merges to `master` — it is not part of the permanent
> documentation set.

**Branch:** `claude/github-issue-review-k7se3p` (branched from `master` @ `7729097`)
**Commit to verify:** `0b09ed73473132961ec0bbbc84eecb4780fc322c`
**Issue:** https://github.com/ritchiecarroll/go2cs/issues/34 (reporter: `masonwheeler`)

---

## 1. Session instructions — paste this as the new session's prompt

> Work in the `ritchiecarroll/go2cs` repo on branch `claude/github-issue-review-k7se3p`.
> Read `docs/handoff/ISSUE-34-verification.md` first and follow it.
>
> Your job is **verification, not diagnosis** — the fix for issue #34 is already committed
> (`0b09ed7`). This machine has the toolchain the previous session lacked (.NET SDK), so:
>
> 1. Confirm you are on a **.NET 10 SDK** (C# 14) — that is the SDK the bug needs. Report the
>    version you actually have; if it is older, say so and stop rather than reporting a green
>    build that proves nothing.
> 2. Reproduce the failure **first** (check out `master`, build `go2cs-gen`, capture the CS9273 /
>    CS1061 errors), then check out the fix branch and show the same build green. A fix verified
>    without a reproduction is a guess with better manners.
> 3. Run the verification steps in §5 of the handoff doc, in order. Record real command output —
>    you will quote it.
> 4. If further C# 14 breakages appear deeper in the solution, fix them in the same style (see §6)
>    and commit to the same branch. Do not paper over them with a `LangVersion` pin.
> 5. When the work is done, **post a single comment on issue #34** using the template in §7 —
>    root cause, the fix, how to get it, and what you actually verified (with the SDK version and
>    real output). Be honest about anything that stayed unverified. End the comment with the
>    Claude Code attribution footer.
> 6. Push the branch. **Do not open a pull request** unless I explicitly ask for one.

---

## 2. What the reporter hit

Running `.\deploy-core.ps1` on a .NET 10 SDK, the verification build fails at the **first** project —
`go2cs-gen`, the Roslyn analyzer every converted project references — with 7 errors, so all 304
deployed projects go with it. The reported diagnostics:

```
StructTypeTemplate.cs(392,21): error CS9273: In language version 14.0, 'field' is a keyword within a
                               property accessor. Rename the variable or use the identifier '@field' instead.
StructTypeTemplate.cs(394,35): error CS1061: 'string' does not contain a definition for 'DeclaredAccessibility'
StructTypeTemplate.cs(394,90): error CS1061: 'string' does not contain a definition for 'IsStatic'
StructTypeTemplate.cs(394,108): error CS1061: 'string' does not contain a definition for 'IsImplicitlyDeclared'
StructTypeTemplate.cs(397,49): error CS1061: 'string' does not contain a definition for 'Name'
StructTypeTemplate.cs(400,45): error CS1061: 'string' does not contain a definition for 'Type'
StructTypeTemplate.cs(400,115): error CS1061: 'string' does not contain a definition for 'Name'
```

Everything else in that log is pre-existing **warnings** (nullable + IL trim analysis in `golib`) —
noise, not part of this issue.

## 3. Root cause

`getMetadataStructFields` — the metadata fallback that enumerates a cross-package embedded type's
public fields — looped over `IFieldSymbol field`. That local function is nested inside the
`PromotedStructDeclarations` **get** accessor, and **C# 14 makes `field` a keyword inside a property
accessor** (it names the property's synthesized backing field).

`go2cs-gen.csproj` sets `<LangVersion>latest</LangVersion>` and the repo has no `global.json`, so the
language version floats with whatever SDK is installed. On .NET 9 (C# 13) `field` is an ordinary
identifier and the code is fine; on .NET 10 (C# 14) the declaration is CS9273 and each of its four
uses binds to a synthesized `string` backing field instead of the loop variable — hence the five
CS1061s on `string`. The generator's *behavior* was never wrong; the identifier simply expired.

This is why the maintainer's machine stayed green and the reporter's did not.

## 4. What is already committed on the branch (`0b09ed7`)

| File | Change |
|---|---|
| `src/gen/go2cs-gen/Templates/StructType/StructTypeTemplate.cs` | `field` → `fieldSymbol` (4 uses, one scope), plus a comment recording *why* the short name cannot come back. Pairs with the sibling `property` loop. |
| `docs/coding-style.md` | New rule 18: never name a local/parameter/pattern variable `field` inside a property accessor. |

No behavior changed — the generator's emitted output is byte-identical.

**A tree-wide sweep was already done statically** (comment/string-stripped scan of every `.cs` under
`src`, both accessor blocks and expression bodies): **exactly one site**, the one fixed. The ~20 other
files that use `field` as an identifier are method-scoped locals, lambda parameters and switch-arm
pattern variables — C# 14 leaves those alone. Converted code in `src/core` is clean too: emitted embed
accessors are expression-bodied properties whose bodies are `Ꮡ`-prefixed simple names or member
accesses, never a bare `field`, so a Go type named `field` (`reflect`, `go/types`) cannot reach the
keyword position. The C# 14 extension-block syntax was checked as well — no member named `extension`.

**Rule 18 is the chosen guard, deliberately.** A scanner test would reimplement a compiler check
heuristically for a constraint the compiler already enforces on every SDK new enough to matter. The
real gap is that an *older* SDK does not warn, and a documented rule is what closes that.

**What stayed unverified, and why this handoff exists:** the previous session had no .NET SDK (and the
proxy blocked the installer), so nothing was compiled. In particular, the reported build died at the
analyzer — **no project past `go2cs-gen` has ever been compiled under C# 14**, so whether further C# 14
breaks lurk deeper in the 304-project solution is genuinely unknown.

## 5. Verification plan

Run in order. Budget timeouts from the **top** of the ranges in `CLAUDE.md` — a healthy full run
legitimately exceeds three minutes, and killing one early reads as a failure.

### 5.0 — Environment
```
dotnet --list-sdks     # a 10.x SDK must be present, else the bug cannot reproduce
go version
```
If there is no .NET 10 SDK, **stop and report that** — a green build on .NET 9 proves nothing here,
because C# 13 never had the `field` keyword. (`-p:LangVersion=14.0` on a 9.x SDK will not help; that
Roslyn does not know C# 14.) If several SDKs are installed, remember the repo has no `global.json`, so
the newest wins — confirm which one MSBuild actually selected rather than assuming.

### 5.1 — Reproduce on `master` (do this before trusting the fix)
```
git checkout master
dotnet build src/gen/go2cs-gen/go2cs-gen.csproj -c Debug
```
Expect the 7 errors from §2. Capture the output — it is the "before" half of the issue comment.

### 5.2 — The fix builds
```
git checkout claude/github-issue-review-k7se3p
dotnet build src/gen/go2cs-gen/go2cs-gen.csproj -c Debug
```
Expect success. Warnings are fine and out of scope.

### 5.3 — The whole solution builds under C# 14 (**the step that matters most**)
This is the definitive sweep: the compiler finds every remaining C# 14 breakage, exhaustively, in a way
no text scan can.
```
dotnet build src/go2cs.slnx        -c Debug -clp:ErrorsOnly    # converter-dev workspace
dotnet build src/go2cs-stdlib.slnx -c Debug -clp:ErrorsOnly    # all 304 converted projects
```
Timeout ≥600s each; cold restore adds minutes. Bucket any errors by code (`error CS####`) — dependents
of a failed project are *skipped*, not errored, so a single leaf failure can hide a long tail. If
anything fails, see §6.

### 5.4 — The reported command, end to end
```
.\deploy-core.ps1
```
from `src\`. This is the reporter's actual scenario — it stages to `%GOPATH%\src\go2cs` and builds the
generated `go2cs-core.slnx`. It is the one result the issue comment must be able to claim.

### 5.5 — No regression on the normal SDK / no behavioral drift
The generator changed, and it is the analyzer for every converted project, so prove its emission is
unchanged rather than assuming it from "only a rename":
```
cd src\tests\Behavioral
.\check-no-regression.ps1        # transpile-only drift gate;  timeout 700s
.\run-behavioral.ps1             # 4 phases, full corpus;      timeout 2100s
```
CNR touches the converter only (unchanged here), so expect a clean `git status`. The behavioral suite's
**Compile** phase is what actually exercises the analyzer; expect the current baseline — 545/545
transpile+compile+golden, 515/515 stdout comparisons.

⚠ Machine hygiene from `CLAUDE.md`, worth re-reading if anything dies mid-run: a truncated log with
exit `-1` and no diagnostic means the run was **killed externally** (a name-matched `Stop-Process`, or
a machine-global `dotnet build-server shutdown` from another worktree) — not a compile failure. Do not
run `build-server shutdown` while sibling sessions may be building; isolate with
`MSBUILDDISABLENODEREUSE=1` and `-p:UseSharedCompilation=false` instead.

## 6. If further C# 14 breakages appear

Likely candidates, in rough order: another contextual-keyword collision, or overload-resolution
ambiguity from C# 14's new `Span<T>`/`ReadOnlySpan<T>` conversions (CS0121) — `golib` carries large
overload sets, so it is the plausible site.

Fix them the same way this one was fixed:

1. **Rename the identifier**, do not escape it with `@` and do not pin `LangVersion`. A pin freezes the
   repo against the future and hides the next instance of the same class; `@field` compiles but reads
   as a workaround for a permanent constraint.
2. **Record why at the site** — this codebase comments the *why* heavily; match that.
3. **Extend `docs/coding-style.md` rule 18** if the new break is a genuinely different class (a new
   contextual keyword, say). Do not add a rule for a one-off mechanical slip.
4. **Re-sweep** — for a contextual-keyword class the appendix scanner generalizes by changing the
   `FIELD` regex; for anything else, the full solution build in §5.3 *is* the sweep.
5. Commit to the same branch, one commit per distinct cause.

If a failure turns out to be a real converter defect rather than a language-version collision, that is
a different animal — lock it in with a behavioral test per `CLAUDE.md`'s "Adding a regression test"
flow, and note it separately in the issue comment.

## 7. Posting back to issue #34

**One comment**, after the verification is done — not a running commentary. Repo etiquette is to
comment only when a reply is genuinely useful; this one is, because someone is blocked and deserves to
know it is fixed and how to get it.

Content it must carry:

- **Root cause in plain terms** — C# 14 made `field` a keyword inside property accessors; the analyzer
  had a loop variable by that name; `LangVersion` is `latest` with no `global.json`, so a .NET 10 SDK
  turns it into a hard build break, and because `go2cs-gen` is the analyzer everything references, the
  whole solution fails before `deploy-core` reaches a single package. Credit the report for pinning it
  precisely — the log had everything needed.
- **The fix** — renamed the loop variable; no behavior change. Branch `claude/github-issue-review-k7se3p`,
  commit `0b09ed7` (update the hash if you add commits), plus a coding-style rule so it cannot come back
  silently on an SDK that does not warn.
- **What you verified**, concretely: the SDK version you used, the reproduction on `master`, the green
  builds, `deploy-core.ps1` completing, and the regression-gate results. Quote real output.
- **Anything still unverified or newly found** — say it plainly. If §5.3 surfaced more C# 14 issues,
  list them and their status.
- **How to consume it** — check out the branch now, or wait for it to land on `master`. Do not promise
  a timeline or a release; that is the maintainer's call.

Do **not** describe the internal process (sweeps, session handoffs, gate names) — the reporter cares
about cause, fix, and availability.

End the comment with the attribution footer, verbatim, as the final lines:

```
---
_Generated by [Claude Code](https://claude.ai/code)_
```

## 8. Do not

- Open a pull request unless the user explicitly asks.
- Push to any branch other than `claude/github-issue-review-k7se3p`.
- Pin `LangVersion` or add a `global.json` to make the error go away.
- Re-baseline behavioral goldens (`--update-targets` / `UpdateTestTargets`) — nothing here should change
  generated output. If a golden moves, that is a finding to investigate, not a baseline to refresh.
- Report a green build without saying which SDK produced it.

## Appendix — the static sweep used

Only a convenience for a *new* contextual-keyword class; the §5.3 solution build is authoritative.
Needs Python 3. Finds bare `field` simple names inside `get`/`set`/`init` bodies, ignoring comments and
string literals.

```python
import re, os, sys

def strip(src):                      # blank out comments and string/char literals
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '/' and src[i+1:i+2] == '/':
            j = src.find('\n', i); j = n if j < 0 else j
            out.append(' ' * (j-i)); i = j
        elif c == '/' and src[i+1:i+2] == '*':
            j = src.find('*/', i+2); j = n if j < 0 else j+2
            out.append(''.join(ch if ch == '\n' else ' ' for ch in src[i:j])); i = j
        elif c == '"' and src[i-1:i] == '@':
            j = i+1
            while j < n:
                if src[j] == '"':
                    if src[j+1:j+2] == '"': j += 2; continue
                    j += 1; break
                j += 1
            out.append(''.join(ch if ch == '\n' else ' ' for ch in src[i:j])); i = j
        elif c in '"\'':
            q, j = c, i+1
            while j < n:
                if src[j] == '\\': j += 2; continue
                if src[j] == q: j += 1; break
                if src[j] == '\n': break
                j += 1
            out.append(' ' * (j-i)); i = j
        else:
            out.append(c); i += 1
    return ''.join(out)

ACCESSOR = re.compile(r'(?<![\w.@])(get|set|init)\s*(\{|=>)')
FIELD    = re.compile(r'(?<![\w.@])field(?![\w])')          # <-- swap for another keyword

def block_end(s, k):
    d = 0
    while k < len(s):
        if s[k] == '{': d += 1
        elif s[k] == '}':
            d -= 1
            if d == 0: return k
        k += 1
    return len(s)

def stmt_end(s, k):
    d = 0
    while k < len(s):
        if s[k] in '([{': d += 1
        elif s[k] in ')]}': d -= 1
        elif s[k] == ';' and d <= 0: return k
        k += 1
    return len(s)

found = 0
for root, dirs, files in os.walk(sys.argv[1] if len(sys.argv) > 1 else 'src'):
    dirs[:] = [d for d in dirs if d not in ('bin', 'obj', '.git', 'Generated', 'archived')]
    for f in files:
        if not f.endswith('.cs'): continue
        p = os.path.join(root, f)
        raw = open(p, encoding='utf-8', errors='replace').read()
        s = strip(raw)
        for m in ACCESSOR.finditer(s):
            a = m.end()-1 if m.group(2) == '{' else m.end()
            b = block_end(s, a) if m.group(2) == '{' else stmt_end(s, a)
            for hit in FIELD.finditer(s[a:b]):
                line = s.count('\n', 0, a + hit.start()) + 1
                print(f"{p}:{line}  [{m.group(1)}]  {raw.splitlines()[line-1].strip()[:110]}"); found += 1
print(f"--- {found} site(s)")
```

Expected on this branch: `--- 0 site(s)`. As a positive control, run it against `master`'s copy of
`StructTypeTemplate.cs` — it reports the 7 uses that became the 7 compiler errors. A scanner that
cannot find the known bug is not evidence of absence. Note it covers explicit accessor bodies; expression-bodied
properties need the companion pass (scan every `=>` statement body), which the previous session ran
separately and which also came back clean.
