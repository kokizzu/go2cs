// The LATE-ASSERT shape that leaves io/fs stuck at 16/18: a VALUE-typed concrete
// type (os.dirFS is `type dirFS string`) lives in a package converted AFTER the
// interface's package, so no compile-time (type, interface) record can exist and
// the assert must resolve at run time. The interfaces are three-deep embedded, so
// the shell also has to carry every transitively-inherited method; one of them is
// unexported, so its shell must be constructible without public-constructor binding.
package main

import (
	"fmt"

	"NamedInterfaceLateAssert/latelib"
)

// dirLike mirrors os.dirFS exactly: a defined string with value-receiver methods.
type dirLike string

func (d dirLike) Name() string { return string(d) }

func (d dirLike) Kind() string { return "dir" }

func (d dirLike) Detail(prefix string) string { return prefix + ":" + string(d) }

func (d dirLike) Whisper() string { return "shh " + string(d) }

// partial satisfies Mid (and therefore Base) but never Top.
type partial struct{ n string }

func (p partial) Name() string { return p.n }

func (p partial) Kind() string { return "partial" }

func report(label string, s string, ok bool) {
	fmt.Println(label, ok, s)
}

func main() {
	var d dirLike = "root"

	s, ok := latelib.TryTop(d)
	report("Top-dirLike    ", s, ok)

	s, ok = latelib.TryMid(d)
	report("Mid-dirLike    ", s, ok)

	s, ok = latelib.TryBase(d)
	report("Base-dirLike   ", s, ok)

	s, ok = latelib.TryQuiet(d)
	report("quiet-dirLike  ", s, ok)

	// A pointer to the same value type: *dirLike's method set is a superset, so
	// every assert still succeeds — through the pointer tier this time.
	s, ok = latelib.TryTop(&d)
	report("Top-ptrDirLike ", s, ok)

	s, ok = latelib.TryQuiet(&d)
	report("quiet-ptrDir   ", s, ok)

	// partial is Mid but NOT Top — the derived interface's extra method must be
	// demanded, not assumed.
	s, ok = latelib.TryTop(partial{n: "p"})
	report("Top-partial    ", s, ok)

	s, ok = latelib.TryMid(partial{n: "p"})
	report("Mid-partial    ", s, ok)

	s, ok = latelib.TryQuiet(partial{n: "p"})
	report("quiet-partial  ", s, ok)
}
