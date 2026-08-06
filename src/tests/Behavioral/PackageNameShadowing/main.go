package main

import (
	"fmt"
	"time"

	// A blank import puts the `go.time` CHILD NAMESPACE in the assembly, which is what forces the
	// import alias to be collision-renamed (`using Δtime = time_package;`) — the condition under
	// which a qualified reference to a collision-renamed MEMBER must carry the renamed qualifier
	// too. It also exercises the cross-package //go:linkname forwarder: tzdata's init() calls
	// time.registerLoadFromEmbeddedTZData, a function `time/tzdata` imports from nowhere.
	_ "time/tzdata"
)

const layout = "2006-01-02T15:04:05.000000000Z07:00"

// describe takes a PARAMETER named after the `time` package, shadowing it inside the body — legal
// Go, and the shape time's own format_test.go uses (`func checkTime(time Time, ...)`). Every
// selector below is a METHOD CALL on that parameter, never a package reference. Note the mix: Year,
// Day and Format are plain names in the converted package, while Month, Hour, Minute, Second,
// Nanosecond and Weekday are also names `time` declares at PACKAGE scope, so the package's collision
// rename applies to the package member and must NOT reach these method selectors.
func describe(time time.Time) string {
	return fmt.Sprintf("%d-%v-%02d %02d:%02d:%02d.%09d %v",
		time.Year(), time.Month(), time.Day(),
		time.Hour(), time.Minute(), time.Second(), time.Nanosecond(), time.Weekday())
}

// qualifiedDurations reads collision-renamed package CONSTS through the (itself renamed) qualifier.
func qualifiedDurations() string {
	return fmt.Sprint(time.Nanosecond, " ", time.Second, " ", time.Minute, " ", time.Hour)
}

// qualifiedLocations reads collision-renamed package VARS the same way.
func qualifiedLocations() string {
	return fmt.Sprint(time.UTC, " ", time.Local != nil)
}

// foldedConstant exercises a compile-time constant expression of a NAMED type whose value falls
// outside int32, in both positions the fold has to survive: an inferred local, and a parenthesized
// method-call receiver. It lives outside main because main shadows the package name partway through.
func foldedConstant() (time.Duration, float64, int) {
	d := 8 * time.Hour
	return d, d.Seconds(), int((8 * time.Hour).Seconds())
}

func main() {
	stamp := time.Date(2009, time.February, 4, 21, 0, 57, 12345600, time.UTC)

	// 1. Parameter shadowing the package name, method calls throughout.
	fmt.Println("1:", describe(stamp))

	// 2. A LOCAL shadowing the package name (time's own TestFormat does exactly this), then a method
	//    call on it. The declaration and every use must agree on the local, not the import alias.
	time := stamp
	fmt.Println("2:", time.Format(layout))

	// 3. Qualified references to collision-renamed package members — consts and vars. Each is
	//    Δ-renamed in the converted package because a Time METHOD shares its name, and the qualifier
	//    is itself Δ-renamed by the tzdata import above; both halves have to move together.
	fmt.Println("3:", qualifiedDurations(), qualifiedLocations())

	// 4. A constant expression of a NAMED type whose value falls outside int32. Folding it must keep
	//    the Duration type: as a bare `long` the receiver has no Seconds method, and — silently — the
	//    inferred variable would print as a digit count instead of a duration.
	d, secs, east := foldedConstant()
	fmt.Println("4:", d, secs, east)

	// 5. Concatenation of two SLICED string literals. Each half renders as a UTF-8 span, and C#'s
	//    span concatenation is a literal-only feature, so the concat needs a real string operand.
	frac := 4
	fmt.Println("5:", "012345678"[:frac]+"000000000"[:9-frac])

	// 6. The dot-imported half (see dotimport.go): the same collision-renamed members reached as
	//    BARE identifiers, where there is no selector to carry the rename.
	fmt.Println("6:", dotImported())
}
