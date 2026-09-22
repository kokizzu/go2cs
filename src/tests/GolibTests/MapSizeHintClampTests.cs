using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

// A map size hint is ADVICE in Go, never an error: runtime.makemap clamps `hint < 0` to 0
// (map_swiss.go:72) and makemap64 clamps a hint that does not fit an int to 0 (:33). Measured:
// internal/runtime/maps' TestTableGroupCount/makemap/n=-1 (and n=-1073741824, and the makemap64 pair)
// reached Dictionary's capacity argument and died ArgumentOutOfRangeException where Go passes.
[TestClass]
public class MapSizeHintClampTests
{
    [TestMethod]
    public void ANegativeHintMakesAnEmptyUsableMap_AsGosMakemapClampsIt()
    {
        foreach (nint hint in new nint[] { -1, -1073741824 })
        {
            map<nint, nint> m = make<map<nint, nint>>(hint);
            Assert.AreEqual((nint)0, len(m), $"hint {hint}: empty");
            m[1] = 2;
            Assert.AreEqual((nint)2, m[1], $"hint {hint}: and usable");
        }
    }

    [TestMethod]
    public void AHintBeyondIntRangeMakesAnEmptyMap_AsMakemap64ClampsIt()
    {
        map<nint, nint> m = make<map<nint, nint>>((nint)int.MaxValue + 1);
        Assert.AreEqual((nint)0, len(m));
    }
}
