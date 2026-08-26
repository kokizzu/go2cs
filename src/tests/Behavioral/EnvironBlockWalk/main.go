// EnvironBlockWalk guards syscall.Environ -- the process environment as Go reads it, and the third
// fork of the Windows-syscall wall: the one where the KERNEL hands back a block and the CALLER
// reinterprets it, so no wrapper is at fault and no mirror-the-wrapper remedy applies.
//
// On Windows, GetEnvironmentStringsW returns a pointer into memory Windows owns: a run of
// double-NUL-terminated UTF-16 strings ending in an empty one, which the caller must walk and then
// hand BACK via FreeEnvironmentStringsW. Go's env_windows.go walks it with pointer arithmetic
// (`for *envp != 0 { ... end = unsafe.Add(end, size) ... }`). Converted literally that becomes a
// `uintptr` -> `ж<uint16>` round trip per step over an address the CLR never handed out, and the
// process died outright -- `Fatal error. Internal CLR error. (0x80131506)`, no panic, no recovery.
// It is not a niche path: os.Environ IS syscall.Environ, os/exec builds every child's environment
// from it, and the whole `net` test suite went down at TestInterfaceAddrsWithNetsh in a 314-name
// contiguous alphabetical tail because of it (docs/phase4/CENSUS-giants-2026-08-26.md).
//
// The walk is hand-owned now (syscall/windows/env_windows_impl.cs), transcribing the block into
// managed strings in one pass and freeing it, the same remedy net.adapterAddresses took for the
// same fork. Linux keeps its auto conversion (env_unix.go reads runtime.envs, a Go slice) and is
// exercised by this test unchanged -- which is the point: the CONTRACT is what is guarded here, not
// one platform's mechanism.
//
// WHAT IS ASSERTED, AND WHY NOT THE CONTENTS. A process environment is machine-varying, so nothing
// here prints an inherited entry. Everything printed is either injected by this program or a shape
// invariant that holds on any machine:
//
//	delta         -- entries gained by adding N sentinels: the walk must find ALL of them, so a
//	                 walk that stops early (or double-counts) cannot report the right delta
//	the sentinels -- read back through Environ, not through Getenv, and covering the cases that
//	                 break a naive parse: '=' inside the VALUE, an empty value, a long value, and
//	                 non-ASCII (a UTF-16 decode that truncates or mis-pairs shows up here)
//	hidden        -- whether any entry begins with '='. Windows keeps per-drive =C:=... records
//	                 there, and accepts one set by name, so the test injects its own rather than
//	                 depending on the machine having any. Go returns such entries; .NET's
//	                 Environment.GetEnvironmentVariables SKIPS them, so this line is what
//	                 distinguishes a faithful block walk from the lossy dictionary shortcut. On
//	                 Unix Go's own Setenv rejects a key containing '=' (EINVAL), so the injection
//	                 simply does not take and both sides report false together -- which is why the
//	                 line carries a bool and never an error string
//	well-formed   -- every entry carries a separator at index >= 1 and no NUL, on the whole
//	                 environment, not just the sentinels
//	stable        -- 50 walks in a row agree. The block is OS memory that must be handed back; a
//	                 walk that frees the wrong pointer, or forgets to free, degrades over repeats
//	                 (STATUS_HEAP_CORRUPTION is the documented shape) rather than failing the first
//	unset         -- Unsetenv removes the entry from a subsequent walk, so the walk is live against
//	                 the real block rather than a snapshot taken once
package main

import (
	"fmt"
	"os"
	"sort"
	"strings"
	"syscall"
)

// The sentinels. Names share one prefix so they can be pulled back out of any environment, and
// sort distinctly. Values cover the parse edges.
var sentinels = []struct {
	name  string
	value string
}{
	{"GO2CS_ENVPROBE_A_PLAIN", "plain-value"},
	{"GO2CS_ENVPROBE_B_EQUALS", "k1=v1=v2"},
	{"GO2CS_ENVPROBE_C_EMPTY", ""},
	{"GO2CS_ENVPROBE_D_LONG", strings.Repeat("abcdefghij", 40)},
	{"GO2CS_ENVPROBE_E_UNICODE", "über-日本語-\U0001f600"},
}

const prefix = "GO2CS_ENVPROBE_"

// hiddenName is a name Windows accepts and reports, and Go's Unix Setenv refuses. See the header.
const hiddenName = "=GO2CSENVPROBEHIDDEN"

