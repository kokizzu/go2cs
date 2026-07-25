// inner plays bytes in the embedded-name collision guard: it supplies a type whose UNQUALIFIED
// name matches the name of the struct that embeds it (io_test.go's `type Buffer struct {
// bytes.Buffer }`). The collision is only expressible across packages — one package cannot declare
// two types with the same name.
package inner

import "fmt"

// Buffer is embedded by a struct of the same name in the main package.
type Buffer struct {
	Data string
	N    int
}

func NewBuffer(data string, n int) Buffer {
	return Buffer{Data: data, N: n}
}

// Describe is a VALUE-receiver method, promoted through the embed.
func (b Buffer) Describe() string {
	return fmt.Sprintf("inner.Buffer(%s,%d)", b.Data, b.N)
}

// Bump is a POINTER-receiver method, promoted through the embed.
func (b *Buffer) Bump() {
	b.N++
}

// Append mutates both promoted fields through the pointer receiver.
func (b *Buffer) Append(s string) {
	b.Data += s
	b.N += len(s)
}
