using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// IS THE TOKEN DOOR WIRED? -- the other half of TokenValueTagRefusalTests.
//
// That class measures whether the PREDICATE is right. This one measures whether the syscall
// trampoline actually CONSULTS it, which is a different question: a correct predicate nothing calls
// is the same as no door, and one arm asserting both would pass when either half is true.
//
// WHY THE DOOR SITS ON `syscalln` AND NOT ON `SyscallN`. Every public entry -- Syscall, Syscall6,
// Syscall9, Syscall12, Syscall15, Syscall18, SyscallN and Proc.Call -- funnels through that one
// private helper. Two derivations agree on it: `syscalln(` has exactly one call site in the file,
// and every native invocation in the file routes to SyscallN. So one loop covers the whole surface
// where a per-entry door would have covered one of eight.
//
// COMPILE-GATED TO THE WINDOWS FLAVOUR, in GolibTests.csproj, using the `!= '' and != 'windows'`
// form its own comment explains: $(GoTargetOS) is UNSET in this project and unset means the default
// windows corpus, so a bare `!= 'windows'` would remove this file on every ordinary run and a
// filtered `dotnet test` would answer "No test matches" with EXIT 0.
//
// EVERY ARM GATES ON THE RUNNING HOST, and the first draft of this class did not -- it reasoned that
// the refusal arms were host-independent because the door throws in managed code BEFORE the
// `delegate* unmanaged` invoke, so no native call is reached. That reasoning is true about the DOOR
// and false about the TEST: reaching `SyscallN` at all runs the windows `syscall` module's
// initializer, which throws `TypeInitializationException` on a linux host. Measured, not assumed --
// all four arms failed exactly that way on a linux container before this gate went in, and the
// csproj comment beside this file's own exclusion had already named the shape ("dies in a module
// initializer, which reads like a regression in whatever is under test rather than as NOT MEASURED").
//
// So a non-Windows host reports Inconclusive, loudly, for every arm. NOT MEASURED is the honest
// answer there; a green would be a lie about a door that nothing exercised.
[TestClass]
public class TokenDoorWiredTests
{
    [TestInitialize]
    public void RequireAWindowsHost()
    {
        // Class-wide rather than per-arm: an arm added later inherits the gate instead of having to
        // remember it, which is the difference between a rule and a habit.
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("NOT MEASURED: the token door lives in the windows syscall flavour, and " +
                                "reaching SyscallN on a non-Windows host dies in that module's initializer. " +
                                "The predicate arms in TokenValueTagRefusalTests ARE host-independent and ran.");
        }
    }

    private struct ReferenceBearing
    {
        internal string name;
        internal nint scalar;
    }

    // A plausible canonical user-mode address, used as the call target in the argument arms so the
    // `fn` check passes and the ARGUMENT check is the thing under test. It is never called: the door
    // throws first, which is the assertion.
    private static readonly nuint HonestLookingTarget = (nuint)0x0000_7FF0_0000_1000UL;

    private static nuint MintToken() =>
        new StandardBox<ReferenceBearing>(new ReferenceBearing { name = "tcp", scalar = 0x5A5A }).PointerOrderToken;

    [TestMethod]
    public void ATokenPassedAsAnArgumentIsRefusedBeforeTheNativeCall()
    {
        nuint token = MintToken();

        PanicException panic = Assert.ThrowsException<PanicException>(
            () => syscall_package.SyscallN(HonestLookingTarget, (uintptr)token),
            "a reference-bearing pointee's order token reaching the trampoline must panic, not be called");

        string message = panic.ToString();

        StringAssert.Contains(message, "managed pointer token",
            "the panic must name the class, so the next member of it is a verdict that explains itself " +
            "rather than an access violation inside a system DLL");
        StringAssert.Contains(message, "argument 0", "and it must name WHICH argument, so the caller is findable");
    }

    [TestMethod]
    public void TheArgumentIndexInTheMessageIsTheRealIndex()
    {
        // A door that always says "argument 0" would be useless on the shapes that matter: the
        // out-parameter families put the offending pointer third or fourth.
        nuint token = MintToken();

        PanicException panic = Assert.ThrowsException<PanicException>(
            () => syscall_package.SyscallN(HonestLookingTarget, (uintptr)1, (uintptr)2, (uintptr)token));

        StringAssert.Contains(panic.ToString(), "argument 2", "the index must be the offending argument's own");
    }

    [TestMethod]
    public void ATokenPassedAsTheCALLTARGETIsRefusedToo()
    {
        // The same defect one step worse: a token in `fn` would be jumped to, not merely passed.
        PanicException panic = Assert.ThrowsException<PanicException>(
            () => syscall_package.SyscallN(MintToken()));

        StringAssert.Contains(panic.ToString(), "call target", "and the message must distinguish it from an argument");
    }

    [TestMethod]
    public void HonestArgumentsReachTheNativeCall_TheDoorRefusesNothingItShouldNot()
    {
        // THE END-TO-END NEGATIVE ARM. The predicate-level negative arms live in
        // TokenValueTagRefusalTests; this one proves the DOOR, as wired into the real trampoline,
        // lets an honest call through to the kernel and back -- the failure mode that would turn
        // working corpus code into panics.

        ж<syscall_package.DLL> kernel32 = syscall_package.MustLoadDLL("kernel32.dll");
        ж<syscall_package.Proc> getCurrentProcessId = kernel32.MustFindProc("GetCurrentProcessId");

        // Zero arguments, a real target, no pointers at all -- if the door refused this, it would
        // refuse everything.
        (uintptr r1, uintptr _, syscall_package.Errno _) = syscall_package.SyscallN(getCurrentProcessId.Addr());

        Assert.AreNotEqual((uintptr)0, r1, "GetCurrentProcessId returned through the door unharmed");
        Assert.AreEqual((uintptr)Environment.ProcessId, r1, "and returned THIS process's id, so the call really happened");
    }
}
