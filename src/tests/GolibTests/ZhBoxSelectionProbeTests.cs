using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go; // ж<T> — the probe references it by open generic to locate golib's assembly

namespace GolibTests;

/// <summary>
/// B′-S0's binding guard: the compile-probe matrix over every must-not-select receiver shape
/// named by <c>DESIGN-zh-box-b-prime.md</c> §4.2.
/// </summary>
/// <remarks>
/// <para>
/// The design's boldest claim is its §4.2 invariant: <b>every must-not-select case either fails to
/// compile on the ref-receiver primary, or resolves to the ж twin by C#'s own overload rules</b> —
/// so there is no silent-wrong-selection class, and the selection rule's failure mode is a build
/// error rather than corrupted aliasing. The 2026-08-21 ratification made that claim's mechanical
/// proof a BINDING part of S0: it must be enforced by construction, not carried by argument.
/// </para>
/// <para>
/// These probes need no converter change and no corpus regen, because the primary/twin pair
/// <i>already exists in production</i>: <c>sync/atomic</c>'s hand-owned <c>type.cs</c> declares
/// <c>[GoRecv] Load/Store/Add/… (this ref Int32 x)</c> — the ref-receiver primary — and
/// <c>RecvGenerator</c> mints the <c>this ж&lt;Int32&gt; Ꮡx</c> twin beside it. That pair is banked
/// (roster row 159, sync/atomic 108/108 + 0), so the probes below interrogate the REAL emitted
/// shapes against the REAL golib <c>ж&lt;T&gt;</c>, not a synthetic mirror that could diverge from
/// what the converter actually emits.
/// </para>
/// <para>
/// The verdict instrument is the compiler itself, twice over: <see cref="SemanticModel"/> reports
/// which overload it selected (so "binds the twin" is Roslyn's own answer, not an inference from a
/// runtime side effect), and the diagnostic bag reports the refusal when the primary cannot bind.
/// Each probe asserts the design's DISJUNCTION directly — refused, or bound to the twin — and
/// records which arm fired, because the claim is satisfied by either.
/// </para>
/// <para>
/// ⚠ The matrix is self-falsifying by construction: the positive controls (§4.2's "selects
/// primary" rows) must report <see cref="Bound.Primary"/>. A classifier that had degenerated into
/// answering "Twin" for everything — the way a guard silently rots — fails those rows immediately.
/// The negative rows alone could not catch that.
/// </para>
/// </remarks>
[TestClass]
public class ZhBoxSelectionProbeTests
{
    /// <summary>Which half of the dual-emission pair the compiler selected.</summary>
    private enum Bound
    {
        /// <summary>No method bound at all — the call was refused.</summary>
        None,

        /// <summary>The ref-receiver primary: <c>this ref Int32</c>.</summary>
        Primary,

        /// <summary>The box twin: <c>this ж&lt;Int32&gt;</c>.</summary>
        Twin
    }

    private readonly record struct ProbeResult(Bound Bound, Bound Rejected, ImmutableArray<Diagnostic> Errors)
    {
        public bool Refused => Errors.Length > 0;

        public string ErrorIds => string.Join(",", Errors.Select(static d => d.Id).Distinct());
    }

    // The scaffold carries one declaration per receiver shape §4.2 names. Every probe substitutes a
    // single statement into Run(), so exactly one invocation exists to resolve.
    private const string Scaffold = """
        using System;
        using System.Collections.Generic;
        using go;
        using go.sync;
        using atomic = go.sync.atomic_package;

        public interface ICounter { int Add(int delta); }

        public class Probe
        {
            public atomic.Int32 Field;                       // a field lvalue: ref-addressable
            public ж<atomic.Int32> Box = default!;           // the row-3 pointer var
            public Dictionary<int, atomic.Int32> Map = new();// map index: NOT ref-addressable
            public atomic.Int32 Prop => default;             // property-shaped: NOT ref-addressable
            public atomic.Int32[] Arr = new atomic.Int32[2]; // element: ref-addressable
            public bool Flag;

            public void Run()
            {
                atomic.Int32 local = default;
                ref atomic.Int32 alias = ref local;
                {{STATEMENT}}
            }
        }

        {{EXTRA}}
        """;

