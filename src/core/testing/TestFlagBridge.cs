// TestFlagBridge.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using go.golib;

namespace go.testing_runtime;

/// <summary>
/// Publishes the host's own command line to the converted <c>flag</c> package's
/// <c>CommandLine</c> set, so a converted <c>TestMain</c> that calls <c>flag.Parse()</c> parses the
/// arguments this host was started with instead of rejecting them.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors <c>testing.Init()</c>, and it exists for the same reason Go has it. Go's test binary
/// is launched by <c>go test</c> with <c>-test.*</c> arguments on its command line, and a package's
/// own <c>TestMain</c> is free to call <c>flag.Parse()</c> — which parses that whole command line
/// through <c>flag.CommandLine</c>. That is legal only because <c>testing.Init()</c> DEFINED every
/// one of those flags on <c>flag.CommandLine</c> before <c>TestMain</c> ran. Nothing consumes or
/// hides the arguments; they are simply declared, so the parse recognizes them.
/// </para>
/// <para>
/// The go2cs host's arguments (<c>--json</c>, <c>-timeout</c>, <c>--result</c>, <c>--junit</c>, and
/// the rest of <see cref="TestOptions"/>'s vocabulary) are the exact analogue of <c>go test</c>'s
/// <c>-test.*</c>, and until this existed they were declared nowhere: every converted <c>TestMain</c>
/// calling <c>flag.Parse()</c> died on <c>flag provided but not defined: -json</c> before a single
/// test ran. <c>internal/fuzz</c>, <c>go/internal/srcimporter</c>, <c>os/exec</c> and
/// <c>crypto/tls</c> are the members of that class in the converted corpus.
/// </para>
/// <para>
/// The <c>-test.*</c> set is registered TOO, in full, with the values this run actually holds —
/// not merely the spellings the host was given. A converted test reads them: <c>os/exec</c>'s
/// <c>TestMain</c> gates on <c>flag.Lookup("test.run").Value.String() == ""</c> and
/// <c>flag.Lookup("test.list")</c>, and <c>runtime</c>'s gdb tests read
/// <c>flag.Lookup("test.parallel").Value.(flag.Getter).Get().(int)</c>. A missing definition is a nil
/// dereference there rather than a parse error, so registering only what appeared on the command
/// line would trade one failure shape for a quieter one. Registering the whole set is both what
/// <c>testing.Init()</c> does and what closes the class.
/// </para>
/// <para>
/// <b>Why the <c>flag</c> package is bound LATE rather than referenced.</b> Go's <c>testing</c>
/// imports <c>flag</c>, so a project reference is the obvious mirror — but it was measured and it
/// does not work here. The converter-generated test projects set
/// <c>DisableTransitiveProjectReferences=true</c>, which is load-bearing (a transitive reference
/// contributing a child <c>go.&lt;pkg&gt;</c> namespace collides with the emitted
/// <c>using</c> aliases, CS0576). So a <c>testing</c> → <c>flag</c> reference does NOT deploy
/// <c>flag.dll</c> beside a test host whose own package does not import <c>flag</c> — 124 of the
/// corpus's 141 test projects — and any unconditional use of it there would be a
/// <c>FileNotFoundException</c> in every one. It also cost every test project's build a measured
/// +33% (unicode/utf8: 7.5 s warm → 10.2 s) for a reference all but seventeen of them cannot use.
/// </para>
/// <para>
/// Late binding is not a workaround for that; it is the accurate statement of the dependency. The
/// converted <c>flag</c> package is present in a test compilation if and only if the package under
/// test imports it, which is exactly when a converted <c>flag.Parse()</c> can be reached and when
/// <c>flag.CommandLine</c> is observable at all. When it is absent there is nothing to register into
/// and nothing that could look; doing nothing is then correct rather than merely safe. The bound
/// surface is deliberately narrow — six static methods of one type — and every ARGUMENT type
/// (<c>@string</c>, <c>nint</c>, <c>nuint</c>, <c>time.Duration</c>) is a golib or <c>time</c> type
/// the host already references, so only the <c>flag</c> type itself is resolved by name.
/// </para>
/// <para>
/// A name the test package already defined is SKIPPED, because the converted
/// <c>flag.FlagSet.Var</c> panics on redefinition ("flag redefined") and the host must never turn a
/// package's own flag into a crash. The host's unprefixed spellings (<c>-v</c>, <c>-run</c>,
/// <c>-timeout</c>, …) are the only ones that could collide — Go's <c>-test.</c> prefix exists
/// precisely to make collision impossible — and across all of GOROOT's non-<c>cmd</c> test sources
/// exactly one such definition exists (<c>-v</c>, in <c>cmd/compile/internal/ssa</c>, which is not
/// converted). The guard is cheap and the package's own flag rightly wins.
/// </para>
/// </remarks>
internal static class TestFlagBridge
{
    // The converted flag package: type `go.flag_package` in assembly `flag` (flag.csproj's
    // AssemblyName). Resolved by name because the host must not reference it — see the remarks.
    private const string FlagPackageTypeName = "go.flag_package, flag";

