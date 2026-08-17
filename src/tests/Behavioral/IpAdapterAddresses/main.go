// IpAdapterAddresses guards net's Windows interface enumeration -- the IP_ADAPTER_ADDRESSES chain
// that net.adapterAddresses asks GetAdaptersAddresses to fill and that FOUR consumers then read:
// interfaceTable (net.Interfaces), interfaceAddrTable (net.InterfaceAddrs / Interface.Addrs),
// interfaceMulticastAddrTable (Interface.MulticastAddrs) and dnsReadConfig -- getSystemDNSConfig's
// only source of DNS servers on Windows, and therefore the reason no converted program could resolve
// a host name at all until this landed.
//
// Go fills a []byte and then walks it as a linked record. The converted IpAdapterAddresses cannot be
// walked that way: it carries nine `ж<T>` fields, an `array<byte>` PhysicalAddress and an
// `array<uint32>` ZoneIndices where the native record has raw pointers and inline storage, so golib
// correctly refuses to alias the byte run as that struct and the reinterpret falls back to a
// native-address box -- after which the loop's OWN nil test fabricates a managed reference out of
// adapter bytes and the process dies (ACCESS_VIOLATION in ж<IpAdapterAddresses>.op_Equality, first
// measured from crypto/tls's TestVerifyHostname). adapterAddresses is now hand-owned against
// blittable mirrors and transcribes the whole chain -- every record, all six of its nested lists,
// and every sockaddr -- into managed boxes (net/windows/interface_windows_impl.cs).
//
// A no-fault check proves nothing here: a mirror with the wrong offsets returns garbage WITHOUT
// crashing, which is how this class fails most often. So every line below is a value the boundary
// copy has to get right. The host's own adapter set is not printed -- it varies by machine and can
// change between the Go run and the C# one -- so the output is derived facts BOTH sides compute over
// the same live host. Some are invariants that must read true or the transcription is broken; the
// rest are host facts printed as booleans, where AGREEMENT is the test, whatever the host answers.
//
// What each line reaches, by native offset into IP_ADAPTER_ADDRESSES_LH:
//
//	IfIndex            offset   4  -- indexes positive and unique
//	FriendlyName       offset  72  -- a raw PWCHAR, transcribed into managed storage; names non-empty,
//	                                  printable, unique, and round-tripping through InterfaceByName
//	PhysicalAddress    offset  80  -- inline [8]byte; every address within MAX_ADAPTER_ADDRESS_LENGTH
//	PhysicalAddressLen offset  88  -- loopback has none; some adapter has the six bytes of a MAC
//	Mtu                offset  96  -- loopback's is exactly 0xffffffff, which net reports as -1
//	IfType             offset 100  -- IF_TYPE_SOFTWARE_LOOPBACK is what makes FlagLoopback appear
//	OperStatus         offset 104  -- the loopback pseudo-interface is always up
//	FirstUnicastAddress   offset 24 -- a chain, each entry carrying a SOCKET_ADDRESS at its own
//	                                  offset 16 and OnLinkPrefixLength far out at offset 56
//	FirstMulticastAddress offset 40 -- the same shape, a different list
//
// The two exact-value pins are the loopback addresses. 127.0.0.1/8 and ::1/128 are fixed on every
// Windows host, and each is an END-TO-END proof of one sockaddr flavor: the family word, the address
// octets read at the native sockaddr's own offsets, and the prefix length read 56 bytes into the
// unicast entry -- past the SOCKET_ADDRESS the managed layout collapses to a reference. A byte-order
// or offset slip prints a different network rather than failing quietly.
//
// NOT covered, stated rather than implied: the anycast, prefix and WINS-server chains are
// transcribed but this host reports none (net.InterfaceAddrs returned zero *net.IPAddr entries), and
// the DNS-server chain has no exported reach at all -- dnsReadConfig is unexported and its answer
// needs a network to observe. All four share the one producer and the one transcription loop shape,
// so they are guarded transitively; the DNS payoff is measured through the test pipeline instead.
package main

import (
	"fmt"
	"net"
)

