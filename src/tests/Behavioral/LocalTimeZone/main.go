// LocalTimeZone guards the Windows local-timezone path, which reaches the syscall struct-passing
// seam: time.Local's lazy initLocal calls syscall.GetTimeZoneInformation, and Go passes the address
// of a 172-byte TIME_ZONE_INFORMATION whose two WCHAR[32] name buffers are INLINE. The converted
// struct holds those names as golib array<uint16> MANAGED references, so handing the kernel that
// address wrote 172 native bytes over a ~64-byte managed object: the process died with an
// ACCESS_VIOLATION inside slice<ushort>..ctor on the very next `z.StandardName[:]`. Every converted
// program calling Weekday / Location / Local on Windows crashed. syscall's GetTimeZoneInformation
// is now hand-owned against a blittable mirror (zsyscall_windows_impl.cs).
//
// A no-fault check is not enough — a mirror with the wrong offsets returns garbage without
// crashing. So this prints values that DEPEND on every field the copy has to get right: the zone
// abbreviation comes from the name buffers, the offset from Bias/StandardBias/DaylightBias, and
// rendering a FIXED instant in the local zone exercises the StandardDate/DaylightDate transition
// records that decide which of the two applies.
package main

import (
	"fmt"
	"time"
)

func main() {
	// A fixed instant, rendered through the local zone. Fully deterministic: same answer on every
	// run, and the parts that vary by machine (abbreviation, offset) vary IDENTICALLY for Go and
	// the converted C#, which is exactly what the output comparison checks.
	t := time.Unix(1700000000, 0).In(time.Local)

	fmt.Println(t.Format("2006-01-02 15:04:05 MST"))
	fmt.Printf("%v\n", t)
	fmt.Println(t.Weekday(), t.YearDay())

	name, offset := t.Zone()
	fmt.Println(name, offset)
	fmt.Println(len(name) > 0, offset%60 == 0)

	// The reverse direction: a wall-clock time interpreted in the local zone and converted back to
	// an absolute instant. Wrong Bias handling shifts this by whole hours.
	local := time.Date(2023, time.November, 14, 22, 13, 20, 0, time.Local)
	fmt.Println(local.UTC().Format(time.RFC3339))
	fmt.Println(local.Equal(t))

	// Summer and winter instants land on opposite sides of any DST rule the zone has, so a broken
	// StandardDate/DaylightDate copy shows up as two identical offsets (or two identical names)
	// where the platform zone actually has a transition.
	_, janOffset := time.Date(2023, time.January, 15, 12, 0, 0, 0, time.Local).Zone()
	_, julOffset := time.Date(2023, time.July, 15, 12, 0, 0, 0, time.Local).Zone()
	fmt.Println(janOffset == offset || julOffset == offset)

	// time.Local must be the location a locally-constructed time reports.
	fmt.Println(local.Location() == time.Local, time.Local.String())
}
