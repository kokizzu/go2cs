// TestExecution.cs - Gbtc
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
/// One running test or subtest — the state behind the <c>*testing.T</c> a converted test body holds.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>t.</c>-something a Go test can call lands here: <c>Fail</c>/<c>FailNow</c>,
/// <c>Skip</c>, <c>Log</c>, <c>Helper</c>, <c>Cleanup</c>, <c>Run</c> (subtests), <c>Parallel</c>,
/// <c>TempDir</c> and <c>Setenv</c>. Instances form a tree — a subtest keeps its parent — because
/// Go's naming, failure propagation and parallel release are all defined over that tree.
/// </para>
/// <para>
/// Each test gets its OWN thread rather than a pool work item, for two independent reasons. A
/// parallel test PARKS its thread until the serial phase completes, and dozens-to-hundreds of
/// parked thread-pool threads would starve the pool — stalling the suite and every converted
/// goroutine the tests spawn, since golib queues those on that same pool. And a dedicated thread
/// can be created with a deliberately huge stack reservation (see <c>TestThreadStackSize</c>):
/// Go goroutine stacks GROW, so deeply recursive test code is legal Go, while a .NET thread's
/// stack is a fixed reservation whose overflow is uncatchable and kills the entire host.
/// </para>
/// <para>
/// <c>FailNow</c> and <c>SkipNow</c> must abandon the test body immediately, which in Go is a
/// <c>runtime.Goexit</c> on the test's goroutine. Here that is <see cref="TestAbortException"/>,
/// thrown on the test's own thread and caught at its root — which is also why several members check
/// they are on the OWNING thread first: Go says those calls are only valid from the test's own
/// goroutine, and honoring them from another one would unwind a stack that is not the test's.
/// </para>
/// <para>
/// The current execution flows to goroutines through an <see cref="AsyncLocal{T}"/>, so a failure
/// raised inside a goroutine the test started can be attributed to the test that started it rather
/// than to whatever happened to be running.
/// </para>
/// </remarks>
public sealed class TestExecution
{
    // Go goroutine stacks GROW (to ~1GB on 64-bit), so deeply recursive test code is legal Go —
    // io/fs's TestCVE202230630 legitimately recurses 10,001 frames through globWithLimit before
    // its own depth guard fires. A .NET thread's stack is a FIXED reservation (default ~1MB) and
    // an overflow is uncatchable, killing the whole host. 256MB reserves address space only
    // (pages commit on demand), giving Go-scale headroom at no real memory cost.
    private const int TestThreadStackSize = 256 * 1024 * 1024;

    // The test a goroutine belongs to. Set on the test's own thread, it flows into every goroutine
    // that thread starts (and into theirs, transitively) because ThreadPool.QueueUserWorkItem —
    // golib's goroutine dispatch — captures the ExecutionContext an AsyncLocal lives in.
    private static readonly AsyncLocal<TestExecution?> s_current = new();

    private readonly TestRunner m_runner;
    private readonly TestExecution? m_parent;
    private readonly object m_syncRoot = new();
    private readonly Stack<Action> m_cleanups = new();
    private readonly List<string> m_logs = [];
    private readonly Dictionary<string, int> m_subtestNames = new(StringComparer.Ordinal);
    private readonly ManualResetEventSlim m_parallelGate = new(false);
    private int m_ownerThread;
    private int m_tempDirSequence;
    private bool m_parallel;
    private bool m_holdsParallelSlot;
    private bool m_finished;
    private bool m_failed;
    private bool m_skipped;

    internal TestExecution(TestRunner runner, string name, TestExecution? parent, string source, int line)
    {
        m_runner = runner;
        m_parent = parent;
        Name = name;
        Source = source;
        Line = line;
    }

    public string Name { get; }

    public string Source { get; }

    public int Line { get; }

    public bool Failed { get { lock (m_syncRoot) return m_failed; } }

    public bool Skipped { get { lock (m_syncRoot) return m_skipped; } }

    public bool InfrastructureFailed { get; private set; }

    internal Task Completion { get; private set; } = Task.CompletedTask;

    private TaskCompletionSource ParallelSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task ParallelReached => ParallelSource.Task;

    internal List<TestExecution> ParallelChildren { get; } = [];

    internal void Start(Action<ж<testing_package.T>> action)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Completion = completion.Task;

