using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;
using io = go.io_package;
using pprof = go.runtime.pprof_package;

namespace GolibTests;

/// <summary>
/// Guards the CPU-profiler setters on every target (runtime/<goos>/cpuprof_<goos>_impl.cs, Go's plan9
/// no-interrupt shape) and the readProfile push they make reachable. Red against the converted
/// bodies. Windows: setcpuprofilerate's first call, setThreadCPUProfiler(0), goes stdcall6 -> asmcgocall
/// and throws NotImplementedException. Linux (measured 2026-09-26) and darwin: setProcessCPUProfiler's
/// setProcessCPUProfilerTimer reaches getsig -> sigaction -> rt_sigaction (linux), a PartialStubGenerator
/// throw. Either way pprof's cpu.profiling and runtime's cpuprof.on stay set and cpuprof.lock is HELD,
/// so the next StartCPUProfile in the process BLOCKS on that lock. Every pprof call is therefore
/// bounded, so the red reads as a named failure and not as a hung host. The arms run in this order
/// because that state is process-wide (MSTest runs this assembly serially).
/// </summary>
[TestClass]
public class RuntimeCPUProfilerTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    private static T Bounded<T>(string what, Func<T> call)
    {
        Task<T> task = Task.Run(call);

        if (!task.Wait(Bound))
            Assert.Fail($"{what} did not return within {Bound.TotalSeconds} s (cpuprof.lock held by an earlier throw?)");

        return task.Result;
    }

    private static void Bounded(string what, Action call) => Bounded<bool>(what, () => { call(); return true; });

    [TestMethod]
    public void CpuProfileRateRoundTripsWithoutThrowing()
    {
        SetCPUProfileRate(100);
        Assert.AreEqual(100, GoThreadProfileHz, "the thread setter records m.profilehz");

        SetCPUProfileRate(0);
        Assert.AreEqual(0, GoThreadProfileHz);
    }

    [TestMethod]
    public void StartStopThenStartAgain()
    {
        for (int round = 1; round <= 2; round++)
        {
            error err = Bounded("StartCPUProfile", () => pprof.StartCPUProfile(io.Discard));
            Assert.IsNull(err, $"round {round}: StartCPUProfile returned {err?.Error()}");
            Bounded("StopCPUProfile", pprof.StopCPUProfile);
        }
    }

    // A Go io.Writer / io.Reader over managed bytes: the profile the writer goroutine emits, and Go's own
    // parser reading it back.
    private sealed class Sink : io.Writer
    {
        public readonly System.Collections.Generic.List<byte> Bytes = new();

        public (nint n, error err) Write(slice<byte> p)
        {
            lock (Bytes)
                Bytes.AddRange(p.ToArray());

            return (len(p), null!);
        }
    }

    private sealed class Source(byte[] data) : io.Reader
    {
        private int m_offset;

        public (nint n, error err) Read(slice<byte> p)
        {
            if (m_offset >= data.Length)
                return (0, io.EOF);

            int n = Math.Min((int)len(p), data.Length - m_offset);

            for (int i = 0; i < n; i++)
                p[i] = data[m_offset + i];

            m_offset += n;
            return (n, null!);
        }
    }

    // COORD's round-trip arm: on a golib goroutine StartCPUProfile -> StopCPUProfile RETURNS within the bound.
    // StopCPUProfile returns only after `<-cpu.done`, which profileWriter sends as it exits, so a return is
    // the proof that the writer goroutine ended (its readProfile parked on the empty profBuf until the log
    // closed, then answered eof). The bytes are then a well-formed profile by Go's own parser, carrying
    // zero samples -- no sampler exists to add one. Nothing else about its content is asserted.
    [TestMethod]
    public void RoundTripOnAGoroutineEndsWithAValidProfileOfZeroSamples()
    {
        Sink sink = new();
        error? startErr = null;
        Exception? failure = null;
        using System.Threading.ManualResetEventSlim finished = new(false);

        go.golib.Goroutine.Start(() =>
        {
            try
            {
                startErr = pprof.StartCPUProfile(sink);

                if (startErr is null)
                    pprof.StopCPUProfile();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                finished.Set();
            }
        });

        Assert.IsTrue(finished.Wait(Bound), $"StartCPUProfile -> StopCPUProfile did not return within {Bound.TotalSeconds} s on a goroutine");

        if (failure is not null)
            Assert.Fail($"the round trip threw: {failure}");

        Assert.IsNull(startErr, $"StartCPUProfile returned {startErr?.Error()}");

        byte[] bytes;

        lock (sink.Bytes)
            bytes = sink.Bytes.ToArray();

        Assert.IsTrue(bytes.Length > 0, "profileWriter wrote no profile");

        (ж<go.@internal.profile_package.Profile> p, error err) = go.@internal.profile_package.Parse(new Source(bytes));
        Assert.IsNull(err, $"the profile does not parse: {err?.Error()}");
        Assert.AreEqual(0, (int)len(p.Value.Sample), "samples in a profile no sampler fed");
    }

    [TestMethod]
    public void SecondStartWithoutStopIsGosErrorNotAThrow()
    {
        error first = Bounded("StartCPUProfile", () => pprof.StartCPUProfile(io.Discard));
        Assert.IsNull(first, $"first StartCPUProfile returned {first?.Error()}");

        try
        {
            error second = Bounded("second StartCPUProfile", () => pprof.StartCPUProfile(io.Discard));
            Assert.IsNotNull(second, "a second StartCPUProfile without Stop must fail");
            Assert.AreEqual("cpu profiling already in use", second.Error().ToString());
        }
        finally
        {
            Bounded("StopCPUProfile", pprof.StopCPUProfile);
        }
    }
}
