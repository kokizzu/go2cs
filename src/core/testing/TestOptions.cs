// TestOptions.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using go.golib;

namespace go.testing_runtime;
/// <summary>
/// The <c>go test</c> command line, parsed — the flags this host understands, with Go's defaults.
/// </summary>
/// <remarks>
/// <para>
/// The flag names, their meanings and their defaults track <c>go test</c> deliberately, because the
/// Phase-4 comparison runs the SAME arguments against both sides. A flag that means something
/// slightly different here would make the two runs incomparable without any error being reported —
/// the run would simply be answering a different question.
/// </para>
/// <para>
/// <c>-run</c> is compiled to a regex ARRAY, one element per <c>/</c>-separated segment, because
/// Go's filter matches subtest paths segment by segment rather than the joined name.
/// <c>ParseDuration</c> accepts Go's duration syntax (<c>2m</c>, <c>500ms</c>, <c>1h30m</c>) rather
/// than .NET's, for the same reason.
/// </para>
/// </remarks>
internal sealed class TestOptions
{
    public bool Json { get; private set; }
    public bool Verbose { get; private set; }
    public bool Short { get; private set; }
    public int Count { get; private set; } = 1;
    public int Parallel { get; private set; } = Environment.ProcessorCount;
    public int? ShuffleSeed { get; private set; }
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromMinutes(10.0D);
    public string? ResultFile { get; private set; }
    public string? JUnitFile { get; private set; }
    private Regex[]? Filters { get; set; }

    public static TestOptions Parse(string[] args)
    {
        TestOptions options = new();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            string key = arg;
            string? value = null;
            int equals = arg.IndexOf('=');

            if (equals >= 0)
            {
                key = arg[..equals];
                value = arg[(equals + 1)..];
            }

            switch (key)
            {
                case "--json":
                    options.Json = true;
                    // `go test -json` implies -v (cmd/go passes -test.v), so the Go side of the
                    // differential runs with testing.Verbose() == true; mirror it or tests that
                    // gate on Verbose() (sort's countOps) skip here while passing there.
                    options.Verbose = true;
                    break;
                case "-v":
                case "-test.v":
                    options.Verbose = value is null || bool.Parse(value);
                    break;
                case "-short":
                case "-test.short":
                    // Backs testing.Short() in the shim; go test's default (flag absent) is false.
                    options.Short = value is null || bool.Parse(value);
                    break;
                case "-run":
                case "-test.run":
                    value ??= NextValue(args, ref i, key);
                    options.Filters = value.Split('/').Select(part =>
                        new Regex(part, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1.0D))).ToArray();
                    break;
                case "-count":
                case "-test.count":
                    value ??= NextValue(args, ref i, key);
                    options.Count = Math.Max(1, int.Parse(value, CultureInfo.InvariantCulture));
                    break;
                case "-parallel":
                case "-test.parallel":
                    // Caps simultaneously RUNNING parallel tests, like go test's -parallel flag
                    // (whose default is GOMAXPROCS — the processor count matches that default).
                    value ??= NextValue(args, ref i, key);
                    options.Parallel = Math.Max(1, int.Parse(value, CultureInfo.InvariantCulture));
                    break;
                case "-shuffle":
                case "-test.shuffle":
                    value ??= NextValue(args, ref i, key);
                    options.ShuffleSeed = value == "on"
                        ? Random.Shared.Next()
                        : value == "off" ? null : int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "-timeout":
                case "-test.timeout":
                    value ??= NextValue(args, ref i, key);
                    options.Timeout = ParseDuration(value);
                    break;
                case "--result":
                    value ??= NextValue(args, ref i, key);
                    options.ResultFile = value;
                    break;
                case "--junit":
                    value ??= NextValue(args, ref i, key);
                    options.JUnitFile = value;
                    break;
                default:
                    throw new ArgumentException($"unsupported converted test option: {arg}");
            }
        }

        return options;
    }

    public void ResolveOutputPaths(string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(ResultFile) && !Path.IsPathRooted(ResultFile))
            ResultFile = Path.GetFullPath(Path.Combine(baseDirectory, ResultFile));
        if (!string.IsNullOrWhiteSpace(JUnitFile) && !Path.IsPathRooted(JUnitFile))
            JUnitFile = Path.GetFullPath(Path.Combine(baseDirectory, JUnitFile));
    }

    public bool ShouldRun(string fullName)
    {
        if (Filters is null)
            return true;

        string[] nameParts = fullName.Split('/');
        int count = Math.Min(nameParts.Length, Filters.Length);
        for (int i = 0; i < count; i++)
        {
            if (!Filters[i].IsMatch(nameParts[i]))
                return false;
        }
        return true;
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length)
            throw new ArgumentException($"missing value for {option}");
        return args[index];
    }

    // Go's duration syntax is a SEQUENCE of decimal-and-unit pairs ("30m0s", "1h30m", "1.5s") — which
    // is exactly what time.Duration.String() emits and what `go test -timeout` accepts. A single-pair
    // parser rejected the pipeline's own `-test-timeout` value the moment it was threaded through
    // (`-timeout 30m0s` -> "invalid Go-style duration", exit 2 before a single test ran), so parse the
    // real grammar. Units are Go's: ns, us (µs / μs), ms, s, m, h.
    private const string DurationUnits = "ns|us|\u00B5s|\u03BCs|ms|s|m|h";

    private static TimeSpan ParseDuration(string value)
    {
        if (!Regex.IsMatch(value, $@"^(?:\d+(?:\.\d+)?(?:{DurationUnits}))+$", RegexOptions.CultureInvariant))
            throw new ArgumentException($"invalid Go-style duration: {value}");

        TimeSpan total = TimeSpan.Zero;

        foreach (Match match in Regex.Matches(value, $@"(?<number>\d+(?:\.\d+)?)(?<unit>{DurationUnits})", RegexOptions.CultureInvariant))
        {
            double number = double.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);

            total += match.Groups["unit"].Value switch
            {
                "ns" => TimeSpan.FromTicks((long)(number / 100.0D)),
                "us" or "\u00B5s" or "\u03BCs" => TimeSpan.FromMilliseconds(number / 1000.0D),
                "ms" => TimeSpan.FromMilliseconds(number),
                "s" => TimeSpan.FromSeconds(number),
                "m" => TimeSpan.FromMinutes(number),
                "h" => TimeSpan.FromHours(number),
                _ => throw new ArgumentException($"invalid duration unit: {value}")
            };
        }

        return total;
    }
}
