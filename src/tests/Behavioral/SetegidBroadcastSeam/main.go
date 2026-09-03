//go:build linux

// SetegidBroadcastSeam -- the guard for DESIGN-cgocaller-keystone.md §2 increment 1.
//
// Go's credential setters must change the credentials of EVERY thread in the process, not just the
// calling one: that is the whole reason syscall_linux.go carries two implementations of each. The
// discriminating observation is therefore NOT that Setegid returns nil -- any implementation that
// stops answering ENOTSUP would satisfy that -- but that a thread which is parked, running no code
// of its own, has its credentials changed underneath it.
//
// A raw setresgid(2) changes the CALLING thread only, so a parked thread does not follow it.
// glibc's nptl setxid broadcast, which cgo builds reach through cgo_libc_setegid, does reach it.
// This program prints both threads' /proc/self/task/<tid>/status Gid line before and after, so the
// two mechanisms produce visibly different output and the golden cannot be satisfied by the wrong
// one.
package main

import (
	"fmt"
	"os"
	"runtime"
	"strings"
	"syscall"
)

// gidLine reports the four numbers of a thread's Gid: line -- real, effective, saved, fs -- which
// is exactly the field TestSetuidEtc compares.
func gidLine(tid int) string {
	b, err := os.ReadFile(fmt.Sprintf("/proc/self/task/%d/status", tid))
	if err != nil {
		return "read error"
	}
	for _, line := range strings.Split(string(b), "\n") {
		if strings.HasPrefix(line, "Gid:") {
			return strings.Join(strings.Fields(line[4:]), " ")
		}
	}
	return "no Gid line"
}

func main() {
	if runtime.GOOS != "linux" {
		fmt.Println("not linux; nothing to observe")
		return
	}

	// LOUD skip: this seam changes the process's effective gid, which only root may do. A silent
	// skip would let an unprivileged host read as a pass.
	if os.Geteuid() != 0 {
		fmt.Println("SKIP: needs root -- this seam changes the process's effective gid and reads /proc")
		return
	}

	tidCh := make(chan int)
	release := make(chan struct{})
	done := make(chan string)

	go func() {
		// Pin this goroutine to its own OS thread and park it. It runs no code at all while main
		// changes credentials, so nothing it does can be responsible for what it observes.
		runtime.LockOSThread()
		defer runtime.UnlockOSThread()
		tid := syscall.Gettid()
		tidCh <- tid
		<-release
		done <- gidLine(tid)
	}()

	parked := <-tidCh
	self := syscall.Gettid()

	fmt.Println("before main  :", gidLine(self))
	fmt.Println("before parked:", gidLine(parked))

	if err := syscall.Setegid(1); err != nil {
		fmt.Println("setegid(1) failed:", err)
		return
	}

	mainAfter := gidLine(self)
	fmt.Println("after  main  :", mainAfter)

	close(release)
	parkedAfter := <-done
	fmt.Println("after  parked:", parkedAfter)
	fmt.Println("the parked thread followed the change:", parkedAfter == mainAfter)

	if err := syscall.Setegid(0); err != nil {
		fmt.Println("restore failed:", err)
		return
	}
	fmt.Println("restored main:", gidLine(self))
}
