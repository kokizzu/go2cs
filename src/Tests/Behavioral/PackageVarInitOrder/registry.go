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
