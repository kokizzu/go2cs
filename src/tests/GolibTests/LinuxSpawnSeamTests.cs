using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;

namespace GolibTests;

[TestClass]
public class LinuxSpawnSeamTests
{
    // The exec-wall implementation's two measured gates (design §5.1 and OQ-2, ratified
    // 2026-08-22). Both drive the CONVERTED surface — syscall.StartProcess/Wait4 — because the
    // seam under test is the posix_spawn hand-own behind forkExec, not a P/Invoke in isolation.
    // Linux-only by construction (the seam is the linux flavor); on Windows both report
    // Inconclusive rather than vacuous green.

    // §5.1: glibc reports child-setup and exec failures SYNCHRONOUSLY from posix_spawn — the
    // property that lets the hand-own delete Go's status-pipe protocol. A missing binary must
    // fail HERE, as ENOENT from Start, never as a child that exits 127.
    [TestMethod]
    public void SpawnFailureIsSynchronous()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("the posix_spawn seam is the linux flavor");
            return;
        }

        var attr = new go.syscall_package.ProcAttr
        {
            Files = new slice<uintptr>(new uintptr[] { 0, 1, 2 }),
        };

        var (pid, _, err) = syscall.StartProcess(
            "/nonexistent-go2cs-spawn-gate"u8,
            new slice<@string>(new @string[] { "/nonexistent-go2cs-spawn-gate"u8 }),
            new StandardBox<go.syscall_package.ProcAttr>(attr));

        Assert.AreEqual((nint)0, pid, "a failed spawn must not report a pid");
        Assert.IsNotNull(err, "spawning a missing binary must fail synchronously");

        // The corpus's own errno-comparison idiom: the error interface carries a boxed Errno, and
        // AreEqual is what converted Go uses for `err == ENOENT`. Asserting the VALUE (not a
        // rendered message) is both stronger and rendering-independent.
        Assert.IsTrue(AreEqual(err, syscall.ENOENT),
            $"expected ENOENT from the spawn call itself, got: {err}");
    }

    // OQ-2: the CLR installs its own SIGCHLD handling for System.Diagnostics.Process; the gate
    // proves its reaper is pid-targeted and does NOT steal children this seam spawns. The child
    // exits UNOBSERVED (well before the wait), GC pressure runs meanwhile, and a delayed Wait4
    // must still return the pid with its exit status — ECHILD here would mean the runtime reaped
    // our child out from under Go's wait protocol, the failure mode the design refuses to assume
    // away (the Mono precedent).
    [TestMethod]
    public void UnobservedChildSurvivesUntilWait()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("the posix_spawn seam is the linux flavor");
            return;
        }

        var attr = new go.syscall_package.ProcAttr
        {
            Files = new slice<uintptr>(new uintptr[] { 0, 1, 2 }),
        };

        var (pid, _, err) = syscall.StartProcess(
            "/bin/true"u8,
            new slice<@string>(new @string[] { "/bin/true"u8 }),
            new StandardBox<go.syscall_package.ProcAttr>(attr));

        Assert.IsNull(err, $"spawning /bin/true failed: {err}");
        Assert.IsTrue(pid > 0, "no pid from a successful spawn");

        // Let the child exit unobserved while the GC churns — the window where a wrong reaper
        // would consume the zombie.
        for (int i = 0; i < 8; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            System.Threading.Thread.Sleep(200);
        }

        ref var status = ref heap(new go.syscall_package.WaitStatus(), out var Ꮡstatus);
        var (waited, werr) = syscall.Wait4(pid, Ꮡstatus, 0, nil);

        Assert.IsNull(werr, $"Wait4 failed — ECHILD here means the runtime's reaper stole the child: {werr}");
        Assert.AreEqual(pid, waited, "Wait4 returned a different pid");
        Assert.IsTrue(status.Exited(), "child status did not decode as exited");
        Assert.AreEqual((nint)0, status.ExitStatus(), "/bin/true must exit 0");
    }
}
