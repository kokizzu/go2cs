package main

import (
	"fmt"
	// A DOT import makes every exported member of `time` a BARE identifier in this file — the shape
	// time's own external test files use (`. "time"`). There is no selector for the qualified-name
	// resolver to rewrite, so a member the package collision-renames (Second, Minute, Hour,
	// Nanosecond, UTC, Local — each shares its name with a Time METHOD) has to be resolved here.
	. "time"
)

// dotImported reaches collision-renamed CONSTS, VARS and TYPES with no qualifier at all. The types
// (Month, Weekday, Duration, Location) are the control: those already resolve through the imported
// type aliases, so they must stay exactly as they were.
func dotImported() string {
	var m Month = March
	var w Weekday = Thursday
	var d Duration = Nanosecond + Second + Minute + Hour
	var loc *Location = UTC

	stamp := Date(2011, m, 12, 3, 4, 5, 600000000, loc)

	return fmt.Sprint(
		m, " ", w, " ", d, " ", loc, " ", Local != nil, " ",
		stamp.Format(RFC3339Nano), " ",
		stamp.Hour(), stamp.Minute(), stamp.Second(), stamp.Nanosecond(),
	)
}