func main() {
	fmt.Println("-- net.Interfaces --")

	ifs, err := net.Interfaces()
	fmt.Println("error:", err)

	if err != nil {
		return
	}

	fmt.Println("interfaces reported:", len(ifs) > 0)

	indexesPositive := true
	indexesUnique := true
	namesNonEmpty := true
	namesPrintable := true
	namesUnique := true
	mtusPlausible := true
	hardwareAddrsWithinMax := true
	sixByteHardwareAddr := false
	loopbacks := 0

	seenIndex := map[int]bool{}
	seenName := map[string]bool{}

	var loopback net.Interface

	for _, ifi := range ifs {
		if ifi.Index <= 0 {
			indexesPositive = false
		}
		if seenIndex[ifi.Index] {
			indexesUnique = false
		}
		seenIndex[ifi.Index] = true

		if ifi.Name == "" {
			namesNonEmpty = false
		}
		if !printable(ifi.Name) {
			namesPrintable = false
		}
		if seenName[ifi.Name] {
			namesUnique = false
		}
		seenName[ifi.Name] = true

		// -1 is net's reading of Mtu == 0xffffffff; anything else must be a real link MTU.
		if ifi.MTU != -1 && (ifi.MTU <= 0 || ifi.MTU > 65536) {
			mtusPlausible = false
		}

		// MAX_ADAPTER_ADDRESS_LENGTH is 8; PhysicalAddressLength indexes into an inline [8]byte, so a
		// longer answer means the length and the buffer were read at different strides.
		if len(ifi.HardwareAddr) > 8 {
			hardwareAddrsWithinMax = false
		}
		if len(ifi.HardwareAddr) == 6 {
			sixByteHardwareAddr = true
		}

		if ifi.Flags&net.FlagLoopback != 0 {
			if loopbacks == 0 {
				loopback = ifi
			}
			loopbacks++
		}
	}

	fmt.Println("indexes positive:", indexesPositive)
	fmt.Println("indexes unique:", indexesUnique)
	fmt.Println("names non-empty:", namesNonEmpty)
	fmt.Println("names printable:", namesPrintable)
	fmt.Println("names unique:", namesUnique)
	fmt.Println("MTUs plausible:", mtusPlausible)
	fmt.Println("hardware addresses within MAX_ADAPTER_ADDRESS_LENGTH:", hardwareAddrsWithinMax)
	fmt.Println("some adapter reports a six-byte hardware address:", sixByteHardwareAddr)

	fmt.Println("-- the loopback pseudo-interface --")
	fmt.Println("present:", loopbacks > 0)
	fmt.Println("index positive:", loopback.Index > 0)
	fmt.Println("name non-empty:", loopback.Name != "")
	fmt.Println("MTU is -1:", loopback.MTU == -1)
	fmt.Println("no hardware address:", len(loopback.HardwareAddr) == 0)
	fmt.Println("up:", loopback.Flags&net.FlagUp != 0)
	fmt.Println("running:", loopback.Flags&net.FlagRunning != 0)
	fmt.Println("multicast:", loopback.Flags&net.FlagMulticast != 0)
	fmt.Println("not broadcast:", loopback.Flags&net.FlagBroadcast == 0)
	fmt.Println("not point-to-point:", loopback.Flags&net.FlagPointToPoint == 0)

	fmt.Println("-- lookup round trips --")
	// interfaceTable(ifindex) -- the FILTERED walk, including the early break the full enumeration
	// never takes. Both directions must name the record the enumeration named.
	byIndexAgrees := true
	byNameAgrees := true

	for _, ifi := range ifs {
		found, err := net.InterfaceByIndex(ifi.Index)
		if err != nil || found == nil || found.Index != ifi.Index || found.Name != ifi.Name {
			byIndexAgrees = false
		}

		found, err = net.InterfaceByName(ifi.Name)
		if err != nil || found == nil || found.Index != ifi.Index || found.Name != ifi.Name {
			byNameAgrees = false
		}
	}

	fmt.Println("InterfaceByIndex agrees:", byIndexAgrees)
	fmt.Println("InterfaceByName agrees:", byNameAgrees)

	_, err = net.InterfaceByName("go2cs-no-such-adapter")
	fmt.Println("unknown name is an error:", err != nil)

	fmt.Println("-- net.InterfaceAddrs --")

	addrs, err := net.InterfaceAddrs()
	fmt.Println("error:", err)

	if err != nil {
		return
	}

	fmt.Println("addresses reported:", len(addrs) > 0)

	ipsValid := true
	masksValid := true
	anycastIPsValid := true
	loopback4 := false
	loopback6 := false
	nonLoopback := false

	for _, addr := range addrs {
		switch a := addr.(type) {
		case *net.IPNet:
			// The unicast chain. A fabricated sockaddr fails the length check; a mis-read
			// OnLinkPrefixLength makes CIDRMask answer nil, which fails the mask check.
			if len(a.IP) != net.IPv4len && len(a.IP) != net.IPv6len {
				ipsValid = false
				continue
			}
			ones, bits := a.Mask.Size()
			if bits == 0 || ones > bits {
				masksValid = false
				continue
			}
			if a.IP.To4() != nil && bits != 8*net.IPv4len {
				masksValid = false
			}
			if a.IP.To4() == nil && bits != 8*net.IPv6len {
				masksValid = false
			}
			if a.IP.IsLoopback() {
				if a.IP.To4() != nil && ones == 8 {
					loopback4 = true
				}
				if a.IP.To4() == nil && ones == 128 {
					loopback6 = true
				}
			} else {
				nonLoopback = true
			}
		case *net.IPAddr:
			// The anycast chain -- transcribed, but this host reports none, so these lines record
			// that the arm exists rather than that it was exercised.
			if len(a.IP) != net.IPv4len && len(a.IP) != net.IPv6len {
				anycastIPsValid = false
			}
		default:
			ipsValid = false
		}
	}

	fmt.Println("unicast IPs are 4 or 16 bytes:", ipsValid)
	fmt.Println("unicast masks agree with the address family:", masksValid)
	fmt.Println("anycast IPs are 4 or 16 bytes:", anycastIPsValid)
	fmt.Println("127.0.0.1/8 present:", loopback4)
	fmt.Println("::1/128 present:", loopback6)
	fmt.Println("some non-loopback address present:", nonLoopback)

	fmt.Println("-- per-interface addresses --")

	perInterface := 0
	perInterfaceErr := false
	multicast := 0
	multicastErr := false
	multicastIsMulticast := true
	multicastIPsValid := true

	for _, ifi := range ifs {
		ua, err := ifi.Addrs()
		if err != nil {
			perInterfaceErr = true
		}
		perInterface += len(ua)

		ma, err := ifi.MulticastAddrs()
		if err != nil {
			multicastErr = true
		}
		multicast += len(ma)

		for _, addr := range ma {
			a, ok := addr.(*net.IPAddr)
			if !ok {
				multicastIPsValid = false
				continue
			}
			if len(a.IP) != net.IPv4len && len(a.IP) != net.IPv6len {
				multicastIPsValid = false
				continue
			}
			// 224.0.0.0/4 for IPv4, ff00::/8 for IPv6 -- garbage sockaddr bytes land outside both
			// far more often than inside.
			if !a.IP.IsMulticast() {
				multicastIsMulticast = false
			}
		}
	}

	fmt.Println("Addrs errors:", perInterfaceErr)
	fmt.Println("MulticastAddrs errors:", multicastErr)
	fmt.Println("per-interface addresses sum to InterfaceAddrs:", perInterface == len(addrs))
	fmt.Println("multicast addresses reported:", multicast > 0)
	fmt.Println("multicast IPs are 4 or 16 bytes:", multicastIPsValid)
	fmt.Println("multicast IPs are multicast:", multicastIsMulticast)
}

// printable reports whether every rune of a transcribed FriendlyName is a printable character. A
// name read at the wrong offset is empty, truncated at a stray NUL, or full of control runes.
func printable(s string) bool {
	if s == "" {
		return false
	}

	for _, r := range s {
		if r < 0x20 || r == 0x7f {
			return false
		}
	}

	return true
}
