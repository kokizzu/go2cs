package main

import (
	"fmt"
	"reflect"
	"sort"
	"strings"
)

// counter has BOTH receiver forms: Add/Reset take a pointer (they mutate), Value/Label
// take a value. The method set of *counter therefore holds all four, and counter's holds
// only the two value-receiver ones — the rule reflect.NumMethod and Method(i) must agree on.
type counter struct {
	n int
}

func (c *counter) Add(d int) int  { c.n += d; return c.n }
func (c *counter) Reset()         { c.n = 0 }
func (c counter) Value() int      { return c.n }
func (c counter) Label(p string) string {
	return fmt.Sprintf("%s=%d", p, c.n)
}

// unexported methods are NOT in a concrete type's reflect method table.
func (c counter) hidden() int { return -1 }

// embedded promotes Zeta into outer's table, where it sorts by NAME among outer's own
// methods rather than being appended.
type embedded struct{}

func (embedded) Zeta() string { return "zeta" }

type outer struct {
	embedded
}

func (outer) Beta() string  { return "beta" }
func (outer) Alpha() string { return "alpha" }

func names(t reflect.Type) []string {
	out := make([]string, 0, t.NumMethod())
	for i := 0; i < t.NumMethod(); i++ {
		out = append(out, t.Method(i).Name)
	}
	return out
}

func main() {
	c := &counter{}
	pv := reflect.ValueOf(c)
	pt := pv.Type()

	// The count and the walk must agree, and the walk must be in Go's order (by name).
	fmt.Println("ptr NumMethod:", pt.NumMethod(), "Value.NumMethod:", pv.NumMethod())
	fmt.Println("ptr methods:", strings.Join(names(pt), ","))
	sorted := append([]string(nil), names(pt)...)
	sort.Strings(sorted)
	fmt.Println("ptr order is sorted:", reflect.DeepEqual(sorted, names(pt)))

	// A value's method set drops the pointer-receiver methods.
	vv := reflect.ValueOf(*c)
	fmt.Println("val NumMethod:", vv.Type().NumMethod(), "methods:", strings.Join(names(vv.Type()), ","))

	// Value.Method(i) binds the receiver: Call takes only the method's own arguments,
	// and the method value's Type has the receiver removed.
	for i := 0; i < pt.NumMethod(); i++ {
		m := pt.Method(i)
		mv := pv.Method(i)
		mt := mv.Type()
		fmt.Printf("  [%d] %s index=%d pkgpath=%q numIn=%d numOut=%d\n",
			i, m.Name, m.Index, m.PkgPath, mt.NumIn(), mt.NumOut())
	}

	// Pointer-receiver call through a bound method value MUTATES the receiver.
	add := pv.Method(0)
	fmt.Println("Add kind:", add.Kind(), "in0:", add.Type().In(0).Kind())
	fmt.Println("Add(5) =", add.Call([]reflect.Value{reflect.ValueOf(5)})[0].Interface())
	fmt.Println("Add(7) =", add.Call([]reflect.Value{reflect.ValueOf(7)})[0].Interface())
	fmt.Println("receiver observed the writes:", c.n)

	// A VALUE-receiver method reached through the pointer takes a copy of the pointee
	// (this is the binding path a closed delegate cannot express).
	label := pv.MethodByName("Label")
	fmt.Println("Label:", label.Call([]reflect.Value{reflect.ValueOf("n")})[0].Interface())
	fmt.Println("Value:", pv.MethodByName("Value").Call(nil)[0].Interface())

	// A no-result method still binds and calls (Call returns an empty slice).
	reset := pv.MethodByName("Reset")
	fmt.Println("Reset outs:", len(reset.Call(nil)), "-> n =", c.n)

	// MethodByName round-trips through the same table Method(i) indexes.
	m, ok := pt.MethodByName("Value")
	fmt.Println("MethodByName Value:", ok, m.Name, m.Index, pt.Method(m.Index).Name)
	_, missing := pt.MethodByName("Nope")
	fmt.Println("MethodByName absent:", missing, "value form valid:", pv.MethodByName("Nope").IsValid())

	// Type.Method(i).Func is the UNBOUND form — receiver first.
	c2 := &counter{n: 3}
	addM, _ := pt.MethodByName("Add")
	fmt.Println("unbound Func numIn:", addM.Type.NumIn(),
		"call:", addM.Func.Call([]reflect.Value{reflect.ValueOf(c2), reflect.ValueOf(4)})[0].Interface())

	// A promoted embedded method sorts by name in place, it is not appended.
	fmt.Println("outer methods:", strings.Join(names(reflect.TypeOf(outer{})), ","))
	fmt.Println("outer Zeta:", reflect.ValueOf(outer{}).MethodByName("Zeta").Call(nil)[0].Interface())

	// An interface type's table carries its own methods, and a method value obtained
	// through an interface dispatches to the dynamic value.
	var s fmt.Stringer = stringish("hi")
	st := reflect.TypeOf(&s).Elem()
	fmt.Println("iface NumMethod:", st.NumMethod(), "name:", st.Method(0).Name)
	fmt.Println("iface dispatch:", reflect.ValueOf(s).MethodByName("String").Call(nil)[0].Interface())
}

type stringish string

func (s stringish) String() string { return "<" + string(s) + ">" }
