// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.runtime;

using bytes = bytes_package;
using flag = flag_package;
using os = os_package;
using static go.runtime.trace_package;
using testing = testing_package;
using time = time_package;
using fs = io.fs_package;
using io = io_package;

partial class trace_test_package {

internal static ж<bool> saveTraces = flag.Bool("savetraces"u8, false, "save traces collected by tests"u8);

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object skippingBecauseTestTraceˢ = (@string)"skipping because -test.trace is set"u8;
private static readonly @string testTraceStartStopˢ = "TestTraceStartStop"u8;

public static void TestTraceStartStop(ж<testing.T> Ꮡt) {
    if (IsEnabled()) {
        Ꮡt.Skip(skippingBecauseTestTraceˢ);
    }
    var buf = @new<bytes.Buffer>();
    {
        var err = Start(new bytes_BufferжWriter(buf)); if (err != default!) {
            Ꮡt.Fatalf("failed to start tracing: %v"u8, err);
        }
    }
    Stop();
    nint size = buf.Len();
    if (size == 0) {
        Ꮡt.Fatalf("trace is empty"u8);
    }
    time.Sleep(100 * time.Millisecond);
    if (size != buf.Len()) {
        Ꮡt.Fatalf("trace writes after stop: %v -> %v"u8, size, buf.Len());
    }
    saveTrace(Ꮡt, buf, testTraceStartStopˢ);
}

public static void TestTraceDoubleStart(ж<testing.T> Ꮡt) {
    if (IsEnabled()) {
        Ꮡt.Skip(skippingBecauseTestTraceˢ);
    }
    Stop();
    var buf = @new<bytes.Buffer>();
    {
        var err = Start(new bytes_BufferжWriter(buf)); if (err != default!) {
            Ꮡt.Fatalf("failed to start tracing: %v"u8, err);
        }
    }
    {
        var err = Start(new bytes_BufferжWriter(buf)); if (err == default!) {
            Ꮡt.Fatalf("succeed to start tracing second time"u8);
        }
    }
    Stop();
    Stop();
}

internal static void saveTrace(ж<testing.T> Ꮡt, ж<bytes.Buffer> Ꮡbuf, @string name) {
    ref var buf = ref Ꮡbuf.DerefOrNull();

    if (!saveTraces.Value) {
        return;
    }
    {
        var err = os.WriteFile(name + ".trace"u8, buf.Bytes(), 384); if (err != default!) {
            Ꮡt.Errorf("failed to write trace file: %s"u8, err);
        }
    }
}

} // end trace_test_package
