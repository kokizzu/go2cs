// PkgLevelFuncLitLocals guards escape analysis for the LOCALS of a function literal that is NOT
// inside any function declaration — a package-level `var` initializer, the shape every Go test
// table uses (`var tests = []struct{ name string; f func() }{{"…", func(){ … }}}`).
//
// The analysis driver walked `*ast.FuncDecl` bodies only. A literal reached through a package-level
// declaration hit a separate arm that marked its PARAMS and named results but never ran the
// define-walk, so none of its own locals were analyzed and every one of them stayed unboxed. A local
// whose capture-mode pointer-receiver method is called then emitted the VALUE receiver form, which
// binds no `ж<T>` extension overload at all (sync mutex_test's `misuseTests` table: `var mu
// sync.Mutex; mu.Unlock()` — CS1929 ×16). Writes made through such a method would also have landed
// in a copy had it compiled, so this is a silent-wrongness class as well as a build break.
//
// Every declaration form the define-walk covers is exercised inside a package-level literal, and the
// in-declaration counterparts are kept alongside as the control: both must produce the same values.
package main

import "fmt"

type counter struct {
	n int
}

// inc takes the address of a receiver FIELD, which makes it capture-mode (direct-ж): its only
// receiver overload is `ж<counter>`, so a value local on which it is called must be heap-boxed.
func (c *counter) inc(by int) {
	p := &c.n
	*p += by
}

func (c counter) get() int { return c.n }

type entry struct {
	name string
	f    func() int
}

// A package-level table of function literals — the construct that got no analysis at all.
var table = []entry{
	{"var-decl", func() int {
		var c counter // *ast.DeclStmt
		c.inc(3)
		c.inc(4)
		return c.get()
	}},
	{"short-define", func() int {
		c := counter{n: 10} // *ast.AssignStmt DEFINE
		c.inc(5)
		return c.get()
	}},
	{"for-init", func() int {
		total := 0
		for c := (counter{n: 1}); c.get() < 4; { // *ast.ForStmt init define
			c.inc(1)
			total = c.get()
		}
		return total
	}},
	{"if-init", func() int {
		if c := (counter{n: 7}); c.get() > 0 { // *ast.IfStmt init define
			c.inc(2)
			return c.get()
		}
		return -1
	}},
	{"switch-init", func() int {
		switch c := (counter{n: 20}); { // *ast.SwitchStmt init define
		default:
			c.inc(2)
			return c.get()
		}
	}},
	{"range-value", func() int {
		total := 0
		for _, c := range []counter{{n: 1}, {n: 2}} { // *ast.RangeStmt define
			c.inc(10)
			total += c.get()
		}
		return total
	}},
	{"nested-literal", func() int {
		var c counter
		inner := func() { c.inc(6) } // a literal nested inside the package-level literal
		inner()
		inner()
		return c.get()
	}},
}

// A single package-level literal assigned to a var (not inside a composite literal).
var single = func() int {
	var c counter
	c.inc(21)
	c.inc(21)
	return c.get()
}

// The in-declaration control: identical bodies that always had analysis.
func inDecl() []int {
	var out []int

	var a counter
	a.inc(3)
	a.inc(4)
	out = append(out, a.get())

	b := counter{n: 10}
	b.inc(5)
	out = append(out, b.get())

	return out
}

func main() {
	for _, e := range table {
		fmt.Println(e.name, e.f())
	}
	fmt.Println("single", single())
	fmt.Println("control", inDecl())
}
