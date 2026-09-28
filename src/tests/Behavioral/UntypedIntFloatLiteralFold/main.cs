namespace go;

using fmt = fmt_package;

partial class main_package {

internal static UntypedInt retainExtraPercent => 10;

internal static UntypedInt baseline => /* 100 << 20 */ 104857600;

internal static UntypedInt pct => 29;

internal static UntypedInt procs => 14;

internal static UntypedFloat capacityPerProc => 1e9;

[GoType("num:int64")] partial struct duration;

internal static int64 advance(duration d) {
    return (int64)d;
}

internal static void Main() {
    fmt.Println((uint64)(1.0D / (retainExtraPercent / 100.0D)));
    fmt.Println((nint)(1.2D * baseline), (int64)(1.5D * baseline), (nint)(0.2D * baseline));
    fmt.Println((uint64)(pct / 100.0D * 100));
    fmt.Println((nint)(unchecked((nint)(14000000000L))));
    uint64 capacity = 14000000000UL;
    fmt.Println(capacity == (uint64)(procs * capacityPerProc), capacity == (uint64)(capacityPerProc * procs));
    int64 want = 1.5D * baseline;
    fmt.Println(want);
    fmt.Println(advance((duration)(28000000000L)));
}

} // end main_package
