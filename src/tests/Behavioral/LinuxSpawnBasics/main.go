// LinuxSpawnBasics guards the process-spawn seam end to end through os/exec's portable surface:
// self-re-exec via os.Args[0] (the helper protocol every Go suite uses), environment round-trip,
// captured stdout, and exit-code propagation. The program's observable output is identical on
// every GOOS — on Windows it exercises the banked CreateProcessW hand-own, on Linux the
// posix_spawn hand-own (docs/phase4/DESIGN-linux-exec.md) — so one golden guards both seams.
package main

import (
	"fmt"
	"os"
	"os/exec"
	"strings"
)

func main() {
	if os.Getenv("SPAWN_BASICS_CHILD") == "1" {
		fmt.Println("child: hello from the spawned image")
		fmt.Println("child: marker =", os.Getenv("SPAWN_BASICS_MARKER"))
		os.Exit(3)
	}

	cmd := exec.Command(os.Args[0])
	cmd.Env = append(os.Environ(), "SPAWN_BASICS_CHILD=1", "SPAWN_BASICS_MARKER=xyzzy")

	out, err := cmd.CombinedOutput()

	fmt.Print(string(out))
	if err == nil {
		fmt.Println("parent: unexpected nil error, want exit status 3")
		return
	}

	exitErr, ok := err.(*exec.ExitError)
	if !ok {
		fmt.Println("parent: spawn failed:", strings.ReplaceAll(err.Error(), os.Args[0], "<self>"))
		return
	}

	fmt.Println("parent: child exit code =", exitErr.ExitCode())
	fmt.Println("parent: captured", len(strings.Split(strings.TrimSpace(string(out)), "\n")), "lines")
}
