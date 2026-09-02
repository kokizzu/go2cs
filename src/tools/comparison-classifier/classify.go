// classify.go — semantic-bill run-layer classification instrument.
//
// Buckets every non-matching row of a package's go2cs_test_comparison.json by
// FAILURE MECHANISM, cross-referencing the C# test host's own go2cs_test_results.json
// event stream for per-test diagnostic detail. Read-only; writes nothing back.
//
// Usage: comparison-classifier <package-dir> [<package-dir> ...]
//
// Implements two standing doctrines from this repo's CLAUDE.md:
//   - tail-first: the LAST event in results.json's event stream is checked before
//     any per-test analysis, since a package-level timeout or crash announces
//     itself there explicitly.
//   - freshness: a results.json older than its sibling comparison.json is flagged
//     rather than trusted, per "a FAILED -tests BUILD leaves the PREVIOUS
//     comparison record in place."
//
// Mechanism taxonomy, and what each one has actually been run against. Two real
// corpus records exist as of this build (unicode/utf8, fully clean; runtime, a
// whole-host crash) — everything else below is confirmed only against hand-built
// fixtures that reproduce the real schema exactly (see testdata/ beside this file),
// not yet against a live divergent record. That distinction matters: a fixture
// proves the CODE PATH is reachable and does the right thing with that shape of
// input; it does not prove the shape occurs in the real corpus, or occurs the way
// the fixture assumes. Treat every "fixture-verified" mechanism below as untested
// against reality until a real package exercises it.
//   - clean                  : matched=true, errors=[] (LIVE: unicode/utf8)
//   - host-crash-at-init     : comparison errors[] carries a "converted tests: ...
//                              failed: exit status N" entry with a .NET stack-trace
//                              shape (LIVE: runtime, the getg NotImplementedException
//                              record). Package-level, reported regardless of whether
//                              results.json loaded: when it's absent this is the only
//                              signal and classification stops there; when it DID
//                              load (a real, partial record up to the crash), this is
//                              reported ALONGSIDE the per-test rows rather than
//                              instead of them (LIVE, second shape: runtime again —
//                              a goroutine panic in TestCaller took the whole host
//                              down mid-run, after 849 real per-test rows had
//                              already been produced; see testdata/runtime-panic/)
//   - timeout                : results.json's last event is
//                              {"test":"","action":"timeout"} (fixture-verified
//                              against the doctrine's documented shape)
//   - native-fault           : results.json exists but does not end in a
//                              package-level terminal event — mid-run death, not
//                              at init (fixture-verified; also confirmed it falls
//                              through to per-test analysis afterward rather than
//                              stopping, since whatever ran before the death is
//                              still real data)
//   - notimpl-stub-by-name   : a per-test mismatch whose C# terminal event's Output
//                              contains "NotImplementedException" (fixture-verified;
//                              the exception SHAPE is the same one seen live in
//                              runtime's host-level crash, but that was a whole-host
//                              death, not a per-test failure — this is reasoning by
//                              analogy, not the same evidence)
//   - go-panic-text          : a per-test mismatch whose Output contains "panic:"
//                              (fixture-verified)
//   - assertion-mismatch     : a per-test mismatch with any other Output (first
//                              line extracted) (fixture-verified)
//   - empty-unreached        : Go side has a verdict, C# side is "" / absent, and
//                              no results.json event exists for that test at all
//                              (LIVE for the "no results.json at all" shape: every
//                              one of runtime's ~880 entries hit this via the
//                              host-crash-at-init short-circuit, which reports them
//                              as ONE root-cause finding rather than 880 phantom
//                              ones — see hostCrashAtInit's early return. The
//                              "results.json EXISTS but this one test has no event
//                              in it" variant is fixture-verified only)
//   - empty-in-progress-killed: fixture-verified
//   - empty-go-side          : fixture-verified
//   - unclassified           : a mismatch this tool could not place in any bucket
//                              above; always reported by name rather than silently
//                              dropped (fixture-verified via a deliberately
//                              malformed errors[] line)
//
// NOT implemented: the parallel-set-equality collapse for scattered empty-unreached
// findings (CLAUDE.md's "compare the empty set against the t.Parallel() set before
// reading scattered as genuine divergence" doctrine) — that needs the original Go
// test source to know which tests are parallel, which this tool does not read. A
// package whose empties are actually one serial-phase death will currently be
// reported as N separate empty-unreached findings rather than collapsed to one.
// Flag this explicitly in any report this tool produces; do not silently undercount
// the phantom-finding risk it names.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"time"
)

