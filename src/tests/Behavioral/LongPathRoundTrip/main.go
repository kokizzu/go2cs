// LongPathRoundTrip guards the semantics of a path longer than Windows' MAX_PATH travelling through
// os -- the behavior runtime.canUseLongPaths decides, and the reason wiring that flag is a two-part
// change rather than a one-line one.
//
// WHAT DECIDES THE BEHAVIOR
//   os.fixLongPath consults internal/syscall/windows.CanUseLongPaths, which Go's //go:linkname
//   aliases onto runtime.canUseLongPaths. True: the path is handed to the kernel as written. False:
//   os rewrites it into the extended-length \\?\ form first. Go's runtime sets the flag in
//   initLongPathSupport, right after it sets the PEB's IsLongPathAwareProcess bit -- and in the
//   converted model NEITHER half runs (osinit is the bootstrap go2cs emits already marked not-run,
//   and the body's stdcall bottoms out in a throwing stub). golib performs the PEB write from
//   InitializeGoLib, and runtime/windows/os_windows_impl.cs copies the OUTCOME of that write into
//   canUseLongPaths.
//
// WHAT THIS TEST PROVES, AND WHAT IT CANNOT
//   It proves the ROUND TRIP: every os operation this program performs against a >MAX_PATH path must
//   produce the same observed values in the converted C# as in the Go binary. That is exactly the
//   regression surface the flag creates, because the flag changes which spelling reaches the kernel
//   for every long path in the corpus -- and it catches the specific failure the populate half is
//   designed against: a canUseLongPaths set true while the PEB bit is NOT actually set stops os
//   prefixing paths that then silently fail, so C# would report errors here where Go reports none.
//   That is the "plausible-looking wrong answer" case, and it goes red.
//
//   It deliberately does NOT claim to observe the flag's VALUE, because -- measured, not assumed --
//   nothing a Go program can do through os distinguishes the two spellings. addExtendedPrefix
//   normalizes through GetFullPathName BEFORE prepending \\?\, so the classic tells all come out
//   identical either way: a `.` segment, a `..` segment, a doubled separator, forward slashes, and
//   even a trailing dot resolve the same with the prefix as without it. (A first cut of that
//   measurement passed the prefix explicitly, which skips the normalization and made six probes look
//   like sharp discriminators; they are not.) The divergence the flag removes is real but is a
//   difference in the string a syscall RECEIVES, not in any answer os hands back -- which is why
//   golib called leaving it false "the conservative side" rather than a bug.
//
// A no-fault check would not be enough here, per the LocalTimeZone precedent: a broken long-path
// path can return wrong data without faulting. So every step prints values that depend on the bytes
// actually reaching and leaving the filesystem -- content, sizes, the directory listing, the
// rename's effect -- rather than merely "err == nil".
//
// NOTHING MACHINE-SPECIFIC IS PRINTED. os.MkdirTemp's directory differs per run and per machine, so
// the absolute path never appears in the output; only derived facts (a length THRESHOLD, base names,
// sizes, contents, sorted listings) do. Those are identical for the Go binary and the converted C#
// running on the same machine, which is what the output comparison checks.
package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
)

