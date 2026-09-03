// displacedCommentDrain_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Guards the comment sink across a DISPLACED declaration in `-comments` output.
//
// A function registered in manualConversionFuncs emits a one-line placeholder and returns, which
// skips both of the places a converted declaration drains the free-floating-comment sink: the
// writeDoc that flushes the comments standing ahead of the declaration, and the body visit that
// flushes the ones inside it. The sink's drain is POSITIONAL, so nothing was lost — everything the
// displaced declaration failed to claim was flushed by the NEXT declaration's own writeDoc, which
// takes every comment positioned before it. The corpus carried both halves of the resulting
// misplacement: syscall_linux's recvmsgRaw body comment (`// receive at least one normal byte`)
// emitted immediately above `sendmsgN`, reading as its doc comment, and runtime mbitmap's
// getgcmask — the last declaration of its file, so its 32 body-comment lines were flushed by the
// end-of-file drain and landed after the class's closing brace.
//
// The invariant pinned here is the one that survives an emission change: a declaration FOLLOWING a
// displaced one carries its own doc comment and nothing else, the displaced body's commentary
// appears nowhere, and a comment that stood ahead of the displaced declaration stays ahead of its
// placeholder.

package main

import (
	"strings"
	"testing"
)

// displacedFixture registers displaced free functions for the fixture module's package path for
// the duration of one test. The registry is a package-level map with no seam to convert through,
// and the whole point of the guard is to exercise the REAL emission path, so the entry is injected
// and restored rather than faked. Cleanup runs before any other test observes it — the registry's
// own guards (manualConversionDestination_test, manualConversionScope_test) walk this same map and
// would rightly reject a fixture package that has no hand-owned file behind it.
func displacedFixture(t *testing.T, pkgPath string, funcNames ...string) {
	t.Helper()

	previous, existed := manualConversionFuncs[pkgPath]
	entry := map[string]goosScope{}

	for _, name := range funcNames {
		entry[name] = goosAny
	}

	manualConversionFuncs[pkgPath] = entry

	t.Cleanup(func() {
		if existed {
			manualConversionFuncs[pkgPath] = previous
			return
		}

		delete(manualConversionFuncs, pkgPath)
	})
}

// requireAbsent asserts no emitted line carries the text — the retired half of the fix, and the
// only assertion that can catch a "drain it somewhere" non-fix.
func requireAbsent(t *testing.T, lines []string, text string) {
	t.Helper()

	for i, line := range lines {
		if strings.Contains(line, text) {
			t.Errorf("comment %q from a displaced body survived into the emission at line %d: %q", text, i+1, line)
		}
	}
}

// TestDisplacedFuncDoesNotStealTheNextDeclarationsComments is the syscall_linux shape: a displaced
// declaration sits between two converted ones, so its unclaimed comments surfaced under the
// FOLLOWING declaration.
func TestDisplacedFuncDoesNotStealTheNextDeclarationsComments(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	displacedFixture(t, "example.com/app", "displacedSum")

	lines := convertWithComments(t, `package main

// A standing note that is not attached to anything — a blank line separates it from the
// declaration below, so Go leaves it free-floating rather than making it a doc comment.

// displacedSum is hand-converted; the placeholder stands in for this body.
func displacedSum(n int) int {
	total := 0
	// tally the run inside the displaced body
	for i := 0; i < n; i++ {
		total += i
	}
	return total
}

func undocumentedNext(n int) int {
	return n + 1
}

// documentedLast keeps its own doc comment.
func documentedLast(n int) int {
	return n + 2
}

func main() {
	_ = documentedLast(undocumentedNext(1))
}
`)

	placeholder := findCommentLine(t, lines, "func displacedSum is hand-converted")

	// The displaced body's own commentary documents code this file does not contain.
	requireAbsent(t, lines, "tally the run inside the displaced body")

	// A comment that stood AHEAD of the displaced declaration still belongs ahead of it — the
	// same drain a converted declaration performs, which the early return also used to skip.
	standing := findCommentLine(t, lines, "A standing note that is not attached to anything")

	if standing > placeholder {
		t.Errorf("a comment preceding the displaced declaration was flushed AFTER its placeholder (comment line %d, placeholder line %d)", standing+1, placeholder+1)
	}

	// The DISCRIMINATING assertion, and the corpus shape it is drawn from: syscall_linux's
	// sendmsgN carries no doc comment of its own, so the displaced recvmsgRaw's stranded body
	// comment landed directly above its signature and read as its documentation. A declaration
	// Go left undocumented must be emitted undocumented.
	undocumented := findDeclarationLine(t, lines, "undocumentedNext(")

	if preceding := strings.TrimSpace(lines[undocumented-1]); strings.HasPrefix(preceding, "//") {
		t.Errorf("an undocumented declaration following a displaced one acquired a doc comment — the line above %q is %q", strings.TrimSpace(lines[undocumented]), preceding)
	}

	// The weaker half, kept because it is the invariant the defect threatened rather than the one
	// it happened to break: a documented declaration still carries its OWN doc comment. The drain
	// runs ahead of the doc, so a stranded comment stacked above this one instead of displacing
	// it — this assertion passes on the defective converter and is a control, not a witness.
	documented := findDeclarationLine(t, lines, "documentedLast(")
	preceding := strings.TrimSpace(lines[documented-1])

	if !strings.Contains(preceding, "documentedLast keeps its own doc comment") {
		t.Errorf("the declaration following a displaced one lost its own doc comment — the line above %q is %q", strings.TrimSpace(lines[documented]), preceding)
	}
}

// TestDisplacedFuncAtEndOfFileDoesNotLeakPastTheClass is the runtime mbitmap shape: the displaced
// declaration is the file's LAST, so there is no following declaration to misattribute to and the
// end-of-file drain flushed the body's comments after the class's closing brace instead.
func TestDisplacedFuncAtEndOfFileDoesNotLeakPastTheClass(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	displacedFixture(t, "example.com/app", "displacedTail")

	lines := convertWithComments(t, `package main

func main() {
	_ = 1
}

// displacedTail is hand-converted and is the last declaration of its file.
func displacedTail(n int) int {
	// first stranded line
	if n > 0 {
		// second stranded line
		return n
	}
	// third stranded line
	return 0
}
`)

	findCommentLine(t, lines, "func displacedTail is hand-converted")

	for _, stranded := range []string{
		"first stranded line",
		"second stranded line",
		"third stranded line",
	} {
		requireAbsent(t, lines, stranded)
	}
}

// findDeclarationLine returns the index of the emitted C# declaration carrying the signature
// fragment — the first line holding it that is not itself a comment.
func findDeclarationLine(t *testing.T, lines []string, fragment string) int {
	t.Helper()

	for i, line := range lines {
		trimmed := strings.TrimSpace(line)

		if strings.HasPrefix(trimmed, "//") || !strings.Contains(trimmed, fragment) {
			continue
		}

		if i == 0 {
			t.Fatalf("declaration %q is the first emitted line, so it has no preceding comment to check", fragment)
		}

		return i
	}

	t.Fatalf("no declaration carrying %q was emitted:\n%s", fragment, strings.Join(lines, "\n"))

	return -1
}
