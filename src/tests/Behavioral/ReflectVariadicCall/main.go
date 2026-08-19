package main

import (
	"errors"
	"fmt"
	"reflect"
	"sort"
)

// reflect.Value.Call on a VARIADIC func value. Go's contract is that Call itself builds the tail
// slice from the trailing arguments, so every case below hands Call loose arguments and reads back
// what the callee saw. The managed bridge cannot reach any of it through a reflective invoke: a
// converted Go variadic lowers its tail to `params Span[T]`, Span is a ref struct, and both
// Delegate.DynamicInvoke and MethodInfo.Invoke marshal through an object array a ref struct cannot
// enter. The whole surface below therefore runs on typed dispatch, and `go run` is the oracle.

// A DECLARED variadic function: used as a value it takes golib's variadic family delegate.
func join(prefix string, parts ...int) string {
	out := prefix
	for _, p := range parts {
		out += fmt.Sprintf("/%d", p)
	}
	return out
}

// No fixed parameters at all — the family's zero-arity shape.
func sum(nums ...int) int {
	total := 0
	for _, n := range nums {
		total += n
	}
	return total
}

// A `...any` tail, which is the commonest Go variadic and the one text/template's FuncMap holds.
func describe(tag string, vals ...any) string {
	return fmt.Sprintf("%s%v", tag, vals)
}

// MULTI-return: the delegate's result is a tuple the bridge destructures positionally.
func firstNonZero(vals ...int) (int, error) {
	for _, v := range vals {
		if v != 0 {
			return v, nil
		}
	}
	return 0, errors.New("all zero")
}

// A variadic with NO result at all — the Action half of the family, whose Call returns no values.
var recorded []string

func record(label string, parts ...string) {
	line := label
	for _, p := range parts {
		line += ":" + p
	}
	recorded = append(recorded, line)
}

// Two fixed parameters ahead of the tail, so the fixed prefix is exercised past arity one.
func between(lo, hi int, vals ...int) int {
	n := 0
	for _, v := range vals {
		if v >= lo && v <= hi {
			n++
		}
	}
	return n
}

// A variadic METHOD: Value.Method binds the receiver and the result is an ordinary func value, so
// the same Call path must serve it.
type counter struct {
	base int
}

func (c counter) Total(vals ...int) int {
	t := c.base
	for _, v := range vals {
		t += v
	}
	return t
}

func callString(fn any, args ...any) string {
	in := make([]reflect.Value, len(args))
	for i, a := range args {
		in[i] = reflect.ValueOf(a)
	}
	out := reflect.ValueOf(fn).Call(in)
	parts := make([]string, len(out))
	for i, o := range out {
		parts[i] = fmt.Sprintf("%v", o.Interface())
	}
	return "[" + fmt.Sprint(parts) + "]"
}

func main() {
	// ---- the declared-function shape, with a full tail and with an empty one ----
	fmt.Println("join3    :", callString(join, "go", 1, 2, 3))
	fmt.Println("join0    :", callString(join, "go"))

	// ---- no fixed parameters ----
	fmt.Println("sum      :", callString(sum, 4, 5, 6))
	fmt.Println("sum0     :", callString(sum))

	// ---- an `...any` tail ----
	fmt.Println("describe :", callString(describe, "tag=", 1, "two", true))

	// ---- multi-return, both arms ----
	fmt.Println("firstA   :", callString(firstNonZero, 0, 0, 9))
	fmt.Println("firstB   :", callString(firstNonZero))

	// ---- no result: Call returns an empty slice and the side effect is what is observed ----
	fmt.Println("recordN  :", len(reflect.ValueOf(record).Call([]reflect.Value{
		reflect.ValueOf("a"), reflect.ValueOf("x"), reflect.ValueOf("y"),
	})))
	fmt.Println("recorded :", recorded)

	// ---- two fixed parameters ----
	fmt.Println("between  :", callString(between, 2, 4, 1, 2, 3, 4, 5))

	// ---- a variadic method value ----
	m := reflect.ValueOf(counter{base: 100}).MethodByName("Total")
	fmt.Println("method   :", callString(m.Interface(), 1, 2))

	// ---- the FuncMap shape: variadic func LITERALS in a map[string]any, which is exactly how
	//      text/template registers user functions. A literal in an `any` slot has no delegate
	//      target, so it takes C#'s NATURAL delegate type rather than the family one.
	funcs := map[string]any{
		"cat": func(sep string, parts ...string) string {
			out := ""
			for i, p := range parts {
				if i > 0 {
					out += sep
				}
				out += p
			}
			return out
		},
		"count": func(vals ...any) int { return len(vals) },
		"pair": func(a int, rest ...int) (int, int) {
			t := 0
			for _, r := range rest {
				t += r
			}
			return a, t
		},
	}
	names := make([]string, 0, len(funcs))
	for name := range funcs {
		names = append(names, name)
	}
	sort.Strings(names)
	for _, name := range names {
		fn := reflect.ValueOf(funcs[name])
		fmt.Printf("funcmap %-6s: variadic=%v numIn=%d in-last=%v\n",
			name, fn.Type().IsVariadic(), fn.Type().NumIn(), fn.Type().In(fn.Type().NumIn()-1))
	}
	fmt.Println("cat      :", callString(funcs["cat"], "-", "a", "b", "c"))
	fmt.Println("count    :", callString(funcs["count"], 1, 2, 3, 4))
	fmt.Println("pair     :", callString(funcs["pair"], 7, 1, 2))

	// ---- the arity contract: a variadic Call may take fewer arguments than NumIn, but not fewer
	//      than the fixed prefix ----
	fmt.Println("toofew   :", recoverText(func() { reflect.ValueOf(join).Call(nil) }))
	fmt.Println("ok-empty :", recoverText(func() { reflect.ValueOf(sum).Call(nil) }))
}

func recoverText(f func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprintf("%v", r)
		}
	}()
	f()
	return "<no panic>"
}
