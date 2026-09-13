// safePushGuard_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// src/safe-push.sh is the push/announce/security composition (coordinator ruling, 2026-09-06). It
// exists because on that day four participants -- including the two who had written the rule down --
// each composed those three steps wrongly at least once: a census placed in the same command as the
// push it gated, a rejected push reporting rc=0 through a pipe, a --force-with-lease SHA expanded
// from a nine-character prefix, and an `echo` asserting "silence above = clean" over output that
// falsified it. In every case the instrument was correct and the composition made its verdict inert.
//
// A script nobody runs fails open, which is false-green route #6, so the ruling was that it lands in
// src/ WITH A GUARD. This is the guard, and it lives here for the same reason
// projitemsIntegrity_test.go and internal/repoguard/fleetIdentifierCensus_test.go do: the converter's
// own `go test ./...` is the one gate every lane already pays for.
//
// ⚠ IT NESTS, DELIBERATELY AND AT A MEASURED PRICE. The self-test's positive control is a REAL push
// to a hermetic bare repository, and a real push runs the script's security gate, which invokes
// `go test` on go2cs/internal/repoguard -- so this test spawns bash, which spawns go test. About 25 s on a 215 s
// suite, roughly 10%, paid by every lane on every run.
//
// The cheaper alternative was to run only the arms that need no network and leave the real-push arm
// to lanes. It was REFUSED, and by the finding that produced this file: the self-test's own earlier
// version reached only --dry-run, so it guarded everything except the push path the script exists
// for. Adopting that here would re-commit, knowingly and by name, the defect the instrument was
// built to close. A cheaper guard that omits the dangerous path is not a cheaper guard; it is a
// weaker one wearing the same name. The cost is reducible later by narrowing the inner invocation --
// an optimisation, not a design change, and not a reason to defer the arm.
func TestSafePushSelfTest(t *testing.T) {
	// A POSIX spelling, not filepath.Join: the argument is read by bash, not by Windows. The host form
	// `..\safe-push.sh` happens to survive Git Bash, and reaches a WSL bash as `..safe-push.sh`.
	script := "../safe-push.sh"

	// bash is how every lane already drives git in this fleet, on Windows through Git Bash and
	// natively elsewhere. A host without a usable one cannot run the composition either, so there is
	// nothing this guard could assert about it -- but the skip NAMES itself rather than passing
	// quietly, because an unmeasured arm reported as a pass is the class this whole file is about.
	// A SHALLOW clone cannot reach the real-push arm at all, so this guard is UNMEASURED there and says
	// so BY NAME rather than going red for a reason that has nothing to do with the script. Checked
	// before bash, because it needs no bash to decide and it is the more specific answer.
	if shallow, why := safePushRepoIsShallow(); shallow {
		t.Skip(why)
	}

	bash, unmeasured := safePushBash()
	if bash == "" {
		t.Skip(unmeasured)
	}
	t.Logf("driving src/safe-push.sh through %s", bash)

	out, err := exec.Command(bash, script, "--self-test").CombinedOutput()
	text := string(out)

	if err != nil {
		t.Fatalf("src/safe-push.sh --self-test failed: %v\n%s", err, text)
	}

	// The verdict line, and then the ARM COUNT -- because an exit code cannot distinguish a suite
	// that ran ten arms from one that silently lost nine of them, which is the count-match lesson
	// this repository has already paid for in a GolibTests reading taken from a stale tree.
	if !strings.Contains(text, "SELF-TEST CLEAN") {
		t.Fatalf("src/safe-push.sh --self-test did not report a clean run:\n%s", text)
	}

	const wantArms = 10
	if got := strings.Count(text, "\n  ok   "); got != wantArms {
		t.Fatalf("expected %d passing arms from src/safe-push.sh --self-test, counted %d -- an arm that quietly stops running is exactly what this count exists to catch:\n%s",
			wantArms, got, text)
	}

	// Each arm's REASON, not merely its count. An earlier version of that suite had three arms
	// aborting on an unrelated branch-existence check before reaching the validation they existed to
	// test, and "it refused" read as proof the check worked while the check never executed. A red
	// that goes red for the wrong reason looks exactly like a control working, which makes it worse
	// than one that never fires. These are the reasons, so a rewrite that keeps the arm names and
	// loses their meaning fails here.
	for _, reason := range []string{
		"never expanded from a prefix",
		"does not resolve to a commit",
		"not a hex object name",
		"Pass --new if that is intended",
		"Announce the new SHA",
		"announce-then-push protects nobody",
		"WITHOUT evaluating --force-with-lease",
		"SAFEPUSH OK",
		"push failed",
		"cmd/go DOES cache this invocation",
	} {
		if !strings.Contains(text, reason) {
			t.Errorf("the self-test no longer asserts the reason %q -- an arm asserts the REASON it failed, or it is not a control:\n%s", reason, text)
		}
	}
}

