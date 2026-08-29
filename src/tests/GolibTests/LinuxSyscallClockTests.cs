using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;

namespace GolibTests;

[TestClass]
public class LinuxSyscallClockTests
{
    // syscall.Gettimeofday and syscall.Time both bottom out in the same declaration —
    // `internal static partial Errno gettimeofday(ж<Timeval>)` in syscall_linux_amd64.cs — which
    // carries no body, so the PartialStubGenerator supplies a throwing one. Measured on the Linux
    // roster: syscall's own TestGettimeofday reports `infrastructure-error`, not a divergence,
    // because the host throws NotImplementedException out of the generated stub rather than
    // answering. Two of syscall's rows ride on it.
    //
    // Nothing about this needs a syscall: `Timeval` on linux/amd64 is `{ int64 Sec; int64 Usec }`,
    // blittable and free of managed references, so the struct-passing class that forced Fstat and
    // Uname into blittable mirrors does not apply here. The wall clock the CLR already reads on
    // Linux IS clock_gettime(CLOCK_REALTIME) — the same source Go's vDSO path uses — so the honest
    // implementation is to read it and fill the two fields.
    //
    // Linux-only by construction: the declaration lives under syscall/linux. On Windows this
    // reports Inconclusive rather than a vacuous green.

    [TestMethod]
    public void GettimeofdayAnswersAWallClock()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("gettimeofday is the linux flavor's declaration");
            return;
        }

        ref var tv = ref heap(new go.syscall_package.Timeval(), out var Ꮡtv);

        // Bracket the call with the CLR's own reading of the same clock, so the assertion is a
        // RANGE rather than a fixed value — the only form that is both strict and non-flaky.
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var err = syscall.Gettimeofday(Ꮡtv);
        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.IsNull(err, $"Gettimeofday must succeed, got: {err?.Error().ToString()}");

        // THE GATE: a throwing stub never reaches here, and a zero-filled struct fails it.
        Assert.IsTrue(tv.Sec >= before && tv.Sec <= after,
            $"Sec must sit inside the bracketing wall-clock reads [{before}, {after}], got {tv.Sec}");

        Assert.IsTrue(tv.Usec >= 0 && tv.Usec < 1_000_000,
            $"Usec must be a microsecond remainder in [0, 1e6), got {tv.Usec}");
    }

    [TestMethod]
    public void TimeAgreesWithGettimeofday()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("gettimeofday is the linux flavor's declaration");
            return;
        }

        // syscall.Time is the SECOND caller of the same declaration and takes a different path
        // through it (it writes through an optional out-pointer as well as returning). Covering
        // both callers means a fix that satisfies one shape cannot pass while the other stays
        // broken.
        ref var t = ref heap(new go.syscall_package.Time_t(), out var Ꮡt);
        long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var (tt, err) = syscall.Time(Ꮡt);
        long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.IsNull(err, $"Time must succeed, got: {err?.Error().ToString()}");
        Assert.IsTrue((long)tt >= before && (long)tt <= after,
            $"returned Time_t must sit inside [{before}, {after}], got {(long)tt}");
        Assert.AreEqual((long)tt, (long)t,
            "Time must write the same value through its out-pointer as it returns");
    }
}