    private static readonly Lazy<ImmutableArray<MetadataReference>> s_references = new(BuildReferences);

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();

        // The framework surface, taken from THIS test host's own trusted-platform set so the probe
        // compilation targets exactly the runtime the corpus is built against — no hand-listed
        // facade set to go stale at the next TFM hop.
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            foreach (string path in trusted.Split(Path.PathSeparator))
            {
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    builder.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // The two assemblies under test: golib supplies the real ж<T>; sync/atomic supplies the real
        // banked primary/twin pair. Both are already ProjectReferences of GolibTests, so their
        // locations come from the loaded assemblies rather than from a guessed path.
        AddAssemblyOf(builder, typeof(ж<>));
        AddAssemblyOf(builder, typeof(go.sync.atomic_package.Int32));

        return builder.ToImmutable();
    }

    private static void AddAssemblyOf(ImmutableArray<MetadataReference>.Builder builder, Type type)
    {
        string location = type.Assembly.Location;

        if (!string.IsNullOrEmpty(location) && File.Exists(location))
            builder.Add(MetadataReference.CreateFromFile(location));
    }

    /// <summary>
    /// The R3 arm-(a) tests' bridge into this matrix's compiler/reference plumbing — same scaffold,
    /// same classification, a string verdict so the caller carries no Roslyn types.
    /// </summary>
    internal static void ProbePublic(string statement, string extra, out string bound, out int errors)
    {
        ProbeResult result = Probe(statement, extra);
        bound = result.Bound.ToString();
        errors = result.Errors.Length;
    }

