using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

// The GOROUTINE PROFILE's registry surface -- Goroutine.ProfileSnapshot(), which
// runtime/pprof's pprof_goroutineProfileWithLabels reports.
//
// WHAT A GOROUTINE PROFILE NEEDS, AND WHY EACH PIECE CAN BREAK SILENTLY
//   Go's printCountProfile groups samples by (stack, labels), so a profile that cannot tell two
//   goroutines' start functions apart, or cannot read a goroutine's labels from the thread taking
//   the profile, reports a plausible and wrong answer rather than failing. Three mechanisms carry
//   that and all three are invisible from the outside:
//
//     1. THE START FUNCTION (Go's gp.startpc). `builtin.goǃ`'s arity rungs hand Goroutine.Start a
//        CLOSURE, so the closure's own Method names the launcher's lambda, not the Go function the
//        goroutine runs. The rungs pass the user's delegate alongside it. Drop that argument and
//        every goroutine reports one indistinguishable stack -- which still profiles, still parses,
//        and silently collapses every count.
//     2. THE LABEL MIRROR. Labels live in an AsyncLocal, because Go INHERITS them at goroutine
//        creation and ExecutionContext capture at thread start is that rule exactly -- but an
//        AsyncLocal is readable only from the thread whose context holds it. Goroutine.Enter seeds
//        a per-goroutine mirror from the flowed value so a profile can read it. Drop the seeding
//        and an INHERITED label (pprof.Do's entire purpose) reads as no label at all, while a label
//        the goroutine sets on itself still works -- so the half that breaks is the half most tests
//        do not exercise.
//     3. THE POPULATION. Go's profile reports gcount() -- user goroutines -- plus the finalizer
//        goroutine while it runs a finalizer body (isSystemGoroutine answers false for runfinq
//        exactly while fingStatus&fingRunningFinalizer is set). Both halves are wrong in a way that
//        reads as a small count difference: without the filter the runtime's own goroutines appear,
//        without the window a parked finalizer disappears.
//
// SCOPE, stated because it is narrower than the whole contract. The (n, ok) SIZING protocol --
// fetch(nil) answers (n, false), a large-enough slice fills and answers (n, true), a too-small one
// writes nothing -- lives in runtime/pprof, an assembly this project does not reference and whose
// members are internal to it. Adding a reference (or an InternalsVisibleTo on a converted package)
// to reach twenty lines of contract translation would cost more than it guards. That protocol is
// covered where it is actually exercised: writeRuntimeProfile's two-call loop IS the sizing test,
// and runtime/pprof's TestGoroutineCounts runs it.
//
// POSITIVE CONTROLS, measured at the cut (2026-09-04) rather than asserted -- each neuters exactly
// one mechanism, the tree is restored byte-identically after each, and the restored tree re-runs
// 3/3 green:
//   * the launcher rungs' entry argument removed  -> 2 RED:
//       TheProfileDistinguishesStartFunctions      ("Expected:<3>. Actual:<0>. expected 3 goroutines
//                                                    started at ParkA")
//       AnInheritedLabelIsReadableFromAnotherThread ("Expected:<2>. Actual:<0>")
//   * Enter's label seeding removed               -> 1 RED:
//       AnInheritedLabelIsReadableFromAnotherThread ("an inherited label must reach the child's entry")
//   * CountsAsUser reduced to !IsSystem           -> 1 RED:
//       TheProfileReportsGcountsPopulation         ("Expected:<1>. Actual:<0>. a system goroutine
//                                                    doing user work must be in the profile")
//
// The first control reddens TWO tests, and that is a stated dependency rather than a coverage
// overlap: the label test IDENTIFIES its children by their start function, so it cannot run at all
// without mechanism 1. Isolation still holds where it has to -- the label mechanism is reddened
// ALONE by the second control and the population mechanism ALONE by the third, so neither is
// standing in for the other's coverage.
//
// (An earlier draft of this comment claimed one control reddens one test. It was written before the
// controls were run; the measurement above is what replaced it. The controls' own first run
// produced no verdict line at all -- a POSIX path reached MSBuild as an unknown switch -- which an
// ordinary grep reports as "no failures", so the runner now aborts an arm that yields no verdict.)
[TestClass]
public class GoroutineProfileTests
{
    // Distinct start functions, one per test so that a parallel sibling cannot move a count.
    // NoInlining for the reason GoroutineCreatorTests gives: this frame IS the identity asserted on.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ParkA(channel<int> c) => c.Receive();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ParkB(channel<int> c) => c.Receive();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ParkLabelled(channel<int> c) => c.Receive();