// safePushBash returns the bash that can actually run src/safe-push.sh on this host, or "" and the
// reason this guard is UNMEASURED here.
//
// ⚠ ON WINDOWS, "SOME bash ON PATH" IS NOT "A bash THAT CAN RUN THIS SCRIPT" (measured 2026-09-12). A
// PowerShell session resolved `bash` to C:\Windows\System32\bash.exe -- WSL -- and the guard FAILED in
// ~5 s with a message reading exactly like a broken safe-push.sh, while the same tree with Git Bash
// first on PATH PASSED in ~44 s through a real hermetic push. A false red on the script that gates
// every fleet push, pointing away from its cause. Fixing only the path spelling does not rescue WSL:
// measured, it gets three arms in and dies on `fatal: not a git repository`, because a worktree's
// `.git` file names its gitdir as a Windows path Linux git cannot open; and a non-login WSL bash has
// no `go` for the security gate either (probed) -- the script would run against Linux's own git and
// toolchain, not this host's, which is a different measurement even where it succeeds.
//
// So on Windows the guard does not trust PATH order at all. It resolves Git for Windows' bash from
// the git this host actually uses (`git --exec-path` sits inside the install), and when there is no
// such bash it SKIPS as UNMEASURED naming what it looked for -- never a false red, and never a pass
// that was not earned by the script running.
func safePushBash() (string, string) {
	if runtime.GOOS != "windows" {
		if path, err := exec.LookPath("bash"); err == nil {
			return path, ""
		}
		return "", "bash is not on PATH, so src/safe-push.sh cannot be exercised here -- this guard is UNMEASURED on this host, not passing"
	}

	pathBash := "no bash"
	if p, err := exec.LookPath("bash"); err == nil {
		pathBash = p
	}
	unmeasured := func(why string) string {
		return fmt.Sprintf("%s, so Git Bash cannot be located and src/safe-push.sh cannot be exercised here -- "+
			"this guard is UNMEASURED on this host, not passing. PATH's bash (%s) is deliberately NOT used: "+
			"on Windows it may be WSL, which cannot run this script against this host's git and toolchain", why, pathBash)
	}

	git, err := exec.LookPath("git")
	if err != nil {
		return "", unmeasured("git is not on PATH")
	}
	out, err := exec.Command(git, "--exec-path").Output()
	if err != nil {
		return "", unmeasured(fmt.Sprintf("`%s --exec-path` failed (%v)", git, err))
	}
	execPath := strings.TrimSpace(string(out))
	if !filepath.IsAbs(execPath) {
		return "", unmeasured(fmt.Sprintf("`git --exec-path` answered %q, which is not a Windows path", execPath))
	}

	// <install>\mingw64\libexec\git-core: the install root is the nearest ancestor holding the MSYS
	// runtime's usr\bin\bash.exe. bin\bash.exe is preferred -- it is the launcher that puts the
	// install's own git on PATH, and the form the measured passing arm used.
	dir := filepath.Clean(execPath)
	for range 4 {
		parent := filepath.Dir(dir)
		if parent == dir {
			break
		}
		dir = parent
		runtimeBash := filepath.Join(dir, "usr", "bin", "bash.exe")
		if !isRegularFile(runtimeBash) {
			continue
		}
		if launcher := filepath.Join(dir, "bin", "bash.exe"); isRegularFile(launcher) {
			return launcher, ""
		}
		return runtimeBash, ""
	}
	return "", unmeasured(fmt.Sprintf("no Git for Windows install holding usr\\bin\\bash.exe was found above %s", execPath))
}

func isRegularFile(path string) bool {
	info, err := os.Stat(path)
	return err == nil && info.Mode().IsRegular()
}

// safePushRepoIsShallow reports whether the repository this test runs in is a SHALLOW clone, and the
// reason this guard is UNMEASURED when it is.
//
// ⚠ THE SELF-TEST CANNOT PASS IN A SHALLOW CLONE, AND ITS RED LOOKS LIKE A BROKEN safe-push.sh
// (measured 2026-09-13 in a cloud lane's container). The hermetic-origin arm seeds its bare repository
// with `git push -q "$bare" "HEAD^{commit}:refs/heads/seeded"`, and git refuses to push history it does
// not have:
//
//	! [remote rejected]  HEAD^{commit} -> seeded (shallow update not allowed)
//	ABORT: self-test: cannot seed the hermetic origin
//
// Four of the ten arms pass first, so the failure arrives mid-suite with a plausible-looking tail, and
// `src/safe-push.sh` is byte-identical to its base -- a lane reading this red will look for its own
// change's fingerprints in the script that gates every fleet push. Every cloud lane clones shallow, so
// this is not an edge case: it is the permanent state of a whole class of host, and the whole suite
// reads red there for a reason no lane's diff can fix.
//
// A skip is the honest shape and a PASS would not be: the script is UNMEASURED here, not proven. The
// arm that cannot run is precisely the real-push arm this file's own header refuses to omit, so the
// skip NAMES that -- a reader must be able to tell "we did not test the push path" from "the push path
// works".
//
// ⚠ IT FAILS TOWARD RUNNING, NOT TOWARD SKIPPING. An unanswerable query returns false, so a host whose
// git is too old for `--is-shallow-repository`, or where the query errors for any other reason, still
// runs the guard. A skip that fires when its own question could not be asked is a silent disarm, which
// is the shape this entire file exists to prevent.
func safePushRepoIsShallow() (bool, string) {
	return safePushRepoIsShallowIn("")
}

