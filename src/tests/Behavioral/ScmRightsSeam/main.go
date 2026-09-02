// ScmRightsSeam guards the LINUX syscall.Recvmsg / syscall.SendmsgN struct-passing seam -- the
// ANCILLARY half of the sockaddr wall, whose addressed half SendtoSeam guards and whose receive
// half Recvfrom already covers.
//
// recvmsgRaw hands the kernel a MANAGED Msghdr: `Name` points at a managed RawSockaddrAny, `Iov`
// at a managed Iovec whose `Base` is an object reference, and `Control` into a managed slice --
// and the kernel WRITES the name and the control buffer. That is Recvfrom's defect with two write
// targets instead of one, which is the half of this class that corrupts the managed heap rather
// than merely misdirecting. SendmsgN is the same structs on the read side, i.e. Sendto's defect.
//
// WHY THE PAYLOAD IS NOT THE TEST. A payload-only round trip travels through `Iov` and would pass
// with `Control` pointing anywhere at all, so it cannot see the very buffer this seam exists for.
// Every assertion below therefore depends on the CONTROL buffer's contents: a file descriptor is
// passed through it with SCM_RIGHTS, and the received descriptor is USED. A wrong control address
// yields no descriptor, a truncated one, or a descriptor that does not read back what was written
// -- none of which the payload can hide.
//
// The proof that the received FD is the sent one is a value the KERNEL moved: the parent writes a
// known byte string into a pipe before sending its read end, and the child-side descriptor must
// read exactly that back. Two DIFFERENT descriptors cannot both do that.
package main

import (
	"fmt"
	"syscall"
)

func fatal(what string, err error) {
	if err != nil {
		fmt.Println(what, "failed:", err)
		panic(what)
	}
}

func main() {
	const secret = "scm-rights-payload"

	// A pipe whose READ end is the descriptor that travels through the control buffer.
	var pipeFds [2]int
	fatal("pipe2", syscall.Pipe2(pipeFds[:], 0))
	pipeRead, pipeWrite := pipeFds[0], pipeFds[1]
	defer syscall.Close(pipeRead)

	n, err := syscall.Write(pipeWrite, []byte(secret))
	fatal("write(pipe)", err)
	fatal("close(pipeWrite)", syscall.Close(pipeWrite))
	fmt.Println("bytes staged in the pipe:", n == len(secret))

	// The socketpair the descriptor is passed over.
	pair, err := syscall.Socketpair(syscall.AF_UNIX, syscall.SOCK_STREAM, 0)
	fatal("socketpair", err)
	sender, receiver := pair[0], pair[1]
	defer syscall.Close(sender)
	defer syscall.Close(receiver)

	// SEND: one payload byte (the kernel requires at least one) plus the descriptor in the
	// control buffer. This is SendmsgN's encode -- Msghdr, Iovec and the control image all reach
	// the kernel by address.
	rights := syscall.UnixRights(pipeRead)
	fmt.Println("control image is non-empty:", len(rights) > 0)

	sent, err := syscall.SendmsgN(sender, []byte{'x'}, rights, nil, 0)
	fatal("sendmsg", err)
	fmt.Println("payload bytes sent:", sent == 1)

	// RECEIVE: the kernel WRITES the control buffer here, which is the corrupting half.
	payload := make([]byte, 8)
	oob := make([]byte, syscall.CmsgSpace(4))
	rn, oobn, _, _, err := syscall.Recvmsg(receiver, payload, oob, 0)
	fatal("recvmsg", err)
	fmt.Println("payload byte received:", rn == 1 && payload[0] == 'x')
	fmt.Println("control bytes received:", oobn == len(oob))

	// DECODE: the control buffer must parse as exactly one SCM_RIGHTS message carrying one fd.
	scms, err := syscall.ParseSocketControlMessage(oob[:oobn])
	fatal("parse control", err)
	fmt.Println("control messages:", len(scms))

	fds, err := syscall.ParseUnixRights(&scms[0])
	fatal("parse rights", err)
	fmt.Println("descriptors received:", len(fds))

	// USE IT. A descriptor the kernel really transferred reads back what the pipe holds; a wrong
	// one is closed, invalid, or somebody else's.
	got := make([]byte, len(secret))
	rn, err = syscall.Read(fds[0], got)
	fatal("read(received fd)", err)
	syscall.Close(fds[0])
	fmt.Println("received descriptor reads the staged bytes:", rn == len(secret) && string(got[:rn]) == secret)
}
