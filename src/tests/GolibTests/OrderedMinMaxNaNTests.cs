using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// Go's <c>min</c>/<c>max</c> built-ins PROPAGATE NaN: the spec states that if any argument is a NaN
/// the result is a NaN. golib's operator-bound pair answered <c>x &lt; y ? x : y</c>, and C#'s
/// comparison operators are false for every comparison involving a NaN — so a NaN on the LEFT was
/// silently discarded and the other operand came back.
/// </summary>
/// <remarks>
/// Measured by slices' <c>TestMinMaxNaNs</c>, which replaces each element of a float64 slice with NaN
/// in turn and requires both <c>Min</c> and <c>Max</c> to propagate it — <c>slices.Min</c>/<c>Max</c>
/// reduce with these exact overloads. Guarded here rather than only there because all FOUR overloads
/// carry the rule (the two-argument operator form and the params <see cref="IComparable{T}"/> form,
/// for both min and max) and only one of them was reachable from that package.
/// </remarks>
[TestClass]
public class OrderedMinMaxNaNTests
{
    [TestMethod]
    public void TwoArgumentMinAndMaxPropagateNaNFromEitherSide()
    {
        // The defect: with the NaN on the left, both answered the other operand.
        Assert.IsTrue(double.IsNaN(min(double.NaN, 1.0)), "min(NaN, y) must be NaN");
        Assert.IsTrue(double.IsNaN(max(double.NaN, 1.0)), "max(NaN, y) must be NaN");

        // The side that already worked, pinned so a fix cannot trade one for the other.
        Assert.IsTrue(double.IsNaN(min(1.0, double.NaN)), "min(x, NaN) must be NaN");
        Assert.IsTrue(double.IsNaN(max(1.0, double.NaN)), "max(x, NaN) must be NaN");

        Assert.IsTrue(double.IsNaN(min(double.NaN, double.NaN)));
        Assert.IsTrue(double.IsNaN(max(double.NaN, double.NaN)));
    }

    [TestMethod]
    public void TwoArgumentMinAndMaxAreUnchangedForOrdinaryFloats()
    {
        Assert.AreEqual(-400.4, min(-400.4, 999.9));
        Assert.AreEqual(-400.4, min(999.9, -400.4));
        Assert.AreEqual(999.9, max(-400.4, 999.9));
        Assert.AreEqual(999.9, max(999.9, -400.4));

        Assert.AreEqual(double.NegativeInfinity, min(double.NegativeInfinity, 0.0), "infinity is ordered, not a NaN");
        Assert.AreEqual(double.PositiveInfinity, max(double.PositiveInfinity, 0.0));
    }

    [TestMethod]
    public void Float32CarriesTheSameRule()
    {
        Assert.IsTrue(float.IsNaN(min(float.NaN, 1.0f)));
        Assert.IsTrue(float.IsNaN(max(float.NaN, 1.0f)));
        Assert.AreEqual(1.0f, min(1.0f, 2.0f));
        Assert.AreEqual(2.0f, max(1.0f, 2.0f));
    }

    [TestMethod]
    public void IntegersAreUnaffected()
    {
        // The NaN arm is gated on a per-T constant precisely so no integer instantiation pays for
        // it; this holds the ANSWERS, which is what the gate must not change.
        Assert.AreEqual(3, min(3, 9));
        Assert.AreEqual(9, max(3, 9));
        Assert.AreEqual((nint)(-1), min((nint)(-1), (nint)0));
        Assert.AreEqual((nint)0, max((nint)(-1), (nint)0));
        Assert.AreEqual(1u, min(1u, 2u));
        Assert.AreEqual(2L, max(1L, 2L));
    }

    [TestMethod]
    public void ParamsOverloadsPropagateNaNFromAnyPosition()
    {
        // The three-or-more-argument form binds the IComparable<T> overload, whose total order sorts
        // NaN BELOW everything — which is Go's answer for min and the OPPOSITE of Go's answer for
        // max. The doc comment used to claim the total order matched Go for both.
        Assert.IsTrue(double.IsNaN(min(double.NaN, 1.0, 2.0)), "NaN first");
        Assert.IsTrue(double.IsNaN(min(1.0, double.NaN, 2.0)), "NaN in the middle");
        Assert.IsTrue(double.IsNaN(min(1.0, 2.0, double.NaN)), "NaN last");

        Assert.IsTrue(double.IsNaN(max(double.NaN, 1.0, 2.0)), "NaN first");
        Assert.IsTrue(double.IsNaN(max(1.0, double.NaN, 2.0)), "NaN in the middle");
        Assert.IsTrue(double.IsNaN(max(1.0, 2.0, double.NaN)), "NaN last");
    }

    [TestMethod]
    public void ParamsOverloadsAreUnchangedForOrdinaryValues()
    {
        Assert.AreEqual(-400.4, min(1.0, 999.9, 3.14, -400.4, -5.14));
        Assert.AreEqual(999.9, max(1.0, 999.9, 3.14, -400.4, -5.14));
        Assert.AreEqual(1, min(3, 9, 1, 7));
        Assert.AreEqual(9, max(3, 9, 1, 7));
    }

    // A NAMED Go float — `type myFloat float64` — reaches min/max as the generated single-field
    // wrapper, not as the primitive. Go's cmp.Ordered admits it (`~float64`), so the NaN rule is its
    // rule too. This is the shape the classification walks a field to recognize.
    private readonly struct NamedFloat64(double value) :
        IComparisonOperators<NamedFloat64, NamedFloat64, bool>,
        IComparable<NamedFloat64>
    {
        private readonly double m_value = value;

        public int CompareTo(NamedFloat64 other) => m_value.CompareTo(other.m_value);

        public bool Equals(NamedFloat64 other) => m_value == other.m_value;

        public override bool Equals(object? obj) => obj is NamedFloat64 other && Equals(other);

        public override int GetHashCode() => m_value.GetHashCode();

        public bool IsNaN => double.IsNaN(m_value);

        public static bool operator <(NamedFloat64 left, NamedFloat64 right) => left.m_value < right.m_value;

        public static bool operator <=(NamedFloat64 left, NamedFloat64 right) => left.m_value <= right.m_value;

        public static bool operator >(NamedFloat64 left, NamedFloat64 right) => left.m_value > right.m_value;

        public static bool operator >=(NamedFloat64 left, NamedFloat64 right) => left.m_value >= right.m_value;

        public static bool operator ==(NamedFloat64 left, NamedFloat64 right) => left.Equals(right);

        public static bool operator !=(NamedFloat64 left, NamedFloat64 right) => !(left == right);
    }

    [TestMethod]
    public void ANamedFloatWrapperCarriesTheSameRule()
    {
        NamedFloat64 nan = new(double.NaN);
        NamedFloat64 one = new(1.0);
        NamedFloat64 two = new(2.0);

        Assert.IsTrue(min(nan, one).IsNaN, "min(NaN, y) on a named float must be NaN");
        Assert.IsTrue(max(nan, one).IsNaN, "max(NaN, y) on a named float must be NaN");
        Assert.IsTrue(min(one, nan).IsNaN);
        Assert.IsTrue(max(one, nan).IsNaN);

        Assert.AreEqual(one, min(one, two));
        Assert.AreEqual(two, max(one, two));
    }
}
