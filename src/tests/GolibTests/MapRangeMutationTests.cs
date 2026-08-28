using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

// GolibTests' csproj carries the numeric Go aliases but not `any`, which only converted code needs.
// Declared here rather than added to the shared project so this file pays for its own vocabulary.
using any = System.Object;

namespace GolibTests;

[TestClass]
public class MapRangeMutationTests
{
    // Go's RANGE-OVER-MAP contract, which is not Dictionary's enumeration contract. The spec:
    //
    //     "If a map entry that has not yet been reached is removed during iteration, the
    //      corresponding iteration value will not be produced. If a map entry is created during
    //      iteration, that entry may be produced during the iteration or may be skipped."
    //
    // Dictionary<TKey, TValue>'s enumerator throws InvalidOperationException on a structural ADD,
    // so map<K,V> implements the contract itself by walking a snapshot of the keys and re-reading
    // each value at the moment it is visited (map.cs, enumerateStore).
    //
    // The Go-EXPRESSIBLE half of this is guarded from the Go side by the MapMutateDuringRange
    // behavioral test, which is where the emitted `foreach` is proven against `go run`. What is
    // guarded HERE is the part that behavioral test cannot reach directly: the NIL-KEY entry's
    // participation in a mutating range. The nil-key check used to sit at the GetEnumerator call
    // site and now lives inside the iterator, so a regression there would either drop the entry or
    // -- worse -- yield a phantom (default!, default) pair for every map that has no nil key at all.

    [TestMethod]
    public void InsertingDuringRangeCompletesInsteadOfThrowing()
    {
        map<@string, nint> m = new() { ["a"u8] = 1, ["b"u8] = 2, ["c"u8] = 3 };

        int visited = 0;

        foreach ((@string key, nint value) in m)
        {
            // Every inserted key is two runes long, so producing it (which Go leaves free) cannot
            // re-enter this branch and the loop terminates either way.
            if (len(key) != 1)
                continue;

            visited++;
            m[key + "!"u8] = value * 10;
        }

        Assert.AreEqual(3, visited, "every pre-existing entry is produced exactly once");
        Assert.AreEqual(6, (int)len(m), "the inserts landed in the map itself");
        Assert.AreEqual(10, (int)m["a!"u8]);
        Assert.AreEqual(30, (int)m["c!"u8]);
    }

    [TestMethod]
    public void RemovingDuringRangeDrainsTheMap()
    {
        map<@string, nint> m = new() { ["p"u8] = 1, ["q"u8] = 2, ["r"u8] = 3 };

        foreach ((@string key, nint _) in m)
            delete(m, key);

        Assert.AreEqual(0, (int)len(m));
    }

    [TestMethod]
    public void OverwritingDuringRangeProducesTheCurrentValue()
    {
        map<@string, nint> m = new() { ["x"u8] = 1 };

        // The snapshot carries KEYS, never values, so a value written before the entry is visited
        // is the one produced -- which is what Go reads out of the bucket on arrival.
        m["x"u8] = 99;

        List<nint> produced = [];

        foreach ((@string _, nint value) in m)
            produced.Add(value);

        CollectionAssert.AreEqual(new List<nint> { 99 }, produced);
    }

    [TestMethod]
    public void RangeOverNilMapProducesNothing()
    {
        map<@string, nint> m = default;

        Assert.IsTrue(m.IsNil);
        Assert.AreEqual(0, rangeCount(m));
    }

    [TestMethod]
    public void MapWithoutANilKeyNeverProducesAPhantomEntry()
    {
        // A reference-typed key, so the nil-key SLOT exists as a possibility -- and must stay
        // unvisited while nothing has been stored under it.
        map<any, nint> m = new() { [(@string)"k"u8] = 1 };

        List<any> keys = m.Select(static entry => entry.Key).ToList();

        Assert.AreEqual(1, keys.Count);
        Assert.IsFalse(keys.Any(static k => k is null), "no phantom nil-key entry");
    }

    [TestMethod]
    public void TheNilKeyEntryIsVisitedByARangeThatAlsoInserts()
    {
        map<any, nint> m = new() { [(@string)"k"u8] = 1 };

        m[default!] = 7;

        Assert.AreEqual(2, (int)len(m));

        bool sawNilKey = false;
        int visited = 0;

        foreach ((any key, nint value) in m)
        {
            visited++;

            if (key is null)
            {
                sawNilKey = true;
                Assert.AreEqual(7, (int)value);
            }

            // The insert that made Dictionary's own enumerator throw. The nil entry is produced
            // ahead of the buckets, so this also proves the iterator survives past that first yield.
            m[(@string)$"added{visited}"] = value;
        }

        Assert.IsTrue(sawNilKey, "range visits the nil key like any other key");
        Assert.AreEqual(2, visited, "both pre-existing entries produced, neither insert re-entered");
    }

