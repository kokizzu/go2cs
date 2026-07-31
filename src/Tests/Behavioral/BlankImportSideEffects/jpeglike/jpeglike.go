// Package jpeglike is a SECOND blank-imported registrant, so the guard covers more than one blank
// import in a single file: each needs its own generated hook, and two hooks in one class must not
// collide (a shared generated name would be CS0111) nor let the second silently go unforced.
package jpeglike

import "BlankImportSideEffects/registry"

func init() {
	registry.Register("jpeglike", "jpeglike decoder")
}
