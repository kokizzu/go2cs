using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

/// <summary>
/// <c>runtime.SetFinalizer</c>'s ARGUMENT RULE — the predicate, asserted directly.
/// </summary>
/// <remarks>
/// <para>
/// WHAT THESE MEASURE, AND WHY THEY ARE NOT IN <c>FinalizerDispatchTests</c>. Those arms drive real
/// collection and finalization, so they measure LIVENESS and dispatch together. These measure the
/// DECISION alone: <see cref="GoReflect.TryBindFinalizerArgument"/> is a pure function of a referent
/// and a delegate, so every arm here is deterministic and needs no GC, no dedicated thread and no
/// timing.
/// </para>
/// <para>
/// THE DEFECT THEY GUARD. A Go DEFINED POINTER TYPE (<c>type Tintptr *int</c>) is emitted as a
/// WRAPPER over <c>ж&lt;nint&gt;</c> with a user-defined implicit operator pair — measured at
/// go2cs-gen's own <c>InheritedTypeTemplate.cs:329-331</c> — and NOT as a subclass.
/// <c>Delegate.DynamicInvoke</c> binds through the default binder, which performs identity,
/// reference, boxing and primitive-widening conversions and never invokes a user-defined operator.
/// So <c>SetFinalizer(Tintptr(x), func(v *int){...})</c> — which Go ACCEPTS, both being pointers with
/// one unnamed and the same element type — could not be called at all, and the finalizer runner's
/// bare catch swallowed the failure: dequeued, counted, idle, never run, no error surface anywhere.
/// </para>
/// <para>
/// ⚠ <b>ARM 1 IS THE ROW.</b> It is Go's <c>TestFinalizerType</c> shape 3 (iteration index 2), which
/// an iteration-index probe measured as the first shape that never delivers — the one that consumes
/// runtime's whole package deadline for zero converted verdicts (i9, 2026-09-08).
/// </para>
/// <para>
/// ⚠ <b>WHAT IS NOT GUARDED HERE, stated rather than implied.</b> The interface branch has two
/// halves: an interface the referent's type DIRECTLY implements (arm 4, a reference conversion) and a
/// Go interface it satisfies STRUCTURALLY, which binds through <c>AdapterBinder</c>'s duck-typing
/// shell. Only the first is asserted. A trustworthy fixture for the second needs a converted Go
/// method set — extension methods in the receiver's own assembly — and this project has no generator
/// to mint one; hand-writing an approximation would guard the approximation. That half is exercised
/// by the corpus row (Go's shape 5) and is named as owed rather than faked.
/// </para>
/// </remarks>
[TestClass]
public class FinalizerBindingTests
{
    // A hand-written stand-in for what go2cs-gen emits for `type Tintptr *int`. GolibTests does NOT
    // reference the analyzer -- the [GoType] attributes elsewhere in this project are inert metadata --
    // so the generated wrapper cannot be obtained here and its SHAPE is reproduced instead.
    //
    // ⚠ The two members below are not decoration: they are exactly the key GoReflect.TryUnwrapWrapperValue
    // reads -- a [GoType] marker with a non-empty, non-"dyn" definition, and a private instance field
    // named m_value -- and the single-argument constructor is what TryConvertTo uses in the other
    // direction. Arm 0 asserts that this stand-in really does present that shape, so a future change to
    // the wrapper contract fails HERE, loudly, instead of turning the arms below into a test of a fossil.
    [GoType("ж<nint>")]
    private sealed class NamedIntPointer
    {
        private readonly ж<nint> m_value;

        public NamedIntPointer(ж<nint> value) => m_value = value;

        public ж<nint> Value => m_value;

        public static implicit operator NamedIntPointer(ж<nint> value) => new(value);

        public static implicit operator ж<nint>(NamedIntPointer value) => value.m_value;
    }

    private static ж<nint> NewIntBox(nint value) => new StandardBox<nint>(value);

