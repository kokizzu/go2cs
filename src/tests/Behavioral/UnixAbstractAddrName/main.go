// Guards Go's AF_UNIX name decode for an ABSTRACT (autobound) unix socket.
//
// Go's anyToSockaddr rewrites a leading NUL in sun_path as '@' and then scans to the first NUL
// over the FIXED 108-byte array, ignoring the length the kernel reported. That matters exactly
// once: an autobound abstract socket, for which Linux reports addrlen == 2 (the family alone).
// Go therefore finds a zeroed Path[0], rewrites it, stops at Path[1], and answers "@".
//
// The converted decode bounded both steps on addrlen-2 -- zero for that case -- so the rewrite was
// skipped and the name came back EMPTY. Measured before the fix: Go len 1 / first byte 64 against
// converted len 0 / no first byte. That is what made net's TestUnixConnLocalAndRemoteNames fail:
// its row 1 compares LocalAddr() against &UnixAddr{Name:"@"}, so reflect.DeepEqual was correctly
// reporting two genuinely different values, and the failure printed as two hex words only because
// the test host's formatter could not render %#v.
//
// The output is deterministic: Go's answer is exactly "@", not a kernel-assigned name.
//
// PLATFORM-EXCLUSIVE, not early-out. This package is marked [GoPlatformExclusive("linux")] and is
// skipped by name on every other host, following SendtoSeam's recorded convention rather than the
// runtime.GOOS early-out it replaced. Two reasons, both of which an early-out leaves standing. The
// abstract namespace is a LINUX kernel feature -- a leading NUL in sun_path names no filesystem
// entry -- so DialUnix with a nil laddr autobinds nothing to rewrite on any other platform, and the
// three printed lines are answers only here. And the GOLDEN cannot be shared: net's converted
// emission differs by platform (the Windows syscall flavor mints the Δ-prefixed aliases the linux
// one does not), so a committed .cs would be one platform's and read as standing drift on the other.
// An early-out fixes neither, and its branch could never run under the skip, which is why there is
// none here.
package main

import (
	"fmt"
	"net"
	"os"
	"path/filepath"
)

func main() {
	dir, err := os.MkdirTemp("", "abstractaddr")
	if err != nil {
		fmt.Println("FATAL mkdtemp:", err)
		return
	}
	defer os.RemoveAll(dir)

	ta, err := net.ResolveUnixAddr("unix", filepath.Join(dir, "s"))
	if err != nil {
		fmt.Println("FATAL resolve:", err)
		return
	}
	ln, err := net.ListenUnix("unix", ta)
	if err != nil {
		fmt.Println("FATAL listen:", err)
		return
	}
	defer ln.Close()
	go func() {
		if c, e := ln.Accept(); e == nil {
			c.Close()
		}
	}()

	// A nil local address makes the kernel autobind an ABSTRACT name.
	c, err := net.DialUnix("unix", nil, ta)
	if err != nil {
		fmt.Println("FATAL dial:", err)
		return
	}
	defer c.Close()

	la := c.LocalAddr().(*net.UnixAddr)
	fmt.Println("net       =", la.Net)
	fmt.Println("name len  =", len(la.Name))
	fmt.Println("name is @ =", la.Name == "@")
}
