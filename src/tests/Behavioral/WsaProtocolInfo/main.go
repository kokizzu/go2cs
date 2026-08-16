// WsaProtocolInfo guards the two Winsock struct-passing seams on the `net` INITIALISATION path --
// WSAStartup's WSADATA and WSAEnumProtocols' WSAPROTOCOL_INFOW -- both of which the kernel writes at
// an address the wrapper hands it.
//
// Go's structs match those native layouts exactly because their character buffers are INLINE:
// WSAData ends in [257]byte and [129]byte, and WSAProtocolInfo ends in [256]uint16, nested inside a
// GUID ([8]byte) and a WSAProtocolChain ([7]uint32). The converted structs cannot be: golib holds a
// Go fixed array as an `array<T>` MANAGED REFERENCE -- one word where Windows writes 512 (or 257, or
// 129) bytes of storage. A converted WSAProtocolInfo is roughly 120 bytes against the native record's
// 628, so an enumeration of 32 records is 20,096 bytes of kernel writes into a ~3.8 KB managed array:
// a multi-kilobyte heap overwrite that also fabricates object references wherever it lands on an
// `array<T>` field. Both wrappers are now hand-owned against blittable mirrors
// (syscall/windows/zsyscall_windows_wsa_impl.cs).
//
// A no-fault check proves nothing here -- a mirror with the wrong offsets returns garbage WITHOUT
// crashing, which is the shape this class takes most often. So every line below is a value the
// boundary copy has to get right, and every one of them is host-INVARIANT (no counts, no flag words,
// no vendor strings), spread across the whole of both native records:
//
//	WSADATA
//	  Version        offset   0  -- echoed back as the version negotiated, 2.2
//	  HighVersion    offset   2  -- the highest the provider supports, 2.2 on every supported Windows
//	  Description    offset  16  -- a NUL-terminated ASCII string, read for shape not content
//
//	WSAPROTOCOL_INFOW (asked for IPPROTO_TCP, so the CATALOG guarantees what comes back)
//	  ProtocolChain  offset  40  -- ChainLen within [0, MAX_PROTOCOL_CHAIN]
//	  AddressFamily  offset  76  -- AF_INET or AF_INET6, nothing else answers a TCP query
//	  SocketType     offset  88  -- SOCK_STREAM, for every entry
//	  Protocol       offset  92  -- IPPROTO_TCP, for every entry
//	  ProtocolName   offset 116  -- NUL-terminated printable UTF-16, the last 512 bytes of the record
//
// The three fields at 76/88/92 are the load-bearing ones: they sit past the nested GUID and
// WSAProtocolChain, whose inline arrays are exactly what the managed layout collapses, so a record
// read at managed strides cannot agree with them by luck. ProtocolName then pins the FAR end of the
// record, 512 bytes further on.
//
// THE SIZE IS AN INPUT, and it is checked separately. WSAEnumProtocols is told how many BYTES the
// buffer holds and, when that is too few, rewrites it with the size the catalog needs -- in NATIVE
// strides. A required size that is not a whole number of WSAPROTOCOL_INFOW records means the boundary
// is measuring the wrong struct, which is the same defect Process32First's dwSize has.
//
// WHY IT MATTERS BEYOND THE CORRUPTION: internal/poll's checkSetFileCompletionNotificationModes runs
// this exact enumeration once per process that imports `net`, and sets
// useSetFileCompletionNotificationModes only when EVERY returned entry carries XP1_IFS_HANDLES. That
// flag decides FD.skipSyncNotif -- whether a synchronously-completing overlapped operation returns
// immediately or waits for a completion packet -- so a corrupt answer silently changes the IO path
// the managed netpoller takes (docs/phase4/DESIGN-netpoll-managed-poller.md, OQ5). The decision is
// printed below as a bool: it is a property of the host's Winsock catalog, so Go and the conversion
// must agree on it, whatever it is.
package main

import (
	"fmt"
	"syscall"
	"unsafe"
)

