using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class HostEnvironmentVisibilityTests
{
    // THE INVARIANT: an environment variable the TEST HOST pins must be visible to CONVERTED code.
    //
    // TestHost.Run pins TZ=UTC for determinism (and restores it in the finally) using
    // Environment.SetEnvironmentVariable alone. Converted code does not read the CLR's view: it
    // reads syscall.Getenv, and that is a per-flavor implementation. On the unix flavors
    // (syscall/linux/env_unix.cs, syscall/darwin/env_unix.cs) Getenv answers from `envs`, a slice
    // built ONCE by a static field initializer — `internal static slice<@string> envs =
    // runtime_envs()` — so a variable the CLR sets afterwards is invisible there, and the pin is
    // inert. That gap was found 2026-08-28 in the os/exec arc, where the same mechanism made a
    // GO2CS_TEST_SANDBOX marker unreachable by converted children.
    //
    // THE WINDOWS FLAVOR IS DIFFERENT, which is what this guard exists to hold. syscall/windows/
    // env_windows.cs implements Getenv over the Win32 GetEnvironmentVariable, which reads the
    // process's LIVE environment block — and .NET's Environment.SetEnvironmentVariable updates
    // exactly that block on Windows (on unix it maintains only a managed copy and never calls
    // setenv(3), which is the same asymmetry from the other side). So on this flavor the pin does
    // reach converted code, and the roster's Windows rows have been running with TZ genuinely
    // pinned. This test measures that rather than assuming it: it is the only mechanical statement
    // of the invariant, and if the Windows Getenv is ever re-implemented over a snapshot the way
    // the unix ones are, this fails instead of silently un-pinning every time-sensitive row.

    [TestMethod]
    public void HostEnvironmentPinIsVisibleToConvertedCode()
    {
        const string probeName = "GO2CS_HOST_ENV_VISIBILITY_PROBE";
        string? previous = Environment.GetEnvironmentVariable(probeName);

        try
        {
            // Exactly what TestHost.Run does for TZ.
            Environment.SetEnvironmentVariable(probeName, "UTC");

            (@string value, bool found) = go.syscall_package.Getenv(probeName);

            Assert.IsTrue(found,
                "converted syscall.Getenv did not see a variable the host had just set — a host environment pin (TZ=UTC among them) is inert for converted code on this flavor");
            Assert.AreEqual("UTC", value.ToString(),
                "converted syscall.Getenv saw a stale value for a variable the host had just set");
        }
        finally
        {
            Environment.SetEnvironmentVariable(probeName, previous);
        }
    }

    [TestMethod]
    public void HostEnvironmentUnsetIsVisibleToConvertedCode()
    {
        // The restore half of the same contract: TestHost.Run puts TZ back in its finally, and a
        // run that cannot un-pin leaks its pin into whatever runs next in the same process.
        const string probeName = "GO2CS_HOST_ENV_VISIBILITY_PROBE_UNSET";
        string? previous = Environment.GetEnvironmentVariable(probeName);

        try
        {
            Environment.SetEnvironmentVariable(probeName, "UTC");
            Environment.SetEnvironmentVariable(probeName, null);

            (_, bool found) = go.syscall_package.Getenv(probeName);

            Assert.IsFalse(found,
                "converted syscall.Getenv still saw a variable the host had removed — the host's restore does not reach converted code on this flavor");
        }
        finally
        {
            Environment.SetEnvironmentVariable(probeName, previous);
        }
    }
}
