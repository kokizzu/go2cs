package main

import (
	"fmt"

	lib "GenericTypeAliasLib"
)

type Pair[K comparable, V any] struct {
	Key K
	Val V
}

// Local generic aliases: of a generic type, partially instantiated, an alias of an alias, and of a map.
type P[K comparable, V any] = Pair[K, V]
type StrPair[V any] = Pair[string, V]
type SP[V any] = StrPair[V]
type IntMap[V any] = map[int]V

// A local PLAIN alias keeps its name.
type Pairs = []Pair[string, int]

func swap[T comparable](p P[T, T]) P[T, T] { return P[T, T]{Key: p.Val, Val: p.Key} }

// A generic alias inside a generic function: signature, local, composite literal.
func wrap[T any](v T) lib.Alias[T] {
	var a lib.Alias[T] = lib.NewBox(v)
	b := lib.Alias[T]{V: a.Get()}
	return b
}

func main() {
	// local aliases: declaration, composite literal, parameter, return, partial instantiation
	var p P[string, int] = P[string, int]{Key: "a", Val: 1}
	sp := StrPair[bool]{Key: "b", Val: true}
	var sp2 SP[bool] = sp
	m := IntMap[string]{1: "one"}
	fmt.Println("local:", p, sp, sp2, m[1], swap(P[int, int]{Key: 1, Val: 2}))

	// identity: an alias IS its target
	var pair Pair[string, int] = p
	p = pair
	var pi any = p
	_, isPair := pi.(Pair[string, int])
	fmt.Println("identity:", pair, isPair)

	// cross-package aliases: generic type, map, slice, func, pointer, constraint
	var a lib.Alias[int] = lib.Alias[int]{V: 5}
	a.Set(6)
	var b lib.Box[int] = a
	s := lib.Set[string]{"x": {}}
	l := lib.List[int]{1, 2, 3}
	var f lib.Mapper[int] = func(x int) int { return x * 2 }
	var ptr lib.Ptr[int] = &b
	ptr.Set(7)
	n := lib.NumBox[float64]{V: 1.5}
	fmt.Println("cross:", a.Get(), b.Get(), len(s), l, f(4), lib.Twice(f, 3), n.Get())
	fmt.Println("generic:", lib.Sum(l), lib.Sum(lib.List[float64]{0.5, 0.25}), wrap("w").Get())

	// plain aliases keep their names
	var ps Pairs = Pairs{{Key: "k", Val: 9}}
	var ib lib.IntBox = lib.NewBox(8)
	fmt.Println("plain:", ps, lib.Label("w"), lib.Unbox(ib))

	aliasedImport()
}
