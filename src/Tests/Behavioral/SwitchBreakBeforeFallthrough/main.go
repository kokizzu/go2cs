package main

import "fmt"

const (
	isoZone = iota
	numZone
	loopZone
	nameZone
)

// zone mirrors the shape time.Parse uses for RFC3339's "Z07:00" layout element: the ISO arm
// consumes a literal "Z" and leaves the switch with `break`, and when it does not, `fallthrough`
// hands the same text to the numeric-offset arm. A Go `break` exits the SWITCH, so it must skip the
// fallthrough entirely — raising the fallthrough flag outside the wrapper that gives the break a
// target makes every break fall through as well, which is how time.Parse lost the "Z" arm.
func zone(kind int, s string) string {
	out := "unset"

	switch kind {
	case isoZone:
		if len(s) > 0 && s[0] == 'Z' {
			out = "utc(" + s[1:] + ")"
			break
		}
		fallthrough
	case numZone:
		out = "offset(" + s + ")"
	case loopZone:
		// This break belongs to the loop, not the switch, so the case still falls through — the
		// two must be told apart, not lumped together as "the body contains a break".
		for i := 0; i < 3; i++ {
			if i == 1 {
				break
			}
			s += "!"
		}
		fallthrough
	case nameZone:
		// A break with no fallthrough behind it: the unchanged path.
		if s == "" {
			out = "anonymous"
			break
		}
		out = "named(" + s + ")"
	default:
		out = "other(" + s + ")"
	}

	return out
}

func main() {
	cases := []struct {
		kind int
		s    string
	}{
		{isoZone, "Z07:00"},
		{isoZone, "Z"},
		{isoZone, "+08:00"},
		{numZone, "-05:00"},
		{loopZone, "x"},
		{nameZone, ""},
		{nameZone, "MST"},
		{9, "?"},
	}

	for _, c := range cases {
		fmt.Printf("%d %q -> %s\n", c.kind, c.s, zone(c.kind, c.s))
	}
}