        // Each execution gets a DEDICATED thread rather than Task.Run: a parallel test parks its
        // thread on m_parallelGate (and then on the -parallel limiter) until the serial phase
        // completes, and dozens-to-hundreds of parked thread-pool threads would starve the pool
        // (injection is ~1 thread/s) — stalling both the suite and any converted goroutines the
        // tests spawn, which golib queues on that same pool. Dedicated threads keep test parking
        // and goroutine scheduling independent at stdlib-suite scale.
        Thread thread = new(() =>
        {
            // This dedicated thread IS the test's goroutine (Go's tRunner runs every test in one),
            // so mark it as such: runtime.Goexit — which testing.T.FailNow is specified in terms
            // of — is supported from a goroutine and gated from the main goroutine.
            using Goroutine.Scope goroutine = Goroutine.Enter();

            try
            {
                Execute(action);
            }
            catch (Exception ex)
            {
                // Execute contains its own handling; anything escaping it is a host defect. Contain
                // it here — an unhandled exception on a background thread would hit golib's
                // AppDomain backstop, which prints the report to stderr and exits 2 (like Go):
                // the whole run dies mid-flight with NO result files and every unrelated test
                // killed, instead of one attributed infrastructure failure.
                m_runner.RecordInfrastructureFailure(Name, $"test host failure: {ex}");
            }
            finally
            {
                completion.TrySetResult();
            }
        }, TestThreadStackSize)
        {
            IsBackground = true,
            Name = $"go2cs test: {Name}"
        };

