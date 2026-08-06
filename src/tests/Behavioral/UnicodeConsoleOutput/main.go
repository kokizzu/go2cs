package main

import "fmt"

// Guards UTF-8 fidelity of converted-program stdout. Go always writes raw UTF-8
// bytes; .NET encodes through Console.OutputEncoding, which defaults to the OEM
// code page and silently substitutes '?' for anything it cannot represent. The
// failure only shows when stdout is a pipe or a file, which is exactly how the
// behavioral harness (and the tour's ".NET Run" pane) captures it.
func main() {
	// The Tour of Go's very first lesson.
	fmt.Println("Hello, 世界")

	// Non-Latin scripts and a math symbol: unrepresentable in CP437/CP1252.
	fmt.Printf("%s|%s|%s\n", "日本語", "Ελληνικά", "Кириллица")
	fmt.Println("π ≈ 3.14159")

	// Beyond the BMP: a surrogate pair in UTF-16, four bytes in UTF-8.
	fmt.Println("🌍 earth")

	// A rune round-trips through the string as UTF-8, so len() counts bytes.
	world := "世界"
	fmt.Println(len(world), string([]rune(world)[0]))
}