// safePushRepoIsShallowIn is safePushRepoIsShallow's testable core: dir is the directory the query is
// asked from, empty meaning the process working directory.
func safePushRepoIsShallowIn(dir string) (bool, string) {
	command := exec.Command("git", "rev-parse", "--is-shallow-repository")
	command.Dir = dir

	out, err := command.Output()

	if err != nil {
		return false, ""
	}

	if strings.TrimSpace(string(out)) != "true" {
		return false, ""
	}

	return true, "this is a SHALLOW clone, so src/safe-push.sh --self-test cannot seed its hermetic origin -- " +
		"`git push HEAD^{commit}:refs/heads/seeded` is refused with \"shallow update not allowed\" and the run " +
		"aborts on \"self-test: cannot seed the hermetic origin\" four arms in. The real-push arm is therefore " +
		"UNMEASURED on this host, not passing: nothing here proves the push path works. Run this guard in a " +
		"full clone, or `git fetch --unshallow` first"
}

// TestSafePushShallowDetectionFiresOnAShallowCloneAndNotOnAFullOne is the positive control for the skip
// above, and the negative half in the same test.
//
// A skip nobody has watched fire is the same liability as a gate nobody has watched go red: it would
// silently swallow the whole guard the day its predicate inverted, on every host, and the suite would
// still print ok. So the control builds a real two-commit repository and a real depth-1 clone of it,
// and asserts the predicate reads TRUE in the clone and FALSE in the full origin -- the second half is
// what would catch a predicate that answers "shallow" everywhere.
//
// Two commits, not one: a depth-1 clone of a single-commit repository need not be marked shallow at
// all, since the whole history already fits. That would make the positive arm pass or fail on a detail
// of git's bookkeeping rather than on the predicate.
func TestSafePushShallowDetectionFiresOnAShallowCloneAndNotOnAFullOne(t *testing.T) {
	git, err := exec.LookPath("git")

	if err != nil {
		t.Skip("git is not on PATH, so the shallow-clone predicate is UNMEASURED here, not passing")
	}

	root := t.TempDir()
	origin := filepath.Join(root, "origin")
	shallow := filepath.Join(root, "shallow")

	run := func(dir string, args ...string) {
		t.Helper()
		// Identity on the command line, never in a global config: the test must not write to the
		// host's git configuration, and a container often has no user identity at all.
		full := append([]string{"-c", "user.name=go2cs test", "-c", "user.email=test@example.invalid"}, args...)
		command := exec.Command(git, full...)
		command.Dir = dir

		if out, err := command.CombinedOutput(); err != nil {
			t.Fatalf("git %v in %s failed: %v\n%s", args, dir, err, out)
		}
	}

	if err := os.MkdirAll(origin, 0o755); err != nil {
		t.Fatalf("cannot create the control origin: %v", err)
	}

	run(origin, "init", "-q", "-b", "main")

	for _, name := range []string{"first", "second"} {
		if err := os.WriteFile(filepath.Join(origin, name+".txt"), []byte(name+"\n"), 0o644); err != nil {
			t.Fatalf("cannot write the control commit's file: %v", err)
		}

		run(origin, "add", name+".txt")
		run(origin, "commit", "-q", "--no-gpg-sign", "-m", name)
	}

	// file:// rather than a bare path: git ignores --depth on a local-path clone, which would hand back
	// a FULL clone and a positive arm that quietly proves nothing.
	run(root, "clone", "-q", "--depth", "1", "file://"+filepath.ToSlash(origin), shallow)

	// POSITIVE: the depth-1 clone must read shallow, with a reason naming both halves.
	isShallow, why := safePushRepoIsShallowIn(shallow)

	if !isShallow {
		t.Fatalf("the predicate read NOT shallow in a depth-1 clone of a two-commit repository -- it cannot fire, so the skip in TestSafePushSelfTest is dead code")
	}

	for _, want := range []string{"shallow update not allowed", "cannot seed the hermetic origin", "UNMEASURED"} {
		if !strings.Contains(why, want) {
			t.Errorf("the skip reason no longer names %q -- a skip states what was not measured, or it reads as a pass: %s", want, why)
		}
	}

	// NEGATIVE: the full origin must read NOT shallow, or the predicate answers "shallow" everywhere
	// and the guard is disabled on every host.
	if isShallow, why := safePushRepoIsShallowIn(origin); isShallow {
		t.Fatalf("the predicate read SHALLOW in a full (non-cloned) repository -- it would skip the guard everywhere: %s", why)
	}
}
