using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

// A Go binary is statically linked: every package it uses is present from its first instruction. The
// CLR binds an assembly LAZILY, on the first JIT that needs it, under whatever TOKEN the binding thread
// holds -- and a thread impersonating an ANONYMOUS token cannot open an image file. os/user's
// TestImpersonatedSelf/0 is the measured case (FileNotFoundException for internal.itoa, first touched
// on the failure path Go expects, with the DLL on disk). The test host now preloads the managed
// reference closure at start (TestHost.PreloadManagedReferenceClosure, COORD ruling (a)).
//
// Two DIFFERENT not-yet-loaded members of the host's closure, deliberately: A proves the premise in
// this very process (bound first under an anonymous token, it fails), and B -- after the preload --
// loads under the same token. One member for both arms could read a cached bind failure as the fix
// failing, or a cached success as the fix working.
[TestClass]
public class ImpersonatedAssemblyLoadTests
{
    private const int SecurityAnonymous = 0;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ImpersonateSelf(int impersonationLevel);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool RevertToSelf();

    private static Exception? UnderAnonymousToken(Action action)
    {
        Exception? caught = null;
        Thread thread = new(() =>
        {
            if (!ImpersonateSelf(SecurityAnonymous))
                throw new InvalidOperationException($"ImpersonateSelf(SecurityAnonymous) failed: {Marshal.GetLastWin32Error()}");
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                RevertToSelf();
            }
        });
        thread.Start();
        thread.Join();
        return caught;
    }

    // The host's TRANSITIVE closure, walked from the DLL FILES by metadata alone -- reading an image's
    // assembly-reference table binds nothing, so the walk cannot itself load a candidate. The DIRECT
    // references alone were measured insufficient: in a full GolibTests run earlier tests have already
    // bound them, and the arm went NotExecuted (a vacuous skip in exactly the gate that matters). Only
    // images shipped beside the tests are candidates; framework references resolve elsewhere and are
    // not walked.
    private static AssemblyName[] UnloadedClosureMembers(Assembly root)
    {
        HashSet<string> loaded = new(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name!), StringComparer.OrdinalIgnoreCase);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<AssemblyName> unloaded = [];
        Stack<string> pending = new();
        pending.Push(root.Location);

        while (pending.Count > 0)
        {
            using FileStream stream = File.OpenRead(pending.Pop());
            using PEReader pe = new(stream);

            if (!pe.HasMetadata)
                continue;

            MetadataReader metadata = pe.GetMetadataReader();

            foreach (AssemblyReferenceHandle handle in metadata.AssemblyReferences)
            {
                string name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                string image = Path.Combine(AppContext.BaseDirectory, name + ".dll");

                if (!seen.Add(name) || !File.Exists(image))
                    continue;

                if (!loaded.Contains(name))
                    unloaded.Add(new AssemblyName(name));

                pending.Push(image);
            }
        }

        return [.. unloaded];
    }

    [TestMethod]
    public void AnAssemblyFirstBoundUnderAnAnonymousTokenLoadsOnceTheClosureIsPreloaded()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("anonymous-token impersonation is a Windows capability");

        Assembly root = typeof(TestHost).Assembly;
        AssemblyName[] unloaded = UnloadedClosureMembers(root);

        if (unloaded.Length < 2)
            Assert.Inconclusive($"the premise needs two unloaded closure members, found {unloaded.Length} (a prior test bound them)");

        AssemblyName premise = unloaded[0], subject = unloaded[1];

        // RED, in this process: the premise member bound for the FIRST time under an anonymous token.
        Exception? before = UnderAnonymousToken(() => Assembly.Load(premise));
        Assert.IsTrue(before is FileNotFoundException or FileLoadException,
            $"premise: a first bind of {premise.Name} under an anonymous token must fail, got {before?.GetType().Name ?? "success"}");

        // GREEN: the host's preload binds the closure on THIS (un-impersonated) thread ...
        int loaded = TestHost.PreloadManagedReferenceClosure(root);
        Assert.IsTrue(loaded > 0, "the preload must bind the closure");

        // ... after which the subject is already bound, and the same token no longer matters.
        Exception? after = UnderAnonymousToken(() => Assembly.Load(subject));
        Assert.IsNull(after, $"after the preload, {subject.Name} must load under an anonymous token, got {after?.GetType().Name}: {after?.Message}");
    }
}
