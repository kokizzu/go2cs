package main

// A PACKAGE-LEVEL var initializer is never hoisted — the decision is made on the GO AST position,
// not on the emitted C# shape (a package-level table is emitted into an `initᴛ*` method BODY, which
// would otherwise look like an ordinary function to a shape-based rule).
var pkgLevelLiteral = "package level literal stays inline"

// INIT-ORDER guard. `describe` lives in z_impl.go — the LAST file in conversion order — and reads a
// hoisted field declared there. As a plain C# static field initializer this could run before that
// part's initializers (cross-part order is undefined), reading `default(@string)` = "". The
// converter must therefore relocate this initializer into the ordered static constructor; the
// assertion below is that the value is NOT empty.
var derivedAtInit = describe()

type box struct {
	name string
	tag  string
}

type label string
