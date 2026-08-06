package main

import (
	"errors"
	"fmt"
)

// Go's nil rule does not distinguish a pointer PARAMETER from a pointer RECEIVER. Passing a nil
// `*T` to a function is legal: the function RUNS, and the nil-pointer panic happens only where the
// body actually dereferences the pointee -- exactly as NilReceiverMethods guards for the receiver
// side. This guards the same four observable consequences for parameters: the DELEGATED guard that
// never panics, the unconditional deref that does, the ordering of a side effect the body performs
// BEFORE the deref, and the untouched behavior of a non-nil argument.
//
// The eager entry alias `ref var p = ref Ꮡp.Value;` broke the first three: it dereferenced at
// function ENTRY, so a nil argument panicked before a single statement of the body could run. The
// delegated shape is the one that mattered most in practice -- `internal/concurrent`'s
// `newIndirectNode(nil)`, which stores the pointer and never derefs it, was the single root that
// took the whole of `net`'s package-init chain down.

var errNilArg = errors.New("nil argument")

type node struct {
	name string
	n    int
	next *node
}

// ---- 1. DELEGATED guard: the CALLEE decides, so the caller's body must be allowed to run ----

// checkArg is the delegated guard: this, not its caller, is what asks whether p is nil.
func checkArg(p *node, op string) error {
	if p == nil {
		return errNilArg
	}
	return nil
}

// store is the hashtriemap shape: the parameter is STORED (or passed on) and never dereferenced,
// so a nil argument is entirely legal and nothing may panic.
func store(p *node) *node {
	return &node{name: "holder", next: p}
}

// guarded delegates its nil test and only derefs on the non-nil path.
func guarded(p *node) string {
	if err := checkArg(p, "guarded"); err != nil {
		return "guarded: " + err.Error()
	}
	return "guarded: " + p.name
}

// ---- 2. UNCONDITIONAL deref: a nil argument must panic, at the field read ----

func readName(p *node) string {
	_ = checkArg(p, "readname")
	return p.name
}

// ---- 3. A side effect BEFORE the deref: Go prints first, THEN panics ----

func announce(p *node) string {
	fmt.Println("  side effect ran (param shape)")
	_ = checkArg(p, "announce")
	return p.name
}

// ---- 4. A nil-terminated walk: the parameter is repointed to the nil terminator ----

func walkLen(p *node) int {
	n := 0
	for p != nil {
		n += p.n
		p = p.next
	}
	return n
}

// ---- 5. Reference-type pointee: `*error` legally HOLDS nil and is not a deref ----

func setErr(dst *error, e error) string {
	if dst == nil {
		return "setErr: nil dst"
	}
	*dst = e
	return fmt.Sprintf("setErr: %v", *dst)
}

// ---- 6. Normalization: the parameter is REPOINTED before anything reads through it ----

func normalize(p *node) string {
	p = orDefault(p)
	return p.name
}

var fallback = &node{name: "fallback", n: 7}

func orDefault(p *node) *node {
	if p == nil {
		return fallback
	}
	return p
}

func try(label string, fn func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s -> panic: %v\n", label, r)
			return
		}
	}()
	fn()
	fmt.Printf("%s -> returned\n", label)
}

func main() {
	var nilNode *node

	fmt.Println("== 1. nil argument, DELEGATED guard: must not panic ==")
	try("checkArg(nil)", func() {
		err := checkArg(nilNode, "x")
		fmt.Printf("  checkArg -> %v (is errNilArg: %v)\n", err, err == errNilArg)
	})
	try("store(nil)", func() {
		h := store(nilNode)
		fmt.Printf("  store -> %s, next nil: %v\n", h.name, h.next == nil)
	})
	try("guarded(nil)", func() { fmt.Printf("  %s\n", guarded(nilNode)) })

	fmt.Println()
	fmt.Println("== 2. nil argument, UNCONDITIONAL deref: must panic ==")
	try("readName(nil)", func() { _ = readName(nilNode) })

	fmt.Println()
	fmt.Println("== 3. nil argument, side effect BEFORE the deref: effect then panic ==")
	try("announce(nil)", func() { _ = announce(nilNode) })

	fmt.Println()
	fmt.Println("== 4. nil-terminated walk: repointing to the terminator must not panic ==")
	chain := &node{name: "a", n: 1, next: &node{name: "b", n: 2, next: &node{name: "c", n: 3}}}
	try("walkLen(chain)", func() { fmt.Printf("  walkLen -> %d\n", walkLen(chain)) })
	try("walkLen(nil)", func() { fmt.Printf("  walkLen -> %d\n", walkLen(nilNode)) })

	fmt.Println()
	fmt.Println("== 5. *error parameter: holding nil is not a dereference ==")
	var held error
	try("setErr(&held)", func() { fmt.Printf("  %s\n", setErr(&held, errNilArg)) })
	try("setErr(nil)", func() { fmt.Printf("  %s\n", setErr(nil, errNilArg)) })

	fmt.Println()
	fmt.Println("== 6. normalization: repointed before any read ==")
	try("normalize(nil)", func() { fmt.Printf("  normalize -> %s\n", normalize(nilNode)) })
	try("normalize(chain)", func() { fmt.Printf("  normalize -> %s\n", normalize(chain)) })

	fmt.Println()
	fmt.Println("== 7. non-nil arguments: everything unchanged ==")
	try("checkArg(chain)", func() { fmt.Printf("  checkArg -> %v\n", checkArg(chain, "x")) })
	try("guarded(chain)", func() { fmt.Printf("  %s\n", guarded(chain)) })
	try("readName(chain)", func() { fmt.Printf("  readName -> %s\n", readName(chain)) })
	try("announce(chain)", func() { fmt.Printf("  announce -> %s\n", announce(chain)) })
	try("store(chain)", func() {
		h := store(chain)
		fmt.Printf("  store -> %s, next: %s\n", h.name, h.next.name)
	})

	fmt.Println()
	fmt.Println("== 8. a write through a pointer parameter still lands ==")
	rename(chain, "renamed")
	fmt.Printf("  chain.name -> %s\n", chain.name)
}

func rename(p *node, s string) {
	p.name = s
}
