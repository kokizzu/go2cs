// PointerOutParameter exercises the syscall wrappers that take a Go `**T`
// OUT-PARAMETER: the kernel writes a raw address into a slot the caller owns.
//
// Under go2cs `&p` for a `*T` variable is a ж<ж<T>> — a managed box whose slot
// holds an object reference, not eight bytes of address — so the boundary has to
// supply a NATIVE cell and publish what the kernel wrote back into that slot.
// Every assertion below is a VALUE, never merely the absence of a fault: a
// wrapper that told the kernel "no output wanted" answers success and leaves the
// caller's pointer nil, which is invisible to a liveness check.
package main

import (
	"fmt"
	"syscall"
	"unsafe"
)

// The well-known SIDs, chosen because they are identical on every Windows host
// and require no privileges: LocalSystem, BUILTIN\Administrators, Everyone,
// and the null SID.
var wellKnown = []string{
	"S-1-5-18",
	"S-1-5-32-544",
	"S-1-1-0",
	"S-1-0-0",
}

func main() {
	// ---- ConvertStringSidToSid: a `**SID` out-parameter ----
	// ---- ConvertSidToStringSid: a `**uint16` out-parameter ----
	// Together a round trip, so a wrapper that hands back the wrong address is
	// caught by the VALUE and not only by a crash.
	for _, s := range wellKnown {
		sid, err := syscall.StringToSid(s)
		if err != nil {
			fmt.Println("StringToSid error:", err)
			continue
		}
		if sid == nil {
			fmt.Println("StringToSid returned a nil SID for", s)
			continue
		}

		back, err := sid.String()
		if err != nil {
			fmt.Println("SID.String error:", err)
			continue
		}

		fmt.Println(s, "->", back, "roundtrip:", back == s, "len>0:", sid.Len() > 0)
	}

	// A malformed SID must still FAIL, so the remedy cannot be "always report
	// success": an out-parameter left untouched on failure is Go's contract too.
	if _, err := syscall.StringToSid("not-a-sid"); err == nil {
		fmt.Println("malformed SID rejected: false")
	} else {
		fmt.Println("malformed SID rejected: true")
	}

	// The same SID converted twice must agree with itself, which is the property
	// a stale or transient address would break.
	a, _ := syscall.StringToSid("S-1-5-32-545")
	b, _ := syscall.StringToSid("S-1-5-32-545")
	as, _ := a.String()
	bs, _ := b.String()
	fmt.Println("stable:", as == bs, as == "S-1-5-32-545")

	// ---- NetGetJoinInformation: a `**uint16` out-parameter over netapi32 ----
	// A THIRD DLL and a different free routine (NetApiBufferFree), so it proves
	// the remedy belongs to the class rather than to one advapi32 accident. The
	// name varies by host, so the assertions are shape ones — non-empty, and a
	// join status inside the documented range.
	var name *uint16
	var bufType uint32
	if err := syscall.NetGetJoinInformation(nil, &name, &bufType); err != nil {
		fmt.Println("NetGetJoinInformation error:", err)
	} else if name == nil {
		fmt.Println("NetGetJoinInformation left its out-parameter nil")
	} else {
		// READ THROUGH the published pointer rather than only testing it for nil:
		// walking the kernel's UTF-16 buffer to its NUL is what proves the address
		// is the one Windows wrote, since a wrong address reads garbage or faults
		// here instead of quietly answering. The walk is bounded, and the
		// assertions are shape ones because the join name varies by host.
		n := 0
		first := uint16(0)
		for n < 256 {
			c := *(*uint16)(unsafe.Add(unsafe.Pointer(name), 2*n))
			if c == 0 {
				break
			}
			if n == 0 {
				first = c
			}
			n++
		}
		fmt.Println("join name length in range:", n > 0 && n < 256,
			"first rune printable:", first >= 0x20 && first < 0x7f,
			"status in range:", bufType <= 4)
		syscall.NetApiBufferFree((*byte)(unsafe.Pointer(name)))
	}
}
