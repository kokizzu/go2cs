namespace go;

using fmt = fmt_package;
using Δruntime = runtime_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string helperDeferRanˢ = "helper defer ran"u8;
private static readonly @string helperCallingRuntimeˢ = "helper calling runtime.Goexit"u8;
private static readonly @string helperResumedUnreachableˢ = "helper resumed (UNREACHABLE)"u8;

internal static void exitFromHelper(channel/*<-*/<@string> log) => func((defer, recover) => {
    var logʗ1 = log;
    defer(() => {
        logʗ1.ᐸꟷ(helperDeferRanˢ);
    });
    log.ᐸꟷ(helperCallingRuntimeˢ);
    Δruntime.Goexit();
    log.ᐸꟷ(helperResumedUnreachableˢ);
});

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string goroutineDefer2Ranˢ = "goroutine defer 2 ran"u8;
private static readonly @string recoverDuringGoexitˢ = "recover() during Goexit returned nil"u8;
private static readonly @string recoverDuringGoexitˢ2 = "recover() during Goexit returned a value (WRONG)"u8;
private static readonly @string goroutineDefer1Ranˢ = "goroutine defer 1 ran"u8;
private static readonly @string goroutineResumedˢ = "goroutine resumed (UNREACHABLE)"u8;
private static readonly @string aLaterGoroutineStillRunsˢ = "a later goroutine still runs"u8;
private static readonly object mainContinuesPastTheˢ = (@string)"main continues past the exited goroutine"u8;

internal static void Main() {
    var log = new channel<@string>(16);
    var done = new channel<EmptyStruct>(0);
    var doneʗ1 = done;
    var logʗ1 = log;
    goǃ(() => func((defer, recover) => {
        deferǃ(ᴛ1 => close(ᴛ1), doneʗ1, defer);
        var logʗ2 = logʗ1;
        defer(() => {
            logʗ2.ᐸꟷ(goroutineDefer2Ranˢ);
        });
        var logʗ3 = logʗ1;
        defer(() => {
            if (recover() == default!){
                logʗ3.ᐸꟷ(recoverDuringGoexitˢ);
            } else {
                logʗ3.ᐸꟷ(recoverDuringGoexitˢ2);
            }
            logʗ3.ᐸꟷ(goroutineDefer1Ranˢ);
        });
        exitFromHelper(logʗ1);
        logʗ1.ᐸꟷ(goroutineResumedˢ);
    }));
    ᐸꟷ(done);
    close(log);
    foreach (var line in log) {
        fmt.Println(line);
    }
    var after = new channel<@string>(0);
    var afterʗ1 = after;
    goǃ(() => {
        afterʗ1.ᐸꟷ(aLaterGoroutineStillRunsˢ);
    });
    fmt.Println(ᐸꟷ(after));
    fmt.Println(mainContinuesPastTheˢ);
}

} // end main_package
