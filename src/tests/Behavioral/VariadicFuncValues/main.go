package main

import "fmt"

// A VARIADIC function TYPE used as a VALUE — the go/types `comparable(…, reportf)` shape. The
// three lowerings must agree: the delegate type a parameter/var renders as (Actionꓸꓸꓸ/Funcꓸꓸꓸ,
// `params Span<T>` tail), the form a variadic func literal and a named function (method group)
// convert to, and calls through the value passing loose Go-style arguments, an empty tail, or a
// spread slice.

// gather is a NAMED variadic function passed as a value (method-group conversion).
func gather(prefix string, vals ...int) string {
	total := 0

	for _, v := range vals {
		total += v
	}

	return fmt.Sprintf("%s:%d(%d)", prefix, total, len(vals))
}

// apply calls a variadic func VALUE with loose args, an empty tail, and a spread.
func apply(f func(prefix string, vals ...int) string) {
	fmt.Println(f("loose", 1, 2, 3))
	fmt.Println(f("empty"))

	nums := []int{4, 5}
	fmt.Println(f("spread", nums...))
}

// report exercises the void printf-callback shape (go/types' reportf) with any elements.
func report(emit func(format string, args ...any)) {
	emit("%s=%d", "x", 7)
	emit("bare")
}

// logger owns two VARIADIC METHODS, so a `:=` can bind one as a method VALUE and a later
// assignment can swap in the other.
type logger struct{ tag string }

func (l *logger) errorf(format string, args ...any) {
	fmt.Printf(l.tag+"!"+format+"\n", args...)
}

func (l *logger) logf(format string, args ...any) {
	fmt.Printf(l.tag+"~"+format+"\n", args...)
}

// swapEmitter is slices' TestGrow/TestConcat shape verbatim: `errorf := t.Errorf`, conditionally
// `errorf = t.Logf`, then loose Go-style calls through the value. Both halves of the emission are
// load-bearing. The method value forwards through a lambda, whose variadic tail must carry the
// `params ꓸꓸꓸT` convention — rendered as the plain `slice<T>` the signature stores, the value was
// frozen at fixed arity and every loose call was CS1593/CS1503. And the local cannot be left to
// `var`: an inferred natural delegate type binds only the FIRST lambda, so the reassignment below
// has no reason to share it. golib's variadic delegate family (`Actionꓸꓸꓸ<@string, any>`) is what
// both lambdas convert to and what carries the loose-argument call form.
func swapEmitter(l *logger, swap bool) {
	emit := l.errorf

	if swap {
		emit = l.logf
	}

	emit("one %d", 1)
	emit("two %d %d", 2, 3)
	emit("none")

	rest := []any{4, 5}
	emit("spread %d %d", rest...)
}

func main() {
	// A named func satisfies the variadic func-typed parameter.
	apply(gather)

	// A variadic func literal satisfies it too.
	apply(func(prefix string, vals ...int) string {
		return fmt.Sprintf("%s|%d", prefix, len(vals))
	})

	// A variadic func-typed VAR: declared (nil), nil-compared, assigned the named func, called.
	var f func(prefix string, vals ...int) string

	if f == nil {
		fmt.Println("nil func value")
	}

	f = gather
	fmt.Println(f("var", 10))

	report(func(format string, args ...any) {
		fmt.Printf(format+"\n", args...)
	})

	// A variadic METHOD VALUE bound by `:=`, reassigned, and called with loose arguments.
	lg := &logger{tag: "L"}
	swapEmitter(lg, false)
	swapEmitter(lg, true)
}
