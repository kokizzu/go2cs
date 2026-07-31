// Package pnglike plays the "image/png" role: it exists to be BLANK-imported. Nothing outside it
// ever names one of its identifiers, so the only reason its converted assembly must be loaded is
// this init — exactly what Go guarantees and what .NET's first-use module-initializer rule does not.
package pnglike

import "BlankImportSideEffects/registry"

func init() {
	registry.Register("pnglike", "pnglike decoder")
}
