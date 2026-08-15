// The CROSS-FILE half of the local-type-alias scope guard. A `global using` is scoped to the
// whole compilation, so declaring `testFnc` and `fileMaker` here — names this package's other
// file already declares, in two of its functions — is the second shape of the same CS1537. This
// is archive/tar's reader_test.go against its writer_test.go.
//
// A single-file test could not catch it: the emission is per-file, and only the compilation sees
// both.
package main

import "fmt"

// secondWriteOps declares the same two local names a third and fourth time, again over an
// unrelated member set.
func secondWriteOps() {
	type (
		readOp struct {
			cnt int
		}
		testFnc   any // readOp
		fileMaker any // readOp
	)

	tests := []testFnc{readOp{1}, readOp{2}}
	maker := fileMaker(readOp{9})

	sum := 0

	for _, t := range tests {
		if op, ok := t.(readOp); ok {
			sum += op.cnt
		}
	}

	if op, ok := maker.(readOp); ok {
		sum += op.cnt
	}

	fmt.Println("secondWriteOps:", len(tests), sum)
}

// secondAliases repeats the real-alias name too, and reaches the same package-level type the
// other file's alias targets.
func secondAliases() {
	type hdr = Header

	h := hdr{Name: "link.txt", Size: 0}

	fmt.Println("secondAliases:", h, Header{Name: "raw", Size: 7})
}
