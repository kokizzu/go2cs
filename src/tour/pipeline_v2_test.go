package main

import (
	"context"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestCollectCSharpPrefersSourceFiles(t *testing.T) {
	root := t.TempDir()
	if err := os.WriteFile(filepath.Join(root, "main.cs"), []byte("class Main {}"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(root, "package_info.cs"), []byte("metadata"), 0o600); err != nil {
		t.Fatal(err)
	}

	got, err := collectCSharp(root)
	if err != nil {
		t.Fatal(err)
	}
	if !strings.Contains(got, "class Main") {
		t.Fatal("source file was not returned")
	}
	if strings.Contains(got, "metadata") {
		t.Fatal("package metadata obscures the matching source view")
	}
	packageInfo, err := readGeneratedFile(root, "package_info.cs")
	if err != nil {
		t.Fatal(err)
	}
	if packageInfo != "metadata" {
		t.Fatalf("package info = %q, want metadata", packageInfo)
	}
}

func TestGo2CSConvertArgsPreserveComments(t *testing.T) {
	runtime := runtimeConfiguration{mode: runtimeCore, converterRoot: "core"}
	got := strings.Join(go2csConvertArgs(runtime, "input", "output"), " ")
	want := "-comments -recurse -go2cspath core input output"
	if got != want {
		t.Fatalf("go2csConvertArgs = %q, want %q", got, want)
	}
}

func TestGo2CSConvertArgsUseNuGetRecursion(t *testing.T) {
	runtime := runtimeConfiguration{mode: runtimeNuGet, converterRoot: "core"}
	got := strings.Join(go2csConvertArgs(runtime, "input", "output"), " ")
	want := "-comments -recurse=nuget -go2cspath core input output"
	if got != want {
		t.Fatalf("go2csConvertArgs = %q, want %q", got, want)
	}
}

func TestRunStageReportsKilledContext(t *testing.T) {
	ctx, cancel := context.WithCancel(context.Background())
	cancel()

	runner := newPipelineRunner(t.TempDir())
	stage := runner.runStage(ctx, "run", ".NET Run", t.TempDir(), time.Second, "go", "version")
	if stage.Status != "killed" {
		t.Fatalf("status = %q, want killed; output: %s", stage.Status, stage.Output)
	}
	if stage.Output != "Program exited: killed" {
		t.Fatalf("output = %q, want Tour-compatible killed message", stage.Output)
	}
}

func TestConversionIDsAreUnique(t *testing.T) {
	first, err := newConversionID()
	if err != nil {
		t.Fatal(err)
	}
	second, err := newConversionID()
	if err != nil {
		t.Fatal(err)
	}
	if first == second || len(first) != 32 || len(second) != 32 {
		t.Fatalf("unexpected conversion IDs: %q %q", first, second)
	}
}

func TestProgramExitMessageMatchesTour(t *testing.T) {
	if got := programExitMessage(context.Background(), context.Background(), nil); got != "Program exited." {
		t.Fatalf("successful exit = %q", got)
	}

	parent, cancel := context.WithCancel(context.Background())
	cancel()
	if got := programExitMessage(parent, parent, context.Canceled); got != "Program exited: killed" {
		t.Fatalf("killed exit = %q", got)
	}

	command, commandCancel := context.WithDeadline(context.Background(), time.Now().Add(-time.Second))
	defer commandCancel()
	if got := programExitMessage(context.Background(), command, context.DeadlineExceeded); got != "Program exited: process took too long." {
		t.Fatalf("timed-out exit = %q", got)
	}
}

// The Tour writes its exit notice after a single newline and never trims what
// the program produced, so the blank line ahead of the notice is the program's
// own trailing newline -- present for a Println, absent for an io.Copy that
// stops mid-line.
func TestAppendSystemSegmentMatchesTourSpacing(t *testing.T) {
	endedLine := appendSystemSegment([]outputSegment{{Kind: outputStdout, Text: "hello\n"}}, "Program exited.")
	if joined := joinSegments(endedLine); joined != "hello\n\nProgram exited." {
		t.Fatalf("transcript after a completed line = %q", joined)
	}
	if len(endedLine) != 2 || endedLine[0].Kind != outputStdout || endedLine[1].Kind != outputSystem {
		t.Fatalf("appended segments = %+v, want the notice tagged as a system message", endedLine)
	}

	midLine := appendSystemSegment([]outputSegment{{Kind: outputStdout, Text: "You cracked the code!"}}, "Program exited.")
	if joined := joinSegments(midLine); joined != "You cracked the code!\nProgram exited." {
		t.Fatalf("transcript after an unterminated line = %q, want no blank line", joined)
	}

	empty := appendSystemSegment(nil, "Program exited: killed")
	if len(empty) != 1 || empty[0].Kind != outputSystem || empty[0].Text != "Program exited: killed" {
		t.Fatalf("empty appendSystemSegment = %+v", empty)
	}
}

// The Tour colors a program's standard error, so a stage has to keep the two
// streams apart all the way to the interface.
func TestRunStageTagsProgramStreams(t *testing.T) {
	runner := newPipelineRunner(t.TempDir())
	defer runner.close()

	written := runner.runStage(context.Background(), "run", ".NET Run", t.TempDir(), 30*time.Second, "go", "version")
	if written.Status != "passed" {
		t.Fatalf("status = %q, want passed; output: %s", written.Status, written.Output)
	}
	if len(written.Segments) != 2 || written.Segments[0].Kind != outputStdout || written.Segments[1].Kind != outputSystem {
		t.Fatalf("segments = %+v, want tagged standard output then the exit notice", written.Segments)
	}

	failed := runner.runStage(context.Background(), "run", ".NET Run", t.TempDir(), 30*time.Second, "go", "nosuchcommand")
	if failed.Status != "failed" {
		t.Fatalf("status = %q, want failed; output: %s", failed.Status, failed.Output)
	}
	if len(failed.Segments) == 0 || failed.Segments[0].Kind != outputStderr {
		t.Fatalf("segments = %+v, want the diagnostic tagged as standard error", failed.Segments)
	}
	if failed.Output != joinSegments(failed.Segments) {
		t.Fatalf("output %q does not match its segments %q", failed.Output, joinSegments(failed.Segments))
	}
}

func TestFormatTranspileTranscriptListsGeneratedFiles(t *testing.T) {
	root := t.TempDir()
	for name := range map[string]struct{}{
		"main.cs":                {},
		"package_info.cs":        {},
		"tour.local.demo.csproj": {},
	} {
		if err := os.WriteFile(filepath.Join(root, name), []byte("generated"), 0o600); err != nil {
			t.Fatal(err)
		}
	}

	got := formatTranspileTranscript("module diagnostic", "go2cs diagnostic", root, "passed", "Core source")
	for _, want := range []string{"$ go mod tidy", "module diagnostic", "$ go2cs -recurse main.go", "Runtime: Core source", "go2cs diagnostic", "main.cs", "tour.local.demo.csproj", "Transpile completed."} {
		if !strings.Contains(got, want) {
			t.Fatalf("transcript missing %q:\n%s", want, got)
		}
	}
}

func TestCollectCSharpRejectsMetadataOnlyOutput(t *testing.T) {
	root := t.TempDir()
	if err := os.WriteFile(filepath.Join(root, "package_info.cs"), []byte("metadata"), 0o600); err != nil {
		t.Fatal(err)
	}

	if _, err := collectCSharp(root); err == nil {
		t.Fatal("metadata-only output was accepted as converted app source")
	}
}

func TestTourAppOutputDir(t *testing.T) {
	root := filepath.FromSlash("C:/temp/output")
	want := filepath.Join(root, "src", "tour.local", "session")

	if got := tourAppOutputDir(root); got != want {
		t.Fatalf("tourAppOutputDir = %q, want %q", got, want)
	}
}
