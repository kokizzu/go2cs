using System.Runtime.CompilerServices;

// A Go-source frame for the map-growth guards (MapGrowthProfileTests): each probe is the Go function its
// comment names, converted the way the converter emits it.
namespace go;

internal static class mapgrowthprobe_package
{
    // runtime/pprof's growMap: m := make(map[int]int); for i := range 512 { m[i] = i }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void growMap()
    {
        var m = new map<nint, nint>();

        for (nint i = 0; i < 512; i++)
            m[i] = i;
    }

    // m := make(map[int]int, 512); for i := range 512 { m[i] = i }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void growMapHinted()
    {
        var m = new map<nint, nint>(512);

        for (nint i = 0; i < 512; i++)
            m[i] = i;
    }

    // m := make(map[int]int); for i := range 1000 { m[i] = i }: past one table's 1024 slots.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void growMapSplit()
    {
        var m = new map<nint, nint>();

        for (nint i = 0; i < 1000; i++)
            m[i] = i;
    }

    // m := make(map[int]int); for i := range 8 { m[i] = i }; then delete and re-add one key 100 times.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void churnSmallMap()
    {
        var m = new map<nint, nint>();

        for (nint i = 0; i < 8; i++)
            m[i] = i;

        for (int n = 0; n < 100; n++)
        {
            m.Remove(0);
            m[0] = 0;
        }
    }
}
