using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static go.builtin;
using static go.runtime_package;

namespace go;

internal static partial class panicframesprobe_package
{
    // plainPanic's shape, reporting each frame's FILE as well: the spliced runtime.gopanic record is an
    // interned root frame, and its file is rooted by the same rule as the GoStackRoot frame's
    // (RootFramePathTests).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<(string function, string file)> plainPanicFiles()
    {
        List<(string function, string file)> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                slice<uintptr> pcs = new(64);
                pcs = pcs[..(int)Callers(0, pcs)];
                var iterator = CallersFrames(pcs);
                while (true) {
                    var (frame, more) = iterator.Next();
                    if (frame.PC == 0 && !more)
                        break;
                    got.Add(((string)frame.Function, (string)frame.File));
                    if (!more)
                        break;
                }
            }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }
}