    /// <summary>
    /// Declares this run's flags on the converted <c>flag.CommandLine</c>, if the package under test
    /// brought the converted <c>flag</c> package into the compilation. A no-op when it did not.
    /// </summary>
    public static void Register(TestOptions options)
    {
        // throwOnError:false — an absent flag.dll is the ordinary case (124 of 141 test projects),
        // not a fault.
        Type? flagPackage = Type.GetType(FlagPackageTypeName, throwOnError: false);

        if (flagPackage is null)
            return;

        Registrar registrar = new(flagPackage);

        // Go's testing.Init(), flag for flag, with this run's values where the host has one. The
        // usage strings are Go's own so that a TestMain which prints them — crypto/tls's installs a
        // flag.Usage that calls flag.PrintDefaults() — produces output comparable with `go test`.
        registrar.Bool("test.short", options.Short, "run smaller test suite to save time");
        registrar.Bool("test.failfast", false, "do not start new tests after the first test failure");
        registrar.String("test.outputdir", "", "write profiles to `dir`");
        registrar.Bool("test.v", options.Verbose, "verbose: print additional output");
        registrar.Uint("test.count", (nuint)options.Count, "run tests and benchmarks `n` times");
        registrar.String("test.coverprofile", "", "write a coverage profile to `file`");
        registrar.String("test.gocoverdir", "", "write coverage intermediate files to this directory");
        registrar.String("test.list", "", "list tests, examples, and benchmarks matching `regexp` then exit");
        registrar.String("test.run", options.RunPattern, "run only tests and examples matching `regexp`");
        registrar.String("test.skip", "", "do not list or run tests matching `regexp`");
        registrar.String("test.memprofile", "", "write an allocation profile to `file`");
        registrar.Int("test.memprofilerate", 0, "set memory allocation profiling `rate` (see runtime.MemProfileRate)");
        registrar.String("test.cpuprofile", "", "write a cpu profile to `file`");
        registrar.String("test.blockprofile", "", "write a goroutine blocking profile to `file`");
        registrar.Int("test.blockprofilerate", 1, "set blocking profile `rate` (see runtime.SetBlockProfileRate)");
        registrar.String("test.mutexprofile", "", "write a mutex contention profile to the named file after execution");
        registrar.Int("test.mutexprofilefraction", 1, "if >= 0, calls runtime.SetMutexProfileFraction()");
        registrar.Bool("test.paniconexit0", false, "panic on call to os.Exit(0)");
        registrar.String("test.trace", "", "write an execution trace to `file`");
        registrar.Duration("test.timeout", options.Timeout, "panic test binary after duration `d` (default 0, timeout disabled)");
        registrar.String("test.cpu", "", "comma-separated `list` of cpu counts to run each test with");
        registrar.Int("test.parallel", options.Parallel, "run at most `n` tests in parallel");
        registrar.String("test.testlogfile", "", "write test action log to `file` (for use only by cmd/go)");
        registrar.String("test.shuffle", options.ShuffleValue, "randomize the execution order of tests and benchmarks");
        registrar.Bool("test.fullpath", false, "show full file names in error messages");

        // initBenchmarkFlags(). benchtime is a flag.Var in Go (it also accepts an `Nx` iteration
        // count); a string carries every value it accepts and reports it the same way.
        registrar.String("test.bench", "", "run only benchmarks matching `regexp`");
        registrar.Bool("test.benchmem", false, "print memory allocations for benchmarks");
        registrar.String("test.benchtime", "1s", "run each benchmark for duration `d` or N times if `d` is of the form Nx");

        // initFuzzFlags(). fuzztime and fuzzminimizetime are flag.Vars in Go for the same reason.
        registrar.String("test.fuzz", "", "run the fuzz test matching `regexp`");
        registrar.String("test.fuzztime", "0s", "time to spend fuzzing; default is to run indefinitely");
        registrar.String("test.fuzzminimizetime", "1m0s", "time to spend minimizing a value after finding a failing input");
        registrar.String("test.fuzzcachedir", "", "directory where interesting fuzzing inputs are stored (for use only by cmd/go)");
        registrar.Bool("test.fuzzworker", false, "coordinate with the parent process to fuzz random values (for use only by cmd/go)");

        // The host's OWN spellings — the arguments this process was actually started with, which is
        // what flag.Parse() is about to read. TestOptions.Parse has already consumed them; these
        // definitions exist so the parse recognizes them, exactly as Go's -test.* definitions do.
        // Go's flag package strips one OR two leading dashes, so `--json` finds the name `json`.
        registrar.Bool("json", options.Json, "emit go test -json events on stdout");
        registrar.String("result", options.ResultFile ?? "", "write the run's result JSON to `file`");
        registrar.String("junit", options.JUnitFile ?? "", "write the run's JUnit XML to `file`");
        registrar.Bool("v", options.Verbose, "verbose: print additional output");
        registrar.Bool("short", options.Short, "run smaller test suite to save time");
        registrar.String("run", options.RunPattern, "run only tests and examples matching `regexp`");
        registrar.Int("count", options.Count, "run tests and benchmarks `n` times");
        registrar.Int("parallel", options.Parallel, "run at most `n` tests in parallel");
        registrar.String("shuffle", options.ShuffleValue, "randomize the execution order of tests and benchmarks");
        registrar.Duration("timeout", options.Timeout, "panic test binary after duration `d`");
    }