func main() {
	// Injected BEFORE the baseline so it does not disturb the delta below.
	hiddenSet := os.Setenv(hiddenName, "hid") == nil

	before := syscall.Environ()
	if len(before) == 0 {
		fmt.Println("FATAL empty environment")
		os.Exit(1)
	}

	for _, s := range sentinels {
		if err := os.Setenv(s.name, s.value); err != nil {
			fmt.Println("FATAL Setenv", s.name, err)
			os.Exit(1)
		}
	}

	after := syscall.Environ()

	// The walk found exactly the entries that were added -- no early stop, no duplicate.
	fmt.Printf("delta %d\n", len(after)-len(before))

	// The sentinels as the WALK reports them, not as Getenv does.
	fmt.Println("-- sentinels via syscall.Environ --")
	for _, e := range collect(after) {
		fmt.Println(ascii(e))
	}

	// os.Environ is syscall.Environ; proving they agree keeps the whole os-level surface on this
	// one root rather than on a second, quieter copy.
	fmt.Println("-- sentinels via os.Environ --")
	for _, e := range collect(os.Environ()) {
		fmt.Println(ascii(e))
	}

	// Every walked entry agrees with a direct Getenv of its own name. This is the check that a
	// mis-stepped walk (one UTF-16 unit off, or a stride in bytes where units were meant) cannot
	// survive: it would split names and values at the wrong place for the whole environment.
	fmt.Printf("getenv-agrees %v\n", getenvAgrees(after))

	// Entries beginning with '=' -- Go returns them, .NET's dictionary view drops them. See the
	// header. Reported as three booleans so the platforms answer honestly without printing an
	// error string: whether the injection took, whether the walk carries THAT entry, and whether
	// the walk carries any such entry at all (a machine's own per-drive records included).
	fmt.Printf("hidden %v %v %v\n", hiddenSet, contains(after, hiddenName+"="), hasHidden(after))

	// Shape of the WHOLE environment, not just the injected part.
	bad, why := malformed(after)
	fmt.Printf("well-formed %v %s\n", bad == "", why)

	// The block is OS memory the walk borrows and returns; repeats are where a lifetime defect
	// shows up.
	stable := true
	for i := 0; i < 50; i++ {
		if len(syscall.Environ()) != len(after) {
			stable = false
			break
		}
	}
	fmt.Printf("stable %v\n", stable)

	// A live walk sees a removal.
	if err := os.Unsetenv(sentinels[0].name); err != nil {
		fmt.Println("FATAL Unsetenv", err)
		os.Exit(1)
	}
	unset := syscall.Environ()
	fmt.Printf("unset %d %v\n", len(after)-len(unset), contains(unset, sentinels[0].name+"="))
}

// collect pulls the sentinel entries out of an environment and orders them, since block order is
// the OS's business.
func collect(env []string) []string {
	var out []string
	for _, e := range env {
		if strings.HasPrefix(e, prefix) {
			out = append(out, e)
		}
	}
	sort.Strings(out)
	return out
}

// getenvAgrees reports whether every walked entry's value matches a direct lookup of its name. The
// separator search starts at index 1 because an entry may legitimately BEGIN with '=' (Windows'
// per-drive records) -- the same rule Go's own Clearenv follows.
func getenvAgrees(env []string) bool {
	for _, e := range env {
		i := strings.Index(e[1:], "=")
		if i < 0 {
			return false
		}
		name := e[:i+1]
		// A hidden per-drive record is not addressable by name on either platform; skip it here
		// (its presence is reported separately) rather than reading a lookup failure as a mismatch.
		if strings.HasPrefix(name, "=") {
			continue
		}
		if got, ok := syscall.Getenv(name); !ok || got != e[i+2:] {
			return false
		}
	}
	return true
}

func hasHidden(env []string) bool {
	for _, e := range env {
		if strings.HasPrefix(e, "=") {
			return true
		}
	}
	return false
}

// malformed returns the first entry that is not a well-formed environment record, with a reason.
func malformed(env []string) (string, string) {
	for _, e := range env {
		if e == "" {
			return e, "empty"
		}
		if strings.ContainsRune(e, 0) {
			return e, "embedded-nul"
		}
		if strings.Index(e[1:], "=") < 0 {
			return e, "no-separator"
		}
	}
	return "", "ok"
}

func contains(env []string, p string) bool {
	for _, e := range env {
		if strings.HasPrefix(e, p) {
			return true
		}
	}
	return false
}

// ascii renders an entry with every non-printable and non-ASCII rune escaped, so a unicode value
// compares byte-for-byte without depending on the console's output encoding.
func ascii(s string) string {
	const hexDigits = "0123456789abcdef"

	out := make([]byte, 0, len(s))
	for _, r := range s {
		if r >= 0x20 && r < 0x7f {
			out = append(out, byte(r))
			continue
		}
		// Six digits, not four: the emoji sentinel is above the BMP, and a four-digit escape would
		// truncate its high bits identically on both sides -- hiding exactly the decode defect the
		// non-ASCII sentinel is here to catch.
		out = append(out, '\\', 'u')
		for shift := 20; shift >= 0; shift -= 4 {
			out = append(out, hexDigits[(r>>uint(shift))&0xf])
		}
	}
	return string(out)
}
