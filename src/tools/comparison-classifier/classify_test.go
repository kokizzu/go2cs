package main

import (
	"bytes"
	"io"
	"os"
	"strings"
	"testing"
)

// captureStdout runs fn with os.Stdout redirected to a pipe and returns everything
// written to it. classifyPackage/reportFindings write straight to os.Stdout rather
// than an io.Writer parameter, so this is the cheapest way to assert on their output
// without restructuring the tool the census/report code already works.
func captureStdout(t *testing.T, fn func()) string {
	t.Helper()

	r, w, err := os.Pipe()
	if err != nil {
		t.Fatalf("os.Pipe: %v", err)
	}

	orig := os.Stdout
	os.Stdout = w

	done := make(chan string, 1)
	go func() {
		var buf bytes.Buffer
		io.Copy(&buf, r)
		done <- buf.String()
	}()

	fn()

	os.Stdout = orig
	w.Close()

	out := <-done
	r.Close()

	return out
}

// TestClassifyPackage_Clean covers the "clean" bucket: matched=true, errors=[].
// LIVE-verified elsewhere against unicode/utf8 (see the package doc comment); this
// fixture pins the shape as a repeatable test.
func TestClassifyPackage_Clean(t *testing.T) {
	out := captureStdout(t, func() {
		if err := classifyPackage("testdata/clean"); err != nil {
			t.Fatalf("classifyPackage: %v", err)
		}
	})

	if !strings.Contains(out, "clean — no non-matching rows") {
		t.Errorf("expected the clean short-circuit message, got:\n%s", out)
	}

	if strings.Contains(out, "total non-matching rows:") {
		t.Errorf("clean package must never reach reportFindings, got:\n%s", out)
	}
}

// TestClassifyPackage_Mixed exercises six mechanisms in one comparison file: two
// results.json shapes for an unreached test (no event at all, vs. a "run" event with
// no terminal), the three per-test Output-text mechanisms (NotImplementedException,
// "panic:", plain assertion text), an empty-go-side row, and a malformed errors[]
// line that must fall back to "unclassified" rather than being silently dropped.
func TestClassifyPackage_Mixed(t *testing.T) {
	out := captureStdout(t, func() {
		if err := classifyPackage("testdata/mixed"); err != nil {
			t.Fatalf("classifyPackage: %v", err)
		}
	})

	wantMechanisms := []string{
		"empty-unreached",
		"empty-in-progress-killed",
		"notimpl-stub-by-name",
		"go-panic-text",
		"assertion-mismatch",
		"empty-go-side",
		"unclassified",
	}

	for _, m := range wantMechanisms {
		if !strings.Contains(out, m) {
			t.Errorf("expected mechanism %q in output, got:\n%s", m, out)
		}
	}

	if !strings.Contains(out, "total non-matching rows: 7") {
		t.Errorf("expected 7 non-matching rows (TestClean is not one of them), got:\n%s", out)
	}
}

// TestClassifyPackage_NativeFault covers a results.json that exists but does not end
// in a package-level terminal event (test="", action=pass/fail/skip) -- a mid-run
// death rather than a crash at init. The tool must report the PACKAGE-LEVEL native-
// fault line AND continue to per-test analysis: TestAfterCrash has one "run" event
// and no terminal, which is empty-in-progress-killed, not empty-unreached.
func TestClassifyPackage_NativeFault(t *testing.T) {
	out := captureStdout(t, func() {
		if err := classifyPackage("testdata/nativefault"); err != nil {
			t.Fatalf("classifyPackage: %v", err)
		}
	})

	if !strings.Contains(out, "PACKAGE-LEVEL: native-fault") {
		t.Errorf("expected the native-fault package-level line, got:\n%s", out)
	}

	if !strings.Contains(out, "empty-in-progress-killed") {
		t.Errorf("expected TestAfterCrash classified as empty-in-progress-killed (it has a run event), got:\n%s", out)
	}

	if strings.Contains(out, "empty-unreached") {
		t.Errorf("TestAfterCrash has a run event and must not classify as empty-unreached, got:\n%s", out)
	}
}

