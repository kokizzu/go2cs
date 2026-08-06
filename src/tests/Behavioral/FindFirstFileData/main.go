// FindFirstFileData guards the Win32 FindFirstFileW / FindNextFileW struct-passing seam. The kernel
// writes a 592-byte WIN32_FIND_DATAW at the address the wrapper hands it, and Go's win32finddata1
// matches that layout exactly because its two name buffers ([260]uint16 and [14]uint16) are INLINE.
// The converted struct cannot be: golib holds a Go fixed array as an `array<uint16>` MANAGED
// reference, one word where Windows expects 520 (or 28) bytes of storage. Handing the kernel that
// address wrote the native record over a much smaller managed object, so the next read of a name
// field faulted -- an IndexOutOfRangeException in PinnedBuffer, or an outright ACCESS_VIOLATION.
// findFirstFile1 / findNextFile1 are now hand-owned against a blittable mirror
// (syscall/zsyscall_windows_impl.cs).
//
// A no-fault check proves nothing here: a mirror with the wrong offsets returns garbage WITHOUT
// crashing. So this prints values that depend on every field the copy has to get right, spread
// across the whole native record:
//
//	FileAttributes    offset   0   -- the directory bit, per entry
//	LastWriteTime     offset  20   -- stamped to a DISTINCT fixed instant per entry
//	FileSizeLow       offset  32   -- three files written at three distinct known sizes
//	FileName          offset  44   -- the names themselves, ASCII and non-ASCII
//	AlternateFileName offset 564   -- the 8.3 short name (empty where the volume has 8.3 disabled)
//
// Reserved0 (offset 36) is deliberately NOT printed. Windows documents it as the reparse-point tag
// only when FileAttributes has FILE_ATTRIBUTE_REPARSE_POINT and UNDEFINED otherwise, so a tree with
// no reparse point in it has nothing stable to compare -- and creating one needs either
// SeCreateSymbolicLinkPrivilege or a raw FSCTL_SET_REPARSE_POINT, neither of which belongs in a test
// that has to byte-match on any machine. Its offset is nonetheless pinned from both sides: FileSizeLow
// at 32 and FileName at 44 are both verified below, and Reserved0/Reserved1 are the only 8 bytes
// between them.
//
// FindNextFile has its own root: the enumeration loop below is the only thing that reaches it, and
// it reuses one Win32finddata across every entry. path/filepath.EvalSymlinks is the real consumer --
// on Windows its normBase asks FindFirstFile for the on-disk spelling of each path element, so a
// lower-cased path comes back in canonical case.
package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"syscall"
	"time"
)

type entry struct {
	name  string
	alt   string
	isDir bool
	size  uint32
	mtime int64
}

// The tree, and the fixed instant each element is stamped with. Distinct stamps mean a LastWriteTime
// read at the wrong offset cannot coincidentally agree with the right one. Names stress both WCHAR
// buffers: one well past 8.3 (so the short name has to differ from the long one), one non-ASCII (so
// the UTF-16 decode has to be exact), mixed case throughout (so EvalSymlinks has something to
// restore).
var (
	dirs = []struct {
		rel   string
		stamp time.Time
	}{
		{"", time.Date(2021, time.February, 3, 4, 5, 6, 0, time.UTC)},
		{"LongDirectoryNameWellBeyondEightDotThree", time.Date(2021, time.February, 3, 5, 6, 7, 0, time.UTC)},
		{"ÜnicödeDir", time.Date(2021, time.February, 3, 6, 7, 8, 0, time.UTC)},
	}
	files = []struct {
		rel   string
		size  int
		stamp time.Time
	}{
		{"ReadMe.txt", 7, time.Date(2022, time.March, 4, 5, 6, 7, 0, time.UTC)},
		{"LongDirectoryNameWellBeyondEightDotThree\\Payload.bin", 300, time.Date(2023, time.April, 5, 6, 7, 8, 0, time.UTC)},
		{"ÜnicödeDir\\Inner.dat", 65, time.Date(2024, time.May, 6, 7, 8, 9, 0, time.UTC)},
	}
)

func main() {
	// NOT os.MkdirTemp: that reaches runtime_rand, a separate unimplemented root. A pid-suffixed
	// name is unique enough, and never appears in the output.
	root := filepath.Join(os.TempDir(), fmt.Sprintf("go2cs_findfirstfile_%d", os.Getpid()))
	os.RemoveAll(root)

	build(root)

	for _, d := range dirs {
		fmt.Printf("-- enumerate %s --\n", ascii(shown(d.rel)))
		for _, e := range enumerate(filepath.Join(root, d.rel)) {
			fmt.Printf("%s alt=%s dir=%v size=%d mtime=%d\n", ascii(e.name), ascii(e.alt), e.isDir, e.size, e.mtime)
		}
	}

	fmt.Println("-- single lookup --")
	one := lookup(filepath.Join(root, "ReadMe.txt"))
	fmt.Printf("%s alt=%s dir=%v size=%d mtime=%d\n", ascii(one.name), ascii(one.alt), one.isDir, one.size, one.mtime)

	fmt.Println("-- missing --")
	_, err := find(filepath.Join(root, "NoSuchFile.txt"))
	fmt.Println(err == syscall.ERROR_FILE_NOT_FOUND)

	// filepath.EvalSymlinks -> toNorm -> normBase -> FindFirstFile: the canonical spelling of each
	// element comes straight out of the FileName buffer, so a lower-cased path round-trips back to
	// the case the tree was created with.
	fmt.Println("-- canonical case --")
	rootEval, err := filepath.EvalSymlinks(root)
	if err != nil {
		fatal("EvalSymlinks(root)", err)
	}
	for _, f := range files {
		got, err := filepath.EvalSymlinks(strings.ToLower(filepath.Join(root, f.rel)))
		if err != nil {
			fatal("EvalSymlinks", err)
		}
		rel, err := filepath.Rel(rootEval, got)
		if err != nil {
			fatal("Rel", err)
		}
		fmt.Println(ascii(rel))
	}

	os.RemoveAll(root)
}