type ComparisonFile struct {
	Package   string            `json:"package"`
	Status    string            `json:"status"`
	Go        map[string]string `json:"go"`
	CSharp    map[string]string `json:"csharp"`
	Matched   bool              `json:"matched"`
	Skipped   []string          `json:"skipped"`
	Disclosed []string          `json:"disclosed"`
	Excluded  []string          `json:"excluded"`
	Errors    []string          `json:"errors"`
}

type ResultEvent struct {
	Package string  `json:"package"`
	Test    string  `json:"test"`
	Action  string  `json:"action"`
	Elapsed float64 `json:"elapsed"`
	Output  *string `json:"output"`
	Source  *string `json:"source"`
	Line    *int    `json:"line"`
}

type ResultsFile struct {
	SchemaVersion int           `json:"schemaVersion"`
	Package       string        `json:"package"`
	Events        []ResultEvent `json:"events"`
}

type Finding struct {
	Test      string
	Mechanism string
	Detail    string
}

var errorLinePattern = regexp.MustCompile(`^(.+?): Go="([^"]*)" C#="([^"]*)"$`)

func main() {
	if len(os.Args) < 2 {
		fmt.Fprintln(os.Stderr, "usage: comparison-classifier <package-dir> [<package-dir> ...]")
		os.Exit(2)
	}

	exitCode := 0

	for _, dir := range os.Args[1:] {
		if err := classifyPackage(dir); err != nil {
			fmt.Fprintf(os.Stderr, "%s: %v\n", dir, err)
			exitCode = 1
		}
	}

	os.Exit(exitCode)
}

func classifyPackage(dir string) error {
	comparisonPath := filepath.Join(dir, "go2cs_test_comparison.json")
	resultsPath := filepath.Join(dir, "go2cs_test_results.json")

	comparisonBytes, err := os.ReadFile(comparisonPath)
	if err != nil {
		return fmt.Errorf("reading comparison file: %w", err)
	}

	var comparison ComparisonFile
	if err := json.Unmarshal(comparisonBytes, &comparison); err != nil {
		return fmt.Errorf("parsing comparison file: %w", err)
	}

	fmt.Printf("=== %s (status=%q, matched=%v) ===\n", comparison.Package, comparison.Status, comparison.Matched)

	if comparison.Matched && len(comparison.Errors) == 0 {
		fmt.Println("clean — no non-matching rows")
		return nil
	}

	results, resultsErr := loadResults(resultsPath)
	freshnessWarning := ""

	if resultsErr == nil {
		if compStat, err1 := os.Stat(comparisonPath); err1 == nil {
			if resStat, err2 := os.Stat(resultsPath); err2 == nil {
				if resStat.ModTime().Before(compStat.ModTime()) {
					freshnessWarning = fmt.Sprintf(
						"WARNING: results.json is OLDER than comparison.json (%s vs %s) — "+
							"this may be a stale record from a build that failed before rewriting it; "+
							"do not trust per-test cross-references below without re-running",
						resStat.ModTime().Format(time.RFC3339), compStat.ModTime().Format(time.RFC3339))
				}
			}
		}
	}

	if freshnessWarning != "" {
		fmt.Println(freshnessWarning)
	}

	// Tail-first: a package-level timeout or abrupt death announces itself in the
	// LAST event before any per-test analysis is worth doing.
	if resultsErr == nil && len(results.Events) > 0 {
		last := results.Events[len(results.Events)-1]

		if last.Test == "" && last.Action == "timeout" {
			out := ""
			if last.Output != nil {
				out = *last.Output
			}

			fmt.Printf("PACKAGE-LEVEL: timeout — %s\n", out)

			return nil
		}

		if last.Test != "" || (last.Action != "pass" && last.Action != "fail" && last.Action != "skip") {
			fmt.Printf("PACKAGE-LEVEL: native-fault — results.json does not end in a package-level "+
				"terminal event (last event: test=%q action=%q) — the host likely died mid-run rather than at init\n",
				last.Test, last.Action)
			// Continue to per-test analysis anyway: whatever ran before the death is still real data.
		}
	}

	// The host-crash-at-init signature is a PACKAGE-LEVEL event whether or not
	// results.json loaded. When results.json is absent, this is the only signal
	// available and there is nothing further to classify. When results.json DID
	// load, the crash line still belongs in errors[] — the pipeline can write a
	// real, partial results.json right up to the moment the whole process exits
	// non-zero (e.g. an unhandled goroutine panic) — so it is reported ALONGSIDE
	// the per-test rows below, not instead of them. Skipped from the per-line
	// loop afterward so it isn't ALSO reported as "unclassified".
	crashDetail, isCrash := hostCrashAtInit(comparison.Errors)

	if isCrash {
		fmt.Printf("PACKAGE-LEVEL: host-crash-at-init — comparison errors[] carries the process-invocation "+
			"failure (results.json %s):\n  %s\n", resultsLoadState(resultsErr), truncate(crashDetail, 400))

		if resultsErr != nil {
			return nil
		}
	} else if resultsErr != nil {
		fmt.Printf("results.json unavailable (%v) and no host-crash signature found in comparison errors[] — "+
			"treating remaining errors[] entries as unclassified\n", resultsErr)
	}

	findings := classifyErrors(comparison, results, crashDetail)

	reportFindings(findings)

	return nil
}

