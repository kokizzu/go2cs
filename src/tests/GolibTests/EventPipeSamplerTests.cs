using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.CpuProfiler;

namespace GolibTests;

/// <summary>
/// Guards the opt-in CPU sampler's session lifetime (go2cs.CpuProfiler/EventPipeSampler.cs, section 11
/// of DESIGN-managed-profiling.md): Start opens a SampleProfiler session on this process and Stop
/// drains its trace; a session that cannot open (DOTNET_EnableDiagnostics=0 is that class) leaves no
/// samples and throws nothing. The sampler is driven directly here, not through runtime's seam, so
/// RuntimeCpuSamplerSeamTests' proxy stays the registered sampler. The frames piece: Stop writes the
/// sampled stacks of Go functions as synthetic PCs, leaf first, thinned per thread to one sample per
/// 1/hz of elapsed time.
/// </summary>
[TestClass]
public class EventPipeSamplerTests
{
    private static int s_writes;

    private static void CountingWriter(int64 nanotime, slice<uintptr> stack, unsafe_package.Pointer tag) => s_writes++;

    [TestMethod]
    public void ASessionOpensOnThisProcessAndDrainsATrace()
    {
        var sampler = new EventPipeSampler(EventPipeSampler.OpenInProcessSession);

        sampler.Start(100);
        Assert.IsTrue(sampler.LastSessionOpened, "a SampleProfiler session must open on this process");

        // Burn a little CPU so the session has samples to carry.
        var clock = Stopwatch.StartNew();
        ulong spin = 1;

        while (clock.ElapsedMilliseconds < 200)
            spin = spin * 6364136223846793005UL + 1442695040888963407UL;

        GC.KeepAlive(spin);
        sampler.Stop(CountingWriter);

        Assert.IsTrue(sampler.LastTraceBytes > 0, "stopping the session must drain a non-empty trace");
    }

    [TestMethod]
    public void ASessionThatCannotOpenLeavesNoSamplesAndNoThrow()
    {
        var sampler = new EventPipeSampler(() => throw new ServerNotAvailableProbeException());
        s_writes = 0;

        sampler.Start(100);
        sampler.Stop(CountingWriter);

        Assert.IsFalse(sampler.LastSessionOpened);
        Assert.AreEqual(0L, sampler.LastTraceBytes);
        Assert.AreEqual(0, s_writes, "a sampler with no session writes no sample");
    }

    [TestMethod]
    public void AGoHogIsSampledAsItsSyntheticPCAndThinnedToTheRate()
    {
        const int hz = 100;
        const int hogMilliseconds = 1000;
        const int threads = 2;

        var sampler = new EventPipeSampler(EventPipeSampler.OpenInProcessSession);
        var stacks = new List<uintptr[]>();

        void Collect(int64 nanotime, slice<uintptr> stack, unsafe_package.Pointer tag)
        {
            var copy = new uintptr[(int)stack.Length];

            for (int i = 0; i < copy.Length; i++)
                copy[i] = stack[i];

            stacks.Add(copy);
        }

        sampler.Start(hz);
        Assert.IsTrue(sampler.LastSessionOpened, "a SampleProfiler session must open on this process");

        var hogs = new Thread[threads];

        for (int i = 0; i < threads; i++)
        {
            hogs[i] = new Thread(() => cpusamplerprobe_package.cpuHogger(hogMilliseconds));
            hogs[i].Start();
        }

        foreach (Thread hog in hogs)
            hog.Join();

        sampler.Stop(Collect);

        uintptr hogPC = GoSyntheticPC.Of(typeof(cpusamplerprobe_package).GetMethod(nameof(cpusamplerprobe_package.cpuHog1))!);
        uintptr hoggerPC = GoSyntheticPC.Of(typeof(cpusamplerprobe_package).GetMethod(nameof(cpusamplerprobe_package.cpuHogger))!);
        int leafHog = 0;
        int withHogger = 0;

        foreach (uintptr[] stack in stacks)
        {
            Assert.IsTrue(stack.Length > 0, "a written sample carries at least one Go frame");

            if (stack[0] == hogPC)
                leafHog++;

            if (Array.IndexOf(stack, hoggerPC) > 0)
                withHogger++;
        }

        // The hog threads run for 1 s each at 100 Hz: about 200 samples between them once thinned.
        // The unthinned SampleProfiler would give about 2,000.
        int ceiling = threads * hogMilliseconds * hz / 1000 * 3 / 2;

        Console.WriteLine($"managed samples {sampler.LastManagedSamples}, written {stacks.Count}, hog as leaf {leafHog}, with its caller {withHogger}");

        Assert.IsTrue(leafHog >= 20, $"expected samples with the hog as the leaf frame; {leafHog} of {stacks.Count}");
        Assert.IsTrue(withHogger >= leafHog / 2, $"the hog's Go caller is on the sampled stack; {withHogger} of {leafHog}");
        Assert.IsTrue(stacks.Count <= ceiling, $"samples are thinned to {hz} Hz per thread; {stacks.Count} > {ceiling}");
        Assert.AreEqual("cpusamplerprobe.cpuHog1", GoSyntheticPC.NameOf(hogPC));
    }

