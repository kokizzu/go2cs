package main

import (
	"fmt"
	"io"
	"os"
	"os/exec"
)

// Guards the pipe-EOF-barrier class: a child that closes its stdout must EOF the
// parent's StdoutPipe read BEFORE the child exits. The child here closes stdout and
// then BLOCKS waiting for a release byte on stdin, so a runtime that holds any extra
// duplicate of the stdout pipe's write end (the .NET startup-descriptor bug this test
// pins, fixed by golib's builtin.LinuxStdDescriptors.cs) deadlocks both sides into
// the harness run-timeout instead of flaking on a timing threshold.
//
// The idiom is Go's own: os/exec's test suite uses "child closes stdout" as its
// readiness barrier (startHang et al.), so this is the smallest standalone shape of
// a whole family of upstream tests.

func main() {
	if os.Getenv("EOF_BARRIER_CHILD") == "1" {
		child()
		return
	}
	parent()
}

func child() {
	fmt.Println("READY")
	os.Stdout.Close()
	// Block until the parent releases us. If our stdout close did not propagate EOF,
	// the parent never gets here to write the release byte: deadlock, caught by the
	// harness timeout.
	var release [1]byte
	os.Stdin.Read(release[:])
}

func parent() {
	cmd := exec.Command(os.Args[0])
	cmd.Env = append(os.Environ(), "EOF_BARRIER_CHILD=1")
	stdin, err := cmd.StdinPipe()
	if err != nil {
		fmt.Println("StdinPipe error:", err)
		os.Exit(1)
	}
	out, err := cmd.StdoutPipe()
	if err != nil {
		fmt.Println("StdoutPipe error:", err)
		os.Exit(1)
	}
	if err := cmd.Start(); err != nil {
		fmt.Println("Start error:", err)
		os.Exit(1)
	}
	// Must return when the child closes its stdout — child exit cannot be what ends
	// this read, because the child is blocked on stdin until we write the release.
	data, _ := io.ReadAll(out)
	stdin.Write([]byte{'\n'})
	stdin.Close()
	cmd.Wait()
	fmt.Printf("child wrote %q\n", string(data))
	fmt.Println("EOF preceded child exit")
}