    private static ProbeResult Probe(string statement, string extra = "")
    {
        string source = Scaffold.Replace("{{STATEMENT}}", statement).Replace("{{EXTRA}}", extra);

        CSharpCompilation compilation = CSharpCompilation.Create(
            "zhbox-selection-probe",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            s_references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        ImmutableArray<Diagnostic> errors = compilation
            .GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        SyntaxTree tree = compilation.SyntaxTrees[0];
        SemanticModel model = compilation.GetSemanticModel(tree);

        // Resolve the `.Add` site itself. A method VALUE (`f = x.Add`) has no invocation at all, so
        // the member access is the node to ask about; an ordinary call has both, and asking the
        // member access gives the same method symbol.
        MemberAccessExpressionSyntax? site = tree.GetRoot()
            .DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(static m => m.Name.Identifier.ValueText == "Add");

        // A probe with no `.Add` site is a broken probe, not a finding — say so loudly rather than
        // reporting Bound.None, which is a real and different outcome.
        Assert.IsNotNull(site, $"probe statement contains no `.Add` site to resolve: {statement}");

        SymbolInfo info = model.GetSymbolInfo(site);

        // ⚠ Only Symbol counts as BOUND. CandidateSymbols holds what the compiler considered and
        // REJECTED, so folding it in here would report a refused call as a successful primary
        // binding — manufacturing a §4.2 violation out of the exact refusal the design predicts.
        // The rejected candidate is still worth carrying: it is what distinguishes "the primary was
        // never applicable" from "the primary was applicable but the receiver was not a variable".
        Bound bound = Classify(info.Symbol as IMethodSymbol);
        Bound rejected = Classify(info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault());

        return new ProbeResult(bound, rejected, errors);
    }

    private static Bound Classify(IMethodSymbol? method)
    {
        if (method is null)
            return Bound.None;

        // An extension method invoked in reduced form (`x.Add(1)`) reports the reduced symbol;
        // ReducedFrom recovers the static declaration whose first parameter IS the receiver — which
        // is the only place the primary and the twin differ.
        IMethodSymbol declared = method.ReducedFrom ?? method;

        if (declared.Parameters.Length == 0)
            return Bound.None;

        IParameterSymbol receiver = declared.Parameters[0];

        if (receiver.RefKind == RefKind.Ref)
            return Bound.Primary;

        return receiver.Type.Name == "ж" ? Bound.Twin : Bound.None;
    }

    /// <summary>
    /// Asserts §4.2's invariant for one must-not-select shape: the primary must NOT bind, which the
    /// design permits to happen either way — a build refusal, or a clean fall to the twin.
    /// </summary>
    private static void AssertMustNotSelectPrimary(string statement, string shape)
    {
        ProbeResult result = Probe(statement);

        // ⚠ The invariant is a DISJUNCTION, and the refusal arm is the subtle one. Overload
        // resolution can legitimately SELECT the primary — it is the only applicable overload for a
        // receiver of type Int32 — and then the argument-conversion stage refuses the receiver
        // because it is not a variable (CS0206/CS1510). Roslyn therefore reports the primary as the
        // site's symbol on a build that does not compile. That is precisely §4.2's "mis-selection
        // FAILS THE BUILD rather than corrupting", so a refused compilation satisfies the claim
        // however the site is labelled. Asserting on the label instead of the build outcome reads a
        // satisfied invariant as a violation.
        if (result.Refused)
            return;

        Assert.AreEqual(Bound.Twin, result.Bound,
            $"§4.2 violated — {shape} compiled CLEANLY and bound {result.Bound} rather than the twin. " +
            "That is the silent-wrong-selection class the design claims cannot exist.");
    }

    // ---------------------------------------------------------------------------------------
    // Positive controls — §4.2's "selects primary" rows. These are what make the matrix
    // self-falsifying: a classifier stuck on "Twin" fails here, not in the negative rows.
    // ---------------------------------------------------------------------------------------

    [TestMethod]
    public void PlainLocalSelectsThePrimary()
    {
        // §4.2 row 2: "plain local / parameter of type T whose address Go takes only for the call".
        Assert.AreEqual(Bound.Primary, Probe("local.Add(1);").Bound);
    }

    [TestMethod]
    public void RefAliasedLocalSelectsThePrimary()
    {
        // §4.2 row 1: the `ref var z2 = ref heap(…)` shape — the 1,016-local class B′ exists to serve.
        Assert.AreEqual(Bound.Primary, Probe("alias.Add(1);").Bound);
    }

    [TestMethod]
    public void FieldLvalueSelectsThePrimary()
    {
        // §4.2 row 3: "field lvalue reachable without a box hop — `ref s.f` is legal and free".
        Assert.AreEqual(Bound.Primary, Probe("Field.Add(1);").Bound);
    }

    [TestMethod]
    public void ArrayElementSelectsThePrimary()
    {
        // Array elements are ref-addressable storage; the row-4 `&s[i]` family.
        Assert.AreEqual(Bound.Primary, Probe("Arr[0].Add(1);").Bound);
    }

    // ---------------------------------------------------------------------------------------
    // The must-not-select matrix — one probe per §4.2 "twin" row.
    // ---------------------------------------------------------------------------------------

    [TestMethod]
    public void BoxTypedReceiverDoesNotSelectThePrimary() =>
        // §4.2: "ж-typed expression (row-3 pointer var) → twin. The box exists; forwarding through
        // DerefOrNull at the twin costs nothing new."
        AssertMustNotSelectPrimary("Box.Add(1);", "ж-typed receiver");

    [TestMethod]
    public void MapIndexReceiverDoesNotSelectThePrimary() =>
        // §4.2: non-ref-addressable receiver — "`this ref T` cannot bind; C# enforces it, so
        // mis-selection FAILS THE BUILD rather than corrupting".
        AssertMustNotSelectPrimary("Map[0].Add(1);", "map index");

    [TestMethod]
    public void PropertyShapedReceiverDoesNotSelectThePrimary() =>
        // §4.2: the property-shaped accessor arm of the same row — a value, not storage.
        AssertMustNotSelectPrimary("Prop.Add(1);", "property-shaped accessor");

    [TestMethod]
    public void ConditionalExpressionReceiverDoesNotSelectThePrimary() =>
        // §4.2: the conditional-expression arm — the classic non-addressable value.
        AssertMustNotSelectPrimary("(Flag ? Field : local).Add(1);", "conditional expression");

    [TestMethod]
    public void InterfaceTypedReceiverDoesNotSelectThePrimary()
    {
        // §4.2: "interface-typed receiver → twin (via adapter); dispatch surface." The probe binds
        // the interface's own member, which is precisely NOT the primary — the adapter is what
        // reaches the pair, and it holds a box.
        ProbeResult result = Probe("((ICounter)null!).Add(1);");

        Assert.AreNotEqual(Bound.Primary, result.Bound,
            "§4.2 violated — the ref-receiver primary bound through an interface-typed receiver.");
    }

    [TestMethod]
    public void MethodValueDoesNotSelectThePrimary()
    {
        // §4.2: "method value / delegate → twin; delegate parameter types." A ref-receiver extension
        // method cannot be captured as a Func<int,int> method group: C# forbids a method group
        // conversion over a `this ref` extension. Resolved at the member-access site, since a method
        // value has no invocation node at all.
        ProbeResult result = Probe("Func<int, int> f = local.Add; f(1);");

        Assert.AreNotEqual(Bound.Primary, result.Bound,
            "§4.2 violated — the primary was captured as a method value, which would hand a delegate " +
            $"a ref-receiver it cannot carry. errors=[{result.ErrorIds}]");

        Assert.IsTrue(result.Refused || result.Bound == Bound.Twin,
            $"§4.2's disjunction failed for a method value: bound={result.Bound}, " +
            $"rejectedCandidate={result.Rejected}, errors=[{result.ErrorIds}]");
    }

    [TestMethod]
    public void ResultUsedDirectCallStillDoesNotSelectPrimaryOnABox() =>
        // §9 OQ-7: "result-used direct call → twin — the primary cannot yield the receiver's box
        // without minting it." Probed at its box-receiver form, which is where the rule bites.
        AssertMustNotSelectPrimary("int r = Box.Add(1); GC.KeepAlive(r);", "result-used call on a box");

    // ---------------------------------------------------------------------------------------
    // Harness integrity — the probes above are only as trustworthy as the machinery under them.
    // ---------------------------------------------------------------------------------------

    [TestMethod]
    public void TheRefusalCodesAreTheMEASUREDOnesNotTheDesignsCitedPair()
    {
        // §4.2 cites "CS1510/CS1657" as the enforcement codes. Measured against the real pair, the
        // majority code is neither: a map index and a property-shaped accessor are refused with
        // **CS0206** ("a property or indexer may not be passed as an out or ref parameter"), and only
        // the conditional expression yields CS1510. CS1657 does not fire anywhere in this matrix.
        //
        // Pinned rather than merely noted, so that a future compiler behaviour change — a code that
        // moves, or worse, a refusal that quietly stops firing — is caught here rather than
        // discovered by a corpus that silently started aliasing a temporary.
        Assert.AreEqual("CS0206", Probe("Map[0].Add(1);").ErrorIds, "map index");
        Assert.AreEqual("CS0206", Probe("Prop.Add(1);").ErrorIds, "property-shaped accessor");
        Assert.AreEqual("CS1510", Probe("(Flag ? Field : local).Add(1);").ErrorIds, "conditional expression");
    }

    [TestMethod]
    public void TheMatrixCatchesASimulatedSilentWrongSelection()
    {
        // A gate that has never been made to fail proves nothing, so the falsification is baked in
        // rather than run once by hand: inject a BY-VALUE `Add` extension beside the real pair and
        // re-probe the map-index shape. That is a faithful simulation of the hazard §4.2 says cannot
        // exist — an overload the receiver CAN bind by value, so the call compiles cleanly, silently
        // operating on a COPY of a temporary instead of failing the build.
        //
        // Under that mutation the shape must stop being refused and must not reach the twin, which
        // is exactly the condition AssertMustNotSelectPrimary flags. If this test ever passes
        // trivially — because the probe stopped compiling, or the classifier degenerated — the
        // matrix above has quietly become vacuous and the real invariant is no longer being tested.
        const string ByValueOverload = """
            public static class SilentWrongSelectionSimulation
            {
                public static int Add(this atomic.Int32 x, int delta) => delta;
            }
            """;

        ProbeResult mutated = Probe("Map[0].Add(1);", ByValueOverload);

        Assert.IsFalse(mutated.Refused,
            "the simulation is not simulating: a by-value overload should make the map-index shape " +
            $"compile cleanly, but it was still refused with [{mutated.ErrorIds}]");

        Assert.AreNotEqual(Bound.Twin, mutated.Bound,
            "the simulation is not simulating: the mutated shape reached the twin, so it does not " +
            "represent a silent wrong selection at all");

        // And the control: without the mutation, the very same shape IS refused. The pair of
        // assertions is what proves the matrix discriminates, rather than always answering the same way.
        Assert.IsTrue(Probe("Map[0].Add(1);").Refused,
            "the unmutated map-index shape must still be refused, or the matrix proves nothing");
    }

    [TestMethod]
    public void TheProbeScaffoldItselfCompilesClean()
    {
        // If the scaffold carried a latent error, every "Refused" verdict above would be vacuous:
        // the matrix would report the design's invariant satisfied on the strength of an unrelated
        // compile failure. This pins the baseline at zero errors.
        ProbeResult result = Probe("local.Add(1);");

        Assert.AreEqual(0, result.Errors.Length,
            $"the probe scaffold must compile clean or every refusal verdict is vacuous; got [{result.ErrorIds}]");
    }

    [TestMethod]
    public void BothHalvesOfThePairAreVisibleToTheProbe()
    {
        // The whole matrix is meaningless if only one overload is in scope — a "binds the twin"
        // result would then be forced rather than chosen, and a "refused" result could just mean
        // the primary was never a candidate. Assert the pair really is a pair.
        Assert.AreEqual(Bound.Primary, Probe("local.Add(1);").Bound, "the ref-receiver primary must be reachable");
        Assert.AreEqual(Bound.Twin, Probe("Box.Add(1);").Bound, "the ж twin must be reachable");
    }
}

// ---------------------------------------------------------------------------------------------
// R3 arm (a) — the ref-return fluent primary (ruled 2026-09-02; DESIGN-zh-box-b-prime.md's dated
// amendment block carries the gap and the census: RECV-ONLY 38 / NO-RECV 32 / MIXED 5 in the S0
// target packages). A RECV-ONLY method's Go body is `return v`; the primary cannot yield the
// receiver's box (OQ-7), so under R3 the PRIMARY returns `ref T` (the receiver itself, free) and
// the TWIN delegates then returns ITS OWN box — which is exactly Go's semantics, since Go's
// `return v` returns the receiver pointer and the twin HOLDS that pointer. These tests pin both
// halves: the compile-time selection semantics of the ref-return shape (string-form pair through
// the same Roslyn probe), and the runtime identity/mutation contract (real pair, executed).
// ---------------------------------------------------------------------------------------------

/// <summary>The R3 arm-(a) synthetic pair, REAL and executable — the identity guard runs on it.</summary>
public struct FluentElem
{
    public int V;
}

public static class FluentElemExtensions
{
    /// <summary>The R3 primary: ref-return of the receiver, allocation-free.</summary>
    public static ref FluentElem Add(this ref FluentElem v, int delta)
    {
        v.V += delta;
        return ref v;
    }