        thread.Start();
    }

    internal void Wait() => Completion.GetAwaiter().GetResult();

    internal void ReleaseParallel() => m_parallelGate.Set();

    public void Fail()
    {
        lock (m_syncRoot)
        {
            if (m_finished)
                throw new InvalidOperationException($"Fail called after {Name} completed");
            m_failed = true;
        }
        m_parent?.FailFromChild();
    }

    public void FailNow()
    {
        if (!TryEnsureOwner(nameof(FailNow)))
            return;
        Fail();
        throw new TestAbortException();
    }

    public void SkipNow()
    {
        if (!TryEnsureOwner(nameof(SkipNow)))
            return;
        lock (m_syncRoot)
            m_skipped = true;
        throw new TestAbortException();
    }

    public void Log(string text)
    {
        lock (m_syncRoot)
        {
            if (m_finished)
                throw new InvalidOperationException($"Log called after {Name} completed");
            m_logs.Add(text.TrimEnd('\r', '\n'));
        }
    }

    public void Helper()
    {
        // Helper-frame elision is staged; declaration source identity is still reported.
    }

    public void Cleanup(Action cleanup)
    {
        ArgumentNullException.ThrowIfNull(cleanup);
        lock (m_syncRoot)
        {
            // A cleanup registered after the test completed can never run (the cleanup phase is
            // already over) — reject it instead of silently dropping it. Go panics here too.
            if (m_finished)
                throw new InvalidOperationException($"Cleanup called after {Name} completed");
            m_cleanups.Push(cleanup);
        }
    }

    public bool Run(string name, Action<ж<testing_package.T>> action)
    {
        if (!TryEnsureOwner(nameof(Run)))
            return false;
        return m_runner.RunChild(this, name, action);
    }

    public void Parallel()
    {
        if (!TryEnsureOwner(nameof(Parallel)))
            return;
        lock (m_syncRoot)
        {
            if (m_parallel)
                throw new InvalidOperationException($"testing: {Name} called Parallel more than once");
            m_parallel = true;
        }
        ParallelSource.TrySetResult();
        m_parallelGate.Wait();

        // Released from the serial-phase gate; now compete for a -parallel slot so at most
        // Options.Parallel parallel tests RUN simultaneously (Go's -parallel semantics).
        m_runner.AcquireParallelSlot();
        m_holdsParallelSlot = true;
    }

    public @string TempDir()
    {
        string path = Path.Combine(m_runner.WorkingDirectory, ".tmp", SanitizeName(Name), Interlocked.Increment(ref m_tempDirSequence).ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(path);
        Cleanup(() => RemoveAll(path));
        return path;
    }

    // Go's os.RemoveAll semantics for the TempDir cleanup: a reparse point (junction or symlink)
    // is removed as the link itself and never traversed, and a read-only attribute is cleared
    // before a retry the way os.Remove does on Windows. .NET's Directory.Delete(path, true)
    // instead opens some junction targets during its recursive walk -- a junction to an
    // NT-namespace volume root (filepath's TestNTNamespaceSymlink) fails it with
    // UnauthorizedAccessException where Go's cleanup succeeds.
    private static void RemoveAll(string path)
    {
        if (File.Exists(path))
        {
            DeleteEntry(new FileInfo(path));
            return;
        }

        DirectoryInfo directory = new(path);

        if (!directory.Exists)
            return;

        if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
        {
            foreach (FileSystemInfo entry in directory.GetFileSystemInfos())
            {
                if (entry is DirectoryInfo)
                    RemoveAll(entry.FullName);
                else
                    DeleteEntry(entry);
            }
        }

        DeleteEntry(directory);
    }

    private static void DeleteEntry(FileSystemInfo entry)
    {
        try
        {
            entry.Delete();
        }
        catch (UnauthorizedAccessException) when ((entry.Attributes & FileAttributes.ReadOnly) != 0)
        {
            entry.Attributes &= ~FileAttributes.ReadOnly;
            entry.Delete();
        }
    }

    public void Setenv(string key, string value)
    {
        if (!TryEnsureOwner(nameof(Setenv)))
            return;
        if (m_parallel)
            throw new InvalidOperationException("testing: t.Setenv called after t.Parallel");

        string? previous = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
        Cleanup(() => Environment.SetEnvironmentVariable(key, previous));
    }

    internal string NextSubtestName(string requested)
    {
        string baseName = SanitizeName(requested);
        lock (m_syncRoot)
        {
            int sequence = m_subtestNames.TryGetValue(baseName, out int current) ? current + 1 : 0;
            m_subtestNames[baseName] = sequence;
            // Go names EVERY empty-name subtest by its per-parent sequence alone (#00, #01, …,
            // matching Go's testing.tRunner naming) — the sequence IS the name, never a dedup
            // suffix on a shared "#00" (the old form emitted #00, #00#01, … and every subtest
            // row keyed one-sided against `go test -json`). Duplicate NON-empty names keep the
            // Go dedup form name#NN, byte-identical to before (sort.TestFind's ab/ab#01/ab#02).
            string unique = requested.Length == 0
                ? $"#{sequence:00}"
                : sequence == 0 ? baseName : $"{baseName}#{sequence:00}";
            return $"{Name}/{unique}";
        }
    }

    internal static TestExecution? Current => s_current.Value;

    internal void RecordGoroutineFailure(Exception ex)
    {
        string message = $"unhandled exception on a goroutine started by {Name}: {ex}";
        bool completed;

        lock (m_syncRoot)
        {
            completed = m_finished;

            if (!completed)
            {
                InfrastructureFailed = true;
                m_logs.Add(message);
            }
        }

        if (completed)
        {
            // The test already reported its terminal event and was counted; record the late failure
            // at the runner level so it still fails the run and is disclosed as an event.
            m_runner.RecordInfrastructureFailure(Name, message);
        }
        else
        {
            FailFromInfrastructure();
        }
    }

    private void Execute(Action<ж<testing_package.T>> action)
    {
        Stopwatch timer = Stopwatch.StartNew();
        m_ownerThread = Environment.CurrentManagedThreadId;
        s_current.Value = this;
        m_runner.Report(new TestEvent(m_runner.Package, Name, "run", Source: Source, Line: Line));

        testing_package.T t = new() { Execution = this };
        try
        {
            action(new ж<testing_package.T>(t));
        }
        catch (TestAbortException)
        {
        }
        catch (GoexitException)
        {
            // The test's goroutine ended without the test function completing — Go's tRunner
            // detects the same state and reports errNilPanicOrGoexit against the test. FailNow's
            // contract IS this (Go: mark failed, then runtime.Goexit) and it arrives above as a
            // TestAbortException, so a GoexitException here means the test body called
            // runtime.Goexit itself. Go fails the test and then panics the whole binary; failing
            // this one test and letting the run finish keeps the rest of the package measurable.
            Log("test executed panic(nil) or runtime.Goexit");
            Fail();
        }
        catch (PanicException ex)
        {
            Log($"panic: {ex.Message}\n{ex.StackTrace}");
            Fail();
        }
        catch (Exception ex) when (RuntimeErrorPanic.TryAsPanic(ex, out PanicException? panic))
        {
            Log($"panic: {panic!.Message}\n{panic.StackTrace}");
            Fail();
        }
        catch (Exception ex)
        {
            InfrastructureFailed = true;
            Log(ex.ToString());
            FailFromInfrastructure();
        }
        finally
        {
            // Give the -parallel slot back BEFORE waiting on parallel children (Go's ordering):
            // a parallel parent that held its slot while waiting would deadlock its own children
            // under a small -parallel cap.
            if (m_holdsParallelSlot)
            {
                m_holdsParallelSlot = false;
                m_runner.ReleaseParallelSlot();
            }

            foreach (TestExecution child in ParallelChildren)
                child.ReleaseParallel();
            foreach (TestExecution child in ParallelChildren)
                child.Wait();

            RunCleanups();
            timer.Stop();

            lock (m_syncRoot)
                m_finished = true;

            string terminal = InfrastructureFailed ? "infrastructure-error" : Failed ? "fail" : Skipped ? "skip" : "pass";
            string? output;
            lock (m_syncRoot)
                output = m_logs.Count == 0 ? null : string.Join(Environment.NewLine, m_logs);

            m_runner.Report(new TestEvent(m_runner.Package, Name, terminal, timer.Elapsed.TotalSeconds, output, Source, Line));
            m_runner.Completed(this);
        }
    }

    private void RunCleanups()
    {
        while (true)
        {
            Action cleanup;
            lock (m_syncRoot)
            {
                if (m_cleanups.Count == 0)
                    return;
                cleanup = m_cleanups.Pop();
            }

            try
            {
                cleanup();
            }
            catch (TestAbortException)
            {
            }
            catch (Exception ex) when (RuntimeErrorPanic.TryAsPanic(ex, out PanicException? panic))
            {
                lock (m_syncRoot)
                    m_logs.Add($"cleanup panic: {panic!.Message}\n{panic.StackTrace}");
                FailFromInfrastructure();
            }
            catch (Exception ex)
            {
                InfrastructureFailed = true;
                lock (m_syncRoot)
                    m_logs.Add($"cleanup failed: {ex}");
                FailFromInfrastructure();
            }
        }
    }

    private void FailFromChild()
    {
        lock (m_syncRoot)
            m_failed = true;
        m_parent?.FailFromChild();
    }

    private void FailFromInfrastructure()
    {
        lock (m_syncRoot)
            m_failed = true;
        m_parent?.FailFromChild();
    }

    /// <summary>
    /// Verifies the caller is the test's own goroutine for operations Go restricts to it
    /// (FailNow/SkipNow/Run/Parallel/Setenv). A violation is recorded as an infrastructure
    /// failure on THIS execution and the operation becomes a no-op — it must never throw.
    /// </summary>
    /// <remarks>
    /// Throwing here would surface inside foreign converted code: golib's GoFunc exception filter
    /// only captures panic-convertible exceptions (its comment: "Non-panic exceptions fail the
    /// filter and propagate unchanged"), converted goroutines are queued on the bare thread pool
    /// (builtin.goǃ), and golib's AppDomain.UnhandledException backstop prints the report to
    /// stderr and exits 2 (like Go) — terminating the whole run mid-flight with NO result files
    /// and every unrelated test killed. Routing to an infrastructure failure keeps the misuse
    /// disclosed (req §6.1) without throwing into code that cannot handle it. (Fail/Error/Log
    /// intentionally have no owner check — Go permits them from any goroutine.)
    /// </remarks>
    private bool TryEnsureOwner(string operation)
    {
        if (Environment.CurrentManagedThreadId == m_ownerThread)
            return true;

        string message = $"testing: {operation} called from a goroutine other than the test goroutine for {Name}";
        bool completed;

        lock (m_syncRoot)
        {
            completed = m_finished;

            if (!completed)
            {
                InfrastructureFailed = true;
                m_logs.Add(message);
            }
        }

        if (completed)
        {
            // The owning execution already reported its terminal event and was counted — record
            // the late misuse at the runner level so it still fails the run and emits an event.
            m_runner.RecordInfrastructureFailure(Name, message);
        }
        else
        {
            FailFromInfrastructure();
        }

        return false;
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "#00";
        return string.Concat(value.Select(ch => char.IsWhiteSpace(ch) ? '_' : char.IsControl(ch) ? '\uFFFD' : ch));
    }
}

/// <summary>
/// Control flow, not an error: thrown to abandon a test body at <c>t.FailNow()</c> / <c>t.SkipNow()</c>,
/// and caught at the root of that test's own thread.
/// </summary>
/// <remarks>
/// Go implements both with <c>runtime.Goexit</c>, which unwinds the test's goroutine while still
/// running its deferred calls. An exception is the managed equivalent — it unwinds and runs
/// <c>finally</c> blocks — and it stays on the test's OWN thread, which is why the members that
/// throw it verify thread ownership first. It carries no message because nothing reports it: the
/// verdict was already recorded before the throw, and reaching a handler that treats this as a
/// failure REASON means it escaped the thread it belongs to.
/// </remarks>
internal sealed class TestAbortException : Exception;
