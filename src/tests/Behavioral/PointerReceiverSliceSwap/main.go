// Guards the parallel-assignment fix for a pointer-write LHS that has no
// plain-ident root, in its two shapes.
//
// (1) An INDEX LHS over a parenthesized deref: `(*p)[i], (*p)[j] = (*p)[j], (*p)[i]`.
// getIdentifier does not unwrap ParenExpr, so the LHS has no ident root.
//
// (2) A STAR-DEREF LHS over a CALL result: `*c.at(i), *c.at(j) = *c.at(j), *c.at(i)`.
// getIdentifier has no CallExpr arm either, so this deref reached NEITHER the
// selector nor the index arm of the reassignment census and was counted as
// neither reassigned nor declared.
//
// In both shapes the assignment then satisfied no tuple-path gate and shattered
// into two sequential statements that dropped the swap's implicit temporary — the
// first store clobbered the value the second read, losing one element and
// duplicating the other, which makes every swap a no-op. Shape (1) is
// container/heap's myHeap.Swap; shape (2) is internal/trace/internal/oldtrace's
// Events.Swap, the only mutator sort.Stable calls, whose corruption made the
// old-trace parser reject its own event stream. Both must stay simultaneous tuple
// deconstructions. The single-element writes (`(*p)[i] = v`, `*c.at(i) = v`) must
// keep emitting plainly.
package main

import "fmt"

type ints []int

func (p *ints) swap(i, j int) { (*p)[i], (*p)[j] = (*p)[j], (*p)[i] }
func (p *ints) set(i, v int)  { (*p)[i] = v }

func show(p *ints) {
	for i, x := range *p {
		if i > 0 {
			fmt.Print(" ")
		}
		fmt.Print(x)
	}
	fmt.Println()
}

// cell/cells mirror oldtrace's Event/Events: a method returns a pointer INTO the
// backing store and the swap derefs that call on both sides of a parallel assign.
type cell struct{ v int }

type cells []cell

func (c *cells) at(i int) *cell     { return &(*c)[i] }
func (c *cells) swapAt(i, j int)    { *c.at(i), *c.at(j) = *c.at(j), *c.at(i) }
func (c *cells) setAt(i int, v int) { *c.at(i) = cell{v} }

func showCells(c *cells) {
	for i, x := range *c {
		if i > 0 {
			fmt.Print(" ")
		}
		fmt.Print(x.v)
	}
	fmt.Println()
}

func main() {
	// Two disjoint swaps then a single element write.
	p := &ints{10, 20, 30, 40}
	p.swap(0, 3)
	p.swap(1, 2)
	p.set(0, 99)
	show(p) // 99 30 20 10

	// Full reversal by repeated swaps: a lost/duplicated element corrupts it loudly.
	q := &ints{1, 2, 3, 4, 5}
	for i, j := 0, len(*q)-1; i < j; i, j = i+1, j-1 {
		q.swap(i, j)
	}
	show(q) // 5 4 3 2 1

	// Same two exercises through the call-deref shape.
	c := &cells{{10}, {20}, {30}, {40}}
	c.swapAt(0, 3)
	c.swapAt(1, 2)
	c.setAt(0, 99)
	showCells(c) // 99 30 20 10

	d := &cells{{1}, {2}, {3}, {4}, {5}}
	for i, j := 0, len(*d)-1; i < j; i, j = i+1, j-1 {
		d.swapAt(i, j)
	}
	showCells(d) // 5 4 3 2 1

	// A selection sort driven entirely by the call-deref swap: a swap that is a
	// no-op (or that duplicates one side) leaves this visibly unsorted.
	e := &cells{{5}, {3}, {9}, {1}, {7}, {2}, {8}}
	for i := 0; i < len(*e); i++ {
		min := i
		for j := i + 1; j < len(*e); j++ {
			if e.at(j).v < e.at(min).v {
				min = j
			}
		}
		if min != i {
			e.swapAt(i, min)
		}
	}
	showCells(e) // 1 2 3 5 7 8 9
}
