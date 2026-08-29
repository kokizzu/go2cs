// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using testing = testing_package;
using static go.@internal.trace_package;

partial class trace_internal_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string oneElementˢ = "one element"u8;
internal static readonly @string twoElementsˢ = "two elements"u8;
internal static readonly @string sixElementsˢ = "six elements"u8;
internal static readonly @string oneElementAgainˢ = "one element again"u8;
internal static readonly @string twoElementsAgainˢ = "two elements again"u8;

public static void TestQueue(ж<testing.T> Ꮡt) {
    ref var q = ref heap(new global::go.@internal.trace_package.queue<nint>(), out var Ꮡq);
    void check(@string name, slice<nint> exp) {
        foreach (var (_, v) in exp) {
            Ꮡq.Value.push(v);
        }
        foreach (var (i, want) in exp) {
            {
                var (got, ok) = Ꮡq.Value.pop(); if (!ok){
                    Ꮡt.Fatalf("check %q: expected to be able to pop after %d pops"u8, name, i + 1);
                } else 
                if (got != want) {
                    Ꮡt.Fatalf("check %q: expected value %d after on pop %d, got %d"u8, name, want, i + 1, got);
                }
            }
        }
        {
            var (_, ok) = Ꮡq.Value.pop(); if (ok) {
                Ꮡt.Fatalf("check %q: did not expect to be able to pop more values"u8, name);
            }
        }
        {
            var (_, ok) = Ꮡq.Value.pop(); if (ok) {
                Ꮡt.Fatalf("check %q: did not expect to be able to pop more values a second time"u8, name);
            }
        }
    }
    check(oneElementˢ, new nint[]{4}.slice());
    check(twoElementsˢ, new nint[]{64, 12}.slice());
    check(sixElementsˢ, new nint[]{55, 16423, 2352, 644, 12874, 9372}.slice());
    check(oneElementAgainˢ, new nint[]{7}.slice());
    check(twoElementsAgainˢ, new nint[]{77, 6336}.slice());
}

} // end trace_internal_test_package
