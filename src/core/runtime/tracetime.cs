// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// Trace time and clock.
namespace go;

using goarch = @internal.goarch_package;
using _ = unsafe_package;
using @internal;

partial class runtime_package {

// Timestamps in trace are produced through either nanotime or cputicks
// and divided by traceTimeDiv. nanotime is used everywhere except on
// platforms where osHasLowResClock is true, because the system clock
// isn't granular enough to get useful information out of a trace in
// many cases.
//
// This makes absolute values of timestamp diffs smaller, and so they are
// encoded in fewer bytes.
//
// The target resolution in all cases is 64 nanoseconds.
// This is based on the fact that fundamentally the execution tracer won't emit
// events more frequently than roughly every 200 ns or so, because that's roughly
// how long it takes to call through the scheduler.
// We could be more aggressive and bump this up to 128 ns while still getting
// useful data, but the extra bit doesn't save us that much and the headroom is
// nice to have.
//
// Hitting this target resolution is easy in the nanotime case: just pick a
// division of 64. In the cputicks case it's a bit more complex.
//
// For x86, on a 3 GHz machine, we'd want to divide by 3*64 to hit our target.
// To keep the division operation efficient, we round that up to 4*64, or 256.
// Given what cputicks represents, we use this on all other platforms except
// for PowerPC.
// The suggested increment frequency for PowerPC's time base register is
// 512 MHz according to Power ISA v2.07 section 6.2, so we use 32 on ppc64
// and ppc64le.
internal static readonly UntypedInt traceTimeDiv = /* (1-osHasLowResClockInt)*64 + osHasLowResClockInt*(256-224*(goarch.IsPpc64|goarch.IsPpc64le)) */ 256;

[GoType("num:uint64")] partial struct traceTime;

// traceClockNow returns a monotonic timestamp. The clock this function gets
// the timestamp from is specific to tracing, and shouldn't be mixed with other
// clock sources.
//
// nosplit because it's called from exitsyscall, which is nosplit.
//
// traceClockNow is called by golang.org/x/exp/trace using linkname.
//
//go:linkname traceClockNow
//go:nosplit
internal static traceTime traceClockNow() {
    if (osHasLowResClock) {
        return ((traceTime)(cputicks() / traceTimeDiv));
    }
    return ((traceTime)(nanotime() / traceTimeDiv));
}

// traceClockUnitsPerSecond estimates the number of trace clock units per
// second that elapse.
internal static uint64 traceClockUnitsPerSecond() {
    if (osHasLowResClock) {
        // We're using cputicks as our clock, so we need a real estimate.
        return ((uint64)(ticksPerSecond() / traceTimeDiv));
    }
    // Our clock is nanotime, so it's just the constant time division.
    // (trace clock units / nanoseconds) * (1e9 nanoseconds / 1 second)
    return ((uint64)(1.0F / ((float64)traceTimeDiv) * 1e9F));
}

// traceFrequency writes a batch with a single EvFrequency event.
//
// freq is the number of trace clock units per second.
internal static void traceFrequency(uintptr gen) {
    ref var w = ref heap<traceWriter>(out var Ꮡw);
    w = unsafeTraceWriter(gen, nil);
    // Ensure we have a place to write to.
    (w, _) = w.ensure(1 + traceBytesPerNumber);
    /* traceEvFrequency + frequency */
    // Write out the string.
    w.@byte(((byte)traceEvFrequency));
    w.varint(traceClockUnitsPerSecond());
    // Immediately flush the buffer.
    systemstack(
    var traceʗ2 = Δtrace;
    var wʗ2 = w;
    () => {
        @lock(Ꮡtraceʗ2.of(atomic.Int32; seqGC uint64; minPageHeapAddr uint64; debugMalloc bool}.Ꮡlock));
        traceBufFlush(wʗ2.traceBuf, gen);
        unlock(Ꮡtraceʗ2.of(atomic.Int32; seqGC uint64; minPageHeapAddr uint64; debugMalloc bool}.Ꮡlock));
    });
}

} // end runtime_package