    /// <summary>The twin: delegates to the primary, returns ITS OWN box — Go's receiver pointer.</summary>
    public static ж<FluentElem> Add(this ж<FluentElem> Ꮡv, int delta)
    {
        Ꮡv.DerefOrNull().Add(delta);
        return Ꮡv;
    }
}

[TestClass]
public class ZhBoxFluentPrimaryTests
{
    // The string-form of the SAME pair for the Roslyn selection probes. Kept textually beside the
    // real pair above; the executable pair is the identity guard's subject, the string pair the
    // selection probes' — one shape, two instruments.
    private const string FluentExtra = """
        public struct FluentElem { public int V; }
        public static class FluentElemExtensions
        {
            public static ref FluentElem Add(this ref FluentElem v, int delta) { v.V += delta; return ref v; }
            public static ж<FluentElem> Add(this ж<FluentElem> Ꮡv, int delta) { Ꮡv.DerefOrNull().Add(delta); return Ꮡv; }
        }
        """;

    [TestMethod]
    public void PlainLocalSelectsTheRefReturnPrimary()
    {
        ZhBoxSelectionProbeTests.ProbePublic("FluentElem e = default; e.Add(1);", FluentExtra, out var bound, out int errors);
        Assert.AreEqual(0, errors);
        Assert.AreEqual("Primary", bound, "a plain local must select the ref-return primary exactly as it selects today's shape");
    }

