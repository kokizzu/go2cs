// Regression test: a FUNCTION-LOCAL type declaration that go2cs emits as a `using` ALIAS
// rather than as a nested type — a real `type X = Y` alias, or a defined type over a named
// interface such as `type X any`.
//
// Every other local type-declaration kind (struct, interface, slice, map, channel, pointer,
// named-ident) is hoisted through liftLocalTypeDecl: prefixed with its enclosing function and
// uniquified, so two functions never claim one C# name. The alias branch was the one that was
// not, and the alias it writes is a `global using` — scoped to the whole COMPILATION, not to the
// file, let alone the function. Two functions declaring `type testFnc any` therefore collided
// under one name: CS1537, "the using alias appeared previously in this namespace".
//
// archive/tar is the shape: `testFnc` is declared in writer_test.go's TestWriter AND
// TestFileWriter, and again in reader_test.go's TestFileReader, with `fileMaker` alongside it.
// Three diagnostics held all 97 of that package's verdicts.
//
// The declarations below reproduce it exactly: the same local names in two functions of this
// file and again in a second file of the same package. LocalTypeAliasScopeSecond.go carries the
// cross-file half.
package main

import "fmt"

// A package-level named type, so a local alias can target one — the case where mapping the
// lifted name against the wrong type would rename every Header in the file.
type Header struct {
	Name string
	Size int64
}

func (h Header) String() string { return fmt.Sprint(h.Name, "/", h.Size) }

// writeOps: archive/tar's TestWriter shape — a local `any` used as the table's element type.
func writeOps() {
	type (
		opWrite struct {
			str string
		}
		opClose struct {
			err string
		}
		testFnc any // opWrite | opClose
	)

	ops := []testFnc{opWrite{"abc"}, opClose{"eof"}, opWrite{"de"}}

	for _, op := range ops {
		switch v := op.(type) {
		case opWrite:
			fmt.Println("write:", v.str)
		case opClose:
			fmt.Println("close:", v.err)
		}
	}

	fmt.Println("writeOps ops:", len(ops))
}

// fileOps: the SAME local names in a second function of the SAME file, over a DIFFERENT member
// set — which is the whole point. In Go these are two unrelated types that merely share a
// spelling; one compilation-scoped alias cannot represent both.
func fileOps() {
	type (
		makeReg struct {
			size int64
		}
		makeSparse struct {
			size  int64
			holes int64
		}
		testFnc   any // makeReg | makeSparse
		fileMaker any // makeReg | makeSparse
	)

	makers := []fileMaker{makeReg{4}, makeSparse{8, 2}}
	tests := []testFnc{makeReg{1}, makeSparse{3, 1}, makeReg{2}}

	total := int64(0)

	for _, m := range makers {
		switch v := m.(type) {
		case makeReg:
			total += v.size
		case makeSparse:
			total += v.size - v.holes
		}
	}

	fmt.Println("fileOps makers:", len(makers), "tests:", len(tests), "total:", total)
}

// localAliases: the other arm of the same emission branch — a REAL `type X = Y` alias, to a
// same-package named type. The alias is interchangeable with its target in Go, and the lifted
// name must not be registered against the TARGET type, or every Header in this file would be
// renamed along with it — which is what the plain `Header` uses below prove it is not.
//
// (An alias to an unnamed composite — `type names = []string` — is deliberately absent, and stays
// absent now that it works. Its RHS was emitted with UNROOTED type arguments, `go.slice<@string>`,
// which does not resolve at compilation scope: a separate defect of the same emission line, since
// fixed and held by the PackageAliasRootedTypeArgs guard. It is package-level rather than
// function-local, so hosting it here would make this guard fail for a reason that has nothing to
// do with the scope question it exists to hold — and each guard holds one question.)
func localAliases() {
	type hdr = Header

	h := hdr{Name: "small.txt", Size: 5}
	var plain Header = h

	fmt.Println("localAliases:", h, plain, plain.String(), Header{Name: "raw", Size: 1})
}

// namer is a LOCAL interface that a FOREIGN interface's method set already satisfies. Because it
// is declared downstream of fmt, fmt.Stringer cannot inherit it, so go2cs satisfies the conversion
// with a generated `<pkg>_<Src>ᴠ<Iface>` value-adapter class instead.
type namer interface {
	String() string
}

// aliasedIfaceConv: a function-local alias to a FOREIGN named interface, whose value is then
// converted to a local one. The adapter class go2cs-gen emits is minted from the RESOLVED type —
// fmt.Stringer — because the `[assembly: GoImplement<…>]` record carries a C# type, not a
// spelling. So the CAST SITE must compose the same name from the type. Composing it from the
// spelling instead worked only while a local alias happened to be named exactly like its target:
// once the alias is lifted to `aliasedIfaceConv_S`, the site referenced
// `fmt_aliasedIfaceConv_Sᴠnamer`, a class nothing declares (CS0246).
//
// go/types' `rangeStmt` is the corpus shape this reproduces: `type Expr = ast.Expr` declared
// inside the function, converted to the package's own `positioner` by `check.errorf(lhs[i], …)`.
func aliasedIfaceConv() {
	type S = fmt.Stringer

	var s S = Header{Name: "alias.txt", Size: 2}
	var n namer = s

	// The same conversion from the target's own spelling, so the two must land on ONE adapter.
	var direct fmt.Stringer = Header{Name: "direct.txt", Size: 4}
	var d namer = direct

	fmt.Println("aliasedIfaceConv:", n.String(), d.String())
}

func main() {
	writeOps()
	fileOps()
	localAliases()
	aliasedIfaceConv()
	secondWriteOps()
	secondAliases()
}
