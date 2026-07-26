package main

import "fmt"

// Same-file FORWARD reference: first depends on base, declared later in this
// file. Go initializes in dependency order (base, then first); a naive C#
// field-initializer conversion reads base's zero value (first == 1, not 42).
var first = base + 1

var base = 41

// describe reads names, a package var declared in registry.go — a var whose
// initializer calls describe acquires that dependency TRANSITIVELY (mirrors
// syscall's Stdin = getStdHandle(…) reading zsyscall's procGetStdHandle).
func describe(n int) string {
	return names[n]
}

func main() {
	fmt.Println(first)
	fmt.Println(entryName)
	fmt.Println(entryUpper)
	fmt.Println(stdinName)
	fmt.Println(computed)
	fmt.Println(registry.count)
	fmt.Println(len(sizedTable.codes), tableSize)
	fmt.Println(len(chunkHolder.chunks), numChunks)

	// The table must be real storage, not a zero-length placeholder.
	chunkHolder.chunks[numChunks-1] = 7
	fmt.Println(chunkHolder.chunks[numChunks-1])
	fmt.Println(len(kindNames), kindNames[kindFile], kindNames[kindPipe])
	fmt.Println(pipeLabel)
}
