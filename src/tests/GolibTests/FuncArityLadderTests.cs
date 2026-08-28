using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class FuncArityLadderTests
{
    // golib's funcArity.cs continues the BCL delegate family past its 16-parameter ceiling, in
    // namespace `go`, so a Go func type wider than that has a delegate to name at all (Go's own
    // reflect suite drives one: abi_test.go's callArgsManyFloat64 passes a func of 20 parameters
    // returning 19 results, which lowers to a Func<> of 21 type arguments — CS0305 before this).
    //
    // Two things are guarded here, and the second is the load-bearing one:
    //
    //   1. Each rung is wired POSITIONALLY. A hand-written ladder's realistic defect is a
    //      transposed or repeated parameter (`T16 arg16` where `T17 arg17` belongs), which a
    //      plain sum cannot see — sums are order-blind. So the probes weight argument i by i:
    //      feeding 1..20 yields the sum of squares, 2870, and ANY swap of positions i and j moves
    //      it by exactly (i-j)^2 while a drop or duplicate moves it too.
    //
    //   2. The ladder must not COLLIDE with the BCL family. This file imports `System` and `go` at
    //      the same scope, so an arity declared in both would be CS0104 at every use below and the
    //      file would not compile — that is the real guard, and it is structural. The boundary
    //      assertions then state where each half begins, since "it compiled" alone would not say
    //      whether the seam sits where it was meant to.

    private const long SumOfSquaresTo20 = 2870;

    [TestMethod]
    public void TwentyParameterFuncWiresEveryPositionToItsArgument()
    {
        Func<int, int, int, int, int, int, int, int, int, int,
             int, int, int, int, int, int, int, int, int, int, long> weighted =
            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10,
             a11, a12, a13, a14, a15, a16, a17, a18, a19, a20) =>
                a1 * 1L + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8 + a9 * 9 + a10 * 10 +
                a11 * 11 + a12 * 12 + a13 * 13 + a14 * 14 + a15 * 15 + a16 * 16 + a17 * 17 + a18 * 18 + a19 * 19 + a20 * 20;

        long result = weighted(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        Assert.AreEqual(SumOfSquaresTo20, result,
            "every one of the 20 arguments must reach the parameter of the same position");
        Assert.AreEqual("go", typeof(Func<int, int, int, int, int, int, int, int, int, int,
                                         int, int, int, int, int, int, int, int, int, int, long>).Namespace,
            "a 20-parameter Func is golib's rung, not the BCL's");
    }

    [TestMethod]
    public void TwentyParameterActionWiresEveryPositionToItsArgument()
    {
        long observed = 0;
        int calls = 0;

        Action<int, int, int, int, int, int, int, int, int, int,
               int, int, int, int, int, int, int, int, int, int> record =
            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10,
             a11, a12, a13, a14, a15, a16, a17, a18, a19, a20) =>
            {
                observed = a1 * 1L + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8 + a9 * 9 + a10 * 10 +
                           a11 * 11 + a12 * 12 + a13 * 13 + a14 * 14 + a15 * 15 + a16 * 16 + a17 * 17 + a18 * 18 + a19 * 19 + a20 * 20;
                calls++;
            };

        record(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        Assert.AreEqual(1, calls, "the delegate must have been invoked exactly once");
        Assert.AreEqual(SumOfSquaresTo20, observed,
            "every one of the 20 arguments must reach the parameter of the same position");
        Assert.AreEqual("go", typeof(Action<int, int, int, int, int, int, int, int, int, int,
                                           int, int, int, int, int, int, int, int, int, int>).Namespace,
            "a 20-parameter Action is golib's rung, not the BCL's");
    }

    [TestMethod]
    public void TheLadderStartsExactlyWhereTheBclFamilyStops()
    {
        // 16 parameters: the BCL's last rung, which golib must NOT redeclare — doing so would
        // shadow System's corpus-wide (`go` is the nearer scope inside every converted file) and
        // would be ambiguous here, where both namespaces are imported side by side.
        Assert.AreEqual("System", typeof(Func<int, int, int, int, int, int, int, int,
                                             int, int, int, int, int, int, int, int, long>).Namespace,
            "16 parameters must still bind System.Func");
        Assert.AreEqual("System", typeof(Action<int, int, int, int, int, int, int, int,
                                               int, int, int, int, int, int, int, int>).Namespace,
            "16 parameters must still bind System.Action");

        // 17 parameters: the first rung the BCL lacks, so the first golib supplies.
        Assert.AreEqual("go", typeof(Func<int, int, int, int, int, int, int, int, int,
                                         int, int, int, int, int, int, int, int, long>).Namespace,
            "17 parameters must bind golib's Func");
        Assert.AreEqual("go", typeof(Action<int, int, int, int, int, int, int, int, int,
                                           int, int, int, int, int, int, int, int>).Namespace,
            "17 parameters must bind golib's Action");

        // 24 parameters: the top of the ladder as declared. Naming it here is what makes a future
        // change to the ceiling a deliberate edit to this test rather than a silent one.
        Assert.AreEqual("go", typeof(Func<int, int, int, int, int, int, int, int, int, int, int, int,
                                         int, int, int, int, int, int, int, int, int, int, int, int, long>).Namespace,
            "24 parameters must bind golib's Func");
        Assert.AreEqual("go", typeof(Action<int, int, int, int, int, int, int, int, int, int, int, int,
                                           int, int, int, int, int, int, int, int, int, int, int, int>).Namespace,
            "24 parameters must bind golib's Action");
    }
}
