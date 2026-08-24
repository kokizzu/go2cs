// A type satisfies an interface partly by PROMOTION from an embedded interface and partly by its
// own declared methods. Go accepts the assertion; the conversion did not.
//
// A promoted interface method is realized by go2cs-gen as a real C# MEMBER (the struct is declared
// to implement the embedded interface), but go2cs's runtime method set is built from EXTENSION
// methods alone, so the promoted method was invisible to every structural question asked about the
// type. `builtin.Implements<T>` answers a DIRECT assert with C# `is T` — which is why asserting to
// the embedded interface itself always worked — while any OTHER interface falls to the structural
// probe and saw only the directly-declared half. A type embedding net.Conn and adding
// ReadFrom/WriteTo therefore failed `c.(net.PacketConn)`: Go took the UDP arm, the conversion took
// TCP framing. ImplementGenerator now emits the promoted method as an extension method too, so it
// is an ordinary Go method — probe, binder and reflect all agree.
//
// Three rows, and the last two are the controls that keep the fix honest:
//
//   - wrapper  — embedded interface + own method. The defect.
//   - plain    — both methods declared directly. Worked before and must keep working; it is what
//                proved the runtime resolves satisfaction structurally when the methods exist.
//   - holder   — an ORDINARY field whose name equals its type's simple name. Go promotes nothing,
//                so it must NOT satisfy ReadWriter. Both shapes emit the IDENTICAL C# field, so
//                this is what stops any fix from resting on a name heuristic (the same ambiguity
//                that once made dwarf forward Common() through a named field).
//
// NumMethod is printed for each because the fix adds entries to the Go method set: it must add
// exactly the promoted ones and nothing else (a stray BCL interface method would inflate the count
// that reflect.Type.NumMethod reports, and encoding/json gates its Unmarshaler assert on it).
package main

import (
	"fmt"
	"reflect"

	"EmbeddedInterfaceWitness/iolike"
)

type wrapper struct {
	iolike.Reader
	prefix string
}

func (w wrapper) Write(s string) string { return w.prefix + s }

type plain struct{ tag string }

func (p plain) Read() string          { return "read:" + p.tag }
func (p plain) Write(s string) string { return "p:" + s }

type holder struct {
	Reader iolike.Reader
	prefix string
}

func (h holder) Write(s string) string { return h.prefix + s }

func check(label string, value any) {
	if rw, ok := value.(iolike.ReadWriter); ok {
		fmt.Println(label, "ReadWriter: yes", rw.Read(), rw.Write("x"))
	} else {
		fmt.Println(label, "ReadWriter: no")
	}

	if r, ok := value.(iolike.Reader); ok {
		fmt.Println(label, "Reader: yes", r.Read())
	} else {
		fmt.Println(label, "Reader: no")
	}

	fmt.Println(label, "NumMethod:", reflect.TypeOf(value).NumMethod())
}

func main() {
	check("wrapper", wrapper{Reader: iolike.Base{Tag: "base"}, prefix: "w:"})
	check("plain", plain{tag: "p"})
	check("holder", holder{Reader: iolike.Base{Tag: "held"}, prefix: "h:"})
	LocalPromotion()
	checkConflicted()
}

// LocalPromotion is Shape A's guard -- EXPORTED-case deliberately, because the defect needs the
// hoisted name to lead uppercase (a lowercase enclosing func hoists a name the heuristic already
// read as internal, and the guard would pass vacuously against the unfixed generator): a FUNCTION-LOCAL type embedding a foreign interface. The
// converter hoists it to package scope under this function's name, and the hoisted declaration is
// internal — but the name now leads with this function's exported-case letter, which is exactly
// the surface a name-heuristic accessibility reads wrong. The promoted twin the generator emits
// over the hoisted type must not out-rank the type itself (CS0051, the -tests-host Shape A:
// encoding/hex's TestEncoderDecoder_r et al.), which no package-scope test type can check --
// only a local one hoists.
func LocalPromotion() {
	type inner struct {
		iolike.Reader
	}

	w := inner{Reader: iolike.Base{Tag: "local"}}
	fmt.Println("local direct:", w.Read())

	var v any = w

	if r, ok := v.(iolike.Reader); ok {
		fmt.Println("local assert:", r.Read())
	} else {
		fmt.Println("local assert: no")
	}
}

// conflicted is Shape C's guard: bilk embeds a STRUCT providing Read at depth 1 AND the Reader
// INTERFACE also naming Read at depth 1. Go's promotion rule makes the equal-depth pair
// AMBIGUOUS and removes Read from the method set entirely -- conflicted implements NOTHING, and
// its Reader field stays nil by design. io_test.go's Buffer uses exactly this shape to knock
// bytes.Buffer's fast paths out of io.Copy. The conversion once recorded the promoted pair
// anyway; the generator amplified the record into a conformance member and a method-set twin,
// and the method Go had deleted came back at runtime -- forwarding to the nil field (JOB-010
// Shape C, eight io tests). The assert below must print no, and NumMethod must print 0.
type conflicted struct {
	iolike.Base   // struct embed: provides Read at depth 1
	iolike.Reader // interface embed: also names Read at depth 1 -- ambiguous, both removed
}

func checkConflicted() {
	var v any = conflicted{Base: iolike.Base{Tag: "conf"}}

	if _, ok := v.(iolike.Reader); ok {
		fmt.Println("conflicted Reader: yes")
	} else {
		fmt.Println("conflicted Reader: no")
	}

	fmt.Println("conflicted NumMethod:", reflect.TypeOf(v).NumMethod())
}
