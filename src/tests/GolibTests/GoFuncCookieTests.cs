using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using @unsafe = go.unsafe_package;

namespace GolibTests;

/// <summary>
/// The FUNC COOKIE: the word a Go func value answers when read as a word
/// (<c>uintptr(*(*unsafe.Pointer)(unsafe.Pointer(&amp;f)))</c>), which an OS carries through an lParam and
/// <c>*(*func())(unsafe.Pointer(&amp;lparam))</c> resolves back (runtime's nestedCall / callback pair;
/// ruling 2026-09-28 15:03 (2)). The band arms pin the property the syscall door rests on: a cookie is
/// NOT a managed-pointer order token, so the door passes it, and it still names no user memory, so a
/// native dereference of one is refused rather than read.
/// </summary>
[TestClass]
public class GoFuncCookieTests
{
    private delegate void OtherFuncShape();

    [TestMethod]
    public void ACookieIsStableNonZeroAndInItsOwnBand()
    {
        Action fn = () => { };
        nuint word = GoFuncCookie.Of(fn);

        Assert.AreNotEqual((nuint)0, word);
        Assert.AreEqual(word, GoFuncCookie.Of(fn), "every read of one func answers one word");
        Assert.IsTrue(GoFuncCookie.IsCookie(word));
        Assert.IsFalse(ManagedPointerTokens.IsTaggedToken(word), "the syscall door refuses order tokens; a cookie must pass it");
        Assert.IsTrue(ManagedPointerTokens.NamesNoUserMemory(word), "a native dereference of a cookie must be refused, never read");
        Assert.IsFalse(GoFuncCookie.IsCookie(unchecked((nuint)0x8000_8000_0000_1000UL)), "a caller-span token is not a cookie");
        Assert.IsFalse(GoFuncCookie.IsCookie(unchecked((nuint)0xFFFF_8000_0000_1000UL)), "a synthetic PC is not a cookie");
    }

    [TestMethod]
    public void DistinctFuncsGetDistinctCookiesAndNilIsTheZeroWord()
    {
        Action a = () => { };
        Action b = () => Console.Write("");

        Assert.AreNotEqual(GoFuncCookie.Of(a), GoFuncCookie.Of(b));
        Assert.AreEqual((nuint)0, GoFuncCookie.Of(null));
    }

    [TestMethod]
    public void UnsafePointerOfFuncCarriesTheCookieAndNilIsNil()
    {
        Action fn = () => { };

        Assert.AreEqual(GoFuncCookie.Of(fn), (nuint)(uintptr)@unsafe.Pointer.OfFunc(fn));
        Assert.IsTrue(@unsafe.Pointer.OfFunc(null) == nil);
    }

    [TestMethod]
    public void AUintptrSlotHoldingACookieReadsBackAsTheSameFunc()
    {
        int calls = 0;
        Action fn = () => calls++;
        ж<nuint> lparam = new StandardBox<nuint>(GoFuncCookie.Of(fn));

        ж<Action> asFunc = lparam.Reinterpret<nuint, Action>();

        Assert.AreSame(fn, asFunc.Value, "the cookie resolves to the very delegate it was minted for");
        asFunc.Value();
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void AGoUintptrSlotHoldingACookieReadsBackAsTheSameFunc()
    {
        // The EMISSION's slot type: runtime's callback heaps its lparam as go.uintptr, never nuint
        // (`Ꮡlparam.Reinterpret<uintptr, Action>()`). ReferenceEquals, not AreSame: a miss must fail
        // the assert, never format -- i.e. dereference -- the word read as a reference.
        int calls = 0;
        Action fn = () => calls++;
        ж<uintptr> lparam = new StandardBox<uintptr>((uintptr)GoFuncCookie.Of(fn));

        ж<Action> asFunc = lparam.Reinterpret<uintptr, Action>();

        Assert.IsTrue(ReferenceEquals(fn, asFunc.ValueSlot), "a go.uintptr slot holding a cookie resolves to its func");
        asFunc.ValueSlot();
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void AFuncReadThroughAnotherDelegateShapeIsReboundNotLost()
    {
        int calls = 0;
        Action fn = () => calls++;
        ж<nuint> lparam = new StandardBox<nuint>(GoFuncCookie.Of(fn));

        lparam.Reinterpret<nuint, OtherFuncShape>().Value();

        Assert.AreEqual(1, calls, "a named func type over the same signature reaches the same target and method");
    }

    [TestMethod]
    public void AZeroWordReadsAsGosNilFunc()
    {
        ж<nuint> lparam = new StandardBox<nuint>((nuint)0);

        // ValueSlot, the structural read the emission uses (`….ValueSlot()` in runtime's callback):
        // `.Value` is the value-peeking dereference guard, which refuses a box HOLDING nil -- and a
        // nil func is exactly what the zero word must read as. Calling it then panics, as Go's does.
        Assert.IsNull(lparam.Reinterpret<nuint, Action>().ValueSlot);
    }

    [TestMethod]
    public void ACookieWhoseFuncIsGoneIsRefusedByName()
    {
        nuint word = MintForAFuncNobodyHolds();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (GoFuncCookie.Resolve(word) is not null)
            Assert.Inconclusive("the JIT kept the func alive past its frame; the refusal arm cannot be reached on this run");

        ж<nuint> lparam = new StandardBox<nuint>(word);
        PanicException refused = Assert.ThrowsException<PanicException>(() => lparam.Reinterpret<nuint, Action>());
        StringAssert.Contains(refused.Message, "names no live func");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint MintForAFuncNobodyHolds()
    {
        object captured = new();
        Action fn = () => GC.KeepAlive(captured);
        return GoFuncCookie.Of(fn);
    }
}
