package main

import "fmt"

// escaper is a NAMED variadic func — the html/template funcMap shape, whose values are
// method groups of `func(...any) string` stored in a `map[string]any`.
func escaper(args ...any) string {
	return fmt.Sprint(args...)
}

func main() {
	// Declared through an explicitly-typed func var: the value boxed into `any` then
	// carries the golib variadic delegate type the assertion target lowers to.
	var fn func(string, ...any) = func(format string, args ...any) {
		fmt.Printf(format+"\n", args...)
	}
	var logf any = fn

	// Positive assert: a VARIADIC func type as the assertion target must lower to the
	// params-Span delegate form (net/http transport.go logf shape).
	if fn, ok := logf.(func(string, ...any)); ok {
		fn("value=%v flag=%v", 42, true)
	} else {
		fmt.Println("no match")
	}

	// Negative assert: a non-func value must not match the variadic func target.
	var notFn any = "plain"
	if _, ok := notFn.(func(string, ...any)); ok {
		fmt.Println("unexpected match")
	} else {
		fmt.Println("no match for string")
	}

	// A NON-variadic anonymous func target renders identically on both paths.
	var plain any = func(s string) string { return s + "!" }
	if fn, ok := plain.(func(string) string); ok {
		fmt.Println(fn("ok"))
	}

	// A variadic func LITERAL assigned DIRECTLY to `any` — no typed var in between, so
	// nothing gives C# the destination type and its NATURAL function type for a `params`
	// lambda is a SYNTHESIZED anonymous delegate. Without the empty-interface boxing cast
	// the assert below cannot match: `interface {} is <>f__AnonymousDelegate0, not
	// go.Funcꓸꓸꓸ<object, @string>`.
	var direct any = func(args ...any) string { return fmt.Sprint(args...) }
	if f, ok := direct.(func(...any) string); ok {
		fmt.Println("direct:", f(1, 2))
	} else {
		fmt.Println("direct: no match")
	}

	// The corpus shape this guard was extended for: a variadic METHOD GROUP as a map
	// element of `map[string]any`, read back through Go's own type assertion — exactly
	// html/template's `funcMap[n].(func(...any) string)` (TestRedundantFuncs).
	funcMap := map[string]any{"esc": escaper}
	if f, ok := funcMap["esc"].(func(...any) string); ok {
		fmt.Println("mapped:", f("a", "b"))
	} else {
		fmt.Println("mapped: no match")
	}

	// The same method group through a plain assignment and through a slice element: the
	// boundary set is per-slot, so each slot is asserted rather than assumed.
	var assigned any = escaper
	if f, ok := assigned.(func(...any) string); ok {
		fmt.Println("assigned:", f("x"))
	} else {
		fmt.Println("assigned: no match")
	}

	slots := []any{escaper}
	if f, ok := slots[0].(func(...any) string); ok {
		fmt.Println("element:", f("y"))
	} else {
		fmt.Println("element: no match")
	}

	// CONTROL: a NON-variadic func literal direct to `any` needs no cast — C#'s natural
	// function type for it is already `Func<…>`, which is go2cs's own lowering — and must
	// keep matching, so the cast cannot be applied blindly to every func value.
	var directPlain any = func(s string) string { return s + "?" }
	if f, ok := directPlain.(func(string) string); ok {
		fmt.Println("direct plain:", f("z"))
	} else {
		fmt.Println("direct plain: no match")
	}
}
