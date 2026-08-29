using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;
using errors = go.errors_package;

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

    // §3.3: the honest wall must answer in GO'S OWN ERROR CURRENCY, not merely with a name. The
    // design already said so — "returns ENOTSUP naming the field ... never a silent drop (reach
    // Go's own gate, fail Go's own way)" — and the implementation drifted to a bare
    // errors.New(message), which names the field but carries no kind.
    //
    // The kind is load-bearing, not cosmetic. Go's own skip guards call
    // testenv.SyscallIsNotSupported, which accepts an Errno of EPERM/EROFS/EINVAL,
    // fs.ErrPermission, or errors.ErrUnsupported — and nothing else. A kindless refusal satisfies
    // none of them, so on an unprivileged host EIGHT tests in Go's syscall suite that Go SKIPS (it
    // attempts the operation, the kernel answers EPERM, the guard fires) instead FAIL against the
    // converted corpus. The sibling hand-own syscall_linux_impl.cs already fixed this exact shape
    // for runtime_doAllThreadsSyscall, recording the same reasoning: a throwing stub "turned that
    // skip into an infrastructure-error", and ENOTSUP restored it.
    //
    // Asserting the KIND and the NAME, never the rendered message — this file's established idiom.
    [TestMethod]
    public void UnsupportedSysProcAttrFieldAnswersErrUnsupported()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("the posix_spawn seam is the linux flavor");
            return;
        }

        // Cloneflags is outside the mapped set, so the seam must refuse before it ever spawns.
        // /bin/true exists and would otherwise succeed — the refusal itself is under test.
        var sys = new go.syscall_package.SysProcAttr
        {
            Cloneflags = (uintptr)go.syscall_package.CLONE_NEWUSER,
        };

        var attr = new go.syscall_package.ProcAttr
        {
            Files = new slice<uintptr>(new uintptr[] { 0, 1, 2 }),
            Sys = new StandardBox<go.syscall_package.SysProcAttr>(sys),
        };

        var (pid, _, err) = syscall.StartProcess(
            "/bin/true"u8,
            new slice<@string>(new @string[] { "/bin/true"u8 }),
            new StandardBox<go.syscall_package.ProcAttr>(attr));

        Assert.AreEqual((nint)0, pid, "a refused spawn must not report a pid");
        Assert.IsNotNull(err, "an unmapped SysProcAttr field must be refused, never silently dropped");

        // THE GATE: Go's own predicate must accept this error. errors.Is walks the chain and
        // consults Errno.Is, which maps ENOTSUP/ENOSYS/EOPNOTSUPP onto errors.ErrUnsupported.
        // err.Error(), not err: interpolating the error INTERFACE renders the box address
        // ("Got: 0x716590d03af0" in this witness's own first red run), which tells a reader
        // nothing about why the gate failed.
        Assert.IsTrue(errors.Is(err, errors.ErrUnsupported),
            "the seam's refusal must satisfy errors.Is(err, errors.ErrUnsupported) — that is what "
          + $"testenv.SyscallIsNotSupported tests, and eight syscall-suite skips ride on it. "
          + $"Got: {err.Error()}");

        // The design's other half: the wall stays NAMED. A kind without a name would trade one
        // regression for another.
        StringAssert.Contains(err.Error().ToString(), "Cloneflags",
            "the refusal must still name the field it could not express");
    }
}
