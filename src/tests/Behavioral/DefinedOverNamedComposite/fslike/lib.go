// fslike plays testing/fstest in the defined-over-named-composite guard: it owns the NAMED
// composite types (a map, a slice and an array) that the parent package then defines its own
// types directly over. Go resolves every one of those underlyings past the name, which is
// exactly what makes the shared-underlying conversion hop misfire — see main.go.
package fslike

// MapFS plays fstest.MapFS: a named MAP type with a method.
type MapFS map[string]int

func (m MapFS) Get(k string) int { return m[k] }

func (m MapFS) Size() int { return len(m) }

// List is the named-SLICE arm of the same switch.
type List []int

func (l List) Sum() int {
	t := 0
	for _, v := range l {
		t += v
	}
	return t
}

// Buf is the named-ARRAY arm.
type Buf [2]int

func (b Buf) First() int { return b[0] }