    [TestMethod]
    public void BoxTypedReceiverSelectsTheTwinUnderRefReturn()
    {
        ZhBoxSelectionProbeTests.ProbePublic("ж<FluentElem> b = default!; b.Add(1);", FluentExtra, out var bound, out int errors);
        Assert.AreEqual(0, errors);
        Assert.AreEqual("Twin", bound, "a ж-typed receiver must keep the twin under the ref-return primary");
    }

    [TestMethod]
    public void RefCaptureOfTheChainCompiles()
    {
        // The chain-forward property the ref return buys: a direct site MAY capture the ref
        // without any box existing anywhere.
        ZhBoxSelectionProbeTests.ProbePublic("FluentElem e = default; ref FluentElem r = ref e.Add(1); r.V++;", FluentExtra, out var bound, out int errors);
        Assert.AreEqual(0, errors, "ref-capturing the primary's chain must compile");
        Assert.AreEqual("Primary", bound);
    }

    [TestMethod]
    public void ResultUsedByValueCompilesAndBindsThePrimary_TheConverterOwnsThisRow()
    {
        // ⚠ DELIBERATELY INVERTED expectations, documenting the enforcement locus: `var p =
        // e.Add(1)` COMPILES under a ref-return primary (the ref converts to a value copy) and
        // binds the primary — the compiler does NOT catch result-use, so OQ-7's "result-used
        // direct calls stay on the twin" is enforced at CONVERTER EMISSION, not by C#. If this row
        // ever starts refusing, the enforcement boundary moved and the selection table's
        // documentation must move with it.
        ZhBoxSelectionProbeTests.ProbePublic("FluentElem e = default; var p = e.Add(1); p.V++;", FluentExtra, out var bound, out int errors);
        Assert.AreEqual(0, errors, "the value-copy capture is LEGAL C# — the converter, not the compiler, owns this row");
        Assert.AreEqual("Primary", bound);
    }

    [TestMethod]
    public void TwinReturnsItsOwnBox_TheIdentityGuard()
    {
        // Go: `p := Ꮡv.Add(1)` yields the SAME pointer — `p == Ꮡv` is true. The twin returns its
        // own box, so identity holds by construction; this row is what makes that a measured fact.
        ж<FluentElem> box = new StandardBox<FluentElem>(default(FluentElem));

        ж<FluentElem> result = box.Add(41);

        Assert.IsTrue(ReferenceEquals(box, result), "the twin must return the receiver's OWN box — Go pointer identity");
        Assert.AreEqual(41, box.DerefOrNull().V, "the primary's mutation must be visible through the caller's box");

        // And the chain composes on the twin path exactly as Go's fluent chains do.
        Assert.IsTrue(ReferenceEquals(box, box.Add(1).Add(1)));
        Assert.AreEqual(43, box.DerefOrNull().V);
    }

    [TestMethod]
    public void PrimaryMutatesTheCallersStorageDirectly()
    {
        // The allocation-free half: no box anywhere, the ref chain writes the local in place.
        FluentElem e = default;

        e.Add(2).Add(3);

        Assert.AreEqual(5, e.V, "the ref-return chain must write the caller's storage with no box in existence");
    }
}
