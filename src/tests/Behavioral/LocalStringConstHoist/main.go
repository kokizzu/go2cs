package main

import "fmt"

// Arm C (docs/phase4/DESIGN-string-literal-allocation.md §8): a FUNCTION-LOCAL string const is hoisted
// to one static field under its own name, which the local copies. Every shape below must keep Go's value.

type Kind string

type parser struct{ n int }

// A package-level initializer that reaches a function with a hoisted const: the field must be
// initialized before this runs (the relocation into the ordered static constructor, §4.4).
var greeting = makeGreeting("world")

func makeGreeting(who string) string {
	const hello = "hello, "
	return hello + who
}

func atoi(s string) string {
	const fnAtoi = "Atoi"
	if s == "" {
		return fnAtoi + ": empty"
	}
	return fnAtoi + "(" + s + ")"
}

func named() Kind {
	const k Kind = "kind-value"
	return k
}

// A const of a type declared inside the function: the field's type is the lifted local type.
func localKind() string {
	type tag string
	const t tag = "local-tag"
	return string(t)
}

func multi() string {
	const a, b = "first-a", "second-b"
	const (
		c = "group-c"
		d = "group-d"
	)
	return a + "|" + b + "|" + c + "|" + d
}

// The same const name in two functions, with different values.
func labelOne() string {
	const label = "label-one"
	return label
}

func labelTwo() string {
	const label = "label-two"
	return label
}

func shadow() string {
	const s = "outer-scope"
	inner := func() string {
		const s = "inner-scope"
		return s
	}
	return s + "/" + inner()
}

func closure() func() string {
	const captured = "captured-const"
	return func() string { return captured }
}

func empty() int {
	const e = ""
	return len(e)
}

func concatenated() string {
	const whole = "con" + "cat" + "enated"
	return whole
}

func classify(x string) string {
	const yes, no = "matched", "unmatched"
	switch x {
	case yes:
		return "case:" + yes
	}
	return no
}

func bytesConst() int {
	const raw = "\xff\x00\x7f"
	return len(raw) + int(raw[0])
}

func genericLabel[T any](v T) string {
	const prefix = "generic:"
	return fmt.Sprint(prefix, v)
}

func (p *parser) name() string {
	const method = "parser-method"
	p.n++
	return fmt.Sprintf("%s#%d", method, p.n)
}

func loop() int {
	total := 0
	for i := 0; i < 3; i++ {
		const step = "step"
		total += len(step)
	}
	return total
}

var initValue string

func init() {
	const fromInit = "init-const"
	initValue = fromInit
}

func main() {
	fmt.Println(greeting)
	fmt.Println(atoi(""), atoi("42"))
	fmt.Println(named())
	fmt.Println(localKind())
	fmt.Println(multi())
	fmt.Println(labelOne(), labelTwo())
	fmt.Println(shadow())
	fmt.Println(closure()())
	fmt.Println(empty())
	fmt.Println(concatenated())
	fmt.Println(classify("matched"), classify("other"))
	fmt.Println(bytesConst())
	fmt.Println(genericLabel(7), genericLabel("x"))
	p := &parser{}
	fmt.Println(p.name(), p.name())
	fmt.Println(loop())
	fmt.Println(initValue)
}
