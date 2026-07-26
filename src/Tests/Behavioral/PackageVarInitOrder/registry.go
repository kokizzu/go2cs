package main

type reg struct {
	entries []string
	count   int
}

func newReg() *reg {
	return &reg{}
}

func (r *reg) add(name string) string {
	r.entries = append(r.entries, name)
	r.count++
	return name + "-added"
}

var registry = newReg()

var names = []string{"stdin", "stdout"}

// kind is a NAMED integer type, so its constants have no C# `const` form (a [GoType] wrapper
// struct cannot be declared const, CS0283) and are emitted as `static readonly` FIELDS.
type kind uint8

const (
	kindNone kind = iota
	kindFile
	kindPipe
)

// A named-STRING const is the same shape (@string is a struct).
type label string

const labelPipe label = "pipe"