// resultsLoadState renders whether results.json loaded, for the package-level
// host-crash-at-init line -- absent (nothing further to classify) vs loaded
// (a real, partial record exists alongside the crash and is reported below).
func resultsLoadState(resultsErr error) string {
	if resultsErr != nil {
		return "absent"
	}

	return "loaded"
}

func loadResults(path string) (*ResultsFile, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}

	var results ResultsFile
	if err := json.Unmarshal(data, &results); err != nil {
		return nil, fmt.Errorf("parsing results file: %w", err)
	}

	return &results, nil
}

// hostCrashAtInit recognizes the "converted tests: ... failed: exit status N" shape
// this repo's -tests pipeline writes into comparison errors[] when the C# host dies
// before it can write its own results.json at all (verified live against runtime's
// getg NotImplementedException record — the host died in a static constructor,
// before Main() and before any per-test event could be emitted).
func hostCrashAtInit(errors []string) (string, bool) {
	for _, e := range errors {
		if strings.Contains(e, "converted tests:") && strings.Contains(e, "failed: exit status") {
			return e, true
		}
	}

	return "", false
}

func classifyErrors(comparison ComparisonFile, results *ResultsFile, skipLine string) []Finding {
	var eventsByTest map[string][]ResultEvent

	if results != nil {
		eventsByTest = make(map[string][]ResultEvent)

		for _, ev := range results.Events {
			if ev.Test == "" {
				continue
			}

			eventsByTest[ev.Test] = append(eventsByTest[ev.Test], ev)
		}
	}

	findings := make([]Finding, 0, len(comparison.Errors))

	for _, line := range comparison.Errors {
		// Already reported as the PACKAGE-LEVEL host-crash-at-init line above --
		// skip it here so it isn't ALSO counted as a per-line "unclassified" row.
		if skipLine != "" && line == skipLine {
			continue
		}

		m := errorLinePattern.FindStringSubmatch(line)
		if m == nil {
			findings = append(findings, Finding{
				Mechanism: "unclassified",
				Detail:    "errors[] entry does not match the \"<Test>: Go=\\\"X\\\" C#=\\\"Y\\\"\" shape: " + truncate(line, 200),
			})

			continue
		}

		test, goVerdict, csVerdict := m[1], m[2], m[3]
		findings = append(findings, classifyOneMismatch(test, goVerdict, csVerdict, eventsByTest))
	}

	return findings
}

