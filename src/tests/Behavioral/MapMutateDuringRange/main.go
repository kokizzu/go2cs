// Regression test for MUTATING a map while ranging over it.
//
// Go's spec explicitly permits it: "The iteration order over maps is not specified... If a map
// entry that has not yet been reached is removed during iteration, the corresponding iteration
// value will not be produced. If a map entry is created during iteration, that entry may be
// produced during the iteration or may be skipped." So a range that INSERTS a new key must
// still run to completion.
//
// golib's map<K,V> exposed .NET's Dictionary<K,V> enumerator directly, and that enumerator
// throws InvalidOperationException ("Collection was modified") the moment a structural ADD
// happens mid-enumeration. (An UPDATE of an existing key, and a Remove, are both version-free
// since .NET Core 3.0 -- only the insert bites.) That made every Go range-with-insert a
// runtime fault instead of a legal iteration.
//
// The shape is not exotic: net/http's h2 server hits it in promoteUndeclaredTrailers, which
// ranges the handler's header map and writes each promoted "Trailer:Foo" key back as "Foo".
// The exception escaped the handler goroutine, the test host's containment policy swallowed it,
// the h2 stream was never completed with trailers/END_STREAM, and the client blocked forever --
// TestServerUndeclaredTrailers/h2 hung deterministically.
package main

import "fmt"

// dump renders a map deterministically without importing sort: it probes a fixed candidate
// list, so the printed order never depends on Go's (or C#'s) map iteration order.
func dump(m map[string]int, keys []string) string {
	s := ""
	for _, k := range keys {
		if v, ok := m[k]; ok {
			s += fmt.Sprintf("%s=%d ", k, v)
		}
	}
	return fmt.Sprintf("[%s] len=%d", s, len(m))
}

func main() {
	// INSERT during range. Every inserted key is two runes long, so if the range happens to
	// produce it the body skips it -- the result is deterministic either way, which is what
	// makes this a legal Go program and a usable golden.
	insert := map[string]int{"a": 1, "b": 2, "c": 3}
	visited := 0
	for k, v := range insert {
		if len(k) == 1 {
			visited++
			insert[k+"!"] = v * 10
		}
	}
	fmt.Println("insert visited:", visited)
	fmt.Println("insert final:", dump(insert, []string{"a", "a!", "b", "b!", "c", "c!"}))

	// UPDATE of an existing key during range -- always legal, and the .NET enumerator already
	// tolerated it. Kept so the guard covers the whole family, not just the broken member.
	update := map[string]int{"x": 1, "y": 2}
	for k, v := range update {
		update[k] = v + 100
	}
	fmt.Println("update final:", dump(update, []string{"x", "y"}))

	// DELETE during range: an entry removed before it is reached is never produced, so this
	// drains the map whatever order the range picks.
	del := map[string]int{"p": 1, "q": 2, "r": 3}
	for k := range del {
		delete(del, k)
	}
	fmt.Println("delete final:", dump(del, []string{"p", "q", "r"}))

	// INSERT and DELETE together over one map, the shape promoteUndeclaredTrailers is closest
	// to: read a key, write a derived key, drop the original.
	mix := map[string]int{"m": 1, "n": 2}
	for k, v := range mix {
		if len(k) == 1 {
			mix[k+k] = v * 7
			delete(mix, k)
		}
	}
	fmt.Println("mix final:", dump(mix, []string{"m", "mm", "n", "nn"}))

	// A range that inserts into a DIFFERENT map is unaffected either way -- the control.
	src := map[string]int{"s": 5}
	dst := map[string]int{}
	for k, v := range src {
		dst[k+"-copy"] = v
	}
	fmt.Println("control final:", dump(dst, []string{"s-copy"}))
}
