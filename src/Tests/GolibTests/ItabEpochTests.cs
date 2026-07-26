using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class ItabEpochTests
{
    // golib projects both type-assert tiers -- the generated NOMINAL adapters in AdapterRegistry
    // and the runtime duck-typing shells AdapterBinder builds -- into one per-interface itab cache
    // (builtin.Itab<TInterface>), so a resolved pair costs one lookup instead of two. Unifying them
    // reintroduces the hazard the two separate dictionaries avoided by construction: a decision
    // memoized BEFORE a lazily-loaded assembly's module initializer registers the nominal adapter
    // for that same pair. AdapterRegistry.Register therefore bumps an epoch that every itab entry
    // carries, and an entry from an older epoch is never matched.
    //
    // Why this is guarded HERE and not in the behavioral suite. The behavioral project
    // Tests/Behavioral/ItabLateRegistration drives the whole shape end to end from Go -- shells
    // decided first, a real module initializer registering real generated adapters afterwards,
    // every pair re-formed -- and it proves the invalidation is complete and harmless. What it
    // cannot prove is that the invalidation HAPPENS, because tier precedence is deliberately
    // invisible from Go: a shell and a nominal adapter forward to the same receiver methods and
    // unwrap to the same dynamic value, so a stale shell answering in place of an adapter prints
    // exactly the same output. It could only become observable as a Go-vs-C# divergence, which the
    // suite forbids. At the golib level the flip IS observable, and these tests fail outright if
    // the epoch is removed.
    //
    // Each test owns unique types: the caches are process-wide and deliberately never cleared, so
    // sharing a pair between tests would make them order-dependent.

    public interface IGreetLate
    {
        string Greet();
    }

    public sealed class LateWidget
    {
        public string Greet() => "late-widget";
    }

    public sealed class LateWidgetGreeter(LateWidget widget) : IGreetLate
    {
        public string Greet() => widget.Greet();
    }

    // The core of the epoch: a pair decided as a MISS must start SUCCEEDING once a nominal adapter
    // is registered for it. Without the epoch the memoized miss answers forever and the second
    // assertion below stays false -- which is precisely the "a memoized decision must never shadow
    // an adapter registered later" invariant, made visible.
    [TestMethod]
    public void LateRegistrationSupersedesAMemoizedMiss()
    {
        LateWidget widget = new();

        (IGreetLate before, bool okBefore) = ((object)widget)._<IGreetLate>(ꟷ);

        Assert.IsFalse(okBefore, "no adapter is registered yet and no generated shell exists for this pair");
        Assert.IsNull(before);

        AdapterRegistry.Register(typeof(LateWidget), typeof(IGreetLate), static value => new LateWidgetGreeter((LateWidget)value));

        (IGreetLate after, bool okAfter) = ((object)widget)._<IGreetLate>(ꟷ);

        Assert.IsTrue(okAfter, "the registration must invalidate the memoized miss");
        Assert.AreEqual("late-widget", after.Greet());
    }

    public interface IGreetSlot
    {
        string Greet();
    }

    public sealed class SlotAlpha;

    public sealed class SlotBeta;

    public sealed class SlotAlphaGreeter : IGreetSlot
    {
        public string Greet() => "alpha";
    }

    public sealed class SlotBetaGreeter : IGreetSlot
    {
        public string Greet() => "beta";
    }

    // The itab keeps a monomorphic slot in front of its dictionary, holding (dynamic type,
    // resolver) as ONE immutable reference so the pair can never be observed torn. If the slot
    // ever paired one type with another's resolver the wrong implementation would be constructed
    // silently, so alternate the dynamic type on every assert -- which displaces the slot every
    // time -- and demand that each answer belongs to its own type.
    [TestMethod]
    public void MonomorphicSlotNeverPairsATypeWithAnotherTypesResolver()
    {
        AdapterRegistry.Register(typeof(SlotAlpha), typeof(IGreetSlot), static _ => new SlotAlphaGreeter());
        AdapterRegistry.Register(typeof(SlotBeta), typeof(IGreetSlot), static _ => new SlotBetaGreeter());

        object alpha = new SlotAlpha();
        object beta = new SlotBeta();

        for (int i = 0; i < 8; i++)
        {
            (IGreetSlot a, bool okA) = alpha._<IGreetSlot>(ꟷ);
            (IGreetSlot b, bool okB) = beta._<IGreetSlot>(ꟷ);

            Assert.IsTrue(okA);
            Assert.IsTrue(okB);
            Assert.AreEqual("alpha", a.Greet(), $"slot mispaired SlotAlpha on iteration {i}");
            Assert.AreEqual("beta", b.Greet(), $"slot mispaired SlotBeta on iteration {i}");
        }
    }

    public interface IFirstOfTwo
    {
        string Which();
    }

    public interface ISecondOfTwo
    {
        string Which();
    }

    public sealed class TwoFaced;

    public sealed class FirstFace : IFirstOfTwo
    {
        public string Which() => "first";
    }

    // One dynamic type against two interfaces: the caches are per closed generic, and a later
    // registration on one interface must not make the other start answering. This is the shape a
    // single shared (Type, Type) cache would get right for free and a per-interface projection can
    // get wrong -- so it is worth pinning explicitly.
    [TestMethod]
    public void PerInterfaceCachesDoNotAnswerForEachOther()
    {
        object value = new TwoFaced();

        Assert.IsFalse(value._<IFirstOfTwo>(ꟷ).Item2);
        Assert.IsFalse(value._<ISecondOfTwo>(ꟷ).Item2);

        AdapterRegistry.Register(typeof(TwoFaced), typeof(IFirstOfTwo), static _ => new FirstFace());

        (IFirstOfTwo first, bool okFirst) = value._<IFirstOfTwo>(ꟷ);

        Assert.IsTrue(okFirst, "the registered interface must resolve after the epoch bump");
        Assert.AreEqual("first", first.Which());

        Assert.IsFalse(value._<ISecondOfTwo>(ꟷ).Item2, "the unregistered interface must still miss");
    }
}
