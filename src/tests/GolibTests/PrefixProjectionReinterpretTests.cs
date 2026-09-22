using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// Go's PREFIX-DOWNCAST idiom over a descriptor, `(*SwissMapType)(unsafe.Pointer(abi.TypeOf(m)))`
// (internal/runtime/maps' newTestMapType), emitted Reinterpret<abi.Type, SwissMapType>(). A synthetic
// ж<abi.Type> holds only the header, so the pair can never alias (the destination does not fit) and the
// address route handed out a native box over the header's ORDER TOKEN -- whose first dereference is the
// ruled arm-2a refusal. internal/runtime/maps measured it as 104 of 111 verdicts behind one panic.
//
// The owning package registers the faithful answer (abi's cached Type.MapType() projection) as a
// PrefixProjection, and Reinterpret returns it. Criteria: the registered answer is returned, and it is
// the SAME box on every call (pointer identity is Go-observable); a projection that answers nil, and an
// unregistered pair, both keep the address route exactly as it was -- including the arm-2a refusal on
// the first dereference, which must still fire for a genuinely address-less box.
//
// Each test owns its own type pair: the registration is a process-wide static, and a pair shared
// between tests would leak one test's plant into the next.
[TestClass]
public class PrefixProjectionReinterpretTests
{
    private struct HeaderA { internal object kind; }
    private struct WideA { internal HeaderA head; internal object extra; }

    private struct HeaderB { internal object kind; }
    private struct WideB { internal HeaderB head; internal object extra; }

    private struct HeaderC { internal object kind; }
    private struct WideC { internal HeaderC head; internal object extra; }

    [TestMethod]
    public void ARegisteredProjectionIsTheAnswer_AndTheSameBoxEveryTime()
    {
        ж<HeaderA> header = new StandardBox<HeaderA>(new HeaderA { kind = "map" });
        ж<WideA> wide = new StandardBox<WideA>(new WideA { head = header.Value, extra = "elem" });

        PrefixProjection<HeaderA, WideA>.Project = box => box == header ? wide : null;

        ж<WideA> first = header.Reinterpret<HeaderA, WideA>();
        ж<WideA> second = header.Reinterpret<HeaderA, WideA>();

        Assert.AreSame(wide, first, "the downcast answers the owner's projection");
        Assert.AreSame(first, second, "and the same pointer every time, as Go's cast of one address is");
        Assert.AreEqual("elem", first.Value.extra, "whose storage is real -- the dereference is honoured");
    }

    [TestMethod]
    public void AProjectionThatAnswersNil_KeepsTheAddressRoute_AndArm2aStillRefuses()
    {
        ж<HeaderB> header = new StandardBox<HeaderB>(new HeaderB { kind = "struct" });

        PrefixProjection<HeaderB, WideB>.Project = static _ => null;

        ж<WideB> derived = header.Reinterpret<HeaderB, WideB>();

        Assert.IsTrue(derived.IsNative, "a nil projection falls through to the address route unchanged");
        Assert.ThrowsException<PanicException>(() => _ = derived.Value.extra,
            "and the arm-2a refusal still fires on the first dereference of an address-less header");
    }

    [TestMethod]
    public void AnUnregisteredPair_KeepsTheAddressRoute_AndArm2aStillRefuses()
    {
        ж<HeaderC> header = new StandardBox<HeaderC>(new HeaderC { kind = "array" });

        Assert.IsNull(PrefixProjection<HeaderC, WideC>.Project, "premise: nothing registered for this pair");

        ж<WideC> derived = header.Reinterpret<HeaderC, WideC>();

        Assert.IsTrue(derived.IsNative, "the address route, as before this seat");
        Assert.ThrowsException<PanicException>(() => _ = derived.Value.extra,
            "the control: a genuinely address-less box is still refused by name, catchably");
    }
}
