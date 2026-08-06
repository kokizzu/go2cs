// The blank-import side-effect guard. Go's `import _ "pkg"` initializes pkg for its init side
// effects ALONE — nothing here ever names pnglike or jpeglike, and Go still guarantees both are
// fully initialized before this package's own init runs. A converted Go init becomes a .NET
// [ModuleInitializer], which fires only at first use of something in its assembly, so a blank
// import — the case that uses nothing — left the registry empty: image/gif's TestWriter read
// "image: unknown format" for a PNG because image/png's init never registered the decoder.
//
// `registry` is the "image" role and IS named here, so its assembly loads either way; that is the
// point of the split — the registrants must be reachable only through their initialization.
package main

import (
	"fmt"

	_ "BlankImportSideEffects/jpeglike"
	_ "BlankImportSideEffects/pnglike"
	"BlankImportSideEffects/registry"
)

// countAtInit records what the registry held when THIS package's own init ran. Go orders an
// imported package's initialization before the importer's, blank imports included, so it is 2.
var countAtInit int

func init() {
	countAtInit = registry.Count()
}

func main() {
	fmt.Println("count at init:", countAtInit)

	// "giflike" is the negative control: never registered, so a lookup that "succeeds" for
	// everything would not prove anything.
	for _, name := range []string{"jpeglike", "pnglike", "giflike"} {
		if describe, ok := registry.Lookup(name); ok {
			fmt.Println("found:", name, "=>", describe)
		} else {
			fmt.Println("missing:", name)
		}
	}

	fmt.Println("count:", registry.Count())
}
