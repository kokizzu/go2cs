// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using context = context_package;
using Δsync = sync_package;
using testing = testing_package;
using time = time_package;
using static go.context_internal_test_package;

partial class context_test_package {

// afterFuncContext is a context that's not one of the types
// defined in context.go, that supports registering AfterFuncs.
[GoType] partial struct afterFuncContext {
    internal Δsync.Mutex mu;
    internal map<ж<byte>, Action> afterFuncs;
    internal channel<EmptyStruct> done;
    internal error err;
}

internal static context.Context newAfterFuncContext() {
    return new context_test_package.afterFuncContextжContext(Ꮡ(new afterFuncContext(nil)));
}

[GoRecv] internal static (time.Time, bool) Deadline(this ref afterFuncContext c) {
    return (new time.Time(nil), false);
}

internal static /*<-*/channel<EmptyStruct> Done(this ж<afterFuncContext> Ꮡc) => func((defer, recover) => {
    ref var c = ref Ꮡc.Value;

    Ꮡc.of(afterFuncContext.Ꮡmu).Lock();
    defer(Ꮡc.of(afterFuncContext.Ꮡmu).Unlock);
    if (c.done == default!) {
        c.done = new channel<EmptyStruct>(0);
    }
    return c.done;
});

internal static error Err(this ж<afterFuncContext> Ꮡc) => func((defer, recover) => {
    ref var c = ref Ꮡc.Value;

    Ꮡc.of(afterFuncContext.Ꮡmu).Lock();
    defer(Ꮡc.of(afterFuncContext.Ꮡmu).Unlock);
    return c.err;
});

[GoRecv] internal static any Value(this ref afterFuncContext c, any key) {
    return default!;
}

internal static Func<bool> ΔAfterFunc(this ж<afterFuncContext> Ꮡc, Action f) => func<Func<bool>>((defer, recover) => {
    ref var c = ref Ꮡc.Value;

    Ꮡc.of(afterFuncContext.Ꮡmu).Lock();
    defer(Ꮡc.of(afterFuncContext.Ꮡmu).Unlock);
    var k = @new<byte>();
    if (c.afterFuncs == default!) {
        c.afterFuncs = new map<ж<byte>, Action>();
    }
    c.afterFuncs[k] = f;
    var kʗ1 = k;
    return () => func<bool>((defer, recover) => {
        Ꮡc.of(afterFuncContext.Ꮡmu).Lock();
        defer(Ꮡc.of(afterFuncContext.Ꮡmu).Unlock);
        var (_, ok) = Ꮡc.Value.afterFuncs[kʗ1, ꟷ];
        delete(Ꮡc.Value.afterFuncs, kʗ1);
        return ok;
    });
});

internal static void cancel(this ж<afterFuncContext> Ꮡc, error err) => func((defer, recover) => {
    ref var c = ref Ꮡc.Value;

    Ꮡc.of(afterFuncContext.Ꮡmu).Lock();
    defer(Ꮡc.of(afterFuncContext.Ꮡmu).Unlock);
    if (c.err != default!) {
        return;
    }
    c.err = err;
    foreach (var (_, f) in c.afterFuncs) {
        var fʗ1 = f;
        goǃ(fʗ1);
    }
    c.afterFuncs = default!;
});

public static void TestCustomContextAfterFuncCancel(ж<testing.T> Ꮡt) => func((defer, recover) => {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var (ctx1, cancel) = context.WithCancel(new context_test_package.afterFuncContextжContext(ctx0));
    var cancelʗ1 = cancel;
    defer(() => cancelʗ1());
    ctx0.cancel(context.Canceled);
    ᐸꟷ(ctx1.Done());
});

public static void TestCustomContextAfterFuncTimeout(ж<testing.T> Ꮡt) => func((defer, recover) => {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var (ctx1, cancel) = context.WithTimeout(new context_test_package.afterFuncContextжContext(ctx0), veryLongDuration);
    var cancelʗ1 = cancel;
    defer(() => cancelʗ1());
    ctx0.cancel(context.Canceled);
    ᐸꟷ(ctx1.Done());
});

public static void TestCustomContextAfterFuncAfterFunc(ж<testing.T> Ꮡt) => func((defer, recover) => {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var donec = new channel<EmptyStruct>(0);
    var donecʗ1 = donec;
    var stop = context.AfterFunc(new context_test_package.afterFuncContextжContext(ctx0), () => {
        close(donecʗ1);
    });
    var stopʗ1 = stop;
    defer(() => stopʗ1());
    ctx0.cancel(context.Canceled);
    ᐸꟷ(donec);
});

public static void TestCustomContextAfterFuncUnregisterCancel(ж<testing.T> Ꮡt) {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var (_, cancel1) = context.WithCancel(new context_test_package.afterFuncContextжContext(ctx0));
    var (_, cancel2) = context.WithCancel(new context_test_package.afterFuncContextжContext(ctx0));
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 2; if (got != want) {
            Ꮡt.Errorf("after WithCancel(ctx0): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
    cancel1();
    cancel2();
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 0; if (got != want) {
            Ꮡt.Errorf("after canceling WithCancel(ctx0): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
}

public static void TestCustomContextAfterFuncUnregisterTimeout(ж<testing.T> Ꮡt) {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var (_, cancel) = context.WithTimeout(new context_test_package.afterFuncContextжContext(ctx0), veryLongDuration);
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 1; if (got != want) {
            Ꮡt.Errorf("after WithTimeout(ctx0, d): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
    cancel();
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 0; if (got != want) {
            Ꮡt.Errorf("after canceling WithTimeout(ctx0, d): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
}

public static void TestCustomContextAfterFuncUnregisterAfterFunc(ж<testing.T> Ꮡt) {
    var ctx0 = Ꮡ(new afterFuncContext(nil));
    var stop = context.AfterFunc(new context_test_package.afterFuncContextжContext(ctx0), () => {
    });
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 1; if (got != want) {
            Ꮡt.Errorf("after AfterFunc(ctx0, f): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
    stop();
    {
        nint got = len((~ctx0).afterFuncs);
        nint want = 0; if (got != want) {
            Ꮡt.Errorf("after stopping AfterFunc(ctx0, f): ctx0 has %v afterFuncs, want %v"u8, got, want);
        }
    }
}

} // end context_test_package
