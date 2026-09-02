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
//
// THE CONTROL-ONLY PHASE IS A SECOND SHAPE, NOT A REPETITION. Go's sendmsgN supplies a dummy
// byte itself when the caller passes an empty payload on a stream socket, and then reports 0
// written -- the byte the kernel counted was not the caller's. Phase 1 above passes a real
// payload byte, so it can never reach that tail; a hand-own of sendmsgN that drops it answers
// 1 where Go answers 0 and phase 1 stays green, which is exactly what happened. The pairing
// matters as much as the count: `sent == 0` alone is satisfied by a send that did nothing, so
// the descriptor must still arrive and still read the staged bytes.
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

	// PHASE 2: the control-only send. Empty payload, so sendmsgN's own dummy byte is what the
	// kernel counts and the reported count must be 0.
	var pipe2Fds [2]int
	fatal("pipe2 (phase 2)", syscall.Pipe2(pipe2Fds[:], 0))
	pipe2Read, pipe2Write := pipe2Fds[0], pipe2Fds[1]
	defer syscall.Close(pipe2Read)

	n2, err := syscall.Write(pipe2Write, []byte(secret))
	fatal("write(pipe 2)", err)
	fatal("close(pipe2Write)", syscall.Close(pipe2Write))
	fmt.Println("bytes staged in the second pipe:", n2 == len(secret))

	pair2, err := syscall.Socketpair(syscall.AF_UNIX, syscall.SOCK_STREAM, 0)
	fatal("socketpair (phase 2)", err)
	sender2, receiver2 := pair2[0], pair2[1]
	defer syscall.Close(sender2)
	defer syscall.Close(receiver2)

	rights2 := syscall.UnixRights(pipe2Read)
	sent2, err := syscall.SendmsgN(sender2, nil, rights2, nil, 0)
	fatal("sendmsg (control only)", err)
	fmt.Println("control-only send reports no payload bytes:", sent2 == 0)

	// ... and it really sent: the descriptor arrives and reads the staged bytes back.
	payload2 := make([]byte, 8)
	oob2 := make([]byte, syscall.CmsgSpace(4))
	_, oobn2, _, _, err := syscall.Recvmsg(receiver2, payload2, oob2, 0)
	fatal("recvmsg (phase 2)", err)
	scms2, err := syscall.ParseSocketControlMessage(oob2[:oobn2])
	fatal("parse control (phase 2)", err)
	fds2, err := syscall.ParseUnixRights(&scms2[0])
	fatal("parse rights (phase 2)", err)

	got2 := make([]byte, len(secret))
	rn2, err := syscall.Read(fds2[0], got2)
	fatal("read(received fd, phase 2)", err)
	syscall.Close(fds2[0])
	fmt.Println("control-only descriptor reads the staged bytes:", rn2 == len(secret) && string(got2[:rn2]) == secret)
}
