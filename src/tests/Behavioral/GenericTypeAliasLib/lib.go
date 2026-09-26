// Package GenericTypeAliasLib declares Go 1.24 generic type aliases for GenericTypeAlias to consume.
package GenericTypeAliasLib

// Box is a generic defined type with a value and a pointer method.
type Box[T any] struct{ V T }

func (b Box[T]) Get() T   { return b.V }
func (b *Box[T]) Set(v T) { b.V = v }

func NewBox[T any](v T) Box[T] { return Box[T]{V: v} }

// Generic aliases of a generic type, a map, a slice, a func and a pointer.
type Alias[T any] = Box[T]
type Set[T comparable] = map[T]struct{}
type List[T any] = []T
type Mapper[T any] = func(T) T
type Ptr[T any] = *Box[T]

// A generic alias with a union constraint.
type Number interface{ ~int | ~float64 }
type NumBox[T Number] = Box[T]

// PLAIN aliases keep their names: of a basic type, and of an instantiated generic.
type Word = string
type IntBox = Box[int]

func Sum[T Number](xs List[T]) T {
	var s T
	for _, x := range xs {
		s += x
	}
	return s
}

func Twice[T any](m Mapper[T], v T) T { return m(m(v)) }

func Label(w Word) Word { return "<" + w + ">" }

func Unbox(b IntBox) int { return b.Get() }
