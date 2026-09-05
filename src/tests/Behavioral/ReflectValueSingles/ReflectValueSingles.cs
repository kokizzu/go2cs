namespace go;

using fmt = fmt_package;
using reflect = reflect_package;
using strings = strings_package;

partial class main_package {

internal static void expectPanic(@string label, @string want, Action f) {
    GoFrame ᒐ = default;
    try {
        defer(() => {
            var r = recover();
            @string msg = fmt.Sprint(r);
            fmt.Printf("%-16s panicked: %v  mentions %q: %v  text: %s\n"u8, label, r != default!, want, strings.Contains(msg, want), msg);
        }, ref ᒐ);
        f();
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string setLen10ˢ = "SetLen(10)"u8;
private static readonly @string setLenˢ = "SetLen"u8;
private static readonly @string setCap10ˢ = "SetCap(10)"u8;
private static readonly @string setCapˢ = "SetCap"u8;
private static readonly @string setLen1ˢ = "SetLen(-1)"u8;
private static readonly @string setCap1ˢ = "SetCap(-1)"u8;
private static readonly @string setCap6Lenˢ = "SetCap(6)<len"u8;
private static readonly object afterSetLen5LenCapˢ = (@string)"after SetLen(5): len, cap ="u8;
private static readonly object afterSetCap6LenCapˢ = (@string)"after SetCap(6): len, cap ="u8;
private static readonly object afterSetCap5LenCapˢ = (@string)"after SetCap(5): len, cap ="u8;
private static readonly object contentsˢ = (@string)"contents"u8;
private static readonly @string setCap4Lenˢ = "SetCap(4)<len"u8;
private static readonly @string setLen6Capˢ = "SetLen(6)>cap"u8;
private static readonly @string arraySetLenˢ = "array SetLen"u8;
private static readonly @string arraySetCapˢ = "array SetCap"u8;
private static readonly object writeThroughTheReCappedˢ = (@string)"write through the re-capped window seen by the original:"u8;

internal static void Main() {
    ref var xs = ref heap<slice<nint>>(out var Ꮡxs);
    xs = new nint[]{1, 2, 3, 4, 5, 6, 7, 8}.slice();
    ref var xa = ref heap<array<nint>>(out var Ꮡxa);
    xa = new nint[]{10, 20, 30, 40, 50, 60, 70, 80}.array();
    ref var vs = ref heap<reflectꓸValue>(out var Ꮡvs);
    vs = reflect.ValueOf(Ꮡxs).Elem();
    var vsʗ1 = vs;
    expectPanic(setLen10ˢ, setLenˢ, () => {
        vsʗ1.SetLen(10);
    });
    var vsʗ2 = vs;
    expectPanic(setCap10ˢ, setCapˢ, () => {
        vsʗ2.SetCap(10);
    });
    var vsʗ3 = vs;
    expectPanic(setLen1ˢ, setLenˢ, () => {
        vsʗ3.SetLen(-1);
    });
    var vsʗ4 = vs;
    expectPanic(setCap1ˢ, setCapˢ, () => {
        vsʗ4.SetCap(-1);
    });
    var vsʗ5 = vs;
    expectPanic(setCap6Lenˢ, setCapˢ, () => {
        vsʗ5.SetCap(6);
    });
    vs.SetLen(5);
    fmt.Println(afterSetLen5LenCapˢ, len(xs), cap(xs));
    vs.SetCap(6);
    fmt.Println(afterSetCap6LenCapˢ, len(xs), cap(xs));
    vs.SetCap(5);
    fmt.Println(afterSetCap5LenCapˢ, len(xs), cap(xs), contentsˢ, xs);
    var vsʗ6 = vs;
    expectPanic(setCap4Lenˢ, setCapˢ, () => {
        vsʗ6.SetCap(4);
    });
    var vsʗ7 = vs;
    expectPanic(setLen6Capˢ, setLenˢ, () => {
        vsʗ7.SetLen(6);
    });
    ref var va = ref heap<reflectꓸValue>(out var Ꮡva);
    va = reflect.ValueOf(Ꮡxa).Elem();
    var vaʗ1 = va;
    expectPanic(arraySetLenˢ, setLenˢ, () => {
        vaʗ1.SetLen(8);
    });
    var vaʗ2 = va;
    expectPanic(arraySetCapˢ, setCapˢ, () => {
        vaʗ2.SetCap(8);
    });
    var backing = xs[..(int)(cap(xs))];
    backing[0] = 99;
    fmt.Println(writeThroughTheReCappedˢ, xs[0] == 99);
}

} // end main_package
