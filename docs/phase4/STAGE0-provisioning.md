# STAGE 0 — fleet provisioning record (.NET 10 hop)

**Executed per [`../DotNetMigration.md`](../DotNetMigration.md) §2, as written** (coordinator
dispatch, mailbox 2026-08-24: *"SDK 10.0.4xx across the fleet, `dotnet --version` recorded per box,
the stage record citing the runbook section"*). This file is the canonical per-machine provisioning
note §2 step 2 calls for; machines are appended as their legs are provisioned.

Channel `10.0.4xx` resolved to **SDK 10.0.400** on both OSes on 2026-08-24 — record the resolved
number per box, never the channel alone (patch levels drift across a fleet).

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

## Machine: i9 (win-x64, the sweeper) — PENDING, owner: i9's session

Not reachable from lane R. The commands, verbatim (PowerShell, per §2):

```powershell
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
.\dotnet-install.ps1 -Channel 10.0.4xx -InstallDir "$env:USERPROFILE\dotnet10" -NoPath
& "$env:USERPROFILE\dotnet10\dotnet.exe" --list-sdks     # record here
dotnet --list-sdks; dotnet --list-runtimes               # record BOTH pre-existing inventories here
dotnet --version                                          # must be unchanged
```

## Machine: G's laptop (win-x64) — PENDING, owner: G's session

Same commands as the i9 row. Record the resolved SDK number and both inventories here.

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