func main() {
	var data syscall.WSAData
	if err := syscall.WSAStartup(uint32(0x202), &data); err != nil {
		fmt.Println("FATAL WSAStartup", err)
		return
	}
	defer syscall.WSACleanup()

	fmt.Println("-- WSAStartup --")
	// 0x202 is 2.2 little-endian in the WORD Windows echoes back, and 2.2 is also the highest
	// version every supported Windows reports, so both are fixed values rather than host facts.
	fmt.Println("version negotiated:", data.Version == 0x202)
	fmt.Println("high version:", data.HighVersion == 0x202)
	fmt.Println("description printable:", printableASCII(data.Description[:]))

	fmt.Println("-- WSAEnumProtocols --")

	// The same query internal/poll issues: a NUL-terminated list holding IPPROTO_TCP alone.
	protos := [2]int32{syscall.IPPROTO_TCP, 0}

	var buf [32]syscall.WSAProtocolInfo
	length := uint32(unsafe.Sizeof(buf))

	n, err := syscall.WSAEnumProtocols(&protos[0], &buf[0], &length)
	if err != nil {
		fmt.Println("FATAL WSAEnumProtocols", err)
		return
	}

	fmt.Println("entry count in range:", n >= 1 && n <= int32(len(buf)))

	chainOK, familyOK, typeOK, protoOK, nameOK := true, true, true, true, true
	for i := int32(0); i < n; i++ {
		p := buf[i]
		if p.ProtocolChain.ChainLen < 0 || p.ProtocolChain.ChainLen > syscall.MAX_PROTOCOL_CHAIN {
			chainOK = false
		}
		if p.AddressFamily != syscall.AF_INET && p.AddressFamily != syscall.AF_INET6 {
			familyOK = false
		}
		if p.SocketType != syscall.SOCK_STREAM {
			typeOK = false
		}
		if p.Protocol != syscall.IPPROTO_TCP {
			protoOK = false
		}
		if !printableUTF16(p.ProtocolName[:]) {
			nameOK = false
		}
	}

	fmt.Println("chain length in range:", chainOK)
	fmt.Println("address family is inet:", familyOK)
	fmt.Println("socket type is stream:", typeOK)
	fmt.Println("protocol is tcp:", protoOK)
	fmt.Println("protocol name printable:", nameOK)

	// The DECISION internal/poll derives from the enumeration.
	ifs := true
	for i := int32(0); i < n; i++ {
		if buf[i].ServiceFlags1&syscall.XP1_IFS_HANDLES == 0 {
			ifs = false
		}
	}
	fmt.Println("every entry carries XP1_IFS_HANDLES:", ifs)

	fmt.Println("-- required size --")

	// A buffer declared to hold nothing must be refused, and the size that comes back must be a
	// whole number of NATIVE records -- the check that the boundary measured the right struct.
	record := uint32(unsafe.Sizeof(buf[0]))
	required := uint32(0)

	m, err := syscall.WSAEnumProtocols(&protos[0], &buf[0], &required)
	fmt.Println("undersized buffer refused:", m == -1 && err != nil)
	fmt.Println("required size is whole records:", required >= record && required%record == 0)
	fmt.Println("required size matches entry count:", required == uint32(n)*record)
}

// printableASCII reports whether b is a non-empty NUL-terminated run of printable ASCII. Content is
// deliberately not compared: the WSADATA description is a provider string ("WinSock 2.0" today), and
// its SHAPE is what a wrong offset destroys.
func printableASCII(b []byte) bool {
	if len(b) == 0 || b[0] == 0 {
		return false
	}
	for _, c := range b {
		if c == 0 {
			return true
		}
		if c < 0x20 || c > 0x7e {
			return false
		}
	}
	return false
}

// printableUTF16 is printableASCII over the protocol name's WCHAR buffer. Every name in the Windows
// TCP catalog is ASCII ("MSAFD Tcpip [TCP/IP]"), so a rune outside that range means the 512 bytes at
// the end of the record are not the ones Windows wrote.
func printableUTF16(s []uint16) bool {
	if len(s) == 0 || s[0] == 0 {
		return false
	}
	for _, r := range s {
		if r == 0 {
			return true
		}
		if r < 0x20 || r > 0x7e {
			return false
		}
	}
	return false
}
