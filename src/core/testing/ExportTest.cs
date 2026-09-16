// ExportTest.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

using go.testing_runtime;

// go2cs HAND-OWNED (whole file). This is the shape of Go's own testing/export_test.go: the names
// Go's testing package exports SOLELY so its own tests can bind them. Go 1.24.13's export_test.go
// exports four, and the corpus declares none of them; this file declares the ONE whose binder the
// corpus carries and NAMES the other three rather than declaring them, because a declaration with
// no target is a dangling name (q92, ruled 2026-09-16).
//
// ⚠ IT IS NOT NAMED export_test.cs, AND THE REASON IS MECHANICAL. testing.csproj takes
// `<Compile Include="*.cs" />` and then `<Compile Remove="*_test.cs;…" />`, while
// testing.tests.csproj lists its files EXPLICITLY. A file named export_test.cs would be removed
// from the package project by that exclusion and never added to the test project by that list, so
// it would be compiled by NOTHING and this declaration would be a silent no-op that breaks no build.
// The member must live in the PACKAGE assembly in any case: a converted test binds
// `testing.ParallelConflict` through `using testing = testing_package;`, and a partial class cannot
// span two assemblies.
[module: go.GoManualConversion]

namespace go;

public static partial class testing_package
{
    // Go 1.24.13 testing/export_test.go:13  `const ParallelConflict = parallelConflict`, whose value
    // is testing.go:1530. The corpus already carries that value at the 1.24.13 text — row 130 landed
    // it in the hand-owned host as TestExecution.ParallelConflictText — so nothing is re-derived from
    // Go here and there is exactly one copy of the string. What was missing is the exported NAME the
    // test binds: at 1.23.12 the test carried its own two literals and asserted them by identity; at
    // 1.24.13 those two collapse into one and the test stops carrying a value and starts IMPORTING
    // the package's. That change of shape is what this declaration answers.
    //
    // ⚠ PUBLIC, NOT INTERNAL, AND THE RULING NAMED THIS CASE. An internal member would be the
    // narrower surface, reachable from the row's own test assembly through an InternalsVisibleTo
    // grant — and testing.csproj does NOT carry one for `testing.tests`. That is the measured fact
    // the ruling's condition turns on: the project grants internals to `GolibTests` alone, through a
    // hand-written AssemblyAttribute for the host's own MSTest guards. So the member is PUBLIC and
    // this comment is the why.
    //
    // Context, stated at its real strength rather than the strength it first read at: the emitted
    // `<InternalsVisibleTo Include="$(AssemblyName).tests" />` appears in THREE of the five converted
    // projects in this host (testing/fstest, testing/iotest, testing/quick) and NOT in
    // testing/slogtest or testing/internal/testdeps — so it is the emitted shape where it appears,
    // not a universal one, and the first census here said "every converted sibling" on a look at
    // three. Adding that line to testing.csproj and narrowing this member to `internal` is a
    // separate change, raised rather than taken.
    //
    // The type is `@string` and the initializer is the host's own const: `public static readonly
    // @string` is the corpus's spelling for a package-level Go string constant (103 sites; `public
    // const @string` is impossible — @string is a struct — and appears 0 times), and golib's
    // `implicit operator @string(string)` carries the host's C# literal across.
    public static readonly @string ParallelConflict = TestExecution.ParallelConflictText;

    // ⚠ OWED, NOT DECLARED — the other three names Go 1.24.13's export_test.go exports. Each has
    // exactly ONE binder in Go's own test files and the corpus carries NONE of those files, so a
    // declaration here would bind nothing today:
    //
    //     var   PrettyPrint          = prettyPrint            binder benchmark_test.go       not carried
    //     type  HighPrecisionTime    = highPrecisionTime      binder testing_windows_test.go not carried
    //     var   HighPrecisionTimeNow = highPrecisionTimeNow   binder testing_windows_test.go not carried
    //
    // They are a standing row, not a hop regression: the corpus has never declared them at either
    // pin, where ParallelConflict is new at 1.24.13 (`parallelConflict` does not exist at 1.23.12 at
    // all). The day one of those binder files is carried, its name is declared here beside this one.
}
