// ParseDebugVarsAtStartTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using Δruntime = go.runtime_package;

namespace GolibTests;

// PARSEDEBUGVARS RUNS AT PROCESS START, as Go's schedinit runs it (COORD ruling 2026-09-27, ledger
// ea8a9a8b7e). runtime's module initializer (goenvs_impl.cs) now calls parsedebugvars right after it
// snapshots envs, so every dbgvar default applies and a GODEBUG or GOTRACEBACK present at START is parsed.
//
// A converted program cannot be started from here with its own environment, so the start-time state is
// read in two ways. Arm 1 reads THIS process's state as its module initializer left it. Arms 2-4 re-run
// the start sequence (envs, then parsedebugvars) over a chosen environment through runtime's seams, then
// run it again over the saved environment to restore. MSTest runs this assembly serially (no
// [Parallelize]), which is what makes swapping the runtime's environment safe for the length of an arm.
//
// Arms and their red plants (each made to fail once by removing its part of the cut):
//   1. with no GODEBUG at start, debug.profstackdepth is Go's default 128 and cgocheck is 1.
//      Red: remove the module initializer's parsedebugvars call (profstackdepth reads 0, since the
//      hand-applied 128 of mprof_impl.cs is retired).
//   2. GODEBUG=profstackdepth=0 and =N are honored. No plant isolates the start-ORDER hazard the
//      retired patch carried; arm 1's plant is the start-time half, and this arm pins the values.
//   3. GODEBUG=cgocheck=0 at start turns the pinner's cgo check off, because the check reads
//      debug.cgocheck. Red: restore pinner_impl.cs's private cached GODEBUG parse.
//   4. (Windows) GOTRACEBACK=wer at start reaches enableWER, which clears SEM_NOGPFAULTERRORBOX
//      through kernel32 and does not throw. Red: remove the hand-owned enableWER so the converted
//      stdcall0 -> asmstdcall body runs, and the arm names the NotImplementedException.
[TestClass]
public class ParseDebugVarsAtStartTests
{
    private struct Leaf
    {
        public long x;
    }

    private struct Mid
    {
        public ж<Leaf> o;
    }

    private static string[] Without(string[] environment, params string[] keys)
    {
        return environment.Where(entry => !keys.Any(key => entry.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    private static string[] With(string[] environment, string entry)
    {
        string key = entry[..entry.IndexOf('=')];
        return Without(environment, key).Append(entry).ToArray();
    }

    // Re-run the start sequence over `environment`, run `body`, and always restore the saved start.
    private static void AtStart(string[] environment, Action body)
    {
        string[] saved = Δruntime.GoStartEnvironment();

        try
        {
            Δruntime.GoParseDebugVarsAtStart(environment);
            body();
        }
        finally
        {
            Δruntime.GoParseDebugVarsAtStart(saved);
        }
    }

    [TestMethod]
    public void AProcessStartedWithoutGodebugHasGosDefaults()
    {
        string[] start = Δruntime.GoStartEnvironment();

        if (start.Any(entry => entry.StartsWith("GODEBUG=", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.Inconclusive("this process was started with a GODEBUG, so its defaults are not Go's bare defaults");
            return;
        }

        Assert.AreEqual(128, Δruntime.GoDebugProfStackDepth, "debug.profstackdepth at start (Go's dbgvars default)");
        Assert.AreEqual(1, Δruntime.GoDebugCgoCheck, "debug.cgocheck at start (parsedebugvars' default)");
    }

    [TestMethod]
    public void GodebugProfstackdepthAtStartIsHonoredAtZeroAndAtN()
    {
        string[] bare = Without(Δruntime.GoStartEnvironment(), "GODEBUG", "GOTRACEBACK");

        AtStart(With(bare, "GODEBUG=profstackdepth=0"), () =>
            Assert.AreEqual(0, Δruntime.GoDebugProfStackDepth, "GODEBUG=profstackdepth=0"));

        AtStart(With(bare, "GODEBUG=profstackdepth=7"), () =>
            Assert.AreEqual(7, Δruntime.GoDebugProfStackDepth, "GODEBUG=profstackdepth=7"));

        AtStart(bare, () =>
            Assert.AreEqual(128, Δruntime.GoDebugProfStackDepth, "no GODEBUG: the default"));
    }

    [TestMethod]
    public void GodebugCgocheckZeroAtStartTurnsThePinnerCheckOff()
    {
        string[] bare = Without(Δruntime.GoStartEnvironment(), "GODEBUG", "GOTRACEBACK");
        var unpinned = new StandardBox<Mid>(new Mid { o = new StandardBox<Leaf>(new Leaf { x = 1 }) });

        AtStart(bare, () =>
        {
            Assert.AreEqual(1, Δruntime.GoDebugCgoCheck);
            Assert.ThrowsException<PanicException>(() => Δruntime.GoCgoCheckPointer(unpinned, true),
                "cgocheck=1: an unpinned level-1 pointer must fail the check");
        });

        AtStart(With(bare, "GODEBUG=cgocheck=0"), () =>
        {
            Assert.AreEqual(0, Δruntime.GoDebugCgoCheck);
            Δruntime.GoCgoCheckPointer(unpinned, true);
        });
    }

    private const uint SemNoGpFaultErrorBox = 0x0002;

    [DllImport("kernel32.dll")]
    private static extern uint GetErrorMode();

    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint mode);

    [TestMethod]
    public void GotracebackWerAtStartReachesEnableWerAndClearsTheFaultBox()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("enableWER acts only on Windows; elsewhere it is Go's own empty stub");
            return;
        }

        string[] bare = Without(Δruntime.GoStartEnvironment(), "GODEBUG", "GOTRACEBACK");
        uint original = GetErrorMode();

        try
        {
            SetErrorMode(original | SemNoGpFaultErrorBox);

            AtStart(With(bare, "GOTRACEBACK=wer"), () => { });

            Assert.AreEqual(0u, GetErrorMode() & SemNoGpFaultErrorBox, "enableWER clears SEM_NOGPFAULTERRORBOX, as Go's does");
        }
        catch (Exception ex) when (ex is not AssertFailedException)
        {
            Assert.Fail($"GOTRACEBACK=wer at start threw {ex.GetType().FullName}: {ex.Message}");
        }
        finally
        {
            SetErrorMode(original);
        }
    }
}
