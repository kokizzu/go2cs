package main

import "fmt"

// TB mirrors testing.TB: a named interface with an unexported "sealing" method whose
// name (private) collides with a C# reserved keyword. The converter escapes it to
// @private, but Roslyn's IMethodSymbol.Name strips the @ when the method is pulled into
// a dynamic interface's runtime conversion class via the base-interface (AllInterfaces)
// method walk — the escape must be re-applied for the emitted C# to parse.
type TB interface {
	Name() string
	private()
}

type harness struct {
	name     string
	deadline int
}

func (h harness) Name() string          { return h.name }
func (h harness) private()              {}
func (h harness) Deadline() (int, bool) { return h.deadline, true }

// commandContext mirrors testenv.CommandContext: it type-asserts its TB argument to an
// anonymous interface that EMBEDS TB and adds a method. go2cs lifts that anonymous
// assertion target to a [GoType("dyn")] interface; go2cs-gen then generates a runtime
// conversion class implementing every inherited method — including the escaped @private().
func commandContext(t TB) {
	if td, ok := t.(interface {
		TB
		Deadline() (int, bool)
	}); ok {
		d, _ := td.Deadline()
		fmt.Println("Name:", td.Name(), "deadline:", d)
	} else {
		fmt.Println("no deadline")
	}
}


// gate exercises the OTHER source of the same keyword-named method: one declared DIRECTLY on the
// anonymous interface rather than inherited. Syntax-sourced names arrive ALREADY escaped
// (MethodDeclarationSyntax.Identifier.Text is "@private"), so the wrapper's COMPOSED member names
// -- the ByPtr/ByVal delegate types and their s_ backing fields -- must be built from the BARE name:
// `@` is legal only at the start of a token, so `s_@privateByPtr` is not one token and corrupts the
// generated class (CS0102 on a phantom `s_` member).
//
// private() is also VOID and is actually CALLED through the wrapper, which pins the forwarding
// method's two-arm dispatch: with no `else` between the arms a void method ran the by-value arm and
// then fell through to the by-pointer arm unconditionally, dereferencing a null delegate/box
// (NullReferenceException) for every value-sourced wrapper and for every pointer-sourced one whose
// Go method has a VALUE receiver. printing from private() makes both the crash and a would-be
// double dispatch observable.
type gate struct{ name string }

func (g gate) private()     { fmt.Println("sealed:", g.name) }
func (g gate) Kind() string { return "gate:" + g.name }

func direct(v any) {
	if d, ok := v.(interface {
		private()
		Kind() string
	}); ok {
		d.private()
		fmt.Println("direct:", d.Kind())
	} else {
		fmt.Println("no direct")
	}
}
func main() {
	commandContext(harness{name: "cmd", deadline: 100})

	// value-sourced wrapper
	direct(gate{name: "v"})

	// pointer-sourced wrapper over a VALUE-receiver method (no by-pointer delegate resolves)
	direct(&gate{name: "p"})
}
