package main

import "fmt"

// Package-level TUPLE var specs whose initializers reach later-declared package
// vars (prefix/suffix) through package functions — Go initializes in dependency
// order (types.Info.InitOrder), so every spec below must relocate into the
// ordered static constructor; left inline, C# field initializers would run them
// first and read empty strings (crypto/internal/edwards25519's identity read a
// nil feOne and threw in the package type initializer).

// ONE non-blank name (the edwards25519 shape): the blank's value is discarded,
// the non-blank component is assigned directly by the relocated init method.
var single, _ = makeGreeting()

// TWO non-blank names (the darwin os shape, initCwd/initCwdErr = Getwd()): one
// relocated method evaluates the call once into a local and assigns both.
var cwd, cwdErr = fakeGetwd()

// Blank in the MIDDLE: the relocated assignments read non-adjacent components.
var head, _, tail = makeTrio()

// Depends on a RELOCATED tuple var: must relocate too — single is only assigned
// in the ordered ctor, so its dependents cannot stay field initializers.
var chained = single + "?"

// An ADDRESSED moved tuple var: heap-boxed with the default value, assigned
// through its ref property by the ordered ctor, read back through the pointer.
var boxed, _ = makeGreeting()

var prefix = "go"

var suffix = "2cs"

// Control: no dependency on any package var, so this spec keeps the inline
// once-evaluated hidden-holder emission and stays out of package_init.cs.
var safeA, safeB = constantPair()

func makeGreeting() (string, error) {
	return prefix + "-" + suffix, nil
}

func fakeGetwd() (string, error) {
	return "/" + prefix + "/" + suffix, nil
}

func makeTrio() (string, int, string) {
	return prefix + "<", len(prefix), ">" + suffix
}

func constantPair() (int, int) {
	return 10, 20
}

func readThrough(p *string) string {
	return *p
}

func main() {
	fmt.Println(single)
	fmt.Println(cwd, cwdErr)
	fmt.Println(head, tail)
	fmt.Println(chained)
	fmt.Println(readThrough(&boxed))
	fmt.Println(safeA, safeB)
}
