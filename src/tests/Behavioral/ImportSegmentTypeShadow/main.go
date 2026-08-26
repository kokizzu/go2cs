package main

import (
	"fmt"
	"sync/atomic"
)

// A package-level TYPE whose name matches the LEADING PATH SEGMENT of an imported package must not
// occlude that package's forcing hook.
//
// Every import whose package initializes transitively gets a `[GoInit]` hook emitted at the top of
// the importing file's CLASS body:
//
//	[GoInit] internal static void initᴛᴛimportꓸsyncꓸatomic() {
//	    builtin.initPackage(typeof(sync.atomic_package));
//	}
//
// That `typeof` is the ONE place the converter writes a raw namespace-qualified path inside a class
// body — every other cross-package reference goes through a file-scoped `using` alias, and a using
// DIRECTIVE resolves at namespace scope where class members are not in play. C# resolves the leading
// identifier of a namespace-or-type-name by searching the enclosing TYPE declarations outward first,
// so the `sync` declared below (emitted as a nested `[GoType] partial struct sync` of `main_package`)
// binds ahead of the `go.sync` namespace and the hook fails to compile:
//
//	error CS0426: The type name 'atomic' does not exist in the type 'main_package.sync'
//
// Note the asymmetry this pins: `using atomic = sync.atomic_package;` a few lines ABOVE the hook
// resolves perfectly well, and every `atomic.Int32` in the body below reaches the package through
// that alias. Only the hook is exposed, and only because of where it is written.
//
// Go is entirely happy with this program: importing "sync/atomic" binds the identifier `atomic`, not
// `sync`, so the name is free for a local declaration. The same shape reached the converter from
// Go's own image_test.go, whose test-local `type image interface{…}` occluded the hooks for
// `image/color` and `image/color/palette`. The remedy is collision-gated `global::` rooting of the
// forcing target (writeImportInit / forcingTargetShadowed), which restarts lookup at the global
// namespace and so cannot be occluded by anything the class declares.
type sync struct {
	label string
	hits  int32
}

func (s *sync) bump(c *atomic.Int32) {
	s.hits = c.Add(1)
}

func main() {
	var c atomic.Int32
	s := sync{label: "shadowed"}

	s.bump(&c)
	s.bump(&c)
	s.bump(&c)

	fmt.Println(s.label, s.hits, c.Load())
}
