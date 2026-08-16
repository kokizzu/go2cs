// LookupServicePort guards the hand-owned syscall.GetAddrInfoW / FreeAddrInfoW pair
// (syscall/windows/zsyscall_windows_addrinfo_impl.cs) -- the NAME-RESOLUTION members of the syscall
// struct-passing class. The generated wrappers hand the kernel the ADDRESS of a managed AddrinfoW,
// whose Canonname / Addr / Next are managed references where native ADDRINFOW has raw pointers, so
// Windows wrote a native `*ADDRINFOW` into a reference slot and the process died with
// `Fatal error. 0xC0000005` inside Syscall6 -- measured first from crypto/tls's TestVerifyHostname,
// and reachable from every converted program that resolves a name or a service.
//
// net.LookupPort is the one reach into that pair that needs neither DNS nor a network: it asks
// GetAddrInfoW to resolve a SERVICE name and reads the port back out of the returned sockaddr. That
// makes it the whole mechanism in one call --
//
//	the hints mirror     -- network selects Socktype/Protocol, and "tcp4"/"tcp6" additionally pin
//	                        ai_family, so the request Windows reads has to be the request Go built.
//	the chain copy       -- the result is a linked list of native records transcribed into managed
//	                        ж<AddrinfoW> boxes; a wrong Next or a wrong Family reads as a wrong port
//	                        or an error, never as a crash.
//	the pointer handoff  -- `(*syscall.RawSockaddrInet4)(unsafe.Pointer(result.Addr))` is a MANAGED
//	                        pointer projected to a scalar and converted back, carried across the
//	                        `syscall.Pointer` field by golib's ManagedPointerTokens.
//	the sockaddr decode  -- Port lives in the sockaddr in NETWORK byte order and the caller finishes
//	                        with Ntohs, so a byte-order slip in the transcription prints a swapped
//	                        port (443 -> 47873) rather than failing.
//
// tcp4 and tcp6 are both asked because the transcription picks the managed sockaddr TYPE from
// ai_family: the first exercises sockaddr_in, the second sockaddr_in6.
//
// Nothing host-varying is printed. The ports come from the host's own services database, so Go and
// the conversion read the same answers on any machine, and a service the host does not know prints
// "error" on both sides rather than a message whose text could differ.
package main

import (
	"fmt"
	"net"
)

func main() {
	queries := []struct {
		network string
		service string
	}{
		{"tcp", "http"},
		{"tcp", "https"},
		{"tcp", "domain"},
		{"udp", "domain"},
		{"tcp4", "https"},
		{"tcp6", "https"},
		{"tcp", "go2cs-no-such-service"},
	}

	for _, q := range queries {
		port, err := net.LookupPort(q.network, q.service)

		if err != nil {
			fmt.Printf("%s/%s -> error\n", q.network, q.service)
			continue
		}

		fmt.Printf("%s/%s -> %d\n", q.network, q.service, port)
	}
}