// TestClassifyPackage_RuntimePanic is a REAL fixture, not a hand-built one: the
// actual go2cs_test_comparison.json/go2cs_test_results.json from a runtime -tests
// compare run that hit a genuine nil-pointer panic in TestCaller on a goroutine,
// taking the whole host down mid-run (849 real rows already produced by then).
// This is the shape that exposed the classifier's original gap: the
// host-crash-at-init signature landed in comparison.errors[] ALONGSIDE a results.json
// that loaded fine, and the tool used to only check that signature when results.json
// failed to load at all -- so it fell through to "unclassified" instead of being
// reported as the package-level event it is. Locks in the fix.
func TestClassifyPackage_RuntimePanic(t *testing.T) {
	out := captureStdout(t, func() {
		if err := classifyPackage("testdata/runtime-panic"); err != nil {
			t.Fatalf("classifyPackage: %v", err)
		}
	})

	if !strings.Contains(out, "PACKAGE-LEVEL: host-crash-at-init") {
		t.Errorf("expected the package-level host-crash-at-init line even though results.json loaded, got first 500 chars:\n%s", firstN(out, 500))
	}

	if !strings.Contains(out, "results.json loaded") {
		t.Errorf("expected the package-level line to state results.json loaded (not absent), got first 500 chars:\n%s", firstN(out, 500))
	}

	// The per-test rows must still be there, alongside the package-level line --
	// this is the whole point of the fix, not just the line's presence.
	if !strings.Contains(out, "TestCaller") {
		t.Errorf("expected TestCaller's own per-test row to still be reported alongside the package-level line, got:\n%s", out)
	}

	if !strings.Contains(out, "go-panic-text") {
		t.Errorf("expected TestCaller classified go-panic-text (real panic text captured), got:\n%s", out)
	}

	if !strings.Contains(out, "empty-unreached") {
		t.Errorf("expected the 800+ shadow rows still classified as empty-unreached, got:\n%s", out)
	}

	// The crash line must be reported exactly ONCE -- not also duplicated as an
	// "unclassified" per-line row now that classifyErrors skips it.
	if n := strings.Count(out, "converted tests:"); n != 1 {
		t.Errorf("expected the host-crash-at-init errors[] line to appear exactly once (package-level only, not also duplicated into unclassified), got %d occurrences", n)
	}
}

// firstN returns the first n bytes of s, for a bounded test-failure message
// against an 850-row real fixture's full output.
func firstN(s string, n int) string {
	if len(s) <= n {
		return s
	}

	return s[:n] + "... [truncated]"
}

// TestClassifyPackage_Timeout covers the tail-first doctrine's primary case: a
// results.json whose last event is {"test":"","action":"timeout"}. The tool must
// report the package-level timeout line and return WITHOUT descending into per-test
// analysis at all (a killed package's remaining tests never got a real verdict).
func TestClassifyPackage_Timeout(t *testing.T) {
	out := captureStdout(t, func() {
		if err := classifyPackage("testdata/timeout"); err != nil {
			t.Fatalf("classifyPackage: %v", err)
		}
	})

	if !strings.Contains(out, "PACKAGE-LEVEL: timeout") {
		t.Errorf("expected the package-level timeout line, got:\n%s", out)
	}

	if !strings.Contains(out, "package timeout after 00:30:00") {
		t.Errorf("expected the timeout event's own Output text quoted, got:\n%s", out)
	}

	if strings.Contains(out, "total non-matching rows:") {
		t.Errorf("a package-level timeout must short-circuit before per-test analysis, got:\n%s", out)
	}
}

// TestClassifyPackage_MissingDir covers the error path: no comparison file at all.
func TestClassifyPackage_MissingDir(t *testing.T) {
	if err := classifyPackage("testdata/does-not-exist"); err == nil {
		t.Fatal("expected an error for a missing comparison file, got nil")
	}
}

// TestHostCrashAtInit pins the exact signature this repo's -tests pipeline writes
// when the C# host dies before it can produce a results.json at all -- verified live
// against runtime's own getg NotImplementedException record (see the package doc
// comment). A close-but-not-matching line must NOT be recognized as a crash.
func TestHostCrashAtInit(t *testing.T) {
	tests := []struct {
		name    string
		errors  []string
		wantHit bool
	}{
		{
			name:    "real signature",
			errors:  []string{`converted tests: dotnet run --project host.csproj failed: exit status 134`},
			wantHit: true,
		},
		{
			name:    "unrelated error",
			errors:  []string{`TestFoo: Go="pass" C#="fail"`},
			wantHit: false,
		},
		{
			name:    "half the signature only",
			errors:  []string{`converted tests: something went wrong but did not name an exit status`},
			wantHit: false,
		},
		{
			name:    "no errors at all",
			errors:  nil,
			wantHit: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			_, hit := hostCrashAtInit(tt.errors)
			if hit != tt.wantHit {
				t.Errorf("hostCrashAtInit(%v) hit = %v, want %v", tt.errors, hit, tt.wantHit)
			}
		})
	}
}