func main() {
	base, err := os.MkdirTemp("", "go2cs-longpath")

	if err != nil {
		fmt.Println("mkdirtemp failed:", err)
		return
	}

	defer os.RemoveAll(base)

	// Twelve 24-character segments put the leaf well past MAX_PATH (260) from any plausible temp
	// root, without approaching the ~32767 ceiling either spelling has.
	deep := base

	for i := 0; i < 12; i++ {
		deep = filepath.Join(deep, strings.Repeat("d", 24))
	}

	fmt.Println("exceeds MAX_PATH:", len(deep) > 260)

	if err := os.MkdirAll(deep, 0o755); err != nil {
		fmt.Println("mkdirall failed:", err)
		return
	}

	// The directory really exists, and is a directory.
	dirInfo, err := os.Stat(deep)
	fmt.Println("dir stat:", err == nil, dirInfo != nil && dirInfo.IsDir(), dirInfo != nil && dirInfo.Name() == strings.Repeat("d", 24))

	// WRITE then READ BACK. The content is what proves the bytes reached the file and came back:
	// a long path that "succeeds" against the wrong target would read empty or stale.
	sep := string(filepath.Separator)
	name := "probe.txt"
	file := deep + sep + name
	payload := "the quick brown fox jumps over the lazy dog"

	if err := os.WriteFile(file, []byte(payload), 0o644); err != nil {
		fmt.Println("writefile failed:", err)
		return
	}

	data, err := os.ReadFile(file)
	fmt.Println("read back:", err == nil, string(data) == payload, len(data))

	info, err := os.Stat(file)
	fmt.Println("file stat:", err == nil, info != nil && info.Size() == int64(len(payload)), info != nil && !info.IsDir(), info != nil && info.Name() == name)

	// APPEND through a second handle: exercises OpenFile's own fixLongPath call, which is a
	// different call site from ReadFile/WriteFile.
	handle, err := os.OpenFile(file, os.O_APPEND|os.O_WRONLY, 0o644)

	if err == nil {
		_, werr := handle.WriteString("!")
		cerr := handle.Close()
		fmt.Println("append:", werr == nil, cerr == nil)
	} else {
		fmt.Println("openfile failed:", err)
	}

	grown, err := os.ReadFile(file)
	fmt.Println("after append:", err == nil, len(grown) == len(payload)+1, strings.HasSuffix(string(grown), "g!"))

	// A SECOND entry, so the listing below has something to order.
	second := deep + sep + "another.dat"

	if err := os.WriteFile(second, []byte{1, 2, 3}, 0o644); err != nil {
		fmt.Println("second writefile failed:", err)
	}

	// DIRECTORY LISTING at the long path -- ReadDir reaches the platform's directory enumeration
	// (FindFirstFileW / FindNextFileW on Windows) with the long path in hand.
	entries, err := os.ReadDir(deep)
	names := make([]string, 0, len(entries))

	for _, entry := range entries {
		names = append(names, fmt.Sprintf("%s:%v", entry.Name(), entry.IsDir()))
	}

	sort.Strings(names)
	fmt.Println("readdir:", err == nil, len(names), strings.Join(names, ","))

	// NORMALIZATION, through the long path. These are the forms the two spellings were suspected of
	// treating differently; they are measured to agree, so what they pin here is that the CONVERTED
	// os/syscall resolves them exactly as Go does -- which is a real property, and platform-specific
	// (a trailing dot is stripped on Windows and is part of the name on Linux). Go and the converted
	// C# run on the same machine, so both sides see the same answer whatever it is.
	probes := []struct {
		label string
		path  string
	}{
		{"dot-segment", deep + sep + "." + sep + name},
		{"dotdot-segment", deep + sep + "nope" + sep + ".." + sep + name},
		{"double-separator", deep + sep + sep + name},
		{"trailing-dot", file + "."},
	}

	for _, probe := range probes {
		probeInfo, probeErr := os.Stat(probe.path)
		fmt.Println("normalize "+probe.label+":", probeErr == nil, probeInfo != nil && probeInfo.Size() == int64(len(payload)+1))
	}

	// RENAME within the long directory, then confirm the move really happened in both directions.
	renamed := deep + sep + "renamed.txt"
	renameErr := os.Rename(file, renamed)
	_, oldStatErr := os.Stat(file)
	moved, readErr := os.ReadFile(renamed)
	fmt.Println("rename:", renameErr == nil, os.IsNotExist(oldStatErr), readErr == nil, len(moved) == len(payload)+1)

	// REMOVE one entry, then the whole tree. RemoveAll walks the long path itself, so a spelling that
	// cannot reach these files leaves the tree behind rather than failing loudly.
	fmt.Println("remove one:", os.Remove(renamed) == nil)

	removeErr := os.RemoveAll(base)
	_, goneErr := os.Stat(base)
	fmt.Println("removeall:", removeErr == nil, os.IsNotExist(goneErr))
}
