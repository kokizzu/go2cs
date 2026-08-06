// Package registry plays the "image" role of the blank-import side-effect shape: a decoder table
// that OTHER packages fill from their init functions and that the importer reads back. Nothing here
// knows the decoders exist — the only thing that puts them in the table is the fact that Go
// initialized their packages.
package registry

var decoders = map[string]string{}

// Register records a decoder. Called only from another package's init.
func Register(name, describe string) {
	decoders[name] = describe
}

// Lookup reports the decoder registered under name, if any.
func Lookup(name string) (string, bool) {
	describe, ok := decoders[name]
	return describe, ok
}

// Count reports how many decoders have registered so far.
func Count() int {
	return len(decoders)
}
