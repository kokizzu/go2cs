package main

import (
	"errors"
	"io/fs"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"testing"
)

// findProject returns the lexicographically first .csproj under root.
//
// This lives in the test file because only the test needs it: the pipeline itself already knows
// the project path it just generated, so a search would be answering a question production code
// never asks. It is kept as a helper (rather than inlined into TestFindProject) so the test reads
// as "generate a tree, then find the project in it".
func findProject(root string) (string, error) {
	var projects []string
	err := filepath.WalkDir(root, func(path string, entry fs.DirEntry, err error) error {
		if err != nil {
			return err
		}
		if !entry.IsDir() && strings.HasSuffix(strings.ToLower(entry.Name()), ".csproj") {
			projects = append(projects, path)
		}
		return nil
	})
	if err != nil {
		return "", err
	}
	// Sort so a tree with several projects yields a stable answer rather than whatever order the
	// filesystem happened to hand back.
	sort.Strings(projects)
	if len(projects) == 0 {
		return "", errors.New("no generated project")
	}
	return projects[0], nil
}

func TestCollectCSharp(t *testing.T) {
	root := t.TempDir()
	if err := os.WriteFile(filepath.Join(root, "b.cs"), []byte("class B {}"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(root, "a.cs"), []byte("class A {}"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.Mkdir(filepath.Join(root, "obj"), 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(root, "obj", "ignored.cs"), []byte("ignore"), 0o600); err != nil {
		t.Fatal(err)
	}

	got, err := collectCSharp(root)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Index(got, "a.cs") > strings.Index(got, "b.cs") {
		t.Fatal("generated files are not stable-sorted")
	}
	if strings.Contains(got, "ignore") {
		t.Fatal("obj output was included")
	}
}

func TestFindProject(t *testing.T) {
	root := t.TempDir()
	project := filepath.Join(root, "tour.csproj")
	if err := os.WriteFile(project, []byte("<Project />"), 0o600); err != nil {
		t.Fatal(err)
	}
	got, err := findProject(root)
	if err != nil {
		t.Fatal(err)
	}
	if got != project {
		t.Fatalf("findProject = %q, want %q", got, project)
	}
}

func TestHTMLescape(t *testing.T) {
	got := htmlEscape(`<bad title="x">&`)
	if strings.Contains(got, "<bad") || !strings.Contains(got, "&amp;") {
		t.Fatalf("htmlEscape returned unsafe text: %q", got)
	}
}
