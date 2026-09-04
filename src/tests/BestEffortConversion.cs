// BestEffortConversion.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The ONE definition of "did this converter invocation actually regenerate the package's output, or
// did it degrade and exit 0 anyway". LINKED by path, like ConverterBuildInputs.cs and
// PlatformExclusive.cs beside it.
//
// go2cs EXITS 0 on a package it could not fully type-check: conversionDriver.go prints
// "WARNING: <pkg> did not fully type-check; converting best-effort ..." (and "visit file error" for a
// recovered visitor panic, where one source file's emission was skipped outright), writes a degraded
// emission, and returns success. Every instrument that asks the exit code ALONE therefore reads that
// run as a measured transpile. check-no-regression.ps1 has classified these two stderr classes as
// NOT MEASURED by name since 2026-08-08, for exactly this reason, and the markers below are its
// markers -- deliberately not a re-wording, because a paraphrase that drifts from the converter's
// actual text is a predicate that silently stops matching.
//
// Every OTHER "WARNING" line is ADVISORY (unsafe.Sizeof usage, and friends): present on a healthy
// run, counted by CNR, never fatal. A predicate that matched "WARNING" generally would refuse every
// golden in the corpus, which is a false RED rather than a false green but is just as unusable.
//
// ---------------------------------------------------------------------------------------------
// MERGE POINT, stated here so it cannot be resolved by accident. This file is written at the path
// SUB-Q10 announced for the same predicate, ON PURPOSE: two differently-NAMED predicates would
// auto-merge cleanly and leave the tree with two definitions of "measurable transpile", which is the
// silent-duplication shape CLAUDE.md's lane-integration section forbids. The same path collides as an
// add/add CONFLICT instead, which cannot be missed. Take SUB-Q10's version at merge -- it is the
// richer one, carrying the runner's Status.BestEffort classification as well -- and re-point the two
// call sites here (BehavioralRunner.RunTranspile and UpdateTestTargets) at it.
// ---------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;

internal static class BestEffortConversion
{
    // The converter's own words. Kept as one alternation so the live pattern is a single literal a
    // guard can extract, rather than a list a guard would have to reassemble.
    private static readonly Regex s_marker =
        new("did not fully type-check|visit file error", RegexOptions.Compiled);

    /// <summary>
    /// True when <paramref name="converterOutput"/> shows the converter degraded rather than fully
    /// regenerating the package, with the first such line reported through
    /// <paramref name="marker"/> so a refusal can quote the converter instead of paraphrasing it.
    /// </summary>
    public static bool IsBestEffort(string? converterOutput, out string marker)
    {
        marker = "";

        if (string.IsNullOrEmpty(converterOutput))
            return false;

        string? line = converterOutput
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .FirstOrDefault(l => s_marker.IsMatch(l));

        if (line is null)
            return false;

        marker = line.Trim();
        return true;
    }
}
