// Package latelib exists to be loaded LATE. Its only job is to hold conversion sites the
// converter CAN record, so go2cs-gen emits a generated nominal adapter for each pair and
// registers it from this assembly's module initializer — which the CLR does not run until
// something in this module is first used.
package latelib

import "ItabLateRegistration/lib"

// Prime performs the recordable conversions. Assigning a POINTER to the interface is what makes
// the generated adapter register itself: a value-typed nominal match is satisfied by having the
// struct implement the interface directly, which leaves nothing to register at run time.
func Prime() (string, int) {
	c := lib.Circle{R: 7}
	q := lib.Square{S: 8}

	var n lib.Named = &c
	var s lib.Sized = &q

	return n.Name(), s.Size()
}
