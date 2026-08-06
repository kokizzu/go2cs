using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using static go2cs.Symbols;

namespace GolibTests;

// A Go METHOD SET is reconstructed at run time from EMITTED C#, and the emitted name is not always
// the Go name: a `-tests` variant Δ-renames a test-file method declarator whose bare name would
// hijack a same-named dot-imported function at every unqualified call site (B9 in the converter's
// name-collision analysis). io's `func (c *writeStringChecker) WriteString` emits as `ΔWriteString`
// against the dot-imported `io.WriteString`, while `io.StringWriter`'s member keeps the bare name —
// so a probe comparing emitted names answers MISS for a type Go says implements the interface.
//
// Why this is guarded HERE and not in the behavioral suite. The Δ-rename is produced ONLY by a
// `-tests` conversion, over a declarator in a `_test.go` file; an ordinary behavioral project is an
// ordinary `go2cs <dir>` conversion with no test files, so no behavioral program can emit the shape
// at all. The one corpus that does — `io`'s converted suite — is not banked yet (four `os`-arc rows
// are open), so it is not a standing gate either. These fixtures are therefore shaped like the
// emitted code the converter really produces: a `[GoRecv] this ref X` primary with the
// RecvGenerator's `ж<X>` twin beside it, under a Δ-prefixed name.
//
// Each test owns unique types: the method-set candidate cache is process-wide, so sharing a
// receiver between tests would make them order-dependent.
[TestClass]
public class GoMethodNameProjectionTests
{
    // The fixtures spell the marker LITERALLY, because emitted identifiers are literal — this pins
    // that what they spell is still the marker the runtime projects away. Without it a marker change
    // would leave the fixtures guarding nothing rather than failing.
    [TestMethod]
    public void FixtureSpellsTheCollisionAvoidanceMarker()
    {
        MethodInfo renamed = typeof(GoMethodNameProjectionFixtures)
            .GetMethod(nameof(GoMethodNameProjectionFixtures.ΔScribe), [typeof(ж<DeltaOnlyScribe>), typeof(string)]);

        Assert.IsNotNull(renamed);
        Assert.AreEqual($"{ShadowVarMarker}Scribe", renamed.Name);
    }

    // The defect, at the probe: `*DeltaOnlyScribe`'s only `Scribe` is emitted `ΔScribe`, so an
    // emitted-name comparison answers MISS and no duck-typing shell is ever built for the pair.
    [TestMethod]
    public void ADeltaRenamedMethodSatisfiesTheInterfaceItCarries()
    {
        Assert.IsTrue(typeof(ж<DeltaOnlyScribe>).StructurallyImplements(typeof(IScribeShape)));
    }

    // The binder must agree with the probe — a shell that could not bind what the probe counted
    // would resolve the assert and then null-dispatch.
    [TestMethod]
    public void TheBinderResolvesADeltaRenamedMethodByItsGoName()
    {
        AdapterBinder.ResolveReceiverMethods(typeof(DeltaOnlyScribe), "Scribe", out MethodInfo byPtr, out MethodInfo byVal);

        Assert.IsNotNull(byPtr, "the ж<X> twin of the Δ-renamed method is the bindable form");
        Assert.AreEqual($"{ShadowVarMarker}Scribe", byPtr.Name);
        Assert.IsNull(byVal, "the [GoRecv] `this ref X` primary is a pointer-receiver method, never the by-value half");
    }

    // The projection is a SECOND pass, so a plainly-named candidate always wins. Both halves of the
    // rule matter: if the projection ran first, a Δ-renamed method could displace the real one.
    [TestMethod]
    public void TheExactEmittedNameWinsOverAProjectedOne()
    {
        AdapterBinder.ResolveReceiverMethods(typeof(BothNamesScribe), "Scribe", out MethodInfo byPtr, out MethodInfo _);

        Assert.IsNotNull(byPtr);
        Assert.AreEqual("Scribe", byPtr.Name);
        Assert.AreEqual("plain", byPtr.Invoke(null, [new ж<BothNamesScribe>(new BothNamesScribe()), "x"]));
    }

    // The projection recovers a NAME; it must not weaken the signature match that name gates. A
    // Δ-renamed candidate whose Go signature differs is still a MISS, exactly as the bare-named case
    // is (the `Unwrap() error` vs `Unwrap() []error` conflation this probe exists to prevent).
    [TestMethod]
    public void AProjectedNameStillRequiresAMatchingSignature()
    {
        Assert.IsFalse(typeof(ж<WrongSignatureScribe>).StructurallyImplements(typeof(IScribeShape)));
    }

    // A type with no candidate under either spelling stays a MISS — the projection widens nothing.
    [TestMethod]
    public void AnUnrelatedTypeStillMisses()
    {
        Assert.IsFalse(typeof(ж<SilentScribe>).StructurallyImplements(typeof(IScribeShape)));
    }
}

/// <summary>Interface the fixtures below are probed against; its member carries the bare Go name.</summary>
public interface IScribeShape
{
    string Scribe(string text);
}

/// <summary>Carries its <c>Scribe</c> method under the Δ-renamed emission ONLY.</summary>
public struct DeltaOnlyScribe;

/// <summary>Carries BOTH spellings, so exact-over-projected precedence is observable.</summary>
public struct BothNamesScribe;

/// <summary>Carries a Δ-renamed <c>Scribe</c> whose signature does not match the interface.</summary>
public struct WrongSignatureScribe;

/// <summary>Carries no <c>Scribe</c> at all.</summary>
public struct SilentScribe;

// Extension methods are discovered only from a top-level, sealed, non-generic class — which is what
// a converted `<pkg>_package` static class is — so these cannot be nested in the test class.
public static class GoMethodNameProjectionFixtures
{
    [GoRecv] public static string ΔScribe(this ref DeltaOnlyScribe s, string text) => $"delta:{text}";

    public static string ΔScribe(this ж<DeltaOnlyScribe> s, string text) => $"delta:{text}";

    [GoRecv] public static string Scribe(this ref BothNamesScribe s, string text) => $"plain:{text}";

    public static string Scribe(this ж<BothNamesScribe> s, string text) => "plain";

    [GoRecv] public static string ΔScribe(this ref BothNamesScribe s, string text) => $"delta:{text}";

    public static string ΔScribe(this ж<BothNamesScribe> s, string text) => "delta";

    [GoRecv] public static string ΔScribe(this ref WrongSignatureScribe s, int count) => $"delta:{count}";

    public static string ΔScribe(this ж<WrongSignatureScribe> s, int count) => $"delta:{count}";
}
