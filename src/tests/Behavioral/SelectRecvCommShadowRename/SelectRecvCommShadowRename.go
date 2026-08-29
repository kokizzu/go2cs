// A select whose comm clause is a RECEIVE ASSIGNMENT (`case x := <-c:`) must render the
// channel operand with the SAME shadow-renamed identifier every other reference uses.
//
// The variable-analysis pass visits a comm clause's expression only on the SEND and
// bare-RECEIVE arms; the ASSIGNMENT arm processed the LHS declarations alone, so no ident
// inside the RHS (`<-c`) ever received an identNames mapping. The select temp then emitted
// the RAW name — `var selᴛ1 = c;` — while the twelve other references in the region used the
// renamed `cΔ1`. That is CS0841 rather than CS0103 precisely because the rename was
// NECESSARY: a distinct, later `c` is declared in the same C# method scope, so the bare `c`
// binds forward to it.
//
// Guards reflect's TestChan (`all_test.go:1720`), whose `case x := <-c:` sits inside a loop
// body declaring `var c chan int` while a different `c` lives later in the same function.
package main

import "fmt"

func main() {
	for loop := 0; loop < 2; loop++ {
		// Shadow-renamed: the function declares a DIFFERENT `c` later on.
		var c chan int
		c = make(chan int, 2)
		c <- 40 + loop

		// Single-value receive assignment — the failing shape.
		select {
		case x := <-c:
			fmt.Println("recv", x)
		default:
			fmt.Println("empty")
		}

		// Two-value receive assignment — the same arm, comma-ok form.
		c <- 50 + loop
		select {
		case x, ok := <-c:
			fmt.Println("recv2", x, ok)
		default:
			fmt.Println("empty2")
		}

		// Nothing buffered: the default arm runs, proving the temp still binds.
		select {
		case x := <-c:
			fmt.Println("unexpected", x)
		default:
			fmt.Println("drained")
		}

		// A bare receive and a send in the same select keep working (the arms that
		// were already visited), mixed with the assignment arm above.
		d := make(chan int, 1)
		select {
		case d <- 7:
			fmt.Println("sent")
		case <-c:
			fmt.Println("unexpected recv")
		}
		fmt.Println("d", <-d)
	}

	// The LATER, DIFFERENT `c` whose presence forces the rename above.
	var c chan string
	c = make(chan string, 1)
	c <- "tail"
	fmt.Println(<-c)
}