func build(root string) {
	for _, d := range dirs {
		if err := os.MkdirAll(filepath.Join(root, d.rel), 0o755); err != nil {
			fatal("MkdirAll", err)
		}
	}

	for _, f := range files {
		body := make([]byte, f.size)
		for i := range body {
			body[i] = byte('a' + i%26)
		}
		if err := os.WriteFile(filepath.Join(root, f.rel), body, 0o644); err != nil {
			fatal("WriteFile", err)
		}
	}

	// Files first, then directories: writing into a directory bumps that directory's own mtime.
	for _, f := range files {
		if err := os.Chtimes(filepath.Join(root, f.rel), f.stamp, f.stamp); err != nil {
			fatal("Chtimes", err)
		}
	}
	for i := len(dirs) - 1; i >= 0; i-- {
		if err := os.Chtimes(filepath.Join(root, dirs[i].rel), dirs[i].stamp, dirs[i].stamp); err != nil {
			fatal("Chtimes", err)
		}
	}
}

// enumerate is the FindNextFile root: FindFirstFile opens the search and every entry after the first
// arrives through FindNextFile, into the SAME reused Win32finddata.
func enumerate(dir string) []entry {
	pattern, err := syscall.UTF16PtrFromString(dir + `\*`)
	if err != nil {
		fatal("UTF16PtrFromString", err)
	}

	var data syscall.Win32finddata

	h, err := syscall.FindFirstFile(pattern, &data)
	if err != nil {
		fatal("FindFirstFile", err)
	}
	defer syscall.FindClose(h)

	var out []entry
	for {
		if e, ok := toEntry(&data); ok {
			out = append(out, e)
		}
		if err := syscall.FindNextFile(h, &data); err != nil {
			if err == syscall.ERROR_NO_MORE_FILES {
				break
			}
			fatal("FindNextFile", err)
		}
	}

	// Directory order is the filesystem's business; the comparison needs a fixed one.
	sort.Slice(out, func(i, j int) bool { return out[i].name < out[j].name })
	return out
}

func lookup(path string) entry {
	e, err := find(path)
	if err != nil {
		fatal("FindFirstFile", err)
	}
	return e
}

func find(path string) (entry, error) {
	p, err := syscall.UTF16PtrFromString(path)
	if err != nil {
		return entry{}, err
	}

	var data syscall.Win32finddata

	h, err := syscall.FindFirstFile(p, &data)
	if err != nil {
		return entry{}, err
	}
	syscall.FindClose(h)

	e, _ := toEntry(&data)
	return e, nil
}

// toEntry reads every field the boundary copy has to carry, and reports false for the "." and ".."
// entries a directory search always leads with.
func toEntry(data *syscall.Win32finddata) (entry, bool) {
	name := syscall.UTF16ToString(data.FileName[:])
	if name == "." || name == ".." {
		return entry{}, false
	}
	return entry{
		name:  name,
		alt:   altName(data),
		isDir: data.FileAttributes&syscall.FILE_ATTRIBUTE_DIRECTORY != 0,
		size:  data.FileSizeLow,
		mtime: data.LastWriteTime.Nanoseconds(),
	}, true
}

// altName reads the 8.3 short name, which is empty for a name that already fits 8.3 (and for every
// name where the volume has 8.3 generation disabled). The explicit element-0 read is the check that
// matters here: an AlternateFileName field the kernel clobbered would not be readable at all.
//
// It doubled as a workaround until 2026-08-02. The converted syscall.UTF16ToString of an ALL-NUL
// buffer threw IndexOutOfRangeException, because it ends in unsafe.String(SliceData(empty), 0) and
// SliceData hands over a zero-length buffer whose element 0 does not exist. That was a defect in
// the converted unsafe.String, unrelated to the FindFirstFile seam this test guards; it is fixed
// (unsafe.String returns "" for a zero length without pinning its pointer) and guarded in its own
// right by the UnsafeStringEmpty behavioral test, so this branch is now redundant for that purpose.
func altName(data *syscall.Win32finddata) string {
	if data.AlternateFileName[0] == 0 {
		return ""
	}
	return syscall.UTF16ToString(data.AlternateFileName[:])
}

func shown(rel string) string {
	if rel == "" {
		return "<root>"
	}
	return rel
}

// ascii renders a name with every non-printable and non-ASCII rune escaped, so a unicode filename
// can be compared byte-for-byte without depending on the console's output encoding.
func ascii(s string) string {
	const hexDigits = "0123456789abcdef"

	out := make([]byte, 0, len(s))
	for _, r := range s {
		if r >= 0x20 && r < 0x7f {
			out = append(out, byte(r))
			continue
		}
		out = append(out, '\\', 'u')
		for shift := 12; shift >= 0; shift -= 4 {
			out = append(out, hexDigits[(r>>uint(shift))&0xf])
		}
	}
	return string(out)
}

func fatal(what string, err error) {
	fmt.Println("FATAL", what, err)
	os.Exit(1)
}
