namespace go;

using @unsafe = unsafe_package;

partial class main_package {

internal static @unsafe.Pointer ptrOf(ж<int64> Ꮡx) {
    return @unsafe.Pointer.FromPinnedBox(Ꮡx);
}

internal static bool isNil(@unsafe.Pointer p) {
    return p == nil;
}

internal static slice<@unsafe.Pointer> makePtrs(ж<int64> Ꮡx) {
    return new @unsafe.Pointer[]{@unsafe.Pointer.FromPinnedBox(Ꮡx)}.slice();
}

} // end main_package
