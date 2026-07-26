namespace go;

using fmt = fmt_package;
using atomic = sync.atomic_package;
using sync;

partial class main_package {

internal static ж<atomic.Int32> Ꮡlocked = new(default(atomic.Int32));
internal static ref atomic.Int32 locked => ref Ꮡlocked.Value;
internal static ж<atomic.Uint64> ᏑrunGoid = new(default(atomic.Uint64));
internal static ref atomic.Uint64 runGoid => ref ᏑrunGoid.Value;

internal static int32 lockUnlock(uint64 id) => func((defer, recover) => {
    while (!Ꮡlocked.CompareAndSwap(0, 1)) {
    }
    deferǃ(Ꮡlocked.Store, (int32)(0), defer);
    ᏑrunGoid.Store(id);
    deferǃ(ᏑrunGoid.Store, (uint64)(0), defer);
    return Ꮡlocked.Load();
});

internal static int32 localAtomicDefer() => func((defer, recover) => {
    ref var n = ref heap(new atomic.Int32(), out var Ꮡn);
    Ꮡn.Store(41);
    deferǃ(Ꮡn.Store, (int32)(0), defer);
    return Ꮡn.Load();
});

internal static void Main() {
    fmt.Println((@string)"while locked:"u8, lockUnlock(7));
    fmt.Println((@string)"after unlock:"u8, Ꮡlocked.Load());
    fmt.Println((@string)"runGoid:"u8, ᏑrunGoid.Load());
    fmt.Println((@string)"local during:"u8, localAtomicDefer());
}

} // end main_package
