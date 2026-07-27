// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using slices = slices_package;
using testing = testing_package;

partial class zstd_package {

// literalPredefinedDistribution is the predefined distribution table
// for literal lengths. RFC 3.1.1.3.2.2.1.
internal static slice<int16> literalPredefinedDistribution = new int16[]{
    4, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1,
    2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 1, 1, 1, 1, 1,
    (int16)(-1), (int16)(-1), (int16)(-1), (int16)(-1)
}.slice();

// offsetPredefinedDistribution is the predefined distribution table
// for offsets. RFC 3.1.1.3.2.2.3.
internal static slice<int16> offsetPredefinedDistribution = new int16[]{
    1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, (int16)(-1), (int16)(-1), (int16)(-1), (int16)(-1), (int16)(-1)
}.slice();

// matchPredefinedDistribution is the predefined distribution table
// for match lengths. RFC 3.1.1.3.2.2.2.
internal static slice<int16> matchPredefinedDistribution = new int16[]{
    1, 4, 3, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, (int16)(-1), (int16)(-1),
    (int16)(-1), (int16)(-1), (int16)(-1), (int16)(-1), (int16)(-1)
}.slice();

[GoType("dyn")] partial struct TestPredefinedTables_tests {
    internal @string name;
    internal slice<int16> distribution;
    internal nint tableBits;
    internal Func<ж<Reader>, nint, slice<fseEntry>, slice<fseBaselineEntry>, error> toBaseline;
    internal slice<fseBaselineEntry> predef;
}

// TestPredefinedTables verifies that we can generate the predefined
// literal/offset/match tables from the input data in RFC 8878.
// This serves as a test of the predefined tables, and also of buildFSE
// and the functions that make baseline FSE tables.
public static void TestPredefinedTables(ж<testing.T> Ꮡt) {
    var tests = new TestPredefinedTables_tests[]{
        new(
            name: "literal"u8,
            distribution: literalPredefinedDistribution,
            tableBits: 6,
            toBaseline: (Func<ж<Reader>, nint, slice<fseEntry>, slice<fseBaselineEntry>, error>)(makeLiteralBaselineFSE),
            predef: predefinedLiteralTable[..]
        ),
        new(
            name: "offset"u8,
            distribution: offsetPredefinedDistribution,
            tableBits: 5,
            toBaseline: (Func<ж<Reader>, nint, slice<fseEntry>, slice<fseBaselineEntry>, error>)(makeOffsetBaselineFSE),
            predef: predefinedOffsetTable[..]
        ),
        new(
            name: "match"u8,
            distribution: matchPredefinedDistribution,
            tableBits: 6,
            toBaseline: (Func<ж<Reader>, nint, slice<fseEntry>, slice<fseBaselineEntry>, error>)(makeMatchBaselineFSE),
            predef: predefinedMatchTable[..]
        )
    }.slice();
    foreach (var (_, test) in tests) {
        ref var testΔ1 = ref heap<TestPredefinedTables_tests>(out var ᏑtestΔ1);
        testΔ1 = test;
        var testʗ1 = testΔ1;
        Ꮡt.Run(testΔ1.name, (ж<testing.T> tΔ1) => {
            ref var r = ref heap(new Reader(), out var Ꮡr);
            var table = new slice<fseEntry>(((nint)1).Lsh((uint64)(testʗ1.tableBits)));
            {
                var err = r.buildFSE(0, testʗ1.distribution, table, testʗ1.tableBits); if (err != default!) {
                    tΔ1.Fatal(err);
                }
            }
            var baselineTable = new slice<fseBaselineEntry>(builtin.len(table));
            {
                var err = testʗ1.toBaseline(Ꮡr, 0, table, baselineTable); if (err != default!) {
                    tΔ1.Fatal(err);
                }
            }
            if (!slices.Equal<slice<fseBaselineEntry>, fseBaselineEntry>(baselineTable, testʗ1.predef)) {
                tΔ1.Errorf("got %v, want %v"u8, baselineTable, testʗ1.predef);
            }
        });
    }
}

} // end zstd_package
