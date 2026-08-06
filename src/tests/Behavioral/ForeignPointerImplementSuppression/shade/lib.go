// shade exists for ONE reason: it declares an interface with the same SIMPLE name as tone's.
//
// The record key collapses an interface to its class-relative tail (`tone_package.Level`) so the
// two sides of the lookup spell it alike, and the package class is what keeps that collapse honest.
// Drop it and `*tone.Tone → tone.Level` would satisfy `*tone.Tone → shade.Level` too, referencing
// the adapter that implements the WRONG interface — image's Paletted→image.Image record satisfying
// a Paletted→draw.Image cast, CS1503.
package shade

// Level is deliberately identical in shape to tone.Level, and deliberately NOT the same interface.
type Level interface {
	Tone() int
	Name() string
	Set(d int)
}
