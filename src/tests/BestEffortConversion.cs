// BestEffortConversion.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The ONE definition of "did this converter run actually regenerate the package's output", shared by
// the two harnesses that transpile behavioral packages: BehavioralRunner and the MSTest
// BehavioralTestBase. LINKED into both by path, for the same reason ConverterBuildInputs.cs and
// PlatformExclusive.cs are -- the harnesses take no assembly dependency on each other, so a shared
// assembly is not available, and two copies of a predicate are two chances for them to disagree
// about which runs may be believed.
//
// WHY IT EXISTS. go2cs exits ZERO on a best-effort conversion. A package that does not fully
// type-check on this host still converts: go/types leaves every expression downstream of the load
// error untyped, the converter says so on stderr and emits what it can. Both harnesses asked the
// exit code alone, so that run reported the Transpile phase as PASS -- and the poisoned .cs then
// reached Compile, Target and Output, where it reads as a downstream break attributed to the wrong
// layer, or (worse) as a byte-identical Target pass over a file the run never regenerated.
//
// check-no-regression.ps1 has classified the SAME two stderr classes as NOT MEASURED by name since
// 2026-08-08, for the same reason and in the same words. This file is that classification moved into
// the harnesses that were missing it; the wording of the markers is deliberately identical, so the
// three instruments cannot drift into disagreeing about what a measured transpile is.
//
// The two classes, and why only these two:
//   * "did not fully type-check" (conversionDriver.go) -- the package loaded WITH errors and every
//     expression depending on one of them is emitted untyped. Best-effort by construction.
//   * "visit file error"        (conversionDriver.go / autoSiblingOperations.go) -- a recovered
//     visitor panic; that source file's emission was SKIPPED entirely.
// Every other converter WARNING is advisory (unsafe.Sizeof usage and friends) -- present on a
// perfectly healthy run, and treating one as unmeasured would make the honest verdict unreachable.
//
// NOT a failure and NOT a pass. Nothing was learned about the package either way, which is the same
// shape as an expired timeout budget and takes the same word: NOT MEASURED. The two harnesses map it
// onto their own vocabulary, and the two vocabularies are NOT equally strong -- stated here rather
// than blurred: BehavioralRunner's Status.BestEffort joins the NOT MEASURED bucket and so fails the
// run through its EXIT CODE, while MSTest's Assert.Inconclusive marks the test NotExecuted, which
// `dotnet test` reports as Skipped and does not count as a failure. That is exactly the strength F8's
// own platform-exclusive skip has in that harness, and it is the property that matters here: an
// unmeasured project must never read as a PASS. The runner is where the verdict is carried.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

internal static class BestEffortConversion
{
    // Substring alternation rather than an anchored line match, deliberately, and the two spellings
    // differ in a way that matters: the type-check line comes from log.Printf, so it arrives behind a
    // date-and-time log prefix ("2026/09/04 02:29:29 WARNING: <pkg> did not fully type-check; ..."),
    // while "visit file error" comes through showWarning, which writes a bare "WARNING: " and, in the
    // visitor-scoped form, appends the source file. Anchoring on either shape would make the predicate
    // depend on the converter's diagnostic FORMATTING rather than on what it said.
    //
    // The markers are ASCII on purpose. The type-check warning's full text contains an em dash, and
    // a stderr stream decoded under any of the encodings a Windows console can hand back would put
    // that character at risk; nothing in this pattern can be mangled by an encoding choice.
    private static readonly Regex s_marker =
        new("did not fully type-check|visit file error", RegexOptions.Compiled);

    /// <summary>
    /// The converter-stderr lines saying this run did NOT fully regenerate the package's output.
    /// Empty when it did -- including when stderr carried advisory warnings, which are normal.
    /// </summary>
    public static string[] NotFullyRegeneratedLines(string? converterStdErr)
    {
        if (string.IsNullOrEmpty(converterStdErr))
            return Array.Empty<string>();

        List<string> lines = new();

        foreach (string line in converterStdErr.Split('\n'))
        {
            string trimmed = line.TrimEnd('\r');

            if (s_marker.IsMatch(trimmed))
                lines.Add(trimmed.Trim());
        }

        return lines.ToArray();
    }

    /// <summary>
    /// True when the converter's stderr says the emission is best-effort, with the offending lines.
    /// A caller that has already seen a NON-ZERO exit code should report that instead: a converter
    /// that failed outright is a louder and more specific fact than the degradation it printed on
    /// the way down.
    /// </summary>
    public static bool NotFullyRegenerated(string? converterStdErr, out string[] lines)
    {
        lines = NotFullyRegeneratedLines(converterStdErr);
        return lines.Length > 0;
    }
}
