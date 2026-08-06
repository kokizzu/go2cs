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

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object whileLockedˢ = (@string)"while locked:"u8;
private static readonly object afterUnlockˢ = (@string)"after unlock:"u8;
private static readonly object runGoidˢ = (@string)"runGoid:"u8;
private static readonly object localDuringˢ = (@string)"local during:"u8;

internal static void Main() {
    fmt.Println(whileLockedˢ, lockUnlock(7));
    fmt.Println(afterUnlockˢ, Ꮡlocked.Load());
    fmt.Println(runGoidˢ, ᏑrunGoid.Load());
    fmt.Println(localDuringˢ, localAtomicDefer());
}

} // end main_package
