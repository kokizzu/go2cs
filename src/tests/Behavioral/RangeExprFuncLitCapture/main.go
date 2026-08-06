package main

import "fmt"

// probe is the table-driven test shape: a struct whose field is a closure over
// locals declared before the loop.
type probe struct {
	name string
	run  func() int
}

func main() {
	// Slice/map locals require a capture snapshot, which the converter emits as a
	// STATEMENT before the enclosing statement. Inside a `for … range` composite
	// literal that statement has no slot of its own, so the range statement must
	// hoist it ahead of the loop.
	vals := []int{3, 1, 4, 1, 5}
	counts := map[string]int{"a": 2, "b": 7}
	total := 0

	for _, p := range []probe{
		{name: "sum", run: func() int {
			t := 0
			for _, v := range vals {
				t += v
			}
			return t
		}},
		{name: "len", run: func() int { return len(vals) }},
		{name: "map", run: func() int { return counts["a"] + counts["b"] }},
	} {
		n := p.run()
		total += n
		fmt.Println(p.name, n)
	}

	fmt.Println("total", total)

	// The same shape one level down: the range expression is a composite literal
	// whose element closes over a local declared inside another loop body.
	for round := 0; round < 2; round++ {
		seen := []string{}
		for _, add := range []func(string){
			func(s string) { seen = append(seen, s+"!") },
			func(s string) { seen = append(seen, s+"?") },
		} {
			add(fmt.Sprint(round))
		}
		fmt.Println(round, seen)
	}
}
