// Regression test for NIL map keys.
//
// Go's map accepts a nil key whenever the key type can be nil — map[any]V, map[error]V,
// map[*T]V — and the nil entry participates in every map operation: index read/write,
// comma-ok, delete, len, range, clear, and the composite literal. In go2cs a Go map is a
// `readonly struct map<K,V>` over a Dictionary<K,V>, and Dictionary REJECTS a null key with
// ArgumentNullException before its comparer is ever consulted, so every one of those
// operations threw instead of behaving like Go (sync's TestIssue40999 died on the first
// `m.Store(nil, ...)`). The nil entry now lives in a dedicated slot on the backing store.
package main

import "fmt"

type myErr struct{ msg string }

func (e *myErr) Error() string { return e.msg }

func main() {
	// ---- map[any]V ----
	m := make(map[any]string)

	m[nil] = "nil-key"
	m["a"] = "alpha"
	m[7] = "seven"

	fmt.Println("len:", len(m))
	fmt.Println("index:", m[nil])

	v, ok := m[nil]
	fmt.Println("comma-ok:", v, ok)

	// range visits the nil key like any other key
	seen, sawNil := 0, false
	for k, kv := range m {
		seen++
		if k == nil {
			sawNil = true
			fmt.Println("range nil entry:", kv)
		}
	}
	fmt.Println("range:", seen, sawNil)

	// overwrite through the nil key
	m[nil] = "again"
	fmt.Println("overwrite:", len(m), m[nil])

	// a non-nil key is unaffected by the nil entry
	fmt.Println("neighbors:", m["a"], m[7])

	// delete
	delete(m, nil)
	v, ok = m[nil]
	fmt.Println("after delete:", len(m), ok, v == "")

	// deleting an absent nil key is a no-op
	delete(m, nil)
	fmt.Println("delete again:", len(m))

	// re-add, then clear
	m[nil] = "back"
	fmt.Println("re-add:", len(m), m[nil])
	clear(m)
	_, ok = m[nil]
	fmt.Println("after clear:", len(m), ok)

	// ---- composite literal with a nil key ----
	lit := map[any]int{nil: 1, "b": 2}
	fmt.Println("literal:", len(lit), lit[nil], lit["b"])

	// ---- map[error]V ----
	em := make(map[error]int)
	em[nil] = 10
	em[&myErr{"boom"}] = 20
	fmt.Println("error map:", len(em), em[nil])

	ev, eok := em[nil]
	fmt.Println("error comma-ok:", ev, eok)

	delete(em, nil)
	_, eok = em[nil]
	fmt.Println("error after delete:", len(em), eok)

	// ---- reading a nil key from a NIL map ----
	var nm map[any]string
	nv, nok := nm[nil]
	fmt.Println("nil map:", len(nm), nv == "", nok)

	// ---- a nil key on an empty non-nil map ----
	empty := make(map[any]string)
	_, eok2 := empty[nil]
	fmt.Println("empty map:", len(empty), eok2)

	// ---- PRINTING a map that carries a nil key ----
	// fmt renders the nil entry as any other, with `<nil>` for the key; Go orders map keys through
	// internal/fmtsort, where nil compares LOW, so the nil entry leads. (Only ONE non-nil key type
	// per map here: fmtsort orders interface keys of DIFFERENT concrete types by type-descriptor
	// address, which is no more predictable in Go than it is in C#.)
	pm := map[any]int{nil: 1, "b": 2}
	fmt.Println("print:", pm)
	fmt.Printf("printf: %v\n", pm)

	only := map[any]int{nil: 5}
	fmt.Println("print only nil:", only)

	pe := map[error]int{nil: 7}
	fmt.Println("print error map:", pe)

	var pp *int
	pptr := map[*int]int{pp: 8}
	fmt.Println("print ptr map:", pptr)
}