    [TestMethod]
    public void Arm0_TheStandInPresentsTheGeneratedWrapperShape()
    {
        Type t = typeof(NamedIntPointer);

        Assert.IsTrue(t.GetCustomAttributes(typeof(GoTypeAttribute), false).Length > 0,
            "ARM 0: the stand-in lost its [GoType] marker, which is one of the two things the unwrap " +
            "keys on. Every arm below is now measuring a type the converter would never produce.");
        Assert.IsNotNull(t.GetField("m_value", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
            "ARM 0: the stand-in lost its private m_value field — the other half of the unwrap's key.");
        Assert.IsFalse(typeof(ж<nint>).IsAssignableFrom(t),
            "ARM 0: the stand-in DERIVES from the box, so a reference conversion exists and the arms " +
            "below would pass for a reason the real generated wrapper does not share. The generated " +
            "wrapper HOLDS its box (InheritedTypeTemplate) and never inherits it — that is the whole " +
            "reason DynamicInvoke could not bind.");
    }

    [TestMethod]
    public void Arm1_ANamedPointerBindsToItsUNNAMEDUnderlying()
    {
        NamedIntPointer referent = new(NewIntBox(-2));
        Action<ж<nint>> finalizer = _ => { };

        bool bound = GoReflect.TryBindFinalizerArgument(referent, finalizer, out object? argument, out string? rejection);

        Assert.IsTrue(bound,
            "ARM 1 IS THE ROW: Go's TestFinalizerType shape 3, a defined pointer type with a finalizer " +
            "taking the UNDERLYING pointer. Go accepts it — both pointers, one unnamed, same element — " +
            $"and the binding was refused with: {rejection}");
        Assert.IsInstanceOfType(argument, typeof(ж<nint>),
            "ARM 1: the pair bound but the argument was not converted to the parameter's type, so " +
            "DynamicInvoke will still refuse it. Binding and CONVERTING are one step or neither.");
    }

    [TestMethod]
    public void Arm2_IdenticalTypesBind()
    {
        ж<nint> referent = NewIntBox(-1);
        Action<ж<nint>> finalizer = _ => { };

        Assert.IsTrue(GoReflect.TryBindFinalizerArgument(referent, finalizer, out object? argument, out _),
            "ARM 2: the same-type case. This one always bound, including before the predicate existed — " +
            "it is here so a regression in the new path cannot hide behind arm 1.");
        Assert.AreSame(referent, argument,
            "ARM 2: an identical pair must pass the referent THROUGH, not a copy or a conversion of it.");
    }

    [TestMethod]
    public void Arm3_GosEmptyInterfaceBinds()
    {
        ж<nint> referent = NewIntBox(7);
        Action<object> finalizer = _ => { };

        Assert.IsTrue(GoReflect.TryBindFinalizerArgument(referent, finalizer, out object? argument, out _),
            "ARM 3: Go's `any` parameter, emitted as `object`. Go accepts any object for an empty " +
            "interface, and this is the shape FinalizerDispatchTests arm 6 drives end to end.");
        Assert.AreSame(referent, argument, "ARM 3: `any` must pass the referent through unchanged.");
    }

    private interface IDirectlyImplemented { }

    private sealed class DirectImplementer : IDirectlyImplemented { }

    [TestMethod]
    public void Arm4_AnInterfaceTheReferentImplementsBinds()
    {
        DirectImplementer referent = new();
        Action<IDirectlyImplemented> finalizer = _ => { };

        Assert.IsTrue(GoReflect.TryBindFinalizerArgument(referent, finalizer, out object? argument, out _),
            "ARM 4: an interface the referent's own type implements — a reference conversion, which the " +
            "default binder always handled. Asserted so the interface branch cannot regress silently.");
        Assert.AreSame(referent, argument, "ARM 4: a directly-implemented interface passes the referent through.");
    }

    [TestMethod]
    public void Arm5_APairingGoRejectsIsRefusedWithGosOwnText()
    {
        ж<nint> referent = NewIntBox(0);
        Action<@string> finalizer = _ => { };

        bool bound = GoReflect.TryBindFinalizerArgument(referent, finalizer, out _, out string? rejection);

        Assert.IsFalse(bound,
            "ARM 5: a pointer object with a STRING finalizer parameter. Go rejects this at registration; " +
            "accepting it here would mean a pairing Go refuses is taken and then fails silently at " +
            "dispatch, which is the OTHER half of the defect this predicate replaces.");
        Assert.IsNotNull(rejection, "ARM 5: a refusal must carry Go's text, not a bare false.");
        StringAssert.StartsWith(rejection!, "runtime.SetFinalizer: cannot pass ",
            "ARM 5: the message is Go's, read from runtime/mfinal.go at BOTH pinned toolchains " +
            "(1.23.12 :473/:476/:499 and 1.24.13 :495/:498/:521 — byte-identical), and a test that " +
            $"accepts a different one would let the port's own wording drift. Got: {rejection}");
        StringAssert.Contains(rejection!, " to finalizer ",
            $"ARM 5: Go's text names BOTH types, object first. Got: {rejection}");
    }

    [TestMethod]
    public void Arm6_AFinalizerTakingTwoArgumentsIsRefused()
    {
        ж<nint> referent = NewIntBox(0);
        Action<ж<nint>, nint> finalizer = (_, _) => { };

        Assert.IsFalse(GoReflect.TryBindFinalizerArgument(referent, finalizer, out _, out string? rejection),
            "ARM 6: Go requires EXACTLY ONE input (mfinal.go's `ft.InCount != 1`). A two-argument " +
            "delegate could never be invoked, and before this predicate it was accepted at registration " +
            "and dropped in silence at dispatch.");
        Assert.IsNotNull(rejection, "ARM 6: the arity refusal carries Go's text too.");
    }
}
