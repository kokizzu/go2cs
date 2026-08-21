[assembly: go.GoPositionMap("main.go", "main.cs", "AAoggtaCgg==")]

namespace go;

using fmt = fmt_package;
using static sync_package;
using Δsync = sync_package;

partial class main_package {

internal static bool ready(ж<Δsync.Mutex> Ꮡmu) {
    return Ꮡmu != nil;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object mutexReadyˢ = (@string)"mutex ready:"u8;

internal static void Main() {
    ref var mu = ref heap(new Δsync.Mutex(), out var Ꮡmu);
    fmt.Println(mutexReadyˢ, ready(Ꮡmu));
}

} // end main_package
