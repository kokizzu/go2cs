using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class HostEnvironmentVisibilityTests
{
    // THE INVARIANT: whether an environment variable the TEST HOST pins in-process is visible to
    // CONVERTED code is a PROPERTY OF THE FLAVOR, and this class states it per flavor rather than
    // asserting one flavor's answer everywhere.
    //
    // TestHost.Run pins TZ=UTC for determinism (and restores it in the finally) using
    // Environment.SetEnvironmentVariable alone. Converted code does not read the CLR's view: it
    // reads syscall.Getenv, and that is a per-flavor implementation.
    //
    // UNIX (syscall/linux/env_unix.cs, syscall/darwin/env_unix.cs) answers from `envs`, and that
    // slice is a SNAPSHOT taken before any test body can run. Two links, both static:
    // syscall_package's `internal static slice<@string> envs = runtime_envs()` is a static field
    // initializer, and the runtime.envs it copies is filled by runtime/goenvs_impl.cs's
    // [ModuleInitializer] — which is the faithful stand-in for the slot Go fills in schedinit,
    // "before any Go code runs". So an in-process Environment.SetEnvironmentVariable made from a
    // test body is TOO LATE BY CONSTRUCTION, and the host's TZ pin is inert for converted code on
    // this flavor. Go's own semantics agree — runtime.GOROOT documents "the environment variable,
    // if set at process start", and setenv_c only mirrors into the C environment under cgo — so
    // this is faithfulness, not a defect, and it must not be "fixed" by re-reading the live
    // environment on each Getenv.
    //
    // (The earlier note here said the unix gap was that .NET keeps only a managed copy and never
    // calls setenv(3). That is true of .NET and it is NOT the operative mechanism: goenvs_impl.cs
    // builds the snapshot FROM Environment.GetEnvironmentVariables(), i.e. from that same managed
    // copy — so even a setenv(3)-visible write would be invisible, because the snapshot is already
    // taken. The deciding fact is WHEN, not WHERE.)
    //
    // WINDOWS (syscall/windows/env_windows.cs) implements Getenv over the Win32
    // GetEnvironmentVariable, which reads the process's LIVE environment block and consults no
    // snapshot — and .NET's Environment.SetEnvironmentVariable updates exactly that block there. So
    // on that flavor the pin does reach converted code, and the roster's Windows rows have been
    // running with TZ genuinely pinned.
    //
    // THE OPERATIONAL CONSEQUENCE is the pipeline's, not this suite's: because the unix answer is
    // "invisible", TZ has to be delivered where the snapshot can still see it — in the CHILD's
    // environment, which is what testConversion.go's runCommandWithTimeoutEnv now appends for both
    // sides of the comparison. This class is the mechanical statement of the fact that makes that
    // necessary, so if either flavor's answer ever changes, the pipeline's delivery choice is
    // re-opened here rather than silently un-pinning every time-sensitive row.
    //
    // Both methods FORCE THE SNAPSHOT before touching the probe (the leading Getenv call), so what
    // they measure is the flavor and not the order MSTest happened to touch assemblies in: the
    // snapshot freezes at the first touch of syscall_package, and a test that set its probe first
    // would be measuring its own position in the run.

    private const string EnvironmentAlwaysPresent = "PATH";

    [TestMethod]
    public void HostEnvironmentPinVisibilityMatchesTheFlavor()
    {
        forceConvertedEnvironmentSnapshot();

        const string probeName = "GO2CS_HOST_ENV_VISIBILITY_PROBE";
        string? previous = Environment.GetEnvironmentVariable(probeName);

        try
        {
            // Exactly what TestHost.Run does for TZ.
            Environment.SetEnvironmentVariable(probeName, "UTC");

            // Vacuity control on the SET side: without this, "converted code cannot see it" would
            // also pass for a probe that was never set at all.
            Assert.AreEqual("UTC", Environment.GetEnvironmentVariable(probeName),
                "the host's own Environment.SetEnvironmentVariable did not take — the flavor assertion below would be vacuous");

            (@string value, bool found) = go.syscall_package.Getenv(probeName);

            if (OperatingSystem.IsWindows())
            {
                Assert.IsTrue(found,
                    "converted syscall.Getenv did not see a variable the host had just set — on the windows flavor Getenv reads the live environment block, so a host environment pin (TZ=UTC among them) has gone inert for converted code");
                Assert.AreEqual("UTC", value.ToString(),
                    "converted syscall.Getenv saw a stale value for a variable the host had just set");
            }
            else
            {
                Assert.IsFalse(found,
                    $"converted syscall.Getenv saw \"{value}\" for a variable set AFTER the environment snapshot was taken — on a snapshot flavor that cannot happen unless syscall.Getenv stopped answering from `envs` or the snapshot moved off goenvs_impl.cs's [ModuleInitializer]. Either way the pipeline's reason for delivering TZ through the child environment has changed and must be re-derived");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(probeName, previous);
        }
    }

    [TestMethod]
    public void HostEnvironmentUnsetVisibilityMatchesTheFlavor()
    {
        // The restore half of the same contract: TestHost.Run puts TZ back in its finally, and on a
        // flavor that CAN see the pin, a run that cannot un-pin leaks it into whatever runs next in
        // the same process. On a snapshot flavor there is nothing to leak, and the honest statement
        // of that is the one below — the snapshot is populated (so "invisible" is not vacuous) and
        // an in-process set-then-unset moves it in neither direction.
        forceConvertedEnvironmentSnapshot();

        const string probeName = "GO2CS_HOST_ENV_VISIBILITY_PROBE_UNSET";
        string? previous = Environment.GetEnvironmentVariable(probeName);

        try
        {
            Environment.SetEnvironmentVariable(probeName, "UTC");
            Environment.SetEnvironmentVariable(probeName, null);

            Assert.IsNull(Environment.GetEnvironmentVariable(probeName),
                "the host's own Environment.SetEnvironmentVariable(null) did not take — the flavor assertion below would be vacuous");

            (_, bool found) = go.syscall_package.Getenv(probeName);

            Assert.IsFalse(found, OperatingSystem.IsWindows()
                ? "converted syscall.Getenv still saw a variable the host had removed — the host's restore does not reach converted code on the windows flavor, so a pin leaks into whatever runs next in this process"
                : "converted syscall.Getenv saw a variable that was never in the environment snapshot — on a snapshot flavor Getenv must answer from `envs`, and this says it no longer does");
        }
        finally
        {
            Environment.SetEnvironmentVariable(probeName, previous);
        }
    }

    // Touch converted syscall.Getenv once, for two reasons. It freezes the unix snapshot HERE
    // rather than at whichever assertion happens to run first, which is what makes the flavor
    // assertions order-independent; and its answer is the vacuity control on the READ side — a
    // snapshot that came back empty would make "converted code cannot see the probe" true for the
    // wrong reason. PATH is the variable this can ask about on every flavor the corpus targets.
    private static void forceConvertedEnvironmentSnapshot()
    {
        (@string value, bool found) = go.syscall_package.Getenv(EnvironmentAlwaysPresent);

        Assert.IsTrue(found,
            $"converted syscall.Getenv could not find {EnvironmentAlwaysPresent}, which every flavor's process environment carries — the environment plumbing is broken outright, and every visibility assertion in this class would pass for that reason instead of the one it names");
        Assert.AreNotEqual(0, value.Length,
            $"converted syscall.Getenv reported {EnvironmentAlwaysPresent} as present but empty");
    }
}