    /// <summary>
    /// Runs the converted <c>flag.Parse()</c> if nothing has parsed yet — Go's <c>m.Run</c> step
    /// (<c>if !flag.Parsed() { flag.Parse() }</c>), which is what actually writes the command
    /// line's VALUES into the registered flags. A no-op when the converted <c>flag</c> package is
    /// absent, exactly like <see cref="Register"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without this step a package that declares its own test flags but has NO <c>TestMain</c>
    /// never parses them: registration alone leaves every custom flag at its default, which is a
    /// silently wrong program rather than an error. Measured on <c>os/signal</c>
    /// (2026-08-27): <c>TestDetectNohup</c> re-execs itself with <c>-check_sighup_ignored</c>, the
    /// child's <c>*checkSighupIgnored</c> read <c>false</c>, so the child ran the PARENT branch and
    /// re-exec'd again — an unbounded spawn recursion that ate the package deadline and reported as
    /// a hang. In Go the flag is true in the child because <c>m.Run</c> parses; this is that parse.
    /// </para>
    /// <para>
    /// Ordering with a package's own <c>TestMain</c> is preserved in effect: a <c>TestMain</c> that
    /// calls <c>flag.Parse()</c> re-parses the same arguments into the same definitions (the call
    /// is idempotent over an unchanged command line), and one that never calls it was relying on
    /// <c>m.Run</c>'s parse — which is exactly the step being restored. Both vocabularies are on the
    /// converted flag package by the time this runs — the package's own flags via
    /// <c>InitializePackageUnderTest</c>, the host's via <see cref="Register"/> — so this parse sees
    /// exactly the one combined flag set Go's single <c>flag.Parse()</c> sees. An UNDEFINED name
    /// reaches it too, deliberately (the host stopped ruling on those, 2026-08-28): <c>flag</c> then
    /// applies Go's own contract — its message, the package's <c>flag.Usage</c>, and
    /// <c>ExitOnError</c>'s status — which is the only way a package that overrides <c>Usage</c>
    /// (crypto/tls's bogo <c>os.Exit(89)</c>) can behave as Go specifies.
    /// </para>
    /// </remarks>
    public static void Parse()
    {
        Type? flagPackage = Type.GetType(FlagPackageTypeName, throwOnError: false);

        if (flagPackage is null)
            return;

        MethodInfo parsed = Bind(flagPackage, "Parsed");

        if (parsed.Invoke(null, []) is bool alreadyParsed && alreadyParsed)
            return;

        Bind(flagPackage, "Parse").Invoke(null, []);
    }

