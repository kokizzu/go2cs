namespace go;

using fmt = fmt_package;
using @unsafe = unsafe_package;

partial class main_package {

[GoType("num:uintptr")] partial struct Handle;

internal static uintptr addrOfNamed(ж<Handle> Ꮡh) {
    return (uintptr)new @unsafe.Pointer(Ꮡh);
}

internal static uintptr addrOfPtrPtr(ж<ж<uint16>> Ꮡpp) {
    return (uintptr)new @unsafe.Pointer(Ꮡpp);
}

internal static uintptr addrOfBasic(ж<uint16> Ꮡb) {
    return (uintptr)new @unsafe.Pointer(Ꮡb);
}

internal static (uintptr, Handle) liveAlias(ж<Handle> Ꮡh) {
    ref var h = ref Ꮡh.Value;

    h = h + 7;
    return ((uintptr)new @unsafe.Pointer(Ꮡh), h);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object namedNilˢ = (@string)"named nil  :"u8;
private static readonly object ptrptrNilˢ = (@string)"ptrptr nil :"u8;
private static readonly object basicNilˢ = (@string)"basic nil  :"u8;
private static readonly object namedNonnilNonzeroˢ = (@string)"named nonnil nonzero :"u8;
private static readonly object ptrptrNonnilNonzeroˢ = (@string)"ptrptr nonnil nonzero:"u8;
private static readonly object basicNonnilNonzeroˢ = (@string)"basic nonnil nonzero :"u8;
private static readonly object liveAliasNonzeroˢ = (@string)"live alias nonzero:"u8;
private static readonly object valueˢ = (@string)"value:"u8;
private static readonly object readbackˢ = (@string)"readback:"u8;

internal static void Main() {
    fmt.Println(namedNilˢ, addrOfNamed(nil));
    fmt.Println(ptrptrNilˢ, addrOfPtrPtr(nil));
    fmt.Println(basicNilˢ, addrOfBasic(nil));
    ref var h = ref heap(new Handle(), out var Ꮡh);
    h = 3;
    fmt.Println(namedNonnilNonzeroˢ, addrOfNamed(Ꮡh) != 0);
    ref var u = ref heap(new uint16(), out var Ꮡu);
    u = 9;
    ref var pu = ref heap<ж<uint16>>(out var Ꮡpu);
    pu = Ꮡu;
    fmt.Println(ptrptrNonnilNonzeroˢ, addrOfPtrPtr(Ꮡpu) != 0);
    fmt.Println(basicNonnilNonzeroˢ, addrOfBasic(Ꮡu) != 0);
    var (addr, val) = liveAlias(Ꮡh);
    fmt.Println(liveAliasNonzeroˢ, addr != 0, valueˢ, val, readbackˢ, h);
}

} // end main_package
