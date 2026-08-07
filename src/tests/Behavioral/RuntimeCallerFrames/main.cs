namespace go;

using fmt = fmt_package;
using Δruntime = runtime_package;

partial class main_package {

internal static nint selfLine() {
    var (_, _, line, _) = Δruntime.Caller(0);
    return line;
}

internal static nint callerLine() {
    var (_, _, line, _) = Δruntime.Caller(1);
    return line;
}

internal static @string callerFile() {
    var (_, @file, _, _) = Δruntime.Caller(1);
    return @file;
}

internal static nint grandLine() {
    var (_, _, line, _) = Δruntime.Caller(2);
    return line;
}

internal static nint wrapGrand() {
    return grandLine();
}

internal static (nint, nint) sameSite() {
    return (callerLine(), wrapGrand());
}

internal static nint siteA() {
    return callerLine();
}

internal static nint siteB() {
    return callerLine();
}

internal static bool okAt(nint skip) {
    var (_, _, _, ok) = Δruntime.Caller(skip);
    return ok;
}

internal static bool deepOK() {
    return okAt(2);
}

internal static nint depth() {
    var pc = new slice<uintptr>(256);
    return Δruntime.Callers(0, pc);
}

internal static nint depthPlus1() {
    return depth();
}

internal static nint depthPlus2() {
    return depthPlus1();
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object sameLineAgreementˢ = (@string)"same-line agreement:"u8;
private static readonly object selfLineConstantˢ = (@string)"self line constant:"u8;
private static readonly object selfLineDiffersFromCallˢ = (@string)"self line differs from call site:"u8;
private static readonly object distinctCallSitesˢ = (@string)"distinct call sites:"u8;
private static readonly object sameFileˢ = (@string)"same file:"u8;
private static readonly object fileReportedˢ = (@string)"file reported:"u8;
private static readonly object okAt0ˢ = (@string)"ok at 0:"u8;
private static readonly object okAt1ˢ = (@string)"ok at 1:"u8;
private static readonly object okTwoLevelsUpˢ = (@string)"ok two levels up:"u8;
private static readonly object okPastTheStackˢ = (@string)"ok past the stack:"u8;
private static readonly object callersDepthDeltaˢ = (@string)"callers depth delta:"u8;

internal static void Main() {
    var (x, y) = sameSite();
    fmt.Println(sameLineAgreementˢ, x == y);
    fmt.Println(selfLineConstantˢ, selfLine() == selfLine());
    fmt.Println(selfLineDiffersFromCallˢ, selfLine() != callerLine());
    fmt.Println(distinctCallSitesˢ, siteA() != siteB());
    var (_, here, _, _) = Δruntime.Caller(0);
    fmt.Println(sameFileˢ, here == callerFile());
    fmt.Println(fileReportedˢ, len(here) > 0);
    fmt.Println(okAt0ˢ, okAt(0));
    fmt.Println(okAt1ˢ, okAt(1));
    fmt.Println(okTwoLevelsUpˢ, deepOK());
    fmt.Println(okPastTheStackˢ, okAt(1000));
    fmt.Println(callersDepthDeltaˢ, depthPlus2() - depth());
}

} // end main_package