func classifyOneMismatch(test, goVerdict, csVerdict string, eventsByTest map[string][]ResultEvent) Finding {
	events := eventsByTest[test]

	if csVerdict == "" && goVerdict != "" {
		if len(events) == 0 {
			return Finding{Test: test, Mechanism: "empty-unreached",
				Detail: fmt.Sprintf("Go=%q, no C# event at all — never started (host died/timed out before its turn)", goVerdict)}
		}

		// Started but no terminal outcome recorded.
		return Finding{Test: test, Mechanism: "empty-in-progress-killed",
			Detail: fmt.Sprintf("Go=%q, %d C# event(s) exist but none reached a pass/fail/skip terminal — "+
				"likely cut off mid-test by a native fault or timeout", goVerdict, len(events))}
	}

	if goVerdict == "" && csVerdict != "" {
		return Finding{Test: test, Mechanism: "empty-go-side",
			Detail: fmt.Sprintf("C#=%q, no Go verdict recorded — unusual; not a mechanism this tool has a live example of", csVerdict)}
	}

	// Both sides have a verdict but they differ. Find the C# terminal event's Output
	// for the actual diagnostic text.
	var output string

	for _, ev := range events {
		if ev.Action == "fail" || ev.Action == "panic" {
			if ev.Output != nil {
				output = *ev.Output
			}

			break
		}
	}

	switch {
	case strings.Contains(output, "NotImplementedException"):
		return Finding{Test: test, Mechanism: "notimpl-stub-by-name",
			Detail: fmt.Sprintf("Go=%q C#=%q — %s", goVerdict, csVerdict, extractStubName(output))}
	case strings.Contains(output, "panic:"):
		return Finding{Test: test, Mechanism: "go-panic-text",
			Detail: fmt.Sprintf("Go=%q C#=%q — %s", goVerdict, csVerdict, firstLine(output))}
	case output != "":
		return Finding{Test: test, Mechanism: "assertion-mismatch",
			Detail: fmt.Sprintf("Go=%q C#=%q — first differing line: %s", goVerdict, csVerdict, firstLine(output))}
	default:
		return Finding{Test: test, Mechanism: "unclassified",
			Detail: fmt.Sprintf("Go=%q C#=%q — no C# event Output captured to classify against", goVerdict, csVerdict)}
	}
}

func extractStubName(output string) string {
	idx := strings.Index(output, "NotImplementedException")
	if idx < 0 {
		return firstLine(output)
	}

	rest := output[idx:]
	if nl := strings.IndexAny(rest, "\r\n"); nl >= 0 {
		rest = rest[:nl]
	}

	return rest
}

func firstLine(s string) string {
	if nl := strings.IndexAny(s, "\r\n"); nl >= 0 {
		s = s[:nl]
	}

	return truncate(s, 300)
}

func truncate(s string, n int) string {
	if len(s) <= n {
		return s
	}

	return s[:n] + fmt.Sprintf("... [%d more chars]", len(s)-n)
}

func reportFindings(findings []Finding) {
	byMechanism := make(map[string][]Finding)

	for _, f := range findings {
		byMechanism[f.Mechanism] = append(byMechanism[f.Mechanism], f)
	}

	mechanisms := make([]string, 0, len(byMechanism))
	for m := range byMechanism {
		mechanisms = append(mechanisms, m)
	}

	sort.Strings(mechanisms)

	for _, m := range mechanisms {
		fs := byMechanism[m]
		fmt.Printf("%-28s %d\n", m, len(fs))

		for _, f := range fs {
			if f.Test != "" {
				fmt.Printf("  %-40s %s\n", f.Test, f.Detail)
			} else {
				fmt.Printf("  %s\n", f.Detail)
			}
		}
	}

	fmt.Printf("total non-matching rows: %d\n", len(findings))
}
