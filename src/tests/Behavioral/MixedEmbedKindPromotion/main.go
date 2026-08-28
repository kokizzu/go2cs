// MixedEmbedKindPromotion guards depth-1 method promotion when a struct embeds MORE THAN ONE KIND
// of thing, and the interface it satisfies draws members from more than one of them.
//
// The interface-implementation generator resolved a promoted member through an embedded POINTER
// whenever the struct had exactly one, unconditionally — on the reasoning that the member's
// promotion is what type-checked the cast, so there was nothing to decide. That is true only while
// the pointer embed is the struct's ONLY promotion source. net/http's transport test declares
// `type breakableConn struct { net.Conn; *brokenState }`, where Read, Close, LocalAddr, RemoteAddr
// and the three deadline setters all come from the INTERFACE embed: the single hop claimed them
// anyway and forwarded every one through *brokenState — CS1929 x7, each naming a type that has no
// such method, which reads exactly like a corpus regression rather than a missing hop.
//
// Two shapes, one per half of the remedy:
//
//   - `mixed` embeds a POINTER and an INTERFACE. Go decides depth-1 promotion per MEMBER, routing
//     each name to the embed whose method set declares it, so the hop must be gated on what it
//     actually provides and the rest must fall through to the interface arm.
//   - `outer` embeds `holder` by VALUE, and `holder` embeds an INTERFACE. The interface-field arm
//     only ever looked at the ADAPTED struct's own fields, so a member promoted through a value hop
//     to an interface field stayed unbound and fell to the bare receiver — net/http's
//     `closeWriteTestConn`, whose Read and Write live on its embed's io.Reader/io.Writer.
//
// Write-through is asserted in both: a forwarder that reached a COPY would still compile.
package main

import "fmt"

type counter struct{ n int }

func (c *counter) Bump() int { c.n++; return c.n }

type greeter interface{ Greet() string }

type hello struct{ who string }

func (h hello) Greet() string { return "hello " + h.who }

// A POINTER embed and an INTERFACE embed side by side: Bump promotes from *counter, Greet from
// greeter, and neither embed declares the other's member.
type mixed struct {
	*counter
	greeter
}

type greetBumper interface {
	Greet() string
	Bump() int
}

// An interface embedded one level DOWN, reached through a value embed.
type holder struct {
	greeter
	tag string
}

type outer struct {
	holder
	extra int
}

func (o *outer) Extra() int { return o.extra }

type greetExtra interface {
	Greet() string
	Extra() int
}

func main() {
	c := &counter{}
	m := &mixed{c, hello{"world"}}

	var gb greetBumper = m
	fmt.Println(gb.Greet(), gb.Bump(), gb.Bump())

	// The promoted Bump reached the ORIGINAL counter, not a copy of it.
	fmt.Println(c.n)

	o := &outer{holder{hello{"deep"}, "t"}, 7}

	var ge greetExtra = o
	fmt.Println(ge.Greet(), ge.Extra(), o.tag)

	// The interface embed is a FIELD, so replacing it changes what the promotion answers — proving
	// the forwarder reads it live rather than having captured a value at cast time.
	o.greeter = hello{"replaced"}
	fmt.Println(ge.Greet())
}