    [TestMethod]
    public void ASampleCarriesTheLabelsItsThreadHadWhenSampled()
    {
        const int hz = 100;
        const int hogMilliseconds = 1000;

        var sampler = new EventPipeSampler(EventPipeSampler.OpenInProcessSession);
        var samples = new List<(uintptr[] stack, unsafe_package.Pointer tag)>();
        unsafe_package.Pointer label = unsafe_package.Pointer.FromPinnedBox(Ꮡ(42L));

        void Collect(int64 nanotime, slice<uintptr> stack, unsafe_package.Pointer tag)
        {
            var copy = new uintptr[(int)stack.Length];

            for (int i = 0; i < copy.Length; i++)
                copy[i] = stack[i];

            samples.Add((copy, tag));
        }

        sampler.Start(hz);
        Assert.IsTrue(sampler.LastSessionOpened, "a SampleProfiler session must open on this process");

        // Go's pprof.Do: the labelled hog sets its labels (runtime_setProfLabel is Goroutine.SetProfileLabels)
        // after profiling started, and clears them when it is done; the other hog never sets any.
        var labelled = new Thread(() =>
        {
            go.golib.Goroutine.SetProfileLabels(label);
            cpusamplerprobe_package.cpuHogger(hogMilliseconds);
            go.golib.Goroutine.SetProfileLabels(null);
        });
        var unlabelled = new Thread(() => cpusamplerprobe_package.cpuHogger2(hogMilliseconds));

        labelled.Start();
        unlabelled.Start();
        labelled.Join();
        unlabelled.Join();

        sampler.Stop(Collect);

        uintptr hoggerPC = GoSyntheticPC.Of(typeof(cpusamplerprobe_package).GetMethod(nameof(cpusamplerprobe_package.cpuHogger))!);
        uintptr hogger2PC = GoSyntheticPC.Of(typeof(cpusamplerprobe_package).GetMethod(nameof(cpusamplerprobe_package.cpuHogger2))!);
        int labelledSamples = 0, labelledTagged = 0, unlabelledSamples = 0, unlabelledTagged = 0;

        foreach ((uintptr[] stack, unsafe_package.Pointer tag) in samples)
        {
            bool tagged = tag is not null && tag != nil;

            if (Array.IndexOf(stack, hoggerPC) >= 0)
            {
                labelledSamples++;

                if (tagged && ReferenceEquals(tag, label))
                    labelledTagged++;
            }
            else if (Array.IndexOf(stack, hogger2PC) >= 0)
            {
                unlabelledSamples++;

                if (tagged)
                    unlabelledTagged++;
            }
        }

        Console.WriteLine($"labelled hog {labelledTagged} of {labelledSamples} tagged; unlabelled hog {unlabelledTagged} of {unlabelledSamples} tagged");

        Assert.IsTrue(labelledSamples >= 20 && unlabelledSamples >= 20, $"both hogs are sampled; {labelledSamples} and {unlabelledSamples}");
        Assert.AreEqual(labelledSamples, labelledTagged, "every sample of the labelled hog carries its labels");
        Assert.AreEqual(0, unlabelledTagged, "no sample of the unlabelled hog carries labels");
    }

    private sealed class ServerNotAvailableProbeException() : Exception("diagnostic port not available (probe)");
}
