namespace go;

using @unsafe = unsafe_package;
using System.Runtime.CompilerServices;

partial class main_package {

[MethodImpl(MethodImplOptions.NoInlining)] internal static @unsafe.Pointer ptrOf(ж<int64> Ꮡx) {
    return @unsafe.Pointer.FromPinnedBox(Ꮡx);
}

[MethodImpl(MethodImplOptions.NoInlining)] internal static bool isNil(@unsafe.Pointer p) {
    return p == nil;
}

[MethodImpl(MethodImplOptions.NoInlining)] internal static slice<@unsafe.Pointer> makePtrs(ж<int64> Ꮡx) {
    return new @unsafe.Pointer[]{@unsafe.Pointer.FromPinnedBox(Ꮡx)}.slice();
}

} // end main_package
