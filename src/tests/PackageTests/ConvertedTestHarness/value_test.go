package convertedtestharness

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestSamePackageUnexported(t *testing.T) {
	if got := add(2, 3); got != 5 {
		t.Fatalf("add(2, 3) = %d, want 5", got)
	}
}

func TestSubtestsCleanupAndParallel(t *testing.T) {
	cleaned := false
	t.Cleanup(func() {
		cleaned = true
	})

	for _, name := range []string{"duplicate", "duplicate"} {
		name := name
		t.Run(name, func(sub *testing.T) {
			sub.Parallel()
			if sub.Name() == "" {
				sub.Error("subtest name was empty")
			}
		})
	}

	if cleaned {
		t.Error("cleanup ran before the parent test completed")
	}
}

func TestTempDir(t *testing.T) {
	if t.TempDir() == "" {
		t.Error("TempDir returned an empty path")
	}
}

// TestTempDirNamesAreCaseDistinct guards the temp-dir path against a case-insensitive filesystem.
//
// Go gives every test its own directory via os.MkdirTemp, whose random suffix makes collisions
// impossible, so this passes trivially on the Go side. The C# host derives the path from the test
// name instead — deliberately, so a failed run's tree is reproducible and findable — and a
// case-PRESERVING name over a case-INSENSITIVE filesystem (Windows, macOS) is not a unique path:
// two tests whose names differ only by case resolve to ONE directory, and each one's cleanup
// deletes the other's tree. That is a real, observed defect: os's TestFileReaddir and
// TestFileReadDir, both parallel, doing exactly this to each other.
//
// The two subtests below differ only by case, which makes them the smallest reproduction. Comparing
// the paths case-INSENSITIVELY is the assertion, because that is the comparison the filesystem
// itself makes; comparing them with != would pass while the bug is present.
func TestTempDirNamesAreCaseDistinct(t *testing.T) {
	dirs := make([]string, 0, 2)

	for _, name := range []string{"collide", "COLLIDE"} {
		t.Run(name, func(sub *testing.T) {
			dir := sub.TempDir()
			if dir == "" {
				sub.Fatal("TempDir returned an empty path")
			}

			// Prove the directory is real and writable while the subtest owns it — a path that
			// merely looks distinct but was never created would pass the comparison below.
			marker := filepath.Join(dir, "marker.txt")
			if err := os.WriteFile(marker, []byte(name), 0o600); err != nil {
				sub.Fatalf("write marker in TempDir: %v", err)
			}

			got, err := os.ReadFile(marker)
			if err != nil {
				sub.Fatalf("read marker back from TempDir: %v", err)
			}

			if string(got) != name {
				sub.Fatalf("marker content = %q, want %q", string(got), name)
			}

			dirs = append(dirs, dir)
		})
	}

	if len(dirs) != 2 {
		t.Fatalf("recorded %d temp directories, want 2", len(dirs))
	}

	if strings.EqualFold(dirs[0], dirs[1]) {
		t.Fatalf("subtests differing only by case share a temp directory on a case-insensitive filesystem: %q and %q", dirs[0], dirs[1])
	}
}

// TestFixtureRead proves the testdata fixture is readable from the isolated working directory on
// the GO SIDE TOO (requirements §10 — fixture reads were previously only proven by the MSTest
// runtime guards).
func TestFixtureRead(t *testing.T) {
	data, err := os.ReadFile("testdata/message.txt")
	if err != nil {
		t.Fatalf("read testdata fixture: %v", err)
	}

	if len(data) < 7 || string(data[:7]) != "fixture" {
		t.Fatalf("unexpected fixture content: %q", string(data))
	}
}

func TestMain(m *testing.M) {
	m.Run()
}

// NOTE: the branch fixture carried an intentionally malformed `Testlower` declaration to prove
// non-registration, but Go 1.23's `go test` runs the vet `tests` analyzer by default and REFUSES
// to build the package ("Testlower has malformed name") — the Go side of the oracle could never
// run. Invalid-name rejection stays guarded at the converter level (TestIsGoTestName).
