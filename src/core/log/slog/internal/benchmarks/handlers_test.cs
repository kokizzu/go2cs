// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.log.slog.@internal;

using bytes = bytes_package;
using context = context_package;
using slog = go.log.slog_package;
using slices = slices_package;
using testing = testing_package;
using go.log;
using io = io_package;
using static go.log.slog.@internal.benchmarks_package;
using time = time_package;

partial class benchmarks_internal_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string textˢ = "text"u8;
internal static readonly @string asyncˢ = "async"u8;

public static void TestHandlers(ж<testing.T> Ꮡt) {
    var ctx = context.Background();
    ref var r = ref heap<slog.Record>(out var Ꮡr);
    r = slog.NewRecord(testTime, slog.LevelInfo, testMessage, 0);
    r.AddAttrs(testAttrs.ꓸꓸꓸ);
    var ctxʗ1 = ctx;
    var rʗ1 = r;
    Ꮡt.Run(textˢ, (ж<testing.T> tΔ1) => {
        ref var b = ref heap(new bytes.Buffer(), out var Ꮡb);
        var h = newFastTextHandler(new benchmarks_internal_test_package.bytes_BufferжWriter(Ꮡb));
        {
            var err = h.Handle(ctxʗ1, rʗ1); if (err != default!) {
                tΔ1.Fatal(err);
            }
        }
        @string got = Ꮡb.String();
        if (got != wantText) {
            tΔ1.Errorf("\ngot  %q\nwant %q"u8, got, wantText);
        }
    });
    var ctxʗ2 = ctx;
    var rʗ2 = r;
    Ꮡt.Run(asyncˢ, (ж<testing.T> tΔ2) => {
        var h = newAsyncHandler();
        {
            var err = h.Handle(ctxʗ2, rʗ2); if (err != default!) {
                tΔ2.Fatal(err);
            }
        }
        var got = (~h).ringBuffer[0].ΔClone();
        if (!got.Time.Equal(rʗ2.Time) || !slices.EqualFunc<slice<slog.Attr>, slice<slog.Attr>, slog.Attr, slog.Attr>(attrSlice(got), attrSlice(rʗ2), (Func<slog.Attr, slog.Attr, bool>)(slog.Equal))) {
            tΔ2.Errorf("got %+v, want %+v"u8, got, rʗ2);
        }
    });
}

internal static slice<slog.Attr> attrSlice(slog.Record r) {
    r = r.ΔClone();

    ref var @as = ref heap<slice<slog.Attr>>(out var Ꮡas);
    r.Attrs((slog.Attr a) => {
        Ꮡas.ValueSlot = append(Ꮡas.ValueSlot, a);
        return true;
    });
    return @as;
}

} // end benchmarks_internal_test_package
