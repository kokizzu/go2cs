// ErrorShellCarrierEqualityTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using go;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go.builtin;

namespace GolibTests;

// A named-uint8 error stand-in for crypto/tls's `type AlertError uint8` — the equality operator
// is what AreEqual invokes once both carriers unwrap. Its Error method is a Go-style extension
// below, which is how the shell's delegate binder finds a converted method.
internal readonly struct testAlert(byte value) : IEquatable<testAlert>
{
    internal readonly byte m_value = value;

    public bool Equals(testAlert other) => m_value == other.m_value;

    public override bool Equals(object? obj) => obj is testAlert other && Equals(other);

    public override int GetHashCode() => m_value.GetHashCode();

    public static bool operator ==(testAlert left, testAlert right) => left.Equals(right);

    public static bool operator !=(testAlert left, testAlert right) => !left.Equals(right);
}

internal sealed class refErrTarget;

internal static class ErrorShellCarrierEqualityTestExtensions
{
    internal static @string Error(this testAlert alert) => "alert(" + alert.m_value + ")";

    internal static @string Error(this ж<refErrTarget> _) => "ref";
}

/// <summary>
/// Go interface equality is decided on the dynamic (type, value) an interface holds, never on
/// which CARRIER class holds it — and <see cref="error{T}"/> is a carrier: golib's own generic
/// shell for <c>error</c>, minted independently wherever a value with no nominal adapter crosses
/// into error space (fmt's <c>%w</c> assert is one such minter, a test assembly's cast another).
/// </summary>
/// <remarks>
/// Every go2cs-gen generic shell joins the <see cref="IInterfaceAdapter"/> unwrap protocol so
/// <c>AreEqual</c> can reach the value it carries; <see cref="error{T}"/> was the one shell that
/// had not, so two shells over the same value — or a shell meeting a generated value adapter —
/// fell to reference equality and compared UNEQUAL. crypto/tls's TestQUICHandshakeError is the
/// measured consumer: <c>quicError</c> wraps <c>AlertError(a)</c> through fmt's <c>%w</c> (a
/// shell), the test's <c>errors.Is</c> target arrives as a test-assembly value adapter, and the
/// same alert value never matched itself. Pointer-backed shells must keep Go's pointer-identity
/// rule instead: the carrier exposes the ж box, not the pointee.
/// </remarks>
[TestClass]
public class ErrorShellCarrierEqualityTests
{
    [TestMethod]
    public void TwoShellsOverTheSameValueCompareEqual()
    {
        error left = error.As(new testAlert(42));
        error right = error.As(new testAlert(42));

        Assert.IsTrue(AreEqual(left, right), "two independently minted shells of one Go (type, value) are the same interface value");
        Assert.IsFalse(AreEqual(left, error.As(new testAlert(43))), "different values stay unequal");
    }

    [TestMethod]
    public void ShellExposesItsValueThroughTheInterfaceAdapterProtocol()
    {
        // The protocol membership itself, stated directly: this is what lets EVERY consumer of
        // the unwrap rule (AreEqual, the assert machinery, reflection's dynamic-type derivation)
        // reach the Go value inside the shell, exactly as they already do for generated shells.
        error shell = error.As(new testAlert(7));

        Assert.IsInstanceOfType(shell, typeof(IInterfaceAdapter), "error<T> is a carrier and must join the carrier protocol");
        Assert.IsInstanceOfType(((IInterfaceAdapter)shell).Value, typeof(testAlert), "a value-backed shell carries the value itself");
    }

    [TestMethod]
    public void PointerBackedShellsCompareByBoxIdentity()
    {
        ж<refErrTarget> box = new StandardBox<refErrTarget>(new refErrTarget());

        error viaSameBox1 = error.As(box);
        error viaSameBox2 = error.As(box);
        error viaOtherBox = error.As(new StandardBox<refErrTarget>(new refErrTarget()));

        Assert.IsTrue(AreEqual(viaSameBox1, viaSameBox2), "pointer-backed shells over ONE box are Go-equal pointers");
        Assert.IsFalse(AreEqual(viaSameBox1, viaOtherBox), "distinct boxes are distinct Go pointers, whatever their pointees hold");
        Assert.AreSame(box, ((IInterfaceAdapter)viaSameBox1).Value, "a pointer-backed shell carries the ж box, not the pointee");
    }
}