    // A field rather than a captured local, so the two system bodies below can be METHOD GROUPS:
    // StartForGuard records the delegate's own Method as the start function, and a closure would
    // record the lambda instead of the function this test asserts on. `channel<T>` is a struct, so
    // the default value is Go's nil channel and the test assigns a real one before starting either.
    private static channel<int> s_systemGate;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SystemParkPlain() => s_systemGate.Receive();

    // The finalizer goroutine's shape: a system goroutine running the user's code, which is what
    // Go's fingRunningFinalizer window means (mfinal.cs opens exactly this around a finalizer body).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SystemParkDoingUserWork()
    {
        using (Goroutine.EnterUserWork())
            s_systemGate.Receive();
    }

    private static MethodBase MethodOf(string name) =>
        typeof(GoroutineProfileTests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    // A real function of the converted runtime package -- what makes a goroutine SYSTEM. The
    // predicate reads the package, never the name (SystemGoroutineTests pins that separately).
    private static MethodBase RuntimeMethod() =>
        typeof(runtime_package).GetMethods(BindingFlags.Public | BindingFlags.Static).First(m => !m.IsGenericMethodDefinition);

    private static int CountOf(GoroutineProfileEntry[] profile, MethodBase function) =>
        profile.Count(e => e.Function is not null && e.Function.MethodHandle == function.MethodHandle);

    private static GoroutineProfileEntry[] AwaitProfile(Func<GoroutineProfileEntry[], bool> ready)
    {
        GoroutineProfileEntry[] profile = Goroutine.ProfileSnapshot();

        for (int i = 0; i < 600 && !ready(profile); i++)
        {
            System.Threading.Thread.Sleep(5);
            profile = Goroutine.ProfileSnapshot();
        }

        return profile;
    }

    // MECHANISM 1. Two goroutines started at different functions are two different entries, and the
    // synthetic PCs the profile mints from them differ -- which is what lets printCountProfile put
    // them in different groups. The calling goroutine, started by no `go` statement, reports NO
    // start function rather than borrowing one.
    [TestMethod]
    public void TheProfileDistinguishesStartFunctions()
    {
        channel<int> c = new(0);

        try
        {
            using (Goroutine.Enter())
            {
                MethodBase parkA = MethodOf(nameof(ParkA));
                MethodBase parkB = MethodOf(nameof(ParkB));

                for (int i = 0; i < 3; i++)
                    builtin.goǃ(ParkA, c);

                for (int i = 0; i < 2; i++)
                    builtin.goǃ(ParkB, c);

                GoroutineProfileEntry[] profile = AwaitProfile(p => CountOf(p, parkA) == 3 && CountOf(p, parkB) == 2);

                Assert.AreEqual(3, CountOf(profile, parkA), "expected 3 goroutines started at ParkA");
                Assert.AreEqual(2, CountOf(profile, parkB), "expected 2 goroutines started at ParkB");

                Assert.IsTrue(profile.Any(e => e.Function is null),
                    "the calling goroutine was entered directly, so it must report NO start function -- an empty stack, not a borrowed frame");

                // The bridge to what runtime/pprof does with these: one synthetic PC per function,
                // distinct, and resolvable to the two distinct Go names a profile prints.
                nuint pcA = GoSyntheticPC.Of(parkA);
                nuint pcB = GoSyntheticPC.Of(parkB);

                Assert.AreNotEqual(pcA, pcB, "two start functions must mint two synthetic PCs, or every group collapses into one");
                Assert.AreNotEqual(GoSyntheticPC.NameOf(pcA), GoSyntheticPC.NameOf(pcB), "the two PCs must resolve to different Go names");
            }
        }
        finally
        {
            c.Close();
        }
    }

    // MECHANISM 2. A label set BEFORE the `go` statement reaches the child's registry entry, and is
    // readable from this thread -- which is the whole reason the mirror exists, since the child's
    // AsyncLocal is unreadable from here. The setter's own goroutine carries it too, and the
    // read-back on this thread still answers what it was given.
    [TestMethod]
    public void AnInheritedLabelIsReadableFromAnotherThread()
    {
        channel<int> c = new(0);
        object marker = new();

        try
        {
            using (Goroutine.Enter())
            {
                try
                {
                    MethodBase parked = MethodOf(nameof(ParkLabelled));

                    Goroutine.SetProfileLabels(marker);

                    Assert.AreSame(marker, Goroutine.GetProfileLabels(), "the read-back on the setting thread must answer what was set");

                    for (int i = 0; i < 2; i++)
                        builtin.goǃ(ParkLabelled, c);

                    GoroutineProfileEntry[] profile = AwaitProfile(p => CountOf(p, parked) == 2);
                    GoroutineProfileEntry[] children = [.. profile.Where(e => e.Function is not null && e.Function.MethodHandle == parked.MethodHandle)];

                    Assert.AreEqual(2, children.Length, "expected 2 goroutines started at ParkLabelled");
                    Assert.IsTrue(children.All(e => ReferenceEquals(e.Labels, marker)),
                        "an inherited label must reach the child's entry -- Go's `newg.labels = mp.curg.labels`, read from a thread that is not the child");

                    Assert.IsTrue(profile.Any(e => e.Function is null && ReferenceEquals(e.Labels, marker)),
                        "the goroutine that SET the label must carry it on its own entry");
                }
                finally
                {
                    // AsyncLocal outlives this scope on a reused host thread, so the label is
                    // cleared the way pprof.Do clears it rather than left to leak into a sibling.
                    Goroutine.SetProfileLabels(null);
                }
            }
        }
        finally
        {
            c.Close();
        }
    }

    // MECHANISM 3. The population is Go's gcount(): system goroutines are absent, EXCEPT while one
    // is running user code -- Go's finalizer-goroutine special case, which is what puts a parked
    // finalizer into runtime/pprof's TestGoroutineCounts. The registry itself still holds both, so
    // the filter is the profile's and not a loss of bookkeeping.
    [TestMethod]
    public void TheProfileReportsGcountsPopulation()
    {
        s_systemGate = new channel<int>(0);

        try
        {
            MethodBase plain = MethodOf(nameof(SystemParkPlain));
            MethodBase userWork = MethodOf(nameof(SystemParkDoingUserWork));

            Goroutine.StartForGuard(SystemParkPlain, RuntimeMethod());
            Goroutine.StartForGuard(SystemParkDoingUserWork, RuntimeMethod());

            // Wait on the REGISTRY, not on the profile: waiting on the profile for the goroutine
            // that must be ABSENT from it would wait forever, and asserting before it has started
            // would pass vacuously.
            bool RegistryHasBoth() =>
                Goroutine.Snapshot().Count(g => g.Entry is not null &&
                    (g.Entry.MethodHandle == plain.MethodHandle || g.Entry.MethodHandle == userWork.MethodHandle)) == 2;

            for (int i = 0; i < 600 && !RegistryHasBoth(); i++)
                System.Threading.Thread.Sleep(5);

            Assert.IsTrue(RegistryHasBoth(), "both system goroutines must be registered before the profile is read, or the assertions below are vacuous");

            GoroutineProfileEntry[] profile = AwaitProfile(p => CountOf(p, userWork) == 1);

            Assert.AreEqual(1, CountOf(profile, userWork),
                "a system goroutine doing user work must be in the profile -- Go's fing while it runs a finalizer");
            Assert.AreEqual(0, CountOf(profile, plain),
                "a system goroutine is otherwise absent -- Go's profile reports gcount(), which subtracts them");

            Assert.IsTrue(Goroutine.Snapshot().Any(g => g.Entry is not null && g.Entry.MethodHandle == plain.MethodHandle && g.IsSystem),
                "the registry must still hold the filtered goroutine -- the profile narrows a population, it does not lose one");
        }
        finally
        {
            s_systemGate.Close();
        }
    }
}
