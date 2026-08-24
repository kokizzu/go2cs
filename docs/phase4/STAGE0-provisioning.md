# STAGE 0 — fleet provisioning record (.NET 10 hop)

**Executed per [`../DotNetMigration.md`](../DotNetMigration.md) §2, as written** (coordinator
dispatch, mailbox 2026-08-24: *"SDK 10.0.4xx across the fleet, `dotnet --version` recorded per box,
the stage record citing the runbook section"*). This file is the canonical per-machine provisioning
note §2 step 2 calls for; machines are appended as their legs are provisioned.

Channel `10.0.4xx` resolved to **SDK 10.0.400** on both OSes on 2026-08-24 — record the resolved
number per box, never the channel alone (patch levels drift across a fleet).

## The commands

Written out here because a row cannot cite them: each row below records what a box *resolved to*, not
what to type, and a row saying "same commands as the row above" terminates in no command at all.

**Windows**, PowerShell native (a bash wrapper eats `$env:USERPROFILE` before PowerShell sees it, and
PowerShell rejects POSIX-form `/c/...` script paths — both are session mechanics, not runbook defects):

```powershell
# 0. BEFORE — both hives, per DotNetMigration.md §2(2). The Test-Path is the one that
#    catches a side-by-side install the default hive cannot report.
dotnet --version; dotnet --list-sdks; dotnet --list-runtimes
Test-Path "$env:USERPROFILE\dotnet10\dotnet.exe"

# 1. INSTALL — user-local, -NoPath, machine default untouched (§2(1)).
Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1 -OutFile .\dotnet-install.ps1
.\dotnet-install.ps1 -Channel 10.0.4xx -InstallDir "$env:USERPROFILE\dotnet10" -NoPath

# 2. AFTER — both hives again. The default's list must be UNCHANGED from step 0.
dotnet --version; dotnet --list-sdks
& "$env:USERPROFILE\dotnet10\dotnet.exe" --list-sdks
& "$env:USERPROFILE\dotnet10\dotnet.exe" --list-runtimes
```

**Linux / WSL** is the same shape with `dotnet-install.sh`, `--channel`/`--install-dir`/`--no-path`,
and `$HOME/dotnet10`.

`--list-runtimes` on the side-by-side root is not optional: the SDK carries its own host, and the
runtime patch it brings (10.0.11 for SDK 10.0.400) is the number every later leg's
`FrameworkDescription` probe must match. The SDK number alone does not imply it.

---

## Machine: R's box (win-x64, the lane worktree host)

Provisioned 2026-08-24 (lane R). Official `dotnet-install.ps1`, `-Channel 10.0.4xx -NoPath`,
user-local per §2(1).

| | value |
|:--|:--|
| Side-by-side root | `C:\Users\rcarroll\dotnet10` |
| SDK installed | **10.0.400** |
| Host it carries | 10.0.11 x64 |
| Machine default AFTER (untouched, §2(1)) | `dotnet --version` → **9.0.317** |
| Pre-existing SDKs (`--list-sdks` before) | 9.0.316, 9.0.317 (`C:\Program Files\dotnet\sdk`) |
| Pre-existing runtimes (before) | NETCore.App 8.0.29, 9.0.18, 9.0.19, **10.0.11** (`C:\Program Files\dotnet`) |
| `global.json` | none, per §2(4) — not before the TFM moves |

⚠ **This box already carried a 10.0.11 RUNTIME under the machine default before Stage 0** (VS/servicing
installed). That is precisely §2(3)'s hazard made concrete: an unproven "new-runtime leg" here could
silently run on the machine-default install rather than the side-by-side root — the probe discipline
is not optional on this box, it is the difference between two identically-versioned runtimes.

## Machine: WSL Ubuntu-22.04 (linux-x64, the Linux lane distro)

Provisioned 2026-08-24 (lane R). Official `dotnet-install.sh`, `-Channel 10.0.4xx -NoPath`,
user-local per §2(1).

| | value |
|:--|:--|
| Side-by-side root | `/root/dotnet10` |
| SDK installed | **10.0.400** |
| Host it carries | 10.0.11 x64 (NETCore.App + AspNetCore.App 10.0.11) |
| Machine default AFTER (untouched) | `/usr/local/bin/dotnet --version` → **9.0.317** |
| Pre-existing SDKs (before) | 9.0.317 (`/usr/share/dotnet/sdk`) |
| `global.json` | none, per §2(4) |

## Machine: i9 (win-x64, the sweeper)

Provisioned 2026-08-24 (i9's session). Official `dotnet-install.ps1`, `-Channel 10.0.4xx -NoPath`,
user-local per §2(1). Commands per *The commands* above, PowerShell native (not bash-wrapped, so
neither of R's two invocation traps applied here).

| | value |
|:--|:--|
| Side-by-side root | `C:\Users\rcarroll\dotnet10` |
| SDK installed | **10.0.400** |
| Host it carries | 10.0.11 x64 (NETCore.App + AspNetCore.App + WindowsDesktop.App 10.0.11) |
| Machine default AFTER (untouched, §2(1)) | `dotnet --version` → **9.0.317** |
| Pre-existing SDKs (`--list-sdks` before) | 9.0.317 (`C:\Program Files\dotnet\sdk`) |
| Pre-existing runtimes (before) | AspNetCore/NETCore/WindowsDesktop.App 6.0.36, 7.0.20, 8.0.30, 9.0.19 (`C:\Program Files\dotnet`) — no 10.x present |
| `global.json` | none, per §2(4) — not before the TFM moves |

No repeat of R's box hazard here: this machine carried **no** pre-existing 10.x runtime under the
machine default before Stage 0, so unlike R's box, an unproven "new-runtime leg" here would not
silently collide with an identically-versioned default — still probing per §2(3) regardless, since
that discipline is the fleet's standing rule, not a per-box exception.

## Machine: i7-5820K (win-x64, the interim coordinator)

Provisioned 2026-08-24. Commands per *The commands* above, PowerShell native.

| | value |
|:--|:--|
| Side-by-side root | `C:\Users\ritchie\dotnet10` (account `ritchie` — the root is derived from `$env:USERPROFILE`, not copied from a sibling row) |
| SDK installed | **10.0.400** |
| Host it carries | 10.0.11 x64 (NETCore.App + AspNetCore.App + WindowsDesktop.App 10.0.11) |
| Machine default AFTER (untouched, §2(1)) | `dotnet --version` → **9.0.317** |
| Pre-existing SDKs (`--list-sdks` before) | 2.1.202, 2.1.504, 2.1.505, 2.1.512, 5.0.100, 9.0.317 (`C:\Program Files\dotnet\sdk`) — unchanged after |
| Pre-existing runtimes (before) | NETCore.App 2.0.9 … 9.0.19; AspNetCore.App 2.1.8 … 9.0.19; WindowsDesktop.App 3.1.32 … 9.0.19 (`C:\Program Files\dotnet`) — **no 10.x present**, unchanged after |
| User-local hive BEFORE (§2(2) probe) | `~\dotnet10` absent; `~\.dotnet` holds only first-run sentinels and tools (no `sdk/`, no `shared/`); `%LOCALAPPDATA%\Microsoft\dotnet` holds only `optimizationdata` — **no pre-existing side-by-side install** |
| `global.json` | none, per §2(4) — not before the TFM moves |

Like the i9 and unlike R's box, this machine carries **no** 10.x runtime under the machine default, so
its two hives are disjoint by version and a leg's identity is unambiguous from the probe alone. That
same absence is what makes it the box where §5's inverted exposure bites hardest: after the TFM moves,
every apphost-launched instrument here needs `DOTNET_ROOT`, because the machine default has nothing to
run a new-TFM binary on (see the trap-5 matrix, measured on this box).

**First-run experience:** run without the suppression variables, so the SDK's first invocation wrote
the usual user-level state and **replaced this account's ASP.NET Core HTTPS development certificate**.
No machine-level effect; recorded because the next box may care.

## Machine: G's laptop (win-x64) — PENDING, owner: G's session

Commands per *The commands* above. Record the resolved SDK number, the resolved install root, and both
hives' inventories here.

---

## Shakedown notes (first execution of §2 — per the dispatch, gaps fix in the runbook)

1. **Runbook gap, fixed:** §2(2) said "the machine's provisioning note" without naming where notes
   live. This file is now that home, and §2(2) names it.
2. **Two invocation traps burned on the first box, neither a runbook defect:** (a) a bash-quoted
   `"$env:USERPROFILE"` is eaten by bash before PowerShell sees it — pass literal Windows paths;
   (b) PowerShell rejects POSIX-form (`/c/...`) script paths — `&` needs the `C:\...` form. Both are
   the session-mechanics family, recorded here so the i9/G rows don't re-pay them.
3. **The channel form works as documented**: `-Channel 10.0.4xx` resolved to 10.0.400 identically on
   both OSes; no version had to be guessed.
4. **Runbook gap, fixed:** the rows cited each other for commands (*"same commands as the i9 row"* /
   *"verbatim from the pending row above"*) and no row carried any. The *The commands* section is now
   the single place they live, and a row cites it rather than a sibling.
5. **Runbook gap, fixed:** §2(1) said "user-local" without saying *whose* user. A literal root copied
   from a sibling row provisions the wrong account on a fleet with differing usernames; the install
   directory derives from `$env:USERPROFILE` / `$HOME` and each row records what it resolved to.
6. **Unstated side effect, now stated:** the SDK's first-run experience fires on the first `dotnet`
   call and rewrites the account's ASP.NET Core HTTPS development certificate. It is user-level, not
   machine-level, so it stays inside the standing grant's three conditions — but "machine defaults
   untouched" does not mean "nothing changed", and a box whose dev certificate was trusted for other
   work has lost that trust. §2(1) now names the three suppression variables.
7. **Trap 5's exposure rule was wrong and is corrected in the runbook.** Apphost launch is *not*
   immune to a side-by-side root; it is immune to `PATH`. With `DOTNET_ROOT` set — which §2(3)
   requires of any real leg — an apphost-launched old-TFM binary fails exactly as a muxer-launched
   one does. What an apphost-immunity reading actually observes is a **half-constituted leg**
   (`PATH` moved, `DOTNET_ROOT` not), whose apphost instruments are still running the old runtime:
   a trap-4 false measurement that looks like a pass. The same matrix found the inverse exposure
   waiting at the TFM stage, where `DOTNET_ROLL_FORWARD` cannot help and only `DOTNET_ROOT` can.
