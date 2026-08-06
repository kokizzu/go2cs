package main

import "fmt"

// Go formats a []byte under the STRING verbs as the bytes themselves, and only under %v/%d as the
// list of numbers its elements are. The empty and nil cases are where the two readings diverge most
// visibly: %q of an empty []byte is `""`, never `"[]"`. os's TestExecutable reports a child that
// produced no output with `%q`, so a formatter that quotes the %v rendering instead reports the
// formatter's own gap in place of the failure.
func main() {
	var nilBytes []byte
	emptyBytes := []byte{}
	text := []byte("ab\n")
	raw := []byte{0xff, 0x00, 0x41}

	fmt.Printf("q: %q %q %q\n", nilBytes, emptyBytes, text)
	fmt.Printf("s: %s|%s|%s|\n", nilBytes, emptyBytes, text)
	fmt.Printf("x: %x|%x|%x|%x|\n", nilBytes, emptyBytes, text, raw)
	fmt.Printf("X: %X|\n", raw)
	fmt.Printf("v: %v %v %v\n", nilBytes, emptyBytes, text)
	fmt.Printf("d: %d %d\n", emptyBytes, text)

	// Through Sprintf as well, since that is the path a test-failure message takes.
	fmt.Println("sprintf:", fmt.Sprintf("%q", nilBytes), fmt.Sprintf("%q", text))

	// A named []byte type formats the same way its underlying type does.
	type named []byte
	fmt.Printf("named: %q %q\n", named(nil), named("hi"))
}
