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

// Untyped constants, declared in the file the compiler sees LAST, that entries.go's
// package-level vars consume. C# cannot say `const` for these (their emitted type is a
// [GoType] struct), so they must not be order-sensitive fields.
const chunkBits = 4

const numChunks = 1 << chunkBits

const tableSize = 37

type holder struct {
	chunks [numChunks]uint32
}

type table struct {
	codes []byte
}

func newTable(n int) *table {
	return &table{codes: make([]byte, n)}
}

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