    /// <summary>
    /// Whether the converted <c>flag.CommandLine</c> defines <paramref name="name"/> — the question
    /// that decides an unrecognized host flag, once the package under test has initialized.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>False when the converted <c>flag</c> package is absent, and that is the right answer
    /// rather than a fallback.</b> A package that does not import <c>flag</c> cannot have declared a
    /// flag, so no name the host does not own can belong to it, and the host's rejection of a typo
    /// stays exactly as strict as it was for the 124 of 141 test projects in that position.
    /// </para>
    /// <para>
    /// Only DEFINEDNESS is asked. Whether the flag is boolean — Go's <c>parseOne</c> consults
    /// <c>boolFlag.IsBoolFlag()</c> to decide if the next token is its value — is deliberately not,
    /// because <see cref="TestOptions.Parse"/> has already stopped by the time this runs and left
    /// the remainder to the program's own <c>flag.Parse()</c>, which answers it with the real flag
    /// set instead of through reflection over a generated interface wrapper.
    /// </para>
    /// </remarks>
    public static bool IsDefined(string name)
    {
        Type? flagPackage = Type.GetType(FlagPackageTypeName, throwOnError: false);

        return flagPackage is not null &&
               BindLookup(flagPackage).Invoke(null, [(@string)name]) is not null;
    }

    private static MethodInfo BindLookup(Type flagPackage) =>
        Bind(flagPackage, "Lookup", typeof(@string));

    private static MethodInfo Bind(Type flagPackage, string name, params Type[] parameterTypes) =>
        flagPackage.GetMethod(name, BindingFlags.Public | BindingFlags.Static, binder: null, parameterTypes, modifiers: null) ??
        throw new InvalidOperationException(
            $"testing: the converted flag package does not declare {name}({string.Join(", ", parameterTypes.Select(type => type.Name))}) — " +
            "the test host cannot declare its own command line and a converted TestMain calling flag.Parse() would reject it");

    /// <summary>
    /// The converted <c>flag</c> package's typed package-level registrars, bound once by name.
    /// </summary>
    /// <remarks>
    /// The TYPED registrars are used rather than <c>flag.Func</c>/<c>flag.BoolFunc</c>, whose
    /// <c>funcValue</c> reports an empty <c>String()</c> and is not a <c>flag.Getter</c>. Converted
    /// tests read both — <c>os/exec</c> calls <c>.Value.String()</c> and <c>runtime</c> asserts
    /// <c>.Value.(flag.Getter).Get().(int)</c> — so a definition that parses but cannot report its
    /// value would satisfy <c>flag.Parse</c> and still fail the tests that look.
    /// </remarks>
    private readonly struct Registrar
    {
        private readonly MethodInfo m_lookup;
        private readonly MethodInfo m_bool;
        private readonly MethodInfo m_int;
        private readonly MethodInfo m_uint;
        private readonly MethodInfo m_string;
        private readonly MethodInfo m_duration;

        public Registrar(Type flagPackage)
        {
            m_lookup = BindLookup(flagPackage);
            m_bool = Bind(flagPackage, "Bool", typeof(@string), typeof(bool), typeof(@string));
            m_int = Bind(flagPackage, "Int", typeof(@string), typeof(nint), typeof(@string));
            m_uint = Bind(flagPackage, "Uint", typeof(@string), typeof(nuint), typeof(@string));
            m_string = Bind(flagPackage, "String", typeof(@string), typeof(@string), typeof(@string));
            m_duration = Bind(flagPackage, "Duration", typeof(@string), typeof(time_package.Duration), typeof(@string));
        }

        public void Bool(string name, bool value, string usage) => Define(m_bool, name, value, usage);

        public void Int(string name, nint value, string usage) => Define(m_int, name, value, usage);

        public void Uint(string name, nuint value, string usage) => Define(m_uint, name, value, usage);

        public void String(string name, string value, string usage) => Define(m_string, name, (@string)value, usage);

        // time.Duration counts NANOSECONDS; a TimeSpan tick is 100 of them.
        public void Duration(string name, TimeSpan value, string usage) =>
            Define(m_duration, name, (time_package.Duration)(value.Ticks * 100L), usage);

        private void Define(MethodInfo registrar, string name, object value, string usage)
        {
            // The converted FlagSet.Var PANICS on a redefinition, so a name the package under test
            // already defined is left alone — its own flag is the one that should win.
            if (m_lookup.Invoke(null, [(@string)name]) is not null)
                return;

            registrar.Invoke(null, [(@string)name, value, (@string)usage]);
        }
    }
}