    // A NaN key is equal to NOTHING, itself included (GoEqualityComparer gives the float
    // representations Go's `==` rather than the BCL's NaN-finds-itself rule), so no lookup can
    // ever match one. That makes it the single shape where re-reading a value on arrival is the
    // wrong instrument: the lookup always misses, and an unguarded miss silently drops every NaN
    // entry from every range. encoding/json's TestMarshalTextFloatMap reads it out as a panic --
    // mapEncoder sizes its slice from len() and fills it from MapRange, so a short range leaves
    // zero reflect.Values behind. These pin the disambiguation.

    private static double nan()
    {
        // Built from runtime zeros so the C# compiler cannot fold it to a literal.
        double zero = 0.0D;
        return zero / zero;
    }

    [TestMethod]
    public void NaNKeysAreStoredSeparatelyAndAreUnretrievable()
    {
        map<float64, nint> m = new();

        m[nan()] = 1;
        m[nan()] = 1;

        Assert.AreEqual(2, (int)len(m), "NaN is equal to nothing, so each store adds an entry");

        var (_, found) = m[nan(), ꟷ];
        Assert.IsFalse(found, "no lookup can ever match a NaN key");
    }

    [TestMethod]
    public void RangeProducesEveryNaNKeyedEntry()
    {
        map<float64, nint> m = new();

        m[nan()] = 1;
        m[nan()] = 1;

        int visited = 0;
        nint sum = 0;

        foreach ((float64 key, nint value) in m)
        {
            visited++;
            sum += value;
            Assert.IsTrue(double.IsNaN(key));
        }

        Assert.AreEqual(2, visited, "a range must produce entries its own lookup cannot find");
        Assert.AreEqual(2, (int)sum);
    }

    [TestMethod]
    public void DeleteCannotRemoveANaNKeyedEntry()
    {
        map<float64, nint> m = new();

        m[nan()] = 1;
        m[nan()] = 1;

        delete(m, nan());

        Assert.AreEqual(2, (int)len(m), "delete matches nothing, so both entries survive");
        Assert.AreEqual(2, rangeCount(m), "and the range still produces both");
    }

    // Counts by actually RANGING. Enumerable.Count() would not do: it short-circuits to
    // ICollection<T>.Count and never touches the enumerator, so it reports the stored count
    // however badly the range is broken -- measured, by watching it pass against the very
    // regression these tests exist to catch.
    private static int rangeCount<TKey, TValue>(map<TKey, TValue> m) where TKey : notnull
    {
        int n = 0;

        foreach ((TKey _, TValue _) in m)
            n++;

        return n;
    }

    [TestMethod]
    public void NaNKeysSurviveARangeThatAlsoMutates()
    {
        map<float64, nint> m = new() { [1.5] = 10 };

        m[nan()] = 1;
        m[nan()] = 1;

        // The visit COUNT is left unasserted on purpose: 2.5 is retrievable, so Go's "created
        // during iteration may be produced or may be skipped" genuinely applies to it.
        foreach ((float64 key, nint value) in m)
        {
            if (key == 1.5)
                m[2.5] = value * 2;
        }

        Assert.AreEqual(4, (int)len(m));
        Assert.AreEqual(20, (int)m[2.5]);

        int nans = 0;

        foreach ((float64 key, nint _) in m)
        {
            if (double.IsNaN(key))
                nans++;
        }

        Assert.AreEqual(2, nans, "both NaN entries still produced after the mutating range");
    }

    [TestMethod]
    public void ClearRemovesNaNKeyedEntriesFromAnInFlightRange()
    {
        map<float64, nint> m = new();

        m[nan()] = 1;
        m[nan()] = 1;
        m[1.5] = 10;

        int visited = 0;

        foreach ((float64 _, nint _) in m)
        {
            visited++;
            // clear is the ONE operation that can take an unretrievable entry out, so anything
            // not yet reached must stop being produced -- the guarantee Go gives for removal.
            clear(m);
        }

        Assert.AreEqual(1, visited, "clear stops the range dead, NaN entries included");
        Assert.AreEqual(0, (int)len(m));
    }

    [TestMethod]
    public void DeletingTheNilKeyDuringRangeIsNotProducedLater()
    {
        map<any, nint> m = new() { [(@string)"k"u8] = 1 };

        m[default!] = 7;

        // The nil entry is produced FIRST, so deleting it from the body cannot suppress it here;
        // what this pins is that the delete is honored and the range still terminates cleanly.
        int visited = 0;

        foreach ((any key, nint _) in m)
        {
            visited++;
            delete(m, key);
        }

        Assert.AreEqual(2, visited);
        Assert.AreEqual(0, (int)len(m), "both entries, nil key included, were removed");
    }
}
