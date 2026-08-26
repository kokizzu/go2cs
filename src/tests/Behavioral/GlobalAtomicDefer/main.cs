namespace go;

using fmt = fmt_package;
using atomic = sync.atomic_package;
using sync;

partial class main_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyncꓸatomic() {
    builtin.initPackage(typeof(sync.atomic_package));
}

internal static ж<atomic.Int32> Ꮡlocked = new(default(atomic.Int32));
internal static ref atomic.Int32 locked => ref Ꮡlocked.Value;
internal static ж<atomic.Uint64> ᏑrunGoid = new(default(atomic.Uint64));
internal static ref atomic.Uint64 runGoid => ref ᏑrunGoid.Value;

internal static int32 lockUnlock(uint64 id) {
    GoFrame ᒐ = default;
    try {
        while (!Ꮡlocked.CompareAndSwap(0, 1)) {
        }
        defer(Ꮡlocked.Store, (int32)(0), ref ᒐ);
        ᏑrunGoid.Store(id);
        defer(ᏑrunGoid.Store, (uint64)(0), ref ᒐ);
        return Ꮡlocked.Load();
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

internal static int32 localAtomicDefer() {
    GoFrame ᒐ = default;
    try {
        ref var n = ref heap(new atomic.Int32(), out var Ꮡn);
        Ꮡn.Store(41);
        defer(Ꮡn.Store, (int32)(0), ref ᒐ);
        return Ꮡn.Load();
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

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
