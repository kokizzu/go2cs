// DynIfaceParamNameCollision guards the generated dyn-interface wrapper against a collision
// between its forwarding method's RECEIVER LOCAL and an interface-method PARAMETER of the same
// name. go2cs-gen names the wrapper's ByPtr/ByVal delegate receiver with a marker suffix but
// declared the forwarding body's local as the bare `target`, so an interface method with a
// parameter literally named `target` -- net/http.Pusher's `Push(target string, opts *PushOptions)
// error`, the real Go shape -- emitted
//
//     public error Push(@string target)
//     {
//         DTTarget target = m_target;                 // CS0136
//         ...
//         return s_PushByVal!(target, target);        // receiver in the parameter's slot, CS1503
//     }
//
// Two failures for one cause: the shadow does not compile, and the forwarded call would have
// passed the receiver where the parameter belongs. `value` is the second colliding name to cover:
// wrappers are also emitted for methods whose parameters shadow other generated names.
//
// Both a value-sourced and a pointer-sourced wrapper are exercised, and every parameter value is
// PRINTED, so a receiver/parameter mix-up shows up in the output rather than only in the build.
package main

import "fmt"

type pusher struct{ id string }

func (p pusher) Push(target string, weight int) error {
	fmt.Println("push", target, weight, "from", p.id)
	return nil
}

func (p pusher) Label() string { return "pusher:" + p.id }

type setter struct{ id string }

func (s *setter) Set(value string) { fmt.Println("set", value, "on", s.id) }

func serve(v any) {
	if p, ok := v.(interface {
		Push(target string, weight int) error
		Label() string
	}); ok {
		fmt.Println("err:", p.Push("/style.css", 7), p.Label())
	} else {
		fmt.Println("not a pusher")
	}
}

func apply(v any) {
	if s, ok := v.(interface {
		Set(value string)
	}); ok {
		s.Set("blue")
	} else {
		fmt.Println("not a setter")
	}
}

func main() {
	// value-sourced wrapper
	serve(pusher{id: "v"})

	// pointer-sourced wrapper
	serve(&pusher{id: "p"})

	// void forwarder whose only parameter shadows a generated name
	apply(&setter{id: "s"})

	// the comma-ok miss control
	apply(pusher{id: "x"})
}
